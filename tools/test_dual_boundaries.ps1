# In-memory native assemblies only. Preserve sale arithmetic, buyer budget
# deduction, ownership transfer and the production dual state/graph methods.
$all=[Mono.Cecil.FieldDefinition]::new('TestAllItems',[Mono.Cecil.FieldAttributes]'Public,Static',
    (Method $game 'PlayerStore' 'FindAllItem').ReturnType)
(GetCType $game 'PlayerStore').Fields.Add($all)
foreach($pair in @(@('PlayerStore','FindAllItem'),@('EmporiumEntry','GetAllItems'))){
    $m=Method $game $pair[0] $pair[1];$il=$m.Body.GetILProcessor();$first=$m.Body.Instructions[0]
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$all))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$first))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$all))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
}
foreach($name in @('GetRandomUpperFemaleSpriteName','GetRandomUpperMaleSpriteName')){
    $il=Body (Method $game 'SpriteDict' $name);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'femaleUpper3');$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
foreach($pair in @(@('PlayerStore','PerformOnSoldCheck'),@('StoreClient','TrackItemVariety'),@('StoreReputation','OnItemTraded'),@('RunStats','OnItemSold'),@('SoldItem','TrackItem'),@('StallWatchdog','Crumb'))){
    Ret $game $pair[0] $pair[1]
}
$price=Method $game 'GameItem' 'GetNegociatedValue'
$field=[Mono.Cecil.FieldDefinition]::new('TestPrice',[Mono.Cecil.FieldAttributes]'Public', $price.ReturnType)
(GetCType $game 'GameItem').Fields.Add($field)
$usePrice=[Mono.Cecil.FieldDefinition]::new('TestUsePrice',[Mono.Cecil.FieldAttributes]'Public',$game.MainModule.TypeSystem.Boolean)
(GetCType $game 'GameItem').Fields.Add($usePrice)
$il=$price.Body.GetILProcessor();$first=$price.Body.Instructions[0]
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldfld,$usePrice))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$first))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldfld,$field))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
# Placement boundary: successful gifts actually enter the supported inventory fixture.
foreach($name in @('TryAcceptAllMid','TryAcceptAll')){
    $m=Method $game 'GraphUtils' $name 3;$il=$m.Body.GetILProcessor();$first=$m.Body.Instructions[0]
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$all))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$first))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,((GetCType $game 'GraphUtils').Fields|Where-Object Name -eq 'TestRemaining')))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$first))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$all))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1))
    $add=[Mono.Cecil.MethodReference]::new('Add',$game.MainModule.TypeSystem.Void,$all.FieldType)
    $add.HasThis=$true;$add.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($all.FieldType.ElementType.GenericParameters[0]))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Callvirt,$add))
}
# Apply exactly the production capture/post-sale boundary without starting Unity/Harmony.
$on=Method $game 'StoreClient' 'OnItemBought';$il=$on.Body.GetILProcessor()
foreach($ret in @($on.Body.Instructions|Where-Object {$_.OpCode.Code -eq 'Ret'})){
    $captureStart=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
    foreach($branch in @($on.Body.Instructions)){
        if($branch.Operand -is [Mono.Cecil.Cil.Instruction] -and [object]::ReferenceEquals($branch.Operand,$ret)){$branch.Operand=$captureStart}
    }
    $il.InsertBefore($ret,$captureStart)
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1))
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_2))
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DualShowcaseEffects' 'Capture'))))
}
$sell=Method $game 'PlayerStore' 'SellItem';$il=$sell.Body.GetILProcessor();$first=$sell.Body.Instructions[0]
$state=[Mono.Cecil.Cil.VariableDefinition]::new($game.MainModule.ImportReference((GetCType $mod 'EleanorRomance.DualShowcaseEffects/Sale')))
$sell.Body.Variables.Add($state);$sell.Body.InitLocals=$true
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DualShowcaseEffects' 'BeginSale'))))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Stloc,$state))
foreach($instruction in @($sell.Body.Instructions)){
    if($instruction.OpCode.Code -eq 'Ldsfld' -and $instruction.Operand.FullName -eq 'PlayerStore PlayerStore::instance'){
        $instruction.OpCode=[Mono.Cecil.Cil.OpCodes]::Call;$instruction.Operand=Method $game 'PlayerStore' 'get_Instance'
    }
    if($instruction.OpCode.Code -eq 'Ret'){
        $il.InsertBefore($instruction,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloc,$state))
        $il.InsertBefore($instruction,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1))
        $il.InsertBefore($instruction,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DualShowcaseEffects' 'EndSale'))))
    }
}
foreach($pair in @(@('PlayerStore','SaveGame'),@('SaveManager','Save'))){
    $m=Method $game $pair[0] $pair[1];$il=$m.Body.GetILProcessor();$first=$m.Body.Instructions[0];$end=$m.Body.Instructions[-1]
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DualShowcaseEffects' 'BeforeSave'))))
    $il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$end))
}

$show=Method $game 'EmporiumEntry' 'get_showcaseElement'
$showField=[Mono.Cecil.FieldDefinition]::new('TestShowcase',[Mono.Cecil.FieldAttributes]'Public,Static',$show.ReturnType)
(GetCType $game 'EmporiumEntry').Fields.Add($showField)
$il=$show.Body.GetILProcessor();$first=$show.Body.Instructions[0]
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$all))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$first))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$showField))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))

# A headless blank MonoBehaviour has no Unity native pointer. Model its live
# scene existence by reference identity at the storage scan boundary only.
foreach($instruction in (Method $mod 'EleanorRomance.DualRewards' 'Exists').Body.Instructions){
    if($instruction.OpCode.Code -eq 'Call' -and $instruction.Operand.DeclaringType.FullName -eq 'UnityEngine.Object' -and $instruction.Operand.Name -eq 'op_Equality'){
        $instruction.Operand=$mod.MainModule.ImportReference([object].GetMethod('ReferenceEquals',[type[]]@([object],[object])))
    }
}

Ret $mod 'EleanorRomance.Items' 'EnsureOrnamentSprite'

# Deterministic random boundary; the production once-per-day persistence remains.
$roll=[Mono.Cecil.FieldDefinition]::new('TestRoll',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.TypeSystem.Single)
(GetCType $mod 'EleanorRomance.DualBusiness').Fields.Add($roll)
foreach($instruction in (Method $mod 'EleanorRomance.DualBusiness' 'TryQueue').Body.Instructions){
    if($instruction.OpCode.Code -eq 'Call' -and $instruction.Operand.DeclaringType.FullName -eq 'UnityEngine.Random' -and $instruction.Operand.Name -eq 'get_value'){
        $instruction.OpCode=[Mono.Cecil.Cil.OpCodes]::Ldsfld;$instruction.Operand=$roll
    }
}
