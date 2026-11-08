# Character progression through the real chapter graphs and completion hooks.
$motivationZh=@{}
Import-Csv (Join-Path $PSScriptRoot '../Localization/zh.csv') -Header key,value | ForEach-Object {$motivationZh[$_.key]=$_.value}
function Motivation-Nodes($head){
    $pending=[System.Collections.Generic.Queue[Dialogue]]::new()
    $seen=[System.Collections.Generic.HashSet[Dialogue]]::new()
    if($head){$pending.Enqueue($head)}
    while($pending.Count){
        $node=$pending.Dequeue();if(!$seen.Add($node)){continue}
        $node
        if($node.nextDialogue){$pending.Enqueue($node.nextDialogue)}
        foreach($choice in $node.choices){if($choice.Item2){$pending.Enqueue($choice.Item2)}}
    }
}
function Has-MotivationBlock($head,$block){
    return @((Motivation-Nodes $head) | Where-Object {$_.plainText -like "er_maid_${block}_*"}).Count -gt 0
}
function Assert-NoEarlyAddress($head){
    foreach($node in (Motivation-Nodes $head)){
        if($node.title -ne [EleanorRomance.L]::Eleanor){continue}
        $line=$motivationZh[$node.plainText]
        Assert ($line -notmatch '^[…\s]*主人[，。！]|给主人|叫你主人，我叫了') "Premature direct address: $line"
    }
}
function Motivation-Visit($code){
    $station.dayCounter=[EleanorRomance.StoryDirector]::TargetDay($code)-[EleanorRomance.StoryDirector]::StoryOffset()
    return Main-Client $code
}

# Both successful rescue strategies and protection fallback. The old injury is
# disclosed before custody, then trust is repaired only after the actual result.
foreach($early in @($false,$true)){foreach($route in @('exchange','protection','fallback')){
    New-MaidRun
    [EleanorRomance.RomanceState]::EarlyStart=$early
    [EleanorRomance.RomanceState]::Savings=800
    [EleanorRomance.RomanceState]::UncleReported=$true
    [EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
    $c=Motivation-Visit 12;Assert-NoEarlyAddress $c.mainDialogue
    Finish-Dialogue (Pick (Pick $c.mainDialogue 1) 0)
    Assert (![EleanorRomance.MaidState]::AddressAccepted -and ![EleanorRomance.MaidState]::DebutShown) 'Entry granted the private identity.'
    $c=Motivation-Visit 13;Assert-NoEarlyAddress $c.mainDialogue
    Assert (Has-MotivationBlock $c.mainDialogue 'm12_b004') 'Old injury was not disclosed before drive custody.'
    $study=Pick $c.mainDialogue 0
    Assert ([EleanorRomance.MaidState]::Flag('honesty.promised') -and ![EleanorRomance.MaidState]::Flag('reconciled')) 'Promise alone repaired trust.'
    Finish-Dialogue (Pick $study 1)
    Assert ([EleanorRomance.RomanceState]::DriveState -eq 2) 'Early honesty displaced drive custody.'
    $c=Motivation-Visit 14;Finish-Dialogue (Pick $c.mainDialogue 0)
    $c=Motivation-Visit 15;Assert-NoEarlyAddress $c.mainDialogue
    $index=if($route -eq 'exchange'){0}else{1};Finish-Dialogue (Pick $c.mainDialogue $index)
    $c=Motivation-Visit 16;Assert-NoEarlyAddress $c.mainDialogue
    if($route -eq 'exchange'){Finish-Dialogue $c.mainDialogue}
    else{Finish-Dialogue (Pick $c.mainDialogue $(if($route -eq 'fallback'){3}else{1}))}
    $c=Motivation-Visit 17;Assert-NoEarlyAddress $c.mainDialogue
    $rescueDay=$station.dayCounter
    Assert (Has-MotivationBlock $c.mainDialogue 'm16_b007') 'Rescue did not return to the earlier honesty promise.'
    Assert (![EleanorRomance.MaidState]::Flag('reconciled')) 'Creating the result committed trust before reading it.'
    Finish-Dialogue $c.mainDialogue
    $expected=if($route -eq 'protection'){4}else{3}
    Assert ([EleanorRomance.RomanceState]::MotherState -eq 1 -and
        [EleanorRomance.RomanceState]::DriveState -eq $expected -and [EleanorRomance.MaidState]::Flag('reconciled') -and
        ![EleanorRomance.MaidState]::AddressAccepted) 'Rescue/trust facts or delayed address were wrong.'
    $c=Motivation-Visit 18;Assert-NoEarlyAddress $c.mainDialogue;Leave-Trade $c
    $c=Motivation-Visit 19;Assert-NoEarlyAddress $c.mainDialogue
    Assert (!$c.spriteName.StartsWith('er_portrait_eleanor_maid_')) 'Work visit used the formal maid outfit.'
    Finish-Dialogue $c.mainDialogue;Leave-Trade $c
    Assert-NoEarlyAddress $ui.currentDialog
    Assert (![EleanorRomance.MaidState]::AddressAccepted -and ![EleanorRomance.MaidState]::DebutShown) 'Post-rescue work skipped dependency development.'
    $c=Motivation-Visit 20;Finish-Dialogue $c.mainDialogue
    Assert ($station.dayCounter -ge $rescueDay+4) 'Address immediately followed rescue instead of later visits.'
    Assert (![EleanorRomance.MaidState]::AddressAccepted) 'Address was accepted on arrival/stock spawn.'
    $store.TryPlayerDismissCurrentClient($true)
    $wrap=$ui.currentDialog
    Assert (Has-MotivationBlock $wrap 'm19_b006') 'First deliberate address is missing after business.'
    $node=$wrap
    while($node -and $node.plainText -ne 'er_maid_m19_b006_07'){
        if($node.startAction){$node.startAction.Invoke()};$node.ExecuteEndAction();$node=$node.nextDialogue
    }
    Assert ($node -ne $null -and ![EleanorRomance.MaidState]::AddressAccepted) 'The private address was committed before its line.'
    Finish-Dialogue $node
    Assert ([EleanorRomance.MaidState]::AddressAccepted -and [EleanorRomance.MaidState]::DebutShown -and
        [EleanorRomance.RomanceState]::Chapter -eq 21 -and ![EleanorRomance.MaidState]::Lovers) 'First address failed or also forced a romance.'
    # Rebuild after an interrupted/replayed visit: do not announce a second first.
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    $cash=$store.playerCash;$c=Main-Client 20
    Assert (!(Has-MotivationBlock $c.allDoneDialogue 'm19_b006') -and
        (Has-MotivationBlock $c.allDoneDialogue 'm19_b007') -and $store.playerCash -eq $cash) 'Saved address replayed its first occurrence or charged again.'
}}
'PASS: both starting modes x exchange/protection/fallback, early disclosure/later trust repair, post-rescue dependency, delayed address/debut, exact-line commit and saved no-second-first.'

# Returning the original is an active repair, retaining the work agreement and
# the rescue's continuation. Neither the tragic nor betrayal branches
# claim an address that has not happened in a new playthrough.
New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
[EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
$cash=$store.playerCash;$c=Main-Client 13
Finish-Dialogue (Pick $c.mainDialogue 1)
Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.MaidState]::Flag('honesty.promised') -and [EleanorRomance.RomanceState]::DriveState -eq 2 -and
    [EleanorRomance.RomanceState]::Chapter -eq 14 -and $store.playerCash -eq $cash -and
    ![EleanorRomance.MaidState]::AddressAccepted) 'Early handback ended the agreement, invented a payment or lost her drive.'
$c=[EleanorRomance.EleanorStory]::CreateClient(14)
Assert ($c -ne $null -and $c.identifier -eq 'er_story_maid_m13') 'Early handback did not continue the attendant investigation.'
New-MaidRun;[EleanorRomance.MaidState]::Enter(2)
foreach($code in @(101,104,105,201)){
    [EleanorRomance.RomanceState]::Route=$(if($code -eq 201){4}else{3})
    [EleanorRomance.RomanceState]::BetrayalKind=1
    $c=Main-Client $code;Assert-NoEarlyAddress $c.mainDialogue
    if($code -eq 105){Leave-Trade $c;Assert-NoEarlyAddress $ui.currentDialog}
}
Assert (![EleanorRomance.MaidState]::AddressAccepted) 'Failed rescue granted the later identity.'
'PASS: early handback preserves the agreement, drive, resources and rescue continuation; tragic and betrayal branches contain no invented past address.'

# Conservative migration of already-played 0.73 dialogue. No story, payment,
# world result or feature schema is reset, and new entries remain unaddressed.
foreach($chapter in @(13,19,20,21,24,300)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.MaidState]::MotivationVersion=0
    [EleanorRomance.RomanceState]::Chapter=$chapter
    [EleanorRomance.RomanceState]::PendingChapter=0
    [EleanorRomance.RomanceState]::DriveState=2
    $cash=$store.playerCash
    1..3 | ForEach-Object {[EleanorRomance.MaidRouteMigration]::PreserveEstablishedAddress()}
    Assert ([EleanorRomance.MaidState]::AddressAccepted -and [EleanorRomance.MaidState]::MotivationVersion -eq 1 -and
        [EleanorRomance.RomanceState]::Chapter -eq $chapter -and [EleanorRomance.RomanceState]::PendingChapter -eq 0 -and
        [EleanorRomance.RomanceState]::DriveState -eq 2 -and $store.playerCash -eq $cash) 'Existing-history migration changed world/progression/payment.'
}
New-MaidRun;[EleanorRomance.MaidState]::Enter(2)
[EleanorRomance.MaidRouteMigration]::PreserveEstablishedAddress()
Assert (![EleanorRomance.MaidState]::AddressAccepted) 'New waived entry was mistaken for an old established relationship.'
$store.playerCash=0
[EleanorRomance.MaidRouteMigration]::PreserveEstablishedAddress()
Assert (![EleanorRomance.MaidState]::AddressAccepted) 'Funds were used as a private-address gate.'
'PASS: established-address migration across six old stages, repeated loads, unchanged resources/world/schema and new cash/waived-entry separation.'
