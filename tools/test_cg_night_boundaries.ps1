# Offline only. Keep all route logic, the actual choice delegates and the native sleep ordering.
# Replace rendered pages, Unity timers, and native scene transitions with controllable boundaries.
function AuditField($assembly,$ownerType,$name,$ref) {
    $field=[Mono.Cecil.FieldDefinition]::new($name,[Mono.Cecil.FieldAttributes]'Public,Static',$ref)
    $ownerType.Fields.Add($field)
    return $field
}
function AuditGetter($asm,$type,$name,$field) {
    $il=Body (Method $asm $type $name)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$field)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
foreach($type in @('StoreShutterButton','EndOfDayUIManager','StoreUIManager')) {
    $ownerType=GetCType $game $type
    $field=AuditField $game $ownerType 'NightInstance' $ownerType
    AuditGetter $game $type 'get_Instance' $field
}
$uiType=GetCType $mod 'EleanorRomance.EndingPageUI'
$pageFields=@{}
foreach($pair in @(@('NightPage','Boolean'),@('FailPage','Boolean'),@('Next','Action'),@('Text','String'))) {
    $pageFields[$pair[0]]=AuditField $mod $uiType $pair[0] ($mod.MainModule.ImportReference([type]("System."+$pair[1])))
}
AuditGetter $mod $uiType.FullName 'get_PageOpen' $pageFields.NightPage
$il=Body (Method $mod $uiType.FullName 'HideRoutePage')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$pageFields.NightPage);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $mod $uiType.FullName 'ShowRoutePage')
$ok=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$pageFields.FailPage);$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse,$ok)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret);$il.Append($ok)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld,((GetCType $mod 'EleanorRomance.EndingPages/Page').Fields | Where-Object Name -eq 'Text'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$pageFields.Text)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_2);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$pageFields.Next)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1);$il.Emit([Mono.Cecil.Cil.OpCodes]::Dup);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$pageFields.NightPage);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Preserve the enabled/story-active gates; the fixture locale is zh-CN.
$m=Method $mod 'EleanorRomance.ChapterVisits' 'get_Active'
$locale=$m.Body.Instructions | Where-Object { $_.Operand.Name -eq 'get_SelectedLocale' } | Select-Object -First 1
if(!$locale){throw 'Chapter locale gate changed'}
$locale.OpCode=[Mono.Cecil.Cil.OpCodes]::Ldc_I4_1;$locale.Operand=$null
$m.Body.GetILProcessor().InsertAfter($locale,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
$il=Body (Method $mod 'EleanorRomance.PortraitCompat' 'GetLocalSprite');$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Verify sleep checkpoints against the unmodified native callback graph before substituting the scene.
$native=GetCType $game 'EndOfDayUIManager'
$callbacks=@($native.NestedTypes.Methods | Where-Object Name -like '<OnEndDayButtonClick>*')
$black=@($callbacks | Where-Object Name -like '*b__0' | ForEach-Object {$_.Body.Instructions.Operand.Name}) -join ','
$wake=@($callbacks | Where-Object Name -like '*b__1' | ForEach-Object {$_.Body.Instructions.Operand.Name}) -join ','
if($black -notmatch 'EndNight,.*OnDayEnd,.*BeginDay,.*ExecuteGameOver,.*SaveGame' -or $wake -notmatch 'OpenUI'){throw 'Native autosave sequence changed'}
foreach($entry in @(@('PlayerStore','TryCallNextClient','NightBell'),@('StoreShutterButton','EndAndClose','NightClose'),@('EndOfDayUIManager','OnEndDayButtonClick','NightSleep'))) {
    $ownerType=GetCType $game $entry[0]
    $field=AuditField $game $ownerType $entry[2] ($game.MainModule.ImportReference([Action]))
    $il=Body (Method $game $entry[0] $entry[1]);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$field)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,($game.MainModule.ImportReference([Action].GetMethod('Invoke'))));$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
$m=Method $game 'MetaHelper' 'WaitAndDo' 2
$field=AuditField $game (GetCType $game 'MetaHelper') 'NightTimer' ($game.MainModule.ImportReference([Action]))
$il=Body $m;$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$field);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $game 'ModHelper' 'StartCoroutine' 2);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'StoreUIManager' 'EnableBackingBlock'
Ret $game 'EndOfDayUIManager' 'InitUI'
# Exercise the installed game's two original black-screen callbacks, including its game-over branches.
# Blank only UI instructions that need live GameObjects, keep native state writes and checkpoint calls.
function ClearUiRange($first,$end) {
    for($i=$first;$i -ne $end;$i=$i.Next){if(!$i){throw 'Native UI boundary moved'};$i.OpCode=[Mono.Cecil.Cil.OpCodes]::Nop;$i.Operand=$null}
}
$before=$callbacks | Where-Object Name -like '*b__0'
$after=$callbacks | Where-Object Name -like '*b__1'
$ops=$before.Body.Instructions
$force=$ops | Where-Object {$_.Operand.Name -eq 'ForceImmediateClose'}
ClearUiRange $ops[0] $force.Next
$closeAll=$ops | Where-Object {$_.Operand.Name -eq 'CloseAllUI'}
$closeUI=$ops | Where-Object {$_.Operand.Name -eq 'CloseUI'}
ClearUiRange $closeAll.Previous $closeUI.Next
$saveText=$ops | Where-Object {$_.Operand.Name -eq 'savingTMP'}
$clearRecorder=$ops | Where-Object {$_.Operand.Name -eq 'ClearAllElement'}
ClearUiRange $saveText.Previous $clearRecorder.Next
$tutorial=$ops | Where-Object {$_.Operand.Name -eq 'OnDayBegin'}
ClearUiRange $tutorial.Previous $tutorial.Next
$field=AuditField $game $native 'NightReactorFailure' $game.MainModule.TypeSystem.Boolean
AuditGetter $game 'ReactorHelper' 'ConsumeExplosion' $field
$over=AuditField $game $native 'NightGameOver' $game.MainModule.TypeSystem.Boolean
$ops=$after.Body.Instructions
$show=$ops | Where-Object {$_.Operand.Name -eq 'get_activeSelf'}
ClearUiRange $ops[0] $show
$show.OpCode=[Mono.Cecil.Cil.OpCodes]::Ldsfld;$show.Operand=$over
$paper=$ops | Where-Object {$_.Operand.Name -eq 'RestorePaperUIClosedForSleep'}
ClearUiRange $show.Next.Next $paper.Next
$open=$ops | Where-Object {$_.Operand.Name -eq 'OpenUI'}
ClearUiRange $open.Next $ops[-1]
foreach($p in @(@('PlayerStore','EndNight'),@('StoreEventManager','OnDayEnd'),@('PlayerStore','BeginDay'),@('PlayerStore','SaveGame'),@('PlayerStore','ExecuteGameOver'),@('StartOfDayUIManager','OpenUI'))) {
    $field=AuditField $game (GetCType $game $p[0]) ('Night'+$p[1]) ($game.MainModule.ImportReference([Action]))
    $il=Body (Method $game $p[0] $p[1]);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$field)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,($game.MainModule.ImportReference([Action].GetMethod('Invoke'))));$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
$morningType=GetCType $game 'StartOfDayUIManager'
AuditGetter $game $morningType.FullName 'get_Instance' (AuditField $game $morningType 'NightInstance' $morningType)
# Keep real DialogUIManager.ContinueConversation() (including its !isSecMonologuePlaying &&
# currentClientInstance == null abort gate) while stubbing only Unity UI bubble rendering.
foreach($mName in @('DisplayClientText','DisplayerPlayerText','DisplaySecText','HideBubbles','HideChoiceBubble')) {
    Ret $game 'DialogUIManager' $mName
}
# Apply ChapterWindowDialogue* Harmony hooks onto DialogUIManager so tests execute the real patched loop.
$contMethod = Method $game 'DialogUIManager' 'ContinueConversation'
$contPrefixType = GetCType $mod 'EleanorRomance.ChapterWindowDialogueContinuePatch'
$contPrefixType.IsNotPublic = $false; $contPrefixType.IsPublic = $true
$contPrefix = Method $mod $contPrefixType.FullName 'Prefix'
$contPrefix.IsPrivate = $false; $contPrefix.IsPublic = $true
$il = $contMethod.Body.GetILProcessor(); $firstOp = $contMethod.Body.Instructions[0]
$il.InsertBefore($firstOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($firstOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,(Import-Reference $game.MainModule $contPrefix)))
$il.InsertBefore($firstOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brtrue,$firstOp))
$il.InsertBefore($firstOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))

$remMethod = Method $game 'DialogUIManager' 'RemoveCurrentDialog'
$remPostType = GetCType $mod 'EleanorRomance.ChapterWindowDialogueRemovePatch'
$remPostType.IsNotPublic = $false; $remPostType.IsPublic = $true
$remPost = Method $mod $remPostType.FullName 'Postfix'
$remPost.IsPrivate = $false; $remPost.IsPublic = $true
$il = $remMethod.Body.GetILProcessor(); $lastOp = $remMethod.Body.Instructions[$remMethod.Body.Instructions.Count - 1]
$il.InsertBefore($lastOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($lastOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,(Import-Reference $game.MainModule $remPost)))

$endMethod = Method $game 'DialogUIManager' 'EndConversation'
$endPreType = GetCType $mod 'EleanorRomance.ChapterWindowDialogueEndPatch'
$endPreType.IsNotPublic = $false; $endPreType.IsPublic = $true
$endPre = Method $mod $endPreType.FullName 'Prefix'
$endPre.IsPrivate = $false; $endPre.IsPublic = $true
$il = $endMethod.Body.GetILProcessor(); $firstEndOp = $endMethod.Body.Instructions[0]
$il.InsertBefore($firstEndOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
$il.InsertBefore($firstEndOp,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,(Import-Reference $game.MainModule $endPre)))
