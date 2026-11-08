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

# A: native estimate/retry/walkout and the common sample close on both paths.
foreach($success in @($true,$false)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    $script:businessStock.Clear();$c=Main-Client 20
    Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.MaidInteractions]::Flag('lesson.opened')) 'Actual lesson intro did not open its record.'
    $cash=$store.playerCash
    if($success){[AppraisalServiceHelper]::OnEstimate($script:businessStock[0],100)}
    else{
        [AppraisalServiceHelper]::OnEstimate($script:businessStock[0],1000)
        Assert ([EleanorRomance.MaidInteractions]::Number('lesson.result') -eq 0) 'A retry was treated as a final lesson result.'
        $store.TryPlayerDismissCurrentClient($true)
    }
    Finish-Dialogue $ui.currentDialog
    $result=if($success){1}else{2}
    Assert ([EleanorRomance.MaidInteractions]::Number('lesson.result') -eq $result -and
        [EleanorRomance.RomanceState]::Chapter -eq 21) 'Lesson feedback failed to rejoin the original chapter.'
    Assert ($store.playerCash -eq $cash+$(if($success){35}else{0})) 'Lesson changed native fee arithmetic.'
    foreach($claimed in @($false,$true)){
        Set-Partner;[EleanorRomance.MaidState]::BasicComplete=$true
        [EleanorRomance.MaidState]::Flag('p3.paid',$true)
        [EleanorRomance.MaidInteractions]::Flag('lesson.echo',$false)
        [EleanorRomance.MaidState]::Flag('biz.sample.claimed',$claimed)
        [EleanorRomance.MaidState]::PendingVisit=25
        $c=[EleanorRomance.MaidStory]::CreateEvent(25);Use-Client $c
        Finish-Dialogue $c.mainDialogue;Leave-Trade $c
        Assert ([EleanorRomance.MaidInteractions]::Flag('lesson.echo') -and [EleanorRomance.MaidState]::Progress -eq 25) "Sample claimed=$claimed lost the lesson echo."
    }
}
Dividend-Run 25
$old=[EleanorRomance.StoryEngine]::Text('test','closing');[EleanorRomance.MaidInteractions]::AppendLessonEcho($old)
Assert ($old.nextDialogue -eq $null) 'An old save invented a past lesson.'
'PASS: lesson final outcomes, native retry/fee, chapter continuation and sample-claimed/unclaimed echoes without invented old history.'

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

# D: no date/resource change; invitation and party each resume at a read cursor.
Gala-Run
Assert (![EleanorRomance.GalaStory]::Due(82) -and [EleanorRomance.GalaStory]::Due(83)) 'Old completed save did not receive a three-day invitation gap.'
$station.dayCounter=83;$c=Gala-Client;$cash=$store.playerCash;$day=$station.dayCounter
Assert (!(Has-MotivationBlock $c.mainDialogue 'interaction_b006')) 'Gala acquired unrelated business dialogue.'
$c.mainDialogue.startAction.Invoke();$c.mainDialogue.ExecuteEndAction()
$c=Gala-Client
Assert ($c.mainDialogue.plainText -eq [EleanorRomance.MaidContent]::Get('gala_b001')[1].Key -and
    [EleanorRomance.MaidState]::Int('gala.invite_line') -eq 1 -and [EleanorRomance.GalaStory]::Attempts -eq 1) 'Invitation restart lost its read cursor.'
Finish-Dialogue $c.mainDialogue
$accept=Pick $c.mainDialogue 0
# Acceptance has five lines; stop two lines into the party, then rebuild.
$node=$accept
while($node -and $node.plainText -notlike '*gala_b004*'){$node.ExecuteEndAction();$node=$node.nextDialogue}
Assert ($node -ne $null) 'Accepted invitation did not reach the party.'
$node.ExecuteEndAction();$node=$node.nextDialogue;$node.ExecuteEndAction()
$cursor=[EleanorRomance.MaidState]::Int('gala.ball_line')
$c=Gala-Client
Assert ($cursor -eq 2 -and [EleanorRomance.GalaStory]::Phase -eq 4) 'Party interruption was not saved.'
Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.GalaStory]::Phase -eq 5 -and $store.playerCash -eq $cash -and $station.dayCounter -eq $day -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Narrated party changed native cash/day or failed completion.'
$events=[StoreEventManager]::new();$station.storeEventManager=$events
[EleanorRomance.GalaStory]::TickNews($events)
Assert (![bool]$events.IsEventActive([EleanorRomance.GalaStory]::NewsId)) 'Party news appeared on the same game day.'
$station.dayCounter++
$urgent=[StoreEvent]::new();$urgent.identifier='native_priority';$urgent.importance=9;$urgent.duration=2
$events.ActivateEvent($urgent,$false,$false)
[EleanorRomance.GalaStory]::TickNews($events)
Assert ([EleanorRomance.GalaStory]::Phase -eq 5) 'Gala news displaced important native news.'
$events=[StoreEventManager]::new();$station.storeEventManager=$events
[EleanorRomance.MaidWorldEvents]::State=1
[EleanorRomance.MaidState]::SetInt('world.due_day',$station.dayCounter)
[EleanorRomance.GalaStory]::TickNews($events)
Assert ([EleanorRomance.GalaStory]::Phase -eq 5) 'Gala news raced ahead of a due mother-review callback.'
[EleanorRomance.MaidWorldEvents]::State=0
[EleanorRomance.GalaStory]::TickNews($events);[EleanorRomance.GalaStory]::TickNews($events)
Assert ([EleanorRomance.GalaStory]::Phase -eq 6 -and @($events.GetActiveEvents() | Where-Object identifier -eq 'er_gala_public_partner').Count -eq 1) 'Next-day native news failed or duplicated.'
$c=[EleanorRomance.MaidDividends]::CreateVisit();Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.MaidState]::Flag('gala.reply')) 'News reply did not attach to an existing safe visitor.'
'PASS: gala three-day gate, invitation/party read cursors, no cash/day changes, next-day news priority/deduplication and existing-visit reply.'

foreach($book in @($true,$false)){
    Gala-Run;$station.dayCounter=83;$c=Gala-Client;Finish-Dialogue $c.mainDialogue
    Finish-Dialogue (Pick $c.mainDialogue 1)
    Assert ([EleanorRomance.GalaStory]::Phase -eq 2 -and [EleanorRomance.GalaStory]::Attempts -eq 1) 'First delay failed.'
    $station.dayCounter=89;$c=[EleanorRomance.MaidDividends]::CreateVisit()
    Assert (!(Has-MotivationBlock $c.mainDialogue 'gala_b007')) 'Second invitation appeared before seven days.'
    $station.dayCounter=90;[EleanorRomance.MaidState]::PendingVisit=915
    $c=[EleanorRomance.MaidDividends]::CreateVisit();Use-Client $c
    Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.GalaStory]::Phase -eq 9 -and [EleanorRomance.GalaStory]::Attempts -eq 2 -and [EleanorRomance.MaidState]::PendingVisit -eq 915) 'Second invitation bypassed the choice or prematurely completed its host.'
    Finish-Dialogue (Pick $c.mainDialogue $(if($book){0}else{1}))
    Assert ([EleanorRomance.MaidState]::PendingVisit -eq 0) 'Second invitation lost its host completion.'
    if($book){
        Assert (![EleanorRomance.GalaStory]::Due(90) -and [EleanorRomance.GalaStory]::Due(91)) 'Booked visit was not due tomorrow.'
        $station.dayCounter=93;$c=Gala-Client
        Assert ((Has-MotivationBlock $c.mainDialogue 'gala_b009')) 'Delayed appointment did not acknowledge a different party.'
        Finish-Dialogue $c.mainDialogue;Finish-Dialogue (Pick $c.mainDialogue 1)
    }
    $station.dayCounter++
    $c=[EleanorRomance.MaidDividends]::CreateVisit();Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.GalaStory]::Phase -eq 8 -and [EleanorRomance.GalaStory]::Attempts -eq 2 -and ![EleanorRomance.GalaStory]::Due(200)) 'Two declined invitations did not close with the solo reply.'
}
'PASS: seven-day second invitation, no default choice bypass, host completion, next-day booking, missed-party confirmation and maximum-two-invitation closure.'

# Trade completion owns a shared closing node beyond the invitation choice.
Gala-Run;$station.dayCounter=90;[EleanorRomance.GalaStory]::Phase=2
[EleanorRomance.MaidState]::SetInt('gala.attempts',1)
[EleanorRomance.MaidState]::SetInt('gala.retry_day',90)
$null=[EleanorRomance.MaidBusiness]::AcceptOrder();$c=Biz-Client 910
$store.TryPlayerDismissCurrentClient($true)
Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 910 -and [EleanorRomance.GalaStory]::Phase -eq 9) 'Business host finished on the invitation prompt.'
Finish-Dialogue (Pick $ui.currentDialog 0)
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 0 -and [EleanorRomance.GalaStory]::Phase -eq 3) 'Business host did not finish after the invitation answer.'
'PASS: native trade-dismiss wrapper preserves second-invitation choices and settles its shared host closing only after the answer.'

# Two portrait modes, plus hot-disable/missing-art and unrelated-scene fallback.
Gala-Run;[EleanorRomance.MaidState]::PendingVisit=916
$own.Sprites[[EleanorRomance.PortraitCompat]::GalaKey]=$sprite
$beauty.IsEnabled=$false;[EleanorRomance.PortraitCompat]::Refresh()
Assert ((Gala-Client).spriteName -eq 'eleanor1') 'Vanilla gala did not reuse the original Upper Level Eleanor.'
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh()
$station.dayCounter=83;[EleanorRomance.MaidState]::PendingVisit=0;$store.storeClientManager.clientStack.Clear()
Assert ([EleanorRomance.MaidStory]::TryQueue($store.storeClientManager,83) -and $store.storeClientManager.clientStack[0].spriteName -eq [EleanorRomance.PortraitCompat]::GalaKey) 'Real queue factory required its not-yet-written pending visit to select the gown.'
$c=Gala-Client
Assert ($c.spriteName -eq [EleanorRomance.PortraitCompat]::GalaKey) 'Beauty gala did not select its local gown.'
$beauty.IsEnabled=$false;[EleanorRomance.PortraitCompat]::Refresh();[EleanorRomance.PortraitCompat]::SanitizeLoadedClients()
Assert ($c.spriteName -eq 'eleanor1') 'Disabling beauty left a stale gown key in the current visitor.'
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh();$own.Sprites.Remove([EleanorRomance.PortraitCompat]::GalaKey) | Out-Null
Assert ((Gala-Client).spriteName -eq 'eleanor1') 'Missing gown failed its native fallback.'
Assert ([EleanorRomance.PortraitCompat]::ResolveGala('foreign_client') -eq 'eleanor1') 'Gown escaped its scene.'
[EleanorRomance.RomanceState]::MotherState=0
Assert (![EleanorRomance.GalaStory]::Eligible -and [EleanorRomance.GalaStory]::CreateVisit() -eq $null) 'Gala ignored its living-mother relationship gate.'
'PASS: native/beauty gown selection, disabled/missing-art fallback, scene isolation and relationship eligibility.'
