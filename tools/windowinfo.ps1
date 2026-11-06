Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinInfo {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT pt);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
}
"@
$p = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "not running"; exit }
$h = $p.MainWindowHandle
$wr = New-Object WinInfo+RECT
$cr = New-Object WinInfo+RECT
[WinInfo]::GetWindowRect($h, [ref]$wr) | Out-Null
[WinInfo]::GetClientRect($h, [ref]$cr) | Out-Null
Write-Output ("window " + $wr.Left + "," + $wr.Top + " " + ($wr.Right-$wr.Left) + "x" + ($wr.Bottom-$wr.Top))
Write-Output ("client " + $cr.Left + "," + $cr.Top + " " + ($cr.Right-$cr.Left) + "x" + ($cr.Bottom-$cr.Top))
Write-Output ("dpi " + [WinInfo]::GetDpiForWindow($h))
