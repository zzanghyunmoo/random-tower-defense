[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$TestPlatform,

    [string]$ResultsDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectVersionPath = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$projectVersionText = Get-Content -LiteralPath $projectVersionPath -Raw
$versionMatch = [regex]::Match($projectVersionText, 'm_EditorVersion:\s*(\S+)')
$isWindowsPlatform = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

if (-not $versionMatch.Success) {
    throw 'Could not read the Unity version from ProjectSettings/ProjectVersion.txt.'
}

$unityVersion = $versionMatch.Groups[1].Value
$unityEditorPath = $env:UNITY_EDITOR_PATH

if ([string]::IsNullOrWhiteSpace($unityEditorPath) -and $isWindowsPlatform) {
    $unityEditorPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
}

if ([string]::IsNullOrWhiteSpace($unityEditorPath) -or -not (Test-Path -LiteralPath $unityEditorPath)) {
    throw "Unity $unityVersion was not found. Set UNITY_EDITOR_PATH to the Unity executable."
}

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path $projectRoot "TestResults/$TestPlatform"
}

New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$testResultsPath = Join-Path $ResultsDirectory 'results.xml'
$logPath = Join-Path $ResultsDirectory 'unity.log'
$platformArgument = $TestPlatform.ToLowerInvariant()

if (Test-Path -LiteralPath $testResultsPath) {
    Remove-Item -LiteralPath $testResultsPath -Force
}

$unityArguments = @(
    '-batchmode',
    '-nographics',
    '-projectPath', $projectRoot,
    '-runTests',
    '-testPlatform', $platformArgument,
    '-testResults', $testResultsPath,
    '-logFile', $logPath
)

if ($isWindowsPlatform) {
    $windowsArguments = @($unityArguments | ForEach-Object {
        if ($_.Contains(' ')) {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    })

    $unityProcess = Start-Process `
        -FilePath $unityEditorPath `
        -ArgumentList $windowsArguments `
        -PassThru `
        -WindowStyle Hidden
    Wait-Process -Id $unityProcess.Id
    $unityProcess.Refresh()
    $unityExitCode = $unityProcess.ExitCode
}
else {
    & $unityEditorPath @unityArguments
    $unityExitCode = $LASTEXITCODE
}

if ($unityExitCode -ne 0) {
    throw "Unity $TestPlatform tests failed. See $logPath."
}

if (-not (Test-Path -LiteralPath $testResultsPath)) {
    throw "Unity $TestPlatform tests did not produce $testResultsPath. See $logPath."
}

Write-Host "Unity $TestPlatform tests passed. Results: $testResultsPath"
