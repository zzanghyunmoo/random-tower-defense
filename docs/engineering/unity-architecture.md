# Unity 아키텍처 초안

> 코딩 에이전트가 따라야 하는 상세 가드레일의 원본은 [`../../AGENTS.md`](../../AGENTS.md)입니다.

## 방향

이 프로젝트는 **Unity 6 LTS 계열 + C#**으로 개발하되, 1인 개발과 코딩 에이전트 협업을 고려해 **코드 중심 / 데이터 중심 Unity 구조**를 사용합니다.

Unity는 화면, 입력, 에셋, 빌드, 배포를 담당하고 핵심 게임 규칙은 가능한 한 Unity에 종속되지 않는 순수 C# 코드로 분리합니다.

## 핵심 원칙

- 핵심 게임 로직은 순수 C# 클래스에 둔다.
- `MonoBehaviour`는 Unity 생명주기, View 연결, 입력/애니메이션/사운드 호출만 담당한다.
- 전투 계산, 타워 공격, 몬스터 이동, 웨이브 진행, 랜덤 보상은 테스트 가능한 코드로 분리한다.
- Core 로직은 가능하면 `UnityEngine`에 의존하지 않는다.
- 밸런스 값은 코드에 하드코딩하지 않고 Definition 데이터로 분리한다.
- 씬/프리팹/인스펙터에는 연결 정보만 두고 핵심 규칙을 숨기지 않는다.
- MVP 전까지는 과한 범용화는 피하되, 테스트 가능한 경계는 유지한다.

## 권장 레이어

| 레이어                | 역할                                                         | Unity 의존성 |
| --------------------- | ------------------------------------------------------------ | ------------ |
| Core / Domain         | 전투 계산, 타워 규칙, 몬스터 상태, 웨이브 진행, 랜덤 보상    | 없음         |
| Application / Systems | 게임 루프 조율, 스폰, 저장/로드 흐름, Core 시스템 실행       | 최소화       |
| Unity Adapters        | `MonoBehaviour`, Scene, Prefab, Input, Animation, Audio 연결 | 있음         |
| Presentation          | UI, 이펙트, 사운드, 화면 표시                                | 있음         |
| Data                  | 타워/몬스터/웨이브/스테이지/강화 정의                        | 최소화       |

## 권장 폴더 구조

```text
Assets/
  _Project/
    Scripts/
      Core/
        Combat/
        Towers/
        Enemies/
        Waves/
        Economy/
        Random/
      Application/
        Systems/
        UseCases/
      UnityAdapters/
        MonoBehaviours/
        Installers/
        Views/
      Presentation/
        UI/
        Effects/
        Audio/
      Data/
        Definitions/
        Runtime/
      Editor/
      Tests/
    Data/
      Towers/
      Enemies/
      Waves/
      Stages/
      Upgrades/
    Prefabs/
    Scenes/
    Art/
    Audio/
    Settings/
```

## 초기 런타임 오브젝트

초기 MVP에서는 다음 오브젝트를 만들 수 있지만, 가능한 한 얇은 Adapter/View로 유지합니다.

- `GameController`: 전투 상태와 승리/패배 흐름을 Application System에 위임
- `WaveSpawner`: WaveDefinition을 읽고 스폰 시스템 호출
- `PathFollower`: Core 이동 결과를 Unity Transform에 반영
- `EnemyView`: EnemyState 표시, 피격/사망 연출 연결
- `TowerView`: 타워 표시, 타겟/공격 연출 연결
- `ProjectileView`: 발사체 표시와 충돌 Adapter 역할
- `TowerSpawnerView`: 입력을 받아 타워 소환 UseCase 호출
- `CombatHud`: 체력, 재화, 웨이브 UI 표시

## 데이터 오브젝트

- `EnemyDefinition`: 체력, 속도, 보상
- `TowerDefinition`: 공격력, 사거리, 공격 속도, 타워 등급
- `ProjectileDefinition`: 속도, 충돌 반경, 이펙트 참조 ID
- `WaveDefinition`: 적 종류, 수량, 스폰 간격
- `StageDefinition`: 웨이브 목록, 시작 체력, 시작 재화
- `UpgradeDefinition`: 강화 효과와 비용
- `RewardTableDefinition`: 랜덤 보상 후보와 가중치

## 기능 추가 순서

1. Core 모델/규칙 정의
2. 데이터 Definition 정의
3. 단위 테스트 작성
4. Application System 작성
5. Unity Adapter / View 연결
6. Prefab 또는 Scene 연결
7. 에디터 실행 확인
8. 모바일 빌드 영향 확인
