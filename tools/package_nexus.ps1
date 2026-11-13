param(
    [string]$Version,
    [string]$Dotnet = 'dotnet',
    [string]$Preview = 'Art\nexus\eleanor_story_expand_nexus_wide_v1.png'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$manifest = Get-Content (Join-Path $root 'manifest.xml') -Raw
if (!$Version) { $Version = [string]$manifest.Manifest.ModVersion }
if($manifest.Manifest.ModVersion -ne $Version) { throw 'Nexus version differs from manifest.' }

# Both the Workshop and Nexus packages use the same bundled beauty set.
# Eleanor's CutePortraits base portraits live in Textures/Items; Lydia and
# Mother use the project's own beautified portraits. No external mod is copied.
$csproj = Join-Path $root 'EleanorRomance.csproj'
& $Dotnet build $csproj -c Release | Out-Host
if($LASTEXITCODE -ne 0) { throw 'Nexus build failed.' }
$dll = Join-Path $root 'bin/Release/EleanorRomance.dll'
$deps = Join-Path $root 'bin/Release/EleanorRomance.deps.json'
if(!(Test-Path -LiteralPath $dll)) { throw "Release DLL missing: $dll" }

$parent = Join-Path $root ('dist/nexus-' + $Version)
$stage = Join-Path $parent 'EleanorRomance'
if(Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$null = New-Item -ItemType Directory -Path $stage -Force

Copy-Item -LiteralPath (Join-Path $root 'manifest.xml') -Destination (Join-Path $stage 'manifest.xml') -Force
$stagedManifest = Join-Path $stage 'manifest.xml'
[xml]$staged = Get-Content $stagedManifest -Raw
$staged.Manifest.Name = [string]$staged.Manifest.Name + ' [Nexus]'
$staged.Manifest.Description = '[Nexus package] Bundled beautified Eleanor portraits (CutePortraits, used with permission); Lydia and Mother use project beauty art. The separate CutePortraits mod is not required. ' + [string]$staged.Manifest.Description
$staged.Save($stagedManifest)

Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $stage 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'docs/NEXUS_README.txt') -Destination (Join-Path $stage 'NEXUS_README.txt') -Force
$previewSource = Join-Path $root $Preview
if(Test-Path -LiteralPath $previewSource) {
    Copy-Item -LiteralPath $previewSource -Destination (Join-Path $stage 'preview.png') -Force
} else {
    Copy-Item -LiteralPath (Join-Path $root 'preview.png') -Destination (Join-Path $stage 'preview.png') -Force
}

Copy-Item -LiteralPath $dll -Destination (Join-Path $stage 'EleanorRomance.dll') -Force
if(Test-Path -LiteralPath $deps) {
    Copy-Item -LiteralPath $deps -Destination (Join-Path $stage 'EleanorRomance.deps.json') -Force
}

foreach($folder in @('Localization','Textures/Items')) {
    $destination = Join-Path $stage $folder
    $null = New-Item -ItemType Directory -Path $destination -Force
    Get-ChildItem -LiteralPath (Join-Path $root $folder) -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destination $_.Name) -Force
    }
}

$beauty = Get-ChildItem -LiteralPath (Join-Path $root 'Textures/Items') -File -Filter 'cp_portrait_eleanor*.png'
if($beauty.Count -lt 5) { throw "Bundled Eleanor portrait set is incomplete: $($beauty.Count) files" }

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
    variant = 'nexus'
    bundledEleanorPortraits = $beauty.Count
    files = $inventory.Count
    bytes = ($inventory | Measure-Object length -Sum).Sum
    dllSHA256 = (Get-FileHash -LiteralPath (Join-Path $stage 'EleanorRomance.dll') -Algorithm SHA256).Hash
    archive = $archive
    archiveSHA256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $parent 'package-result.json') -Encoding utf8
$result
