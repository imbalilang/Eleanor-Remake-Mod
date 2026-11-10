# Ordinary-dialogue extensions against the installed native assembly. Only
# native presentation is replaced; eligibility, graph actions and saved state run.
function Gala-Run($day=80){
    Dividend-Run 28 $day
    [EleanorRomance.MaidInteractions]::OnLoaded()
    [EleanorRomance.GalaStory]::Phase=0
}
function Gala-Client {
    [EleanorRomance.MaidState]::PendingVisit=916
    $c=[EleanorRomance.GalaStory]::CreateVisit();Use-Client $c;return $c
}

# A: chapter 19 is ordinary dialogue; retired appraisal records have no effect.
foreach($legacy in @($false,$true)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.MaidState]::AddressAccepted=$legacy
    if($legacy){
        [EleanorRomance.MaidInteractions]::Flag('lesson.opened',$true)
        [EleanorRomance.MaidInteractions]::Set('lesson.result',1)
        [EleanorRomance.MaidState]::Flag('biz.fee.er_story_maid_m19',$true)
    }
    $script:businessStock.Clear();$cash=$store.playerCash;$c=Main-Client 20
    Assert ($c.clientIntent -eq [StoreClient+ClientIntent]::DIALOGUE -and
        $null -eq $c.tryInterceptDismiss -and $null -eq $c.allDoneDialogue) 'Chapter 19 still waits for an appraisal or bell.'
    Assert ((Has-MotivationBlock $c.mainDialogue 'm19_b005') -and
        (Has-MotivationBlock $c.mainDialogue $(if($legacy){'m19_b007'}else{'m19_b006'}))) 'Direct relationship continuation lost its address variant.'
    Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.RomanceState]::Chapter -eq 21 -and
        [EleanorRomance.MaidState]::AddressAccepted -and [EleanorRomance.MaidState]::DebutShown -and
        $script:businessStock.Count -eq 0 -and $store.playerCash -eq $cash) 'Direct dialogue did not complete without appraisal stock/fees.'
    Set-Partner;[EleanorRomance.MaidState]::BasicComplete=$true
    [EleanorRomance.MaidState]::Flag('p3.paid',$true)
    foreach($claimed in @($false,$true)){
        [EleanorRomance.MaidState]::Flag('biz.sample.claimed',$claimed)
        [EleanorRomance.MaidState]::PendingVisit=25
        $c=[EleanorRomance.MaidStory]::CreateEvent(25);Use-Client $c
        Finish-Dialogue $c.mainDialogue;Leave-Trade $c
        Assert (!(Has-MotivationBlock $ui.currentDialog 'interaction_b004') -and
            !(Has-MotivationBlock $ui.currentDialog 'interaction_b005') -and
            [EleanorRomance.MaidState]::Progress -eq 25) 'Sample wrap-up retained a retired appraisal memory or blocked progress.'
    }
}
foreach($choice in 0..1){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
    $c=Main-Client 20;Finish-Dialogue (Pick $c.mainDialogue $choice)
    Assert ([EleanorRomance.MaidState]::Flag('reconciled') -and
        [EleanorRomance.RomanceState]::Chapter -eq 21) 'Removing appraisal lost an existing reconciliation choice.'
}
'PASS: direct chapter 19 dialogue without appraisal/bell/fees, both address variants, both reconciliation choices and no retired sample memories.'

# B: MotherState is deliberately zero before this chapter's final action.
foreach($route in 1..2){foreach($early in @($false,$true)){
    New-MaidRun 17;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.RomanceState]::Chapter=17;[EleanorRomance.RomanceState]::PendingChapter=17
    [EleanorRomance.RomanceState]::EarlyStart=$early
    $station.dayCounter=$(if($early){29}else{40})
    [EleanorRomance.RomanceState]::NextVisitDay=$station.dayCounter
    [EleanorRomance.RomanceState]::Route=$route;[EleanorRomance.RomanceState]::MotherState=0
    [EleanorRomance.RomanceState]::ProtectionReady=($route -eq 2)
    if($early){$store.startingPerks.Add([StartingPerk]@{id='eleanor_romance.early_start'})}
    $manager=$store.storeClientManager;$store.currentClientInstance=$null
    $ordinary=[EleanorRomance.EleanorStory]::CreateClient(17);$manager.clientStack.Add($ordinary)
    [EleanorRomance.MaidInteractions]::TestMorningPresentation=[Action[object]]{param($x) Use-Client $x}
    [EleanorRomance.MaidInteractions]::ResolveMorning()
    $c=$store.currentClientInstance.GetClientBlueprint()
    Assert ($manager.clientStack.Count -eq 0 -and (Has-MotivationBlock $c.mainDialogue $(if($route -eq 2){'interaction_b008'}else{'interaction_b007'}))) 'Morning promotion missed its route or kept a duplicate visitor.'
    [EleanorRomance.MaidInteractions]::ResolveMorning()
    Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.RomanceState]::MotherState -eq 1 -and ![EleanorRomance.MaidInteractions]::MorningEntry) 'Morning chapter did not commit the original rescue result.'
}}
foreach($blocked in @('current','urgent','presentation')){
    New-MaidRun 17;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.RomanceState]::Chapter=17;[EleanorRomance.RomanceState]::PendingChapter=17
    [EleanorRomance.RomanceState]::NextVisitDay=40;[EleanorRomance.RomanceState]::Route=1
    $manager=$store.storeClientManager;$store.currentClientInstance=$null
    $ordinary=[EleanorRomance.EleanorStory]::CreateClient(17);$manager.clientStack.Add($ordinary)
    if($blocked -eq 'current'){$native=[StoreClient]::new();$native.identifier='native';Use-Client $native}
    if($blocked -eq 'urgent'){$native=[StoreClient]::new();$native.identifier='native';$native.isSentenceOfficer=$true;$manager.clientStack.Add($native)}
    [EleanorRomance.MaidInteractions]::TestMorningPresentation=[Action[object]]{param($x) throw 'simulated native presentation failure'}
    [EleanorRomance.MaidInteractions]::ResolveMorning()
    Assert ($manager.clientStack.Contains($ordinary) -and ![EleanorRomance.MaidInteractions]::MorningEntry) 'Morning failure/priority lost the ordinary fallback.'
}
'PASS: both morning routes, unfinished mother flag, early-start scheduling, native priority, single promotion and presentation-failure fallback.'

# C: actual result + two days, exact-line commit and existing carrier settlement.
Dividend-Run 25
[EleanorRomance.MaidInteractions]::OnLoaded()
$cash=$store.playerCash
$c=[EleanorRomance.MaidDividends]::CreateVisit()
Assert (!(Has-MotivationBlock $c.mainDialogue 'interaction_b006')) 'Old save immediately replayed the customer news.'
$station.dayCounter=62;[EleanorRomance.MaidState]::PendingVisit=915
$c=[EleanorRomance.MaidDividends]::CreateVisit();Use-Client $c
Assert ((Has-MotivationBlock $c.mainDialogue 'interaction_b006') -and ![EleanorRomance.MaidInteractions]::Flag('customer.shown')) 'Customer echo committed during visitor construction.'
Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.MaidInteractions]::Flag('customer.shown') -and $store.playerCash -eq $cash+400 -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Customer echo interrupted the existing dividend settlement or added a reward.'
$c=[EleanorRomance.MaidDividends]::CreateVisit()
Assert (!(Has-MotivationBlock $c.mainDialogue 'interaction_b006')) 'Customer echo replayed.'
'PASS: customer-news result/two-day gates, conservative old-save start, exact-line dedupe and unchanged host dividend payment.'

. (Join-Path $PSScriptRoot 'test_gala_closing_routes.ps1')
