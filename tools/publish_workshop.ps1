# Updates the existing owned item using the game's bundled Steamworks client.
# No credentials or new item are created. -CheckOnly performs authentication only.
param(
    [string]$GameRoot='D:/Program Files (x86)/Steam/steamapps/common/Probably Stolen',
    [string]$ContentFolder,
    [string]$DescriptionFile,
    [string]$NotesFile,
    [string]$ResultFile,
    [string]$Version='0.77',
    [switch]$CheckOnly
)
$ErrorActionPreference='Stop'
$expectedApp=4348910
[UInt64]$expectedOwner=76561198109201343
[UInt64]$expectedItem=3834588749
$data=Join-Path $GameRoot 'Probably Stolen_Data'
$env:SteamAppId=[string]$expectedApp
$env:SteamGameId=[string]$expectedApp
$env:PATH=(Join-Path $data 'Plugins/x86_64')+';'+$env:PATH
$null=[Reflection.Assembly]::LoadFrom((Join-Path $data 'Managed/com.rlabrecque.steamworks.net.dll'))
$initialized=$false
$buffer=[IntPtr]::Zero
try {
    if(![Steamworks.SteamAPI]::Init()){throw 'SteamAPI.Init failed. Steam must be running and logged in.'}
    $initialized=$true
    if(![Steamworks.SteamUser]::BLoggedOn()){throw 'Steam client is not logged in.'}
    if([Steamworks.SteamUtils]::GetAppID().m_AppId -ne $expectedApp){throw 'Steam application ID mismatch.'}
    if([Steamworks.SteamUser]::GetSteamID().m_SteamID -ne $expectedOwner){throw 'Steam account does not own the intended Workshop item.'}
    Write-Output 'Steam client and original item owner verified.'
    if($CheckOnly){return}
    if(!$ContentFolder -or !$DescriptionFile -or !$NotesFile -or !$ResultFile){throw 'Content, description, notes, and result paths are required.'}
    $folder=[IO.Path]::GetFullPath($ContentFolder)
    [xml]$manifest=Get-Content -LiteralPath (Join-Path $folder 'manifest.xml') -Raw
    if($manifest.Manifest.ID -ne 'eleanor_romance' -or $manifest.Manifest.ModVersion -ne $Version){throw 'Package ID/version mismatch.'}
    $inventoryPath=Join-Path (Split-Path $folder -Parent) 'release-files.json'
    $inventory=Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
    foreach($file in $inventory){
        $actual=Join-Path $folder $file.path
        if((Get-FileHash -LiteralPath $actual -Algorithm SHA256).Hash -ne $file.sha256){throw ('Staged file changed: '+$file.path)}
    }
    if(@(Get-ChildItem -LiteralPath $folder -File -Recurse).Count -ne $inventory.Count){throw 'Untracked files in release content.'}
    $description=Get-Content -LiteralPath $DescriptionFile -Raw -Encoding utf8
    $notes=Get-Content -LiteralPath $NotesFile -Raw -Encoding utf8
    if([Text.Encoding]::UTF8.GetByteCount($description) -ge 8000){throw 'Description exceeds conservative 8000-byte limit.'}
    $app=[Steamworks.AppId_t]::new($expectedApp)
    $item=[Steamworks.PublishedFileId_t]::new($expectedItem)
    $handle=[Steamworks.SteamUGC]::StartItemUpdate($app,$item)
    if(![Steamworks.SteamUGC]::SetItemContent($handle,$folder)){throw 'SetItemContent failed.'}
    if(![Steamworks.SteamUGC]::SetItemDescription($handle,$description)){throw 'SetItemDescription failed.'}
    $call=[Steamworks.SteamUGC]::SubmitItemUpdate($handle,$notes)
    $clock=[Diagnostics.Stopwatch]::StartNew()
    $lastStatus=''
    $complete=$false
    while($clock.Elapsed.TotalSeconds -lt 180){
        # Keep this completion handle for the explicit GetAPICallResult below.
        $failed=$false
        if([Steamworks.SteamUtils]::IsAPICallCompleted($call,[ref]$failed)){
            if($failed){throw 'Steam update API call failed.'}
            $complete=$true;break
        }
        [UInt64]$processed=0;[UInt64]$total=0
        $status=[string][Steamworks.SteamUGC]::GetItemUpdateProgress($handle,[ref]$processed,[ref]$total)
        if($status -ne $lastStatus){Write-Output ("Workshop update: {0} ({1}/{2} bytes)" -f $status,$processed,$total);$lastStatus=$status}
        Start-Sleep -Milliseconds 500
    }
    if(!$complete){throw 'Steam update result timed out; verify remote state before submitting again.'}
    $size=[Runtime.InteropServices.Marshal]::SizeOf([type][Steamworks.SubmitItemUpdateResult_t])
    $buffer=[Runtime.InteropServices.Marshal]::AllocHGlobal($size)
    $failed=$false
    $ok=[Steamworks.SteamUtils]::GetAPICallResult($call,$buffer,$size,[Steamworks.SubmitItemUpdateResult_t]::k_iCallback,[ref]$failed)
    if(!$ok -or $failed){throw 'Could not retrieve Steam update result.'}
    $result=[Runtime.InteropServices.Marshal]::PtrToStructure($buffer,[type][Steamworks.SubmitItemUpdateResult_t])
    $report=[pscustomobject]@{
        version=$Version;item=[string]$result.m_nPublishedFileId.m_PublishedFileId
        result=[string]$result.m_eResult;needsLegalAgreement=$result.m_bUserNeedsToAcceptWorkshopLegalAgreement
        elapsedSeconds=[Math]::Round($clock.Elapsed.TotalSeconds,2)
        localBytes=($inventory|Measure-Object length -Sum).Sum
        localFiles=$inventory.Count;completedUtc=[DateTime]::UtcNow.ToString('o')
    }
    $report|ConvertTo-Json|Set-Content -LiteralPath $ResultFile -Encoding utf8
    $report
    if($result.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or $result.m_nPublishedFileId.m_PublishedFileId -ne $expectedItem){throw 'Steam did not confirm the intended item update.'}
    if($result.m_bUserNeedsToAcceptWorkshopLegalAgreement){throw 'Steam requires the owner to accept its Workshop legal agreement.'}
} finally {
    if($buffer -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::FreeHGlobal($buffer)}
    if($initialized){[Steamworks.SteamAPI]::Shutdown()}
}
