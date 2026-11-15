foreach($supply in 0..2){foreach($answer in 0..2){
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true
    [EleanorRomance.MaidState]::Progress=23
    [EleanorRomance.MaidState]::PendingVisit=24
    [EleanorRomance.MaidState]::Flag('p2.paid',$true)
    $client=[EleanorRomance.MaidStory]::CreateEvent(24);Use-Client $client
    $branch=Pick $client.mainDialogue $supply
    if($supply -eq 2){$closing=$branch}
    else{
        Finish-Dialogue $branch
        [StoreClientInstance]::TestHasStock=$false
        $client.TestOnDealAccepted($null)
        $closing=$ui.currentDialog
    }
    $prompt=Choice-Node $closing
    Assert ($prompt -ne $null -and $prompt.choices.Count -eq 3) 'Outfit response menu is missing.'
    Finish-Dialogue $closing
    Assert ([PlayerStore]::TestDismissCount -eq 0 -and [EleanorRomance.MaidState]::PendingVisit -eq 24 -and
        [EleanorRomance.MaidState]::Progress -eq 23 -and ![EleanorRomance.MaidProjects]::ResultShown(2)) 'Outfit prompt dismissed Eleanor or committed before an answer.'
    Finish-Dialogue (Pick $prompt $answer)
    Finish-Dialogue (Pick $prompt $answer)
    Assert ([PlayerStore]::TestDismissCount -eq 1 -and [EleanorRomance.MaidState]::PendingVisit -eq 0 -and
        [EleanorRomance.MaidState]::Progress -eq 24 -and [EleanorRomance.MaidProjects]::ResultShown(2)) 'Outfit answer failed to complete once.'
}}
'PASS: all three supply paths and all three outfit answers wait at the prompt; completion/departure follows the chosen reply.'

New-MaidRun;Set-Partner
$client=[EleanorRomance.MaidStory]::CreateEvent(26);Use-Client $client
$node=$client.mainDialogue
$count=0
foreach($line in (Motivation-Nodes $node)){
    if($line.plainText -like 'er_maid_m26_*' -and $line.title -ne 'PLAYER'){
        Assert ($line.title -eq 'er_name_upper_buyer') 'Upper Level buyer resolves to the nonexistent er_name_upperbuyer key.'
        $actual=Record-Named $line
        Assert ($actual -eq ('er_name_upper_buyer: '+$line.plainText) -and
            [EleanorRomance.DialogueSpeakers]::TestCaption -eq 'er_name_upper_buyer') 'Buyer caption or native history has the wrong name.'
        $count++
    }
}
Assert ($count -ge 14) 'Procurement opening/answers did not cover all buyer lines.'
'PASS: all 14 Upper Level buyer lines use the existing localized name in captions and native history.'

New-MaidRun;Set-Partner
[EleanorRomance.MaidState]::PendingVisit=27
$client=[EleanorRomance.MaidStory]::CreateEvent(27);Use-Client $client
$client.OnArrival.Invoke()
Assert ((($client.clientBuyingTagList | Sort-Object) -join ',') -eq 'HOUSEHOLD_GOOD,MATERIAL,TOOL') '900-credit restocking tablet is missing its three buying interests.'
foreach($tag in @('TOOL','MATERIAL','HOUSEHOLD_GOOD')){
    Assert ([TypeHelper]::ALL_TYPES_LIST.Contains($tag)) "Tablet cannot display buying category $tag."
    $item=Tagged-Item $tag
    Assert ($client.IsClientBuyingThisItem($item)) "Restocking no longer accepts $tag."
    $protected=Tagged-Item $tag;$null=$protected.EnableTag('IMPORTANT_TAG')
    Assert (!$client.IsClientBuyingThisItem($protected)) 'Visible buying interests bypassed protected-item filtering.'
    $stolen=Tagged-Item $tag;[StolenHelper]::SetHeat($stolen,100)
    Assert (!$client.IsClientBuyingThisItem($stolen)) 'Visible buying interests bypassed stolen-item filtering.'
    $client.onItemBought.Invoke($item,10)
    Assert (!$client.IsClientBuyingThisItem($item)) 'Visible buying interests allow an already-paid item again.'
}
Assert (!$client.IsClientBuyingThisItem((Tagged-Item 'FOOD'))) 'Restocking buys an unrelated category.'
$foreign=[StoreClient]::new();$foreign.clientIntent=[StoreClient+ClientIntent]::BUY;$foreign.clientBuyingTagList.Add('TOOL')
$protected=Tagged-Item 'TOOL';$null=$protected.EnableTag('IMPORTANT_TAG')
Assert ($foreign.IsClientBuyingThisItem($protected)) 'Business buyer filtering changed another customer.'
'PASS: restocking interests are visible while legal/category/paid-item checks remain enforced; other customers are unchanged.'

$tablet=Blank ([NegociationUIManager])
$tabletFlags=[Reflection.BindingFlags]'NonPublic,Instance'
$frameField=([NegociationUIManager]).GetField('frame',$tabletFlags)
$frameField.SetValue($tablet,(Blank $frameField.FieldType))
$buildInfo=([NegociationUIManager]).GetMethod('BuildInfoBlock',$tabletFlags)
$script:tabletInterests=@()
[TabletUI]::TestInfoRows=[Action[string,object]]{param($label,$tags)
    if($label -eq 'ui_nego_interests'){$script:tabletInterests=@($tags | ForEach-Object text)}
}
foreach($visit in @(27,912,910)){
    New-MaidRun;Set-Partner
    if($visit -eq 910){$null=[EleanorRomance.MaidBusiness]::AcceptOrder()}
    $client=[EleanorRomance.MaidStory]::CreateEvent($visit)
    $script:tabletInterests=@()
    $null=$buildInfo.Invoke($tablet,@($client))
    $expected=if($visit -eq 910){'type_material,type_tool'}else{'type_household_good,type_material,type_tool'}
    Assert ((($script:tabletInterests | Sort-Object) -join ',') -eq $expected) "Native tablet interest row is incomplete for visit $visit."
}
$script:tabletInterests=@()
$null=$buildInfo.Invoke($tablet,@($foreign))
Assert (($script:tabletInterests -join ',') -eq 'type_tool') 'Native tablet changed an unrelated customer.'
'PASS: native tablet BuildInfoBlock emits the expected localized interest tags for restocking, routine buying and orders; unrelated tablet tags remain intact.'
