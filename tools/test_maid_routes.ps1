# Uses the in-memory native assembly set up by test_runtime_flows.ps1 -Maid.
$script:businessStock=[System.Collections.Generic.List[GameItem]]::new()
[EleanorRomance.MaidBusiness]::TestPutStock=[Action[object]]{param($item) $script:businessStock.Add($item)}
[ReputationUIManager]::TestInstance=Blank ([ReputationUIManager])
[AchievementTracker]::TestInstance=Blank ([AchievementTracker])
$rules=[DifficultyRules]::new()
([type]'Difficulty').GetField('TestRules').SetValue($null,$rules)
$rules.upgradePriceMultiplier=1
$store.startingPerks=[System.Collections.Generic.List[StartingPerk]]::new()
$store.storeReputations=[System.Collections.Generic.List[StoreReputation]]::new()
foreach($faction in @('FACTION_UPPER_LEVEL','FACTION_LOWER_LEVEL')){
    $r=[StoreReputation]::new();$r.factionId=$faction;$store.storeReputations.Add($r)
}
function Set-Reps($upper,$lower){
    [StoreReputation]::GetStoreReputation('FACTION_UPPER_LEVEL').SetReputation($upper)
    [StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').SetReputation($lower)
}
function New-MaidRun($code=12){
    Reset-Run $code
    [EleanorRomance.MaidProjects]::Recover()
    [ReputationUIManager]::TestNotify=$null
    $store.playerCash=20000;$store.wildFavor=0
    $store.networkUpgrade=[System.Collections.Generic.Dictionary[string,NetworkUpgrade]]::new()
    $store.storeClientManager=[StoreClientManager]::new()
    Set-Reps 60 60
    $rules.upgradePriceMultiplier=1
    $store.startingPerks.Clear()
    [EleanorRomance.MaidProjects]::OnLoaded()
}
function Pick($head,$index){
    $node=Choice-Node $head
    Assert ($node -ne $null) 'Expected a choice node.'
    Assert ($node.IsChoiceEnabledAt($index)) "Choice $index is unexpectedly disabled."
    $choice=$node.choices[$index]
    if($choice.Item3){$choice.Item3.Invoke()}
    return $choice.Item2
}
function Main-Client($code){
    [EleanorRomance.RomanceState]::Chapter=$code
    [EleanorRomance.RomanceState]::PendingChapter=$code
    $c=[EleanorRomance.EleanorStory]::CreateClient($code)
    Assert ($c -ne $null) "Maid chapter $code is missing."
    Use-Client $c
    return $c
}
function Set-Partner {
    [EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.MaidState]::Confession=1
    [EleanorRomance.RomanceState]::Romance=1
    [EleanorRomance.RomanceState]::Route=2
    [EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::ProtectionReady=$true
    [EleanorRomance.RomanceState]::DriveState=4
}
function Assert-ProjectLayout($expected,$stage){
    # Native Open invalidates definitions before BuildTabs; testing just the
    # runtime hidden flag misses the source-template and cached-layout defects.
    $cashBefore=$store.playerCash;$favorBefore=$store.wildFavor
    foreach($plan in [EleanorRomance.MaidProjects]::Plans){$store.networkUpgrade[$plan.Id].hidden=$false}
    (Blank ([WildUIManager])).OpenUI($false)
    foreach($plan in [EleanorRomance.MaidProjects]::Plans){
        Assert ($store.networkUpgrade[$plan.Id].hidden -eq ($plan.Id -notin $expected)) "Legacy open did not refresh visibility at $stage for $($plan.Id)."
    }
    [NetworkUpgrade]::InvalidateDefinitions()
    $flags=[Reflection.BindingFlags]'NonPublic,Static'
    $null=([WildNetworkUI]).GetMethod('BuildTabs',$flags).Invoke($null,@())
    $tabs=([WildNetworkUI]).GetField('tabs',$flags).GetValue($null)
    $actual=@($tabs | ForEach-Object {$_.cards} | ForEach-Object {$_.cells} |
        ForEach-Object {$_.id} | Where-Object {$_ -like 'er_maid_p*'} | Sort-Object)
    Assert (($actual -join ',') -eq (@($expected | Sort-Object) -join ',')) "Unexpected native cards at ${stage}: $($actual -join ',')"
    foreach($plan in [EleanorRomance.MaidProjects]::Plans){
        Assert ($store.networkUpgrade[$plan.Id].hidden -eq ($plan.Id -notin $expected)) "Runtime visibility disagrees at $stage for $($plan.Id)."
    }
    Assert ($store.playerCash -eq $cashBefore -and $store.wildFavor -eq $favorBefore) 'Rebuilding the layout changed payment resources.'
}

# Spoiler boundaries use the actual native filter/layout, including cache
# invalidation on every open, rather than an assertion about hidden flags alone.
New-MaidRun
[NetworkUpgrade]::InvalidateDefinitions()
Assert (@([NetworkUpgrade]::GetDefinitionsInOrder() | Where-Object {$_.id -like 'er_maid_p*' -and (!$_.hidden -or $_.state -ne 0)}).Count -eq 0) 'Native definition templates were not hidden/locked by default.'
Assert-ProjectLayout @() 'before entry'
[EleanorRomance.MaidState]::Enter(1);[EleanorRomance.RomanceState]::Route=2
Assert-ProjectLayout @() 'before reunion'
Set-Partner
[EleanorRomance.MaidState]::Confession=0;[EleanorRomance.RomanceState]::Romance=0
Assert-ProjectLayout @('er_maid_p1_cash') 'reunion before confession'
Set-Partner
[EleanorRomance.MaidState]::Flag('p1.paid',$true)
Assert-ProjectLayout @('er_maid_p1_cash') 'paid housing before base conclusion'
[EleanorRomance.MaidState]::BasicComplete=$true
Assert-ProjectLayout @('er_maid_p1_cash') 'base conclusion before housing feedback'
[EleanorRomance.MaidState]::Flag('p1.shown',$true)
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash') 'base conclusion and housing feedback'
[EleanorRomance.MaidState]::Flag('p2.paid',$true);[EleanorRomance.MaidState]::Text('p2.plan','er_maid_p2_cash')
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash') 'paid pass before chapter 24'
[EleanorRomance.MaidState]::Progress=24
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash','er_maid_p3_cash') 'chapter 24 complete'
[EleanorRomance.MaidState]::Flag('p3.paid',$true);[EleanorRomance.MaidState]::Text('p3.plan','er_maid_p3_favor')
[EleanorRomance.MaidState]::Progress=25
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash','er_maid_p3_cash') 'chapter 25 complete'
[EleanorRomance.MaidState]::Progress=26
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash','er_maid_p3_cash','er_maid_p4_cash') 'chapter 26 complete'
[EleanorRomance.MaidState]::End('test',4)
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash','er_maid_p3_cash') 'agreement ended with paid records'
[EleanorRomance.RomanceState]::Route=4
Assert-ProjectLayout @() 'relationship terminated'
New-MaidRun
Assert-ProjectLayout @() 'fresh run after an advanced run'
'PASS: legacy open and native definition rebuild/filter/layout across all four reveal gates, selected paid plans, termination and fresh runs.'

# Entry exploration, withdrawal, insufficient money, successful and interrupted payment.
New-MaidRun
$c=Main-Client 12;$cash=$store.playerCash
$offer=Pick $c.mainDialogue 1
Assert ($store.playerCash -eq $cash -and ![EleanorRomance.MaidState]::HasHistory) 'Exploring the offer charged or locked the route.'
$menu=Choice-Node $offer
$store.playerCash=799
Assert (!$menu.IsChoiceEnabledAt(0)) 'Insufficient entry cash did not disable confirmation.'
$store.playerCash=800
$paid=Pick $offer 0
Assert ($store.playerCash -eq 0 -and [EleanorRomance.MaidState]::EntryPaidCash -eq 800 -and
    [EleanorRomance.RomanceState]::Savings -eq 800) 'Entry did not make one separate 800 payment.'
($menu.choices[0]).Item3.Invoke()
Assert ($store.playerCash -eq 0) 'Repeated entry callback charged twice.'
Finish-Dialogue $paid
Assert ([EleanorRomance.RomanceState]::Chapter -eq 13) 'Paid entry did not continue the investigation.'
New-MaidRun
$c=Main-Client 12;$offer=Pick $c.mainDialogue 1
Finish-Dialogue (Pick $offer 1)
Assert (![EleanorRomance.MaidState]::HasHistory -and $store.playerCash -eq 20000 -and
    [EleanorRomance.RomanceState]::Romance -eq 0 -and [EleanorRomance.RomanceState]::Chapter -eq 13) 'Withdrawal altered payment or normal relationship.'
New-MaidRun
$null=[EleanorRomance.MaidProjects]::TryEnter();$savedCash=$store.playerCash
$c=Main-Client 12;Finish-Dialogue $c.mainDialogue
Assert ($store.playerCash -eq $savedCash -and [EleanorRomance.RomanceState]::Chapter -eq 13) 'Interrupted paid entry was charged again.'
'PASS: proposed/withdrawn/insufficient/confirmed/repeated/interrupted entry payment.'

# Genuine local save facts; repeat load without changing date, then both directions to the drive.
$fixture=Get-Content (Join-Path $PSScriptRoot 'fixtures/legacy_800_real_anonymized.json') -Raw | ConvertFrom-Json
foreach($case in @('confirm','retract','back')){
    New-MaidRun
    [ModHelper]::TestData.Clear()
    foreach($p in $fixture.state.PSObject.Properties){[ModHelper]::SetModData('eleanor_romance',$p.Name,[string]$p.Value)}
    $station.dayCounter=$fixture.day
    foreach($p in $fixture.native.PSObject.Properties){$store.storeClientData.($p.Name)=$p.Value}
    $before=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    $cash=$store.playerCash
    [EleanorRomance.MaidRouteMigration]::OnLoaded();$due=[EleanorRomance.MaidState]::LegacyDueDay
    1..3 | ForEach-Object {[EleanorRomance.MaidRouteMigration]::OnLoaded()}
    Assert ([EleanorRomance.MaidState]::LegacyResume -eq 1 -and [EleanorRomance.MaidState]::LegacyDueDay -eq $due) 'Legacy load replayed migration/reset its date.'
    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
    Assert ($store.storeClientManager.clientStack.Count -eq 0) 'Main drive visit bypassed the not-yet-due legacy prologue.'
    foreach($key in $before.Keys){if($key -ne 'eleanor_romance:er.romance'){Assert ([ModHelper]::TestData[$key] -eq $before[$key]) "Migration changed $key"}}
    $station.dayCounter=$due
    $null=[EleanorRomance.MaidStory]::TryQueue($store.storeClientManager,$due)
    $c=$store.storeClientManager.clientStack[0];Use-Client $c
    $store.TryPlayerDismissCurrentClient($true);$wrap=$ui.currentDialog
    Assert ($wrap -ne $null) 'Legacy shop wrap did not open.'
    if($case -eq 'retract'){
        $branch=Pick $wrap 1
    } else {
        $help=Pick $wrap 0;Finish-Dialogue $help
        $finalIndex=if($case -eq 'back'){1}else{0}
        $branch=Pick $help $finalIndex
    }
    Finish-Dialogue $branch
    Assert ($store.playerCash -eq $cash -and [EleanorRomance.MaidState]::EntryPaidCash -eq 0 -and
        [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Legacy continuation charged, invented pay, or stranded its event.'
    if($case -eq 'confirm'){
        Assert ([EleanorRomance.MaidState]::EntryKind -eq 2 -and [EleanorRomance.MaidState]::HasHistory) 'Final confirmation did not record the waived old entry.'
    } else {
        Assert (![EleanorRomance.MaidState]::HasHistory) 'Retracting the old proposal opened the attendant line.'
    }
    $station.dayCounter++
    $c=Main-Client 13;Finish-Dialogue (Pick $c.mainDialogue 1)
    Assert ([EleanorRomance.RomanceState]::Chapter -eq 14 -and [EleanorRomance.RomanceState]::DriveState -eq 2) 'Real legacy facts did not reach drive custody.'
}
foreach($change in @(@('PendingChapter',13),@('SchemaVersion',3),@('OldBlocked',$true),@('Route',4),@('BetrayalKind',1),@('DriveState',1),@('MotherState',2),@('Chapter',14))){
    New-MaidRun
    [ModHelper]::TestData.Clear()
    foreach($p in $fixture.state.PSObject.Properties){[ModHelper]::SetModData('eleanor_romance',$p.Name,[string]$p.Value)}
    [EleanorRomance.RomanceState]::($change[0])=$change[1]
    [EleanorRomance.MaidRouteMigration]::OnLoaded()
    Assert ([EleanorRomance.MaidState]::LegacyResume -eq 0) "Excluded legacy shape $($change[0]) was migrated."
}
'PASS: real anonymized legacy facts, three reloads, v0.7 trade/retract/final-confirm prologue, drive continuation and excluded shapes.'

# Four single cash plans, native pricing/favor and one payment per project.
New-MaidRun
Assert (($store.networkUpgrade.Keys | Where-Object {$_ -like 'er_maid_p*'}).Count -eq 4) 'Four project definitions not registered.'
[EleanorRomance.MaidProjects]::OnLoaded()
Assert (($store.networkUpgrade.Keys | Where-Object {$_ -like 'er_maid_p*'}).Count -eq 4) 'Registration duplicated upgrades.'
# v0.7 D: default hidden, story-gated rows, and one-time repair of native unlocks.
Assert (@($store.networkUpgrade.Values | Where-Object {$_.id -like 'er_maid_p*' -and !$_.hidden}).Count -eq 0) 'A maid project was visible before the attendant line.'
$cash=$store.playerCash
Assert (![EleanorRomance.MaidProjects]::TryPurchase('er_maid_p1_cash') -and $store.playerCash -eq $cash) 'A pre-story purchase attempt was allowed or charged.'
Set-Partner
[EleanorRomance.MaidProjects]::RefreshStates()
Assert ([EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p1_cash')) -and
    ![EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p2_cash'))) 'P1/P2 visibility gate is wrong after the mother is safe.'
[EleanorRomance.MaidProjects]::RefreshStates($true)
Assert ([EleanorRomance.MaidState]::Flag('notified.p1') -and ![EleanorRomance.MaidState]::Flag('notified.p2')) 'Project-open notices did not fire once for P1 only.'
New-MaidRun
$store.networkUpgrade['er_maid_p1_cash'].state=2
$store.networkUpgrade['er_maid_p4_cash'].state=2
$cash=$store.playerCash
[EleanorRomance.MaidProjects]::OnLoaded()
Assert ($store.playerCash -eq $cash+9000 -and $store.networkUpgrade['er_maid_p1_cash'].state -eq 0 -and
    $store.networkUpgrade['er_maid_p4_cash'].state -eq 0 -and
    [EleanorRomance.MaidState]::Flag('repair.er_maid_p1_cash') -and
    [EleanorRomance.MaidState]::Flag('repair.er_maid_p4_cash')) 'Premature native unlocks were not revoked/refunded to the quoted cash.'
$cash=$store.playerCash
[EleanorRomance.MaidProjects]::OnLoaded()
Assert ($store.playerCash -eq $cash) 'Premature-unlock repair refunded twice.'
'PASS: v0.7 project default-hidden visibility, P1 notice, and one-time abnormal-unlock repair.'
New-MaidRun
Set-Partner
$station.dayCounter=60
$store.wildFavor=400
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p1_cash')) 'P1 payment failed.'
Assert ($store.playerCash -eq 19400 -and $store.wildFavor -eq 0) 'Native favor/cash split is wrong.'
Assert (![EleanorRomance.MaidProjects]::TryPurchase('er_maid_p1_cash') -and $store.playerCash -eq 19400) 'P1 paid twice.'
# Exercise the real deferred hook with a ready store, then the post-restore phase.
# This also represents restarting after an already-paid project was saved.
$paidDay=[EleanorRomance.MaidProjects]::PaidDay(1)
[EleanorRomance.MaidProjects]::Unregister()
$store.networkUpgrade.Clear()
$entry=[EleanorRomance.ModEntry]::new();$entry.OnEnable()
Assert ($store.networkUpgrade.Count -eq 0) 'Menu enable registered projects before the load hook.'
$initHook=([ModHook]).GetField('OnGameLoadedInit',[Reflection.BindingFlags]'Public,NonPublic,Static').GetValue($null)
$initHook.Invoke()
Assert ($store.networkUpgrade.Count -eq 4) 'Load-init did not register all definitions before restore.'
[EleanorRomance.MaidProjects]::OnLoaded()
$initHook.Invoke()
Assert (@($store.networkUpgrade.Keys | Where-Object {$_ -like 'er_maid_p*'}).Count -eq 4 -and $store.networkUpgrade['er_maid_p1_cash'].state -eq 2) 'Repeated load registration lost the paid state or duplicated definitions.'
Assert ([EleanorRomance.MaidProjects]::Paid(1) -and [EleanorRomance.MaidProjects]::PaidDay(1) -eq $paidDay -and
    $store.playerCash -eq 19400 -and $store.wildFavor -eq 0) 'Deferred registration changed payment history or charged again.'
$entry.OnDisable()
$store.networkUpgrade.Clear()
[EleanorRomance.StoryDirector]::OnNewGameStarted('default')
Assert (@($store.networkUpgrade.Keys | Where-Object {$_ -like 'er_maid_p*'}).Count -eq 4 -and [EleanorRomance.MaidProjects]::Paid(1) -and $store.playerCash -eq 19400) 'New-game callback failed to register projects safely/idempotently.'
'PASS: ready-store load-init, repeated registration, post-restore paid state, and new-game callback.'
[EleanorRomance.MaidState]::BasicComplete=$true
[EleanorRomance.MaidState]::BasicDay=59
[EleanorRomance.MaidState]::ProgressDay=59
[EleanorRomance.RomanceState]::Chapter=300
[EleanorRomance.RomanceState]::PendingChapter=0
[EleanorRomance.RomanceState]::EpilogueDone=$true
$station.dayCounter=61
Assert ([EleanorRomance.MaidStory]::EventForDay(61) -eq 902) 'Late paid P1 has no independent result producer.'
$null=[EleanorRomance.MaidStory]::TryQueue($store.storeClientManager,61)
$c=$store.storeClientManager.clientStack[0];Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.MaidProjects]::ResultShown(1) -and [EleanorRomance.MaidProjects]::Available(2)) 'Late housing result did not open P2.'
Assert ([EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p2_cash')) -and
    ![EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p2_favor'))) 'P2 did not expose exactly one cash plan.'
Set-Reps 60 30
$rules.upgradePriceMultiplier=1.5
$q=[EleanorRomance.MaidProjects]::GetQuote([EleanorRomance.MaidProjects]::Find('er_maid_p2_cash'))
$before=$store.playerCash
Assert ($q.Total -eq 2250 -and $q.Discount -and [EleanorRomance.MaidProjects]::TryPurchase('er_maid_p2_cash')) 'Native difficulty quote or automatic discount failed.'
Assert ($store.playerCash -eq $before-2250 -and [StoreReputation]::GetLLReputation() -eq 30) 'P2 did not charge the discounted price without spending reputation.'
Assert (![EleanorRomance.MaidProjects]::TryPurchase('er_maid_p2_cash')) 'P2 charged twice.'
Assert ([EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p2_cash')) -and
    ![EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p2_favor')) -and
    ![EleanorRomance.MaidProjects]::Visible([EleanorRomance.MaidProjects]::Find('er_maid_p3_cash'))) 'Settled P2 or future P3 visibility is wrong.'
'PASS: four native upgrades, idempotent registration, native quote/favor split, late P1, automatic P2 discount and zero reputation spending.'

# One missing P4 reputation removes only the discount, never permission to buy.
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=26
Set-Reps 50 49
$before=$store.playerCash
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p4_cash') -and $store.playerCash -eq $before-8000 -and
    [StoreReputation]::GetULReputation() -eq 50 -and [StoreReputation]::GetLLReputation() -eq 49 -and
    ![EleanorRomance.MaidState]::Flag('p4.discount')) 'P4 full-price payment incorrectly required both discount thresholds.'
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=26
Set-Reps 50.25 50.75
$before=$store.playerCash
[PlayerStore]::TestProjectCashHook=[Action]{[PlayerStore]::TestProjectCashHook=$null;throw 'Injected cash observer failure'}
Assert (![EleanorRomance.MaidProjects]::TryPurchase('er_maid_p4_cash')) 'Injected partial failure incorrectly succeeded.'
Assert ($store.playerCash -eq $before -and [StoreReputation]::GetStoreReputation('FACTION_UPPER_LEVEL').GetReputationExact() -eq 50.25 -and
    [StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').GetReputationExact() -eq 50.75 -and ![EleanorRomance.MaidProjects]::Paid(4)) 'Rollback lost fractional reputation or cash.'
[PlayerStore]::TestProjectCashHook=[Action]{$store.SaveGame();[SaveManager]::Save()}.GetNewClosure()
[PlayerStore]::TestSaveCount=0;[SaveManager]::TestSaveCount=0
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p4_cash')) 'P4 retry failed.'
Assert ([PlayerStore]::TestSaveCount -eq 1 -and [SaveManager]::TestSaveCount -eq 0 -and
    [EleanorRomance.MaidState]::Int('tx.phase') -eq 2) 'Reentrant save escaped the atomic boundary.'
[PlayerStore]::TestProjectCashHook=$null
'PASS: P4 full price below discount thresholds, exact rollback/retry, no reputation spending and deferred save boundary.'

# Both native UI entry points call the service, block native second deductions,
# and leave unrelated upgrades untouched.
foreach($uiName in @('MaidLegacyPurchasePatch','MaidNetworkPurchasePatch')){
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true
    [EleanorRomance.MaidState]::Flag('p1.paid',$true)
    [EleanorRomance.MaidState]::Flag('p1.shown',$true)
    $method=([EleanorRomance.MaidProjects].Assembly.GetType('EleanorRomance.'+$uiName)).GetMethod('Prefix',[Reflection.BindingFlags]'NonPublic,Static')
    [object[]]$uiCallArguments=if($uiName -eq 'MaidLegacyPurchasePatch'){@((Blank ([WildUIManager])),'er_maid_p2_cash')}else{@('er_maid_p2_cash')}
    Assert (!$method.Invoke($null,$uiCallArguments)) "$uiName allowed native double charging."
    $cash=$store.playerCash
    Assert ([EleanorRomance.MaidProjects]::Paid(2)) "$uiName did not buy P2."
    $null=$method.Invoke($null,$uiCallArguments)
    Assert ($store.playerCash -eq $cash) "$uiName charged twice."
    $uiCallArguments[-1]='BREWER'
    Assert ($method.Invoke($null,$uiCallArguments)) "$uiName intercepted another upgrade."
}
'PASS: both native UI purchase entry points, repeat clicks and unrelated upgrade passthrough.'

# Real native discount math; reputation and faction perks remain unchanged.
New-MaidRun;Set-Partner
$pillar=Blank ([StoreReputationPerk]);$pillar.perkId='LLPILLAR';$pillar.perkThreshold=50
[StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').perks.Add($pillar)
Set-Reps 60 60
$favorite=Blank ([StartingPerk]);$favorite.id='wild_favorite';$store.startingPerks.Add($favorite)
$q=[EleanorRomance.MaidProjects]::GetQuote([EleanorRomance.MaidProjects]::Find('er_maid_p1_cash'))
Assert ($q.Total -eq 765) 'Native faction and starting-perk discounts were not reused.'
$store.startingPerks.Clear()
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=26
Set-Reps 50 50
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p4_cash') -and $pillar.isUnlocked) 'A cash-only project removed the faction perk.'
[StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').perks.Clear()

# A prepared journal restores only this purchase; a committed journal never recharges.
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::SetInt('tx.project',4)
[EleanorRomance.MaidState]::SetInt('tx.cash',1234)
[EleanorRomance.MaidState]::SetInt('tx.favor',99)
[EleanorRomance.MaidState]::Number('tx.upper',61.25)
[EleanorRomance.MaidState]::Number('tx.lower',62.75)
[EleanorRomance.MaidState]::SetInt('tx.phase',1)
$store.playerCash=234;$store.wildFavor=0
[EleanorRomance.MaidProjects]::Recover()
Assert ($store.playerCash -eq 1234 -and $store.wildFavor -eq 99 -and
    [StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').GetReputationExact() -eq 62.75) 'Prepared transaction recovery failed.'
[EleanorRomance.MaidState]::SetInt('tx.phase',2)
$store.playerCash=55;[EleanorRomance.MaidProjects]::Recover()
Assert ($store.playerCash -eq 55) 'Committed transaction recovery changed money.'
'PASS: native discounts/perk updates, prepared rollback and committed no-recharge recovery.'

function Leave-Trade($client){
    Use-Client $client
    $store.TryPlayerDismissCurrentClient($true)
    Finish-Dialogue $ui.currentDialog
}
function Play-Event($id){
    [EleanorRomance.MaidState]::PendingVisit=$id
    $c=[EleanorRomance.MaidStory]::CreateEvent($id);Assert ($c -ne $null) "Event $id is missing."
    Use-Client $c
    if($id -eq 26){$branch=Pick $c.mainDialogue 0;Finish-Dialogue $branch;Leave-Trade $c}
    elseif($id -eq 24){Finish-Dialogue (Pick $c.mainDialogue 2)}
    elseif($id -in @(25,27)){Finish-Dialogue $c.mainDialogue;Leave-Trade $c}
    elseif($id -eq 28){Finish-Dialogue (Pick $c.mainDialogue 2)}
    else{Finish-Dialogue $c.mainDialogue}
    return $c
}

# Waiting reminders follow the actual project, including different resource
# shortages. They must not purchase anything, advance a chapter, or strand a
# saved waiting visit if the player has since paid.
foreach($project in 1..4){
    $modes=if($project -eq 1){@(0,2)}else{@(0,1,2)}
    foreach($mode in $modes){
        New-MaidRun;Set-Partner
        [EleanorRomance.MaidState]::BasicComplete=$true
        [EleanorRomance.MaidState]::Progress=$(if($project -eq 4){26}elseif($project -eq 3){24}else{23})
        [EleanorRomance.MaidState]::ProgressDay=0
        [EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::PendingChapter=0
        [EleanorRomance.RomanceState]::EpilogueDone=$true
        if($project -gt 1){[EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::Flag('p1.shown',$true)}
        $store.playerCash=$(if($mode -eq 2){20000}else{0});$store.wildFavor=0
        if($mode -eq 1){Set-Reps 0 0}
        [EleanorRomance.MaidState]::SetInt('wait_index',$mode)
        $station.dayCounter=80
        Assert ([EleanorRomance.MaidStory]::EventForDay(80) -eq 906) "Project $project did not offer its due waiting visit."
        $cash=$store.playerCash;$progress=[EleanorRomance.MaidState]::Progress
        [EleanorRomance.MaidState]::PendingVisit=906
        $c=[EleanorRomance.MaidStory]::CreateEvent(906)
        Assert ($c.mainDialogue.title -eq [EleanorRomance.L]::Eleanor -and
            $c.mainDialogue.plainText -eq "er_maid_wait_project_$project") "Wrong project/speaker in waiting reminder $project."
        Finish-Dialogue $c.mainDialogue
        Assert ($store.playerCash -eq $cash -and ![EleanorRomance.MaidProjects]::Paid($project) -and
            [EleanorRomance.MaidState]::Progress -eq $progress -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Waiting reminder changed payment/progression or failed to complete.'
    }
}
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=24
[EleanorRomance.MaidState]::ProgressDay=70
[EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::PendingChapter=0
[EleanorRomance.RomanceState]::EpilogueDone=$true
[EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::Flag('p1.shown',$true)
[EleanorRomance.MaidState]::Flag('p3.paid',$true);[EleanorRomance.MaidState]::SetInt('p3.day',75)
$station.dayCounter=80;$cash=$store.playerCash
$null=Play-Event 906
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 0 -and $store.playerCash -eq $cash -and
    [EleanorRomance.MaidStory]::EventForDay(81) -eq 25) 'A saved waiting visit stranded its already-paid result.'
'PASS: four project reminders, cash/connections/preparation variants, unchanged resources and saved paid-visit continuation.'

# Full new attendant base route B through the five return visits.
New-MaidRun
[EleanorRomance.RomanceState]::Savings=800
$station.dayCounter=39
$c=Main-Client 12;Finish-Dialogue (Pick (Pick $c.mainDialogue 1) 0)
$station.dayCounter=41;$c=Main-Client 13;Finish-Dialogue (Pick $c.mainDialogue 1)
$station.dayCounter=43;$c=Main-Client 14;Finish-Dialogue (Pick $c.mainDialogue 2)
$station.dayCounter=44;$c=Main-Client 15;Finish-Dialogue (Pick $c.mainDialogue 1)
$station.dayCounter=45;$c=Main-Client 16;Finish-Dialogue (Pick $c.mainDialogue 1)
$station.dayCounter=46;$c=Main-Client 17;Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.RomanceState]::MotherState -eq 1 -and [EleanorRomance.RomanceState]::DriveState -eq 4) 'Protection route did not reach confirmed safety and research delivery.'
$null=[EleanorRomance.MaidProjects]::TryPurchase('er_maid_p1_cash')
$station.dayCounter=48;$c=Main-Client 18;Leave-Trade $c
$station.dayCounter=50;$c=Main-Client 19
$item=New-Item '1' 1;$item.unitValue=100;$c.onItemSold.Invoke($item,110)
Leave-Trade $c
Assert (![EleanorRomance.MaidState]::DebutShown -and ![EleanorRomance.MaidState]::AddressAccepted -and
    [EleanorRomance.MaidProjects]::ResultShown(1)) 'Work visit prematurely established the identity or lost housing feedback.'
$station.dayCounter=52;$c=Main-Client 20;Finish-Dialogue $c.mainDialogue;Leave-Trade $c
Assert (![EleanorRomance.RomanceState]::RomanceConfirmed) 'Chapter 19 prematurely confirmed love.'
Assert ([EleanorRomance.MaidState]::DebutShown -and [EleanorRomance.MaidState]::AddressAccepted) 'First address/maid debut was not committed in chapter 19.'
$station.dayCounter=54;$c=Main-Client 21;Finish-Dialogue (Pick $c.mainDialogue 0)
Assert ([EleanorRomance.MaidState]::Lovers -and [EleanorRomance.MaidState]::BuyerState -eq 1 -and
    ![EleanorRomance.MaidState]::BuyerFeedbackShown) 'Confession or real buyer invitation failed.'
$station.dayCounter=55;$null=Play-Event 903
Assert ([EleanorRomance.MaidState]::BuyerState -eq 1) 'Buyer was counted as departed merely by creating it.'
$c=[EleanorRomance.MaidStory]::CreateEvent(903);[EleanorRomance.MaidState]::PendingVisit=903
$c.onItemBought.Invoke($item,100);Leave-Trade $c
Assert ([EleanorRomance.MaidState]::BuyerState -eq 3) 'Actual buyer trade/leave did not produce feedback.'
$station.dayCounter=56;$c=Main-Client 22;Leave-Trade $c
$station.dayCounter=58;$c=Main-Client 23
$gift=Pick $c.mainDialogue 1
[GraphUtils]::TestRemaining=1
$menu=Choice-Node $gift
$choice=$menu.choices[0];$choice.Item3.Invoke()
Assert (![EleanorRomance.RomanceState]::LocketGiven) 'Full inventory consumed the locket.'
[GraphUtils]::TestRemaining=0
Finish-Dialogue (Pick $gift 0)
Assert ([EleanorRomance.RomanceState]::LocketGiven -and ![EleanorRomance.RomanceState]::LocketWithEleanor -and
    [EleanorRomance.MaidState]::BuyerFeedbackShown) 'Locket retry or actual buyer feedback failed.'
$station.dayCounter=60;$c=Main-Client 24;Finish-Dialogue (Pick $c.mainDialogue 0)
Assert ([EleanorRomance.MaidState]::BasicComplete -and [EleanorRomance.RomanceState]::PerfumeGiven -and
    [EleanorRomance.RomanceState]::ScavAssist -and [EleanorRomance.MaidWorldEvents]::State -eq 1) 'Basic rewards or B receipt failed.'
$receiptDay=[EleanorRomance.MaidState]::Int('world.submitted_day')
$perfume=[GraphUtils]::TestLastGift
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p2_cash')) 'P2 did not open after base conclusion.'
Assert ([EleanorRomance.MaidStory]::EventForDay(61) -ne 24 -and [EleanorRomance.MaidStory]::EventForDay(62) -eq 24) 'Chapter 24 relative delay is wrong.'
$station.dayCounter=62;$null=Play-Event 24
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p3_cash')) 'P3 did not open.'
$station.dayCounter=65;$null=Play-Event 25
$station.dayCounter=67;$null=Play-Event 26
Assert ([EleanorRomance.MaidProjects]::TryPurchase('er_maid_p4_cash')) 'P4 did not open after procurement visit.'
$station.dayCounter=70;$null=Play-Event 27
Assert (![EleanorRomance.MaidState]::Flag('mother_at_27')) 'P4 payment incorrectly acquitted the mother.'
Assert ([EleanorRomance.MaidStory]::EventForDay(71) -ne 28 -and [EleanorRomance.MaidStory]::EventForDay(72) -eq 28) 'Final visit delay is wrong.'
$station.dayCounter=72;$null=Play-Event 28
Assert ([EleanorRomance.MaidState]::Progress -eq 28 -and [EleanorRomance.MaidState]::Contract -eq 3 -and
    [object]::ReferenceEquals($perfume,[GraphUtils]::TestLastGift)) 'Advanced conclusion failed or duplicated the base reward.'
Assert ([EleanorRomance.MaidState]::Int('world.due_day') -eq $receiptDay+7) 'Projects changed the world review date.'
'PASS: new entry, route B rescue, native trades, confession, real buyer, full-storage locket retry, base rewards and all five return visits.'

# Every response in chapter 28 succeeds without charging or granting a second gift.
foreach($answer in 0..2){
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=27
    [EleanorRomance.MaidState]::PendingVisit=28
    $cash=$store.playerCash;$c=[EleanorRomance.MaidStory]::CreateEvent(28)
    Finish-Dialogue (Pick $c.mainDialogue $answer)
    Assert ([EleanorRomance.MaidState]::Progress -eq 28 -and [EleanorRomance.MaidState]::Lovers -and
        $store.playerCash -eq $cash) "Final response $answer was not a successful free conclusion."
}
# Three responses now confirm the already chosen attendant romance.
foreach($answer in 0..2){
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::Confession=0;[EleanorRomance.RomanceState]::Romance=0
    [EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::SetInt('p1.day',38)
    $c=Main-Client 21;Finish-Dialogue (Pick $c.mainDialogue $answer)
    Assert ([EleanorRomance.MaidState]::Confession -eq 1 -and [EleanorRomance.MaidState]::Lovers) 'A confession tone changed the selected relationship.'
}
# v0.7 removal of "let me think again": a deferred save migrates once to the attendant romance.
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::Confession=3;[EleanorRomance.RomanceState]::Romance=2
[EleanorRomance.RomanceState]::Chapter=23
[EleanorRomance.MaidRouteMigration]::MigrateDeferred()
Assert ([EleanorRomance.MaidState]::Confession -eq 1 -and [EleanorRomance.RomanceState]::Romance -eq 1 -and
    [EleanorRomance.MaidState]::Flag('migrated.deferred') -and [EleanorRomance.MaidState]::Lovers) 'Deferred save was not migrated to the attendant romance.'
[EleanorRomance.MaidRouteMigration]::MigrateDeferred()
Assert ([EleanorRomance.MaidState]::Confession -eq 1 -and [EleanorRomance.RomanceState]::Romance -eq 1) 'Deferred migration was not idempotent.'
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::Confession=3;[EleanorRomance.RomanceState]::Romance=2
[EleanorRomance.RomanceState]::Chapter=22
[EleanorRomance.MaidRouteMigration]::MigrateDeferred()
Assert ([EleanorRomance.MaidState]::Flag('migrated.deferred') -and
    ![EleanorRomance.MaidState]::Flag('migrated.lydia_deferred')) 'Migration before reading chapter 22 wrongly skipped Lydia''s normal conversation.'
'PASS: all final answers and three attendant confession responses, with unchanged deferred migration.'

# World event is independent of maid history and purchases. No historical retroactive receipt.
foreach($romance in @(1,3)){
    New-MaidRun
    [EleanorRomance.RomanceState]::Romance=$romance
    [EleanorRomance.RomanceState]::Route=2;[EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::ProtectionReady=$true;[EleanorRomance.RomanceState]::DriveState=4
    $station.dayCounter=59;$c=Main-Client 24;Finish-Dialogue (Pick $c.mainDialogue 0)
    Assert ([EleanorRomance.MaidWorldEvents]::State -eq 1 -and ![EleanorRomance.MaidState]::HasHistory) 'Original B relationship missed its receipt.'
    $due=[EleanorRomance.MaidState]::Int('world.due_day')
    $events=[StoreEventManager]::new();$station.storeEventManager=$events
    $station.dayCounter=$due-1;[EleanorRomance.MaidWorldEvents]::TickNews($events)
    Assert (![EleanorRomance.MaidWorldEvents]::Acquitted) 'News was published early.'
    $urgent=[StoreEvent]::new();$urgent.identifier='test_urgent';$urgent.IsPriority=$true;$urgent.importance=10
    $station.dayCounter=$due
    $events.ActivateEvent($urgent,$false,$false)
    $station.dayCounter=$due;[EleanorRomance.MaidWorldEvents]::TickNews($events)
    Assert ([EleanorRomance.MaidWorldEvents]::State -eq 2 -and ![EleanorRomance.MaidWorldEvents]::Acquitted) 'Urgent news did not postpone the review result.'
    $events.RemoveAllEvent('test_urgent');$station.dayCounter++
    [EleanorRomance.MaidWorldEvents]::TickNews($events)
    Assert ([EleanorRomance.MaidWorldEvents]::Acquitted -and $events.IsEventActive('er_maid_mother_acquitted')) 'Native newspaper did not publish the review result.'
    [EleanorRomance.MaidWorldEvents]::TickNews($events)
    Assert ($events.GetActiveEvents().Count -eq 1 -and [EleanorRomance.MaidState]::Int('world.due_day') -eq $due) 'News replayed or reset its date.'
    $null=Play-Event 905
    Assert (![EleanorRomance.MaidWorldEvents]::ChatPending) 'World result short conversation did not commit once.'
}
New-MaidRun
[EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::EpilogueDone=$true
[EleanorRomance.RomanceState]::Route=2;[EleanorRomance.RomanceState]::MotherState=1
[EleanorRomance.RomanceState]::ProtectionReady=$true;[EleanorRomance.RomanceState]::DriveState=4
$station.dayCounter=99;[EleanorRomance.MaidWorldEvents]::TickNews($station.storeEventManager)
Assert ([EleanorRomance.MaidWorldEvents]::State -eq 0) 'Old completed B save was retrospectively submitted.'
'PASS: original B romance/friend receipt, exact seven-day wait, urgent-news delay, native publication, dedupe and no retrospective submission.'

# Mother attendance reads news, not money. Story C/D keeps its native outcomes.
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=26
[EleanorRomance.MaidWorldEvents]::State=3
$null=Play-Event 27
Assert ([EleanorRomance.MaidState]::Flag('mother_at_27')) 'Published acquittal did not allow mother attendance.'
foreach($legacy in @($false,$true)){
    New-MaidRun
    [EleanorRomance.MaidState]::Enter($(if($legacy){2}else{1}))
    [EleanorRomance.RomanceState]::Route=3
    foreach($code in 101..108){
        $c=Main-Client $code
        if($c.clientIntent -eq [StoreClient+ClientIntent]::BUY){Leave-Trade $c}else{Finish-Dialogue $c.mainDialogue}
    }
    Assert ([EleanorRomance.RomanceState]::MotherState -eq 2 -and [EleanorRomance.RomanceState]::WorkState -eq 3 -and
        [EleanorRomance.RomanceState]::Chapter -eq 300 -and ![EleanorRomance.MaidState]::Active) 'C route failed or retained the contract.'
}
foreach($kind in 1..4){
    New-MaidRun
    [EleanorRomance.MaidState]::Enter(1);[EleanorRomance.MaidState]::End('test',4)
    [EleanorRomance.RomanceState]::Route=4;[EleanorRomance.RomanceState]::BetrayalKind=$kind
    $c=Main-Client 201;Finish-Dialogue $c.mainDialogue
    $expected=if($kind -eq 1){3}elseif($kind -eq 4){0}else{2}
    Assert ([EleanorRomance.RomanceState]::MotherState -eq $expected -and
        [EleanorRomance.RomanceState]::Chapter -eq 300) "D route $kind changed actual world facts."
}
'PASS: attendance from actual news, both C payment histories, and four D/withdrawal outcomes.'

# Route A and protection fallback preserve the original confirmed exchange.
foreach($fallback in @($false,$true)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.RomanceState]::DriveState=2
    [EleanorRomance.RomanceState]::Route=$(if($fallback){2}else{1})
    $c=Main-Client 16
    if($fallback){Finish-Dialogue (Pick $c.mainDialogue 3)}else{Finish-Dialogue $c.mainDialogue}
    $c=Main-Client 17;$before=$store.playerCash
    Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.RomanceState]::Route -eq 1 -and [EleanorRomance.RomanceState]::MotherState -eq 1 -and
        [EleanorRomance.RomanceState]::DriveState -eq 3 -and $store.playerCash -eq $before -and
        [EleanorRomance.MaidProjects]::Available(1)) 'A/fallback changed the exchange or the v0.7 no-compensation response.'
}

# Whitelist, current and queued saved keys, missing art and optional mod toggles.
New-MaidRun;Set-Partner
$own=Blank ([LoadedMod]);$own.Manifest=[ModManifest]::new();$own.Manifest.ID='eleanor_romance';$own.IsEnabled=$true
$beauty=Blank ([LoadedMod]);$beauty.Manifest=[ModManifest]::new();$beauty.Manifest.ID='cute_portraits';$beauty.IsEnabled=$true
$own.Sprites=[System.Collections.Generic.Dictionary[string,UnityEngine.Sprite]]::new()
$sprite=Blank ([UnityEngine.Sprite])
([UnityEngine.Object]).GetField('m_CachedPtr',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($sprite,[IntPtr]1)
$own.Sprites['er_portrait_eleanor_maid_female_beauty']=$sprite
$vanillaSprite=Blank ([UnityEngine.Sprite])
([UnityEngine.Object]).GetField('m_CachedPtr',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($vanillaSprite,[IntPtr]2)
$own.Sprites['er_portrait_eleanor_maid_female_vanilla']=$vanillaSprite
[ModLoader]::TestMods=[System.Collections.Generic.List[LoadedMod]]::new();[ModLoader]::TestMods.Add($own);[ModLoader]::TestMods.Add($beauty)
[SpriteDict]::TestInstance=Blank ([SpriteDict]);[SpriteDict]::TestInstance.spriteDictionary=[System.Collections.Generic.Dictionary[string,UnityEngine.Sprite]]::new()
([UnityEngine.Object]).GetField('m_CachedPtr',[Reflection.BindingFlags]'NonPublic,Instance').SetValue([SpriteDict]::TestInstance,[IntPtr]1)
[EleanorRomance.PortraitCompat]::Refresh()
Assert (![EleanorRomance.PortraitCompat]::CanUseMaid('er_story_maid_m18')) 'Maid art leaked into the earlier work visit.'
Assert ([EleanorRomance.PortraitCompat]::CanUseMaid('er_story_maid_m19')) 'Delayed debut requires its own already-played flag.'
Assert (![EleanorRomance.PortraitCompat]::CanUseMaid('er_story_maid_m20')) 'Post-debut art leaked before the debut.'
[EleanorRomance.MaidState]::DebutShown=$true
Assert ([EleanorRomance.PortraitCompat]::ResolveMaid('er_story_maid_m19','eleanor_scav') -eq 'er_portrait_eleanor_maid_female_beauty') 'Valid private scene did not resolve local art.'
foreach($scene in @('er_story_maid_m17','er_story_maid_m21','er_story_maid_m26','er_story_maid_m12','er_story_maid_m24','er_story_maid_m25','er_story_maid_m27','er_story_maid_exit')){
    Assert (![EleanorRomance.PortraitCompat]::CanUseMaid($scene)) "Maid art leaked into $scene."
}
$queued=[StoreClient]::new();$queued.identifier='er_story_maid_m19';$queued.spriteName='er_portrait_eleanor_maid_female_beauty';$store.storeClientManager.clientStack.Add($queued)
$current=[StoreClient]::new();$current.identifier='er_story_maid_m28';$current.spriteName='er_portrait_eleanor_maid_female_beauty';Use-Client $current
$beauty.IsEnabled=$false
[EleanorRomance.PortraitCompat]::Refresh();[EleanorRomance.PortraitCompat]::SanitizeLoadedClients()
Assert ($queued.spriteName -eq 'er_portrait_eleanor_maid_female_vanilla' -and $current.spriteName -eq 'er_portrait_eleanor_maid_female_vanilla') 'Disabled optional mod did not switch saved maid visitors to vanilla maid art.'
Assert ([EleanorRomance.PortraitCompat]::CanUseMaid('er_story_maid_m19') -and
    [EleanorRomance.PortraitCompat]::ResolveMaid('er_story_maid_m19','eleanor_scav') -eq 'er_portrait_eleanor_maid_female_vanilla') 'Vanilla maid art still depends on the beauty mod.'
$debut=[EleanorRomance.EleanorStory]::CreateClient(20)
Assert ($debut.spriteName -eq 'er_portrait_eleanor_maid_female_vanilla') 'Actual debut factory did not select the vanilla maid portrait.'
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh();[EleanorRomance.PortraitCompat]::SanitizeLoadedClients()
Assert ($current.spriteName -eq 'er_portrait_eleanor_maid_female_vanilla') 'Re-enabling replaced an existing vanilla maid visitor.'
Assert ([EleanorRomance.PortraitCompat]::ResolveMaid('er_story_maid_m19','eleanor_scav') -eq 'er_portrait_eleanor_maid_female_beauty') 'New private visitor did not select the re-enabled beauty style.'
$beauty.IsEnabled=$false;[EleanorRomance.PortraitCompat]::Refresh()
$own.Sprites.Remove('er_portrait_eleanor_maid_female_vanilla') | Out-Null
[EleanorRomance.PortraitCompat]::SanitizeLoadedClients()
Assert ($current.spriteName -eq 'eleanor_scav' -and $queued.spriteName -eq 'eleanor_scav') 'Missing vanilla maid art left stale saved keys.'
$beauty.IsEnabled=$true;[EleanorRomance.PortraitCompat]::Refresh()
$own.Sprites.Clear()
Assert ([EleanorRomance.PortraitCompat]::ResolveMaid('er_story_maid_m19','eleanor_scav') -eq 'eleanor_scav') 'Missing local art did not silently fall back.'
'PASS: route A/fallback compensation, both maid styles, actual debut, scene whitelist, saved-key switching, re-enable and missing-art fallback.'

# Endings describe the result actually shown, and delayed projects produce no
# false waiting-for-money scenes during their fixed delivery gap.
New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::BasicDay=59
[EleanorRomance.MaidState]::Progress=23;[EleanorRomance.MaidState]::ProgressDay=59
[EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::Flag('p1.shown',$true)
[EleanorRomance.MaidState]::Flag('p2.paid',$true);[EleanorRomance.MaidState]::SetInt('p2.day',59)
[EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::PendingChapter=0;[EleanorRomance.RomanceState]::EpilogueDone=$true
Assert ([EleanorRomance.MaidStory]::EventForDay(60) -eq 0) 'Paid project waiting for its date triggered a false unpaid chat.'
Assert (![EleanorRomance.MaidStory]::Ending('survived').Contains('er_maid_ending_b002_02')) 'Paid but unshown pass was claimed in the ending.'
[EleanorRomance.MaidState]::Progress=27
Assert ([EleanorRomance.MaidStory]::Ending('survived').Contains('er_maid_ending_b002_05') -and
    ![EleanorRomance.MaidStory]::Ending('survived').Contains('er_maid_ending_b003_01')) 'Chapter 27 falsely claimed the renewed agreement.'
Assert ([EleanorRomance.MaidStory]::Ending('dead').Contains('er_maid_ending_b008_01')) 'Player death used a peaceful ending.'
[EleanorRomance.MaidState]::BasicComplete=$false;[EleanorRomance.RomanceState]::Chapter=22
Assert ([EleanorRomance.MaidStory]::Ending('survived') -ne $null) 'Early game ending lost a mutually confirmed relationship.'
'PASS: actual-stage endings, early confirmed relationship and no unpaid chat during paid delivery gaps.'

New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
[EleanorRomance.RomanceState]::Route=2;[EleanorRomance.RomanceState]::ProtectionChannel=2
$store.playerCash=0
$c=Main-Client 16;Finish-Dialogue $c.mainDialogue
Assert ([EleanorRomance.RomanceState]::ProtectionReady -and $store.playerCash -eq 0) 'Interrupted paid protection was charged or gated again.'
'PASS: already-paid protection channel resumes after interruption without a second charge.'

# Original equal romance from its unchanged entry choice through its conclusion.
New-MaidRun
[EleanorRomance.RomanceState]::Savings=800;[EleanorRomance.RomanceState]::FairDeals=3
[EleanorRomance.RomanceState]::UncleReported=$true
$c=Main-Client 12;Finish-Dialogue (Pick $c.mainDialogue 0)
$c=Main-Client 13;Finish-Dialogue (Pick $c.mainDialogue 1)
$c=Main-Client 14;Finish-Dialogue (Pick $c.mainDialogue 0)
$c=Main-Client 15;Finish-Dialogue (Pick $c.mainDialogue 0)
$c=Main-Client 16;Finish-Dialogue $c.mainDialogue
$c=Main-Client 17;Finish-Dialogue (Pick $c.mainDialogue 1)
$c=Main-Client 18;Leave-Trade $c
$c=Main-Client 19;Leave-Trade $c
$c=Main-Client 20;Finish-Dialogue (Pick $c.mainDialogue 0)
$c=Main-Client 21;Finish-Dialogue $c.mainDialogue
Assert (($store.storeClientManager.clientStack | Where-Object identifier -eq 'er_story_coop_buyer').Count -eq 1) 'Original cooperation buyer did not queue.'
$c=Main-Client 22;Leave-Trade $c
$c=Main-Client 23;Finish-Dialogue (Pick $c.mainDialogue 1)
Finish-Dialogue (Pick $ui.currentDialog 1)
$c=Main-Client 24;Finish-Dialogue (Pick $c.mainDialogue 0)
Assert (![EleanorRomance.MaidState]::HasHistory -and [EleanorRomance.RomanceState]::RomanceConfirmed -and
    [EleanorRomance.RomanceState]::PerfumeGiven -and [EleanorRomance.RomanceState]::ScavAssist -and
    [EleanorRomance.RomanceState]::Chapter -eq 300) 'Original equal romance was changed or blocked.'
'PASS: original equal romance entry, A exchange, native trading, confession, future and base conclusion.'
