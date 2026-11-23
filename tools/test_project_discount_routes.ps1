function Discount-Run($project=3) {
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true
    [EleanorRomance.MaidState]::Progress=26
    foreach ($previous in 1..($project-1)) {
        [EleanorRomance.MaidState]::Flag("p$previous.paid",$true)
        [EleanorRomance.MaidState]::Flag("p$previous.shown",$true)
    }
}
# Every edge is a price condition, never a purchase prerequisite. Use both the
# native UI GetCost and the actual service, including native favor offsets.
foreach($case in @(
    @(2,60,29,3000,$false),@(2,0,30,1500,$true),@(2,-20,30,1500,$true),
    @(3,39,60,5000,$false),@(3,40,0,2500,$true),@(3,40,-20,2500,$true),
    @(4,50,49,8000,$false),@(4,49,50,8000,$false),@(4,50,50,4000,$true)
)){
    Discount-Run $case[0];Set-Reps $case[1] $case[2]
    $p=$case[0];$id="er_maid_p${p}_cash";$plan=[EleanorRomance.MaidProjects]::Find($id)
    $store.wildFavor=100
    $q=[EleanorRomance.MaidProjects]::GetQuote($plan)
    Assert ($q.Discount -eq $case[4] -and $q.Total -eq $case[3] -and $store.networkUpgrade[$id].GetCost() -eq $q.Total -and
        [NetworkUpgrade]::GetDefinition($id).GetCost() -eq $q.Total) "UI/service/definition quote disagreed at P$p rep $($case[1])/$($case[2])."
    $description=[EleanorRomance.MaidProjects]::Description($plan)
    $conditionKey=if($p -eq 2){'er_maid_discount_lower'}elseif($p -eq 3){'er_maid_discount_upper'}else{'er_maid_discount_both'}
    $statusKey=if($case[4]){'er_maid_discount_active'}else{'er_maid_discount_pending'}
    Assert ($description.Contains($conditionKey) -and $description.Contains($statusKey)) 'Project description omitted its threshold or current discount status.'
    $cash=$store.playerCash
    Assert ([EleanorRomance.MaidProjects]::TryPurchase($id) -and $store.playerCash -eq $cash-($case[3]-100) -and $store.wildFavor -eq 0) 'Single cash purchase did not charge its displayed quote.'
    Assert ([StoreReputation]::GetULReputation() -eq $case[1] -and [StoreReputation]::GetLLReputation() -eq $case[2] -and
        [EleanorRomance.MaidState]::Int("p$p.upper") -eq 0 -and [EleanorRomance.MaidState]::Int("p$p.lower") -eq 0 -and
        [EleanorRomance.MaidState]::Flag("p$p.discount") -eq $case[4]) 'The new discount spent reputation or recorded the wrong story variant.'
    $cash=$store.playerCash
    Assert (![EleanorRomance.MaidProjects]::TryPurchase($id) -and $store.playerCash -eq $cash) 'Repeated clicks charged a project twice.'
}
'PASS: all single-cash price thresholds, native UI/definition/service consistency, description conditions/status, full-price fallback, exact charges, native favor and no reputation spending.'

Discount-Run;Set-Reps 40 60
$rules.upgradePriceMultiplier=1.5
$pillar=Blank ([StoreReputationPerk]);$pillar.perkId='LLPILLAR';$pillar.perkThreshold=50
[StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').perks.Add($pillar)
Set-Reps 40 60
$store.startingPerks.Add([StartingPerk]@{id='wild_favorite'})
$id='er_maid_p3_cash';$plan=[EleanorRomance.MaidProjects]::Find($id)
$expected=[NetworkUpgrade]::new('unrelated');$expected.cost=2500
Assert ($store.networkUpgrade[$id].GetCost() -eq $expected.GetCost() -and
    [EleanorRomance.MaidProjects]::GetQuote($plan).Total -eq $expected.GetCost()) 'Project discount bypassed native difficulty/perk/rounding rules.'
Set-Reps 39 60;$expected.cost=5000
Assert ($store.networkUpgrade[$id].GetCost() -eq $expected.GetCost() -and ![EleanorRomance.MaidProjects]::GetQuote($plan).Discount) 'A reputation drop left a sticky discount.'
Set-Reps 40 60;$expected.cost=2500
$cash=$store.playerCash
Assert ([EleanorRomance.MaidProjects]::TryPurchase($id) -and $store.playerCash -eq $cash-$expected.GetCost() -and $pillar.isUnlocked) 'Purchase did not re-evaluate the live reputation or preserved faction perk.'
Set-Reps 0 0
Assert ([EleanorRomance.MaidState]::Flag('p3.discount')) 'The result story forgot which terms were used after reputation changed.'
[StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').perks.Clear()
# Missing reputation is no longer a reason to omit a funding visit.
Discount-Run 2;Set-Reps 0 0
[EleanorRomance.MaidState]::Progress=23
$store.playerCash=2000
Assert ([EleanorRomance.MaidBusiness]::NeedsFunding()) 'Low reputation suppressed the fundraising daily event.'
Set-Reps 0 30
Assert (![EleanorRomance.MaidBusiness]::NeedsFunding()) 'The funding gate ignored the newly affordable discounted price.'
'PASS: live price changes, native difficulty/faction/starting-perk math, paid-story discount history and fundraising eligibility above/below the discount threshold.'
