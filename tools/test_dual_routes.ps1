# Dot-source after test_runtime_flows.ps1 -Maid -Dual has loaded real assemblies.
function Dual-Run($rep=0){
    New-MaidRun 19
    [EleanorRomance.DualShowcaseEffects]::ClearMemory()
    [EleanorRomance.DualShowcaseEffects]::Enabled=$true
    [PlayerStore]::TestAllItems=[System.Collections.Generic.List[GameItem]]::new()
    [EmporiumEntry]::TestShowcase=New-Inventory
    [EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::MotherVisited=$true
    [EleanorRomance.RomanceState]::Route=1
    [EleanorRomance.RomanceState]::UncleReported=$true
    [EleanorRomance.RomanceState]::FairDeals=3
    [EleanorRomance.RomanceState]::LastQueuedDay=-1
    $station.dayCounter=49
    Set-Reps $rep 0
    [EleanorRomance.StoryCoreFacts]::Migrate()
    [EleanorRomance.DualMigration]::OnLoaded()
}
function Dual-Finish($id,$choice=-1){
    $c=[EleanorRomance.DualStory]::CreateScene($id);Assert ($null -ne $c) "Missing dual scene $id"
    Use-Client $c;Finish-Dialogue $c.mainDialogue
    if($choice -ge 0){Finish-Dialogue (Pick $c.mainDialogue $choice)}
    return $c
}
Dual-Run
$c=Dual-Finish 'entry' 0
Assert ([EleanorRomance.DualState]::Done('entry') -and ![EleanorRomance.DualState]::Done('dr02')) 'Entry consumed the separate reward scene.'
Assert (![EleanorRomance.DualProgression]::Passed(1) -and ![EleanorRomance.DualState]::PlaqueReceived) 'Entry below 30 granted progression/reward.'
Assert ([EleanorRomance.DualDirector]::SuppressMainChapter(19) -and [EleanorRomance.DualDirector]::SuppressMainChapter(20)) 'Old chapter can bypass the checkpoint.'
foreach($stage in 1..3){
    if($stage -gt 1){[EleanorRomance.DualState]::Flag($(if($stage -eq 2){'scene.dr08'}else{'scene.dr11'}),$true);[EleanorRomance.RomanceState]::Romance=1;[EleanorRomance.DualState]::CoreCommitted=$true}
    Set-Reps ($stage*30-1) 0;[EleanorRomance.DualProgression]::Check()
    Assert (![EleanorRomance.DualProgression]::Passed($stage)) "Stage $stage passed below its threshold."
    Set-Reps ($stage*30) 0;[EleanorRomance.DualProgression]::Check()
    Assert ([EleanorRomance.DualProgression]::Passed($stage) -and [EleanorRomance.DualProgression]::Earned($stage)) "Stage $stage did not latch."
    Set-Reps 0 0;[EleanorRomance.DualProgression]::Check()
    Assert ([EleanorRomance.DualProgression]::Passed($stage)) 'Reputation decrease revoked progress.'
}
Dual-Run 90
[EleanorRomance.DualProgression]::Check()
Assert (![EleanorRomance.DualProgression]::Passed(1)) 'Early reputation peak passed inactive checkpoint.'
$null=Dual-Finish 'entry' 0
Assert ([EleanorRomance.DualProgression]::Passed(1) -and ![EleanorRomance.DualProgression]::Passed(2)) 'Entry granted every checkpoint together.'

foreach($stage in 1..3){
    Dual-Run;[EleanorRomance.DualState]::Accept(1);[EleanorRomance.DualState]::Flag('scene.entry',$true)
    if($stage -gt 1){[EleanorRomance.RomanceState]::Romance=1;[EleanorRomance.DualState]::Flag('scene.dr08',$true);[EleanorRomance.DualState]::Flag('gate.1.passed',$true)}
    if($stage -eq 3){[EleanorRomance.DualState]::Flag('scene.dr11',$true);[EleanorRomance.DualState]::CoreCommitted=$true;[EleanorRomance.DualState]::Flag('gate.2.passed',$true)}
    foreach($offset in @(0,4,8)){
        $station.dayCounter=70+$offset;$mainDay=[EleanorRomance.DualState]::LastImportantDay
        $id=[EleanorRomance.DualDaily]::Next($station.dayCounter,$stage);$null=Dual-Finish $id
        Assert ([EleanorRomance.DualState]::LastImportantDay -eq $mainDay -and [EleanorRomance.DualState]::PendingId -eq '') 'Waiting daily consumed main progress/pending slot.'
        Assert ([EleanorRomance.DualDaily]::Next($station.dayCounter+1,$stage) -eq $null) 'Stage daily ignored cooldown.'
    }
}
'PASS: explicit entry, 29/30 59/60 89/90 boundaries, no spending/regression, stage activation, main suppression and six rotating wait dailies.'

# Migrate automatic dr05 separately from real consent, preserve future schemas.
Dual-Run;[EleanorRomance.RomanceState]::Romance=1
[ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',1)
[EleanorRomance.DualState]::Entry=2;[EleanorRomance.DualState]::Flag('scene.dr05',$true)
[EleanorRomance.DualMigration]::OnLoaded()
Assert (![EleanorRomance.DualState]::HasHistory -and ![EleanorRomance.DualState]::Done('dr05') -and [EleanorRomance.RomanceState]::Romance -eq 1) 'Automatic initialization was treated as consent or lost romance.'
$null=Dual-Finish 'entry' 1
Assert (![EleanorRomance.DualState]::HasHistory -and [EleanorRomance.RomanceState]::Romance -eq 1) 'Declining compatibility confirmation revoked ordinary lover.'
Dual-Run;[EleanorRomance.DualState]::Accept(1);[EleanorRomance.DualState]::Flag('scene.dr09',$true)
[ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',1)
[EleanorRomance.DualMigration]::OnLoaded()
Assert ([EleanorRomance.DualProgression]::Passed(2) -and ![EleanorRomance.DualProgression]::Earned(2)) 'Historical waiver gave a new ornament without reputation.'
$before=[ModHelper]::GetModDataKeys('eleanor_romance').Count
[ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',99)
[EleanorRomance.DualMigration]::OnLoaded();[EleanorRomance.DualState]::Accept(1);[EleanorRomance.DualProgression]::Check()
Assert ([EleanorRomance.DualState]::FeatureVersion -eq 99 -and [ModHelper]::GetModDataKeys('eleanor_romance').Count -eq $before) 'Future schema was modified.'
[EleanorRomance.DualState]::Reset()
Assert (![EleanorRomance.DualState]::Done('dr09') -and [EleanorRomance.DualState]::PendingId -eq '') 'F9 reset retained scene/visit keys.'
Dual-Run;[EleanorRomance.RomanceState]::FairDeals=0
Assert (![EleanorRomance.DualDirector]::CanOfferEntry(49)) 'Entry promised romance despite missing original qualification.'
'PASS: consent evidence, single compatibility choice, historical gate waiver without new rewards, future-schema read-only protection, complete reset and early qualification.'

# Full delayed route: real sale, exact core facts and ordinary fallback.
foreach($early in @($false,$true)){
    Dual-Run 150;[EleanorRomance.RomanceState]::EarlyStart=$early
    $null=Dual-Finish 'entry' 0
    $station.dayCounter++
    $null=Dual-Finish 'dr02'
    $station.dayCounter++
    $c=[EleanorRomance.DualStory]::CreateScene('dr03');Use-Client $c
    Assert ($c.clientIntent -eq [StoreClient+ClientIntent]::SELL) 'dr03 is not a real seller.'
    Finish-Dialogue $c.mainDialogue
    $saleItem=New-Item '1' 1;$saleItem.unitValue=100;$c.onItemSold.Invoke($saleItem,100)
    $store.TryPlayerDismissCurrentClient($true);Finish-Dialogue $ui.currentDialog
    Assert ([EleanorRomance.StoryCoreFacts]::Done(19) -and [EleanorRomance.DualState]::Done('dr03')) 'Real sale closure did not commit both histories.'
    foreach($id in @('dr04','dr05','dr06','dr07','dr08','dr09','dr10','coop')){
        $station.dayCounter++
        if($id -eq 'dr05'){$null=Dual-Finish $id 0}else{$c=Dual-Finish $id}
        if($id -eq 'dr08' -and $ui.currentDialog -and (Choice-Node $ui.currentDialog)){
            Finish-Dialogue (Pick $ui.currentDialog 1)
        }
    }
    Assert ([EleanorRomance.StoryCoreFacts]::Done(20) -and [EleanorRomance.StoryCoreFacts]::Done(22) -and [EleanorRomance.StoryCoreFacts]::Done(23)) 'Replacement node ledger incomplete.'
    Assert ([EleanorRomance.StoryCoreFacts]::CooperationDone -and ![EleanorRomance.StoryCoreFacts]::CooperationClosed) 'Cooperation intro faked its buyer settlement.'
    $station.dayCounter++;$c=[EleanorRomance.EleanorStory]::CreateClient(21)
    # Close the actual shared cooperation buyer through its native wrapper.
    $factory=[EleanorRomance.EleanorStory].GetMethod('CreateCooperationBuyer',[Reflection.BindingFlags]'NonPublic,Static')
    $buyer=$factory.Invoke($null,@());Use-Client $buyer;$store.TryPlayerDismissCurrentClient($true);Finish-Dialogue $ui.currentDialog
    Assert ([EleanorRomance.StoryCoreFacts]::CooperationClosed) 'Cooperation buyer did not settle.'
    $station.dayCounter++;$c=Dual-Finish 'dr11'
    Finish-Dialogue (Pick $c.mainDialogue 0)
    Assert ([EleanorRomance.DualState]::CoreCommitted -and [EleanorRomance.RomanceState]::PerfumeGiven) 'Core completed without real reward path.'
    foreach($id in @('dr13','dr14','dr15','dr16','dr17','dr18','dr19','dr20')){
        $station.dayCounter++
        if($id -in @('dr14','dr15','dr17','dr18')){$null=Dual-Finish $id 0}else{$null=Dual-Finish $id}
    }
    Assert ([EleanorRomance.DualState]::Confirmed -and [EleanorRomance.DualState]::Done('dr20') -and ![EleanorRomance.ScavAssistance]::Eligible) 'Confirmation/final reward source incorrect.'
    Assert ([EleanorRomance.DualOutcomes]::Ending('dead').Contains('dr_epilogue_death')) 'Actual death ending suffix missing.'
}
Dual-Run 50;[EleanorRomance.DualState]::Accept(1)
$null=Dual-Finish 'dr05' 1
Assert ([EleanorRomance.RomanceState]::Romance -eq 3 -and ![EleanorRomance.DualState]::Active -and ![EleanorRomance.RomanceState]::RomanceLocked) 'Friends were locked or kept in dual route.'
'PASS: real SELL closure, replacement fact ledger, separate cooperation settlement, shared locket/perfume path, both start modes, confirmation, ending suffix and friend fallback.'

# Drive the actual queue while waiting, rather than only constructing scenes.
Dual-Run 0;$null=Dual-Finish 'entry' 0
$station.dayCounter=70;[EleanorRomance.DualState]::LastDailyDay=70
[EleanorRomance.RomanceState]::LastQueuedDay=-1;$store.currentClientInstance=$null
$manager=$store.storeClientManager
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 0 -and [EleanorRomance.RomanceState]::LastQueuedDay -eq -1 -and [EleanorRomance.DualState]::PendingId -eq '') 'Waiting reserved a native customer slot or bypassed the gate.'
Set-Reps 30 0
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 1 -and [EleanorRomance.DualState]::PendingId -eq 'dr02') 'Reached checkpoint did not schedule its actual reward scene.'
# No surviving native client: the same scene resumes without repeat consent.
$manager.clientStack.Clear();[EleanorRomance.DualDirector]::RecoverPending($manager)
Assert ([EleanorRomance.DualState]::PendingId -eq '' -and [EleanorRomance.RomanceState]::LastQueuedDay -eq -1) 'Lost queued scene kept its stale same-day occupancy.'
# Unreadable reputation is not interpreted as a real zero.
$upper=[StoreReputation]::GetStoreReputation('FACTION_UPPER_LEVEL');$store.storeReputations.Remove($upper)|Out-Null
$rep=123;Assert (![EleanorRomance.DualProgression]::TryReputation([ref]$rep)) 'Missing faction was treated as a real reputation value.'
$store.storeReputations.Add($upper)
# Original deferred endings resolve explicitly, including already-completed old cores.
foreach($chapter in @(24,300)){
    Dual-Run;[EleanorRomance.RomanceState]::Chapter=$chapter;[EleanorRomance.RomanceState]::PendingChapter=0
    [EleanorRomance.RomanceState]::Romance=2;[EleanorRomance.DualState]::Entry=3
    $factory=[EleanorRomance.EleanorStory].GetMethod('CreateDeferredResolution',[Reflection.BindingFlags]'NonPublic,Static')
    $c=$factory.Invoke($null,@());Use-Client $c;Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.RomanceState]::Romance -eq 2) 'Deferred migration made a choice before the player.'
    Finish-Dialogue (Pick $c.mainDialogue 0)
    Assert ([EleanorRomance.RomanceState]::Romance -eq 1 -and [EleanorRomance.RomanceState]::Chapter -eq $chapter) 'Deferred choice replayed or reset old world progress.'
}
# Route-B receipt is submitted only after its actual text, not the keep-gift branch.
Dual-Run 150;[EleanorRomance.DualState]::Accept(1)
[EleanorRomance.RomanceState]::Route=2;[EleanorRomance.RomanceState]::Romance=1
[EleanorRomance.RomanceState]::ProtectionReady=$true;[EleanorRomance.RomanceState]::DriveState=4
$c=Dual-Finish 'dr11';Finish-Dialogue (Pick $c.mainDialogue 1)
Assert ([EleanorRomance.MaidWorldEvents]::NeedsReceipt -and ![EleanorRomance.DualProgression]::Passed(3)) 'Unplayed receipt was submitted on the keep-gift branch.'
$station.dayCounter++;$null=Dual-Finish 'claim'
Assert (![EleanorRomance.MaidWorldEvents]::NeedsReceipt -and [EleanorRomance.RomanceState]::PerfumeGiven) 'Technical claim failed to finish actual receipt/perfume facts.'
'PASS: actual generator wait/priority, lost-client rescheduling, unavailable reputation, ordinary legacy deferred resolution and real route-B receipt continuation.'

foreach($chapter in @(21,300)){
    Dual-Run;[EleanorRomance.DualState]::Accept(1)
    [ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',1)
    [EleanorRomance.RomanceState]::Chapter=$chapter
    [ModHelper]::SetModDataBool('eleanor_romance','er.core.initialized',$false)
    [EleanorRomance.StoryCoreFacts]::Migrate()
    Assert (![EleanorRomance.StoryCoreFacts]::CooperationDone -and ![EleanorRomance.StoryCoreFacts]::CooperationClosed) 'Old pending/jumped chapter fabricated cooperation facts.'
    Assert ([EleanorRomance.DualState]::Flag('migration.coop_waived') -eq ($chapter -eq 300)) 'Historical continuation waiver was confused with actual completion.'
}
'PASS: old pending c21 and v0.81 skipped-core saves distinguish actual cooperation from historical route waiver.'

Dual-Run;[EleanorRomance.DualState]::Accept(1)
[EleanorRomance.RomanceState]::Romance=1;[EleanorRomance.RomanceState]::Chapter=300
[EleanorRomance.RomanceState]::WorkState=1;[EleanorRomance.RomanceState]::LocketWithEleanor=$true
[EleanorRomance.DualState]::Flag('scene.dr08',$true)
[ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',1)
[ModHelper]::SetModDataBool('eleanor_romance','er.core.initialized',$false)
[EleanorRomance.StoryCoreFacts]::Migrate();[EleanorRomance.DualMigration]::OnLoaded()
Assert ([EleanorRomance.StoryCoreFacts]::PerfumePending -and [EleanorRomance.StoryCoreFacts]::LocketPending) 'Old skipped gifts had no recovery claim.'
$c=Dual-Finish 'claim';Finish-Dialogue (Pick $c.mainDialogue 1)
Assert ([EleanorRomance.RomanceState]::PerfumeGiven -and [EleanorRomance.StoryCoreFacts]::LocketRefused -and ![EleanorRomance.StoryCoreFacts]::LocketPending) 'Old gifts replayed work or lost locket refusal state.'
'PASS: old replaced work/core with unclaimed locket/perfume recovers rewards without replaying work, with real guarded claim/refusal.'

Dual-Run;[EleanorRomance.DualState]::Accept(2)
[EleanorRomance.RomanceState]::Romance=1;[EleanorRomance.RomanceState]::Chapter=300
[EleanorRomance.RomanceState]::WorkState=1;[EleanorRomance.RomanceState]::LocketWithEleanor=$true
[EleanorRomance.DualState]::Flag('scene.dr08',$true)
[ModHelper]::SetModDataInt('eleanor_romance','er.dual.version',1)
[ModHelper]::SetModDataBool('eleanor_romance','er.core.initialized',$false)
[EleanorRomance.StoryCoreFacts]::Migrate()
Assert (![EleanorRomance.StoryCoreFacts]::LocketPending) 'Unknown old-lover refusal was treated as a never-offered locket.'

# Select mother facts independently from new/already-established employment.
foreach($work in @(0,1)){
    foreach($case in @(@(1,0,'dr08_mother_a'),@(1,3,'dr08_mother_a'),@(2,0,'dr08_mother_pending'),@(2,3,'dr08_mother_cleared'))){
        Dual-Run 150
        [EleanorRomance.DualState]::Accept(1)
        [EleanorRomance.RomanceState]::WorkState=$work
        [EleanorRomance.RomanceState]::Route=$case[0]
        [EleanorRomance.MaidWorldEvents]::State=$case[1]
        $c=[EleanorRomance.DualStory]::CreateScene('dr08')
        $actual=@((Motivation-Nodes $c.mainDialogue) | Where-Object {$_.plainText -like 'er_dual_dr08_mother_*'})
        Assert ($actual.Count -eq 1 -and $actual[0].plainText -eq "er_dual_$($case[2])_01") "Wrong mother facts for route $($case[0]), review $($case[1]), work $work."
    }
}
'PASS: route-A exile and route-B pending/withdrawn mother dialogue stay distinct for both new and existing employment.'

# The shortened discussion still waits for both independent acceptance lines.
Dual-Run 150;[EleanorRomance.DualState]::Accept(1)
[EleanorRomance.RomanceState]::Romance=1
$c=[EleanorRomance.DualStory]::CreateScene('dr17');Use-Client $c
Finish-Dialogue $c.mainDialogue
Assert (![EleanorRomance.DualState]::Confirmed) 'Opening discussion committed the relationship.'
$accept=Pick $c.mainDialogue 0
Assert (![EleanorRomance.DualState]::Confirmed) 'Selecting confirmation skipped the independent answers.'
$node=$accept
if($node.title -eq [EleanorRomance.L]::Player){$node.ExecuteEndAction();$node=$node.nextDialogue}
foreach($speaker in @([EleanorRomance.L]::Eleanor,[EleanorRomance.L]::Get('er_name_lydia'))){
    Assert ($node.title -eq $speaker) 'Missing independent acceptance speaker.'
    $node.ExecuteEndAction();$node=$node.nextDialogue
    Assert (![EleanorRomance.DualState]::Confirmed) 'Relationship committed before the acceptance scene finished.'
}
Finish-Dialogue $node
Assert ([EleanorRomance.DualState]::Confirmed) 'Acceptance scene no longer commits the relationship.'
'PASS: shortened discussion and the confirmation choice preserve two independent answers before relationship commit.'
