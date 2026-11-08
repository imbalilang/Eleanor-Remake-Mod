param([Parameter(Mandatory=$true)][string]$Managed,[Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference='Stop'
foreach($name in @('UnityEngine.CoreModule','0Harmony','DseCecil','Assembly-CSharp')){
    $assembly=[Reflection.Assembly]::LoadFrom((Join-Path $Managed ($name+'.dll')))
}
$scanner=$assembly.GetType('ModScanner')
$method=$scanner.GetMethod('Scan',[Reflection.BindingFlags]'Public,NonPublic,Static')
[string[]]$dlls=@(Get-ChildItem -LiteralPath $Package -Filter '*.dll' -File | ForEach-Object FullName)
[string[]]$allowed=@(Get-ChildItem -LiteralPath $Managed -Filter '*.dll' -File | ForEach-Object BaseName)
$result=$method.Invoke($null,[object[]]@($dlls,$allowed,[IO.Path]::GetFullPath($Package),$false))
$report=[pscustomobject]@{refused=$result.Refused;error=$result.Error;methods=$result.MethodsScanned;capabilities=@($result.Capabilities);hardFlags=@($result.HardFlags);findings=$result.Findings.Count}
$report|ConvertTo-Json -Depth 5
if($report.refused -or $report.error -or $report.hardFlags.Count -or $report.capabilities.Count){throw 'Native scanner did not return a capability-free package.'}
