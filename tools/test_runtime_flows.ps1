#requires -Version 7.0
# Offline checks against an installed 0.46D assembly. Game and mod DLLs are
# rewritten only in memory; no game files or saves are modified.
param(
    [Parameter(Mandatory=$true)][string]$Managed,
    [string]$ModDll = (Join-Path $PSScriptRoot '..\bin\Release\EleanorRomance.dll'),
    [switch]$Maid,
    [switch]$Dual
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Managed 'Unity.Burst.Cecil.dll')
$resolve = [Func[System.Runtime.Loader.AssemblyLoadContext,System.Reflection.AssemblyName,System.Reflection.Assembly]]({
    param($context,$name)
    if ($name.Name.EndsWith('.resources')) { return $null }
    $candidate = [IO.Path]::Combine($Managed, $name.Name + '.dll')
    if ([IO.File]::Exists($candidate)) { return $context.LoadFromAssemblyPath($candidate) }
    return $null
}.GetNewClosure())
[System.Runtime.Loader.AssemblyLoadContext]::Default.add_Resolving($resolve)
$cecilResolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$cecilResolver.AddSearchDirectory($Managed)
$read = [Mono.Cecil.ReaderParameters]::new()
$read.AssemblyResolver = $cecilResolver
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $Managed 'Assembly-CSharp.dll'), $read)
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ModDll, $read)
function GetCType($assembly, $name) { $assembly.MainModule.GetTypes() | Where-Object FullName -eq $name }
function Method($assembly, $type, $name, $count = -1) {
    $methods = (GetCType $assembly $type).Methods | Where-Object Name -eq $name
    if ($count -ge 0) { $methods = $methods | Where-Object { $_.Parameters.Count -eq $count } }
    return $methods | Select-Object -First 1
}
function Body($method) {
    $method.Body = [Mono.Cecil.Cil.MethodBody]::new($method)
    return $method.Body.GetILProcessor()
}
function Ret($assembly, $type, $name, $count = -1) {
    $il = Body (Method $assembly $type $name $count)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
# Replace only rendering, localization, stock lookup and departure UI boundaries.
# Native business-done detection, post-deal grafting, completion hooks, shape
# transforms and collision tests remain unchanged and run below.
foreach ($assembly in @($game,$mod)) {
    foreach ($type in $assembly.MainModule.GetTypes()) {
        foreach ($method in $type.Methods) {
            if (!$method.HasBody) { continue }
            foreach ($instruction in @($method.Body.Instructions)) {
                if ($instruction.OpCode.Code -in @('Call','Callvirt') -and
                    $instruction.Operand.DeclaringType.FullName -eq 'UnityEngine.Debug' -and
                    $instruction.Operand.ReturnType.FullName -eq 'System.Void') {
                    $n = $instruction.Operand.Parameters.Count
                    for ($i = 1; $i -lt $n; $i++) {
                        $method.Body.GetILProcessor().InsertBefore($instruction,
                            [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Pop))
                    }
                    $instruction.OpCode = [Mono.Cecil.Cil.OpCodes]::Pop
                    $instruction.Operand = $null
                }
            }
        }
    }
}
$dialog = GetCType $game 'Dialogue'
$setText = $dialog.Methods | Where-Object { $_.Name -eq 'SetText' -and $_.Parameters[1].ParameterType.FullName -eq 'System.String' }
$il = Body $setText
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,($dialog.Fields | Where-Object Name -eq 'title'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_2)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,($dialog.Fields | Where-Object Name -eq 'plainText'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
foreach ($typeName in @('DialogUIManager','PlayerStore','StoreStation')) {
    $type = GetCType $game $typeName
    $field = [Mono.Cecil.FieldDefinition]::new('TestInstance',
        [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static, $type)
    $type.Fields.Add($field)
    $il = Body (Method $game $typeName 'get_Instance')
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$field)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
if($Maid){
    # Model the actual menu lifecycle: there is no store yet. Any access to
    # the native creating getter is a bootstrap error, including swallowed ones.
    $t=GetCType $game 'PlayerStore'
    $reads=[Mono.Cecil.FieldDefinition]::new('TestInstanceReads',[Mono.Cecil.FieldAttributes]'Public,Static',$game.MainModule.TypeSystem.Int32)
    $t.Fields.Add($reads)
    $instance=$t.Fields | Where-Object Name -eq 'TestInstance'
    $il=Body (Method $game 'PlayerStore' 'get_Instance')
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$reads);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$reads)
    $ready=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldsfld,$instance)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$instance);$il.Emit([Mono.Cecil.Cil.OpCodes]::Brtrue,$ready)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'PlayerStore creating getter accessed before a run exists')
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Newobj,$game.MainModule.ImportReference([NullReferenceException].GetConstructor([type[]]@([string]))))
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Throw)
    $il.Append($ready);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
$manager = GetCType $game 'DialogUIManager'
$il = Body (Method $game 'DialogUIManager' 'StartDialogue')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,($manager.Fields | Where-Object Name -eq 'currentDialog'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld,($dialog.Fields | Where-Object Name -eq 'startAction'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Dup)
$end = [Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Pop)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse_S,$end)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([Action].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il.Append($end)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$storeType = GetCType $game 'PlayerStore'
$countField = [Mono.Cecil.FieldDefinition]::new('TestDismissCount',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$game.MainModule.TypeSystem.Int32)
$storeType.Fields.Add($countField)
$il = Body (Method $game 'PlayerStore' 'DismissCurrentClient')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$countField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,$countField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$instanceType = GetCType $game 'StoreClientInstance'
$stockField = [Mono.Cecil.FieldDefinition]::new('TestHasStock',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$game.MainModule.TypeSystem.Boolean)
$instanceType.Fields.Add($stockField)
$il = Body (Method $game 'StoreClientInstance' 'IsCurrentClientStillHaveThingToSell')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$stockField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il = Body (Method $game 'LocHelper' 'Get' 3)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il = Body (Method $game 'LocHelper' 'GetLocalizedOrDefault' 4)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Test localization uses stable keys, preserving the PLAYER sentinel.
$il = Body (Method $mod 'EleanorRomance.L' 'Get')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
foreach ($method in @('Log','LogError')) { Ret $mod 'EleanorRomance.ModEntry' $method }
$il=Body (Method $mod 'EleanorRomance.ModEntry' 'LogError')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$mod.MainModule.ImportReference([Console].GetMethod('WriteLine',[type[]]@([string]))))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'GameGridInventory' 'Validate'
Ret $game 'GameItemElement' 'Validate'
$il = Body (Method $game 'GameItem' 'SetSpriteAndShapeFromMod')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Fixture for the real bracelet factory.
$directory = GetCType $game 'ItemDirectory'
$itemField = [Mono.Cecil.FieldDefinition]::new('TestItem',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,(GetCType $game 'GameItem'))
$directory.Fields.Add($itemField)
$il = Body (Method $game 'ItemDirectory' 'CreateEmptyItem')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$itemField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$m=Method $game 'StoreClient' 'OnDealAccepted'
$m.IsPrivate=$false
$m.IsPublic=$true
$m.Name='TestOnDealAccepted'
foreach ($pair in @(@('StoreClientInstance','storeClient'),@('DialogUIManager','currentDialog'),@('StoreClient','clientBudget'))) {
    $f=(GetCType $game $pair[0]).Fields | Where-Object Name -eq $pair[1]
    $f.IsPrivate=$false
    $f.IsPublic=$true
}
# In-memory run data and chapter-construction boundaries (no save file writes).
$helper=GetCType $game 'ModHelper'
$dataMethod=Method $game 'ModHelper' 'ModDataStore'
$dataField=[Mono.Cecil.FieldDefinition]::new('TestData',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$dataMethod.ReturnType)
$helper.Fields.Add($dataField)
$il=Body $dataMethod
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$dataField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
Ret $game 'StoreClient' 'ApplyBudgetModifier'
Ret $game 'StoreClient' 'StampOwnedCounterItems'
foreach($m in (GetCType $game 'StoreClientFactionSetup').Methods | Where-Object Name -match '^Init') {
    $il=Body $m; $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
foreach($n in @('Resolve','ResolveCharacter')) {
    $il=Body (Method $mod 'EleanorRomance.PortraitCompat' $n)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
if(!$Maid){Ret $mod 'EleanorRomance.StoryEngine' 'GiveSelfRefillingPerfume'}
Ret $mod 'EleanorRomance.Items' 'EnsureBraceletSprite'
Ret $mod 'EleanorRomance.Items' 'EnsureLocketSprite'
$il=Body (Method $game 'SpriteDict' 'GetRandomSecSpriteName')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldstr,'maleSec1');$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Native choice icons and reward items are created without Unity rendering.
$master=GetCType $game 'DirectoryMaster'
$factoryField=[Mono.Cecil.FieldDefinition]::new('TestFactory',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,
    $game.MainModule.ImportReference([Func[string,bool,object]]))
$master.Fields.Add($factoryField)
$il=Body (Method $game 'DirectoryMaster' 'Item' 2)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$factoryField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$game.MainModule.ImportReference([Func[string,bool,object]].GetMethod('Invoke')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Castclass,(GetCType $game 'GameItem'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Keep the real native scheduler, and apply the same queue filter as Harmony.
$queueMethod=Method $game 'StoreClientManager' 'AddClient'
$il=Body $queueMethod
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.VanillaEleanorBridge' 'FilterScheduledClient')))
$queueReturn=[Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse,$queueReturn)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$queueField=(GetCType $game 'StoreClientManager').Fields|Where-Object Name -eq 'clientStack'
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld,$queueField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$listAdd=[Mono.Cecil.MethodReference]::new('Add',$game.MainModule.TypeSystem.Void,$queueField.FieldType)
$listAdd.HasThis=$true
$listAdd.Parameters.Add([Mono.Cecil.ParameterDefinition]::new($queueField.FieldType.ElementType.GenericParameters[0]))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,$listAdd)
$il.Append($queueReturn)
# Calendar anchoring and reputation values are run fixtures, not eligibility rules.
$il=Body (Method $game 'StorylineSchedule' 'StartDay')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4,23);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$repType=GetCType $game 'StoreReputation'
$repField=[Mono.Cecil.FieldDefinition]::new('TestSecRep',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$game.MainModule.TypeSystem.Int32)
$repType.Fields.Add($repField)
$il=Body (Method $game 'StoreReputation' 'GetSecReputation')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$repField);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Cash changes keep the actual arithmetic, omitting UI refresh and animation.
$il=Body (Method $game 'PlayerStore' 'ModCash' 2)
$cashField=$storeType.Fields|Where-Object Name -eq 'playerCash'
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0);$il.Emit([Mono.Cecil.Cil.OpCodes]::Dup)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld,$cashField);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add);$il.Emit([Mono.Cecil.Cil.OpCodes]::Stfld,$cashField)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
$il=Body (Method $game 'DialogUIManager' 'CurrentClientLeave')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,(Method $game 'PlayerStore' 'get_Instance'))
foreach($i in 1..3){$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)}
$il.Emit([Mono.Cecil.Cil.OpCodes]::Callvirt,(Method $game 'PlayerStore' 'DismissCurrentClient'))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# Native gift construction is real; the placement boundary supplies space/full.
$il=Body (Method $game 'PlayerStore' 'AddDirectSellingItemToTableMiddle')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Call,$game.MainModule.ImportReference((Method $mod 'EleanorRomance.VanillaEleanorBridge' 'CaptureReward')))
$il.Emit([Mono.Cecil.Cil.OpCodes]::Pop);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
foreach($getter in @('get_frontInvinvElement','get_invElement')){
    $il=Body (Method $game 'EmporiumEntry' $getter)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldnull);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
$placementType=GetCType $game 'GraphUtils'
foreach($pair in @(@('TestRemaining',$game.MainModule.TypeSystem.Int32),@('TestLastGift',(GetCType $game 'GameItem')))){
    $placementType.Fields.Add([Mono.Cecil.FieldDefinition]::new($pair[0],
        [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$pair[1]))
}
foreach($name in @('TryAcceptAllMid','TryAcceptAll')){
    $il=Body (Method $game 'GraphUtils' $name 3)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Stsfld,($placementType.Fields|Where-Object Name -eq 'TestLastGift'))
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,($placementType.Fields|Where-Object Name -eq 'TestRemaining'))
    $il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
}
$il=Body (Method $game 'GraphUtils' 'DestroyIfUnplaced')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
# The placement fixture only needs a non-null Emporium singleton.
$entry=GetCType $game 'EmporiumEntry'
$entryField=[Mono.Cecil.FieldDefinition]::new('TestInstance',
    [Mono.Cecil.FieldAttributes]::Public -bor [Mono.Cecil.FieldAttributes]::Static,$entry)
$entry.Fields.Add($entryField)
$il=Body (Method $game 'EmporiumEntry' 'get_Instance')
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldsfld,$entryField);$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)
if($Maid){
    . (Join-Path $PSScriptRoot 'test_maid_boundaries.ps1')
    . (Join-Path $PSScriptRoot 'test_business_boundaries.ps1')
    . (Join-Path $PSScriptRoot 'test_scav_boundaries.ps1')
    . (Join-Path $PSScriptRoot 'test_interaction_boundaries.ps1')
}
if($Dual){. (Join-Path $PSScriptRoot 'test_dual_boundaries.ps1')}
. (Join-Path $PSScriptRoot 'test_speaker_boundaries.ps1')
foreach ($assembly in @($game,$mod)) {
    $stream = [IO.MemoryStream]::new()
    $assembly.Write($stream)
    $stream.Position = 0
    $null = [System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromStream($stream)
    $stream.Dispose()
}
function Assert($condition,$message) {
    if (!$condition) { throw $message }
}
function Blank($type) { [System.Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject($type) }
if($Maid){
    [PlayerStore]::TestInstance=$null
    $reproduced=$false
    try{[EleanorRomance.MaidProjects]::Register()}catch{$reproduced=$true}
    Assert ($reproduced -and [PlayerStore]::TestInstanceReads -gt 0) 'Bootstrap fixture did not reproduce the unsafe native registration path.'
    [EleanorRomance.MaidProjects]::Unregister()
    [PlayerStore]::TestInstanceReads=0
    $entry=[EleanorRomance.ModEntry]::new()
    $entry.OnEnable();$entry.OnEnable()
    Assert (@([AfterhourEvents]::GetAll() | Where-Object id -eq 'er_eleanor_scav_assistance').Count -eq 1) 'Repeated menu enable duplicated the landfill event.'
    Assert ([PlayerStore]::TestInstanceReads -eq 0) 'Enabling in the mod menu touched/created PlayerStore.'
    $initHook=([ModHook]).GetField('OnGameLoadedInit',[Reflection.BindingFlags]'Public,NonPublic,Static').GetValue($null)
    $registration=@($initHook.GetInvocationList() | Where-Object {$_.Method.DeclaringType.FullName -eq 'EleanorRomance.MaidProjects' -and $_.Method.Name -eq 'Register'})
    Assert ($registration.Count -eq 1) 'Enable repeated or omitted the deferred registration hook.'
    $entry.OnDisable()
    Assert ($null -eq [AfterhourEvents]::GetDefinition('er_eleanor_scav_assistance')) 'Disable retained the landfill event definition.'
    Assert ([PlayerStore]::TestInstanceReads -eq 0) 'Disabling in the mod menu touched/created PlayerStore.'
    $initHook=([ModHook]).GetField('OnGameLoadedInit',[Reflection.BindingFlags]'Public,NonPublic,Static').GetValue($null)
    Assert ($null -eq $initHook -or @($initHook.GetInvocationList() | Where-Object {$_.Method.DeclaringType.FullName -eq 'EleanorRomance.MaidProjects'}).Count -eq 0) 'Disable retained the registration hook.'
    'PASS: menu enable/disable without PlayerStore, original failure reproduced, and deferred hook deduplication.'
}
$store = Blank ([PlayerStore])
[PlayerStore]::TestInstance = $store
$ui = Blank ([DialogUIManager])
[DialogUIManager]::TestInstance = $ui
$script:builds=0
$script:commits=0
function New-Trade($intent) {
    $script:builds=0; $script:commits=0
    [PlayerStore]::TestDismissCount = 0
    $client = [StoreClient]::new()
    $client.clientFaction = 'FACTION_UPPER_LEVEL'
    $client.clientIntent = $intent
    $client.acceptDealDialogue = [Dialogue]::new().SetText('Eleanor','Agreed.')
    $instance = Blank ([StoreClientInstance])
    $instance.storeClient = $client
    $store.currentClientInstance = $instance
    [EleanorRomance.StoryEngine]::InterceptDismiss($client,
        [Func[Dialogue]]{
            $script:builds++
            return [Dialogue]::new().SetText('Eleanor','Wrap-up.').SetNextDialogue(
                [Dialogue]::new().SetText('PLAYER','Goodbye.'))
        },
        [Action]{ $script:commits++ })
    $client.CompleteClientCreation($true)
    return $client
}
function Finish-Dialogue($head) {
    $node = $head
    while ($node) {
        if ($node.startAction) { $node.startAction.Invoke() }
        $node.ExecuteEndAction()
        $node = $node.nextDialogue
    }
}
# Native accept callback adds a continuation while stock remains, then the same
# callback selects the chapter's dynamic allDone chain after the last purchase.
$c = New-Trade ([StoreClient+ClientIntent]::SELL)
[StoreClientInstance]::TestHasStock=$true
$null=$ui.StartDialogue($c.acceptDealDialogue,$false)
Assert ($script:builds -eq 0 -and !$c.isBusinessComplete -and !$c.allDoneDialogue) 'Partial trade ended the visit.'
Assert ($c.acceptDealDialogue.nextDialogue -ne $null) 'Native continuation is missing.'
[StoreClientInstance]::TestHasStock=$false
$c.TestOnDealAccepted($null)
Assert ($script:builds -eq 1 -and $c.isBusinessComplete -and !$c.dismissable) 'Last item did not start wrap-up.'
Finish-Dialogue $ui.currentDialog
Assert ($script:commits -eq 1 -and [PlayerStore]::TestDismissCount -eq 1) 'Last-item wrap-up did not commit/dismiss exactly once.'
# Wholesale path starts accept again with its previous graft still attached.
$c = New-Trade ([StoreClient+ClientIntent]::SELL)
[StoreClientInstance]::TestHasStock=$true
$null=$ui.StartDialogue($c.acceptDealDialogue,$false)
[StoreClientInstance]::TestHasStock=$false
$c.CompleteBusiness()
$null=$ui.StartDialogue($c.acceptDealDialogue,$false)
Assert ($c.acceptDealDialogue.nextDialogue -eq $c.allDoneDialogue) 'Wholesale did not replace the earlier continuation.'
Finish-Dialogue $c.acceptDealDialogue
Assert ($script:builds -eq 1 -and $script:commits -eq 1) 'Wholesale completion duplicated or skipped.'
# First deal can itself be the final deal.
$c = New-Trade ([StoreClient+ClientIntent]::SELL)
[StoreClientInstance]::TestHasStock=$false
$null=$ui.StartDialogue($c.acceptDealDialogue,$false)
Finish-Dialogue $c.acceptDealDialogue
Assert ($script:builds -eq 1 -and $script:commits -eq 1) 'First/final deal did not complete.'
# Native buyer budget detection: keep trading at 1, finish at 0.
$c = New-Trade ([StoreClient+ClientIntent]::BUY)
$c.useClientBudget=$true
$c.clientBudget=1
$c.TestOnDealAccepted($null)
Assert ($script:builds -eq 0) 'Buyer with money was dismissed.'
$c.clientBudget=0
$c.TestOnDealAccepted($null)
Finish-Dialogue $ui.currentDialog
Assert ($script:commits -eq 1) 'Exhausted buyer budget did not complete.'
# Manual next-customer path uses the identical wrap-up with no transaction.
$c = New-Trade ([StoreClient+ClientIntent]::SELL)
$store.TryPlayerDismissCurrentClient($true)
Finish-Dialogue $ui.currentDialog
Assert ($script:builds -eq 1 -and $script:commits -eq 1) 'Manual completion failed.'
$tail=[EleanorRomance.StoryEngine]::Tail($ui.currentDialog)
$tail.ExecuteEndAction()
Assert ($script:commits -eq 1) 'Completion was committed twice.'
'PASS: native partial/last-item/wholesale/first-final/budget/manual trade paths.'

function New-Item($mask,$width=4,$x=0,$y=0,$flip=$false,$rotation=0) {
    $item = Blank ([GameItemElement])
    $item.identifier='eleanor_romance.upper_bracelet'
    $item.state=[TagSystem]::new()
    $item.modifiedState=[TagSystem]::new()
    $item.itemFeatures=[System.Collections.Generic.List[ItemFeature]]::new()
    $item.itemTypes=[System.Collections.Generic.List[string]]::new()
    $item.unitCount=1
    $item.shape=[GridShapeBuilder]::new().SetData($mask,$width).Build()
    $item.modifiedShape=[GridShapeBuilder]::new($item.shape).SetTransform($x,$y,$flip,$rotation).Build()
    $item.mayInventoryAddItemFunc=[Func[GameItem,GameInventory,bool]]{param($i,$inv) $true}
    return $item
}
[DirectoryMaster]::TestFactory=[Func[string,bool,object]]{
    param($identifier,$owned)
    $item=New-Item '1' 1
    $item.identifier=$identifier
    $item.unitValue=100
    if($identifier -eq 'portable_water_purifier') { [DurabilityHelper]::InitItemWithDurability($item,250) }
    if($identifier -eq 'pheromone_perfume') { [UseCountHelper]::InitUseCountItem($item,5,$false,$false,$false,0,0) }
    if($owned){ [GeneralHelper]::SetItemOwned($item,$true) }
    return $item
}
[EmporiumEntry]::TestInstance=Blank ([EmporiumEntry])
function Count-Cells($shape) {
    $n=0
    for($y=0;$y -lt $shape.height;$y++){for($x=0;$x -lt $shape.width;$x++){if($shape.GetLocal($x,$y)){$n++}}}
    return $n
}
$legacy='0000011001100000'
$oversized='0000111111110000'
$repair=[EleanorRomance.Items].GetMethod('RepairBraceletShape',[Reflection.BindingFlags]'NonPublic,Static')
function New-Inventory {
    $inv=Blank ([GameGridInventory])
    [GameGridInventory].GetField('<inventoryShape>k__BackingField',[Reflection.BindingFlags]'NonPublic,Instance').SetValue(
        $inv,[GridShapeBuilder]::new().SetData(('0'*64),8).SetDataOutside([byte]1).Build())
    $inv.mayInventoryAddItemFunc=[Func[GameItem,GameInventory,bool]]{param($i,$v) $true}
    [GameGridInventory].GetField('items',[Reflection.BindingFlags]'NonPublic,Instance').SetValue(
        $inv,[System.Collections.Generic.List[GameItem]]::new())
    return $inv
}
function Inventory-Items($inv) {
    # Native childItems returns a copy; populate the underlying fixture list.
    return ,([GameGridInventory].GetField('items',[Reflection.BindingFlags]'NonPublic,Instance').GetValue($inv))
}
# Both historical masks, all 8 transforms, and placements against all four edges.
# Fill every cell outside the old footprint: shrinking must still fit and must
# leave the neighbours, orientation, and ownership state untouched.
foreach($mask in @($legacy,$oversized)){foreach($flip in @($false,$true)){foreach($rotation in 0..3){
    foreach($x in @(0,4)){foreach($y in @(0,4)){
        $inv=New-Inventory
        $list=Inventory-Items $inv
        $item=New-Item $mask 4 $x $y $flip $rotation
        $item.parentInventory=$inv;$list.Add($item)
        $old=$item.modifiedShape.Clone()
        for($cy=0;$cy -lt 8;$cy++){for($cx=0;$cx -lt 8;$cx++){
            if($old.Get($cx,$cy) -ne 0){continue}
            $neighbour=New-Item '1' 1 $cx $cy
            $neighbour.identifier='test_neighbour';$neighbour.parentInventory=$inv;$list.Add($neighbour)
        }}
        $before=$list.Count
        Assert ($repair.Invoke($null,@($item))) 'Compact bracelet could not fit inside its former footprint.'
        Assert ($item.shape.width -eq 2 -and $item.shape.height -eq 1 -and
            (Count-Cells $item.modifiedShape) -eq 2) 'Wrong compact bracelet footprint.'
        Assert ($item.modifiedShape.flipped -eq $flip -and $item.modifiedShape.orientation -eq $rotation) 'Repair changed the orientation.'
        for($cy=0;$cy -lt 8;$cy++){for($cx=0;$cx -lt 8;$cx++){
            if($item.modifiedShape.Get($cx,$cy) -ne 0){
                Assert ($old.Get($cx,$cy) -ne 0) 'Compact shape moved outside the old occupied cells.'
            }
        }}
        Assert ($list.Count -eq $before -and $item.parentInventory -eq $inv -and
            $item.spritePath -eq 'eleanor_bracelet_compact') 'Repair lost an item or failed to update its sprite.'
        $once=$item.modifiedShape
        Assert ($repair.Invoke($null,@($item)) -and [object]::ReferenceEquals($once,$item.modifiedShape)) 'Repeated load changed an already compact item.'
    }}
}}}
# Inventory rules can defer migration: retain the original item and retry later.
$inv=New-Inventory
$list=Inventory-Items $inv
$inv.mayInventoryAddItemFunc=[Func[GameItem,GameInventory,bool]]{param($i,$v) $false}
$item=New-Item $legacy
$item.parentInventory=$inv; $list.Add($item)
$old=$item.shape;$oldModified=$item.modifiedShape
Assert (!$repair.Invoke($null,@($item))) 'Repair ignored an inventory rule.'
Assert ([object]::ReferenceEquals($old,$item.shape) -and [object]::ReferenceEquals($oldModified,$item.modifiedShape)) 'Deferred repair changed the saved footprint.'
$inv.mayInventoryAddItemFunc=[Func[GameItem,GameInventory,bool]]{param($i,$v) $true}
Assert ($repair.Invoke($null,@($item))) 'Repair did not retry after the rule cleared.'
Assert ((Count-Cells $item.modifiedShape) -eq 2 -and $list.Count -eq 1) 'Repair lost items or kept the wrong shape.'
# Factory creates the corrected shape and attaches a placement retry callback.
[ItemDirectory]::TestItem=New-Item '1' 1
$created=[EleanorRomance.Items]::CreateBracelet()
Assert ($created.shape.width -eq 2 -and $created.shape.height -eq 1 -and
    $created.spritePath -eq 'eleanor_bracelet_compact' -and $created.onSlotAddItemFunc -ne $null) 'New bracelet factory has wrong shape/sprite/hooks.'
'PASS: 2x1 bracelet factory, 64 packed legacy migrations, inventory-rule deferral and retry.'
# Native small positive markups must count as generosity, not just >25%.
foreach($paid in @(101,110,120,125,130)){
    $item=New-Item '1' 1
    $item.unitValue=100
    $tracker=[EleanorRomance.StoryEngine+DealTracker]::new()
    # Native base pricing runs on a simple 100-credit fixture without price features.
    $tracker.Record($item,$paid,$true)
    Assert ($tracker.Dominant -eq 'D') "Markup $paid was not generous."
}
'PASS: small and large overpayment classifications.'

# All 13 trading chapter factories use the same native automatic completion
# flow as well as the optional manual finish; assert real persisted progression.
[ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new()
$station=Blank ([StoreStation]);$station.dayCounter=40
[StoreStation]::TestInstance=$station
$next=@{1=2;2=3;3=4;6=7;7=8;8=9;10=11;18=19;19=20;22=23;103=104;105=106;108=300}
foreach($manual in @($false,$true)){
    foreach($chapter in ($next.Keys | Sort-Object)){
        [ModHelper]::TestData.Clear()
        [PlayerStore]::TestDismissCount=0
        [EleanorRomance.RomanceState]::Chapter=$chapter-1
        [EleanorRomance.RomanceState]::PendingChapter=$chapter
        [EleanorRomance.RomanceState]::MotherState=1
        $c=[EleanorRomance.EleanorStory]::CreateClient($chapter)
        Assert ($c -ne $null -and $c.isBargainAvailable) "Chapter $chapter has no bargaining."
        Assert ($c.acceptDealDialogue.plainText -eq 'er_line_trade_accept') "Chapter $chapter has a placeholder acceptance."
        $instance=Blank ([StoreClientInstance]);$instance.storeClient=$c
        $store.currentClientInstance=$instance
        if($manual) {
            $store.TryPlayerDismissCurrentClient($true)
        } else {
            if($c.clientIntent -eq [StoreClient+ClientIntent]::SELL) {
                $item=New-Item '1' 1;$item.unitValue=100
                if($c.onItemSold){$c.onItemSold.Invoke($item,110)}
                [StoreClientInstance]::TestHasStock=$false
            } else {
                $c.clientBudget=0
            }
            $c.TestOnDealAccepted($null)
        }
        Finish-Dialogue $ui.currentDialog
        Assert ([EleanorRomance.RomanceState]::Chapter -eq $next[$chapter] -and
            [EleanorRomance.RomanceState]::PendingChapter -eq 0 -and
            [PlayerStore]::TestDismissCount -eq 1) "Chapter $chapter progression failed (manual=$manual)."
        if(!$manual -and $chapter -in @(3,7,8,19)) {
            Assert ([EleanorRomance.RomanceState]::Affection -eq 2 -and
                [EleanorRomance.RomanceState]::Discounts -eq 1) "Chapter $chapter did not reward overpayment."
        }
    }
}
'PASS: 13 chapters x automatic/manual completion, bargaining and actual affection/progression state.'

function Reset-Run($chapter=5) {
    [ModHelper]::TestData.Clear()
    [EleanorRomance.RomanceState]::Initialized=$true
    [EleanorRomance.RomanceState]::SchemaVersion=2
    [EleanorRomance.RomanceState]::Chapter=$chapter
    [EleanorRomance.RomanceState]::PendingChapter=$chapter
    $store.storeClientData=[StoreClientData]::new()
    $store.storeClientManager=Blank ([StoreClientManager])
    $store.storeClientManager.clientStack=[System.Collections.Generic.List[StoreClient]]::new()
    $store.currentClientInstance=$null
    [PlayerStore]::TestDismissCount=0
    $station.dayCounter=40
    [GraphUtils]::TestRemaining=0
    [StoreReputation]::TestSecRep=0
}
function Use-Client($client) {
    $instance=Blank ([StoreClientInstance]);$instance.storeClient=$client
    $store.currentClientInstance=$instance
}
function Choice-Node($head) {
    while($head -and $head.choices.Count -eq 0){$head=$head.nextDialogue}
    return $head
}
# Native aid action supplies its price, affordability predicate and accounting.
Reset-Run 6
$store.playerCash=49
$c=[EleanorRomance.EleanorStory]::CreateClient(6)
Use-Client $c
$choices=Choice-Node $c.mainDialogue
Assert ($choices -and !$choices.IsChoiceEnabledAt(0)) 'Cash aid should exist but be disabled below 50.'
$store.playerCash=50
Assert ($choices.IsChoiceEnabledAt(0)) 'Cash aid did not unlock after taking out cash.'
$cash=$choices.choices[0]
$cash.Item3.Invoke()
Assert ($store.playerCash -eq 0 -and $store.storeClientData.eleanorHelpedTimes -eq 1 -and
    [EleanorRomance.RomanceState]::HardshipCashGiven) 'Native 50-credit aid did not pay/record exactly once.'
$cash.Item3.Invoke()
Assert ($store.playerCash -eq 0 -and $store.storeClientData.eleanorHelpedTimes -eq 1) 'Cash aid paid twice.'
Finish-Dialogue $cash.Item2
Assert ($ui.currentDialog.plainText -eq 'er_line_cash_aid_1') 'Cash aid used the food-handing dialogue.'
Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.RomanceState]::Chapter -eq 7 -and
    [EleanorRomance.RomanceState]::Affection -eq 2) 'Cash aid did not finish like food aid.'
$aidSnapshot=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
[ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new($aidSnapshot)
$again=[EleanorRomance.EleanorStory]::CreateClient(6)
$store.playerCash=100
Assert (!(Choice-Node $again.mainDialogue).IsChoiceEnabledAt(0)) 'Reloaded aid allowed a duplicate payment.'
'PASS: native 50-credit aid, dynamic funds, persisted deduplication and cash-specific dialogue.'

# Native warning rewards are captured before placement, then use the mod's
# tested placement/retry mechanism. The numbers below are expectations for the
# installed official update, not values duplicated in production mod code.
foreach($option in 0..1){
    Reset-Run
    Assert ([EleanorRomance.VanillaEleanorBridge]::GiveWarningGift($option)) 'Native gift was not delivered.'
    $gift=[GraphUtils]::TestLastGift
    Assert ([GeneralHelper]::IsItemOwned($gift)) 'Native gift was not player-owned.'
    if($option -eq 0){
        Assert ($gift.identifier -eq 'portable_water_purifier' -and
            [DurabilityHelper]::GetRealDurability($gift) -eq 175) 'Native purifier reward balance changed or was lost.'
    } else {
        Assert ($gift.identifier -eq 'pheromone_perfume' -and
            $gift.GetTagReadonly('CURRENT_USE_COUNT_INT').GetInt() -eq 5) 'Native perfume reward balance changed or was lost.'
    }
}
Reset-Run
[GraphUtils]::TestRemaining=1
Assert (![EleanorRomance.VanillaEleanorBridge]::GiveWarningGift(0)) 'Full inventory falsely accepted a native gift.'
[GraphUtils]::TestRemaining=0
Assert ([EleanorRomance.VanillaEleanorBridge]::GiveWarningGift(0)) 'Native gift could not be retried.'
Assert (![EleanorRomance.VanillaEleanorBridge]::CaptureReward((New-Item '1' 1))) 'Native placement capture leaked outside its scope.'
'PASS: actual native 175-durability/5-use rewards, ownership, full-inventory retry and capture scope.'

# Run the real native CheckStoryLineEleanor rather than a copied rescue calendar.
foreach($early in @($false,$true)){foreach($free in @($false,$true)){
    Reset-Run
    [EleanorRomance.RomanceState]::EarlyStart=$early
    [EleanorRomance.VanillaEleanorBridge]::RecordArrest(8)
    $manager=$store.storeClientManager
    $store.playerCash=499
    $station.dayCounter=41
    $manager.CheckStoryLineEleanor()
    Assert ($manager.clientStack.Count -eq 0) 'Native rescue arrived before two days.'
    $station.dayCounter=42
    $manager.CheckStoryLineEleanor()
    Assert ($manager.clientStack.Count -eq 1 -and $manager.clientStack[0].identifier -eq 'eleanorBailOfficer') 'Native officer did not arrive on D+2.'
    $manager.CheckStoryLineEleanor()
    Assert ($manager.clientStack.Count -eq 1) 'Native scheduler queued a duplicate officer.'
    $officer=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $officer
    $choices=Choice-Node $officer.mainDialogue
    Assert (!$choices.IsChoiceEnabledAt(0)) 'Native paid bail accepted less than 500.'
    [StoreReputation]::TestSecRep=39
    Assert (!$choices.IsChoiceEnabledAt(1)) 'Native free bail accepted reputation below 40.'
    if($free){
        [StoreReputation]::TestSecRep=40
        Assert ($choices.IsChoiceEnabledAt(1)) 'Native reputation bail did not unlock at 40.'
        $choices.choices[1].Item3.Invoke()
        Assert ($store.playerCash -eq 499) 'Free native bail charged money.'
    } else {
        $store.playerCash=500
        Assert ($choices.IsChoiceEnabledAt(0)) 'Native paid bail did not unlock at 500.'
        $choices.choices[0].Item3.Invoke()
        $choices.choices[0].Item3.Invoke()
        Assert ($store.playerCash -eq 0) 'Native bail did not charge exactly once.'
    }
    $store.currentClientInstance=$null
    $manager.CheckStoryLineEleanor()
    Assert ($manager.clientStack.Count -eq 0) 'Freed visitor arrived on the bail day.'
    $station.dayCounter=43
    $manager.CheckStoryLineEleanor()
    Assert ($manager.clientStack.Count -eq 1 -and $manager.clientStack[0].identifier -eq 'eleanorFreed') 'Native freed visitor did not arrive on D+3.'
    $freed=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $freed
    [EleanorRomance.VanillaEleanorBridge]::OnLoaded()
    [EleanorRomance.VanillaEleanorBridge]::OnLoaded()
    Assert ($freed.mainDialogue.startAction -eq $null -and $store.storeClientData.eleanorFreedVisitDay -lt 0) 'Freed visit committed before its dialogue.'
    $savedBail=$store.storeClientData.eleanorBailChoice
    $savedCash=$store.playerCash
    Finish-Dialogue $freed.mainDialogue
    Assert ([EleanorRomance.RomanceState]::Rescued -and ![EleanorRomance.RomanceState]::Arrested -and
        [EleanorRomance.RomanceState]::Chapter -eq 8 -and $store.storeClientData.eleanorFreedVisitDay -eq 43) 'Native release did not resume the saved remake chapter.'
    Finish-Dialogue $freed.mainDialogue
    Assert ($store.playerCash -eq $savedCash -and $store.storeClientData.eleanorBailChoice -eq $savedBail -and
        $store.storeClientData.eleanorHelpedTimes -eq 1) 'Native release replay duplicated accounting.'
}}
'PASS: native scheduler D+2/D+3, 500/40 rules, both starting modes, dedupe and chapter continuation.'

Reset-Run
[EleanorRomance.RomanceState]::Arrested=$true
[EleanorRomance.RomanceState]::TrapResumeChapter=10
[EleanorRomance.RomanceState]::Affection=17
[EleanorRomance.RomanceState]::DriveState=2
[EleanorRomance.VanillaEleanorBridge]::SynchronizeRescue()
Assert ($store.storeClientData.eleanorArrestDay -eq 40 -and [EleanorRomance.RomanceState]::NativeRescueLinked) 'Legacy MOD arrest did not use native migration.'
$station.dayCounter=41
[EleanorRomance.VanillaEleanorBridge]::SynchronizeRescue()
Assert ($store.storeClientData.eleanorArrestDay -eq 40 -and
    [EleanorRomance.RomanceState]::Affection -eq 17 -and [EleanorRomance.RomanceState]::DriveState -eq 2) 'Repeated migration postponed bail or reset story progress.'
[EleanorRomance.RomanceState]::OldBlocked=$true
$store.storeClientData.eleanorArrestDay=7
Assert ([EleanorRomance.VanillaEleanorBridge]::FilterScheduledClient($store.storeClientManager,
    [StoreClient]::new())) 'Preserved vanilla save was blocked.'
[EleanorRomance.VanillaEleanorBridge]::SynchronizeRescue()
Assert ($store.storeClientData.eleanorArrestDay -eq 7) 'Vanilla-preserved save was migrated by the remake.'
'PASS: legacy MOD arrest migration, preserved progression and untouched vanilla-preserved saves.'

# A native introductory visit due on the same day must not accompany the bail officer.
Reset-Run
$station.dayCounter=21
[EleanorRomance.VanillaEleanorBridge]::RecordArrest(7)
$station.dayCounter=23
$store.storeClientManager.CheckStoryLineEleanor()
Assert ($store.storeClientManager.clientStack.Count -eq 1 -and
    $store.storeClientManager.clientStack[0].identifier -eq 'eleanorBailOfficer') 'Vanilla introduction leaked into remake rescue.'
$external=[StoreClient]::new();$external.identifier='eleanor_external';$external.sourceModId='another_mod'
Assert ([EleanorRomance.VanillaEleanorBridge]::FilterScheduledClient($store.storeClientManager,$external)) 'Queue filter swallowed an unrelated mod client.'

# Postponement leaves native bail eligibility intact; refusal ends only the remake.
foreach($decline in @($false,$true)){
    Reset-Run
    [EleanorRomance.VanillaEleanorBridge]::RecordArrest(7)
    $station.dayCounter=42
    $manager=$store.storeClientManager;$manager.CheckStoryLineEleanor()
    $officer=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $officer
    $node=Choice-Node $officer.mainDialogue
    if($decline){$node.choices[2].Item3.Invoke()}else{$node.choices[3].Item3.Invoke()}
    if($officer.OnDismiss){$officer.OnDismiss.Invoke()}
    $store.currentClientInstance=$null
    $station.dayCounter=43
    $manager.CheckStoryLineEleanor()
    if($decline){
        Assert ([EleanorRomance.RomanceState]::RomanceLocked -and
            $manager.clientStack.Count -eq 0) 'Refused bail left a stranded rescue loop.'
    } else {
        Assert ($store.storeClientData.eleanorBailChoice -eq 0 -and
            $manager.clientStack.Count -eq 1) 'Postponed bail could not be offered again.'
    }
}
# An interrupted release does not mark the native visit and is eligible next day.
Reset-Run
[EleanorRomance.VanillaEleanorBridge]::RecordArrest(10)
$store.storeClientData.eleanorBailChoice=1;$store.storeClientData.eleanorBailDay=40
$station.dayCounter=41
$manager=$store.storeClientManager;$manager.CheckStoryLineEleanor()
$interrupted=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $interrupted
$null=$ui.StartDialogue($interrupted.mainDialogue,$false)
$store.currentClientInstance=$null;$station.dayCounter=42
$manager.CheckStoryLineEleanor()
Assert ($manager.clientStack.Count -eq 1 -and
    $store.storeClientData.eleanorFreedVisitDay -lt 0 -and ![EleanorRomance.RomanceState]::Rescued) 'Interrupted release became ineligible.'
$retry=$manager.clientStack[0];Use-Client $retry;Finish-Dialogue $retry.mainDialogue
Assert ([EleanorRomance.RomanceState]::Chapter -eq 10 -and
    $store.storeClientData.eleanorHelpedTimes -eq 1) 'Release retry skipped a chapter or counted help twice.'
'PASS: queue isolation, external-mod passthrough, postponed/refused bail and interrupted release retry.'

$claim=[EleanorRomance.EleanorStory].GetMethod('GiveWarningGift',[Reflection.BindingFlags]'NonPublic,Static')
Reset-Run
[GraphUtils]::TestRemaining=1
$claim.Invoke($null,@([int]0))
Assert (![EleanorRomance.RomanceState]::WarningGiftGiven) 'Full native reward consumed the claim.'
[GraphUtils]::TestRemaining=0
$claim.Invoke($null,@([int]0))
Assert ([EleanorRomance.RomanceState]::WarningGiftGiven) 'Delivered native reward did not persist its claim.'
$received=[GraphUtils]::TestLastGift
$claim.Invoke($null,@([int]1))
Assert ([object]::ReferenceEquals($received,[GraphUtils]::TestLastGift)) 'Saved reward claim allowed a second gift.'
[ItemDirectory]::TestItem=New-Item '1' 1
$locket=[EleanorRomance.Items]::CreateLocket()
Assert ($locket.IsTag('IMPORTANT_TAG')) 'New keepsake did not inherit story-item protection.'
'PASS: native reward claim retry/deduplication and keepsake protection.'
. (Join-Path $PSScriptRoot 'test_arrest_speakers.ps1')
if($Maid){
    . (Join-Path $PSScriptRoot 'test_maid_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_business_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_motivation_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_dividend_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_project_discount_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_scav_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_interaction_routes.ps1')
    . (Join-Path $PSScriptRoot 'test_relationship_choices.ps1')
    . (Join-Path $PSScriptRoot 'test_named_speakers.ps1')
}

if($Dual){. (Join-Path $PSScriptRoot 'test_dual_routes.ps1');. (Join-Path $PSScriptRoot 'test_dual_showcase.ps1')}
. (Join-Path $PSScriptRoot 'test_reading_fixes.ps1')
