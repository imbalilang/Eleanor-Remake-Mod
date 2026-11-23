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
Assert ([EleanorRomance.MaidDividends]::Pending -eq 0 -and [EleanorRomance.MaidDividends]::LastDay -eq 100) 'Opening the ledger credited days before its accrual start.'
$station.dayCounter=104;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 2400) 'Established income was not 600 per game day.'
[EleanorRomance.RomanceState]::RomanceLocked=$true;[EleanorRomance.MaidDividends]::Accrue()
$station.dayCounter=110;[EleanorRomance.MaidDividends]::Accrue()
Assert ([EleanorRomance.MaidDividends]::Pending -eq 2400 -and [EleanorRomance.MaidDividends]::DailyRate -eq 0) 'Terminated cooperation kept earning or erased the ledger.'
'PASS: dividend story/result gates, same-day dedupe, rollback cursor, 200-to-600 rate split, opening accrual and contract termination.'

# Only reading the receipt credits cash. Saved balances, repeated callbacks and
# the native int cash boundary must conserve earned = paid + pending.
Dividend-Run 28
[EleanorRomance.MaidDividends]::Accrue();$station.dayCounter=64
$cash=$store.playerCash;[EleanorRomance.MaidState]::PendingVisit=915
$c=[EleanorRomance.MaidDividends]::CreateVisit();Use-Client $c
Assert ($store.playerCash -eq $cash -and [EleanorRomance.MaidDividends]::Pending -eq 2400) 'Creating a visitor paid cash.'
$receipt=$c.mainDialogue
while ($receipt -and $receipt.plainText -ne 'er_div_receipt') { $receipt=$receipt.nextDialogue }
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
