# Loaded by test_runtime_flows.ps1 -Maid against installed native assemblies.
function Closing-Run($day=83){
    Gala-Run
    $station.dayCounter=$day
    [EleanorRomance.GalaClosing]::Enabled=$true;[EleanorRomance.GalaClosing]::Reset()
    $store.currentClientInstance=$null;$store.storeClientManager.clientStack.Clear()
    $station.storeEventManager=[StoreEventManager]::new()
    $store.storeState=[PlayerStore+StoreState]::OPEN
    $store.playerLocation=[PlayerStore+PlayerLocation]::IN_STORE
    $store.lockShutterButtonFlag=0;$store.isClientBeingArrested=$false;$store.isHandlingShootout=$false
    $store.lockNextBellFlag=0;$store.callNextWhenPossible=$false
    [StoreShutterButton]::TestInstance=Blank ([StoreShutterButton])
    [EndOfDayUIManager]::TestInstance=Blank ([EndOfDayUIManager])
    [StoreUIManager]::TestInstance=Blank ([StoreUIManager])
    [StoreShutterButton]::TestCloses=0;[EndOfDayUIManager]::TestSleeps=0
    $script:presented=0;$script:sleepQueued=0;$script:arrivalQueued=$null
    [EleanorRomance.GalaClosing]::TestPresentation=[Func[object,bool]]{
        param($client) $script:presented++;Use-Client $client
        $null=$ui.StartDialogue($client.mainDialogue,$true);return $true
    }
    [EleanorRomance.GalaClosing]::TestScheduleSleep=[Action]{$script:sleepQueued++}
    [EleanorRomance.GalaClosing]::TestSchedulePresentation=[Action[object]]{param($client) $script:arrivalQueued=$client}
}
function Closing-Client {
    Assert (![EleanorRomance.GalaClosing]::BeforeClose()) 'Empty-shop close did not display the invitation.'
    return $store.currentClientInstance.GetClientBlueprint()
}
function Sleep-Gala {
    $method=[EleanorRomance.GalaClosing].GetMethod('SleepNative',[Reflection.BindingFlags]'NonPublic,Static')
    $null=$method.Invoke($null,@())
}
function Arrive-Gala($client){
    $method=[EleanorRomance.GalaClosing].GetMethod('ResumePresentation',[Reflection.BindingFlags]'NonPublic,Static')
    $null=$method.Invoke($null,@($client))
}

Closing-Run 82
Assert ([EleanorRomance.GalaClosing]::BeforeClose() -and $script:presented -eq 0) 'Invitation ignored its three-day gap.'
Closing-Run
for($i=0;$i -lt 16;$i++){$native=[StoreClient]::new();$native.identifier='native_'+$i;$native.dismissable=$true;$store.storeClientManager.clientStack.Add($native)}
$queued=$store.storeClientManager.clientStack.ToArray()
Assert ([EleanorRomance.MaidStory]::EventForDay(83) -ne 916) 'Gala still enters the business scheduler.'
Assert ([EleanorRomance.GalaClosing]::BeforeClose() -and $script:presented -eq 0 -and $store.storeClientManager.clientStack.Count -eq 16) 'Invitation interrupted sixteen waiting customers or discarded them.'
foreach($client in $queued){Assert ($store.storeClientManager.clientStack.Contains($client)) 'Closing replaced a native waiting customer.'}
$old=[StoreClient]::new();$old.identifier='er_story_gala';$old.sourceModId='eleanor_romance'
$store.storeClientManager.clientStack.Insert(0,$old);[EleanorRomance.MaidState]::PendingVisit=916
[EleanorRomance.GalaClosing]::RecoverQueuedVisits($store.storeClientManager)
Assert ($store.storeClientManager.clientStack.Count -eq 16 -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Old gala queue migration removed customers or retained 916.'
foreach($blocked in @('current','closed','morning','arrest','shootout','lock','outside','pending','disabled')){
    Closing-Run
    switch($blocked){
        current{$native=[StoreClient]::new();$native.dismissable=$true;Use-Client $native}
        closed{$store.storeState=[PlayerStore+StoreState]::CLOSED}
        morning{$store.storeState=[PlayerStore+StoreState]::MORNING}
        arrest{$store.isClientBeingArrested=$true}
        shootout{$store.isHandlingShootout=$true}
        lock{$store.lockShutterButtonFlag=1}
        outside{$store.playerLocation=[PlayerStore+PlayerLocation]::INVENTOR}
        pending{[EleanorRomance.MaidState]::PendingVisit=915}
        disabled{[EleanorRomance.GalaClosing]::Enabled=$false}
    }
    Assert ([EleanorRomance.GalaClosing]::BeforeClose() -and $script:presented -eq 0) "Closing invitation ignored $blocked boundary."
}
'PASS: gala absent from day queue, sixteen native customers retained, old queue removal, last-customer and close/safety gates.'

Closing-Run;$store.lockNextBellFlag=1;$store.callNextWhenPossible=$true
Assert (![EleanorRomance.GalaClosing]::BeforeClose() -and $script:presented -eq 0 -and $null -ne $script:arrivalQueued -and !$store.callNextWhenPossible) 'Closing replaced the last customer before its departure animation finished.'
$waiting=$script:arrivalQueued;Arrive-Gala $waiting
Assert ($script:presented -eq 0 -and ![EleanorRomance.GalaClosing]::BeforeClose()) 'Waiting for departure displayed or duplicated the invitation.'
$store.lockNextBellFlag=0;Arrive-Gala $waiting;Arrive-Gala $waiting
Assert ($script:presented -eq 1 -and $store.currentClientInstance.GetClientBlueprint().identifier -eq 'er_story_gala') 'Completed departure failed to display exactly one closing visitor.'
# A scheduled callback from a prior save must not affect a later shop session.
Closing-Run;Arrive-Gala $waiting
Assert ($script:presented -eq 0) 'A stale animation callback displayed an old invitation.'
'PASS: last-customer departure finishes before Eleanor appears, single presentation and stale-callback isolation.'

Closing-Run;$cash=$store.playerCash;$c=Closing-Client
Assert ([EleanorRomance.GalaStory]::Attempts -eq 1 -and $store.storeClientManager.clientStack.Count -eq 0) 'Closing presentation used the queue or missed its start.'
Assert (![EleanorRomance.GalaClosing]::BeforeClose() -and $script:presented -eq 1) 'Double close spawned a second invitation.'
$c.mainDialogue.ExecuteEndAction();$store.currentClientInstance=$null;$store.lockShutterButtonFlag=0
[EleanorRomance.GalaClosing]::Reset();[EleanorRomance.GalaClosing]::RecoverQueuedVisits($store.storeClientManager)
$c=Closing-Client
Assert ($c.mainDialogue.plainText -eq [EleanorRomance.MaidContent]::Get('gala_b001')[1].Key) 'Interrupted invitation lost its read cursor.'
Finish-Dialogue $c.mainDialogue
$yes=Pick $c.mainDialogue 0
Assert ([EleanorRomance.GalaStory]::Phase -eq 4 -and [EleanorRomance.MaidState]::Int('gala.departure_day') -eq 83) 'Acceptance was not persisted before departure.'
Assert (!(Has-MotivationBlock $yes 'gala_b004')) 'Acceptance still narrates the party in the shop.'
Finish-Dialogue $yes
Assert ([StoreShutterButton]::TestCloses -eq 1 -and $script:sleepQueued -eq 1 -and $store.visitLeftTonight -eq 0 -and $station.dayCounter -eq 83) 'Departure missed native close, offered an outing or skipped the shutter transition.'
[EleanorRomance.GalaClosing]::Finish($true)
Assert ([StoreShutterButton]::TestCloses -eq 1 -and $script:sleepQueued -eq 1) 'Duplicate completion closed/scheduled twice.'
Sleep-Gala
Assert ([EndOfDayUIManager]::TestSleeps -eq 1 -and $station.dayCounter -eq 84 -and $store.storeState -eq [PlayerStore+StoreState]::MORNING -and $store.playerCash -eq $cash) 'Native overnight boundary did not advance exactly one day.'
Sleep-Gala;[EleanorRomance.GalaClosing]::OnNewDay()
Assert ([EndOfDayUIManager]::TestSleeps -eq 1 -and [EleanorRomance.GalaStory]::Phase -eq 6 -and $station.storeEventManager.IsEventActive([EleanorRomance.GalaStory]::NewsId)) 'News did not appear next morning or sleep repeated.'
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 0 -and ![EleanorRomance.GalaStory]::Due(200)) 'Completed gala invited again.'
'PASS: close-click surprise, duplicate protection, invitation cursor, acceptance persistence, no party graph, native close/sleep once and next-morning news.'

Closing-Run;$c=Closing-Client;Finish-Dialogue $c.mainDialogue
Finish-Dialogue (Pick $c.mainDialogue 1)
Assert ([EleanorRomance.GalaStory]::Phase -eq 7 -and [StoreShutterButton]::TestCloses -eq 1 -and $script:sleepQueued -eq 0 -and $station.dayCounter -eq 83) 'Decline skipped the night instead of closing normally.'
Assert (![EleanorRomance.GalaStory]::Due(200)) 'Declined invitation repeated.'
Closing-Run
[EleanorRomance.GalaClosing]::TestPresentation=[Func[object,bool]]{param($x) throw 'simulated presentation failure'}
Assert ([EleanorRomance.GalaClosing]::BeforeClose() -and $store.currentClientInstance -eq $null -and $store.lockShutterButtonFlag -eq 0 -and [EleanorRomance.MaidState]::PendingVisit -eq 0 -and [EleanorRomance.GalaStory]::Due(83)) 'Presentation failure stranded the close or lost the invitation.'
foreach($phase in @(2,9,3,4)){
    Closing-Run 90
    [EleanorRomance.GalaStory]::Phase=$phase;[EleanorRomance.MaidState]::SetInt('gala.version',1)
    [EleanorRomance.MaidState]::SetInt('gala.retry_day',90);[EleanorRomance.MaidState]::SetInt('gala.booked_day',89)
    $c=Closing-Client;Finish-Dialogue $c.mainDialogue
    if($phase -ne 4){Finish-Dialogue (Pick $c.mainDialogue 0)}
    Sleep-Gala
    Assert ($station.dayCounter -eq 91 -and [EndOfDayUIManager]::TestSleeps -eq 1 -and [EleanorRomance.GalaStory]::Phase -eq 6) "Legacy phase $phase did not finish via closing/next-day news."
}
Closing-Run 84
[EleanorRomance.GalaStory]::Phase=4;[EleanorRomance.MaidState]::SetInt('gala.departure_day',83)
[EleanorRomance.GalaStory]::Initialize()
Assert ([EleanorRomance.GalaStory]::Phase -eq 5 -and ![EleanorRomance.GalaStory]::Due(84)) 'Save after overnight requested a second sleep.'
'PASS: decline keeps normal evening, presentation rollback, legacy postponed/booked/accepted compatibility and saved day-transition dedupe.'

Closing-Run;$c=Closing-Client;$c.mainDialogue.ExecuteEndAction()
$store.lockShutterButtonFlag++
[EleanorRomance.GalaClosing]::OnLoaded()
Assert ($null -eq $store.currentClientInstance -and $null -eq $ui.currentDialog -and $store.lockShutterButtonFlag -eq 1 -and ![EleanorRomance.MaidState]::Flag('gala.close_lock') -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Loading an unfinished invitation retained its presentation or released another shutter lock.'
$store.lockShutterButtonFlag=0;$c=Closing-Client
Assert ($c.mainDialogue.plainText -eq [EleanorRomance.MaidContent]::Get('gala_b001')[1].Key) 'Load recovery lost the invitation cursor.'
Finish-Dialogue $c.mainDialogue;$yes=Pick $c.mainDialogue 0
[EleanorRomance.GalaClosing]::OnLoaded()
$c=Closing-Client
Assert ($c.mainDialogue.plainText -eq [EleanorRomance.MaidContent]::Get('gala_b002')[0].Key) 'Accepted save replayed the invitation or old party graph.'
Finish-Dialogue $c.mainDialogue
# Save after closing but before the blackout: do not close twice, resume sleep.
[EleanorRomance.GalaClosing]::OnLoaded()
Assert ([StoreShutterButton]::TestCloses -eq 1 -and $script:sleepQueued -eq 2 -and $store.visitLeftTonight -eq 0) 'Closed acceptance did not resume the pending overnight transition.'
Sleep-Gala;Sleep-Gala
Assert ($station.dayCounter -eq 84 -and [EndOfDayUIManager]::TestSleeps -eq 1) 'Reloaded acceptance skipped multiple days.'
[EleanorRomance.GalaClosing]::OnLoaded();Sleep-Gala
Assert ([EndOfDayUIManager]::TestSleeps -eq 1 -and [EleanorRomance.GalaStory]::Phase -eq 6) 'Loading next morning slept again.'
'PASS: unfinished invitation/acceptance load recovery, own-lock release only, closed-transition resume and next-morning dedupe.'

foreach($priority in @('urgent','threat','important','mother_due','mother_pending','mother_active')){
    Closing-Run 84;[EleanorRomance.GalaStory]::Phase=5;[EleanorRomance.MaidState]::SetInt('gala.completed_day',83)
    $events=$station.storeEventManager
    if($priority -in @('mother_due','mother_pending')){
        [EleanorRomance.MaidWorldEvents]::State=$(if($priority -eq 'mother_pending'){2}else{1})
        [EleanorRomance.MaidState]::SetInt('world.due_day',84)
    }else{
        $event=[StoreEvent]::new();$event.identifier='test_priority';$event.duration=1
        switch($priority){
            urgent{$event.IsPriority=$true}
            threat{$event.eventType=[StoreEvent+EventType]::THREAT}
            important{$event.importance=8}
            mother_active{$event.identifier=[EleanorRomance.MaidWorldEvents]::NewsId}
        }
        $events.ActivateEvent($event,$false,$false)
    }
    [EleanorRomance.GalaClosing]::OnNewDay()
    Assert ([EleanorRomance.GalaStory]::Phase -eq 5 -and !$events.IsEventActive([EleanorRomance.GalaStory]::NewsId)) "Gala newspaper preempted $priority news."
    $events.RemoveAllEvent('test_priority');$events.RemoveAllEvent([EleanorRomance.MaidWorldEvents]::NewsId)
    [EleanorRomance.MaidWorldEvents]::State=0;$station.dayCounter=85
    [EleanorRomance.GalaClosing]::OnNewDay();[EleanorRomance.GalaStory]::TickNews($events)
    Assert ([EleanorRomance.GalaStory]::Phase -eq 6 -and $events.GetActiveEvents().Count -eq 1) "Delayed gala newspaper failed or duplicated after $priority news cleared."
}
Closing-Run 83;[EleanorRomance.GalaStory]::Phase=5;[EleanorRomance.MaidState]::SetInt('gala.completed_day',83)
[EleanorRomance.GalaStory]::TickNews($station.storeEventManager)
Assert (!$station.storeEventManager.IsEventActive([EleanorRomance.GalaStory]::NewsId)) 'Gala newspaper published on the invitation day.'
'PASS: next-morning publication respects urgent/threat/important and due/pending/active mother news, defers safely and never duplicates.'

Closing-Run;$own.Sprites[[EleanorRomance.PortraitCompat]::GalaKey]=$sprite
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh();$c=Closing-Client
Assert ($c.spriteName -eq [EleanorRomance.PortraitCompat]::GalaKey) 'Closing visitor lost the gown.'
$beauty.IsEnabled=$false;[EleanorRomance.PortraitCompat]::Refresh();[EleanorRomance.PortraitCompat]::SanitizeLoadedClients()
Assert ($c.spriteName -eq 'eleanor1') 'Disabled beauty left a stale gown.'
Closing-Run;$c=Closing-Client
Assert ($c.spriteName -eq 'eleanor1') 'Vanilla close did not use Eleanor original clothes.'
[EleanorRomance.RomanceState]::MotherState=0
Assert (![EleanorRomance.GalaStory]::Eligible) 'Gala lost its safe-mother relationship gate.'
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh();$own.Sprites.Remove([EleanorRomance.PortraitCompat]::GalaKey)|Out-Null
[EleanorRomance.GalaClosing]::Reset()
'PASS: closing gown/native fallback and relationship gates.'
