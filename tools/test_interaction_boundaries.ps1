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

# Closing presentation is the only mocked gala UI boundary. Eligibility,
# queue retirement, saved phases, native-close calls and day transition run.
$t=GetCType $mod 'EleanorRomance.GalaClosing'
# Inspect the production methods before replacing their Unity scheduling seams.
foreach($name in @('SchedulePresentation','ScheduleSleep')){
    $production=Method $mod 'EleanorRomance.GalaClosing' $name
    $calls=@($production.Body.Instructions | Where-Object {$_.OpCode.Code -in @('Call','Callvirt')})
    $owned=@($calls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'ModHelper' -and $_.Operand.Name -eq 'StartCoroutine'})
    $foreign=@($calls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'UnityEngine.MonoBehaviour' -and $_.Operand.Name -in @('StartCoroutine','Invoke','InvokeRepeating')})
    $owner=@($production.Body.Instructions | Where-Object {$_.OpCode.Code -eq 'Ldstr' -and $_.Operand -eq 'eleanor_romance'})
    if($owned.Count -ne 1 -or $foreign.Count -ne 0 -or $owner.Count -ne 1){throw "Unmanaged or incorrectly owned gala scheduler in $name"}
}
'PASS: original gala schedulers use the loader-managed API and correct owner before test seams.'
$presentation=Method $mod 'EleanorRomance.GalaClosing' 'Present'
$calls=@($presentation.Body.Instructions | Where-Object {$_.OpCode.Code -in @('Call','Callvirt')})
$walk=@($calls | Where-Object {$_.Operand.DeclaringType.FullName -eq 'StoreUIManager' -and $_.Operand.Name -eq 'QueueClientArriveAnimation'})
$instant=@($calls | Where-Object {$_.Operand.Name -in @('SetImmediatlyArrive','StartDialogue','StartMainDialogue')})
if($walk.Count -ne 1 -or $instant.Count -ne 0){throw 'Gala presentation bypasses the native walk-in/arrival dialogue path'}
$nativeWalk=Method $game 'StoreUIManager' 'QueueClientArriveAnimation'
$nativeArrival=Method $game 'StoreClientMono' 'OnArrived'
if(!@($nativeWalk.Body.Instructions | Where-Object {$_.OpCode.Code -in @('Call','Callvirt') -and $_.Operand.Name -eq 'StartApprochingStore'}).Count -or
   !@($nativeArrival.Body.Instructions | Where-Object {$_.OpCode.Code -in @('Call','Callvirt') -and $_.Operand.Name -eq 'OnNextClientArrived'}).Count){throw 'Installed native walk/arrival path differs from the gala integration'}
'PASS: production gala uses native walk-in animation and arrival callback instead of snapping or early dialogue.'
$hook=[Mono.Cecil.FieldDefinition]::new('TestPresentation',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.ImportReference([Func[object,bool]]))
$t.Fields.Add($hook)
$il=Body (Method $mod 'EleanorRomance.GalaClosing' 'Present')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$hook);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Func[object,bool]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Keep CloseNative and SleepNative production methods. A scheduler seam lets
# the test advance the blackout only after checking acceptance and duplicate clicks.
$schedule=[Mono.Cecil.FieldDefinition]::new('TestScheduleSleep',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.ImportReference([Action]))
$t.Fields.Add($schedule)
$il=Body (Method $mod 'EleanorRomance.GalaClosing' 'ScheduleSleep')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$schedule)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Action].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Departure animation remains native; the test resumes its completion explicitly.
$arrival=[Mono.Cecil.FieldDefinition]::new('TestSchedulePresentation',[Mono.Cecil.FieldAttributes]'Public,Static',$mod.MainModule.ImportReference([Action[object]]))
$t.Fields.Add($arrival)
$il=Body (Method $mod 'EleanorRomance.GalaClosing' 'SchedulePresentation')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$arrival);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Action[object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)

foreach($typeName in @('StoreShutterButton','EndOfDayUIManager')){
    $type=GetCType $game $typeName
    $field=[Mono.Cecil.FieldDefinition]::new('TestInstance',[Mono.Cecil.FieldAttributes]'Public,Static',$type)
    $type.Fields.Add($field)
    $il=Body (Method $game $typeName 'get_Instance')
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$field);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
foreach($pair in @(@('StoreUIManager','HideClient'),@('StoreUIManager','EnableBackingBlock'),@('DialogUIManager','InterruptDialog'),@('DialogUIManager','HideChoiceBubble'),@('EndOfDayUIManager','InitUI'))){Ret $game $pair[0] $pair[1]}
# The native settlement is intentionally not copied into the mod. These UI
# boundaries count calls; nightly expense hooks stay owned by the game.
$shutter=GetCType $game 'StoreShutterButton'
$counter=[Mono.Cecil.FieldDefinition]::new('TestCloses',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.TypeSystem.Int32)
$shutter.Fields.Add($counter)
$il=Body (Method $game 'StoreShutterButton' 'EndAndClose')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$counter);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$counter)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,(Method $game 'PlayerStore' 'get_Instance'))
$stateField=(GetCType $game 'PlayerStore').Fields|Where-Object Name -eq 'storeState'
$closed=(GetCType $game 'PlayerStore/StoreState').Fields|Where-Object Name -eq 'CLOSED'
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4,[int]$closed.Constant)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,$stateField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$end=GetCType $game 'EndOfDayUIManager'
$sleeps=[Mono.Cecil.FieldDefinition]::new('TestSleeps',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.TypeSystem.Int32)
$end.Fields.Add($sleeps)
$il=Body (Method $game 'EndOfDayUIManager' 'OnEndDayButtonClick')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$sleeps);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$sleeps)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,(Method $game 'StoreStation' 'get_Instance'))
$day=(GetCType $game 'StoreStation').Fields|Where-Object Name -eq 'dayCounter'
$il.Emit([Mono.Cecil.Cil.OpCodes]::Dup);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld,$day)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1);$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,$day)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,(Method $game 'PlayerStore' 'get_Instance'))
$morning=(GetCType $game 'PlayerStore/StoreState').Fields|Where-Object Name -eq 'MORNING'
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4,[int]$morning.Constant);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,$stateField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.GalaClosing' 'OnNewDay')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
