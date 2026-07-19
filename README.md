# 랜덤 타워 디펜스

Unity + C# 기반 2D 랜덤 타워 디펜스 게임입니다.

## 프로젝트 방향

- 대상 플랫폼: Android와 iOS, 장기적으로 Steam
- 게임 엔진: Unity 6000.3.20f1
- 프로그래밍 언어: C#
- 형상 관리: GitHub
- 위키: Notion
- 이슈 관리: Linear

## MVP

첫 번째 목표는 작은 전투 루프를 실제로 플레이 가능한 상태로 만드는 것입니다.

- 웨이브 기반 적 스폰
- 정해진 경로를 따라 이동하는 적
- 랜덤 타워 소환
- 타워 자동 공격
- 적 처치 보상
- 플레이어 체력과 실패 조건
- 기본 전투 UI

## 링크

- Linear 프로젝트: https://linear.app/zzanghyunmoo/project/랜덤-타워-디펜스-82ce3c8d2adb
- Notion 위키: https://www.notion.so/35aef22ad4fc811dbc41c2155500cf52
- GitHub 저장소: https://github.com/zzanghyunmoo/random-tower-defense

## 작업 규칙

- 기능 작업은 Linear 이슈에서 시작합니다.
- 설계와 결정 사항은 Notion과 `docs/`에 남깁니다.
- 구현은 작은 단위로 나누고, MVP 전까지는 과한 범용화를 피합니다.
- 각 구현 PR은 테스트와 리뷰를 통과한 뒤 squash merge하고 다음 작업을 시작합니다.

## 개발 환경

- Unity Hub에서 Unity `6000.3.20f1`을 설치합니다.
- Android Build Support와 하위 SDK/NDK/OpenJDK 모듈을 설치합니다.
- iOS Build Support를 설치합니다. Windows에서는 Xcode 프로젝트 생성까지만 검증합니다.
- 프로젝트의 최소 OS는 Android 7.1(API 25), iOS/iPadOS 15입니다.
- 순수 C# Core 테스트에는 .NET SDK 10이 필요합니다.

```powershell
dotnet restore Tests/RandomTowerDefense.Core.Tests/RandomTowerDefense.Core.Tests.csproj --locked-mode
dotnet test Tests/RandomTowerDefense.Core.Tests/RandomTowerDefense.Core.Tests.csproj --configuration Release --no-restore
./scripts/Test-ProjectLayout.ps1
./scripts/Invoke-Unity.ps1 -Task EditMode
./scripts/Invoke-Unity.ps1 -Task PlayMode
./scripts/Invoke-Unity.ps1 -Task ValidateData
./scripts/Invoke-Unity.ps1 -Task BuildAndroid
./scripts/Invoke-Unity.ps1 -Task ExportIos
```

`-TestPlatform EditMode`와 `-TestPlatform PlayMode`도 이전 명령 호환을 위해 계속 지원합니다. 생성된 APK와 Xcode 프로젝트는 `Builds/Validation/`에 저장되며 Git에는 포함하지 않습니다.

플랫폼 도구 설정, 산출물 확인, Android 설치, iOS 서명 단계는 [모바일 빌드와 기기 검증](docs/ops/mobile-build-validation.md)을 따릅니다. 실제 플레이 결과는 [첫 플레이테스트 체크리스트](docs/playtesting/first-playtest.md)에 기록합니다.
