[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$expectedUnityVersion = '6000.3.20f1'

function Assert-Condition {
    param(
        [Parameter(Mandatory)]
        [bool]$Condition,

        [Parameter(Mandatory)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$projectVersionPath = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$projectVersion = Get-Content -LiteralPath $projectVersionPath -Raw
Assert-Condition ($projectVersion -match [regex]::Escape($expectedUnityVersion)) `
    "Expected Unity $expectedUnityVersion in ProjectVersion.txt."

$editorSettings = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/EditorSettings.asset') -Raw
Assert-Condition ($editorSettings -match 'm_SerializationMode:\s*2') `
    'Asset Serialization Mode must be Force Text.'

$versionControlSettings = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/VersionControlSettings.asset') -Raw
Assert-Condition ($versionControlSettings -match 'm_Mode:\s*Visible Meta Files') `
    'Version Control Mode must be Visible Meta Files.'

$manifestPath = Join-Path $projectRoot 'Packages/manifest.json'
$lockPath = Join-Path $projectRoot 'Packages/packages-lock.json'
Assert-Condition (Test-Path -LiteralPath $manifestPath) 'Packages/manifest.json is missing.'
Assert-Condition (Test-Path -LiteralPath $lockPath) 'Packages/packages-lock.json is missing.'

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
foreach ($packageName in @('com.unity.inputsystem', 'com.unity.test-framework', 'com.unity.ugui')) {
    Assert-Condition ($null -ne $manifest.dependencies.$packageName) `
        "Required package '$packageName' is missing from Packages/manifest.json."
}

$coreAssemblyDefinitionPath = Join-Path $projectRoot 'Assets/_Project/Scripts/Core/RandomTowerDefense.Core.asmdef'
$coreAssemblyDefinition = Get-Content -LiteralPath $coreAssemblyDefinitionPath -Raw | ConvertFrom-Json
Assert-Condition ($coreAssemblyDefinition.noEngineReferences -eq $true) `
    'RandomTowerDefense.Core must not reference UnityEngine.'

$assetsPath = Join-Path $projectRoot 'Assets'
$missingMetaFiles = @()
Get-ChildItem -LiteralPath $assetsPath -Recurse -Force | ForEach-Object {
    if ($_.Name.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    $metaPath = "$($_.FullName).meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $missingMetaFiles += $_.FullName.Substring($projectRoot.Length + 1)
    }
}

Assert-Condition ($missingMetaFiles.Count -eq 0) `
    "Unity .meta files are missing for: $($missingMetaFiles -join ', ')"

$trackedPaths = & git -C $projectRoot ls-files
Assert-Condition ($LASTEXITCODE -eq 0) 'git ls-files failed.'
$forbiddenTrackedPaths = @($trackedPaths | Where-Object {
    $_ -match '^(Library|Temp|Obj|Build|Builds|Logs|UserSettings)/'
})
Assert-Condition ($forbiddenTrackedPaths.Count -eq 0) `
    "Generated Unity output is tracked: $($forbiddenTrackedPaths -join ', ')"

Write-Host "Project layout validation passed for Unity $expectedUnityVersion."
