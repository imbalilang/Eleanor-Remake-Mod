Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName Microsoft.VisualBasic

$p = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) {
    [Microsoft.VisualBasic.Interaction]::AppActivate($p.Id) | Out-Null
    Start-Sleep -Milliseconds 900
}

$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
$out = Join-Path $env:TEMP "ps_game_screen.png"
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Output ("saved " + $out)
