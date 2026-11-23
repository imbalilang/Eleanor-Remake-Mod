# Current rewrite: every authored choice path plus trade/cash/scheduler regressions.
[PlayerStore]::TestReleaseStock=[System.Collections.Generic.List[string]]::new()
function Reset-Modern($kind,$code){
    Reset-Run $(if($kind -eq 'dual'){20}else{$code})
    $station.dayCounter=80
    $store.playerCash=5000
    $store.storeState=[PlayerStore+StoreState]::CLOSED
    [EleanorRomance.MaidState]::FeatureVersion=2
    [EleanorRomance.RomanceState]::Route=2
    [EleanorRomance.RomanceState]::MotherState=1
    [EleanorRomance.RomanceState]::MotherVisited=$true
    [EleanorRomance.RomanceState]::Savings=500
    [EleanorRomance.RomanceState]::Romance=1
    [PlayerStore]::TestReleaseStock.Clear()
    if($kind -eq 'maid'){
        if($code -ge 12){[EleanorRomance.MaidV2State]::GapPaid=$true}
        if($code -ge 13){[EleanorRomance.MaidV2State]::Enter()}
        [EleanorRomance.MaidState]::Confession=1
    }
    if($kind -eq 'rev'){
        [EleanorRomance.RomanceState]::Route=3
        [EleanorRomance.RomanceState]::MotherState=2
        [EleanorRomance.RomanceState]::RevVersion=2
    }
    if($kind -eq 'love'){
        [EleanorRomance.LoveState]::Start(70)
        [EleanorRomance.StoryCoreFacts]::Record(20)
    }
    if($kind -eq 'dual'){
        [EleanorRomance.DualState]::Accept()
    }
}
function Modern-Client($kind,$code){
    switch($kind){
        maid {return [EleanorRomance.MaidV2Story]::CreateChapter([int]$code)}
        rev {return [EleanorRomance.RevolutionV2Story]::CreateChapter([int]$code)}
        love {return [EleanorRomance.LoveStory]::CreateVisit([int]$code)}
        dual {return [EleanorRomance.DualStory]::CreateScene([string]$code)}
    }
}
function Graph-Paths($head,$prefix=''){
    $n=$head;$guard=0
    while($n){
        if(++$guard -gt 2000){throw 'Dialogue cycle'}
        if($n.choices.Count){
            for($i=0;$i -lt $n.choices.Count;$i++){
                $reply=$n.choices[$i].Item2
                if($reply -and $reply.endAction -ne $null){
                    throw 'Choice reply endAction is skipped by native SelectChoice: '+$reply.plainText
                }
                Graph-Paths $reply ($prefix+[string]$i)
            }
            return
        }
        $n=$n.nextDialogue
    }
    return ,$prefix
}
function Play-Path($head,$path=''){
    $n=$head;$p=0;$guard=0
    while($n){
        if(++$guard -gt 2000){throw 'Dialogue cycle during playback'}
        if($n.startAction){$n.startAction.Invoke()}
        $n.ExecuteEndAction()
        if($n.choices.Count){
            $i=if($p -lt $path.Length){[int]::Parse($path.Substring($p++,1))}else{0}
            Assert ($n.IsChoiceEnabledAt($i)) "Disabled authored choice $i"
            $c=$n.choices[$i]
            if($c.Item3){$c.Item3.Invoke()}
            $n=$c.Item2
        }else{$n=$n.nextDialogue}
    }
}
function Graph-Text($head){
    $pending=[System.Collections.Generic.Stack[Dialogue]]::new()
    $seen=[System.Collections.Generic.HashSet[Dialogue]]::new()
    $pending.Push($head)
    while($pending.Count){
        $node=$pending.Pop()
        if(!$node -or !$seen.Add($node)){continue}
        $node.plainText
        if($node.nextDialogue){$pending.Push($node.nextDialogue)}
        foreach($option in $node.choices){if($option.Item2){$pending.Push($option.Item2)}}
    }
}
$qaPaths=0
$qaChoicePaths=0
$qaScenes=@{
    maid=@(8,12,13,14,15,16,17,18,19,20,21,22,25,26,27,28)
    rev=@(280..290)
    love=@(1..10)
    dual=@('dv01','dv02','dv03','dv04','dv05','dv06','dv07','dv08','dv09','dv10','dv11','dv12','dv13')
}
foreach($kind in @('maid','rev','love','dual')){
    foreach($code in $qaScenes[$kind]){
        Reset-Modern $kind $code
        $client=Modern-Client $kind $code
        Assert ($null -ne $client) "Missing $kind $code"
        $paths=@(Graph-Paths $client.mainDialogue)
        $qaChoicePaths += $paths.Count
        "SCENE: $kind $code | $($paths.Count) choice paths | $($paths.Count * 4) replays | $($client.clientIntent)"
        foreach($path in $paths){
            foreach($deal in @('none','fair','discount','over')){
                Reset-Modern $kind $code
                $client=Modern-Client $kind $code;Use-Client $client
                Assert ([PlayerStore]::TestReleaseStock.Count -eq 0) "$kind $code spawned stock while queueing"
                Play-Path $client.mainDialogue $path
                if($client.tryInterceptDismiss){
                    if($deal -ne 'none'){
                        $item=New-Item '1' 1;$item.unitValue=100
                        $paid=switch($deal){fair{100}discount{70}over{150}}
                        if($client.clientIntent -eq [StoreClient+ClientIntent]::SELL){
                            $store.playerCash-=$paid
                            $client.onItemSold.Invoke($item,$paid)
                        }else{$client.onItemBought.Invoke($item,$paid)}
                    }
                    $store.TryPlayerDismissCurrentClient($true)
                    Play-Path $ui.currentDialog
                }
                switch($kind){
                    maid {
                        if($code -eq 15){
                            $expected=switch($path){'0'{17}'1'{16}'2'{280}}
                            Assert ([EleanorRomance.RomanceState]::Chapter -eq $expected) "Maid world choice $path lost its result"
                        }else{
                            Assert ([EleanorRomance.MaidState]::Progress -ge $code) "Maid $code did not commit"
                        }
                    }
                    rev {Assert ([EleanorRomance.RevolutionV2State]::Progress -eq $code) "Revolution $code did not commit"}
                    love {
                        Assert ([EleanorRomance.LoveState]::PendingVisit -eq 0 -and [EleanorRomance.LoveState]::LastImportantDay -eq 80) "Love $code did not commit"
                    }
                    dual {Assert ([EleanorRomance.DualState]::Done($code)) "Harem $code did not commit"}
                }
                $qaPaths++
            }
        }
    }
}
"PASS: $qaChoicePaths distinct authored choice paths, $qaPaths choice/trade replays across 50 scenes."

# The rewritten closing choices must lead to the chosen person's reaction;
# hugging both goes straight to the shared ending, without a kiss reaction.
foreach($option in 0..2){
    Reset-Modern dual dv13
    $client=Modern-Client dual dv13;Use-Client $client
    $choice=Choice-Node $client.mainDialogue
    $head=$choice.choices[$option].Item2
    $text=@(Graph-Text $head)
    Assert (($text -contains 'er_dual_dv13_b002_01') -eq ($option -eq 0)) 'Closing kiss selected the wrong Eleanor reaction'
    Assert (($text -contains 'er_dual_dv13_b003_01') -eq ($option -eq 1)) 'Closing kiss selected the wrong Lydia reaction'
    Assert (@($text | Where-Object {$_ -eq 'er_dual_dv13_b004_01'}).Count -eq 1) 'Closing branch lost or duplicated the common ending'
    $history=@(Render-ArrestHistory $head)
    Assert (@($history | Where-Object {$_ -eq 'er_name_lydia: er_dual_dv13_b004_04'}).Count -eq 1) 'Closing companion line recorded as Eleanor'
    Assert ($history -contains 'name_eleanor: er_dual_dv13_b004_03') 'Eleanor closing line recorded as the companion'
    if($option -eq 0){
        Assert ($history -contains 'name_eleanor: er_dual_dv13_b002_01') 'Eleanor kiss recorded under the wrong name'
    }
    if($option -eq 1){
        Assert ($history -contains 'er_name_lydia: er_dual_dv13_b003_01') 'Lydia kiss recorded under the wrong name'
    }
    Play-Path $client.mainDialogue ([string]$option)
    Assert ([EleanorRomance.DualState]::Done('dv13') -and [EleanorRomance.DualState]::Relation -eq 2 -and
        [EleanorRomance.DualState]::Flag('d.act4')) 'Closing choice failed to confirm the relationship'
}
foreach($scene in @('dv12','dv13')){
    Reset-Modern dual $scene
    foreach($block in 1..4){[EleanorRomance.DualState]::SetInt(('cursor.'+$scene+'_b'+$block.ToString('000')),99)}
    [EleanorRomance.DualState]::SetInt('cursor.dv11_b004',8)
    $client=Modern-Client dual $scene
    Assert ($client.mainDialogue.plainText -eq ('er_dual_'+$scene+'_b001_01')) 'Old closing offset skipped the new manuscript'
    foreach($block in 1..4){Assert ([EleanorRomance.DualState]::Int(('cursor.'+$scene+'_b'+$block.ToString('000'))) -eq 0) 'Old closing branch offset survived the rewrite'}
    Assert ([EleanorRomance.DualState]::Int('cursor.dv11_b004') -eq 8) 'Closing migration touched an earlier scene'
    $client.mainDialogue.ExecuteEndAction()
    $saved=[System.Collections.Generic.Dictionary[string,string]]::new([ModHelper]::TestData)
    [ModHelper]::TestData=[System.Collections.Generic.Dictionary[string,string]]::new($saved)
    $resumed=Modern-Client dual $scene
    Assert ($resumed.mainDialogue.plainText -eq ('er_dual_'+$scene+'_b001_02')) 'Updated closing scene failed to resume normally'
    Reset-Modern dual $scene
    [EleanorRomance.DualState]::Complete($scene)
    [EleanorRomance.DualState]::SetInt(('cursor.'+$scene+'_b001'),4)
    $null=Modern-Client dual $scene
    Assert ([EleanorRomance.DualState]::Done($scene) -and [EleanorRomance.DualState]::Int(('cursor.'+$scene+'_b001')) -eq 4) 'Rewrite reset an already completed scene'
}
'PASS: rewritten closing kiss/hug branches, companion names, relationship commit and one-time old-offset recovery.'

# Ordinary new runs must reach the confession without buying a maid project.
Reset-Modern common 20
[EleanorRomance.RomanceState]::NextVisitDay=70
Assert ([EleanorRomance.StoryDirector]::PendingChapterForDay(80) -eq 20) 'Ordinary confession blocked by P2'
Reset-Modern common 12
$client=[EleanorRomance.MaidV2Story]::CreateEntry();Use-Client $client
Assert ($client.mainDialogue.plainText -like 'er_v06_c11*') 'Common final savings scene was skipped'
Play-Path $client.mainDialogue
Assert ([EleanorRomance.RomanceState]::Chapter -eq 13 -and [EleanorRomance.RomanceState]::Savings -eq 800 -and ![EleanorRomance.MaidState]::Active) 'Common savings fallback failed'

# A declined/unsettled v2 detective-fee choice must not idle until day 42 or
# force the paid-job entry text.
Reset-Modern maid 12
[EleanorRomance.MaidV2State]::GapPaid=$false
[EleanorRomance.MaidV2State]::GapChoice=2
[EleanorRomance.RomanceState]::Savings=500
$station.dayCounter=31
Assert ([EleanorRomance.StoryDirector]::PendingChapterForDay(31) -eq 12) 'Declined v2 gap choice keeps chapter 12 blocked before day 42'
$client=Modern-Client maid 12;Use-Client $client
Assert ($client.mainDialogue.plainText -like 'er_v06_c11*') 'Declined v2 gap choice forced the paid entry scene'
Play-Path $client.mainDialogue
Assert ([EleanorRomance.RomanceState]::Chapter -eq 13 -and [EleanorRomance.RomanceState]::Savings -eq 800 -and ![EleanorRomance.MaidState]::Active) 'Declined v2 gap fallback did not advance'

# Promise precedes stock/trading; actual payment occurs only when the wrap starts.
foreach($choice in @('0','1')){
    Reset-Modern maid 8
    $store.playerCash=500
    $client=Modern-Client maid 8;Use-Client $client
    Play-Path $client.mainDialogue $choice
    Assert ([PlayerStore]::TestReleaseStock.Count -eq 1 -and $store.playerCash -eq 500) 'Promise charged or stock missing'
    $item=New-Item '1' 1;$item.unitValue=100
    $store.playerCash-=100;$client.onItemSold.Invoke($item,100)
    Assert ($store.playerCash -eq 400 -and ![EleanorRomance.MaidV2State]::GapPaid) 'Prepared native wrap charged the loan'
    # Simulate restoring the uncommitted customer after the native sale was saved.
    $client=Modern-Client maid 8;Use-Client $client
    Play-Path $client.mainDialogue
    Assert ([PlayerStore]::TestReleaseStock.Count -eq 1) 'Reload duplicated scavenger stock'
    $store.TryPlayerDismissCurrentClient($true);Play-Path $ui.currentDialog
    $expected=if($choice -eq '0'){200}else{400}
    Assert ($store.playerCash -eq $expected) "Incorrect shortfall debit for choice $choice"
    Assert ([EleanorRomance.MaidV2State]::GapPaid -eq ($choice -eq '0')) 'Loan result disagrees with choice'
    $before=$store.playerCash
    Play-Path $ui.currentDialog
    Assert ($store.playerCash -eq $before) 'Loan duplicated on repeated playback'
}
Reset-Modern maid 8
$store.playerCash=299
$client=Modern-Client maid 8
Assert (!(Choice-Node $client.mainDialogue).IsChoiceEnabledAt(0)) 'Unaffordable promise is enabled'
'PASS: ordinary new-run confession, common savings scene, pre-trade promise, exact loan, refusal, affordability and interrupted-visit recovery.'

# Stock exists at visit time, all leaf choices reach it, and replay cannot duplicate it.
foreach($code in @(280,287,288,289)){
    Reset-Modern rev $code
    $client=Modern-Client rev $code;Use-Client $client
    Play-Path $client.mainDialogue
    $expected=if($code -in @(287,288)){3}else{2}
    Assert ([PlayerStore]::TestReleaseStock.Count -eq $expected) "Revolution $code has no selling stock: $([PlayerStore]::TestReleaseStock -join ',')"
    Play-Path $client.mainDialogue
    Assert ([PlayerStore]::TestReleaseStock.Count -eq $expected) "Revolution $code duplicates stock"
}
'PASS: all four revolution sellers produce stock after dialogue exactly once.'

# Same-day harem follow-up must pass through the public scheduler.
Reset-Modern dual 20
foreach($id in @('entry','dv01','dv02','dv03','dv04')){[EleanorRomance.DualState]::Complete($id)}
[EleanorRomance.DualState]::SetSceneDay('dv04',80)
[EleanorRomance.RomanceState]::LastQueuedDay=80
[EleanorRomance.MaidState]::LastImportantDay=80
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
Assert ($store.storeClientManager.clientStack.Count -eq 1 -and [EleanorRomance.DualState]::PendingId -eq 'dv05') 'Evening visit blocked by public scheduler'
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
Assert ($store.storeClientManager.clientStack.Count -eq 1) 'Evening visit queued twice'
Reset-Modern love 21
[EleanorRomance.LoveState]::PendingVisit=1
[EleanorRomance.LoveState]::NextVisitDay=80
[EleanorRomance.RomanceState]::LastQueuedDay=80
[EleanorRomance.StoryDirector]::OnGenerateCustomerVeryEarly($store.storeClientManager)
Assert ($store.storeClientManager.clientStack.Count -eq 1 -and [EleanorRomance.LoveState]::PendingVisit -eq 1) 'Love visit cannot recover on its reserved day'
'PASS: public scheduler accepts exactly one same-day harem follow-up and recovers an interrupted love visit.'

Reset-Modern love 24
$client=Modern-Client love 10;Use-Client $client
$n=$client.mainDialogue;$sec=$null
while($n){if($n.title -eq 'SEC'){$sec=$n;break};$n=$n.nextDialogue}
Assert ($null -ne $sec) 'Wedding has no companion dialogue'
$history=@(Render-ArrestHistory $sec)
Assert ($history[0] -like 'er_name_sophia:*') 'Wedding companion is recorded as Eleanor'
'PASS: wedding companion records Sophia through the native dialogue dispatcher.'

# World facts must select the right dialogue in the actual clients, including
# the later contract settlement after returning the drive.
foreach($route in @(1,2)){
    Reset-Modern love 21
    [EleanorRomance.RomanceState]::Route=$route
    $client=Modern-Client love 8
    $expected=if($route -eq 1){'er_love_la3_s2_pre_e1_01'}else{'er_love_la3_s2_pre_01'}
    Assert ($client.mainDialogue.plainText -eq $expected) 'Proposal used the wrong research outcome'
    foreach($small in @($false,$true)){
        [EleanorRomance.LoveState]::A3SmallWedding=$small
        $client=Modern-Client love 10
        $block=if($small){'la3_s4_small'}else{'la3_s4'}
        if($route -eq 1){$block+='_e1'}
        Assert ($client.mainDialogue.plainText -eq "er_love_${block}_01") 'Wedding ignored the guest list or research outcome'
    }
    foreach($code in @(17,26)){
        Reset-Modern maid $code
        [EleanorRomance.RomanceState]::Route=$route
        $client=Modern-Client maid $code;Use-Client $client
        $block=if($code -eq 17){'v2_a1c5'}else{'v2_a4c2'}
        $block+=if($route -eq 1){'_exchange'}else{'_s0'}
        Assert ($client.mainDialogue.plainText -eq "er_maid_v2_${block}_01") 'Maid dialogue used the wrong drive/mother facts'
        Play-Path $client.mainDialogue
        if($client.tryInterceptDismiss){$store.TryPlayerDismissCurrentClient($true);Play-Path $ui.currentDialog}
        Assert ([EleanorRomance.MaidState]::Progress -ge $code) 'Conditional maid scene failed to commit'
    }
}
Reset-Modern dual 20
$haremPage=[EleanorRomance.EndingPages].GetMethod('HaremPage',[Reflection.BindingFlags]'NonPublic,Static')
[EleanorRomance.RomanceState]::WorkState=0
Assert ($haremPage.Invoke($null,@()).Text -notlike '*er_ending_harem_01*') 'Rewrite ending invents an upper-level job'
[EleanorRomance.RomanceState]::WorkState=1
Assert ($haremPage.Invoke($null,@()).Text -like '*er_ending_harem_01*') 'Existing job ending was lost'

$endingShapes=@{}
foreach($route in @(1,2)){
    Reset-Modern love 24
    [EleanorRomance.RomanceState]::Route=$route
    [EleanorRomance.LoveState]::Act3Done=$true
    $page=[EleanorRomance.EndingPages]::TryLoveCompletion()
    Assert ($null -ne $page) 'Safe research choice lost the romance ending'
    $endingShapes["love$route"]=$page.TitleKey+'|'+$page.CgKey
    $flavor=if($route -eq 1){'research_returned'}else{'research_public'}
    Assert ($page.Text -like "*er_ending_love_02_${flavor}*") 'Romance ending lost its research background'
    Reset-Modern maid 28
    [EleanorRomance.RomanceState]::Route=$route
    [EleanorRomance.RomanceState]::Romance=1
    [EleanorRomance.MaidState]::Contract=3
    [EleanorRomance.MaidState]::Progress=28
    $page=[EleanorRomance.EndingPages]::TryMaidCompletion()
    Assert ($null -ne $page) 'Safe research choice lost the attendant ending'
    $endingShapes["maid$route"]=$page.TitleKey+'|'+$page.CgKey
    Reset-Modern dual 20
    [EleanorRomance.RomanceState]::Route=$route
    [EleanorRomance.DualState]::Complete('dv13')
    [EleanorRomance.DualState]::Relation=2
    $page=[EleanorRomance.EndingPages]::TryDualCompletion()
    Assert ($null -ne $page) 'Safe research choice lost the harem ending'
    $endingShapes["dual$route"]=$page.TitleKey+'|'+$page.CgKey
}
foreach($kind in @('love','maid','dual')){
    Assert ($endingShapes["${kind}1"] -eq $endingShapes["${kind}2"]) 'Research choice split a relationship ending'
}
'PASS: both safe research choices reach the same three relationship endings; research background remains conditional.'
'PASS: both research outcomes, both wedding guest lists, mother/drive facts, and conditional harem ending.'

# Current common chapters: carry every drive/threat answer through to its
# commit, and verify all three world decisions through the actual trade wrap.
foreach($code in @(13,14)){
    Reset-Modern common $code
    $paths=@(Graph-Paths ([EleanorRomance.EleanorStory]::CreateClient($code)).mainDialogue)
    foreach($path in $paths){
        Reset-Modern common $code
        $client=[EleanorRomance.EleanorStory]::CreateClient($code);Use-Client $client
        Play-Path $client.mainDialogue $path
        Assert ([EleanorRomance.RomanceState]::Chapter -eq ($code+1)) "Common $code choice failed to advance"
        if($code -eq 13){Assert ([EleanorRomance.RomanceState]::DriveState -eq 1) 'Common drive custody lost'}
    }
}
Reset-Modern common 15
$paths=@(Graph-Paths ([EleanorRomance.EleanorStory]::CreateClient(15)).mainDialogue)
foreach($path in $paths){
    Reset-Modern common 15
    $client=[EleanorRomance.EleanorStory]::CreateClient(15);Use-Client $client
    Play-Path $client.mainDialogue $path
    if($path.StartsWith('1')){
        Assert ($client.clientIntent -eq [StoreClient+ClientIntent]::BUY) 'Protect decision did not open trade'
        $store.TryPlayerDismissCurrentClient($true);Play-Path $ui.currentDialog
    }
    $expectedRoute=switch($path.Substring(0,1)){'0'{1}'1'{2}'2'{3}}
    $expectedChapter=if($expectedRoute -eq 3){280}else{18}
    $expectedMother=if($expectedRoute -eq 3){2}else{1}
    Assert ([EleanorRomance.RomanceState]::Route -eq $expectedRoute -and
        [EleanorRomance.RomanceState]::Chapter -eq $expectedChapter -and
        [EleanorRomance.RomanceState]::MotherState -eq $expectedMother) 'Common world decision committed the wrong outcome'
}
foreach($route in @(1,2)){
    foreach($choice in @('0','1')){
        Reset-Modern common 20
        [EleanorRomance.RomanceState]::Route=$route
        [EleanorRomance.RomanceState]::FairDeals=3
        [EleanorRomance.RomanceState]::UncleReported=$true
        $client=[EleanorRomance.EleanorStory]::CreateClient(20);Use-Client $client
        Assert ($null -ne (Choice-Node $client.mainDialogue)) 'Eligible confession lost its relationship choice'
        $expectedBlock=if($route -eq 1){'er_v06_c19_b101_return_01'}else{'er_v06_c19_b101_01'}
        Assert ($client.mainDialogue.plainText -eq $expectedBlock) 'Confession invented a guard after the case was dismissed'
        Play-Path $client.mainDialogue $choice
        $expected=if($choice -eq '0'){1}else{3}
        Assert ([EleanorRomance.RomanceState]::Romance -eq $expected -and
            [EleanorRomance.StoryCoreFacts]::Done(20) -and
            [EleanorRomance.RomanceState]::Chapter -eq 21) 'Confession/friendship failed to commit'
    }
}
Reset-Modern common 20
[EleanorRomance.RomanceState]::FairDeals=2
[EleanorRomance.RomanceState]::UncleReported=$true
$client=[EleanorRomance.EleanorStory]::CreateClient(20);Use-Client $client
Assert ($null -eq (Choice-Node $client.mainDialogue)) 'Ineligible confession offered a relationship choice'
Play-Path $client.mainDialogue
Assert ([EleanorRomance.RomanceState]::Romance -eq 3) 'Ineligible confession failed to remain friends'
'PASS: common drive/threat choices, all three world outcomes, eligible love/friend choices and the positive-deal boundary.'
foreach($rescued in @($false,$true)){
    Reset-Modern common 15
    [EleanorRomance.RomanceState]::Rescued=$rescued
    $text=@(Graph-Text ([EleanorRomance.EleanorStory]::CreateClient(15)).mainDialogue)
    Assert (($text -contains 'er_v06_c14_b003_05') -eq $rescued) 'Protection plan invented a prior arrest'
}
foreach($gift in @(1,3)){
    Reset-Modern common 20
    [EleanorRomance.RomanceState]::FairDeals=3
    [EleanorRomance.RomanceState]::UncleReported=$true
    [EleanorRomance.RomanceState]::ReturnGiftChoice=$gift
    $text=@(Graph-Text ([EleanorRomance.EleanorStory]::CreateClient(20)).mainDialogue)
    Assert ($text -contains 'er_v06_c19_b102_13' -and
        ($text -notcontains 'er_v06_c19_b102_15')) 'Confession closing was truncated or retained removed component callback'
}
'PASS: common protection/confession preserve arrest, release and gift history without component callback.'
