# Dot-sourced before loading the test assemblies. Retain native buying
# predicates, stock factories and dialogue callbacks; replace stock placement
# and item rendering at the presentation boundary.
$t=GetCType $mod 'EleanorRomance.MaidBusiness'
$stockHook=[Mono.Cecil.FieldDefinition]::new('TestPutStock',[Mono.Cecil.FieldAttributes]'Public,Static',
    $mod.MainModule.ImportReference([Action[object]]))
$t.Fields.Add($stockHook)
$il=Body (Method $mod 'EleanorRomance.MaidBusiness' 'PutStock')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$stockHook)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Action[object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'GameItemElement' 'Destroy'
