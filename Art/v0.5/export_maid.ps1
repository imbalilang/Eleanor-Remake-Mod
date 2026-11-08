param([ValidateSet('Beauty','Vanilla','All')][string]$Variant='All')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
public static class ERArtExport {
  class P { public int r,g,b,n; public P(int R,int G,int B,int N){r=R;g=G;b=B;n=N;} }
  static int Range(List<P> a,int ch){int lo=255,hi=0;foreach(var p in a){int v=ch==0?p.r:ch==1?p.g:p.b;lo=Math.Min(lo,v);hi=Math.Max(hi,v);}return hi-lo;}
  static Rectangle OpaqueBounds(Bitmap src) {
    int x0=src.Width,y0=src.Height,x1=-1,y1=-1;
    for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A>=128){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
    if(x1<0)throw new Exception("Empty source alpha");
    return new Rectangle(x0,y0,x1-x0+1,y1-y0+1);
  }
  public static void ApplyEyeRetouch(string original,string edited,string output) {
    // The model edits the eyes. Export only those clusters, registering against
    // the original silhouette, and retain the original 48-colour sprite palette.
    // Every other production pixel, including alpha, is left byte-identical.
    using(var src=new Bitmap(original))using(var edit=new Bitmap(edited))using(var baseSprite=new Bitmap(output))using(var dst=new Bitmap(baseSprite)) {
      var oldBox=OpaqueBounds(src);var box=OpaqueBounds(edit);
      double scale=Math.Min(144.0/oldBox.Width,158.0/oldBox.Height);
      int dw=(int)Math.Round(oldBox.Width*scale),dh=(int)Math.Round(oldBox.Height*scale);
      int dx=(288-dw)/2,dy=54+158-dh;
      var palette=new List<Color>();var seen=new HashSet<int>();
      for(int y=0;y<baseSprite.Height;y++)for(int x=0;x<baseSprite.Width;x++){
        var c=baseSprite.GetPixel(x,y);if(c.A==255&&seen.Add(c.ToArgb()))palette.Add(c);
      }
      var regions=new[]{new Rectangle(129,95,12,7),new Rectangle(149,95,12,7)};
      foreach(var region in regions)for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++) {
        if(baseSprite.GetPixel(x,y).A!=255)continue;
        int sx=box.X+Math.Min(box.Width-1,(int)((x-dx+.5)*box.Width/dw));
        int sy=box.Y+Math.Min(box.Height-1,(int)((y-dy+.5)*box.Height/dh));
        var c=edit.GetPixel(sx,sy);if(c.A<128)throw new Exception("Retouch removed opaque eye pixels");
        long best=long.MaxValue;Color chosen=palette[0];
        foreach(var q in palette){long r=c.R-q.R,g=c.G-q.G,b=c.B-q.B,d=2*r*r+3*g*g+b*b;if(d<best){best=d;chosen=q;}}
        dst.SetPixel(x,y,chosen);
      }
      baseSprite.Dispose();dst.Save(output,ImageFormat.Png);
    }
  }
  public static void Export(string input,string output,int width,int height,int top,int maxW,int maxH,int colors) {
    using(var src=new Bitmap(input)) using(var dst=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
      int x0=src.Width,y0=src.Height,x1=-1,y1=-1;
      for(int y=0;y<src.Height;y++) for(int x=0;x<src.Width;x++) if(src.GetPixel(x,y).A>=128){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
      if(x1<0)throw new Exception("Empty alpha: "+input);
      int sw=x1-x0+1,sh=y1-y0+1;
      double scale=Math.Min((double)maxW/sw,(double)maxH/sh);
      int dw=Math.Max(1,(int)Math.Round(sw*scale)),dh=Math.Max(1,(int)Math.Round(sh*scale));
      int dx=(width-dw)/2,dy=top<0?(height-dh)/2:top+maxH-dh;
      var histogram=new Dictionary<int,int>();
      for(int y=0;y<dh;y++) for(int x=0;x<dw;x++) {
        int sx=x0+Math.Min(sw-1,(int)((x+.5)*sw/dw)),sy=y0+Math.Min(sh-1,(int)((y+.5)*sh/dh));
        var c=src.GetPixel(sx,sy);if(c.A<128)continue;
        c=Color.FromArgb(255,c.R,c.G,c.B);dst.SetPixel(dx+x,dy+y,c);
        int k=c.ToArgb();histogram[k]=histogram.ContainsKey(k)?histogram[k]+1:1;
      }
      var initial=new List<P>();foreach(var kv in histogram){var c=Color.FromArgb(kv.Key);initial.Add(new P(c.R,c.G,c.B,kv.Value));}
      var boxes=new List<List<P>>{initial};
      while(boxes.Count<colors){
        int best=-1,axis=0;double score=-1;
        for(int i=0;i<boxes.Count;i++){if(boxes[i].Count<2)continue;int n=0;foreach(var p in boxes[i])n+=p.n;for(int ch=0;ch<3;ch++){double s=Range(boxes[i],ch)*Math.Sqrt(n);if(s>score){score=s;best=i;axis=ch;}}}
        if(best<0)break;var box=boxes[best];int chn=axis;box.Sort((a,b)=>(chn==0?a.r:chn==1?a.g:a.b).CompareTo(chn==0?b.r:chn==1?b.g:b.b));
        int total=0;foreach(var p in box)total+=p.n;int acc=0,cut=1;for(int i=0;i<box.Count-1;i++){acc+=box[i].n;cut=i+1;if(acc>=total/2)break;}
        boxes[best]=box.GetRange(0,cut);boxes.Add(box.GetRange(cut,box.Count-cut));
      }
      var palette=new List<Color>();foreach(var b in boxes){long r=0,g=0,bl=0,n=0;foreach(var p in b){r+=(long)p.r*p.n;g+=(long)p.g*p.n;bl+=(long)p.b*p.n;n+=p.n;}palette.Add(Color.FromArgb(255,(int)(r/n),(int)(g/n),(int)(bl/n)));}
      var map=new Dictionary<int,Color>();foreach(var kv in histogram){var c=Color.FromArgb(kv.Key);long best=long.MaxValue;Color chosen=palette[0];foreach(var q in palette){long dr=c.R-q.R,dg=c.G-q.G,db=c.B-q.B,d=2*dr*dr+3*dg*dg+db*db;if(d<best){best=d;chosen=q;}}map[kv.Key]=chosen;}
      for(int y=0;y<height;y++)for(int x=0;x<width;x++){var c=dst.GetPixel(x,y);if(c.A==255)dst.SetPixel(x,y,map[c.ToArgb()]);}
      dst.Save(output,ImageFormat.Png);
    }
  }
}
'@
$artRoot = $PSScriptRoot
if($Variant -in @('Beauty','All')){
 [ERArtExport]::Export((Join-Path $artRoot 'masters/eleanor_maid_master.png'),(Join-Path $artRoot '../../Textures/Items/er_portrait_eleanor_maid_female_beauty.png'),288,212,54,144,158,64)
}
if($Variant -in @('Vanilla','All')){
 [ERArtExport]::Export((Join-Path $artRoot 'masters/eleanor_maid_vanilla_master.png'),(Join-Path $artRoot '../../Textures/Items/er_portrait_eleanor_maid_female_vanilla.png'),288,212,54,144,158,48)
 $eyeEdit=Join-Path $artRoot 'masters/eleanor_maid_vanilla_eye_edit.png'
 if(Test-Path -LiteralPath $eyeEdit){
   [ERArtExport]::ApplyEyeRetouch((Join-Path $artRoot 'masters/eleanor_maid_vanilla_master.png'),$eyeEdit,(Join-Path $artRoot '../../Textures/Items/er_portrait_eleanor_maid_female_vanilla.png'))
 }
}
