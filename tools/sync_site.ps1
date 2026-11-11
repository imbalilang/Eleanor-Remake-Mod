# Publish docs/route_map.html as the GitHub Pages site (site/index.html).
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "docs\route_map.html"
$destDir = Join-Path $root "site"
$dest = Join-Path $destDir "index.html"

if (-not (Test-Path $src)) { throw "Missing route map: $src" }
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -LiteralPath $src -Destination $dest -Force

$srcHash = (Get-FileHash -LiteralPath $src -Algorithm SHA256).Hash
$destHash = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash
if ($srcHash -ne $destHash) { throw "Site sync hash mismatch" }

Write-Host "Synced docs/route_map.html -> site/index.html"
Write-Host "Version: $((Select-String -LiteralPath $src -Pattern '"version": "([^"]+)"').Matches.Groups[1].Value)"
