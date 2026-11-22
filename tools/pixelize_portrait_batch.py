from pathlib import Path
import hashlib
import json
import subprocess
import sys

from PIL import Image, ImageDraw
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / 'tools' / 'build_bust_portrait_asset.py'
SRC_DIR = ROOT / 'Art/character-assets-2026-11-20/portrait_regeneration_2026-11-20/transparent_masters'
OUT_DIR = ROOT / 'Art/character-assets-2026-11-20/portrait_pixelization_2026-11-21/batch_11'
BASE_DIR = OUT_DIR / 'baseline_288'
PIXEL_DIR = OUT_DIR / 'pixel_masters_288'
PREVIEW_DIR = OUT_DIR / 'previews'
REPORT_DIR = OUT_DIR / 'reports'
for p in (BASE_DIR, PIXEL_DIR, PREVIEW_DIR, REPORT_DIR):
    p.mkdir(parents=True, exist_ok=True)

ENTRIES = [
    ('eleanor1', 'eleanor1_transparent_master.png'),
    ('eleanor_scav', 'eleanor_scav_transparent_master.png'),
    ('eleanor_maid', 'eleanor_maid_transparent_master.png'),
    ('eleanor_gala', 'eleanor_gala_transparent_master.png'),
    ('eleanor_love_intimate', 'eleanor_love_intimate_transparent_master.png'),
    ('eleanor_rev', 'eleanor_rev_transparent_master.png'),
    ('eleanor_revolution_undersuit', 'eleanor_revolution_undersuit_transparent_master.png'),
    ('lydia_daily', 'lydia_daily_transparent_master.png'),
    ('lydia_date', 'lydia_date_transparent_master.png'),
    ('sophia', 'sophia_transparent_master.png'),
    ('sophia_home', 'sophia_home_transparent_master.png'),
]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def metrics(im):
    a = np.array(im.convert('RGBA'))
    alpha = a[:, :, 3]
    opaque = a[alpha > 0, :3]
    return {
        'size': list(im.size),
        'bbox': list(im.getchannel('A').getbbox()),
        'alpha_values': sorted(np.unique(alpha).tolist()),
        'opaque_colours': int(len(np.unique(opaque, axis=0))),
    }

def pixelize(im, colors=128):
    im = im.convert('RGBA')
    a = np.array(im)
    a[a[:, :, 3] < 128, 3] = 0
    a[a[:, :, 3] >= 128, 3] = 255
    im = Image.fromarray(a, 'RGBA')
    q = im.convert('RGB').quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert('RGBA')
    q.putalpha(im.getchannel('A'))
    return q

def previews(im, stem):
    for name, bg in [('white', (255,255,255,255)), ('gray', (37,44,53,255)), ('black', (0,0,0,255))]:
        canvas = Image.new('RGBA', im.size, bg)
        canvas.alpha_composite(im)
        canvas.convert('RGB').resize((im.width*4, im.height*4), Image.Resampling.NEAREST).save(PREVIEW_DIR / f'{stem}_{name}_4x.png', optimize=True)

entries = []
for key, filename in ENTRIES:
    src = SRC_DIR / filename
    baseline = BASE_DIR / f'{key}_baseline_288.png'
    if not src.exists():
        raise SystemExit(f'missing source: {src}')
    subprocess.run([
        sys.executable, str(BUILD),
        '--source', str(src),
        '--output', str(baseline),
        '--profile', 'cp',
        '--runtime-key', key,
        '--no-magenta-key',
        '--archive-dir', str(OUT_DIR / 'baseline_transparent_masters'),
    ], check=True, cwd=str(ROOT), stdout=subprocess.DEVNULL)
    base_im = Image.open(baseline).convert('RGBA')
    pixel = pixelize(base_im)
    pixel_path = PIXEL_DIR / f'{key}_288.png'
    pixel.save(pixel_path, optimize=True)
    previews(pixel, key)
    entries.append({
        'key': key,
        'source': {'file': str(src), 'sha256': sha(src)},
        'baseline': {'file': str(baseline), 'sha256': sha(baseline), 'metrics': metrics(base_im)},
        'pixel': {'file': str(pixel_path), 'sha256': sha(pixel_path), 'metrics': metrics(pixel)},
    })

# contact sheet 4x on gray
cell_w, cell_h = 288*2, 212*2
cols = 4
rows = (len(entries) + cols - 1) // cols
sheet = Image.new('RGB', (cols*cell_w, rows*(cell_h+30)), (37,44,53))
draw = ImageDraw.Draw(sheet)
for i, e in enumerate(entries):
    im = Image.open(e['pixel']['file']).convert('RGBA')
    bg = Image.new('RGBA', im.size, (37,44,53,255)); bg.alpha_composite(im)
    sheet.paste(bg.convert('RGB').resize((cell_w, cell_h), Image.Resampling.NEAREST),
                ((i%cols)*cell_w, (i//cols)*(cell_h+30)+30))
    draw.text(((i%cols)*cell_w+8, (i//cols)*(cell_h+30)+8), e['key'], fill=(255,255,255))
sheet_path = OUT_DIR / 'batch_11_pixel_contact_sheet.jpg'
sheet.save(sheet_path, quality=93)

report = {
    'status': 'candidates_not_installed',
    'method': 'existing build_bust_portrait_asset.py baseline + local palette/nearest pixelization',
    'pixelization': {'palette_colors': 128, 'alpha_threshold': 128, 'dither': 'NONE'},
    'contact_sheet': str(sheet_path),
    'entries': entries,
}
report_path = REPORT_DIR / 'batch_11_pixelization_report.json'
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'entries': len(entries), 'contact_sheet': str(sheet_path), 'report': str(report_path)}, ensure_ascii=False, indent=2))
