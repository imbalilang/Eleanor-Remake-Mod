# Tests run in memory against the installed managed DLL. No Unity scene, Workshop files or save slots are touched.
param([string]$Managed=(Join-Path $PSScriptRoot '../../Probably Stolen_Data/Managed'), [string]$ReleasedSnapshots)
. (Join-Path $PSScriptRoot 'test_runtime_flows.ps1') -Managed (Resolve-Path $Managed).Path -ChapterNight -SmokeOnly
[DirectoryMaster]::TestFactory = [Func[string,bool,object]]{
    param($id,$owned)
    $item = Blank ([GameItemElement])
    $item.identifier = $id
    $item.state = [TagSystem]::new()
    $item.modifiedState = [TagSystem]::new()
    $item.itemFeatures = [System.Collections.Generic.List[ItemFeature]]::new()
    $item.itemTypes = [System.Collections.Generic.List[string]]::new()
    $item.unitCount = 1
    $item.shape = [GridShapeBuilder]::new().SetData('1',1).Build()
    $item.modifiedShape = [GridShapeBuilder]::new($item.shape).SetTransform(0,0,$false,0).Build()
    $item.mayInventoryAddItemFunc = [Func[GameItem,GameInventory,bool]]{param($i,$inv) $true}
    return $item
}
$modTypes=[EleanorRomance.RomanceState].Assembly
function Hook([string]$owner,[string]$method,[object[]]$arguments=@()) {
    $modTypes.GetType("EleanorRomance.$owner").GetMethod($method,[Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null,$arguments)
}
$station=Blank ([StoreStation]); [StoreStation]::TestInstance=$station
$store=Blank ([PlayerStore]); [PlayerStore]::TestInstance=$store
$store.nightLogs=[System.Collections.Generic.List[string]]::new();$store.nightLogs.Add('previous report')
$store.healthData=New-Object ($store.GetType().GetField('healthData').FieldType.FullName)
$store.storeClientData=[StoreClientData]::new();$store.storeClientManager=[StoreClientManager]::new()
$store.startingPerks=[System.Collections.Generic.List[StartingPerk]]::new();$store.storeReputations=[System.Collections.Generic.List[StoreReputation]]::new();$store.networkUpgrade=[System.Collections.Generic.Dictionary[string,NetworkUpgrade]]::new()
$shutter=Blank ([StoreShutterButton]); [StoreShutterButton]::NightInstance=$shutter
$bed=Blank ([EndOfDayUIManager]); [EndOfDayUIManager]::NightInstance=$bed
[StoreUIManager]::NightInstance=Blank ([StoreUIManager]);[DialogUIManager]::TestInstance=Blank ([DialogUIManager])
Hook LoveA2Closing set_Enabled @($true)
[StoreShutterButton]::NightClose=[Action]{ if(Hook ChapterCloseShutterPatch Prefix @($shutter)){$counts.close++;$store.storeState='CLOSED';$store.visitLeftTonight=1;$store.storeClientManager.clientStack.Clear()} }
[EndOfDayUIManager]::NightSleep=[Action]{ if(Hook ChapterBedtimePatch Prefix @($bed)){$counts.sleep++} }
[PlayerStore]::NightBell=[Action]{ if(Hook ChapterDayOffBellPatch Prefix @($store)){$counts.bell++} }
$blackType=[EndOfDayUIManager].GetNestedTypes([Reflection.BindingFlags]'NonPublic,Public') | Where-Object { $_.GetMethods([Reflection.BindingFlags]'NonPublic,Public,Instance').Name -contains '<OnEndDayButtonClick>b__0' }
$blackInstance=Blank $blackType
$beforeBlack=$blackType.GetMethod('<OnEndDayButtonClick>b__0',[Reflection.BindingFlags]'NonPublic,Public,Instance')
$afterBlack=$blackType.GetMethod('<OnEndDayButtonClick>b__1',[Reflection.BindingFlags]'NonPublic,Public,Instance')
[StartOfDayUIManager]::NightInstance=Blank ([StartOfDayUIManager]);$station.storeEventManager=Blank ([StoreEventManager])
[PlayerStore]::NightEndNight=[Action]{$events.Add('night')}
[StoreEventManager]::NightOnDayEnd=[Action]{$events.Add('events')}
[PlayerStore]::NightBeginDay=[Action]{$events.Add('day');$station.dayCounter++;Hook LoveA2NextDayPatch Postfix}
[PlayerStore]::NightSaveGame=[Action]{$events.Add('save');$counts.save++;$script:morningSave=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)}
[PlayerStore]::NightExecuteGameOver=[Action]{$events.Add('over');[EndOfDayUIManager]::NightGameOver=$true}
[StartOfDayUIManager]::NightOpenUI=[Action]{$events.Add('morning');$counts.morning++;Hook ChapterMorningPatch Postfix}
function Fresh([int]$day,[string]$state='CLOSED') {
    $script:events=[System.Collections.Generic.List[string]]::new();[EndOfDayUIManager]::NightGameOver=$false;[EndOfDayUIManager]::NightReactorFailure=$false
    $script:counts=@{close=0;sleep=0;bell=0;save=0;morning=0}
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new();[EleanorRomance.RomanceState]::ResetForNewGame($false)
    $station.dayCounter=$day;$store.storeState=$state;$store.visitLeftTonight=1;$store.currentClientInstance=$null;$store.storeClientManager.clientStack.Clear();$store.playerCash=250
    Hook LoveA2Closing Reset;[EleanorRomance.EndingPageUI]::HideRoutePage();[EleanorRomance.EndingPageUI]::FailPage=$false;[MetaHelper]::NightTimer=$null
}
function Choose([switch]$No) {
    Assert ([EleanorRomance.EndingPageUI]::NightPage) 'Expected a page before clicking.'
    $action=if($No){[EleanorRomance.EndingPageUI]::Decline}else{[EleanorRomance.EndingPageUI]::Next}
    [EleanorRomance.EndingPageUI]::HideRoutePage(); if($action){$action.Invoke()}
}
function Reload { Hook LoveA2Closing Reset; [EleanorRomance.EndingPageUI]::HideRoutePage(); Hook LoveA2Closing OnLoaded; Hook ChapterLoadedReportPatch OnLoaded }
function CrossNight {
    Assert ($counts.sleep -eq 1) 'Expected a single native sleep entry.'
    $beforeBlack.Invoke($blackInstance,@());$afterBlack.Invoke($blackInstance,@())
    Assert (($events -join '/') -eq 'night/events/day/save/morning') 'Native sleep callbacks lost or duplicated a checkpoint.'
    $bed.OnEndDayButtonClick(); Assert ($counts.sleep -eq 1) 'Sleep restarted from morning.'
}
function Love { [EleanorRomance.LoveState]::Start(54) }
function Dual { [EleanorRomance.RomanceState]::Route=1;[EleanorRomance.RomanceState]::MotherState=1;[EleanorRomance.DualState]::Accept();[EleanorRomance.DualState]::Relation=2 }
function Maid { [EleanorRomance.MaidState]::FeatureVersion=2;[EleanorRomance.MaidState]::EntryKind=3;[EleanorRomance.MaidState]::Contract=1;[EleanorRomance.MaidV2State]::CoWork=$true }
function Mark([string]$id){ [ModHelper]::GetModDataInt('eleanor_romance',"er.cg.$id",-1) }
foreach($no in @($false,$true)) {
    Fresh 62;Love;[EleanorRomance.LoveState]::Act1Done=$true;[EleanorRomance.LoveState]::Complete(8)
    $bed.OnEndDayButtonClick();$bed.OnEndDayButtonClick();Assert ($counts.sleep -eq 0) 'Sleep leaked through the invitation.'
    Choose -No:$no
    Assert ((Mark love_night_stay) -eq 62 -and (Mark love_night_supper) -eq -1) 'Overlapping flexible invitation replaced the proposal.'
    Assert (((Mark wake) -eq 63) -eq !$no) 'Wrong wake checkpoint while the answer is open.'
    if(!$no){Reload;$bed.OnEndDayButtonClick()}else{Choose}
    $bed.OnEndDayButtonClick();CrossNight
    Assert ([EleanorRomance.EndingPageUI]::NightPage -eq !$no) 'Morning picture disagrees with proposal answer.'
    if(!$no){Choose;Assert ((Mark wake) -eq -1) 'Wake not consumed';[ModHelper]::TestData=$morningSave;Reload;Assert (!(Hook ChapterOpenShutterPatch Prefix)) 'Reload missed morning picture';Choose}
    Assert (Hook ChapterOpenShutterPatch Prefix) 'Morning opening blocked.'
}
'PASS proposal yes/no, overlapping invitations, mid-answer load, morning autosave checkpoint and repeated sleep.'
foreach($failArt in @($false,$true)) {
    Fresh 46 OPEN;Maid;[EleanorRomance.MaidState]::Progress=17;$shutter.EndAndClose();$shutter.EndAndClose();$bed.OnEndDayButtonClick()
    Assert ($counts.close -eq 0 -and $counts.sleep -eq 0) 'Trial performance closed too early.'
    [EleanorRomance.EndingPageUI]::FailPage=$failArt;Choose;if(!$failArt){Choose};$shutter.EndAndClose()
    Assert ($counts.close -eq 1 -and $store.visitLeftTonight -eq 1 -and $counts.sleep -eq 0) 'Trial performance took the evening.'
    $bed.OnEndDayButtonClick();CrossNight
}
'PASS soundcheck through its actual continue button, unavailable picture, repeated shutter and native evening allowance.'
foreach($loaded in @('none','OPEN','CLOSED','stale')) {
    Fresh 57 OPEN;Love; Hook LoveA2Closing FinishFromVisit
    if($loaded -eq 'OPEN'){Reload}else{Choose}
    $old=[MetaHelper]::NightTimer;Assert ($null -ne $old) 'Missing meal sleep timer.'
    if($loaded -eq 'CLOSED'){Reload;Assert (!(Hook ChapterLoadedReportPatch Prefix)) 'Old morning report advanced the evening';$old.Invoke();Assert ($counts.sleep -eq 0) 'Old timer survived reload';$old=[MetaHelper]::NightTimer}
    if($loaded -eq 'stale'){Fresh 57 CLOSED;$old.Invoke();Assert ($counts.sleep -eq 0) 'Old slot timer advanced a different slot';continue}
    if($loaded -eq 'none'){$old.Invoke()}else{$bed.OnEndDayButtonClick()};$old.Invoke();$bed.OnEndDayButtonClick()
    Assert ($counts.close -eq 1 -and $store.visitLeftTonight -eq 0) 'Meal closure count or outing incorrect'; CrossNight
}
'PASS Sophia forced close, OPEN/CLOSED recovery, manual sleep versus timer, stale timer after slot change.'
Fresh 66 OPEN;Dual;Hook DualClosingDay Finish;Hook DualClosingDay Finish;Hook DualClosingDay OnLoaded
Assert ($counts.close -eq 1 -and $counts.sleep -eq 0 -and $store.visitLeftTonight -eq 1 -and $store.playerCash -eq 2250) 'Paid closure lost the outing or duplicated payment.'
Choose;$bed.OnEndDayButtonClick();CrossNight
'PASS paid closure awards 2000 once and leaves one outing until the player sleeps.'
foreach($outing in @($false,$true)) {
    Fresh 73;Dual;[EleanorRomance.DualState]::Complete('dv13');[EleanorRomance.DualState]::SetSceneDay('dv13',67);$store.healthData.wentToAfterhoursLastNight=$outing
    $bed.OnEndDayButtonClick();$key=if($outing){'er_cg_shop_out'}else{'er_cg_shop_inside'};Assert ([EleanorRomance.EndingPageUI]::Text -eq $key) 'Shop finale selected the wrong branch.'
    Choose;Reload;$bed.OnEndDayButtonClick();Assert ([EleanorRomance.EndingPageUI]::Text -eq 'er_cg_shop_caption') 'E4 card did not resume';Choose;$bed.OnEndDayButtonClick();CrossNight
}
Fresh 75 MORNING;[EleanorRomance.RomanceState]::Route=3;[EleanorRomance.RomanceState]::MotherState=2;[EleanorRomance.RomanceState]::RevVersion=2;[EleanorRomance.RomanceState]::RevComplete=$true
Hook ChapterMorningPatch Postfix;Choose;Reload;Assert (!(Hook ChapterOpenShutterPatch Prefix)) 'E9 missed second page';Assert ([EleanorRomance.EndingPageUI]::Text -eq 'er_cg_knock_rail') 'E9 intro repeated';Choose;Reload;Hook ChapterMorningPatch Postfix;Assert ([EleanorRomance.EndingPageUI]::Text -eq 'er_cg_knock_caption') 'E9 card did not resume';Choose;Assert (Hook ChapterOpenShutterPatch Prefix) 'E9 blocked opening after ending';Assert ([EleanorRomance.RomanceState]::RevEndingShown) 'E9 not committed'
'PASS E4 inside/outing, E9 transition pages, mid-finale loads, final opening.'
Fresh 61;[EndOfDayUIManager]::NightReactorFailure=$true;$bed.OnEndDayButtonClick();$beforeBlack.Invoke($blackInstance,@());$afterBlack.Invoke($blackInstance,@())
Assert (($events -join '/') -eq 'night/events/day/over') 'Game over saved or opened the next morning.'
'PASS native game-over fork ends before the save and morning report.'

function WalkIn([string]$name) {
    $client=[StoreClient]::new();$client.identifier=$name;$client.clientIntent='BUY';$client.dismissable=$true;return $client
}
function CaptureQueue([object[]]$walkins) {
    Hook ChapterTradeQueuePatch Prefix
    foreach($c in $walkins){Hook ChapterRandomWalkInPatch Postfix @($c)}
    Hook ChapterTradeQueuePatch Postfix @($store.storeClientManager)
}
foreach($action in @('bell','close')) { foreach($no in @($false,$true)) {
    Fresh 62 OPEN;Love;[EleanorRomance.LoveState]::A3T1Done=$true;[EleanorRomance.LoveState]::NextVisitDay=63;[EleanorRomance.RomanceState]::Chapter=[EleanorRomance.RomanceState]::ChapterComplete
    $one=WalkIn 'base';$q=$store.storeClientManager.clientStack;$q.Add($one);CaptureQueue @($one)
    if($action -eq 'bell'){$store.TryCallNextClient()}else{$shutter.EndAndClose()};Choose -No:$no;Choose;if(!$no){Choose}
    if($no -and $action -eq 'bell'){Assert ($counts.bell -eq 1 -and $counts.close -eq 0) 'Declining day off failed to resume bell'}
    else {Assert ($counts.close -eq 1 -and $counts.bell -eq 0 -and $store.visitLeftTonight -eq 1 -and $counts.sleep -eq 0) 'Day off resumed the wrong action'}
}}
Fresh 62 OPEN;Love;[EleanorRomance.LoveState]::A3T1Done=$true;[EleanorRomance.LoveState]::NextVisitDay=63;[EleanorRomance.RomanceState]::Chapter=[EleanorRomance.RomanceState]::ChapterComplete
$q=$store.storeClientManager.clientStack;$one=WalkIn 'base';$q.Add($one);CaptureQueue @($one);$store.TryCallNextClient();Choose;$external=WalkIn 'other_mod';$external.sourceModId='another_mod';$q.Add($external);Choose;Choose
Assert ($counts.close -eq 0 -and $q.Contains($external)) 'A late guest was erased during an outing answer.'
'PASS day off through bell and shutter, declined action once, accepted closure once, late special guest protected.'

foreach($n in 0,1,2) {
    Fresh 70 OPEN;Dual;[EleanorRomance.DualState]::Complete('dv13');$q=$store.storeClientManager.clientStack
    $daily=[EleanorRomance.DualStory]::CreateScene('daily');$q.Add($daily);$walks=@()
    for($i=0;$i -lt $n;$i++){$w=WalkIn "native$i";$walks+=@($w);$q.Add($w)}
    CaptureQueue $walks;Assert ([object]::ReferenceEquals($q[0],$daily)) 'Daily actor displaced.'
    Assert (($q.identifier -contains 'er_story_cg_dual_trade_brooch') -eq ($n -eq 2)) 'Trade consumed a reserved slot or failed to use two ordinary slots.'
    if($n -eq 2){$brooch=$q[2];Hook ChapterTradeQueuePatch TryInsert @($store.storeClientManager);Assert ([object]::ReferenceEquals($q[2],$brooch)) 'Duplicate CG on repeated generation.'
        $null=$q.Remove($daily);Assert ($store.CanEndDay()) 'An optional CG customer disabled the native shutter.';[EleanorRomance.DualState]::PendingId='daily';[EleanorRomance.DualState]::PendingScene=101;[EleanorRomance.DualState]::NextSceneDay=70;[EleanorRomance.RomanceState]::LastQueuedDay=70
        [EleanorRomance.DualDirector]::RecoverPending($store.storeClientManager)
        Assert ([EleanorRomance.DualState]::PendingId -eq '' -and [EleanorRomance.RomanceState]::LastQueuedDay -ne 70) 'CG impersonated an outstanding daily visit.'
        Assert (!(Hook StoryDirector HasModClient @($store.storeClientManager,$false))) 'Scheduler still blocks a surviving CG.'
        for($j=1;$j -le 13;$j++){[EleanorRomance.DualState]::Complete(('dv{0:d2}' -f $j))};[EleanorRomance.DualState]::LastDailyDay=66
        [EleanorRomance.DualState]::SetInt('reward.claimed',3)
        Assert ([EleanorRomance.DualDirector]::TryQueue($store.storeClientManager,70)) 'Lost visit could not be requeued.'
        Assert ($q.Contains($brooch) -and [EleanorRomance.DualState]::PendingId -eq 'daily') 'Wrong restored visit.'
    }
}
Fresh 70 OPEN;Dual;[EleanorRomance.DualState]::Complete('dv13');$q=$store.storeClientManager.clientStack;$ordinary=WalkIn 'regular';$protected=WalkIn 'required-buyer';$protected.dismissable=$false;$q.Add($ordinary);$q.Add($protected);CaptureQueue @($ordinary,$protected)
Assert ($q.Contains($protected) -and !$store.CanEndDay()) 'An obligatory buyer was converted to an optional CG.'
Fresh 49 OPEN;Maid;[EleanorRomance.MaidState]::Progress=18;$q=$store.storeClientManager.clientStack
$delivery=[EleanorRomance.MaidBusiness]::CreateEvent(910);$dividend=[EleanorRomance.MaidDividends]::CreateVisit();$q.Add($delivery);$q.Add($dividend);$w1=WalkIn 'walk1';$w2=WalkIn 'walk2';$q.Add($w1);$q.Add($w2);CaptureQueue @($w1,$w2)
Assert ($q.Contains($delivery) -and $q.Contains($dividend) -and $q[3].identifier -eq 'er_story_cg_maid_trade_shopkeeper') 'Maid business and trader cannot coexist.'
Fresh 57 OPEN;Love;$sophia=[EleanorRomance.LoveStory]::CreateVisit(4);$q=$store.storeClientManager.clientStack;$q.Add($sophia);$native=WalkIn 'ordinary';$q.Add($native);$late=WalkIn 'future';$q.Add($late);Hook ChapterServingOrderPatch Prefix @($store)
Assert ($q.Count -eq 3 -and [object]::ReferenceEquals($q[2],$sophia) -and [object]::ReferenceEquals($q[0],$native) -and [object]::ReferenceEquals($q[1],$late)) 'Sophia was not last after future customers.'
'PASS actual dual daily and brooch coexist, dropped daily restored, commission/dividend with CG, Sophia after future clients.'
Fresh 66 OPEN;Dual;$paidDate=[EleanorRomance.DualStory]::CreateScene('dv10');$q=$store.storeClientManager.clientStack;$q.Add($paidDate);$ordinary=WalkIn 'ordinary';$q.Add($ordinary);$q.Add($delivery)
Hook ChapterServingOrderPatch Prefix @($store);Assert ([object]::ReferenceEquals($q[0],$delivery)) 'Paid date erased a reserved delivery.';$q.RemoveAt(0);Hook ChapterServingOrderPatch Prefix @($store);Assert ([object]::ReferenceEquals($q[0],$paidDate)) 'Ordinary walk-in displaced the paid date.'
'PASS paid date yields to a reserved visit and keeps priority over walk-ins.'

$visits=@{
    'love_night_book'=54;'love_night_supper'=61;'love_night_bathhouse'=67;
    'dual_night_bath'=68;'dual_night_fireworks'=70;'dual_night_hotel'=72;
    'maid_night_steps'=51;'maid_night_costume'=58;'maid_night_encore'=61;
    'rev_ride'=49;'rev_night_departure'=62;'rev_night_jacket'=73;'rev_night_barracks'=74
}
foreach($id in $visits.Keys){
    $chapter=Hook ChapterVisits Find @($id);$labels=$chapter.GetType().GetField('Labels',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($chapter)
    $answers=if($labels.Length -eq 2){@($false,$true)}else{@($false)}
    foreach($no in $answers){
        Fresh $visits[$id];$store.visitLeftTonight=0;$store.healthData.wentToAfterhoursLastNight=$true
        switch -Wildcard ($id) {
            'love*' { Love;[EleanorRomance.LoveState]::T1Done=$true;[EleanorRomance.LoveState]::Act1Done=$true;[EleanorRomance.LoveState]::A3T3Done=$true }
            'dual*' { Dual;[EleanorRomance.DualState]::Complete('dv13') }
            'maid*' { Maid;[EleanorRomance.MaidState]::Progress=27;[EleanorRomance.MaidV2State]::Act2Done=$true;[EleanorRomance.MaidV2State]::GalaInvited=$true }
            'rev*'  { [EleanorRomance.RomanceState]::MotherState=2;[EleanorRomance.RomanceState]::Route=3;[EleanorRomance.RomanceState]::RevVersion=2;[EleanorRomance.RevolutionV2State]::Progress=286;[EleanorRomance.RomanceState]::RevComplete=($visits[$id] -ge 73) }
        }
        $bed.OnEndDayButtonClick();Choose -No:$no
        if(!$no -and $id -eq 'love_night_book'){Reload;$bed.OnEndDayButtonClick();Choose}
        for($page=0;$page -lt 4 -and [EleanorRomance.EndingPageUI]::NightPage;$page++){$bed.OnEndDayButtonClick();Choose}
        Assert ((Mark $id) -eq $visits[$id] -and $store.visitLeftTonight -eq 0 -and $store.healthData.wentToAfterhoursLastNight) "Wrong scene or changed outing result for $id."
        $bed.OnEndDayButtonClick();CrossNight
    }
}
'PASS all remaining 13 bedtime chapters via their actual available choices, including reloading before the book illustration.'
if($ReleasedSnapshots){
    $rows=Get-Content -LiteralPath $ReleasedSnapshots -Raw | ConvertFrom-Json -AsHashtable
    $checked=0
    foreach($row in $rows | Where-Object {$_.name -in 'E4-shown','E4-unshown','E9-shown','E9-unshown'}){
        $dualRoute=$row.name.StartsWith('E4');$state=if($dualRoute){'CLOSED'}else{'MORNING'}
        Fresh $row.day $state;[ModHelper]::TestData.Clear()
        foreach($key in $row.state.Keys){[ModHelper]::SetModData('eleanor_romance',$key,$row.state[$key])}
        [EleanorRomance.StoryDirector]::OnGameLoadedEarly();Hook LoveA2Closing OnLoaded;Hook ChapterLoadedReportPatch OnLoaded
        if($dualRoute){$bed.OnEndDayButtonClick()}else{Hook ChapterMorningPatch Postfix}
        $pages=0;while([EleanorRomance.EndingPageUI]::NightPage -and $pages -lt 5){$pages++;Choose}
        $expected=if($row.name.EndsWith('-shown')){0}elseif($dualRoute){2}else{3}
        Assert ($pages -eq $expected) "Released $($row.name) replayed $pages pages instead of $expected."
        if($dualRoute){CrossNight}else{Assert (Hook ChapterOpenShutterPatch Prefix) 'Released E9 blocked opening.'}
        $checked++
    }
    Assert ($checked -eq 4) 'Missing released E4/E9 snapshots.'
    'PASS four released E4/E9 snapshots via load hooks, with completed and unseen endings.'
}
