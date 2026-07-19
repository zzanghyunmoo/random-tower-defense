---
title: Unity Android Build Support can have an incomplete toolchain
date: 2026-07-20
category: build-errors
module: Unity mobile build automation
problem_type: build_error
component: tooling
symptoms:
  - "Android Build Support appears installed but the embedded SDK is missing command-line tools"
  - "The Android batch build fails before producing an APK"
root_cause: incomplete_setup
resolution_type: environment_setup
severity: medium
tags: [unity, android, sdk, ndk, il2cpp, build-automation]
---

# Unity Android Build Support can have an incomplete toolchain

## Problem

Unity Hub can show Android Build Support as installed while required SDK or NDK contents are absent or only partially extracted. In that state Unity recognizes the Android target, but a batch build fails before it can produce an APK.

## Symptoms

- The Android playback engine exists and the target-support check passes, but the build stops during Android tool discovery.
- The embedded SDK has only some of `build-tools`, `platform-tools`, `platforms`, or `cmdline-tools`.
- The SDK's `sdkmanager` or `adb`, the NDK metadata, or the JDK executable is missing.
- Re-running the same build does not repair the installation.

## What Didn't Work

- Treating Unity Hub's selected-module state as proof that every Android submodule was installed.
- Retrying `BuildAndroid` without first checking the underlying executable and metadata files.
- Starting a Hub module repair in an environment where elevation or long downloads could not complete; the partially installed directory remained unchanged.

## Solution

First verify the toolchain itself, not only the top-level Android module. The exact command-line-tools version can vary, so discover its directory instead of hard-coding one version.

```powershell
$sdkRoot = '<Android SDK root>'
$ndkRoot = '<Android NDK root>'
$jdkRoot = '<JDK 17 root>'

Test-Path (Join-Path $sdkRoot 'platform-tools\adb.exe')
Get-ChildItem (Join-Path $sdkRoot 'cmdline-tools') -Filter sdkmanager.bat -Recurse
Test-Path (Join-Path $ndkRoot 'source.properties')
Test-Path (Join-Path $jdkRoot 'bin\java.exe')
```

Repair the Hub installation or prepare a complete, version-compatible Android toolchain outside the repository. Then pass all three roots to the build process together:

```powershell
$env:UNITY_ANDROID_SDK_ROOT = $sdkRoot
$env:UNITY_ANDROID_NDK_ROOT = $ndkRoot
$env:UNITY_ANDROID_JDK_ROOT = $jdkRoot

./scripts/Invoke-Unity.ps1 -Task BuildAndroid
```

The build entry point requires IL2CPP and ARM64 before invoking Unity's player build (`Assets/_Project/Scripts/Editor/BuildAutomation/MobileBuildPipeline.cs:35`). When an SDK override is present, it also requires NDK and JDK roots, applies the three validated directories, and restores the developer's previous Unity External Tools settings in `finally` (`MobileBuildPipeline.cs:50`, `MobileBuildPipeline.cs:134`, `MobileBuildPipeline.cs:197`).

The PowerShell runner accepts `BuildAndroid`, requires Unity's success marker, and verifies that the expected APK exists (`scripts/Invoke-Unity.ps1:5`, `scripts/Invoke-Unity.ps1:118`). This workflow was verified by the Android APK produced in [PR #19](https://github.com/zzanghyunmoo/random-tower-defense/pull/19).

## Why This Works

Unity's target-support state confirms that the playback engine is available; it does not prove every SDK, NDK, and JDK payload needed later in the build is usable. Checking the files catches partial extraction early. Passing complete roots through explicit environment variables makes the batch build independent of a broken embedded SDK, while restoring the prior preferences prevents a one-off validation build from silently changing the developer's persistent Unity setup.

## Prevention

- Verify `adb`, `sdkmanager`, NDK metadata, and Java before an expensive IL2CPP build when a machine is newly provisioned.
- Treat all three Android tool roots as one configuration unit; do not mix an overridden SDK with implicit NDK or JDK paths.
- Keep local toolchains and generated APKs outside Git-tracked paths.
- Preserve a deterministic build command and check both its success marker and artifact, rather than trusting process exit alone.
- Follow the current commands and external-device boundary in the [mobile build and device validation runbook](../../ops/mobile-build-validation.md).

## Related Issues

- [PR #19: automate mobile validation builds](https://github.com/zzanghyunmoo/random-tower-defense/pull/19)
- [Mobile build and device validation](../../ops/mobile-build-validation.md)
