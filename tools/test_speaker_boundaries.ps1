# Keep native DisplayLine's PLAYER/SEC/client dispatch and native history
# formatting/storage. Omit only rendering around the original history call.
foreach($name in @('DisplayClientText','DisplaySecText','DisplayerPlayerText')){
    $method=Method $game 'DialogUIManager' $name
    $instructions=@($method.Body.Instructions)
    $call=@($instructions | Where-Object {
        $_.OpCode.Code -in @('Call','Callvirt') -and $_.Operand.Name -eq 'UpdateDialogHistory'
    })[0]
    $end=[Array]::IndexOf($instructions,$call);$start=$end-1
    while($start -ge 0 -and $instructions[$start].OpCode.Code -ne 'Ldarg_0'){$start--}
    if($start -lt 0){throw "Native history expression changed in $name"}
    $expression=$instructions[$start..$end]
    if(@($expression | Where-Object {$_.OpCode.FlowControl -in @('Branch','Cond_Branch')}).Count){throw "Unexpected history-expression branch in $name"}
    $il=Body $method
    if($name -eq 'DisplayClientText'){
        $speakerType=GetCType $mod 'EleanorRomance.DialogueSpeakers'
        $previous=[Mono.Cecil.Cil.VariableDefinition]::new((GetCType $game 'Dialogue'))
        $method.Body.Variables.Add($previous);$method.Body.InitLocals=$true
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DialogueSpeakers' 'Begin')))
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Stloc,$previous)
    }
    foreach($instruction in $expression){$il.Append($instruction)}
    if($name -eq 'DisplayClientText'){
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc,$previous)
        $il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DialogueSpeakers' 'End')))
    }
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
# Inject the production history prefix, preserving native formatting/removal.
$history=Method $game 'DialogUIManager' 'UpdateDialogHistory'
$il=$history.Body.GetILProcessor();$first=$history.Body.Instructions[0]
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarga_S,$history.Parameters[0]))
$il.InsertBefore($first,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.DialogueSpeakers' 'RewriteHistory'))))
# Only the Unity label assignment is replaced, retaining its exact argument.
$caption=[Mono.Cecil.FieldDefinition]::new('TestCaption',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.TypeSystem.String)
(GetCType $mod 'EleanorRomance.DialogueSpeakers').Fields.Add($caption)
$captionFault=[Mono.Cecil.FieldDefinition]::new('TestCaptionFault',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.TypeSystem.Boolean)
(GetCType $mod 'EleanorRomance.DialogueSpeakers').Fields.Add($captionFault)
$il=Body (Method $mod 'EleanorRomance.DialogueSpeakers' 'ApplyCaption')
$normal=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$captionFault);$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse,$normal)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'simulated caption failure')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Newobj,$mod.MainModule.ImportReference([InvalidOperationException].GetConstructor([type[]]@([string]))))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Throw)
$il.Append($normal)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$caption);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# The native history enqueue and bounded queue remain intact, without a UI.
$il=Body (Method $game 'RecorderUIManager' 'get_Instance')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$t=GetCType $game 'StoreUIManager'
$f=[Mono.Cecil.FieldDefinition]::new('TestInstance',[Mono.Cecil.FieldAttributes]'Public,Static',$t)
$t.Fields.Add($f)
$il=Body (Method $game 'StoreUIManager' 'get_Instance')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$f);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$leaves=[Mono.Cecil.FieldDefinition]::new('TestSecLeaves',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.TypeSystem.Int32)
$t.Fields.Add($leaves)
$il=Body (Method $game 'StoreUIManager' 'StartLeaveSec')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$leaves);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$leaves)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
