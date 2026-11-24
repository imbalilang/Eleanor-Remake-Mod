# Read-only chapter regressions against the game's managed assembly, using the existing offline fixture.
param([Parameter(Mandatory=$true)][string]$Managed)
. (Join-Path $PSScriptRoot 'test_runtime_flows.ps1') -Managed $Managed -ReleaseQA -SmokeOnly
[DirectoryMaster]::TestFactory = [Func[string,bool,object]]{
    param($id,$owned)
    $item = Blank ([GameItemElement])
    $item.identifier = $id
    $item.state = [TagSystem]::new()
    $item.modifiedState = [TagSystem]::new()
    $item.itemFeatures = [System.Collections.Generic.List[ItemFeature]]::new()
    $item.itemTypes = [System.Collections.Generic.List[string]]::new()
    $item.unitCount = 1
    $item.shape = [GridShapeBuilder]::new().SetData('1',1).Build()
    $item.modifiedShape = [GridShapeBuilder]::new($item.shape).SetTransform(0,0,$false,0).Build()
    $item.mayInventoryAddItemFunc = [Func[GameItem,GameInventory,bool]]{param($i,$inv) $true}
    return $item
}
[ModHelper]::TestData = [System.Collections.Generic.Dictionary[string,string]]::new()
$cgStation = Blank ([StoreStation]); [StoreStation]::TestInstance = $cgStation
$cgStore = Blank ([PlayerStore]); [PlayerStore]::TestInstance = $cgStore
$cgStore.storeClientData = [StoreClientData]::new()
$cgStore.storeClientManager = [StoreClientManager]::new()
$cgStore.startingPerks = [System.Collections.Generic.List[StartingPerk]]::new()
$cgStore.storeReputations = [System.Collections.Generic.List[StoreReputation]]::new()
$cgUI = Blank ([DialogUIManager]); [DialogUIManager]::TestInstance = $cgUI
$cgAssembly = [EleanorRomance.LoveState].Assembly
$eligible = $cgAssembly.GetType('EleanorRomance.ChapterVisits').GetMethod('Eligible',[Reflection.BindingFlags]'Static,NonPublic')
$due = $cgAssembly.GetType('EleanorRomance.ChapterFinales').GetMethod('Due',[Reflection.BindingFlags]'Static,NonPublic')
foreach ($early in @($false,$true)) {
    [EleanorRomance.RomanceState]::ResetForNewGame($early)
    $shift = if ($early) { 11 } else { 0 }
    $days = @(54,55,56,57,58,60,61,63,65,69)
    [EleanorRomance.LoveState]::Start($days[0]-$shift)
    for ($i=0; $i -lt $days.Length; $i++) {
        $cgStation.dayCounter = $days[$i]-$shift
        [EleanorRomance.LoveState]::Complete($i+1)
        if ($i+1 -lt $days.Length) {
            Assert ([EleanorRomance.LoveState]::NextVisitDay -eq $days[$i+1]-$shift) 'Next main visit blocks a planned CG.'
        }
        if ($i -eq 6) {
            $cgStation.dayCounter=62-$shift
            Assert ($eligible.Invoke($null,@('love_day_promenade'))) 'The approved day off is unreachable.'
        }
        if ($i -eq 7) {
            Assert ($eligible.Invoke($null,@('love_night_stay'))) 'Proposal night invitation is unreachable.'
            $cgStation.dayCounter++
            Assert (!$eligible.Invoke($null,@('love_night_stay'))) 'The invitation itself repeated on the following morning.'
        }
    }
    Assert ($due.Invoke($null,@([int](71-$shift),[int]4)) -eq 75-$shift) 'Fourth morning is mistimed.'
    Assert ($due.Invoke($null,@([int](67-$shift),[int]6)) -eq 73-$shift) 'Sixth evening is mistimed.'
}
# Inspect the actual Sofia transaction graph and its real no-sale wrap-up, up to the picture boundary.
[EleanorRomance.RomanceState]::ResetForNewGame($false)
$cgStation.dayCounter=57
[EleanorRomance.LoveState]::Start(54)
$client=[EleanorRomance.LoveStory]::CreateVisit(4)
$node=$client.mainDialogue
while($node -and !$node.choices.Count){$node=$node.nextDialogue}
Assert ($node.choices.Count -eq 3) 'Sophia lost one of her original replies.'
$instance=Blank ([StoreClientInstance]); $instance.storeClient=$client; $cgStore.currentClientInstance=$instance
$cgStore.TryPlayerDismissCurrentClient($true)
$keys=@();$node=$cgUI.currentDialog
while($node){$keys+=$node.plainText;$node=$node.nextDialogue}
Assert ($keys -contains 'er_love_la2_t1_none_01') 'No-sale outcome must still lead to the escort.'
Assert ($keys[-2] -eq 'er_love_la2_s1_common_07' -and $keys[-1] -eq 'er_love_la2_s1_common_08') 'The escort is not the final beat before the stove CG.'
'PASS: Sophia choices and no-sale escort; both calendars, proposal gate, day off, and E4/E9 finale dates.'
