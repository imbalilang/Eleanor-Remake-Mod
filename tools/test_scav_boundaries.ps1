# Native event registration, scheduler, saved state, modifier aggregation,
# attempt limits and injury chances remain real. Supply a store-presence getter
# consistent with the harness singleton and a deterministic float RNG draw.
$il=Body (Method $game 'PlayerStore' 'get_HasInstance')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,((GetCType $game 'PlayerStore').Fields | Where-Object Name -eq 'TestInstance'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Cgt_Un)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$rng=GetCType $game 'RNG'
foreach($pair in @(@('TestScavRollValue',$game.MainModule.TypeSystem.Single),@('TestScavRollCount',$game.MainModule.TypeSystem.Int32),@('TestScavRollChance',$game.MainModule.TypeSystem.Single))){
    $rng.Fields.Add([Mono.Cecil.FieldDefinition]::new($pair[0],[Mono.Cecil.FieldAttributes]'Public,Static',$pair[1]))
}
$draw=$rng.Fields | Where-Object Name -eq 'TestScavRollValue'
$count=$rng.Fields | Where-Object Name -eq 'TestScavRollCount'
$chance=$rng.Fields | Where-Object Name -eq 'TestScavRollChance'
$roll=$rng.Methods | Where-Object {$_.Name -eq 'Roll' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'System.Single'}
$il=Body $roll
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$count);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$count)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$chance)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$draw);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Clt);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Ownership of the flashlight is an unrelated inventory/UI boundary. Native
# injury immunity still returns before this call; base injury calculations run.
$il=Body (Method $game 'EmporiumEntry' 'IsAfterhourHaveOwnedItems')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
