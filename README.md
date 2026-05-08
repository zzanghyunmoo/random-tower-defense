# 랜덤 타워 디펜스

Unity + C# 기반 2D 랜덤 타워 디펜스 게임입니다.

## 프로젝트 방향

- 대상 플랫폼: Android 우선, 이후 iOS, 장기적으로 Steam
- 게임 엔진: Unity
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
- Android 빌드를 먼저 검증하고, iOS와 Steam은 게임 루프가 안정된 뒤 다룹니다.
