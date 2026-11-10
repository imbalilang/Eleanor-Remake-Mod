param(
    [string]$Version,
    [string]$Dotnet = 'dotnet',
    [string]$BeautyTextures = 'D:\Program Files (x86)\Steam\steamapps\workshop\content\4348910\3829696854\Textures\Items',
    [string]$Preview = 'Art\nexus\eleanor_story_expand_nexus_wide_v1.png'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$manifest = Get-Content (Join-Path $root 'manifest.xml') -Raw
if (!$Version) { $Version = [string]$manifest.Manifest.ModVersion }
if($manifest.Manifest.ModVersion -ne $Version) { throw 'Nexus version differs from manifest.' }
if(!(Test-Path -LiteralPath $BeautyTextures)) { throw "Beauty portrait source not found: $BeautyTextures" }

# Build the Nexus-only DLL.  NEXUS_BEAUTY makes PortraitCompat use the
# bundled CutePortraits portraits without requiring the separate mod.
$csproj = Join-Path $root 'EleanorRomance.csproj'
& $Dotnet build $csproj -c Release -p:NexusBeauty=true -p:OutputPath=bin\Nexus\ | Out-Host
if($LASTEXITCODE -ne 0) { throw 'Nexus build failed.' }
$nexusDll = Join-Path $root 'bin/Nexus/EleanorRomance.dll'
$nexusDeps = Join-Path $root 'bin/Nexus/EleanorRomance.deps.json'
if(!(Test-Path -LiteralPath $nexusDll)) { throw "Nexus DLL missing: $nexusDll" }

$parent = Join-Path $root ('dist/nexus-' + $Version)
$stage = Join-Path $parent 'EleanorRomance'
if(Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$null = New-Item -ItemType Directory -Path $stage -Force

Copy-Item -LiteralPath (Join-Path $root 'manifest.xml') -Destination (Join-Path $stage 'manifest.xml') -Force
$stagedManifest = Join-Path $stage 'manifest.xml'
[xml]$staged = Get-Content $stagedManifest -Raw
$staged.Manifest.Name = [string]$staged.Manifest.Name + ' [Nexus Beauty]'
$staged.Manifest.Description = '[Nexus Beauty Edition] Bundled beautified portraits; do not enable the separate CutePortraits mod with this package. ' + [string]$staged.Manifest.Description
$staged.Save($stagedManifest)

Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $stage 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'docs/NEXUS_README.txt') -Destination (Join-Path $stage 'NEXUS_README.txt') -Force
$previewSource = Join-Path $root $Preview
if(Test-Path -LiteralPath $previewSource) {
    Copy-Item -LiteralPath $previewSource -Destination (Join-Path $stage 'preview.png') -Force
} else {
    Copy-Item -LiteralPath (Join-Path $root 'preview.png') -Destination (Join-Path $stage 'preview.png') -Force
}

Copy-Item -LiteralPath $nexusDll -Destination (Join-Path $stage 'EleanorRomance.dll') -Force
if(Test-Path -LiteralPath $nexusDeps) {
    Copy-Item -LiteralPath $nexusDeps -Destination (Join-Path $stage 'EleanorRomance.deps.json') -Force
}

foreach($folder in @('Localization','Textures/Items')) {
    $destination = Join-Path $stage $folder
    $null = New-Item -ItemType Directory -Path $destination -Force
    Get-ChildItem -LiteralPath (Join-Path $root $folder) -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destination $_.Name) -Force
    }
}

# Bundle the authorized CutePortraits portrait set.  The Nexus DLL maps any
# matching vanilla portrait key to these local cp_portrait_* sprites.
$beauty = Get-ChildItem -LiteralPath $BeautyTextures -File -Filter 'cp_portrait_*.png'
if($beauty.Count -eq 0) { throw "No cp_portrait_*.png files found in $BeautyTextures" }
$textures = Join-Path $stage 'Textures/Items'
foreach($file in $beauty) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $textures $file.Name) -Force
}

$inventory = @(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{
        path = $_.FullName.Substring($stage.Length + 1).Replace('\','/')
        length = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
})
$inventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $parent 'release-files.json') -Encoding utf8

$archive = Join-Path $root ('dist/EleanorRomance-' + $Version + '-nexus.zip')
Compress-Archive -LiteralPath $stage -DestinationPath $archive -Force
$result = [pscustomobject]@{
    version = $Version
    variant = 'nexus-beauty'
    beautyPortraits = $beauty.Count
    beautySource = $BeautyTextures
    files = $inventory.Count
    bytes = ($inventory | Measure-Object length -Sum).Sum
    dllSHA256 = (Get-FileHash -LiteralPath (Join-Path $stage 'EleanorRomance.dll') -Algorithm SHA256).Hash
    archive = $archive
    archiveSHA256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $parent 'package-result.json') -Encoding utf8
$result
