---
title: Unity Android Build Support can have an incomplete toolchain
date: 2026-07-20
last_updated: 2026-07-22
category: build-errors
module: Unity mobile build automation
problem_type: build_error
component: tooling
symptoms:
  - "Unity Hub's embedded AndroidPlayer directories appear to be missing adb or a complete NDK"
  - "Static inspection of the embedded toolchain suggests that the Android build cannot succeed"
  - "BuildAndroid succeeds because Unity resolves persisted external SDK and NDK roots instead"
root_cause: scope_issue
resolution_type: workflow_improvement
severity: medium
tags: [unity, android, sdk, ndk, il2cpp, external-tools, build-automation, diagnostics]
---

# Unity Android Build Support can have an incomplete toolchain

## Problem

A preflight that inspects only Unity Hub's embedded `AndroidPlayer` SDK and NDK directories can report a false blocker. Unity may resolve a complete SDK and NDK from its persisted External Tools settings while continuing to use the Hub-provided JDK and Gradle.

The durable question is therefore not whether one assumed directory is complete. It is which tool paths the actual build resolves and whether that build produces a valid artifact. A genuinely incomplete selected toolchain needs repair; an incomplete directory that Unity is not using may already be irrelevant.

## Symptoms

- Unity Hub shows Android Build Support, but fixed-path checks under the editor's embedded `AndroidPlayer` directory find missing SDK or NDK contents.
- A preflight concludes that Android validation is blocked before running the repository's build command.
- The real Unity log reports SDK and NDK roots outside the editor installation, sometimes alongside the Hub-provided JDK and Gradle.
- `BuildAndroid` can still complete, emit `AUTOMATION_PLAYER_BUILD_SUCCEEDED`, and create a non-empty APK.

On 2026-07-22, a fresh `main` validation reproduced this distinction: the embedded-path check reported missing NDK and `adb`, but Unity selected complete per-user SDK and NDK roots and successfully produced the Android validation APK. That machine-specific location is evidence for the run, not a path to commit as shared configuration.

## What Didn't Work

- Inspecting only a hard-coded Hub path and treating missing contents there as proof that Unity cannot build.
- Treating Unity Hub's selected-module state as proof that every Android payload is usable. It does not identify the effective SDK, NDK, and JDK roots for the build.
- Checking only top-level directory existence instead of required executables and metadata such as `adb.exe`, `sdkmanager.bat`, NDK `source.properties`, and `java.exe`.
- Trusting only one completion signal. A process exit code, a log line, or an old APK alone is weaker than the combined evidence of resolved paths, Unity build success, the automation marker, and a newly generated non-empty APK.
- Repairing the Hub installation before running the deterministic build and reading its log. This can cause unnecessary downloads or configuration changes when Unity already has a valid external toolchain selected.

## Solution

Run the repository's Android validation entry point first unless a known safety constraint prevents it:

```powershell
./scripts/Invoke-Unity.ps1 -Task BuildAndroid
```

The runner selects Android, invokes `MobileBuildPipeline.BuildAndroid`, and writes `TestResults/BuildAndroid/unity.log` (`scripts/Invoke-Unity.ps1:33`, `scripts/Invoke-Unity.ps1:60`, `scripts/Invoke-Unity.ps1:70`). The build pipeline validates project data and requires Android support, IL2CPP, and ARM64 before building (`Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:35`).

Next, inspect the paths Unity actually resolved:

```powershell
$log = 'TestResults/BuildAndroid/unity.log'

Select-String -LiteralPath $log -Pattern @(
    'JDK:',
    'Android SDK:',
    'Android NDK:',
    'cmdline-tools',
    'platform-tools',
    'build-tools',
    'adb'
)
```

Verify the concrete files under the roots reported by the current log. Use temporary variables; do not copy a developer-specific absolute path into tracked configuration.

```powershell
$sdkRoot = '<SDK root reported by Unity>'
$ndkRoot = '<NDK root reported by Unity>'
$jdkRoot = '<JDK root reported by Unity>'

Test-Path (Join-Path $sdkRoot 'platform-tools\adb.exe')
Get-ChildItem (Join-Path $sdkRoot 'cmdline-tools') -Filter sdkmanager.bat -Recurse
Get-ChildItem (Join-Path $sdkRoot 'build-tools')
Get-ChildItem (Join-Path $sdkRoot 'platforms')
Test-Path (Join-Path $ndkRoot 'source.properties')
Test-Path (Join-Path $jdkRoot 'bin\java.exe')
```

Then confirm the end-to-end success evidence:

```powershell
Select-String -LiteralPath $log -SimpleMatch 'AUTOMATION_PLAYER_BUILD_SUCCEEDED target=Android'

$apk = Get-Item 'Builds/Validation/Android/RandomTowerDefense.apk'
if ($apk.Length -le 0) { throw 'Android validation APK is empty.' }
Get-FileHash -Algorithm SHA256 -LiteralPath $apk.FullName
```

This mirrors the repository guards. `BuildAndroid` deletes a stale APK, builds, and rejects a missing or empty result (`Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:53`). `BuildPlayer` rejects non-success results and only then emits `AUTOMATION_PLAYER_BUILD_SUCCEEDED` (`Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:112`). The wrapper independently requires a zero Unity exit, the success marker, and the expected output path (`scripts/Invoke-Unity.ps1:105`, `scripts/Invoke-Unity.ps1:118`, `scripts/Invoke-Unity.ps1:128`).

Only when the effective toolchain is incomplete or the real build fails during tool discovery should it be repaired or replaced. A complete external toolchain can be supplied through all three environment variables together:

```powershell
$env:UNITY_ANDROID_SDK_ROOT = '<complete Android SDK root>'
$env:UNITY_ANDROID_NDK_ROOT = '<compatible Android NDK root>'
$env:UNITY_ANDROID_JDK_ROOT = '<compatible JDK root>'

./scripts/Invoke-Unity.ps1 -Task BuildAndroid

Remove-Item Env:UNITY_ANDROID_SDK_ROOT
Remove-Item Env:UNITY_ANDROID_NDK_ROOT
Remove-Item Env:UNITY_ANDROID_JDK_ROOT
```

The override is intentionally all-or-nothing. When `UNITY_ANDROID_SDK_ROOT` is set, the pipeline requires valid NDK and JDK roots, assigns all three, and restores the previous Unity External Tools settings afterward (`Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:134`, `Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:197`).

## Why This Works

The Unity log comes from the exact process performing the Android build, so its tool report reflects the roots selected for that execution. Scanning a different directory cannot answer which toolchain Unity used.

File-level checks then distinguish a directory that merely exists from one containing the executables and metadata the build requires. Finally, the pipeline and wrapper prevent stale or partial success from looking green by deleting the old APK, checking Unity's build result, requiring a dedicated success marker, and verifying the output artifact.

The resulting evidence chain is:

```text
actual BuildAndroid invocation
  -> Unity-reported effective roots
  -> required files under those roots
  -> player build success
  -> automation success marker
  -> newly generated non-empty APK
```

## Prevention

- Diagnose the toolchain Unity resolved for the current run, not a directory guessed from the Unity Hub installation layout.
- Keep machine-specific SDK, NDK, and JDK roots out of tracked configuration and documentation examples.
- Verify `adb`, `sdkmanager`, SDK build tools and platforms, NDK metadata, and Java at the effective roots before blaming IL2CPP or project code.
- Preserve the process result, resolved-path log excerpt, success marker, APK size, and hash as one validation record.
- Treat the three environment overrides as one configuration unit; do not override only the SDK while leaving an implicit NDK or JDK.
- Keep APK generation separate from device validation. A successful build does not prove installation, touch input, safe-area behavior, suspend/resume, or gameplay on hardware (`docs/ops/mobile-build-validation.md:72`).

## Related Issues

- [PR #19: automate mobile validation builds](https://github.com/zzanghyunmoo/random-tower-defense/pull/19)
- [PR #21: capture the original incomplete-toolchain learning](https://github.com/zzanghyunmoo/random-tower-defense/pull/21)
- [Mobile build and device validation](../../ops/mobile-build-validation.md)
