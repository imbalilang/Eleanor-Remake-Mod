# Rendering/placement boundaries only. Story graphs, cash, choices and state remain real.
$qaStore=GetCType $game 'PlayerStore'
$qaStock=[Mono.Cecil.FieldDefinition]::new('TestReleaseStock',[Mono.Cecil.FieldAttributes]'Public,Static',
    $game.MainModule.ImportReference([System.Collections.Generic.List[string]]))
$qaStore.Fields.Add($qaStock)
$qaPlacement=$qaStore.Methods | Where-Object {
    $_.Name -eq 'AddDirectSellingItemToTable' -and $_.Parameters[0].ParameterType.FullName -eq 'GameItem'
} | Select-Object -First 1
$il=Body $qaPlacement
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$qaStock)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,(Method $game 'GameItem' 'get_identifier'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([System.Collections.Generic.List[string]].GetMethod('Add')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $game 'StartingPerk' 'IsPerkActive')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $game 'StoreClientList' 'AddScavGeneralStock')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$qaStock)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'native_scav_stock')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([System.Collections.Generic.List[string]].GetMethod('Add')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $mod 'EleanorRomance.StoryEngine' 'FreeNutrifruitOnce'
Ret $mod 'EleanorRomance.MetaProgress' 'UnlockTrueEnding'
if(!$ChapterNight){Ret $mod 'EleanorRomance.LoveA2Closing' 'CloseAndSleep'}
# This fixture has no native reputation/perk tree. Economy and native quotes
# run in the separate -Maid suite; schedule replays use the neutral cash quote.
$il=Body (Method $mod 'EleanorRomance.MaidProjects' 'NativePrice')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'StoreClientManager' 'TrackClient'
$il=Body (Method $game 'StoreClientManager' 'IsClientBlockedByKnownSnitch')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $mod 'EleanorRomance.EndingPageUI' 'ShowRoutePage')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
