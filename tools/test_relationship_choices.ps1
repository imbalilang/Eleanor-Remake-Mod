# Relationship stability and deliberate, separately confirmed irreversible acts.
foreach($kind in 1..2){foreach($answer in 0..2){
    New-MaidRun;[EleanorRomance.MaidState]::Enter($kind)
    [EleanorRomance.RomanceState]::Route=1;[EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.MaidState]::AddressAccepted=$true;[EleanorRomance.MaidState]::DebutShown=$true
    [EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::Flag('p1.shown',$true)
    $cash=$store.playerCash;$c=Main-Client 21;$menu=Choice-Node $c.mainDialogue
    Assert ($menu.choices.Count -eq 3 -and [EleanorRomance.MaidState]::Confession -eq 0) 'Confession committed during creation or lost response choices.'
    foreach($option in $menu.choices){Assert ($option.Item1.shortDescription -eq 'er_maid_confess_continue_desc') 'Confession option does not explain continuing the agreement.'}
    $response=Pick $c.mainDialogue $answer
    Assert ([EleanorRomance.MaidState]::Confession -eq 0) 'Selecting a response committed before its last line.'
    Finish-Dialogue $response;Finish-Dialogue $response
    Assert ([EleanorRomance.MaidState]::Lovers -and [EleanorRomance.MaidState]::EntryKind -eq $kind -and
        [EleanorRomance.RomanceState]::Chapter -eq 22 -and [EleanorRomance.MaidState]::BuyerState -eq 1 -and
        $store.playerCash -eq $cash) 'A confession tone changed the route, repeated entry payment or lost the buyer.'
    [EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=23
    Assert ([EleanorRomance.MaidProjects]::Available(2)) 'A confession response closed the return projects.'
}}
'PASS: all three confession responses on cash/legacy entry keep the attendant romance, commit only at the end, charge nothing and retain later projects.'

# Completed historical conversions retain their choice, money and paid results.
foreach($relationship in 1..2){
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::SetInt('p1.day',38)
    if($relationship -eq 1){[EleanorRomance.MaidState]::End('ordinary-lovers',1);$expected=2}
    else{[EleanorRomance.MaidState]::End('friends',3);$expected=4}
    $cash=$store.playerCash
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    1..2 | ForEach-Object {[EleanorRomance.MaidRouteMigration]::OnLoaded();[EleanorRomance.MaidRouteMigration]::MigrateDeferred();[EleanorRomance.MaidProjects]::OnLoaded()}
    Assert (![EleanorRomance.MaidState]::Active -and [EleanorRomance.MaidState]::Confession -eq $expected -and $store.playerCash -eq $cash) 'Loading a historical conversion forced it back onto the attendant route.'
    $c=Main-Client 23;Finish-Dialogue (Pick $c.mainDialogue 1)
    Assert ([EleanorRomance.MaidProjects]::ResultShown(1) -and ![EleanorRomance.MaidProjects]::Available(2)) 'Historical conversion lost paid housing or reopened future projects.'
}
'PASS: historical ordinary-lover/friend saves keep their relationship, resources and paid housing without reopening the attendant route.'

# Handback commits custody at the actual selection, retains it across rebuilding,
# and lets the original rescue result (not a promise alone) finish the repair.
foreach($early in @($false,$true)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    [EleanorRomance.RomanceState]::EarlyStart=$early;[EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
    $cash=$store.playerCash;$c=Main-Client 13;$null=Pick $c.mainDialogue 1
    Assert ([EleanorRomance.MaidState]::Flag('honesty.handback') -and [EleanorRomance.RomanceState]::DriveState -eq 2 -and ![EleanorRomance.MaidState]::Flag('reconciled')) 'Handback invented completed trust or failed actual custody.'
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    $c=Main-Client 13
    Assert ((Choice-Node $c.mainDialogue) -eq $null -and (Has-MotivationBlock $c.mainDialogue 'choicefix_b001')) 'Interrupted handback repeated the old moral test or lost fixed custody.'
    Finish-Dialogue $c.mainDialogue
    [EleanorRomance.RomanceState]::Route=1;$c=Main-Client 17;Finish-Dialogue $c.mainDialogue
    Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.MaidState]::Flag('reconciled') -and
        [EleanorRomance.RomanceState]::MotherState -eq 1 -and $store.playerCash -eq $cash) 'Handback failed the shared rescue/trust continuation.'
    $c=Main-Client 20
    Assert ((Choice-Node $c.mainDialogue) -eq $null) 'A repaired new run was tested again in chapter 19.'
    Finish-Dialogue $c.mainDialogue
}
foreach($answer in 0..1){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(2)
    [EleanorRomance.MaidState]::MotivationVersion=0;[EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
    $cash=$store.playerCash;$c=Main-Client 20
    $reply=Pick $c.mainDialogue $answer
    Assert (![EleanorRomance.MaidState]::Flag('reconciled')) 'Legacy repair committed on menu selection.'
    Finish-Dialogue $reply
    Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.MaidState]::Flag('reconciled') -and
        [EleanorRomance.RomanceState]::Chapter -eq 21 -and $store.playerCash -eq $cash -and
        [EleanorRomance.MaidState]::Flag('honesty.open_records') -eq ($answer -eq 1)) 'Legacy repair approach ended the route or lost its recorded action.'
}
'PASS: handback has real custody, rebuild-safe continuation and rescue-earned trust; both legacy repair approaches retain the agreement without a repeated new-run test.'

# Low capability opens a discussion; leaving is its own explicit nested choice.
foreach($exit in @($false,$true)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1);Set-Reps 0 0
    [EleanorRomance.RomanceState]::DriveState=1
    $cash=$store.playerCash;$c=Main-Client 15;$difficulty=Pick $c.mainDialogue 4
    Finish-Dialogue $difficulty
    Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.RomanceState]::Route -eq 0 -and
        [EleanorRomance.RomanceState]::DriveState -eq 1 -and $store.playerCash -eq $cash) 'Raising concerns committed an exit or changed resources.'
    $back=Pick $difficulty 1
    Assert ((Choice-Node $back).choices.Count -eq 5 -and [EleanorRomance.MaidState]::Active) 'Reconsider did not return to the original discussion.'
    Finish-Dialogue (Pick $difficulty $(if($exit){2}else{0}))
    if($exit){Assert (![EleanorRomance.MaidState]::Active -and [EleanorRomance.RomanceState]::BetrayalKind -eq 4 -and
        [EleanorRomance.RomanceState]::DriveState -eq 2 -and [EleanorRomance.RomanceState]::Chapter -eq 201) 'Explicit nested withdrawal failed to return the drive or finish the agreement.'}
    else{Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.RomanceState]::Route -eq 1 -and
        [EleanorRomance.RomanceState]::Chapter -eq 16) 'Low-capability exchange could not continue the chosen relationship.'}
    Assert ($store.playerCash -eq $cash) 'Discussion or normal withdrawal charged/refunded money.'
}
'PASS: low-capability discussion and reconsider leave facts unchanged, exchange continues the route, and only explicit nested withdrawal ends it.'

# Risk reviews expose dedicated descriptions; backing out or rebuilding them
# never performs the actual sale/leak. Final choices retain original outcomes.
foreach($risk in @('handover','sell','secret','public')){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    $rep=[StoreReputation]::GetStoreReputation('FACTION_BLACK_MARKET')
    if($null -eq $rep){$rep=[StoreReputation]::new();$rep.factionId='FACTION_BLACK_MARKET';$store.storeReputations.Add($rep)}
    $rep.SetReputation(50)
    [EleanorRomance.RomanceState]::DriveState=1
    $code=if($risk -in @('handover','sell')){14}else{15}
    $index=switch($risk){handover{3}sell{4}secret{3}public{2}}
    $cash=$store.playerCash;$c=Main-Client $code;$menu=Choice-Node $c.mainDialogue
    $desc=$menu.choices[$index].Item1.shortDescription
    Assert ($desc -ne 'er_maid_choice_no_cost') 'Risky choice lost its dedicated consequence description.'
    $review=Pick $c.mainDialogue $index;Finish-Dialogue $review
    Assert ([EleanorRomance.MaidState]::Active -and [EleanorRomance.RomanceState]::Route -eq 0 -and
        [EleanorRomance.RomanceState]::DriveState -eq 1 -and $store.playerCash -eq $cash) 'Opening a risk review committed its irreversible outcome.'
    $back=Pick $review 1
    Assert ((Choice-Node $back).choices.Count -eq 5 -and $store.playerCash -eq $cash) 'Risk reconsideration failed to return without payment.'
    $c=Main-Client $code;$review=Pick $c.mainDialogue $index
    Finish-Dialogue (Pick $review 0)
    if($risk -eq 'public'){
        Assert ([EleanorRomance.RomanceState]::Route -eq 3 -and [EleanorRomance.RomanceState]::Chapter -eq 101 -and $store.playerCash -eq $cash) 'Confirmed public route lost its original high-risk outcome.'
    }else{
        $kind=switch($risk){handover{1}sell{2}secret{3}}
        $income=switch($risk){handover{300}sell{600}secret{0}}
        Assert (![EleanorRomance.MaidState]::Active -and [EleanorRomance.RomanceState]::BetrayalKind -eq $kind -and
            [EleanorRomance.RomanceState]::Chapter -eq 201 -and $store.playerCash -eq $cash+$income) 'Final betrayal confirmation changed its original payment or outcome.'
    }
}
'PASS: all four irreversible decisions have specific warnings, cancellation/rebuild safety and unchanged final sale/publication outcomes.'

# Story-language alternatives state continued attachment, not apparent release.
$choiceZh=@{};$choiceEn=@{}
Import-Csv (Join-Path $PSScriptRoot '../Localization/zh.csv') -Header key,value | ForEach-Object {$choiceZh[$_.key]=$_.value}
Import-Csv (Join-Path $PSScriptRoot '../Localization/en.csv') -Header key,value | ForEach-Object {$choiceEn[$_.key]=$_.value}
Assert ($choiceZh['er_choice_future_friends'] -match '朋友' -and $choiceEn['er_choice_future_friends'] -match 'friends') 'Deferred ordinary romance still disguises its friendship decision.'
Assert ($choiceZh['er_maid_final_3'] -notmatch '叫不叫|欠我' -and $choiceEn['er_maid_final_3'] -notmatch 'Whether|owe') 'Final response still sounds like withdrawing the private address.'
foreach($answer in 0..2){
    Dividend-Run 27
    [EleanorRomance.MaidState]::PendingVisit=28
    $c=[EleanorRomance.MaidStory]::CreateEvent(28);$menu=Choice-Node $c.mainDialogue
    foreach($option in $menu.choices){Assert ($option.Item1.shortDescription -eq 'er_maid_final_continue_desc') 'Renewal response is missing continued-relationship context.'}
    Finish-Dialogue (Pick $c.mainDialogue $answer)
    Assert ([EleanorRomance.MaidState]::Lovers -and [EleanorRomance.MaidState]::Contract -eq 3 -and
        [EleanorRomance.MaidDividends]::DailyRate -eq 600 -and [EleanorRomance.GalaStory]::Eligible) 'A renewal tone lost romance, dividends or the party epilogue.'
}
'PASS: all renewal responses retain romance/dividends/party eligibility, with clear final-address and deferred-friendship wording in both languages.'
