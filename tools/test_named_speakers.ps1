# The production speaker adapter runs around the installed native dispatcher
# and recorder. Captions are captured at the sole Unity assignment boundary.
[EleanorRomance.DialogueSpeakers]::Enabled=$true
function Record-Named($line){
    [EleanorRomance.DialogueSpeakers]::TestCaption=$null
    $store.dialogHistory=[System.Collections.Generic.Queue[string]]::new()
    $null=$displayLine.Invoke($ui,@($line))
    return $store.dialogHistory.ToArray()[0]
}
foreach($label in @('旁白','Narration','来宾','Guest','采购行管事','Purchasing Manager','治安部通知','Security Notice','新闻','News')){
    New-MaidRun;Set-Partner
    $client=[StoreClient]::new();$client.identifier='er_story_named_test';$client.displayName='Eleanor';Use-Client $client
    $line=[EleanorRomance.StoryEngine]::Text($label,'the same text')
    $actual=Record-Named $line
    Assert ($actual -eq ($label+': the same text') -and [EleanorRomance.DialogueSpeakers]::TestCaption -eq $label -and $client.displayName -eq 'Eleanor') 'Explicit speaker did not update both caption and recorder, or mutated the customer identity.'
}
'PASS: bilingual narration/guest/manager/notice/news captions and native records use the authored role without changing the actual customer.'

# Real doorway, protection, reunion, tragic and narration graphs, not just labels.
Gala-Run;[EleanorRomance.GalaStory]::Phase=4;[EleanorRomance.MaidState]::PendingVisit=916
$client=[EleanorRomance.GalaStory]::CreateVisit();Use-Client $client
foreach($line in (Motivation-Nodes $client.mainDialogue)){
    $actual=Record-Named $line
    $expected=if($line.title -eq 'PLAYER'){'Player'}else{$line.title}
    Assert ($actual -eq ($expected+': '+$line.plainText)) 'Gala doorway narration was attributed to Eleanor.'
}
foreach($block in @('c15.b004','c16.b004','c16.b005','rc.b002')){
    Reset-Run
    $client=[StoreClient]::new();$client.identifier='er_story_source_fixture';$client.displayName='Eleanor';Use-Client $client
    $root=[EleanorRomance.StoryEngine]::Build($block)
    foreach($line in (Motivation-Nodes $root)){
        $actual=Record-Named $line;$expected=if($line.title -eq 'PLAYER'){'Player'}else{$line.title}
        Assert ($actual -eq ($expected+': '+$line.plainText)) "Notice/news/character attribution failed in $block."
    }
}
'PASS: gala doorway, protection notice, rescue news and route-C news retain each authored speaker through the native dispatcher and recorder.'

# Unmarked foreign lines remain native, even when they share a localized title.
Reset-Run
$client=[StoreClient]::new();$client.identifier='other_mod_customer';$client.sourceModId='other_mod';$client.displayName='Native Customer';Use-Client $client
$foreign=[Dialogue]::new().SetText('er_name_narrator','foreign words')
$actual=Record-Named $foreign
Assert ($actual -eq 'Native Customer: foreign words' -and [string]::IsNullOrEmpty([EleanorRomance.DialogueSpeakers]::TestCaption)) "An unrelated mod/native line was relabeled: history=$actual caption=$([EleanorRomance.DialogueSpeakers]::TestCaption)"
$owned=[EleanorRomance.StoryEngine]::Text('Narration','own words')
[EleanorRomance.DialogueSpeakers]::Enabled=$false
$actual=Record-Named $owned
Assert ($actual -eq 'Native Customer: own words' -and [string]::IsNullOrEmpty([EleanorRomance.DialogueSpeakers]::TestCaption)) 'Disabled mod still changed caption/history.'
[EleanorRomance.DialogueSpeakers]::Enabled=$true
Assert ((Record-Named $owned) -eq 'Narration: own words') 'Re-enable lost weak ownership of existing lines.'
$client.sourceModId='eleanor_romance'
$saved=[Dialogue]::new().SetText('er_name_narrator','restored line')
Assert ((Record-Named $saved) -eq 'er_name_narrator: restored line') 'Verified older mod graph could not recover a known role.'
$unknown=[Dialogue]::new().SetText('Foreign Custom Name','unmarked line')
Assert ((Record-Named $unknown) -eq 'Native Customer: unmarked line') 'Saved-graph fallback accepted an arbitrary foreign title.'
'PASS: foreign/native isolation, disabled/re-enabled behavior and known-label recovery only for source-verified old mod graphs.'

# Nested renderers, out-of-band history and exceptional cleanup do not leak.
$outer=[EleanorRomance.StoryEngine]::Text('Narration','outer')
$previous=[EleanorRomance.DialogueSpeakers]::Begin($outer)
$client.sourceModId='other_mod'
Assert ((Record-Named $foreign) -eq 'Native Customer: foreign words') 'A nested foreign renderer inherited the mod role.'
$unrelated='<color=#eb4034>Native Customer:</color> different notification'
[EleanorRomance.DialogueSpeakers]::RewriteHistory([ref]$unrelated)
Assert ($unrelated -like '*Native Customer:*') 'An unrelated notification was relabeled within a rendering scope.'
$matching='<color=#eb4034>Native Customer:</color> outer'
[EleanorRomance.DialogueSpeakers]::RewriteHistory([ref]$matching)
Assert ($matching -like '*Narration:*') 'Nested renderer failed to restore its outer scope.'
$failure=[InvalidOperationException]::new('simulated renderer failure')
$finalizer=[EleanorRomance.ModEntry].Assembly.GetType('EleanorRomance.ModDialogueSpeakerPatch').GetMethod('Finalizer',[Reflection.BindingFlags]'NonPublic,Static')
$returned=$finalizer.Invoke($null,@($failure,$previous))
Assert ([object]::ReferenceEquals($returned,$failure)) 'Speaker cleanup swallowed or replaced the native exception.'
$after='<color=#eb4034>Native Customer:</color> outer'
[EleanorRomance.DialogueSpeakers]::RewriteHistory([ref]$after)
Assert ($after -like '*Native Customer:*') 'Failed renderer leaked the previous speaker into later history.'
[EleanorRomance.DialogueSpeakers]::TestCaptionFault=$true
try{Assert ((Record-Named $outer) -eq 'Narration: outer') 'A cosmetic caption failure stopped the native dialogue/record.'}
finally{[EleanorRomance.DialogueSpeakers]::TestCaptionFault=$false}
$after='<color=#eb4034>Native Customer:</color> outer'
[EleanorRomance.DialogueSpeakers]::RewriteHistory([ref]$after)
Assert ($after -like '*Native Customer:*') 'Cosmetic failure left stale speaker context.'
'PASS: nested foreign calls, unrelated log entries and exceptional cleanup cannot leak the mod speaker or suppress native errors.'
