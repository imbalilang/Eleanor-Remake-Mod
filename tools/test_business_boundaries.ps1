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
$businessPrefix=Method $mod 'EleanorRomance.MaidBusinessBuyerFilterPatch' 'Prefix'
if($businessPrefix){
    $businessPrefix.IsPrivate=$false;$businessPrefix.IsPublic=$true
    $businessPrefix.DeclaringType.IsNotPublic=$false;$businessPrefix.DeclaringType.IsPublic=$true
    $method=Method $game 'StoreClient' 'IsClientRefusingItem'
    $il=$method.Body.GetILProcessor();$first=$method.Body.Instructions[0]
    $result=[Mono.Cecil.Cil.VariableDefinition]::new($game.MainModule.TypeSystem.Boolean)
    $method.Body.Variables.Add($result);$method.Body.InitLocals=$true
    foreach($instruction in @(
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloca,$result),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($businessPrefix)),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$first),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloc,$result),
        [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret)
    )){$il.InsertBefore($first,$instruction)}
}
Ret $game 'GameItemElement' 'Destroy'
$colorMethod=Method $game 'TypeHelper' 'HexToColor'
$color=[Mono.Cecil.Cil.VariableDefinition]::new($colorMethod.ReturnType)
$il=Body $colorMethod
$colorMethod.Body.Variables.Add($color);$colorMethod.Body.InitLocals=$true
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloca,$color)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Initobj,$colorMethod.ReturnType)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc,$color)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$colorMethod=Method $game 'TabletStyle' 'Hex'
$color=[Mono.Cecil.Cil.VariableDefinition]::new($colorMethod.ReturnType)
$il=Body $colorMethod
$colorMethod.Body.Variables.Add($color);$colorMethod.Body.InitLocals=$true
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloca,$color)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Initobj,$colorMethod.ReturnType)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc,$color)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$tabletType=GetCType $game 'TabletUI'
$tagRows=[Mono.Cecil.FieldDefinition]::new('TestInfoRows',[Mono.Cecil.FieldAttributes]'Public,Static',
    $game.MainModule.ImportReference([Action[string,object]]))
$tabletType.Fields.Add($tagRows)
$il=Body (Method $game 'TabletUI' 'InfoRow')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$tagRows)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_3)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_S,[byte]4)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([Action[string,object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4,[single]24)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
