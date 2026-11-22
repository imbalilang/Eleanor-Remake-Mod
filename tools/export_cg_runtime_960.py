from pathlib import Path
from PIL import Image
import argparse, hashlib, json, sys

MASTER_SIZES={(1536,864):('openai',0.625),(1280,720):('grok',0.75),(1024,576):('hunyuan',0.9375)}; RUNTIME=(960,540)
FILTERS={
    'lanczos': Image.Resampling.LANCZOS,
    'bicubic': Image.Resampling.BICUBIC,
    'box': Image.Resampling.BOX,
}

def sha256(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for b in iter(lambda:f.read(1<<20),b''): h.update(b)
    return h.hexdigest()

def main():
    ap=argparse.ArgumentParser(description='Export an OpenAI 1536x864 or Grok 1280x720 CG master to 960x540 lossless PNG.')
    ap.add_argument('input'); ap.add_argument('output')
    ap.add_argument('--filter', choices=sorted(FILTERS), default='lanczos')
    ap.add_argument('--provider', choices=['auto','openai','grok','hunyuan'], default='auto', help='source provider; auto infers 1536=openai, 1280=grok')
    a=ap.parse_args()
    src=Image.open(a.input)
    if src.size not in MASTER_SIZES:
        allowed=' or '.join(f'{w}x{h}' for w,h in MASTER_SIZES)
        raise SystemExit(f'input must be {allowed}, got {src.size}')
    size_provider, scale = MASTER_SIZES[src.size]
    provider = size_provider if a.provider == 'auto' else a.provider
    if provider == 'grok' and src.size != (1280,720):
        raise SystemExit('grok master must be 1280x720')
    if provider == 'hunyuan' and src.size != (1024,576):
        raise SystemExit('hunyuan master must be 1024x576')
    has_alpha = src.mode in ('RGBA','LA') or (src.mode=='P' and 'transparency' in src.info)
    im=src.convert('RGBA' if has_alpha else 'RGB')
    out=im.resize(RUNTIME, FILTERS[a.filter])
    outp=Path(a.output); outp.parent.mkdir(parents=True, exist_ok=True)
    out.save(outp, format='PNG', optimize=True)
    with Image.open(outp) as chk:
        if chk.size != RUNTIME: raise SystemExit(f'output size mismatch: {chk.size}')
    report={
        'input':str(Path(a.input)), 'input_size':list(src.size), 'input_sha256':sha256(a.input),
        'output':str(outp), 'output_size':list(out.size), 'output_sha256':sha256(outp),
        'provider':provider, 'scale':scale, 'filter':a.filter, 'format':'png_lossless', 'alpha':has_alpha,
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))

if __name__=='__main__': main()
