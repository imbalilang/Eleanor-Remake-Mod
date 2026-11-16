# Run with test_runtime_flows.ps1 -Rewrite against the installed game assembly.
foreach($index in 0..1){
    Reset-Run 10
    $client=[EleanorRomance.EleanorStory]::CreateClient(10);Use-Client $client
    $store.TryPlayerDismissCurrentClient($true)
    $wrap=$ui.currentDialog
    $menu=Choice-Node $wrap
    Finish-Dialogue $wrap
    Assert ($menu -and [PlayerStore]::TestDismissCount -eq 0 -and
        [EleanorRomance.RomanceState]::PendingChapter -eq 10) 'Unanswered bracelet prompt committed or dismissed.'
    Finish-Dialogue $menu.choices[$index].Item2
    Assert ([EleanorRomance.RomanceState]::DPreludeState -eq 2 -and
        [EleanorRomance.RomanceState]::Chapter -eq 12 -and
        [PlayerStore]::TestDismissCount -eq 1) 'Selected bracelet refusal failed to commit exactly once.'
}

Reset-Run 11
[EleanorRomance.RomanceState]::DPreludeState=1
[EleanorRomance.RomanceState]::DPreludeStep=2
[EleanorRomance.RomanceState]::DPreludeOriginalPrice=140
[EleanorRomance.RomanceState]::PreludeBraceletId=314
[EleanorRomance.RomanceState]::PreludeBraceletCount=1
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$true
$inventory=New-Inventory
[EmporiumEntry]::TestRewriteInventory=$inventory
$bracelet=New-Item '1' 1
$bracelet.uniqueId=314
$bracelet.parentInventory=$inventory
[GeneralHelper]::SetItemOwned($bracelet,$true)
$other=New-Item '1' 1
$other.uniqueId=315
$other.parentInventory=$inventory
[GeneralHelper]::SetItemOwned($other,$true)
$unowned=New-Item '1' 1
$unowned.uniqueId=316
$unowned.parentInventory=$inventory
[GeneralHelper]::SetItemOwned($unowned,$false)
$items=Inventory-Items $inventory
$items.Add($bracelet)
$items.Add($other)
$items.Add($unowned)
$client=[EleanorRomance.EleanorStory]::CreateClient(11);Use-Client $client
$menu=Choice-Node $client.mainDialogue
Assert ($menu.IsChoiceEnabledAt(1)) 'The counted bracelet did not enable gifting.'
$menu.choices[0].Item3.Invoke()
$offer=[EleanorRomance.EleanorStory].GetMethod('PreludeFixedPrice',[Reflection.BindingFlags]'NonPublic,Static')
Assert ($client.GetBudget() -eq 140 -and $offer.Invoke($null,@($client,$bracelet)) -eq 140 -and
    $offer.Invoke($null,@($client,$unowned)) -eq 0) 'Buyback used a budget modifier or accepted an unowned bracelet.'
# The count, not the inventory location, is the route fact. Removing the
# physical item must not revoke the gift path or the harem qualification.
$items.Remove($bracelet) | Out-Null
Assert ($menu.IsChoiceEnabledAt(1)) 'Removing the physical bracelet incorrectly disabled gifting.'
$menu.choices[1].Item3.Invoke()
Finish-Dialogue $menu.choices[1].Item2
Assert ([EleanorRomance.RomanceState]::DPreludeState -eq 5 -and
    [EleanorRomance.RomanceState]::PreludeBraceletCount -eq 0 -and
    [EleanorRomance.RomanceState]::LydiaFairBracelet -and
    [EleanorRomance.RomanceState]::DPreludeDUnlocked) 'Counted gift did not commit the harem handover.'

Reset-Run 11
[EleanorRomance.RomanceState]::DPreludeState=1
[EleanorRomance.RomanceState]::DPreludeStep=2
[EleanorRomance.RomanceState]::DPreludeOriginalPrice=140
[EleanorRomance.RomanceState]::PreludeBraceletCount=1
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$true
$client=[EleanorRomance.EleanorStory]::CreateClient(11);Use-Client $client
$menu=Choice-Node $client.mainDialogue
Assert ($menu.IsChoiceEnabledAt(1)) 'Counted bracelet did not initially enable gifting.'
[EleanorRomance.RomanceState]::PreludeBraceletCount=0
Assert (!$menu.IsChoiceEnabledAt(1)) 'Zero bracelet count still enabled gifting.'
$menu.choices[1].Item3.Invoke()
Finish-Dialogue $menu.choices[1].Item2
Assert ([EleanorRomance.RomanceState]::DPreludeState -eq 7 -and
    ![EleanorRomance.RomanceState]::DPreludeDUnlocked) 'Zero bracelet count did not reject the gift choice.'
[EmporiumEntry]::TestRewriteInventory=$null

# Count maintenance is event-driven and clamped at zero.
Reset-Run 11
[EleanorRomance.RomanceState]::PreludeBraceletCount=0
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$true
$acq=[EleanorRomance.EleanorStory].GetMethod('NotePreludeBraceletAcquired',[Reflection.BindingFlags]'NonPublic,Static')
$rel=[EleanorRomance.EleanorStory].GetMethod('NotePreludeBraceletReleased',[Reflection.BindingFlags]'NonPublic,Static')
$countItem=New-Item '1' 1
$acq.Invoke($null,@($countItem))
$acq.Invoke($null,@($countItem))
Assert ([EleanorRomance.RomanceState]::PreludeBraceletCount -eq 2) 'Bracelet acquisitions did not accumulate.'
$rel.Invoke($null,@($countItem))
Assert ([EleanorRomance.RomanceState]::PreludeBraceletCount -eq 1) 'Bracelet release did not decrement once.'
$rel.Invoke($null,@($countItem))
$rel.Invoke($null,@($countItem))
Assert ([EleanorRomance.RomanceState]::PreludeBraceletCount -eq 0) 'Bracelet count went below zero or replayed a release.'

# Old saves scan once, after the inventory is loaded.
$ensure=[EleanorRomance.EleanorStory].GetMethod('EnsurePreludeBraceletCountInitialized',[Reflection.BindingFlags]'NonPublic,Static')
$legacyInv=New-Inventory
[EmporiumEntry]::TestRewriteInventory=$legacyInv
$legacyItems=Inventory-Items $legacyInv
$legacyBracelet=New-Item '1' 1
$legacyBracelet.uniqueId=314
$legacyBracelet.parentInventory=$legacyInv
[GeneralHelper]::SetItemOwned($legacyBracelet,$true)
$legacyItems.Add($legacyBracelet)
[EleanorRomance.RomanceState]::PreludeBraceletCount=0
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$false
[EleanorRomance.RomanceState]::PreludeBraceletId=314
[EleanorRomance.RomanceState]::DPreludeState=1
$ensure.Invoke($null,@())
Assert ([EleanorRomance.RomanceState]::PreludeBraceletCount -eq 1 -and
    [EleanorRomance.RomanceState]::PreludeBraceletCountInitialized) 'Legacy state 1 did not scan the held bracelet once.'

[EleanorRomance.RomanceState]::PreludeBraceletCount=0
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$false
[EleanorRomance.RomanceState]::DPreludeState=5
$ensure.Invoke($null,@())
Assert ([EleanorRomance.RomanceState]::PreludeBraceletCount -eq 0 -and
    [EleanorRomance.RomanceState]::PreludeBraceletCountInitialized) 'Legacy completed handover was migrated as held.'

# A failed gift on an early legacy save can be repaired once, from the exact
# registered bracelet instance, but never after the ordinary confession.
Reset-Run 11
[EleanorRomance.RomanceState]::DPreludeState=7
[EleanorRomance.RomanceState]::PreludeBraceletId=314
[EleanorRomance.RomanceState]::PreludeBraceletCount=0
[EleanorRomance.RomanceState]::PreludeBraceletCountInitialized=$false
[EleanorRomance.RomanceState]::Route=1
[EleanorRomance.RomanceState]::MotherState=1
$repairInv=New-Inventory
[EmporiumEntry]::TestRewriteInventory=$repairInv
$repairItems=Inventory-Items $repairInv
$repairBracelet=New-Item '1' 1
$repairBracelet.uniqueId=314
$repairBracelet.parentInventory=$repairInv
[GeneralHelper]::SetItemOwned($repairBracelet,$true)
$repairItems.Add($repairBracelet)
$ensure.Invoke($null,@())
Assert ([EleanorRomance.RomanceState]::DPreludeState -eq 5 -and
    [EleanorRomance.RomanceState]::LydiaFairBracelet -and
    [EleanorRomance.RomanceState]::PreludeBraceletCount -eq 0 -and
    [EleanorRomance.RomanceState]::PreludeBraceletCountInitialized) 'Legacy failed gift was not repaired.'
[EmporiumEntry]::TestRewriteInventory=$null

$companion=[Dialogue]::new().SetText('SEC','companion line')
[EleanorRomance.StoryPresentation]::MarkCompanion($companion,'Eleanor')
$history=@(Render-ArrestHistory $companion)
Assert ($history[0] -eq 'Eleanor: companion line') 'Backup dialogue was attributed to the security officer.'

Reset-Run 18
[EleanorRomance.RomanceState]::DPreludeState=5
[EleanorRomance.RomanceState]::MotherState=1
[EleanorRomance.RomanceState]::Route=1
[EleanorRomance.RomanceState]::FairDeals=3
[EleanorRomance.RomanceState]::UncleReported=$true
$station.dayCounter=54
Assert (![EleanorRomance.DualDirector]::CanOfferEntry(54)) 'Entry skipped the mother visit.'
[EleanorRomance.RomanceState]::MotherVisited=$true
Assert (![EleanorRomance.DualDirector]::CanOfferEntry(54)) 'Entry skipped the common handoff.'
[EleanorRomance.StoryCoreFacts]::Record(19)
Assert ([EleanorRomance.DualDirector]::CanOfferEntry(54)) 'Completed common facts cannot unlock the rewrite.'

Reset-Run 12
[EleanorRomance.RomanceState]::Savings=500
$client=[EleanorRomance.MaidStory]::CreateEntry();Use-Client $client
$menu=Choice-Node $client.mainDialogue
Assert ($menu -and [EleanorRomance.RomanceState]::Savings -eq 800 -and
    ![EleanorRomance.RomanceState]::LocketSold) 'C11 fallback both sold and retained the pendant.'
Assert ($menu.choices[0].Item2.nextDialogue -ne $null) 'Maid entry choice has no displayed reply line.'
Finish-Dialogue $menu.choices[0].Item2
Assert ([EleanorRomance.RomanceState]::Chapter -eq 13) 'The normal C11 choice no longer advances.'

Reset-Run 20
[EleanorRomance.RomanceState]::MotherVisited=$true
[EleanorRomance.RomanceState]::MotherState=1
[EleanorRomance.RomanceState]::Route=1
[EleanorRomance.DualState]::Accept(1)
[EleanorRomance.DualState]::Flag('d.route',$true)
$store.playerCash=125
$station.dayCounter=63
$client=[EleanorRomance.DualStory]::CreateScene('d05');Use-Client $client
$menu=Choice-Node $client.mainDialogue
Finish-Dialogue $client.mainDialogue
Assert ($store.playerCash -eq 125) 'Closure payment happened before agreement.'
$reply=$menu.choices[0].Item2
Finish-Dialogue $reply
Finish-Dialogue $reply
Assert ($store.playerCash -eq 2125 -and [EleanorRomance.DualState]::Done('d05')) 'Closure agreement did not credit exactly 2000.'
foreach($id in @('entry','d02','d03','d04')){[EleanorRomance.DualState]::Complete($id)}
[EleanorRomance.DualState]::Flag('d.act2a',$true)
[EleanorRomance.DualState]::Flag('d.act2',$true)
Assert ([EleanorRomance.DualDirector]::NextScene(64) -eq 'd06') 'Closure is not due the following day.'
Assert (![EleanorRomance.DualDirector]::TryQueue($store.storeClientManager,64)) 'Closure was queued as a regular shop customer.'
$closure=([EleanorRomance.DualState].Assembly.GetType('EleanorRomance.DualClosingDay'))
$pay=$closure.GetMethod('PayOnce',[Reflection.BindingFlags]'NonPublic,Static')
$pay.Invoke($null,@())
Assert ($store.playerCash -eq 2125) 'Closure payment duplicated on recovery.'

[EleanorRomance.DualState]::Complete('d09')
[EleanorRomance.DualState]::Flag('d.act4',$true)
[EleanorRomance.DualState]::Relation=2
[EleanorRomance.DualMigration]::OnLoaded()
Assert ([EleanorRomance.DualState]::CoreCommitted -and
    [EleanorRomance.RomanceState]::Chapter -eq [EleanorRomance.RomanceState]::ChapterComplete) 'E4 did not finish the common progression.'
'PASS: rewritten bracelet choices, common handoff, C11 pendant, closure compensation and schedule, and E4 recovery.'
