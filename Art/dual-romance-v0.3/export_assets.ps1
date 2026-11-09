$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=$PSScriptRoot
$specs=@(
 @('lydia_daily_vanilla','portraits/er_portrait_lydia_female_vanilla.png',288,212,156,158),
 @('lydia_daily_beauty','portraits/er_portrait_lydia_female_beauty.png',288,212,156,158),
 @('lydia_date_vanilla','portraits/er_portrait_lydia_date_female_vanilla.png',288,212,156,158),
 @('lydia_date_beauty','portraits/er_portrait_lydia_date_female_beauty.png',288,212,156,158),
 @('lydia_recommendation_plaque','items/er_lydia_recommendation_plaque.png',64,64,52,48)
)
& 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe' -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'export_palette.ps1')
if($LASTEXITCODE -ne 0){throw 'Palette export failed'}
$report=@()
foreach($spec in $specs){
 $path=Join-Path $root $spec[1];$d=[System.Drawing.Bitmap]::new($path)
 $alpha=[System.Collections.Generic.HashSet[int]]::new();$colors=[System.Collections.Generic.HashSet[int]]::new()
 for($y=0;$y -lt $d.Height;$y++){for($x=0;$x -lt $d.Width;$x++){$c=$d.GetPixel($x,$y);[void]$alpha.Add([int]$c.A);if($c.A -gt 0){[void]$colors.Add($c.ToArgb())}}}
 $report += [pscustomobject]@{file=$spec[1];width=$d.Width;height=$d.Height;format=$d.PixelFormat.ToString();alphaValues=@($alpha);opaqueColors=$colors.Count;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
 $d.Dispose()
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'asset_validation.json') -Encoding UTF8
# Contact sheet is a viewing aid only, not a game texture.
$sheet=[System.Drawing.Bitmap]::new(1152,968)
$g=[System.Drawing.Graphics]::FromImage($sheet);$g.Clear([System.Drawing.Color]::FromArgb(43,38,49));$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font=[System.Drawing.Font]::new('Arial',17)
$labels=@('Original - Daily','Beauty - Daily','Original - Date','Beauty - Date')
for($i=0;$i -lt 4;$i++){$im=[System.Drawing.Image]::FromFile((Join-Path $root $specs[$i][1]));$x=($i%2)*576;$y=[int][Math]::Floor($i/2)*464;$g.DrawString($labels[$i],$font,[System.Drawing.Brushes]::White,[single]($x+24),[single]($y+12));$g.DrawImage($im,[System.Drawing.Rectangle]::new($x,$y+36,576,424));$im.Dispose()}
$font.Dispose();$g.Dispose();$sheet.Save((Join-Path $root 'portrait_contact_sheet.png'),[System.Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose()
$report | Format-Table file,width,height,opaqueColors

