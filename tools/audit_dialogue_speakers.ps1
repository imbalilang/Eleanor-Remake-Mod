param([string]$Managed='D:/Program Files (x86)/Steam/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed',[string]$ModDll)
$auditRoot=Split-Path $PSScriptRoot -Parent
$null=New-Item -ItemType Directory -Path (Join-Path $auditRoot 'dist/report-speakers') -Force
if(!$ModDll){$ModDll=Join-Path $auditRoot 'bin/Release/EleanorRomance.dll'}
. (Join-Path $auditRoot 'tools/test_runtime_flows.ps1') -Managed $Managed -ModDll $ModDll -Rewrite *> (Join-Path $auditRoot 'dist/report-speakers/audit-bootstrap.log')
$ErrorActionPreference='Stop'
$auditRows=[System.Collections.Generic.List[object]]::new()
$auditCases=[System.Collections.Generic.List[object]]::new()
$auditSource=@{}
$main=Get-Content -LiteralPath (Join-Path $auditRoot 'Content/story_v06.json') -Raw -Encoding utf8|ConvertFrom-Json
foreach($chapter in $main.chapters){foreach($block in $chapter.blocks){for($i=0;$i -lt $block.lines.Count;$i++){
    $key='er_v06_'+$block.id.Replace('.','_')+'_'+($i+1).ToString('00')
    $auditSource[$key]=@{role=$block.lines[$i].speaker;block=$block.id;text=$block.lines[$i].text}
}}}
$auditDisplay=[DialogUIManager].GetMethod('DisplayLine',[Reflection.BindingFlags]'NonPublic,Instance')
function Inspect-Speakers($scenario,$client){
    if($null -eq $client){$auditCases.Add(@{scenario=$scenario;status='no visitor in this state'});return}
    Use-Client $client
    $roots=[System.Collections.Generic.List[Dialogue]]::new()
    foreach($head in @($client.mainDialogue,$client.customOnArrestDialogue,$client.allDoneDialogue)){
        if($head){$roots.Add($head)}
    }
    if($client.tryInterceptDismiss){
        $null=$client.tryInterceptDismiss.Invoke()
        if($ui.currentDialog){$roots.Add($ui.currentDialog)}
    }
    $seen=[System.Collections.Generic.HashSet[Dialogue]]::new()
    $pending=[System.Collections.Generic.Stack[Dialogue]]::new()
    foreach($head in $roots){$pending.Push($head)}
    while($pending.Count){
        $node=$pending.Pop()
        if(!$seen.Add($node)){continue}
        if($node.nextDialogue){$pending.Push($node.nextDialogue)}
        foreach($choice in $node.choices){if($choice.Item2){$pending.Push($choice.Item2)}}
        if($node.title -in @('PLAYER','SEC',$client.displayName)){continue}
        $store.dialogHistory=[System.Collections.Generic.Queue[string]]::new()
        $null=$auditDisplay.Invoke($ui,@($node))
        $source=$auditSource[$node.plainText]
        $role=if($source){$source.role}else{if($node.title -eq 'er_name_narrator'){'Narrator'}else{$node.title}}
        $history=$store.dialogHistory.ToArray()[0]
        $auditRows.Add([pscustomobject]@{scenario=$scenario;client=$client.identifier;host=$client.displayName;
            key=$node.plainText;role=$role;block=$(if($source){$source.block}else{''});
            intended=$node.title;actualHistory=$history;resolved=($history -eq ($node.title+': '+$node.plainText));text=$(if($source){$source.text}else{''})})
    }
    $auditCases.Add(@{scenario=$scenario;client=$client.identifier;nodes=$seen.Count;status='inspected'})
}
foreach($mode in @('ordinary','attendant')){foreach($route in 1..2){foreach($code in 1..24){
    if($mode -eq 'ordinary' -and $code -in @(16,17)){continue}
    if($mode -eq 'attendant' -and $code -gt 22){continue}
    Reset-Run $code
    [EleanorRomance.RomanceState]::Route=$route;[EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::ProtectionReady=($route -eq 2)
    [EleanorRomance.RomanceState]::Savings=800;[EleanorRomance.RomanceState]::DriveState=1
    [EleanorRomance.RomanceState]::FairDeals=4;[EleanorRomance.RomanceState]::UncleReported=($code -ne 4)
    [EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
    [EleanorRomance.RomanceState]::Romance=1
    if($mode -eq 'attendant' -and $code -ge 13){[EleanorRomance.MaidV2State]::Enter();[EleanorRomance.MaidState]::Confession=1;[EleanorRomance.RomanceState]::Romance=1}
    [EleanorRomance.RomanceState]::Chapter=$code;[EleanorRomance.RomanceState]::PendingChapter=$code
    Inspect-Speakers "$mode route$route main$code" ([EleanorRomance.EleanorStory]::CreateClient($code))
}}}
foreach($code in 280..290){
    Reset-Run $code
    [EleanorRomance.RomanceState]::Route=3
    [EleanorRomance.RomanceState]::MotherState=2
    [EleanorRomance.RomanceState]::RevVersion=2
    Inspect-Speakers "revolution $code" ([EleanorRomance.EleanorStory]::CreateClient($code))
}
foreach($route in 1..2){foreach($id in (@(25..28)+@(910..912)+@(915))){
    Reset-Run $id
    [EleanorRomance.MaidV2State]::Enter()
    [EleanorRomance.MaidState]::Confession=1
    [EleanorRomance.RomanceState]::Romance=1
    [EleanorRomance.RomanceState]::Route=$route
    [EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.MaidState]::Progress=28
    [EleanorRomance.MaidState]::PendingVisit=$id
    Inspect-Speakers "attendant route$route visit$id" ([EleanorRomance.MaidStory]::CreateEvent($id))
}}
$unique=@($auditRows|Sort-Object client,key -Unique)
$unresolved=@($auditRows|Where-Object {!$_.resolved})
$report=[pscustomobject]@{scope='Read-only runtime factory/graph audit using native DisplayLine and history. Branches are inspected, not played. Not a full save-state Cartesian product.';unresolved=$unresolved.Count;
    cases=$auditCases;explicitRoleLines=$unique;allObservations=$auditRows}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $auditRoot 'dist/report-speakers/runtime-audit.json') -Encoding utf8
[pscustomobject]@{cases=$auditCases.Count;inspected=@($auditCases|Where-Object status -eq 'inspected').Count;
    uniqueKeys=@($auditRows.key|Sort-Object -Unique).Count;unresolved=$unresolved.Count;byRole=@($auditRows|Group-Object role|ForEach-Object {[pscustomobject]@{role=$_.Name;observations=$_.Count;uniqueKeys=@($_.Group.key|Sort-Object -Unique).Count}})}|ConvertTo-Json -Depth 5
$unique|Where-Object role -notin @('Narrator','narrator')|Select-Object scenario,key,role,intended,actualHistory|ConvertTo-Json -Depth 4
if($unresolved.Count){throw "$($unresolved.Count) attribution observations remain incorrect."}
