# Execute the installed native dispatcher and native recorder, not a model of
# what Dialogue.title is supposed to mean. No real UI or player saves are used.
$displayLine=[DialogUIManager].GetMethod('DisplayLine',[Reflection.BindingFlags]'NonPublic,Instance')
[StoreUIManager]::TestInstance=Blank ([StoreUIManager])
function Render-ArrestHistory($head){
    $store.dialogHistory=[System.Collections.Generic.Queue[string]]::new()
    $node=$head
    while($node){$null=$displayLine.Invoke($ui,@($node));$node=$node.nextDialogue}
    return @($store.dialogHistory.ToArray())
}
foreach($name in @('假叔叔','Fake Uncle')){
    Reset-Run 4
    $c=[EleanorRomance.EleanorStory]::CreateClient(4);$c.displayName=$name;Use-Client $c
    # Reproduce the old input: a localized display name goes through the client
    # channel, even though the sentence is the officer's.
    $old=[Dialogue]::new().SetText('治安部警官','reported stolen goods')
    $history=@(Render-ArrestHistory $old)
    Assert ($history[0] -eq ($name+': reported stolen goods')) "Native old attribution differs: $($history[0])"
    $history=@(Render-ArrestHistory $c.customOnArrestDialogue)
    $officer=[LocHelper]::GetLocalizedClientName('name_security_officer')
    $narrator=[EleanorRomance.L]::Get('er_name_narrator')
    $speakers=@($officer,$name,$officer,$officer,$name,'Player',
        $narrator,$officer,'Player',$officer,$officer,'Player',$officer,$narrator)
    $keys=@(
        'er_v06_c04_b003_01','er_v06_c04_b003_02','er_v06_c04_b003_03',
        'er_v06_c04_b003_04','er_v06_c04_b003_05','er_v06_c04_b003_06',
        'er_v06_c04_n005_01','er_v06_c04_n005_02','er_v06_c04_n005_03',
        'er_v06_c04_n005_04','er_v06_c04_n005_05','er_v06_c04_n005_06',
        'er_v06_c04_n005_07','er_v06_c04_n005_08')
    Assert ($history.Count -eq 14) 'Reported uncle scene lost or duplicated lines.'
    for($i=0;$i -lt 14;$i++){
        Assert ($history[$i] -eq ($speakers[$i]+': '+$keys[$i])) "Wrong actual recorder speaker on uncle line $($i+1): $($history[$i])"
    }
    $c.onReportAction.Invoke();$c.onCaptureAction.Invoke()
    $due=[EleanorRomance.RomanceState]::NextVisitDay
    $c.onCaptureAction.Invoke();$c.OnDismiss.Invoke()
    Assert ([EleanorRomance.RomanceState]::UncleReported -and ![EleanorRomance.RomanceState]::UncleAccepted -and
        [EleanorRomance.RomanceState]::Chapter -eq 5 -and [EleanorRomance.RomanceState]::NextVisitDay -eq $due) 'Speaker correction changed report/capture progression or its dedupe.'
    [PlayerStore]::TestDismissCount=0;[StoreUIManager]::TestSecLeaves=0
    $tail=[EleanorRomance.StoryEngine]::Tail($c.customOnArrestDialogue)
    $tail.ExecuteEndAction();$tail.ExecuteEndAction()
    Assert ([PlayerStore]::TestDismissCount -eq 1 -and [StoreUIManager]::TestSecLeaves -eq 1) 'Arrest dialogue no longer dismisses the client and leaves security exactly once.'
}
'PASS: reproduces old all-client attribution, then records every uncle-arrest line as officer/uncle/player through the native dispatcher; report, progression and arrest exit remain deduplicated.'

foreach($name in @('埃莉诺','Eleanor')){
    Reset-Run
    $c=[StoreClient]::new();$c.identifier='er_story_speaker_fixture';$c.displayName=$name;Use-Client $c
    $factory=[EleanorRomance.EleanorStory].GetMethod('BuildCrateArrestDialogue',[Reflection.BindingFlags]'NonPublic,Static')
    $head=$factory.Invoke($null,@())
    $history=@(Render-ArrestHistory $head)
    $officer=[LocHelper]::GetLocalizedClientName('name_security_officer')
    $speakers=@($officer,$name,$officer,$name)
    Assert ($history.Count -eq 4) 'Crate-arrest dialogue lost lines.'
    for($i=0;$i -lt 4;$i++){
        Assert ($history[$i] -eq ($speakers[$i]+': er_v06_c05_b007_'+($i+1).ToString('00'))) 'Crate arrest also attributed the officer to Eleanor.'
    }
}
Assert ([EleanorRomance.StoryEngine]::SpeakerName([EleanorRomance.StorySpeaker]::Player) -eq 'PLAYER' -and
    [EleanorRomance.StoryEngine]::SpeakerName([EleanorRomance.StorySpeaker]::Officer) -eq 'SEC') 'Native speaker control tokens changed with names/localization.'
'PASS: shared crate-arrest dialogue keeps officer/Eleanor attribution in the native recorder, with PLAYER and SEC independent of localized names.'
