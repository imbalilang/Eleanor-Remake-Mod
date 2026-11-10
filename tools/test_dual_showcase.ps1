function Display-Ornaments($mask){
    $inv=New-Inventory;[EmporiumEntry]::TestShowcase=$inv
    [PlayerStore]::TestAllItems.Clear()
    foreach($stage in 1..3){
        [EleanorRomance.DualState]::Flag("reward.$stage.earned",$true)
        if(($mask -band (1 -shl ($stage-1))) -eq 0){continue}
        $item=New-Item '1' 1;$item.identifier=[EleanorRomance.DualRewards]::ItemIds[$stage-1]
        [GeneralHelper]::SetItemOwned($item,$true);$item.parentInventory=$inv
        (Inventory-Items $inv).Add($item);[PlayerStore]::TestAllItems.Add($item)
    }
}
function Upper-Buyer($id='upperLowerVisitorLuxury',$budget=2000,$source=''){
    $c=[StoreClient]::new();$c.identifier=$id;$c.clientIntent=[StoreClient+ClientIntent]::BUY
    $c.sourceModId=$source;$c.SetBudget($budget);Use-Client $c
    [EleanorRomance.DualShowcaseEffects]::Arrive($c);return $c
}
function Sell-Goods($price){
    $item=New-Item '1' 1;$item.identifier='test_goods';$item.TestPrice=$price;$item.TestUsePrice=$true
    [GeneralHelper]::SetItemOwned($item,$true)
    $store.SellItem($item);return $item
}
foreach($mask in 0..7){
    Dual-Run;Display-Ornaments $mask;$store.playerCash=1000
    $c=Upper-Buyer 'upperLowerVisitorLuxury' 500
    $budget=if($mask -band 2){650}else{500}
    Assert ($c.GetBudget() -eq $budget) "Wrong budget for showcase combination $mask."
    [EleanorRomance.DualShowcaseEffects]::Arrive($c)
    Assert ($c.GetBudget() -eq $budget) 'Arrival callback applied budget twice.'
    $null=Sell-Goods 200
    Assert ($c.GetBudget() -eq $budget-200 -and $store.playerCash -eq $(if($mask -band 4){1220}else{1200})) "Wrong sale/tip combination $mask."
    [EleanorRomance.DualShowcaseEffects]::OnLoaded()
    Assert ($c.GetBudget() -eq $budget-200) 'Same live visit restored its full budget.'
    $store.currentClientInstance=$null;[EleanorRomance.DualShowcaseEffects]::OnLoaded()
    $new=Upper-Buyer 'upperLowerVisitorLuxury' 500
    Assert ($new.GetBudget() -eq $budget -and [EleanorRomance.DualState]::Int('visit.goods') -eq 0) 'New same-kind customer inherited an old visit ledger.'
}
Dual-Run;Display-Ornaments 7;$c=Upper-Buyer
$snapshot=$c.GetBudget();$inv=[EmporiumEntry]::TestInstance.showcaseElement
$duplicate=New-Item '1' 1;$duplicate.identifier=[EleanorRomance.ModConstants]::PatronItemId;$duplicate.parentInventory=$inv
[GeneralHelper]::SetItemOwned($duplicate,$true);(Inventory-Items $inv).Add($duplicate)
[EleanorRomance.DualShowcaseEffects]::Arrive($c)
Assert ($c.GetBudget() -eq $snapshot) 'Duplicate item stacked the budget multiplier.'
Display-Ornaments 0;$store.playerCash=0;$null=Sell-Goods 100
Assert ($store.playerCash -eq 110) 'Removing ornaments changed the current arrival snapshot.'
$other=Upper-Buyer 'upperLowerVisitor' 500
Assert ($other.GetBudget() -eq 500) 'New customer ignored removal.'
foreach($pair in @(@('eleanor1',''),@('er_story_coop_buyer','eleanor_romance'),@('upperLowerVisitor','another_mod'),@('random_upper',''))){
    Display-Ornaments 7;$c=Upper-Buyer $pair[0] 500 $pair[1]
    Assert ($c.GetBudget() -eq 500 -and ![EleanorRomance.DualShowcaseEffects]::Eligible($c)) 'Whitelist modified a story/foreign client.'
}
'PASS: all eight showcase combinations, independent effects, snapshot/removal/duplicate safety, live-versus-missing visit reload and verified whitelist exclusions.'

Dual-Run;Display-Ornaments 4;$store.playerCash=1000;$c=Upper-Buyer
$sold=Sell-Goods 500
Assert ($store.playerCash -eq 1550) 'First actual sale did not pay 50.'
$null=Sell-Goods 600
Assert ($store.playerCash -eq 2210) 'Second sale did not pay incremental 60.'
$null=Sell-Goods 900
Assert ($store.playerCash -eq 3150 -and [EleanorRomance.DualState]::Int('visit.tip_paid') -eq 150) 'Per-visit cumulative tip cap incorrect.'
[EleanorRomance.DualShowcaseEffects]::Capture($c,$sold,500)
Assert ($store.playerCash -eq 3150) 'A repeated callback after settlement paid again.'
Dual-Run;Display-Ornaments 4;$store.playerCash=0;$c=Upper-Buyer 'upperLowerVisitor' 100
$null=Sell-Goods 500
Assert ($store.playerCash -eq 110 -and [EleanorRomance.DualState]::Int('visit.goods') -eq 100 -and $c.GetBudget() -eq 0) 'Tips used quoted value instead of actual budget-limited payment.'
Dual-Run;Display-Ornaments 4;$store.playerCash=0;$c=Upper-Buyer
$null=Sell-Goods 0
Assert ($store.playerCash -eq 0 -and [EleanorRomance.DualState]::Int('visit.goods') -eq 0) 'Free goods produced a tip.'
$item=New-Item '1' 1;[GeneralHelper]::SetItemOwned($item,$true)
$cancel=[EleanorRomance.DualShowcaseEffects]::BeginSale($item)
[EleanorRomance.DualShowcaseEffects]::EndSale($cancel,$false)
Assert ($store.playerCash -eq 0 -and [EleanorRomance.DualShowcaseEffects]::BeforeSave()) 'Cancelled sale paid or retained a save lock.'
'PASS: actual native SellItem arithmetic, incremental 500/600/900 settlement, 150 cap, replay safety, exhausted budget, zero-priced goods and cancelled-sale cleanup.'

# Saving inside a real purchase callback must wait until base cash and tip settle.
Dual-Run;Display-Ornaments 4;$store.playerCash=1000;$c=Upper-Buyer
[PlayerStore]::TestSaveCount=0
$c.onItemBought=[Action[GameItem,int]]{param($i,$n) $store.SaveGame();Assert ([PlayerStore]::TestSaveCount -eq 0) 'Saved a half-completed base sale.'}
$null=Sell-Goods 500
Assert ($store.playerCash -eq 1550 -and [PlayerStore]::TestSaveCount -eq 1) 'Deferred sale save did not include actual cash and tip.'
# Observer failure after cash changes must not duplicate a restored pending tip.
Dual-Run;Display-Ornaments 4;$store.playerCash=0;$c=Upper-Buyer
[PlayerStore]::TestProjectCashHook=[Action]{throw 'simulated tip observer failure after cash mutation'}
$null=Sell-Goods 100
[PlayerStore]::TestProjectCashHook=$null
[EleanorRomance.DualShowcaseEffects]::FlushPending()
Assert ($store.playerCash -eq 110 -and [EleanorRomance.DualState]::Text('tip.pending') -eq '0') 'Cash observer failure duplicated a tip.'
[EleanorRomance.DualState]::Text('tip.pending','200');$store.playerCash=[int]::MaxValue-50
[EleanorRomance.DualShowcaseEffects]::FlushPending()
Assert ($store.playerCash -eq [int]::MaxValue -and [EleanorRomance.DualState]::Text('tip.pending') -eq '150') 'Cash overflow lost unpaid tips.'
$store.playerCash=0;[EleanorRomance.DualShowcaseEffects]::FlushPending()
Assert ($store.playerCash -eq 150) 'Pending tip balance did not resume.'
'PASS: base-sale/cash/ledger save boundary, callback-triggered save, observer failure after mutation, cash overflow and retained unpaid balance.'

Dual-Run;[EleanorRomance.DualState]::Flag('reward.1.earned',$true)
[GraphUtils]::TestRemaining=1
Assert (![EleanorRomance.DualRewards]::Give(1) -and ![EleanorRomance.DualState]::PlaqueReceived) 'Full storage consumed reward.'
[GraphUtils]::TestRemaining=0
Assert ([EleanorRomance.DualRewards]::Give(1)) 'Reward retry failed.'
$gift=[GraphUtils]::TestLastGift;[PlayerStore]::TestAllItems.Add($gift)
Assert ([EleanorRomance.DualRewards]::Give(1) -and [object]::ReferenceEquals($gift,[GraphUtils]::TestLastGift)) 'Reward was duplicated.'
[PlayerStore]::TestAllItems.Clear()
Assert (![EleanorRomance.DualRewards]::Ready(1)) 'Lost reward reissued immediately.'
$first=[EleanorRomance.DualState]::Int('reward.1.lost_day');$station.dayCounter++
Assert ([EleanorRomance.DualRewards]::Ready(1) -and [EleanorRomance.DualState]::Int('reward.1.lost_day') -eq $first) 'Missing scan continually postponed reissue.'
$entry=[EmporiumEntry]::TestInstance;[EmporiumEntry]::TestInstance=$null
Assert (![EleanorRomance.DualRewards]::Give(1)) 'Unknown storage scan was treated as missing item.'
[EmporiumEntry]::TestInstance=$entry
foreach($stage in 1..3){
    [ItemDirectory]::TestItem=New-Item '1' 1
    $item=[EleanorRomance.Items]::CreateOrnament($stage)
    Assert ($item.shape.width -eq 1 -and $item.shape.height -eq 1 -and $item.IsTag('IMPORTANT_TAG') -and !$item.IsTag('LUXURY_ITEM')) 'Ornament shape/protection/stock tags incorrect.'
}
'PASS: full-storage retry, unique delivery, first-loss date, delayed reissue, unknown scan safety and all three real 1x1 protected factories.'

# Real referral factory, inventory tag matching, persisted promise and D+2 arrival cooldown.
Dual-Run;Display-Ornaments 1;[EleanorRomance.DualState]::PlaqueReceived=$true
$stock=New-Item '1' 1;$stock.identifier='luxury_stock';$stock.EnableTag('LUXURY_ITEM')|Out-Null
[GeneralHelper]::SetItemOwned($stock,$true);$stock.parentInventory=[EmporiumEntry]::TestInstance.showcaseElement
(Inventory-Items $stock.parentInventory).Add($stock);[PlayerStore]::TestAllItems.Add($stock)
[EleanorRomance.DualBusiness]::TestRoll=0.1
$manager=$store.storeClientManager
Assert ([EleanorRomance.DualBusiness]::TryQueue($manager,49)) 'Displayed plaque and valid stock failed to queue a referral.'
$id=[EleanorRomance.DualState]::Text('referral.pending_id');$buyer=$manager.clientStack[0]
Assert ($id -eq $buyer.identifier -and $buyer.willAcceptItem.Invoke($stock)) 'Referral promise or accepted stock mismatch.'
$household=New-Item '1' 1;$household.identifier='ordinary_household';$household.EnableTag('HOUSEHOLD_GOOD')|Out-Null
[GeneralHelper]::SetItemOwned($household,$true)
Assert (!$buyer.willAcceptItem.Invoke($household)) 'Generic household goods counted as luxury inventory.'
# Lost queue on load and removed display: an already promised visitor still comes.
$manager.clientStack.Clear();[EmporiumEntry]::TestShowcase=New-Inventory;$station.dayCounter=50
[EleanorRomance.DualBusiness]::TestRoll=0.9
Assert ([EleanorRomance.DualBusiness]::TryQueue($manager,50) -and $manager.clientStack[0].identifier -eq $id) 'Removing plaque or loading cancelled/rerolled a promised visitor.'
$buyer=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $buyer
[EleanorRomance.DualBusiness]::OnArrivedOrLeft($buyer,$true)
Assert ([EleanorRomance.DualState]::LastExtraBuyerDay -eq 50 -and [EleanorRomance.DualState]::Text('referral.pending_id') -eq '') 'Referral cooldown used queue day instead of actual arrival.'
$store.currentClientInstance=$null;$station.dayCounter=51;[EleanorRomance.RomanceState]::LastQueuedDay=-1
Assert (![EleanorRomance.DualBusiness]::TryQueue($manager,51)) 'Referral ignored display removal/cooldown.'
'PASS: real referral client, matching buy tags, no household false positives, stable promise after load/removal and actual-arrival cooldown.'

Dual-Run;Display-Ornaments 4;$store.playerCash=1000;$c=Upper-Buyer
$null=Sell-Goods 500;$null=Sell-Goods 600
# Model an older saved ledger restored while the same native object survived.
$store.playerCash=1550
[EleanorRomance.DualState]::SetInt('visit.goods',500)
[EleanorRomance.DualState]::SetInt('visit.tip_paid',50)
[EleanorRomance.DualState]::SetInt('visit.remaining',1500)
[EleanorRomance.DualState]::SetInt('visit.sales',1)
[EleanorRomance.DualShowcaseEffects]::OnLoaded()
$null=Sell-Goods 600
Assert ($store.playerCash -eq 2210 -and [EleanorRomance.DualState]::Int('visit.goods') -eq 1100) 'Live visit load restored budget but kept a newer in-memory tip ledger.'
'PASS: same-object reload rewinds every budget/tip/transaction snapshot field with the restored save.'
