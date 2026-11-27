# Full route scheduling after the common-line handoff. Rendering/night UI stays
# at the same in-memory boundaries as ReleaseQA; route state is never pre-completed.
$closingType = [EleanorRomance.DualState].Assembly.GetType('EleanorRomance.DualClosingDay')
$closingType.GetProperty('Enabled',[Reflection.BindingFlags]'Static,NonPublic').SetValue($null,$true)
foreach($early in @($false,$true)){
    foreach($kind in @('love','maid','rev','dual')){
        $worlds=if($kind -in @('love','maid','dual')){@(1,2)}else{@(2)}
        foreach($world in $worlds){
            $seed= switch($kind){love{21}maid{13}rev{280}dual{20}}
            Reset-Modern $kind $seed
            if($kind -eq 'love'){
                # Exercise the actual mother/reunion/confession handoff. Advancing
                # chapter 20 reserves chapter 21's day before LoveDirector takes over.
                Reset-Modern common 18
                [EleanorRomance.LoveState]::Reset()
                [EleanorRomance.RomanceState]::Romance=0
                [EleanorRomance.RomanceState]::MotherVisited=$false
                [EleanorRomance.RomanceState]::ProtectionReady=($world -eq 2)
                [EleanorRomance.RomanceState]::FairDeals=3
                [EleanorRomance.RomanceState]::UncleReported=$true
                # The bracelet outcome keeps this run on the common line.
                [EleanorRomance.RomanceState]::DPreludeState=2
            }
            if($kind -eq 'maid'){
                [EleanorRomance.RomanceState]::Romance=0
                [EleanorRomance.MaidState]::Confession=0
                # Project purchase/resource checks run in -Maid; this isolates
                # subsequent progression with the required project already paid.
                [EleanorRomance.MaidState]::Flag('p2.paid',$true)
            }
            if($kind -eq 'dual'){
                # The new bracelet-qualified route requires both actual common
                # visits. Do not pre-complete entry at its old day-49 floor.
                Reset-Modern common 18
                [EleanorRomance.RomanceState]::Romance=0
                [EleanorRomance.RomanceState]::MotherVisited=$false
                [EleanorRomance.RomanceState]::ProtectionReady=($world -eq 2)
                [EleanorRomance.RomanceState]::FairDeals=3
                [EleanorRomance.RomanceState]::UncleReported=$true
                [EleanorRomance.RomanceState]::DPreludeState=5
            }
            if($kind -in @('love','dual')){[EleanorRomance.RomanceState]::Route=$world}
            [EleanorRomance.RomanceState]::EarlyStart=$early
            [EleanorRomance.RomanceState]::PendingChapter=0
            [EleanorRomance.RomanceState]::NextVisitDay=1
            [EleanorRomance.RomanceState]::LastQueuedDay=-1
            [EleanorRomance.MaidState]::LastImportantDay=-1
            $offset=if($early){11}else{0}
            $first=switch($kind){love{48}maid{41}rev{46}dual{48}}
            $first-=$offset
            $count=0;$finished=$false;$schedule=[System.Collections.Generic.List[string]]::new()
            for($day=$first;$day -le (110-$offset) -and !$finished;$day++){
                $station.dayCounter=$day
                for($tick=0;$tick -lt 3 -and !$finished;$tick++){
                    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
                    if($store.storeClientManager.clientStack.Count -eq 0){continue}
                    Assert ($store.storeClientManager.clientStack.Count -eq 1) 'Scheduled two visitors at once'
                    $client=$store.storeClientManager.clientStack[0]
                    $store.storeClientManager.clientStack.Clear();Use-Client $client
                    $schedule.Add("${day}:$($client.identifier)")
                    $path=if($kind -eq 'maid' -and [EleanorRomance.RomanceState]::PendingChapter -eq 15){
                        if($world -eq 1){'0'}else{'1'}
                    }else{'0'}
                    Play-Path $client.mainDialogue $path
                    if($client.tryInterceptDismiss){
                        $store.TryPlayerDismissCurrentClient($true);Play-Path $ui.currentDialog
                    }
                    $store.currentClientInstance=$null
                    $count++
                    $finished=switch($kind){
                        love {[EleanorRomance.LoveState]::Act3Done}
                        maid {[EleanorRomance.MaidState]::Progress -eq 28}
                        rev {[EleanorRomance.RevolutionV2State]::Progress -eq 290}
                        dual {[EleanorRomance.DualState]::Done('dv13')}
                    }
                }
            }
            $expected=switch($kind){love{13}maid{if($world -eq 1){14}else{15}}rev{11}dual{16}}
            Assert ($finished -and $count -eq $expected) "$kind scheduling stalled or replayed scenes ($count): $($schedule -join ', ')"
            if($kind -eq 'love'){
                Assert ($schedule[3] -like "$((54-$offset)):er_story_love_a1_01" -and
                    $schedule[12] -like "$((69-$offset)):er_story_love_a3_04") "Love handoff or wedding mistimed: $($schedule -join ', ')"
            }
            if($kind -eq 'dual'){
                Assert ($schedule[2] -like "$((51-$offset)):er_story_dual_d01" -and
                    $schedule[15] -like "$((67-$offset)):er_story_dual_dv13") "Harem handoff schedule: $($schedule -join ', ')"
            }
            "SCHEDULE: $kind | early=$early | world=$world | $($schedule -join ', ')"
        }
    }
}
'PASS: all four route schedules reach their finale, both starting modes and both love/maid/harem world outcomes.'
