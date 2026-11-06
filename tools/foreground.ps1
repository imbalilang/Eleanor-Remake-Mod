Add-Type @"
using System;
using System.Runtime.InteropServices;
public class ForegroundHelper {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
"@
$p = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "not running"; exit }
$h = $p.MainWindowHandle
[ForegroundHelper]::ShowWindow($h, 9) | Out-Null
[ForegroundHelper]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0001 -bor 0x0002) | Out-Null
[ForegroundHelper]::SetWindowPos($h, [IntPtr](-2), 0, 0, 0, 0, 0x0001 -bor 0x0002) | Out-Null
[ForegroundHelper]::BringWindowToTop($h) | Out-Null
[ForegroundHelper]::SetForegroundWindow($h) | Out-Null
Write-Output "foreground requested"
