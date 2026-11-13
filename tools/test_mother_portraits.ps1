#requires -Version 7.0
# Execute the compiled resolver and save migration with only mod detection and
# Unity sprite loading replaced by fixtures. Assemblies are changed in memory.
param(
    [Parameter(Mandatory=$true)][string]$Managed,
    [string]$ModDll = (Join-Path $PSScriptRoot '..\bin\Release\EleanorRomance.dll')
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Managed 'Unity.Burst.Cecil.dll')
$resolve = [Func[System.Runtime.Loader.AssemblyLoadContext,System.Reflection.AssemblyName,System.Reflection.Assembly]]({
    param($context,$name)
    if ($name.Name.EndsWith('.resources')) { return $null }
    $candidate = [IO.Path]::Combine($Managed, $name.Name + '.dll')
    if ([IO.File]::Exists($candidate)) { return $context.LoadFromAssemblyPath($candidate) }
    return $null
}.GetNewClosure())
[System.Runtime.Loader.AssemblyLoadContext]::Default.add_Resolving($resolve)
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($Managed)
$read = [Mono.Cecil.ReaderParameters]::new()
$read.AssemblyResolver = $resolver
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ModDll,$read)
$compat = $mod.MainModule.GetType('EleanorRomance.PortraitCompat')
function Method($type,$name) { $type.Methods | Where-Object Name -eq $name | Select-Object -First 1 }
function Body($method) {
    $method.Body = [Mono.Cecil.Cil.MethodBody]::new($method)
    return $method.Body.GetILProcessor()
}
function Assert($condition,$message) { if (!$condition) { throw $message } }
foreach ($pair in @(@('EleanorRomance.EleanorStory','CreateMotherChapter'),@('EleanorRomance.MaidStory','CreateMother'))) {
    $method = Method ($mod.MainModule.GetType($pair[0])) $pair[1]
    Assert (@($method.Body.Instructions | Where-Object {
        $_.OpCode.Code -eq 'Call' -and $_.Operand.Name -eq 'MotherSpriteKey'
    }).Count -eq 1) 'Public mother visit no longer selects the daily portrait.'
}
$dailySelector = Method ($mod.MainModule.GetType('EleanorRomance.EleanorStory')) 'MotherSpriteKey'
Assert (@($dailySelector.Body.Instructions | Where-Object {
    $_.OpCode.Code -eq 'Call' -and $_.Operand.Name -eq 'ResolveMother'
}).Count -eq 1) 'Daily portrait selector calls the wrong outfit resolver.'
$has = [Mono.Cecil.FieldDefinition]::new('TestHasPortrait',[Mono.Cecil.FieldAttributes]'Public,Static',
    $mod.MainModule.ImportReference([Func[string,bool]]))
$compat.Fields.Add($has)
$il = Body (Method $compat 'EnsureLocalPortrait')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$has)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Func[string,bool]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$stream = [IO.MemoryStream]::new()
$mod.Write($stream); $stream.Position = 0
$null = [System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromStream($stream)
$stream.Dispose()
$script:portraits = [System.Collections.Generic.HashSet[string]]::new()
[EleanorRomance.PortraitCompat]::TestHasPortrait = [Func[string,bool]]{ param($key) $script:portraits.Contains($key) }
$daily = [EleanorRomance.ModConstants]::MotherPortraitBeauty
$homeKey = [EleanorRomance.ModConstants]::MotherHomePortraitBeauty
$legacy = [EleanorRomance.ModConstants]::MotherPortraitVanilla
$sanitize = [EleanorRomance.PortraitCompat].GetMethod('SanitizeClient',[Reflection.BindingFlags]'NonPublic,Static')
function Restore-Portrait($key,$id='er_story_mother') {
    $client = [System.Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([StoreClient])
    $client.spriteName = $key; $client.identifier = $id
    $null = $sanitize.Invoke($null,@($client))
    return $client.spriteName
}
$null = $script:portraits.Add($daily); $null = $script:portraits.Add($homeKey)
Assert ([EleanorRomance.PortraitCompat]::ResolveMother() -eq $daily) 'Daily art is unavailable.'
Assert ([EleanorRomance.PortraitCompat]::ResolveMotherHome() -eq $homeKey) 'Home art is unavailable.'
Assert ((Restore-Portrait $homeKey) -eq $homeKey) 'Save migration changed home clothes to daily clothes.'
foreach ($key in @($daily,$legacy,'femaleUpper3','cp_portrait_femaleUpper3_female')) {
    foreach ($id in @('er_story_mother','er_story_maid_m17')) {
        Assert ((Restore-Portrait $key $id) -eq $daily) "Saved mother visit failed to upgrade: $key / $id"
    }
}
Assert ((Restore-Portrait 'femaleUpper3' 'other_mod_client') -eq 'femaleUpper3') 'Unrelated customer was changed.'
'PASS: daily/home selection, public visit call sites, legacy keys, generic-key upgrades and home outfit preservation.'
$null = $script:portraits.Remove($homeKey)
Assert ([EleanorRomance.PortraitCompat]::ResolveMotherHome() -eq $daily) 'Missing home art did not fall back to daily.'
Assert ((Restore-Portrait $homeKey) -eq $daily) 'Saved home key did not recover from missing art.'
$null = $script:portraits.Remove($daily)
Assert ([EleanorRomance.PortraitCompat]::ResolveMother() -eq 'femaleUpper3') 'Missing daily art did not use the game fallback.'
Assert ([EleanorRomance.PortraitCompat]::ResolveMotherHome() -eq 'femaleUpper3') 'Missing both outfits did not use the game fallback.'
$null = $script:portraits.Add($daily); $null = $script:portraits.Add($homeKey)
Assert ([EleanorRomance.PortraitCompat]::ResolveMother() -eq $daily) 'Restored daily art is unavailable.'
Assert ((Restore-Portrait 'femaleUpper3') -eq $daily) 'A saved public visit was not upgraded.'
'PASS: missing home/daily art, save repair and restored-art upgrade.'
