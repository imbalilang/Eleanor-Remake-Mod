# Native business integration and saved receipts for 0.73.
$script:businessStock=[System.Collections.Generic.List[GameItem]]::new()
[EleanorRomance.MaidBusiness]::TestPutStock=[Action[object]]{param($item) $script:businessStock.Add($item)}

# Filled locket: all rotations/flips at four edges, no movement of neighbours.
$repairLocket=[EleanorRomance.Items].GetMethod('RepairLocketShape',[Reflection.BindingFlags]'NonPublic,Static')
$chain='0110100110010110'
foreach($flip in @($false,$true)){foreach($rotation in 0..3){foreach($x in @(0,4)){foreach($y in @(0,4)){
    $inv=New-Inventory;$list=Inventory-Items $inv
    $item=New-Item $chain 4 $x $y $flip $rotation;$item.identifier='eleanor_romance.locket'
    $item.parentInventory=$inv;$list.Add($item);$old=$item.modifiedShape.Clone()
    $null=[GeneralHelper]::SetItemOwned($item,$true);$null=$item.EnableTag('IMPORTANT_TAG')
    for($cy=0;$cy -lt 8;$cy++){for($cx=0;$cx -lt 8;$cx++){
        if($cx -ge $x -and $cx -lt $x+4 -and $cy -ge $y -and $cy -lt $y+4){continue}
        $n=New-Item '1' 1 $cx $cy;$n.identifier='neighbour';$n.parentInventory=$inv;$list.Add($n)
    }}
    $before=$list.Count
    Assert ($repairLocket.Invoke($null,@($item))) 'Locket did not shrink in an available old bounding box.'
    Assert ($item.shape.width -eq 2 -and $item.shape.height -eq 2 -and (Count-Cells $item.modifiedShape) -eq 4) 'Locket is not a solid 2x2.'
    Assert ($item.modifiedShape.flipped -eq $flip -and $item.modifiedShape.orientation -eq $rotation) 'Locket orientation changed.'
    Assert ($list.Count -eq $before -and $item.parentInventory -eq $inv -and
        [GeneralHelper]::IsItemOwned($item) -and $item.IsTag('IMPORTANT_TAG') -and $item.spritePath -eq 'eleanor_locket_compact') 'Locket update lost inventory/state/art.'
    $once=$item.modifiedShape
    Assert ($repairLocket.Invoke($null,@($item)) -and [object]::ReferenceEquals($once,$item.modifiedShape)) 'Locket repair repeated.'
}}}}
# Fill the holes in an old chain: a solid square must defer, never delete them.
$inv=New-Inventory;$list=Inventory-Items $inv
$item=New-Item $chain;$item.identifier='eleanor_romance.locket';$item.parentInventory=$inv;$list.Add($item)
for($y=0;$y -lt 4;$y++){for($x=0;$x -lt 4;$x++){
    if($item.modifiedShape.Get($x,$y)){continue}
    $n=New-Item '1' 1 $x $y;$n.identifier='nested_neighbour';$n.parentInventory=$inv;$list.Add($n)
}}
$oldShape=$item.shape;$oldModified=$item.modifiedShape;$oldState=$item.modifiedState;$count=$list.Count
$result=$repairLocket.Invoke($null,@($item))
Assert (!$result -and $list.Count -eq $count -and
    [object]::ReferenceEquals($oldShape,$item.shape) -and [object]::ReferenceEquals($oldModified,$item.modifiedShape) -and
    [object]::ReferenceEquals($oldState,$item.modifiedState)) "Packed chain holes were overwritten or rollback changed state: result=$result count=$($list.Count)/$count shape=$([object]::ReferenceEquals($oldShape,$item.shape)) modified=$([object]::ReferenceEquals($oldModified,$item.modifiedShape)) state=$([object]::ReferenceEquals($oldState,$item.modifiedState))."
while($list.Count -gt 1){$list.RemoveAt(1)}
Assert ($repairLocket.Invoke($null,@($item))) 'Deferred locket could not be retried.'
[ItemDirectory]::TestItem=New-Item '1' 1
$created=[EleanorRomance.Items]::CreateLocket()
Assert ($created.shape.width -eq 2 -and $created.shape.height -eq 2 -and
    $created.onSlotAddItemFunc -ne $null -and $created.IsTag('IMPORTANT_TAG')) 'New locket factory shape/hooks/protection are wrong.'
'PASS: solid 2x2 locket factory, 32 transformed edge migrations, packed-hole rollback and retry without deleting neighbours.'

# Native estimate acceptance, real rank fee arithmetic, repeated estimate/load,
# incorrect estimates and manual departure all retain the chapter wrap-up.
New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
$script:businessStock.Clear();$c=Main-Client 20
$store.appraiserLevel=0;$store.appraisalProgress=0;$cash=$store.playerCash
Finish-Dialogue $c.mainDialogue
Assert ($c.clientIntent -eq [StoreClient+ClientIntent]::APPRAISAL_SERVICE -and $script:businessStock.Count -eq 1) 'Chapter 19 did not spawn a native appraisal.'
$appraised=$script:businessStock[0]
Assert ([AppraisalHelper]::GetCorrectBaseValue($appraised) -eq 100) 'Ordinary-tool valuation was not available.'
[AppraisalServiceHelper]::OnEstimate($appraised,100)
Assert ($store.playerCash -eq $cash+35 -and [EleanorRomance.MaidBusiness]::AppraisalPaid($c)) 'Native junior fee was not paid/receipted once.'
Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.RomanceState]::Chapter -eq 21) 'Successful appraisal stranded the original story.'
Assert (@([AppraisalItemRegistry]::GetAll() | Where-Object {$_.id -like '*er_business*'}).Count -eq 0) 'Scripted tool changed the native random appraisal category pool.'
$snapshot=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
[ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new($snapshot)
$null=[AppraisalServiceHelper]::GetAcceptEstimateDialog($appraised,100)
Assert ($store.playerCash -eq $cash+35) 'Saved appraisal receipt paid a second fee.'
foreach($walkout in @($true,$false)){
    New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
    $script:businessStock.Clear();$c=Main-Client 20;Finish-Dialogue $c.mainDialogue;$cash=$store.playerCash
    if($walkout){1..3 | ForEach-Object {[AppraisalServiceHelper]::OnEstimate($script:businessStock[0],1000)};Finish-Dialogue $ui.currentDialog}
    else{Leave-Trade $c}
    Assert ([EleanorRomance.RomanceState]::Chapter -eq 21 -and $store.playerCash -eq $cash) 'Refusal/manual appraisal close lost the story or invented income.'
}
New-MaidRun;[EleanorRomance.MaidState]::Enter(1)
[EleanorRomance.RomanceState]::KnewTrapUnwarned=$true
[EleanorRomance.MaidState]::MotivationVersion=0 # Existing pre-revision save, already used the address.
$c=Main-Client 20;$store.TryPlayerDismissCurrentClient($true)
Finish-Dialogue (Pick $ui.currentDialog 0)
Assert ([EleanorRomance.MaidState]::Flag('reconciled') -and [EleanorRomance.RomanceState]::Chapter -eq 21) 'Appraisal removed reconciliation choice.'
'PASS: native appraisal 35-credit fee, saved no-double-payment, wrong-estimate walkout, manual close and original reconciliation.'

function Tagged-Item($tag){$i=New-Item '1' 1;$i.identifier='business_test';$null=$i.EnableTag($tag);return $i}
function Biz-Client($id){
    [EleanorRomance.MaidState]::PendingVisit=$id
    $c=[EleanorRomance.MaidBusiness]::CreateEvent($id);Use-Client $c
    if($c.OnArrival){$c.OnArrival.Invoke()}
    return $c
}
# Optional order on the two relationship variants and the original future menu.
foreach($maid in @($false,$true)){
    New-MaidRun;Set-Partner
    if(!$maid){[EleanorRomance.MaidState]::End('ordinary',1)}
    $c=Main-Client 23;$cash=$store.playerCash
    $next=Pick $c.mainDialogue 0
    Assert ([EleanorRomance.MaidBusiness]::Number('order.state') -eq 1 -and $store.playerCash -eq $cash) 'Order acceptance charged cash or missed a route.'
    [EleanorRomance.MaidBusiness]::AcceptOrder() | Out-Null
    Assert ([EleanorRomance.MaidBusiness]::Number('order.next') -eq 42) 'Repeated acceptance reset the due day.'
}
New-MaidRun;Set-Partner
$c=Main-Client 23;$null=Pick $c.mainDialogue 1
Assert ([EleanorRomance.MaidBusiness]::Number('order.state') -eq 3 -and [EleanorRomance.MaidState]::Lovers) 'Declining the optional order ended love.'
# First collection may be delayed by story priority; deadline starts on arrival.
New-MaidRun;Set-Partner
$null=[EleanorRomance.MaidBusiness]::AcceptOrder();$station.dayCounter=55
$c=Biz-Client 910;$cash=$store.playerCash
Assert ([EleanorRomance.MaidBusiness]::Number('order.first') -eq 55 -and $c.GetBudget() -eq 600 -and $c.isBargainAvailable) 'Collection deadline/budget/bargaining is wrong.'
$tool=Tagged-Item 'TOOL';$mat=Tagged-Item 'MATERIAL';$other=Tagged-Item 'HOUSEHOLD_GOOD'
$protected=Tagged-Item 'TOOL';$null=$protected.EnableTag('IMPORTANT_TAG')
$stolen=Tagged-Item 'TOOL';[StolenHelper]::SetHeat($stolen,100) # native legal filter
Assert ($c.IsClientBuyingThisItem($tool) -and $c.IsClientBuyingThisItem($mat) -and
    !$c.IsClientBuyingThisItem($other) -and !$c.IsClientBuyingThisItem($protected) -and !$c.IsClientBuyingThisItem($stolen)) 'Native order classification bypassed its restrictions.'
$c.onItemBought.Invoke($tool,120)
$c.onItemBought.Invoke($tool,120)
Assert ([EleanorRomance.MaidBusiness]::Number('order.tools') -eq 1 -and $store.playerCash -eq $cash) 'Callback did not record delivery or invented a second native payment.'
Leave-Trade $c
$station.dayCounter=57
[ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
$c=Biz-Client 910
Assert ($c.GetBudget() -eq 480 -and [EleanorRomance.MaidBusiness]::Number('order.first') -eq 55 -and !$c.IsClientBuyingThisItem($tool)) 'Reload reset spent cash/deadline or rebought delivered item.'
$tool2=Tagged-Item 'TOOL';$c.onItemBought.Invoke($tool2,130)
Assert (!$c.IsClientBuyingThisItem((Tagged-Item 'TOOL')) -and $c.IsClientBuyingThisItem($mat)) 'Filled tool quota stayed open.'
$c.onItemBought.Invoke($mat,90);$c.TestOnDealAccepted($null);Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.MaidBusiness]::Number('order.state') -eq 2 -and
    [EleanorRomance.MaidBusiness]::Number('order.spent') -eq 340 -and [EleanorRomance.MaidState]::PendingVisit -eq 0 -and
    [EleanorRomance.MaidState]::Lovers) "Completed order failed native automatic wrap-up: state=$([EleanorRomance.MaidBusiness]::Number('order.state')) spent=$([EleanorRomance.MaidBusiness]::Number('order.spent')) pending=$([EleanorRomance.MaidState]::PendingVisit) lovers=$([EleanorRomance.MaidState]::Lovers) complete=$($c.isBusinessComplete) dialog=$($ui.currentDialog.plainText)."
New-MaidRun;Set-Partner;$null=[EleanorRomance.MaidBusiness]::AcceptOrder();$c=Biz-Client 910;Leave-Trade $c
$station.dayCounter=45;$manager=$store.storeClientManager
$null=[EleanorRomance.MaidBusiness]::TryQueue($manager,45)
Assert ([EleanorRomance.MaidBusiness]::Number('order.state') -eq 4 -and [EleanorRomance.MaidState]::Lovers) 'Order expiry blocked the relationship.'
New-MaidRun;Set-Partner;$null=[EleanorRomance.MaidBusiness]::AcceptOrder();$c=Biz-Client 910
$c.onItemBought.Invoke((Tagged-Item 'TOOL'),600);$c.clientBudget=0
$c.TestOnDealAccepted($null);Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.MaidBusiness]::Number('order.state') -eq 4 -and
    $ui.currentDialog.plainText -eq 'er_biz_order_budget' -and [EleanorRomance.MaidState]::Lovers) 'Budget exhaustion promised another collection or blocked the story.'
'PASS: optional order on both routes, native legal/category filters, partial delivery/save/resumed budget, quotas, automatic close and nonblocking expiry.'

# Supplies can be declined or bought; all branches retain the original result.
foreach($choice in 0..2){
    New-MaidRun;Set-Partner;$script:businessStock.Clear()
    [EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Flag('p2.paid',$true)
    [EleanorRomance.MaidState]::PendingVisit=24
    $c=[EleanorRomance.MaidStory]::CreateEvent(24);Use-Client $c
    Finish-Dialogue (Pick $c.mainDialogue $choice)
    if($choice -ne 2){
        Assert ($script:businessStock.Count -eq 2 -and $c.isBargainAvailable) 'Supply lot missing goods/bargaining.'
        [StoreClientInstance]::TestHasStock=$false;$c.TestOnDealAccepted($null);Finish-Dialogue $ui.currentDialog
    }
    Assert ([EleanorRomance.MaidState]::Progress -eq 24 -and [EleanorRomance.MaidProjects]::ResultShown(2)) 'Supply choice skipped/blocked the old result.'
}
# Free sample belongs to P3, claims once, normal stock stays normally priced.
New-MaidRun;Set-Partner;$script:businessStock.Clear()
[EleanorRomance.MaidState]::Flag('p3.paid',$true);[EleanorRomance.MaidState]::PendingVisit=25
$c=[EleanorRomance.MaidStory]::CreateEvent(25);Use-Client $c;Finish-Dialogue $c.mainDialogue
$samples=@($script:businessStock | Where-Object {$_.IsTag('ER_BUSINESS_SAMPLE')})
Assert ($samples.Count -eq 1 -and $samples[0].FindItemFeatureByID('free') -ne $null -and
    $samples[0].GetNegociatedValue() -eq 0 -and $script:businessStock.Count -eq 3 -and
    $script:businessStock[1].GetNegociatedValue() -gt 0) 'Sample/ordinary stock separation failed.'
Finish-Dialogue $c.mainDialogue
Assert ($script:businessStock.Count -eq 3) 'Repeated intro duplicated stock.'
Leave-Trade $c
Assert (![EleanorRomance.MaidBusiness]::Done('sample.claimed') -and [EleanorRomance.MaidState]::Progress -eq 25) 'Unclaimed sample blocked story or was consumed.'
$script:businessStock.Clear();$c=Biz-Client 914;Finish-Dialogue $c.mainDialogue
Assert ($script:businessStock.Count -eq 1) 'Pickup spawned paid stock or lost the sample.'
$c.onItemSold.Invoke($script:businessStock[0],0)
Assert ([EleanorRomance.MaidBusiness]::Done('sample.claimed') -and $script:businessStock[0].FindItemFeatureByID('free') -eq $null) 'Sample did not consume the single claim/clear free feature.'
$script:businessStock.Clear();$c=Biz-Client 914;Finish-Dialogue $c.mainDialogue
Assert ($script:businessStock.Count -eq 0) 'Claimed sample was issued again.'
New-MaidRun;Set-Partner;$script:businessStock.Clear()
[EleanorRomance.MaidState]::Flag('p3.paid',$true);[EleanorRomance.MaidState]::Flag('p3.shown',$true)
$c=[EleanorRomance.MaidStory]::CreateEvent(25);Finish-Dialogue $c.mainDialogue
Assert (@($script:businessStock | Where-Object {$_.IsTag('ER_BUSINESS_SAMPLE')}).Count -eq 0) 'An old completed project received a retrospective sample.'
New-MaidRun;Set-Partner;[EleanorRomance.MaidState]::PendingVisit=27
[EleanorRomance.MaidState]::Flag('p4.paid',$true)
$c=[EleanorRomance.MaidStory]::CreateEvent(27);Use-Client $c;$c.OnArrival.Invoke()
Assert ($c.clientIntent -eq [StoreClient+ClientIntent]::BUY -and $c.GetBudget() -eq 900 -and $c.IsClientBuyingThisItem((Tagged-Item 'TOOL'))) 'Launch restocking is not a real native buyer.'
$c.clientBudget=0;$c.TestOnDealAccepted($null);Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.MaidState]::Progress -eq 27 -and [EleanorRomance.MaidProjects]::ResultShown(4)) 'Buyer budget completion lost launch story.'
'PASS: three supply choices and automatic completion, P3 free sample/normal paid stock, retry/deduplication/legacy guard, native 900-credit restocking.'

function Waiting-BusinessRun {
    New-MaidRun;Set-Partner
    [EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=23
    [EleanorRomance.MaidState]::ProgressDay=70;[EleanorRomance.MaidState]::LastImportantDay=70
    [EleanorRomance.MaidState]::Flag('p1.paid',$true);[EleanorRomance.MaidState]::Flag('p1.shown',$true)
    [EleanorRomance.MaidState]::Flag('wait_stage_23',$true)
    [EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::PendingChapter=0
    [EleanorRomance.RomanceState]::EpilogueDone=$true
    $store.playerCash=100;$station.dayCounter=80
}
Waiting-BusinessRun
foreach($id in @(911,912,913,911)){
    $manager=$store.storeClientManager;$manager.clientStack.Clear();$store.currentClientInstance=$null
    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
    Assert ($manager.clientStack.Count -eq 1 -and [EleanorRomance.MaidState]::PendingVisit -eq $id) "Business rotation missed $id."
    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
    Assert ($manager.clientStack.Count -eq 1) 'Business visit queued twice on one day.'
    $c=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $c;Leave-Trade $c
    $store.currentClientInstance=$null
    $station.dayCounter+=3
    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
    Assert ($manager.clientStack.Count -eq 0) 'Routine returned before four-day cooldown.'
    $station.dayCounter++
}
Waiting-BusinessRun;$script:businessStock.Clear()
$c=Biz-Client 911;Finish-Dialogue $c.mainDialogue;Finish-Dialogue $c.mainDialogue
Assert ($script:businessStock.Count -eq 2 -and $c.isBargainAvailable) 'Repeat sale daily spawned twice or lost bargaining.'
Waiting-BusinessRun;$c=Biz-Client 912;$cash=$store.playerCash
Assert ($c.GetBudget() -eq 350 -and $c.IsClientBuyingThisItem((Tagged-Item 'HOUSEHOLD_GOOD'))) 'Repeat buying daily lacks its native budget/item preference.'
$item=Tagged-Item 'TOOL';$c.onItemBought.Invoke($item,80);$c.onItemBought.Invoke($item,80)
Assert ([EleanorRomance.MaidBusiness]::Number('routine.spent.0') -eq 80 -and $store.playerCash -eq $cash) 'Repeat buying receipt replayed payment bookkeeping.'
Waiting-BusinessRun;$script:businessStock.Clear();$c=Biz-Client 913;Finish-Dialogue $c.mainDialogue;$cash=$store.playerCash
[AppraisalServiceHelper]::OnEstimate($script:businessStock[0],100);Finish-Dialogue $ui.currentDialog
Assert ($store.playerCash -eq $cash+35 -and [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Repeat appraisal did not pay/finish through native service.'
foreach($reason in @('cash','favor','paid','urgent','main')){
    Waiting-BusinessRun;$manager=$store.storeClientManager
    switch($reason){
        cash {$store.playerCash=20000}
        favor {$store.wildFavor=20000}
        paid {[EleanorRomance.MaidState]::Flag('p2.paid',$true);[EleanorRomance.MaidState]::SetInt('p2.day',80)}
        urgent {$urgent=[StoreClient]::new();$urgent.identifier='urgent';$urgent.isSentenceOfficer=$true;$manager.clientStack.Add($urgent)}
        main {[EleanorRomance.RomanceState]::Chapter=20;[EleanorRomance.RomanceState]::NextVisitDay=80}
    }
    [EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
    Assert (@($manager.clientStack | Where-Object {$_.identifier -like 'er_story_biz_*'}).Count -eq 0) "Routine ignored $reason priority/funding."
    if($reason -eq 'main'){Assert ($manager.clientStack.Count -eq 1 -and $manager.clientStack[0].identifier -eq 'er_story_maid_m19') 'Due main scene did not win.'}
}
Waiting-BusinessRun;$store.playerCash=0
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 912) 'Zero-cash waiting offered only a seller.'
'PASS: repeated sell/buy/appraisal rotation, four-day spacing, same-day dedupe, cash/favor/paid-project gates, urgent/main priority and zero-cash buyer.'
