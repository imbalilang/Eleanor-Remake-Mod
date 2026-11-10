# Dot-source after the runtime harness; exercise actual choice actions and save data.
function Fix-Pick($head,$index) {
    $menu=Choice-Node $head
    Assert ($menu -and $menu.IsChoiceEnabledAt($index)) 'Reading-fix choice is unexpectedly unavailable.'
    $choice=$menu.choices[$index]
    if($choice.Item3){$choice.Item3.Invoke()}
    return $choice.Item2
}

# Affordability is both a UI predicate and a selection-time check.
Reset-Run 9
$store.playerCash=20
$c=[EleanorRomance.EleanorStory]::CreateClient(9);Use-Client $c
$menu=Choice-Node $c.mainDialogue
Assert ($menu.IsChoiceEnabledAt(1)) 'Return gift did not allow the exact price.'
$store.playerCash=19
Assert (!$menu.IsChoiceEnabledAt(1)) 'Return gift allowed payment below 20.'
$last=[GraphUtils]::TestLastGift
$menu.choices[1].Item3.Invoke()
Finish-Dialogue $menu.choices[1].Item2
Assert ($store.playerCash -eq 19 -and [object]::ReferenceEquals($last,[GraphUtils]::TestLastGift) -and
    [EleanorRomance.RomanceState]::ReturnGiftChoice -eq 0 -and [EleanorRomance.RomanceState]::Chapter -eq 9) 'Stale payment selection granted a free gift or advanced.'
'PASS: return-gift affordability and selection-time balance recheck.'

foreach($index in @(0,1)) {
    Reset-Run 9
    $store.playerCash=100
    [GraphUtils]::TestRemaining=1
    $c=[EleanorRomance.EleanorStory]::CreateClient(9);Use-Client $c
    $response=Fix-Pick $c.mainDialogue $index
    Finish-Dialogue $response
    Assert ($store.playerCash -eq 100 -and [EleanorRomance.RomanceState]::ReturnGiftChoice -eq 0 -and
        [EleanorRomance.RomanceState]::Chapter -eq 9 -and [PlayerStore]::TestDismissCount -eq 0) 'Full inventory charged, consumed or completed the return gift.'
    Assert ([object]::ReferenceEquals((Choice-Node $response),(Choice-Node $c.mainDialogue))) 'Failed gift did not return to its live menu.'
    [GraphUtils]::TestRemaining=0
    $response=Fix-Pick $response $index
    $received=[GraphUtils]::TestLastGift
    $expectedCash=100-20*$index
    Assert ($null -ne $received -and $store.playerCash -eq $expectedCash -and
        [EleanorRomance.RomanceState]::ReturnGiftChoice -eq $index+1) 'Gift retry did not commit delivery and payment.'
    # Replay an action, then reconstruct from a saved run before the response ends.
    (Choice-Node $c.mainDialogue).choices[$index].Item3.Invoke()
    $snapshot=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new($snapshot)
    $retry=[EleanorRomance.EleanorStory]::CreateClient(9);Use-Client $retry
    Assert ($null -eq (Choice-Node $retry.mainDialogue)) 'Delivered return gift offered another choice after reload.'
    Finish-Dialogue $retry.mainDialogue
    Assert ($store.playerCash -eq $expectedCash -and [object]::ReferenceEquals($received,[GraphUtils]::TestLastGift) -and
        [EleanorRomance.RomanceState]::Chapter -eq 10 -and [PlayerStore]::TestDismissCount -eq 1) 'Gift reload duplicated the reward/payment or lost progression.'
}
'PASS: free/paid return-gift full-inventory retry, refund, saved response and duplicate-action protection.'

Reset-Run 9
$store.playerCash=0
$last=[GraphUtils]::TestLastGift
$c=[EleanorRomance.EleanorStory]::CreateClient(9);Use-Client $c
Finish-Dialogue (Fix-Pick $c.mainDialogue 2)
Assert ([EleanorRomance.RomanceState]::ReturnGiftChoice -eq 3 -and [EleanorRomance.RomanceState]::Chapter -eq 10 -and
    $store.playerCash -eq 0 -and [object]::ReferenceEquals($last,[GraphUtils]::TestLastGift)) 'Declining the return gift changed cash/items or failed to continue.'
'PASS: declining return gift preserves cash/items and advances normally.'

if($Dual) {
    $caseBlocks=@('dr_epilogue_route_b_unsubmitted','dr_epilogue_route_b','dr_epilogue_route_b','dr_epilogue_route_b_acquitted')
    foreach($state in 0..3) {
        Dual-Run;[EleanorRomance.DualState]::Accept(1)
        [EleanorRomance.RomanceState]::Route=2
        [EleanorRomance.MaidWorldEvents]::State=$state
        $c=[EleanorRomance.DualStory]::CreateScene('dual_epilogue')
        $lines=[System.Collections.Generic.List[string]]::new();$node=$c.mainDialogue
        while($node){$lines.Add($node.plainText);$node=$node.nextDialogue}
        foreach($block in @('dr_epilogue_route_b_unsubmitted','dr_epilogue_route_b','dr_epilogue_route_b_acquitted')) {
            $text=[EleanorRomance.DualText]::Plain($block)
            Assert ($lines.Contains($text) -eq ($block -eq $caseBlocks[$state])) 'Route-B epilogue invented case submission/withdrawal or used another status.'
        }
        Assert ([EleanorRomance.MaidWorldEvents]::State -eq $state) 'Reading the epilogue changed case progress.'
    }
    'PASS: route-B epilogue selects unsubmitted/pending/published narration from actual state without mutation.'

    Dual-Run;[EleanorRomance.DualState]::Accept(1)
    $day=[EleanorRomance.DualState]::LastImportantDay
    foreach($id in @('wait_2_1','wait_2_2')) {
        $c=[EleanorRomance.DualStory]::CreateScene($id);Use-Client $c
        $name=if($id -eq 'wait_2_1'){[EleanorRomance.L]::Eleanor}else{[EleanorRomance.L]::Get('er_name_lydia')}
        Assert ($c.displayName -eq $name -and $c.mainDialogue.title -eq $name) 'G2 daily portrait/client and first speaker disagree.'
        Finish-Dialogue $c.mainDialogue
        Assert ([EleanorRomance.DualState]::LastImportantDay -eq $day -and ![EleanorRomance.DualProgression]::Passed(2)) 'G2 mixed-visitor daily advanced a main checkpoint.'
    }
    'PASS: G2 waiting alternates real Eleanor/Lydia clients while preserving main progress.'
}
