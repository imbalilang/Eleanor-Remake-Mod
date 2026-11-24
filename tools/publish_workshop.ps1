# Updates the existing owned item using the game's bundled Steamworks client.
# No credentials or new item are created. -CheckOnly performs authentication only.
# Workshop change notes are owner-only: agents must not pass -NotesFile or write
# update notes. Content-only publishes may omit -NotesFile entirely.
param(
    [string]$GameRoot='D:/Program Files (x86)/Steam/steamapps/common/Probably Stolen',
    [string]$ContentFolder,
    [string]$DescriptionFile,
    [string]$NotesFile,
    [string]$ResultFile,
    [string]$Version='0.81',
    [ValidateSet('english','schinese')][string]$Language='english',
    [string]$Title,
    [switch]$Mature,
    [switch]$DescriptionOnly,
    [switch]$KeepDescription,
    [switch]$MetadataOnly,
    [switch]$ClearContentDescriptors,
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
    if(!$ResultFile){throw 'Result path is required.'}
    if($DescriptionOnly -and $MetadataOnly){throw '-DescriptionOnly and -MetadataOnly cannot be combined.'}
    if($DescriptionOnly -and $KeepDescription){throw '-DescriptionOnly and -KeepDescription cannot be combined.'}
    if($Mature -and $ClearContentDescriptors){throw '-Mature and -ClearContentDescriptors cannot be combined.'}
    if(!$MetadataOnly -and !$DescriptionOnly -and !$ContentFolder){
        throw 'Content folder and result paths are required unless -MetadataOnly is used.'
    }
    if(!$KeepDescription -and !$DescriptionFile){throw 'Description path is required unless -KeepDescription is used.'}
    $description=if($DescriptionFile){Get-Content -LiteralPath $DescriptionFile -Raw -Encoding utf8}else{''}
    $notes=if($NotesFile){Get-Content -LiteralPath $NotesFile -Raw -Encoding utf8}else{''}
    if(!$KeepDescription -and [Text.Encoding]::UTF8.GetByteCount($description) -ge 8000){throw 'Description exceeds conservative 8000-byte limit.'}
    $inventory=$null
    if(!$DescriptionOnly -and !$MetadataOnly){
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
    }
    $app=[Steamworks.AppId_t]::new($expectedApp)
    $item=[Steamworks.PublishedFileId_t]::new($expectedItem)
    $handle=[Steamworks.SteamUGC]::StartItemUpdate($app,$item)
    if(![Steamworks.SteamUGC]::SetItemUpdateLanguage($handle,$Language)){throw 'SetItemUpdateLanguage failed.'}
    if($Title -and ![Steamworks.SteamUGC]::SetItemTitle($handle,$Title)){throw 'SetItemTitle failed.'}
    if(!$DescriptionOnly -and !$MetadataOnly){
        if(![Steamworks.SteamUGC]::SetItemContent($handle,$folder)){throw 'SetItemContent failed.'}
    }
    if(!$KeepDescription){
        if(![Steamworks.SteamUGC]::SetItemDescription($handle,$description)){throw 'SetItemDescription failed.'}
    }
    $descriptors=@(
        [Steamworks.EUGCContentDescriptorID]::k_EUGCContentDescriptor_NudityOrSexualContent,
        [Steamworks.EUGCContentDescriptorID]::k_EUGCContentDescriptor_AnyMatureContent
    )
    if($Mature){
        foreach($descriptor in $descriptors){
            if(![Steamworks.SteamUGC]::AddContentDescriptor($handle,$descriptor)){throw 'Setting mature content descriptor failed.'}
        }
    }
    if($ClearContentDescriptors){
        foreach($descriptor in $descriptors){
            if(![Steamworks.SteamUGC]::RemoveContentDescriptor($handle,$descriptor)){throw 'Removing mature content descriptor failed.'}
        }
    }
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
    $localBytes=$null
    $localFiles=$null
    if($inventory){$localBytes=($inventory|Measure-Object length -Sum).Sum;$localFiles=$inventory.Count}
    $report=[pscustomobject]@{
        version=$Version;item=[string]$result.m_nPublishedFileId.m_PublishedFileId
        language=$Language
        result=[string]$result.m_eResult;needsLegalAgreement=$result.m_bUserNeedsToAcceptWorkshopLegalAgreement
        mode=$(if($DescriptionOnly){'description-only'}elseif($MetadataOnly){'metadata-only'}elseif($KeepDescription){'content-only'}else{'content-and-description'})
        matureDescriptorsRequested=[bool]$Mature
        contentDescriptorsCleared=[bool]$ClearContentDescriptors
        elapsedSeconds=[Math]::Round($clock.Elapsed.TotalSeconds,2)
        localBytes=$localBytes
        localFiles=$localFiles;completedUtc=[DateTime]::UtcNow.ToString('o')
    }
    $report|ConvertTo-Json|Set-Content -LiteralPath $ResultFile -Encoding utf8
    $report
    if($result.m_eResult -ne [Steamworks.EResult]::k_EResultOK -or $result.m_nPublishedFileId.m_PublishedFileId -ne $expectedItem){throw 'Steam did not confirm the intended item update.'}
    if($result.m_bUserNeedsToAcceptWorkshopLegalAgreement){throw 'Steam requires the owner to accept its Workshop legal agreement.'}
} finally {
    if($buffer -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::FreeHGlobal($buffer)}
    if($initialized){[Steamworks.SteamAPI]::Shutdown()}
}
