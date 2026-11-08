# Dot-sourced before loading the test assemblies. Retain native fee arithmetic,
# appraisal decisions, buying predicates, stock factories and dialogue callbacks.
# Substitute only counter placement, sound, receipt rendering and analytics.
$t=GetCType $mod 'EleanorRomance.MaidBusiness'
$stockHook=[Mono.Cecil.FieldDefinition]::new('TestPutStock',[Mono.Cecil.FieldAttributes]'Public,Static',
    $mod.MainModule.ImportReference([Action[object]]))
$t.Fields.Add($stockHook)
$il=Body (Method $mod 'EleanorRomance.MaidBusiness' 'PutStock')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$stockHook)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$mod.MainModule.ImportReference([Action[object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$fee=Method $game 'AppraisalServiceHelper' 'GiveAppraisalFee'
$cashWrite=@($fee.Body.Instructions | Where-Object {
    $_.OpCode.Code -eq 'Stfld' -and $_.Operand.Name -eq 'playerCash'
})[0]
if(!$cashWrite){throw 'Native appraisal cash assignment changed.'}
$cut=$fee.Body.Instructions.IndexOf($cashWrite)+1
while($fee.Body.Instructions.Count -gt $cut){$fee.Body.Instructions.RemoveAt($cut)}
$fee.Body.GetILProcessor().Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'StallWatchdog' 'Crumb'
Ret $game 'EmporiumAnalytics' 'TrackAppraisalSent'
Ret $game 'AchievementTracker' 'CheckMasterAppraiser'
Ret $game 'GameItemElement' 'Destroy'
$il=Body (Method $game 'EmporiumEntry' 'GetAllNonOwendItemCount')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Apply the production Harmony prefix/postfix at the native acceptance entry.
$value=Method $game 'AppraisalHelper' 'GetCorrectBaseValue'
$value.Body.InitLocals=$true
$valueResult=[Mono.Cecil.Cil.VariableDefinition]::new($game.MainModule.TypeSystem.Int32)
$value.Body.Variables.Add($valueResult)
$valuePatch=GetCType $mod 'EleanorRomance.BusinessAppraisalValue';$valuePatch.IsPublic=$true
$valuePrefix=Method $mod $valuePatch.FullName 'Prefix';$valuePrefix.IsPublic=$true
$il=$value.Body.GetILProcessor();$first=$value.Body.Instructions[0]
$returnValue=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloc,$valueResult)
foreach($i in @(
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloca,$valueResult),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($valuePrefix)),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$returnValue)
)){$il.InsertBefore($first,$i)}
$il.Append($returnValue);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$patch=GetCType $mod 'EleanorRomance.BusinessAppraisalReceipt'
$patch.IsPublic=$true
foreach($m in $patch.Methods){$m.IsPublic=$true}
$accept=Method $game 'AppraisalServiceHelper' 'GetAcceptEstimateDialog'
$accept.Body.InitLocals=$true
$state=[Mono.Cecil.Cil.VariableDefinition]::new($game.MainModule.TypeSystem.Int32)
$result=[Mono.Cecil.Cil.VariableDefinition]::new((GetCType $game 'Dialogue'))
$accept.Body.Variables.Add($state);$accept.Body.Variables.Add($result)
$il=$accept.Body.GetILProcessor();$first=$accept.Body.Instructions[0]
$returnResult=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloc,$result)
foreach($i in @(
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloca,$result),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloca,$state),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod $patch.FullName 'Prefix'))),
    [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Brfalse,$returnResult)
)){$il.InsertBefore($first,$i)}
foreach($ret in @($accept.Body.Instructions | Where-Object {$_.OpCode.Code -eq 'Ret'})){
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldloc,$state))
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod $patch.FullName 'Postfix'))))
}
$il.Append($returnResult);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Native wrong-estimate walkout must enter the same chapter wrap-up.
$patience=Method $game 'AppraisalServiceHelper' 'GetPatienceRunOutDialog'
$exit=GetCType $mod 'EleanorRomance.BusinessAppraisalExit';$exit.IsPublic=$true
$post=Method $mod $exit.FullName 'Postfix';$post.IsPublic=$true
$il=$patience.Body.GetILProcessor()
foreach($ret in @($patience.Body.Instructions | Where-Object {$_.OpCode.Code -eq 'Ret'})){
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Dup))
    $il.InsertBefore($ret,[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference($post)))
}
