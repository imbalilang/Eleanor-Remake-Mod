function Discount-Run {
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true
    [EleanorRomance.MaidState]::Progress=26
    [EleanorRomance.MaidState]::Flag('p1.paid',$true)
    [EleanorRomance.MaidState]::Flag('p1.shown',$true)
}
# Every edge is a price condition, never a purchase prerequisite. Use both the
# native UI GetCost and the actual service, including native favor offsets.
foreach($case in @(
    @(2,60,29,3000,$false),@(2,0,30,1500,$true),@(2,-20,30,1500,$true),
    @(3,39,60,5000,$false),@(3,40,0,2500,$true),@(3,40,-20,2500,$true),
    @(4,50,49,8000,$false),@(4,49,50,8000,$false),@(4,50,50,4000,$true)
)){
    Discount-Run;Set-Reps $case[1] $case[2]
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
Assert ([EleanorRomance.MaidProjects]::FavorPlan(3)) 'The result story forgot which terms were used after reputation changed.'
[StoreReputation]::GetStoreReputation('FACTION_LOWER_LEVEL').perks.Clear()
# Missing reputation is no longer a reason to omit a funding visit.
Discount-Run;Set-Reps 0 0
[EleanorRomance.MaidState]::Progress=23
$store.playerCash=2000
Assert ([EleanorRomance.MaidBusiness]::NeedsFunding()) 'Low reputation suppressed the fundraising daily event.'
Set-Reps 0 30
Assert (![EleanorRomance.MaidBusiness]::NeedsFunding()) 'The funding gate ignored the newly affordable discounted price.'
'PASS: live price changes, native difficulty/faction/starting-perk math, paid-story discount history and fundraising eligibility above/below the discount threshold.'

# Old favor purchases must appear once in the canonical cash row. Their actual
# cash, favor and reputation expenditures remain historical facts, not refunds.
Discount-Run
foreach($p in 2..4){
    $id="er_maid_p${p}_favor"
    $u=[NetworkUpgrade]::new($id);$u.cost=1234;$u.state=2;$u.hidden=$false
    $store.networkUpgrade[$id]=$u
    [EleanorRomance.MaidState]::Flag("p$p.paid",$true)
    [EleanorRomance.MaidState]::Text("p$p.plan",$id)
    [EleanorRomance.MaidState]::SetInt("p$p.cash",1200+$p)
    [EleanorRomance.MaidState]::SetInt("p$p.favor",100)
    [EleanorRomance.MaidState]::SetInt("p$p.upper",10)
    [EleanorRomance.MaidState]::SetInt("p$p.lower",10)
    [EleanorRomance.MaidState]::SetInt("p$p.day",50+$p)
}
$cash=$store.playerCash
1..3 | ForEach-Object {[EleanorRomance.MaidProjects]::OnLoaded()}
foreach($p in 2..4){
    $legacy="er_maid_p${p}_favor";$canonical="er_maid_p${p}_cash"
    Assert ($store.networkUpgrade[$legacy].hidden -and $store.networkUpgrade[$legacy].state -eq 0 -and
        $store.networkUpgrade[$canonical].state -eq 2 -and !$store.networkUpgrade[$canonical].hidden -and
        $null -eq [NetworkUpgrade]::GetDefinition($legacy)) 'A retired alternative remained visible or lost the settled canonical row.'
    Assert ([EleanorRomance.MaidProjects]::FavorPlan($p) -and [EleanorRomance.MaidProjects]::ResultPending($p) -and
        [EleanorRomance.MaidState]::Text("p$p.plan") -eq $legacy -and
        [EleanorRomance.MaidState]::Int("p$p.cash") -eq 1200+$p -and [EleanorRomance.MaidState]::Int("p$p.favor") -eq 100 -and
        [EleanorRomance.MaidState]::Int("p$p.upper") -eq 10 -and [EleanorRomance.MaidProjects]::PaidDay($p) -eq 50+$p) 'Old payment history/result variant was rewritten.'
    Assert (![EleanorRomance.MaidProjects]::TryPurchase($legacy) -and ![EleanorRomance.MaidProjects]::TryPurchase($canonical)) 'An old settled plan could be bought again.'
}
Assert-ProjectLayout @('er_maid_p1_cash','er_maid_p2_cash','er_maid_p3_cash','er_maid_p4_cash') 'legacy paid favor plans'
Assert ($store.playerCash -eq $cash -and [EleanorRomance.RomanceState]::SchemaVersion -eq 2) 'Legacy migration refunded/charged cash or reset the schema.'
Discount-Run
Assert (@($store.networkUpgrade.Keys | Where-Object {$_ -like 'er_maid_p*'}).Count -eq 4) 'A fresh run re-created retired alternatives.'
'PASS: three legacy favor payments retain immutable expenditure/result history, one settled row each, repeated-load safety, no refund/recharge and four definitions in fresh runs.'
