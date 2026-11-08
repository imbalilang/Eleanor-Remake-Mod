# Replace only the morning NPC presentation, not its eligibility, queue or
# chapter logic. The installed native morning priority is represented by an
# already present native client / native urgent queued client in the test.
$t=GetCType $mod 'EleanorRomance.MaidInteractions'
$hook=[Mono.Cecil.FieldDefinition]::new('TestMorningPresentation',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.ImportReference([Action[object]]))
$t.Fields.Add($hook)
$il=Body (Method $mod 'EleanorRomance.MaidInteractions' 'PresentMorning')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$hook)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Action[object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
