[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [Alias('TestPlatform')]
    [ValidateSet('EditMode', 'PlayMode', 'ValidateData', 'BuildAndroid', 'ExportIos')]
    [string]$Task,

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
    $ResultsDirectory = Join-Path $projectRoot "TestResults/$Task"
}

New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$testResultsPath = Join-Path $ResultsDirectory 'results.xml'
$logPath = Join-Path $ResultsDirectory 'unity.log'
$isTestTask = $Task -in @('EditMode', 'PlayMode')

if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

if ($isTestTask -and (Test-Path -LiteralPath $testResultsPath)) {
    Remove-Item -LiteralPath $testResultsPath -Force
}

$unityArguments = @('-batchmode', '-nographics', '-projectPath', $projectRoot)
if ($isTestTask) {
    $unityArguments += @(
        '-runTests',
        '-testPlatform', $Task.ToLowerInvariant(),
        '-testResults', $testResultsPath,
        '-logFile', $logPath
    )
}
else {
    $methodByTask = @{
        ValidateData = 'RandomTowerDefense.Editor.Build.MobileBuildPipeline.ValidateData'
        BuildAndroid = 'RandomTowerDefense.Editor.Build.MobileBuildPipeline.BuildAndroid'
        ExportIos = 'RandomTowerDefense.Editor.Build.MobileBuildPipeline.ExportIos'
    }
    $buildTargetByTask = @{
        BuildAndroid = 'Android'
        ExportIos = 'iOS'
    }

    if ($buildTargetByTask.ContainsKey($Task)) {
        $unityArguments += @('-buildTarget', $buildTargetByTask[$Task])
    }

    $unityArguments += @(
        '-executeMethod', $methodByTask[$Task],
        '-logFile', $logPath,
        '-quit'
    )
}

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
    throw "Unity task $Task failed. See $logPath."
}

if ($isTestTask) {
    if (-not (Test-Path -LiteralPath $testResultsPath)) {
        throw "Unity $Task tests did not produce $testResultsPath. See $logPath."
    }

    Write-Host "Unity $Task tests passed. Results: $testResultsPath"
    return
}

$successMarker = if ($Task -eq 'ValidateData') {
    'AUTOMATION_DATA_VALIDATION_SUCCEEDED'
}
else {
    'AUTOMATION_PLAYER_BUILD_SUCCEEDED'
}
if (-not (Select-String -LiteralPath $logPath -SimpleMatch $successMarker -Quiet)) {
    throw "Unity task $Task did not report '$successMarker'. See $logPath."
}

$expectedOutput = switch ($Task) {
    'BuildAndroid' { Join-Path $projectRoot 'Builds/Validation/Android/RandomTowerDefense.apk' }
    'ExportIos' { Join-Path $projectRoot 'Builds/Validation/iOS/Unity-iPhone.xcodeproj/project.pbxproj' }
    default { $null }
}
if ($expectedOutput -and -not (Test-Path -LiteralPath $expectedOutput)) {
    throw "Unity task $Task did not create $expectedOutput. See $logPath."
}

Write-Host "Unity task $Task passed. Log: $logPath"
