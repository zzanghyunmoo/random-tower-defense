# 모바일 빌드와 기기 검증

이 문서는 같은 커밋에서 자동 회귀, Android APK, iOS Xcode 프로젝트, 실기기 결과를 구분해 검증하는 실행서다. 생성된 `Builds/`, `TestResults/`, APK, Xcode 프로젝트는 검증 산출물이며 커밋하지 않는다.

## 검증 경계

| 항목 | 이 저장소에서 자동 확인 | 외부 환경에서 확인 |
| --- | --- | --- |
| Core 규칙 | .NET 테스트 | 없음 |
| Unity 연결 | EditMode, PlayMode, 데이터 검증 | 에디터 육안 확인은 필요할 때 수행 |
| Android | 개발용 APK 생성, IL2CPP, ARM64, 패키지 설정 | 실제 기기 설치, 터치, safe area, 앱 복귀, 전체 한 판 |
| iOS | Windows에서도 Xcode 프로젝트 생성 | macOS/Xcode 서명, archive, 실제 기기 실행 |

2026-07-20 기준 자동 검증과 양 플랫폼 산출물 생성은 완료했다. 현재 자동화 환경에는 Android 기기가 연결되어 있지 않았으므로 Android 실기기 항목은 미검증이며, iOS 서명과 기기 실행도 macOS 환경에서 확인해야 한다.

## 준비

- Unity Hub에서 Unity `6000.3.20f1`을 설치한다.
- Android Build Support와 하위 `Android SDK & NDK Tools`, `OpenJDK`를 설치한다.
- iOS Xcode 프로젝트를 만들 컴퓨터에는 iOS Build Support를 설치한다.
- iOS archive와 기기 설치에는 macOS, Xcode, Apple Developer Team과 서명 자산이 필요하다.
- Unity Editor나 다른 batchmode 프로세스가 이 프로젝트를 열고 있지 않은지 확인한다. Unity 테스트와 빌드는 같은 프로젝트에서 순서대로 실행한다.

## 전체 자동 검증

저장소 루트의 PowerShell에서 다음 순서로 실행한다.

```powershell
dotnet restore Tests/RandomTowerDefense.Core.Tests/RandomTowerDefense.Core.Tests.csproj --locked-mode
dotnet test Tests/RandomTowerDefense.Core.Tests/RandomTowerDefense.Core.Tests.csproj --configuration Release --no-restore
./scripts/Test-ProjectLayout.ps1
./scripts/Invoke-Unity.ps1 -Task EditMode
./scripts/Invoke-Unity.ps1 -Task PlayMode
./scripts/Invoke-Unity.ps1 -Task ValidateData
```

Unity 테스트는 `TestResults/<Task>/`에 XML과 로그를 남긴다. 모든 명령이 종료 코드 0으로 끝나고, Unity 로그에 컴파일 오류나 예상하지 못한 예외가 없어야 한다.

## Android APK

기본 Hub 도구를 사용할 때는 다음 명령만 실행한다.

```powershell
./scripts/Invoke-Unity.ps1 -Task BuildAndroid
```

결과는 `Builds/Validation/Android/RandomTowerDefense.apk`다. 자동화는 다음 조건이 아니면 실패한다.

- Android Build Support가 설치되어 있다.
- Scripting Backend가 IL2CPP다.
- Target Architecture가 ARM64 하나다.
- 활성 Build Scene이 하나 이상이다.
- 데이터 검증을 통과한다.
- APK가 실제로 생성되고 비어 있지 않다.

Hub의 SDK 하위 모듈이 불완전하거나 별도 Android 도구를 사용해야 하면 세 경로를 함께 지정한다. 경로는 예시이며 로컬 설치 위치로 바꾼다.

```powershell
$env:UNITY_ANDROID_SDK_ROOT = 'D:\Android\Sdk'
$env:UNITY_ANDROID_NDK_ROOT = 'D:\Android\Ndk\27.2.12479018'
$env:UNITY_ANDROID_JDK_ROOT = 'D:\Java\jdk-17'

./scripts/Invoke-Unity.ps1 -Task BuildAndroid

Remove-Item Env:UNITY_ANDROID_SDK_ROOT
Remove-Item Env:UNITY_ANDROID_NDK_ROOT
Remove-Item Env:UNITY_ANDROID_JDK_ROOT
```

세 경로 중 하나라도 없으면 빌드를 중단한다. 자동화는 환경변수 경로를 사용한 뒤 기존 Unity External Tools 설정을 복원한다. SDK 문제를 조사할 때는 `platform-tools/adb`, `build-tools`, `platforms`, `cmdline-tools`, NDK 디렉터리가 실제로 채워졌는지 확인한다.

### Android 기기 설치와 실행

USB 디버깅을 켠 ARM64 Android 기기를 연결한 뒤 SDK의 `adb`를 사용한다.

```powershell
$androidSdkRoot = '<Android SDK 루트 경로>'
$adb = Join-Path $androidSdkRoot 'platform-tools\adb.exe'
& $adb devices -l
& $adb install -r 'Builds\Validation\Android\RandomTowerDefense.apk'
& $adb shell monkey -p com.zzanghyunmoo.randomtowerdefense -c android.intent.category.LAUNCHER 1
```

`devices -l`에 상태가 `device`인 대상이 정확히 나타나야 한다. 설치 후 [첫 플레이테스트 체크리스트](../playtesting/first-playtest.md)를 수행하고 커밋, APK 해시, 기기와 OS 버전을 함께 기록한다. 크래시가 있으면 재현 직후 `adb logcat -d`를 보관한다.

## iOS Xcode 프로젝트

Windows 또는 macOS에서 다음 명령으로 export한다.

```powershell
./scripts/Invoke-Unity.ps1 -Task ExportIos
```

결과는 `Builds/Validation/iOS/`다. `Unity-iPhone.xcodeproj/project.pbxproj`, `Info.plist`, `Classes/UnityAppController.mm`, `Il2CppOutputProject/`가 있어야 한다.

macOS에서는 export한 프로젝트를 Xcode로 열고 다음을 확인한다.

- Bundle Identifier가 `com.zzanghyunmoo.randomtowerdefense`다.
- Deployment Target이 iOS/iPadOS 15.0 이상이다.
- 올바른 Apple Developer Team과 자동 또는 수동 서명을 선택했다.
- 실제 기기 또는 Generic iOS Device 대상으로 Build가 성공한다.
- Product > Archive가 성공하고 Organizer에서 archive를 검증할 수 있다.
- 실제 iPhone/iPad에서 설치, 가로 방향, 터치, safe area, 중단/복귀, 승패, 재시작을 확인한다.

Unity는 Windows에서도 Xcode 프로젝트를 생성할 수 있지만, Xcode로 컴파일하고 서명하는 단계는 macOS에서 수행한다. 공식 절차는 [Unity iOS 빌드 과정](https://docs.unity3d.com/cn/2023.2/Manual/iphone-BuildProcess.html)과 [Android 환경 설정](https://docs.unity3d.com/kr/6000.0/Manual/android-sdksetup.html)을 참고한다.

## 결과 기록

PR 또는 플레이테스트 노트에 다음을 남긴다.

- 검증한 Git 커밋 SHA
- Unity 버전
- 각 명령의 성공/실패와 테스트 개수
- Android APK 크기와 SHA-256
- iOS export의 Xcode 프로젝트 존재 여부
- 실기기 모델, OS 버전, 화면 특성, 설치 결과
- 미검증 항목과 필요한 외부 환경

실기기나 서명 환경이 없으면 실패로 꾸미거나 완료로 표시하지 않는다. 자동 산출물 성공과 외부 검증 대기를 명확히 나눈다.
