param([int]$X, [int]$Y)
Add-Type -AssemblyName Microsoft.VisualBasic
$p = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) {
    [Microsoft.VisualBasic.Interaction]::AppActivate($p.Id) | Out-Null
    Start-Sleep -Milliseconds 400
}
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class MouseClicker {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, 0);
        mouse_event(0x0004, 0, 0, 0, 0);
    }
}
"@
[MouseClicker]::Click($X, $Y)
Write-Output ("clicked " + $X + "," + $Y)
