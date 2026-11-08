# Dot-sourced before the existing harness loads its in-memory assemblies.
# Keep real upgrade pricing/registration, reputation arithmetic/perk thresholds,
# purchase service, story graphs and native news activation. Replace UI and I/O.
# Keep the native tab predicate and NetworkUpgradeLayout.Build intact, stopping
# immediately after the tab model is stored, before any Unity widgets are made.
# Apply the real Harmony prefix at the same boundary as the installed game.
$buildTabs=Method $game 'WildNetworkUI' 'BuildTabs'
$tabWrite=@($buildTabs.Body.Instructions | Where-Object {
    $_.OpCode.Code -eq 'Stsfld' -and $_.Operand.Name -eq 'tabs'
})[0]
$cut=$buildTabs.Body.Instructions.IndexOf($tabWrite)+1
while($buildTabs.Body.Instructions.Count -gt $cut){$buildTabs.Body.Instructions.RemoveAt($cut)}
$buildTabs.Body.GetILProcessor().Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$layoutPatch=GetCType $mod 'EleanorRomance.MaidProjectLayoutPatch'
if($layoutPatch){
    $prefix=$layoutPatch.Methods | Where-Object Name -eq 'Prefix'
    $layoutPatch.IsPublic=$true;$prefix.IsPublic=$true
    $buildTabs.Body.GetILProcessor().InsertBefore($buildTabs.Body.Instructions[0],
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($prefix)))
}
# Legacy OpenUI has no layout model; retain its production prefix and omit
# only the subsequent Unity widget updates.
$legacyOpen=Method $game 'WildUIManager' 'OpenUI'
$il=Body $legacyOpen
$legacyPatch=GetCType $mod 'EleanorRomance.MaidLegacyProjectLayoutPatch'
if($legacyPatch){
    $prefix=$legacyPatch.Methods | Where-Object Name -eq 'Prefix'
    $legacyPatch.IsPublic=$true;$prefix.IsPublic=$true
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($prefix))
}
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
foreach($typeName in @('ReputationUIManager','AchievementTracker')){
    $t=GetCType $game $typeName
    $f=[Mono.Cecil.FieldDefinition]::new('TestInstance',[Mono.Cecil.FieldAttributes]'Public,Static',$t)
    $t.Fields.Add($f)
    $il=Body (Method $game $typeName 'get_Instance')
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
Ret $game 'AchievementTracker' 'CheckPerfectMediator'
$t=GetCType $game 'ReputationUIManager'
$hook=[Mono.Cecil.FieldDefinition]::new('TestNotify',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.ImportReference([Action]))
$t.Fields.Add($hook)
$il=Body (Method $game 'ReputationUIManager' 'NotifyReputation')
$end=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$hook);$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse,$end)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$hook)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([Action].GetMethod('Invoke')));$il.Append($end)
$t=GetCType $game 'Difficulty'
$f=[Mono.Cecil.FieldDefinition]::new('TestRules',[Mono.Cecil.FieldAttributes]'Public,Static',(GetCType $game 'DifficultyRules'))
$t.Fields.Add($f);$il=Body (Method $game 'Difficulty' 'get_Rules')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
foreach($pair in @(@('PlayerStore','SaveGame'),@('SaveManager','Save'))){
    $t=GetCType $game $pair[0]
    $f=[Mono.Cecil.FieldDefinition]::new('TestSaveCount',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.TypeSystem.Int32)
    $t.Fields.Add($f);$il=Body (Method $game $pair[0] $pair[1])
    $end=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.MaidProjects' 'BeforeSave')))
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse,$end)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$f);$il.Append($end)
}
Ret $mod 'EleanorRomance.MetaProgress' 'UnlockTrueEnding'
Ret $game 'WildUIManager' 'OnElementClicked'
Ret $game 'WildNetworkUI' 'RefreshAll'
Ret $mod 'EleanorRomance.StoryEngine' 'FreeNutrifruitOnce'
Ret $game 'StoreClientList' 'AddScavGeneralStock'
Ret $game 'StoreClient' 'AddBasicDialog'
# Apply the production price prefix while retaining native pricing and rounding.
$cost=Method $game 'NetworkUpgrade' 'GetCost'
$costPatch=GetCType $mod 'EleanorRomance.MaidProjectCostPatch';$costPatch.IsPublic=$true
$costPrefix=Method $mod $costPatch.FullName 'Prefix';$costPrefix.IsPublic=$true
$il=$cost.Body.GetILProcessor();$first=$cost.Body.Instructions[0]
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($costPrefix)))
# Simulate synchronous observers/errors after a cash mutation, instead of the
# removed reputation-spending side effect. The native cash arithmetic fixture stays.
$t=GetCType $game 'PlayerStore'
$cashHook=[Mono.Cecil.FieldDefinition]::new('TestProjectCashHook',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.ImportReference([Action]))
$t.Fields.Add($cashHook)
$cashMutation=Method $game 'PlayerStore' 'ModCash' 2
$il=$cashMutation.Body.GetILProcessor();$ret=$cashMutation.Body.Instructions | Where-Object {$_.OpCode.Code -eq 'Ret'} | Select-Object -Last 1
foreach($i in @(
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$cashHook),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$ret),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$cashHook),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([Action].GetMethod('Invoke')))
)){$il.InsertBefore($ret,$i)}
$il=Body (Method $game 'SpriteDict' 'GetRandomUpperFemaleSpriteName')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'femaleUL1');$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $game 'SpriteDict' 'GetRandomUpperMaleSpriteName')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'maleUL1');$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Asset presence is supplied below; the whitelist and compatibility decisions stay real.
foreach($f in (GetCType $game 'LoadedMod').Fields){$f.IsPrivate=$false;$f.IsPublic=$true}
$t=GetCType $game 'ModLoader'
$f=[Mono.Cecil.FieldDefinition]::new('TestMods',[Mono.Cecil.FieldAttributes]'Public,Static',(Method $game 'ModLoader' 'get_Mods').ReturnType)
$t.Fields.Add($f);$il=Body (Method $game 'ModLoader' 'get_Mods')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$t=GetCType $game 'SpriteDict'
$f=[Mono.Cecil.FieldDefinition]::new('TestInstance',[Mono.Cecil.FieldAttributes]'Public,Static',$t)
$t.Fields.Add($f);$il=Body (Method $game 'SpriteDict' 'get_Instance')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
