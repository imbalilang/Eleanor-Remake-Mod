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
$script:otherModSleepPrefix = $null
[EndOfDayUIManager]::NightSleep=[Action]{
    $run = [bool](Hook ChapterBedtimePatch Prefix @($bed))
    if ($run -and $script:otherModSleepPrefix) { $run = [bool](& $script:otherModSleepPrefix) }
    if ($run) { $counts.sleep++ }
    Hook ChapterBedtimePatch Postfix @($run)
}
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
    $script:otherModSleepPrefix=$null
    $script:events=[System.Collections.Generic.List[string]]::new();[EndOfDayUIManager]::NightGameOver=$false;[EndOfDayUIManager]::NightReactorFailure=$false
    $script:counts=@{close=0;sleep=0;bell=0;save=0;morning=0}
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new();[EleanorRomance.RomanceState]::ResetForNewGame($false)
    $station.dayCounter=$day;$store.storeState=$state;$store.visitLeftTonight=1;$store.currentClientInstance=$null;$store.storeClientManager.clientStack.Clear();$store.playerCash=250
    Hook LoveA2Closing Reset;[EleanorRomance.EndingPageUI]::HideRoutePage();[EleanorRomance.EndingPageUI]::FailPage=$false;[MetaHelper]::NightTimer=$null
}
function Choose {
    Assert ([EleanorRomance.EndingPageUI]::NightPage) 'Expected a page before clicking.'
    $action=[EleanorRomance.EndingPageUI]::Next
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
function FinishWindow([bool]$expectMonologueAfter = $false) {
    Assert (Hook ChapterVisits get_Talking) 'Expected dialogue at the window.'
    $ui = [DialogUIManager]::TestInstance
    $node = $ui.currentDialog
    Assert ($null -ne $node) 'Window dialogue did not start.'
    Assert ($ui.isSecMonologuePlaying) 'WindowTalk failed to enable monologue mode for null-client dialogue.'
    $steps = 0
    while ($node) {
        $steps++
        Assert ($steps -lt 50) 'Window dialogue loop exceeded expected length.'
        Assert ($node.choices.Count -eq 0) 'Unexpected choice in the linear CG dialogue.'
        if (!$node.nextDialogue) {
            $ui.EndConversation()
            $node.ExecuteEndAction()
            break
        }
        $expectedNext = $node.nextDialogue
        $ui.ContinueConversation()
        Assert ([object]::ReferenceEquals($ui.currentDialog, $expectedNext)) "Native ContinueConversation aborted at line $steps ('$($node.plainText)')."
        $node = $ui.currentDialog
    }
    Assert (!(Hook ChapterVisits get_Talking)) 'WindowTalk stayed locked after reaching the final line.'
    Assert ($ui.isSecMonologuePlaying -eq $expectMonologueAfter) 'WindowTalk left unexpected isSecMonologuePlaying state after finishing.'
}
Fresh 62;Love;[EleanorRomance.LoveState]::Act1Done=$true;[EleanorRomance.LoveState]::Complete(8)
$bed.OnEndDayButtonClick();$bed.OnEndDayButtonClick();Assert ($counts.sleep -eq 0) 'Sleep leaked through the window dialogue.'
Assert ([DialogUIManager]::TestInstance.currentDialog.title -ceq 'er_name_narrator') 'The proposal night must begin behind the closed shutter.'
Reload;$bed.OnEndDayButtonClick();FinishWindow
Assert ((Mark love_night_stay) -eq 62 -and (Mark love_night_supper) -eq -1 -and (Mark wake) -eq 63) 'Wrong wake checkpoint.'
$bed.OnEndDayButtonClick();CrossNight;Choose;Assert ((Mark wake) -eq -1) 'Wake not consumed'
[ModHelper]::TestData=$morningSave;Reload;Assert (!(Hook ChapterOpenShutterPatch Prefix)) 'Reload missed morning picture';Choose
Assert (Hook ChapterOpenShutterPatch Prefix) 'Morning opening blocked.'
'PASS automatic proposal night, window reload, next-morning illustration and single native autosave.'
foreach($failArt in @($false,$true)) {
    Fresh 46 OPEN;Maid;[EleanorRomance.MaidState]::Progress=17;$shutter.EndAndClose();$shutter.EndAndClose();$bed.OnEndDayButtonClick()
    Assert ($counts.close -eq 0 -and $counts.sleep -eq 0) 'Trial performance closed too early.'
    [EleanorRomance.EndingPageUI]::FailPage=$failArt;FinishWindow;if(!$failArt){Choose};$shutter.EndAndClose()
    Assert ($counts.close -eq 1 -and $store.visitLeftTonight -eq 1 -and $counts.sleep -eq 0) 'Trial performance took the evening.'
    $bed.OnEndDayButtonClick();CrossNight
}
'PASS trial performance and unavailable picture resume the shutter once.'
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
    $bed.OnEndDayButtonClick()
    $line = [DialogUIManager]::TestInstance.currentDialog
    Assert ($line.title -ceq 'PLAYER' -and $line.plainText -ceq 'er_cg_shop_sign') 'The shopkeeper should remember the sign before supper.'
    FinishWindow;Reload;$bed.OnEndDayButtonClick();Assert ([EleanorRomance.EndingPageUI]::Text -eq 'er_cg_shop_caption') 'E4 card did not resume';Choose;$bed.OnEndDayButtonClick();CrossNight
}
foreach($oldIntro in @($false,$true)) {
    Fresh 75 MORNING;[EleanorRomance.RomanceState]::Route=3;[EleanorRomance.RomanceState]::MotherState=2;[EleanorRomance.RomanceState]::RevVersion=2;[EleanorRomance.RomanceState]::RevComplete=$true
    if($oldIntro){[ModHelper]::SetModDataBool('eleanor_romance','revv2.cg.knock_intro',$true)}
    Hook ChapterMorningPatch Postfix;Assert (!(Hook ChapterOpenShutterPatch Prefix)) 'E9 missed shutter';if(!$oldIntro){FinishWindow};Reload
    Assert (!(Hook ChapterOpenShutterPatch Prefix)) 'E9 lost card';Assert ([EleanorRomance.EndingPageUI]::Text -eq 'er_cg_knock_caption') 'E9 added caption text';Choose
    Assert (Hook ChapterOpenShutterPatch Prefix) 'E9 blocked opening';Assert ([EleanorRomance.RomanceState]::RevEndingShown) 'E9 not committed'
}
'PASS both E4 arrivals, E9 mid-finale loads, released intro checkpoint.'
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
foreach($late in @($false,$true)) {
    Fresh 62 OPEN;Love;[EleanorRomance.LoveState]::A3T1Done=$true;[EleanorRomance.LoveState]::NextVisitDay=63;[EleanorRomance.RomanceState]::Chapter=[EleanorRomance.RomanceState]::ChapterComplete
    $one=WalkIn 'base';$q=$store.storeClientManager.clientStack;$q.Add($one);CaptureQueue @($one);$store.TryCallNextClient()
    Assert ($counts.bell -eq 1 -and $counts.close -eq 0) 'CG intercepted an ordinary call.'
    $shutter.EndAndClose();FinishWindow
    if($late){$external=WalkIn 'other_mod';$external.sourceModId='another_mod';$q.Add($external)}
    Choose
    if($late){Assert ($counts.close -eq 0 -and $q.Contains($external)) 'Lost late guest.'}
    else{Assert ($counts.close -eq 1 -and $store.visitLeftTonight -eq 1 -and $counts.sleep -eq 0) 'Day off changed the night.'}
}
'PASS day off at voluntary closure, normal service bell, late special guest protected.'

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

# The offline fixture leaves native sprite keys unresolved.
$tradePortraits = @{
    love_trade_wall = [EleanorRomance.ModConstants]::EleanorIntimatePortrait
    love_trade_kiss = [EleanorRomance.ModConstants]::EleanorIntimatePortrait
    dual_trade_dice = [EleanorRomance.ModConstants]::EleanorPlainCasualPortrait
    dual_trade_brooch = [EleanorRomance.ModConstants]::EleanorBlackMeshPortrait
    maid_trade_shopkeeper = 'maleBM5'
    maid_trade_shutters = [EleanorRomance.PortraitCompat]::MaidKey
    rev_trade_sill = 'eleanor_rev'
    rev_trade_wrist = 'eleanor_rev'
}
$companions = @{ dual_trade_dice = '莉迪亚'; dual_trade_brooch = '莉迪亚'; maid_trade_shopkeeper = '埃莉诺' }
function AssertTradeDialogue($node, [string[]]$expected, [string]$id, [string]$part) {
    foreach ($spoken in $expected) {
        $speaker, $text = $spoken.Split('　',2)
        Assert ($node -and $node.plainText -ceq $text -and $node.choices.Count -eq 0) "Incorrect trade dialogue in $id ($part)."
        if ($speaker -ceq $companions[$id]) {
            Assert ($node.title -ceq [DialogUIManager]::SEC) "Companion used the main speaking slot in $id ($part)."
        }
        $last = $node; $node = $node.nextDialogue
    }
    Assert (!$node) "Extra trade dialogue in $id ($part)."
    return $last
}

foreach ($id in $tradePortraits.Keys) {
    foreach ($paid in @($true,$false)) {
        Fresh 60 OPEN
        $chapter = Hook ChapterVisits Find @($id)
        $client = Hook ChapterVisits Trade @($chapter)
        Assert ($client.spriteName -ceq $tradePortraits[$id]) "Incorrect portrait for $id."
        if (!$paid) {
            $client.spriteName = 'eleanor1'
            Assert (Hook ChapterVisits RestoreTradePortrait @($client)) "Saved portrait was not restored for $id."
            Assert ($client.spriteName -ceq $tradePortraits[$id]) "Saved $id client still uses its previous outfit."
        }
        $instance = Blank ([StoreClientInstance]); $instance.storeClient = $client
        $store.currentClientInstance = $instance
        $ui = [DialogUIManager]::TestInstance; $ui.currentDialog = $null
        $fieldFlags = [Reflection.BindingFlags]'Instance,NonPublic'
        $field = if ($paid) { 'Sold' } else { 'Unsold' }
        $expected = $chapter.GetType().GetField($field,$fieldFlags).GetValue($chapter)
        $intro = $chapter.GetType().GetField('Intro',$fieldFlags).GetValue($chapter)
        $null = AssertTradeDialogue $client.mainDialogue $intro $id 'Intro'
        if ($paid) {
            $item = [DirectoryMaster]::Item('scrap_metal',$true)
            if ($client.clientIntent -eq [StoreClient+ClientIntent]::SELL) {
                $client.onItemSold.Invoke($item,100)
                [StoreClientInstance]::TestHasStock=$false
            } else {
                $client.onItemBought.Invoke($item,100)
                $client.clientBudget=0
            }
            $client.TestOnDealAccepted($null)
        } else { $store.TryPlayerDismissCurrentClient($true) }
        $last = AssertTradeDialogue $ui.currentDialog $expected $id $field
        Assert (![EleanorRomance.EndingPageUI]::NightPage) "Premature picture in $id."
        $last.ExecuteEndAction()
        Assert ((Mark $id) -eq 60 -and (Mark trade) -eq 60 -and [EleanorRomance.EndingPageUI]::NightPage -eq $paid) "Incorrect trade outcome for $id ($field)."
        if ($paid) {
            Assert ([EleanorRomance.EndingPageUI]::Text -ceq $chapter.GetType().GetField('Caption',$fieldFlags).GetValue($chapter)) "Incorrect caption for $id."
            Choose
        }
    }
}
'PASS eight CG transaction portraits, companion dialogue, saved outfit restoration and 16 sale/no-sale outcomes.'

$earlyCasual = [EleanorRomance.PortraitCompat]::DefaultEleanorKey
$plainCasual = [EleanorRomance.ModConstants]::EleanorPlainCasualPortrait
$legacyCasualKeys = @('eleanor1', $earlyCasual, ($earlyCasual + '_female'))
foreach ($key in $legacyCasualKeys) {
    foreach ($id in @('er_story_ch1', 'er_story_ch2', 'er_story_dual_daily_3', 'other_mod_eleanor')) {
        $client = Blank ([StoreClient])
        $client.identifier = $id
        $client.displayName = 'Eleanor'
        $client.spriteName = $key
        Hook PortraitCompat SanitizeClient @($client)
        $expected = if ($id -eq 'er_story_dual_daily_3') { $plainCasual } else { $key }
        Assert ($client.spriteName -ceq $expected) "Incorrect saved casual portrait for $id ($key)."
    }
}
Assert ((Hook PortraitExpression BaseKey @($earlyCasual)) -ceq 'eleanor1') 'Early casual lost its expression family.'
Assert ((Hook PortraitExpression BaseKey @($plainCasual)) -ceq 'eleanor1_no_earrings') 'Later casual fell back to the early expression family.'
'PASS both casual expression families, legacy saves, and another mod portrait.'

$visits=@{
    'love_night_book'=54;'love_night_supper'=61;'love_night_bathhouse'=67;
    'dual_night_bath'=68;'dual_night_fireworks'=70;'dual_night_hotel'=72;
    'maid_night_steps'=51;'maid_night_costume'=58;'maid_night_encore'=61;
    'rev_ride'=49;'rev_night_departure'=62;'rev_night_jacket'=73;'rev_night_barracks'=74
}
foreach($id in $visits.Keys){
    $chapter=Hook ChapterVisits Find @($id)
        Fresh $visits[$id];$store.visitLeftTonight=0;$store.healthData.wentToAfterhoursLastNight=$true
        switch -Wildcard ($id) {
            'love*' { Love;[EleanorRomance.LoveState]::T1Done=$true;[EleanorRomance.LoveState]::Act1Done=$true;[EleanorRomance.LoveState]::A3T3Done=$true }
            'dual*' { Dual;[EleanorRomance.DualState]::Complete('dv13') }
            'maid*' { Maid;[EleanorRomance.MaidState]::Progress=27;[EleanorRomance.MaidV2State]::Act2Done=$true;[EleanorRomance.MaidV2State]::GalaInvited=$true }
            'rev*'  { [EleanorRomance.RomanceState]::MotherState=2;[EleanorRomance.RomanceState]::Route=3;[EleanorRomance.RomanceState]::RevVersion=2;[EleanorRomance.RevolutionV2State]::Progress=286;[EleanorRomance.RomanceState]::RevComplete=($visits[$id] -ge 73) }
        }
        $bed.OnEndDayButtonClick()
        $cue = [DialogUIManager]::TestInstance.currentDialog
        $expected = $chapter.GetType().GetField('BedtimeCue',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($chapter)
        $expectedTitle = if ($id -in 'maid_night_steps','rev_night_departure') { 'PLAYER' } else { 'er_name_narrator' }
        Assert ($cue.title -ceq $expectedTitle -and $cue.plainText -ceq $expected.Split('　',2)[1]) "Wrong bedtime opening for $id."
        Assert (!$cue.startAction -and $cue.nextDialogue.startAction) "Portraits should be staged after the cue for $id."
        FinishWindow
        if($id -eq 'love_night_book'){Reload;$bed.OnEndDayButtonClick();FinishWindow}
        Assert ([EleanorRomance.EndingPageUI]::Text -ceq $chapter.GetType().GetField('Caption',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($chapter)) "Extra CG text for $id."
        Choose
        Assert ((Mark $id) -eq $visits[$id] -and $store.visitLeftTonight -eq 0 -and $store.healthData.wentToAfterhoursLastNight) "Wrong scene or changed outing result for $id."
        $bed.OnEndDayButtonClick();CrossNight
}
'PASS 13 linear nights, exact authored captions even without art, one save after each.'

# Regression for harem dual_night_bath deadlock: verify full multi-line advance through native
# ContinueConversation, door/bedroll/map blocking during talk & CG, and recovery on external abort.
Fresh 68;Dual;[EleanorRomance.DualState]::Complete('dv13')
$bed.OnEndDayButtonClick()
$ui=[DialogUIManager]::TestInstance
Assert ((Hook ChapterVisits get_Talking) -and $ui.isSecMonologuePlaying) 'dual_night_bath did not enter monologue window talk.'
Assert ($ui.currentDialog.title -eq 'er_name_narrator' -and $ui.currentDialog.plainText.StartsWith('咚咚咚')) 'dual_night_bath should begin with the knock.'
Assert (!(Hook ChapterWindowDoorPatch Prefix) -and !(Hook ChapterWindowBedrollPatch Prefix) -and !(Hook ChapterWindowMapPatch Prefix)) 'Door/bedroll/map stayed clickable during WindowTalk.'
# Simulate an unexpected clear of isSecMonologuePlaying before advancing: ContinueConversation prefix must restore it.
$ui.isSecMonologuePlaying=$false
$ui.ContinueConversation()
Assert ($ui.currentDialog.plainText -eq '别往她那边看啦，今晚只有我一个人过来。') 'dual_night_bath visitor line did not follow the knock.'
$ui.ContinueConversation()
Assert ($null -ne $ui.currentDialog -and $ui.currentDialog.plainText.Contains('三个人')) 'ContinueConversation dropped dual_night_bath after line 1.'
Assert ($ui.isSecMonologuePlaying) 'ContinueConversation prefix did not restore monologue mode.'
# Simulate an external abort (RemoveCurrentDialog): must unlock Talking & monologue so player can sleep or retry.
$ui.RemoveCurrentDialog()
Assert (!(Hook ChapterVisits get_Talking) -and !$ui.isSecMonologuePlaying) 'Aborted WindowTalk left Talking or monologue locked.'
Assert ((Hook ChapterWindowDoorPatch Prefix) -and (Hook ChapterWindowBedrollPatch Prefix) -and (Hook ChapterWindowMapPatch Prefix)) 'Controls stayed locked after WindowTalk abort.'
# Retry sleeping on the same night: dual_night_bath must play completely through all 10 lines, show CG, then sleep.
$bed.OnEndDayButtonClick()
FinishWindow
Assert (!(Hook ChapterWindowDoorPatch Prefix) -and !(Hook ChapterWindowBedrollPatch Prefix) -and !(Hook ChapterWindowMapPatch Prefix)) 'Door/bedroll/map stayed clickable while CG page was open.'
Choose
Assert ((Mark dual_night_bath) -eq 68 -and (Mark night) -eq 68) 'dual_night_bath did not record completion.'
$bed.OnEndDayButtonClick();CrossNight
# Preserve an already-active native security monologue if WindowTalk starts/resets alongside it.
Fresh 68;Dual;[EleanorRomance.DualState]::Complete('dv13')
$ui.isSecMonologuePlaying=$true
$bed.OnEndDayButtonClick()
FinishWindow $true
Choose
Assert ($ui.isSecMonologuePlaying) 'WindowTalk cleared a pre-existing native security monologue.'
$ui.isSecMonologuePlaying=$false
# Leaving the store must clear any active WindowTalk state instead of leaking it outside.
Fresh 68;Dual;[EleanorRomance.DualState]::Complete('dv13')
$bed.OnEndDayButtonClick()
Assert (Hook ChapterVisits get_Talking) 'Expected active WindowTalk before leave-store cleanup check.'
Hook ChapterWindowLeaveStorePatch Prefix
Assert (!(Hook ChapterVisits get_Talking) -and !$ui.isSecMonologuePlaying) 'Leave-store cleanup did not clear WindowTalk state.'
# Premature EndConversation on a non-final line must clear WindowTalk so player can retry and sleep.
Fresh 68;Dual;[EleanorRomance.DualState]::Complete('dv13')
$bed.OnEndDayButtonClick()
Assert ((Hook ChapterVisits get_Talking) -and $null -ne $ui.currentDialog.nextDialogue) 'Expected non-final WindowTalk line before premature EndConversation.'
$ui.EndConversation()
Assert (!(Hook ChapterVisits get_Talking) -and !$ui.isSecMonologuePlaying) 'Premature EndConversation left WindowTalk locked.'
$bed.OnEndDayButtonClick();FinishWindow;Choose;$bed.OnEndDayButtonClick();CrossNight
# Cross-mod sleep interception regression (Day 7 background prompt / post-closing bedtime mod):
# If another mod's OnEndDayButtonClick Prefix runs after ours and returns false, sleepStarting must NOT
# stay latched; the subsequent sleep attempt must still succeed.
Fresh 7
$script:otherBlocked = $true
$script:otherModSleepPrefix = { if ($script:otherBlocked) { $script:otherBlocked = $false; return $false }; return $true }
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 0) 'External mod sleep cancellation should have skipped native sleep.'
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 1) 'ChapterBedtimePatch latched sleepStarting after another mod cancelled OnEndDayButtonClick on Day 7.'
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 1) 'Duplicate sleep click during active blackscreen was not guarded.'
CrossNight
# Also verify cross-mod sleep interception chained right after an Eleanor bedtime CG.
Fresh 68;Dual;[EleanorRomance.DualState]::Complete('dv13')
$script:otherBlocked = $true
$script:otherModSleepPrefix = { if ($script:otherBlocked) { $script:otherBlocked = $false; return $false }; return $true }
$bed.OnEndDayButtonClick();FinishWindow;Choose
Assert ($counts.sleep -eq 0) 'External mod sleep cancellation after Eleanor bedtime CG should pause sleep once.'
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 1) 'Sleep stayed deadlocked after external mod cancelled post-CG sleep.'
CrossNight
# Morning check must clear sleepStarting even if BeginDay was skipped.
Fresh 10 CLOSED
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 1) 'Expected sleep entry before morning reset check.'
$store.storeState = 'MORNING'; Hook ChapterMorningPatch Postfix
$store.storeState = 'CLOSED'; $counts.sleep = 0
$bed.OnEndDayButtonClick()
Assert ($counts.sleep -eq 1) 'ChapterMorningPatch did not clear stale sleepStarting.'
'PASS dual_night_bath ContinueConversation advance, door/bedroll/map guards, abort/EndConversation/leave-store recovery, cross-mod sleep interception, and native monologue preservation.'
if($ReleasedSnapshots){
    $rows=Get-Content -LiteralPath $ReleasedSnapshots -Raw | ConvertFrom-Json -AsHashtable
    $checked=0
    foreach($row in $rows | Where-Object {$_.name -in 'E4-shown','E4-unshown','E9-shown','E9-unshown'}){
        $dualRoute=$row.name.StartsWith('E4');$state=if($dualRoute){'CLOSED'}else{'MORNING'}
        Fresh $row.day $state;[ModHelper]::TestData.Clear()
        foreach($key in $row.state.Keys){[ModHelper]::SetModData('eleanor_romance',$key,$row.state[$key])}
        [EleanorRomance.StoryDirector]::OnGameLoadedEarly();Hook LoveA2Closing OnLoaded;Hook ChapterLoadedReportPatch OnLoaded
        if($dualRoute){$bed.OnEndDayButtonClick()}else{$null=Hook ChapterOpenShutterPatch Prefix}
        if(Hook ChapterVisits get_Talking){FinishWindow}
        $pages=0;while([EleanorRomance.EndingPageUI]::NightPage -and $pages -lt 5){$pages++;Choose}
        $expected=if($row.name.EndsWith('-shown')){0}else{1}
        Assert ($pages -eq $expected) "Released $($row.name) replayed $pages pages instead of $expected."
        if($dualRoute){CrossNight}else{Assert (Hook ChapterOpenShutterPatch Prefix) 'Released E9 blocked opening.'}
        $checked++
    }
    Assert ($checked -eq 4) 'Missing released E4/E9 snapshots.'
    'PASS four released E4/E9 snapshots via load hooks, with completed and unseen endings.'
}
