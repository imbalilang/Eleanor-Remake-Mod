# Start the OpenRouter image relay in a hidden process.
$ErrorActionPreference = "Stop"
$node = "D:\Program Files\nodejs\node.exe"
$server = Join-Path $PSScriptRoot "server.mjs"
if (-not (Test-Path $node)) { throw "node.exe not found at $node" }
Start-Process -FilePath $node -ArgumentList "`"$server`"" -WindowStyle Hidden
Start-Sleep -Seconds 1
try {
    $health = Invoke-RestMethod -Uri "http://127.0.0.1:18787/health" -TimeoutSec 5
    "relay running: $($health | ConvertTo-Json -Compress)"
} catch {
    "relay did not answer on 18787: $($_.Exception.Message)"
}
