# These checks run the installed game's native AfterhourEvents/ScavHelper code.
# Limit the location registry to the landfill for this test, then restore it.
$locations=[AfterhourLocation]::GetAll()
$savedLocations=@($locations)
try {
    $locations.Clear();$locations.Add(($savedLocations | Where-Object Id -eq 'dumping_ground'))
    New-MaidRun
    $store.startingPerks=[System.Collections.Generic.List[StartingPerk]]::new()
    $store.healthData=[HealthData]::new()
    $store.afterhourState=[System.Collections.Generic.Dictionary[string,AfterhourLocationState]]::new()
    $store.scavengingAttempts=0
    [EleanorRomance.ScavAssistance]::Register()
    [EleanorRomance.ScavAssistance]::Register()
    $id='er_eleanor_scav_assistance'
    $def=[AfterhourEvents]::GetDefinition($id)
    Assert ($def.locationId -eq 'dumping_ground' -and $def.durationDays -eq 1 -and $def.dailyChancePercent -eq 70) 'Event location, duration or random frequency is incorrect.'
    Assert ($def.GetTitle() -eq 'er_scav_event_title' -and $def.GetDescription() -eq 'er_scav_event_desc' -and
        $def.GetEffectTexts().Count -eq 2 -and $def.GetTone() -eq [AfterhourEventTone]::Good) 'The native event panel has no visible title/description/effects.'
    Assert (@([AfterhourEvents]::GetAll() | Where-Object id -eq $id).Count -eq 1) 'Repeated registration duplicated the reward.'
    $station.dayCounter=71 # Monday, not the native fixed landfill event day.
    [RNG]::TestScavRollValue=0;[RNG]::TestScavRollCount=0
    [EleanorRomance.RomanceState]::ScavAssist=$false
    [AfterhourEvents]::OnNewDay()
    Assert (![AfterhourEvents]::IsActive($id) -and [RNG]::TestScavRollCount -eq 0 -and [ScavHelper]::GetMaxScavAttempts() -eq 5) 'Unfinished/friend routes received assistance.'
    [EleanorRomance.RomanceState]::ScavAssist=$true
    Assert ($def.canStart.Invoke() -and [ScavHelper]::GetMaxScavAttempts() -eq 5) 'An old unlocked save still received the silent permanent +2.'
    [EleanorRomance.RomanceState]::OldBlocked=$true
    Assert (!$def.canStart.Invoke()) 'Blocked story started a new assistance event.'
    [EleanorRomance.RomanceState]::OldBlocked=$false
    [RNG]::TestScavRollValue=70;[RNG]::TestScavRollCount=0
    [AfterhourEvents]::OnNewDay()
    Assert (![AfterhourEvents]::IsActive($id) -and [RNG]::TestScavRollChance -eq 70 -and [RNG]::TestScavRollCount -eq 1) 'The native random event could not fail at the 70% threshold.'
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 5 -and [ScavHelper]::GetMinorWoundChance() -gt 0 -and [ScavHelper]::GetMajorWoundChance() -gt 0) 'An absent event left bonus attempts or permanent injury immunity.'
    'PASS: native landfill event registration, visible effect model, 70% random failure, old-save eligibility and removal of the permanent buff.'

    $station.dayCounter=72;[RNG]::TestScavRollValue=69.9;[RNG]::TestScavRollCount=0
    [AfterhourEvents]::OnNewDay()
    Assert ([AfterhourEvents]::IsActive($id) -and [AfterhourEvents]::GetEndsOnDay($id) -eq 72 -and [RNG]::TestScavRollCount -eq 1) 'Native scheduling did not start the one-day assistance event.'
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 7 -and [ScavHelper]::GetMinorWoundChance() -eq 0 -and [ScavHelper]::GetMajorWoundChance() -eq 0) 'Native +2/minor/major injury immunity did not apply.'
    [RNG]::TestScavRollValue=0
    Assert (![ScavHelper]::RollMinorWound() -and ![ScavHelper]::RollMajorWound()) 'A zero RNG draw could still injure the player during assistance.'
    $store.scavengingAttempts=5
    Assert ([ScavHelper]::GetScavTimeLeft() -eq 2 -and [ScavHelper]::CanScavenge()) 'The advertised extra attempts could not be used.'
    $store.scavengingAttempts=7
    Assert (![ScavHelper]::CanScavenge()) 'Assistance bypassed the native attempt limit.'
    $store.scavengingAttempts=0
    foreach($key in @('scav_attempts','injury_immunity')){
        Assert ([ModHelper]::GetAfterhourModifier('commissary',$key) -eq 0 -and [ModHelper]::GetAfterhourModifier('dr_jackson',$key) -eq 0) 'Landfill assistance leaked to another location.'
    }
    $nativeState=$store.GetAfterhourState('dumping_ground')
    $restored=[AfterhourLocationState]::new()
    foreach($entry in $nativeState.activeEvents){$restored.activeEvents.Add([AfterhourActiveEvent]@{eventId=$entry.eventId;endsOnDay=$entry.endsOnDay})}
    $store.afterhourState['dumping_ground']=$restored
    $draws=[RNG]::TestScavRollCount
    [EleanorRomance.ScavAssistance]::Unregister()
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 5 -and [AfterhourEvents]::GetActive('dumping_ground').Count -eq 0) 'Disabled event definition still granted benefits.'
    [EleanorRomance.ScavAssistance]::Register()
    Assert ([AfterhourEvents]::GetDaysLeft($id) -eq 1 -and [ScavHelper]::GetMaxScavAttempts() -eq 7 -and [RNG]::TestScavRollCount -eq $draws) 'Restoring saved native event state rerolled or stacked the effect.'
    'PASS: native random success, exactly +2 usable attempts, minor/major injury immunity, location scope, disable/re-enable and saved active-event restoration without reroll.'

    # Wednesday's scheduled Fresh Dump keeps the original scheduler priority.
    $station.dayCounter=73;[RNG]::TestScavRollValue=0;[RNG]::TestScavRollCount=0
    [AfterhourEvents]::OnNewDay()
    Assert (![AfterhourEvents]::IsActive($id) -and [AfterhourEvents]::IsActive('fresh_dump') -and [RNG]::TestScavRollCount -eq 0) 'Assistance displaced the native fixed-day Fresh Dump event or failed to expire.'
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 5 -and [ScavHelper]::GetMinorWoundChance() -gt 5) 'Expired assistance suppressed Fresh Dump injury risk.'
    [AfterhourEvents]::Start($id) | Out-Null # Native debug/manual events may coexist.
    Assert ([AfterhourEvents]::IsActive('fresh_dump') -and [ScavHelper]::GetMaxScavAttempts() -eq 7 -and [ScavHelper]::GetMinorWoundChance() -eq 0 -and [ScavHelper]::GetMajorWoundChance() -eq 0) 'Assistance failed to protect against a simultaneous increased injury risk.'
    [AfterhourEvents]::Start('georgio_assistance') | Out-Null
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 12) 'Assistance replaced instead of adding to another native attempt bonus.'
    $store.startingPerks.Add([StartingPerk]@{id='maimed'})
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 0 -and !$def.canStart.Invoke()) 'Assistance bypassed the maimed restriction.'
    $store.startingPerks.Clear();$store.startingPerks.Add([StartingPerk]@{id='degenerative_osteoarthritis'})
    Assert ([ScavHelper]::GetMaxScavAttempts() -eq 0 -and !$def.canStart.Invoke()) 'Assistance bypassed the native osteoarthritis zero-attempt restriction.'
    'PASS: one-day expiry, original fixed-day priority, simultaneous Fresh Dump/Georgio compatibility, maimed and zero-attempt restrictions.'
}
finally {
    [EleanorRomance.ScavAssistance]::Unregister()
    $locations.Clear();foreach($location in $savedLocations){$locations.Add($location)}
}
