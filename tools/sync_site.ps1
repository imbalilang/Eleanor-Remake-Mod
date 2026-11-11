# Publish docs/route_map.html as the GitHub Pages site (site/index.html).
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "docs\route_map.html"
$destDir = Join-Path $root "site"
$dest = Join-Path $destDir "index.html"

if (-not (Test-Path $src)) { throw "Missing route map: $src" }
New-Item -ItemType Directory -Force -Path $destDir | Out-Null

# The source lives in docs/ and loads artwork from site/assets when opened
# locally; the published copy sits at the site root and uses assets/ instead.
$html = Get-Content -LiteralPath $src -Raw -Encoding UTF8
$html = $html.Replace('../site/assets/', 'assets/')
if ($html -match '\.\./site/assets/') { throw "Site sync left an unresolved asset path" }

$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($dest, $html, $utf8)

Write-Host "Synced docs/route_map.html -> site/index.html"
Write-Host "Version: $((Select-String -LiteralPath $src -Pattern '"version": "([^"]+)"').Matches.Groups[1].Value)"
