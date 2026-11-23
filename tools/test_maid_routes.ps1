# Attendant fixtures for current Wilde purchases and dividend accounting.
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
    Set-Reps 60 60
    $rules.upgradePriceMultiplier=1
    $store.startingPerks.Clear()
    [EleanorRomance.MaidProjects]::OnLoaded()
}
function Set-Partner {
    [EleanorRomance.MaidV2State]::Enter()
    [EleanorRomance.MaidV2State]::Act2Done=$true
    [EleanorRomance.MaidState]::Confession=1
    [EleanorRomance.RomanceState]::Romance=1
    [EleanorRomance.RomanceState]::Route=2
    [EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::ProtectionReady=$true
    [EleanorRomance.RomanceState]::DriveState=4
}
