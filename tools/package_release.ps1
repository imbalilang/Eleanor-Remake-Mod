param([string]$Version='0.77')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
[xml]$manifest=Get-Content (Join-Path $root 'manifest.xml') -Raw
if($manifest.Manifest.ModVersion -ne $Version){throw 'Release version differs from manifest.'}
$parent=Join-Path $root ('dist/workshop-'+$Version)
$stage=Join-Path $parent 'EleanorRomance'
$null=New-Item -ItemType Directory -Path $stage -Force
foreach($file in @('manifest.xml','README.md','preview.png')){Copy-Item -LiteralPath (Join-Path $root $file) -Destination (Join-Path $stage $file) -Force}
foreach($file in @('EleanorRomance.dll','EleanorRomance.deps.json')){Copy-Item -LiteralPath (Join-Path $root ('bin/Release/'+$file)) -Destination (Join-Path $stage $file) -Force}
foreach($folder in @('Localization','Textures/Items')){
    $null=New-Item -ItemType Directory -Path (Join-Path $stage $folder) -Force
    Get-ChildItem -LiteralPath (Join-Path $root $folder) -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $stage ($folder+'/'+$_.Name)) -Force
    }
}
$inventory=@(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{path=$_.FullName.Substring($stage.Length+1).Replace('\','/');length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
$inventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $parent 'release-files.json') -Encoding utf8
$archive=Join-Path $root ('dist/EleanorRomance-'+$Version+'.zip')
Compress-Archive -LiteralPath $stage -DestinationPath $archive -Force
[pscustomobject]@{version=$Version;files=$inventory.Count;bytes=($inventory|Measure-Object length -Sum).Sum;folder=$stage;archive=$archive;sha256=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash}
