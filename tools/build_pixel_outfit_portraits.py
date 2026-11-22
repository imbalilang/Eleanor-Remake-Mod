from pathlib import Path
from collections import deque
from datetime import datetime
import hashlib, json, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(r"B:\Eleanor Story Expand")
GEN = ROOT / "dsh-image-gen"
OUT = ROOT / "Art/character-assets-2026-11-20/portrait_pixel_outfit_2026-11-22"
MASTERS = OUT / "masters_alpha"
RUNTIME = OUT / "runtime_288"
PREVIEWS = OUT / "previews"
REPORTS = OUT / "reports"
ARCHIVE = OUT / "archive"
for p in (MASTERS, RUNTIME, PREVIEWS, REPORTS, ARCHIVE):
    p.mkdir(parents=True, exist_ok=True)

JOBS = [
    {"key":"cp_portrait_eleanor1", "src":ROOT/"Textures/Items/cp_portrait_eleanor1.png", "copy":True},
    {"key":"cp_portrait_eleanor_scav", "src":GEN/"image-3fc5e468.png"},
    {"key":"er_portrait_eleanor_maid_female_beauty", "src":GEN/"image-e13c6d72.png"},
    {"key":"er_portrait_eleanor_gala_female_beauty", "src":GEN/"image-46044cc5.png"},
    {"key":"er_portrait_eleanor_love_intimate_female_beauty", "src":GEN/"image-167a71e6.png"},
    {"key":"cp_portrait_eleanor_rev", "src":GEN/"image-63ea2ed2.png"},
    {"key":"er_portrait_eleanor_revolution_undersuit_female_beauty", "src":GEN/"image-7264e3f7.png"},
    {"key":"er_portrait_lydia_female_beauty", "src":GEN/"image-08cbb283.png"},
    {"key":"er_portrait_lydia_date_female_beauty", "src":GEN/"image-46687350.png"},
    {"key":"er_portrait_sophia_female_beauty", "src":GEN/"image-d826e1dd.png"},
    {"key":"er_portrait_sophia_home_female_beauty", "src":GEN/"image-a2b53efc.png"},
]
ALTS = [
    {"key":"er_portrait_sophia_home_v2_female_beauty", "src":GEN/"image-3a2c4f05.png", "alt_of":"sophia_home"},
    {"key":"er_portrait_sophia_green_v1_female_beauty", "src":GEN/"image-5725ff8f.png", "alt_of":"sophia"},
    {"key":"er_portrait_sophia_green_v2_female_beauty", "src":GEN/"image-ab47921e.png", "alt_of":"sophia"},
]

def sha(path):
    h=hashlib.sha256()
    with open(path,"rb") as f:
        for b in iter(lambda:f.read(1<<20),b""): h.update(b)
    return h.hexdigest()

def white_key(im):
    a=np.array(im.convert("RGBA")); rgb=a[:,:,:3]; h,w=rgb.shape[:2]
    near=(rgb.min(axis=2)>=238)&((rgb.max(axis=2)-rgb.min(axis=2))<=14)
    seen=np.zeros((h,w),dtype=bool); q=deque()
    # Seed top, left and right only. Do not seed bottom: portrait is cut at the waist.
    for x in range(w):
        if near[0,x]: q.append((0,x)); seen[0,x]=True
    for y in range(h):
        for x in (0,w-1):
            if near[y,x] and not seen[y,x]: q.append((y,x)); seen[y,x]=True
    while q:
        y,x=q.popleft()
        for ny,nx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1)):
            if 0<=ny<h and 0<=nx<w and near[ny,nx] and not seen[ny,nx]:
                seen[ny,nx]=True; q.append((ny,nx))
    a[:,:,3]=np.where(seen,0,255)
    # Remove near-white fringe only where it touches transparent background.
    trans=seen
    adj=np.zeros_like(trans)
    adj[1:,:]|=trans[:-1,:]; adj[:-1,:]|=trans[1:,:]; adj[:,1:]|=trans[:,:-1]; adj[:,:-1]|=trans[:,1:]
    fringe=(a[:,:,3]==255)&adj&(rgb.min(axis=2)>=238)&((rgb.max(axis=2)-rgb.min(axis=2))<=14)
    a[:,:,3][fringe]=0
    return Image.fromarray(a,"RGBA"), "white_key"

def harden_alpha(im):
    a=np.array(im.convert("RGBA")); al=a[:,:,3]
    if int((al==0).sum())<100:
        return white_key(im)
    out=a.copy(); out[:,:,3]=np.where(al>=128,255,0)
    return Image.fromarray(out,"RGBA"), "alpha_threshold"

def clean_source(path):
    im=Image.open(path).convert("RGBA")
    # If the image has a meaningful alpha channel, use it; otherwise key the white background.
    a=np.array(im); al=a[:,:,3]
    if int((al==0).sum())>100 and int((al>0).sum())>100:
        return harden_alpha(im)
    return white_key(im)

def main():
    entries=[]
    for job in JOBS:
        key=job["key"]; src=job["src"]; out=RUNTIME/(key+".png")
        if job.get("copy"):
            im=Image.open(src).convert("RGBA"); im.save(out,optimize=True)
            cleaned=MASTERS/(key+".png"); im.save(cleaned,optimize=True)
            entry={"key":key,"source":str(src.relative_to(ROOT)),"status":"copied_existing_workshop_target","output":str(out.relative_to(ROOT)),"size":im.size,"colors":len(np.unique(np.array(im)[np.array(im)[:,:,3]>0,:3],axis=0)),"sha256":sha(out)}
        else:
            cleaned_im, method=clean_source(src)
            cleaned=MASTERS/(key+".png"); cleaned_im.save(cleaned,optimize=True)
            cmd=[sys.executable,str(ROOT/"tools/build_bust_portrait_asset.py"),"--source",str(cleaned),"--output",str(out),"--profile","cp","--no-magenta-key","--archive-dir",str(ARCHIVE/key)]
            r=subprocess.run(cmd,capture_output=True,text=True)
            if r.returncode!=0:
                print("FAIL",key,r.stdout,r.stderr); sys.exit(1)
            im=Image.open(out).convert("RGBA"); aa=np.array(im); al=aa[:,:,3]
            entry={"key":key,"source":str(src.relative_to(ROOT)),"alpha_method":method,"status":"candidate_not_installed","output":str(out.relative_to(ROOT)),"size":im.size,"bbox":list(im.getchannel("A").getbbox()),"alpha_values":sorted(set(np.unique(al).tolist())),"colors":len(np.unique(aa[al>0,:3],axis=0)),"sha256":sha(out)}
        entries.append(entry); print(key,"ok")
    # alternates
    for job in ALTS:
        key=job["key"]; src=job["src"]; out=RUNTIME/(key+".png")
        cleaned_im, method=clean_source(src); cleaned=MASTERS/(key+".png"); cleaned_im.save(cleaned,optimize=True)
        cmd=[sys.executable,str(ROOT/"tools/build_bust_portrait_asset.py"),"--source",str(cleaned),"--output",str(out),"--profile","cp","--no-magenta-key","--archive-dir",str(ARCHIVE/key)]
        r=subprocess.run(cmd,capture_output=True,text=True)
        if r.returncode!=0:
            print("FAIL",key,r.stdout,r.stderr); sys.exit(1)
        im=Image.open(out).convert("RGBA"); aa=np.array(im); al=aa[:,:,3]
        entries.append({"key":key,"source":str(src.relative_to(ROOT)),"alpha_method":method,"status":"alternate_candidate_not_installed","alt_of":job["alt_of"],"output":str(out.relative_to(ROOT)),"size":im.size,"bbox":list(im.getchannel("A").getbbox()),"alpha_values":sorted(set(np.unique(al).tolist())),"colors":len(np.unique(aa[al>0,:3],axis=0)),"sha256":sha(out)})
        print(key,"ok (alt)")
    # previews and sheet
    items=[]
    for e in entries:
        im=Image.open(ROOT/e["output"]).convert("RGBA")
        name=e["key"]+(" [ALT]" if e.get("alt_of") else "")
        items.append((name,im,e))
        bg=Image.new("RGBA",im.size,(37,44,53,255)); bg.alpha_composite(im)
        bg.convert("RGB").resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(PREVIEWS/(e["key"]+"_gray_4x.png"),optimize=True)
    cols=3; tw,th=288*3,212*3; rows=(len(items)+cols-1)//cols
    sheet=Image.new("RGB",(cols*tw,rows*(th+28)),(30,32,38)); dr=ImageDraw.Draw(sheet)
    for i,(name,im,e) in enumerate(items):
        bg=Image.new("RGBA",im.size,(37,44,53,255)); bg.alpha_composite(im)
        x=(i%cols)*tw; y=(i//cols)*(th+28)+28
        sheet.paste(bg.convert("RGB").resize((tw,th),Image.Resampling.NEAREST),(x,y)); dr.text((x+8,y-20),name,fill=(255,255,255))
    sheet_path=OUT/"review_contact_sheet_gray_3x.jpg"; sheet.save(sheet_path,quality=93)
    report={"date":"2026-11-22","status":"candidates_pending_review","installed":False,"profile":"cp","visible_height":150,"canvas":[288,212],"source_style":"author QQ pixel outfit-swap batch 2026-11-20 03:44-03:52","contact_sheet":str(sheet_path.relative_to(ROOT)),"entries":entries}
    (REPORTS/"pixel_outfit_batch_report.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps({"entries":len(entries),"sheet":str(sheet_path),"report":str(REPORTS/'pixel_outfit_batch_report.json')},ensure_ascii=False,indent=2))

if __name__=="__main__":
    main()
