# Persistent dividends: game-day accrual, actual cash, dialogue receipts and queue priority.
function Dividend-Run($stage=25,$day=60){
    New-MaidRun;Set-Partner
    [EleanorRomance.RomanceState]::Chapter=300;[EleanorRomance.RomanceState]::PendingChapter=0
    [EleanorRomance.RomanceState]::EpilogueDone=$true
    [EleanorRomance.MaidState]::BasicComplete=$true;[EleanorRomance.MaidState]::Progress=$stage
    [EleanorRomance.MaidState]::ProgressDay=$day;[EleanorRomance.MaidState]::LastImportantDay=$day-5
    foreach($p in 1..3){[EleanorRomance.MaidState]::Flag("p$p.paid",$true);[EleanorRomance.MaidState]::Flag("p$p.shown",$true)}
    if($stage -ge 27){[EleanorRomance.MaidState]::Flag('p4.paid',$true);[EleanorRomance.MaidState]::Flag('p4.shown',$true)}
    $station.dayCounter=$day
}
Dividend-Run 24
[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 0 -and [EleanorRomance.MaidDividends]::LastDay -eq -1) 'Future paid/shown flags bypassed the story gate.'
[EleanorRomance.MaidState]::Progress=25;[EleanorRomance.MaidState]::Flag('p3.shown',$false)
[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 0) 'Paying P3 alone started its income.'
[EleanorRomance.MaidState]::Flag('p3.shown',$true)
[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 200 -and [EleanorRomance.MaidDividends]::Pending -eq 0) 'First-order income did not start without retroactive credits.'
$station.dayCounter=63;[EleanorRomance.MaidDividends]::Accrue()
1..3 | ForEach-Object {[EleanorRomance.MaidDividends]::Accrue()}
Assert ([EleanorRomance.MaidDividends]::Pending -eq 600) 'Same-day repeats changed the accrual.'
$station.dayCounter=61;[EleanorRomance.MaidDividends]::Accrue()
$station.dayCounter=64;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 800) 'Clock rollback duplicated days.'
[EleanorRomance.MaidState]::Progress=27;[EleanorRomance.MaidState]::Flag('p4.paid',$true)
$station.dayCounter=65;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 200) 'P4 payment without feedback raised the rate.'
[EleanorRomance.MaidState]::Flag('p4.shown',$true);[EleanorRomance.MaidDividends]::Accrue()
$station.dayCounter=66;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 1600 -and [EleanorRomance.MaidDividends]::DailyRate -eq 600) 'Rate change repriced older days.'
Dividend-Run 28 100
[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 0 -and [EleanorRomance.MaidDividends]::LastDay -eq 100) 'Old completed save invented historical dividends.'
$station.dayCounter=104;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 2400) 'Established income was not 600 per game day.'
[EleanorRomance.MaidState]::End('ordinary-lovers',1)
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 600) 'Business income was made conditional on retaining the private role.'
[EleanorRomance.RomanceState]::Route=4;[EleanorRomance.MaidDividends]::Accrue()
$station.dayCounter=110;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 2400 -and [EleanorRomance.MaidDividends]::DailyRate -eq 0) 'Terminated cooperation kept earning or erased the ledger.'
'PASS: dividend story/result gates, same-day dedupe, rollback cursor, 200-to-600 rate split, old-save start and business rights independent of the private role.'

# Only reading the receipt credits cash. Saved balances, repeated callbacks and
# the native int cash boundary must conserve earned = paid + pending.
Dividend-Run 28
[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=64
$cash=$store.playerCash;[EleanorRomance.MaidState]::PendingVisit=915
$c=[EleanorRomance.MaidDividends]::CreateVisit();Use-Client $c
Assert ($store.playerCash -eq $cash -and [EleanorRomance.MaidDividends]::Pending -eq 2400) 'Creating a visitor paid cash.'
$receipt=@((Motivation-Nodes $c.mainDialogue) | Where-Object plainText -eq 'er_div_receipt')[0]
Assert ($receipt -ne $null) 'Dividend visit had no receipt.'
$receipt.ExecuteEndAction()
Assert ($store.playerCash -eq $cash+2400 -and [EleanorRomance.MaidDividends]::Pending -eq 0 -and
    [EleanorRomance.MaidDividends]::TotalPaid -eq 2400) 'Receipt did not move real cash exactly once.'
$station.dayCounter=65;$receipt.ExecuteEndAction()
Assert ($store.playerCash -eq $cash+2400) 'Replaying an old receipt collected a new day.'
[ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
$again=[EleanorRomance.MaidStory]::CreateEvent(915);Finish-Dialogue $again.mainDialogue
Assert ($store.playerCash -eq $cash+3000 -and [EleanorRomance.MaidDividends]::TotalPaid -eq 3000 -and
    [EleanorRomance.MaidState]::PendingVisit -eq 0) 'Saved in-progress visit lost the next accrued day or paid twice.'
Dividend-Run 28;[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=64
$store.playerCash=[int]::MaxValue-100
$paid=[EleanorRomance.MaidDividends]::Collect()
Assert ($paid -eq 100 -and $store.playerCash -eq [int]::MaxValue -and [EleanorRomance.MaidDividends]::Pending -eq 2300) 'Native cash limit overflowed or discarded unpaid funds.'
$store.playerCash=[int]::MaxValue-500
$paid=[EleanorRomance.MaidDividends]::Collect(200)
Assert ($paid -eq 200 -and [EleanorRomance.MaidDividends]::TotalEarned -eq
    [EleanorRomance.MaidDividends]::TotalPaid+[EleanorRomance.MaidDividends]::Pending) 'Partial settlement broke ledger conservation.'
Dividend-Run 28;[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=10000000
[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -gt [int]::MaxValue) 'Accrual was truncated to native int cash.'
'PASS: real receipt cash, no payment on construction, replay/save continuation, partial settlement, native cash overflow safety and long balance conservation.'

# Urgent and main story visits win; skipped visits leave all accrued days intact.
Dividend-Run 27
[EleanorRomance.MaidDividends]::Accrue()
[EleanorRomance.MaidState]::ProgressDay=63
$manager=$store.storeClientManager;$urgent=[StoreClient]::new();$urgent.identifier='test_urgent';$urgent.isSentenceOfficer=$true
$manager.clientStack.Add($urgent);$station.dayCounter=64
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 1 -and [EleanorRomance.MaidDividends]::Pending -eq 2400) 'Urgent visitor was displaced or lost accrued dividends.'
$manager.clientStack.Clear();[EleanorRomance.MaidState]::LastImportantDay=64
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 0) 'Dividend visit violated same-day spacing.'
$station.dayCounter=65;$cash=$store.playerCash
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ([EleanorRomance.MaidState]::PendingVisit -eq 28 -and $manager.clientStack.Count -eq 1) 'Dividend visit displaced the final agreement.'
$c=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $c
Assert (@((Motivation-Nodes $c.mainDialogue) | Where-Object plainText -eq 'er_div_receipt').Count -eq 0) 'Final emotional scene inserted a cash handover.'
Finish-Dialogue (Pick $c.mainDialogue 0)
Assert ($store.playerCash -eq $cash -and [EleanorRomance.MaidDividends]::Pending -eq 3000) 'Final agreement paid or lost saved dividends.'
$store.currentClientInstance=$null;$station.dayCounter=67
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 1 -and [EleanorRomance.MaidState]::PendingVisit -eq 915) 'Due dividend visit was missing or duplicated.'
$c=$manager.clientStack[0];$manager.clientStack.Clear();Use-Client $c;Finish-Dialogue $c.mainDialogue
Assert ($store.playerCash -eq $cash+4200 -and [EleanorRomance.MaidDividends]::NextDelivery -eq 71) 'Postponed settlement lost days or set the wrong next visit.'
# This check concerns unlimited settlements after optional epilogues are closed;
# the party's independent due/priority path is exercised in its own suite.
[EleanorRomance.GalaStory]::Phase=8
[EleanorRomance.MaidState]::SetInt('daily_index',4);$store.currentClientInstance=$null;$station.dayCounter=71
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($manager)
Assert ($manager.clientStack.Count -eq 1 -and [EleanorRomance.MaidState]::PendingVisit -eq 915) 'Finishing four epilogue chats disabled recurring dividends.'
'PASS: dividend accrual through urgent/same-day delays, main-story priority, pure final agreement, deduplicated delivery and unlimited post-ending settlements.'

# Stage introductions and real existing business/daily visitors carry the money.
Dividend-Run 24 65;[EleanorRomance.MaidState]::Flag('p3.shown',$false)
[EleanorRomance.MaidState]::PendingVisit=25
$c=[EleanorRomance.MaidStory]::CreateEvent(25);Use-Client $c;Finish-Dialogue $c.mainDialogue
$store.TryPlayerDismissCurrentClient($true)
Assert (@((Motivation-Nodes $ui.currentDialog) | Where-Object plainText -eq 'er_div_first').Count -eq 1) 'First-order scene did not explain the numeric reward.'
Finish-Dialogue $ui.currentDialog
Assert ([EleanorRomance.MaidDividends]::DailyRate -eq 200 -and [EleanorRomance.MaidDividends]::Pending -eq 0) 'First-order completion did not start next-day accrual.'
$station.dayCounter=70;[EleanorRomance.MaidState]::Flag('p4.paid',$true);[EleanorRomance.MaidState]::PendingVisit=27
$cash=$store.playerCash;$c=[EleanorRomance.MaidStory]::CreateEvent(27);Use-Client $c;Leave-Trade $c
Assert ($store.playerCash -eq $cash+1000 -and [EleanorRomance.MaidDividends]::DailyRate -eq 600 -and
    @((Motivation-Nodes $ui.currentDialog) | Where-Object plainText -eq 'er_div_established').Count -eq 1) 'Launch did not settle the old rate and introduce the new one.'
foreach($id in @(907,911,912,913,914)){
    Dividend-Run 28;[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=64
    [EleanorRomance.MaidState]::PendingVisit=$id;$cash=$store.playerCash
    $c=[EleanorRomance.MaidStory]::CreateEvent($id);Use-Client $c
    if($id -eq 907){Finish-Dialogue $c.mainDialogue}
    else{Finish-Dialogue $c.mainDialogue;Leave-Trade $c}
    Assert ($store.playerCash -eq $cash+2400 -and [EleanorRomance.MaidDividends]::Pending -eq 0) "Eleanor visit $id did not carry accrued dividends."
}
Dividend-Run 28;[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=64
[EleanorRomance.MaidState]::PendingVisit=26;$cash=$store.playerCash
$c=[EleanorRomance.MaidStory]::CreateEvent(26);Use-Client $c;Leave-Trade $c
Assert ($store.playerCash -eq $cash -and [EleanorRomance.MaidDividends]::Pending -eq 2400) 'Another customer handed out Eleanor dividends.'
$description=[EleanorRomance.MaidProjects]::Description([EleanorRomance.MaidProjects]::Find('er_maid_p4_cash'))
Assert ($description.Contains('er_div_project_established') -and $description.Contains('er_div_summary')) 'Wilde project omitted the reward or ledger summary.'
'PASS: first-order/launch numeric introductions, stage-rate boundary, five existing Eleanor visit carriers, no third-party payouts and concise Wilde reward display.'
