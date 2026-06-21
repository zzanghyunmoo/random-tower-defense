# AGENTS.md

이 파일은 코딩 에이전트가 `random-tower-defense` 저장소에서 작업할 때 반드시 따라야 하는 가드레일입니다.

## 범위

- 적용 범위: 이 저장소 전체
- 주요 스택: **Unity 6 LTS 계열 + C#**
- 개발 전제: **1인 개발 + 코딩 에이전트 협업**
- 우선 플랫폼: Android, 이후 iOS, 장기적으로 Steam
- 기본 방향: Unity를 쓰되 **코드 중심 / 데이터 중심 / Git 친화적 구조**로 개발합니다.

## 최우선 원칙

1. 게임 완성이 목표입니다. 엔진/프레임워크를 새로 만들지 않습니다.
2. 핵심 게임 규칙은 Unity에 숨기지 말고 순수 C# 코드로 드러냅니다.
3. `MonoBehaviour`는 얇게 유지하고, 화면/입력/에셋 연결 역할로 제한합니다.
4. 밸런스와 콘텐츠 데이터는 코드와 분리합니다.
5. 에이전트가 이해하기 어려운 씬/프리팹/인스펙터 변경은 최소화합니다.
6. Git diff만 봐도 변경 의도를 알 수 있게 작은 단위로 작업합니다.

---

# 1. 구조 원칙

## 1.1 레이어 분리

권장 레이어는 다음과 같습니다.

| 레이어                    | 역할                                                         | Unity 의존성 |
| ------------------------- | ------------------------------------------------------------ | ------------ |
| `Core` / `Domain`         | 전투 계산, 타워 규칙, 몬스터 상태, 웨이브 진행, 랜덤 보상    | 없음         |
| `Application` / `Systems` | 게임 루프 조율, 스폰, 저장/로드 흐름, Core 시스템 실행       | 최소화       |
| `UnityAdapters`           | `MonoBehaviour`, Scene, Prefab, Input, Animation, Audio 연결 | 있음         |
| `Presentation`            | UI, 이펙트, 사운드, 화면 표시                                | 있음         |
| `Data`                    | 타워/몬스터/웨이브/스테이지/강화 정의                        | 최소화       |

## 1.2 권장 폴더 구조

Unity 프로젝트가 생성되면 가능한 한 다음 구조를 사용합니다.

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

현재 프로젝트 상태와 맞지 않으면, 한 번에 대규모 이동하지 말고 작은 PR/커밋으로 점진적으로 맞춥니다.

## 1.3 `MonoBehaviour` 사용 규칙

### 허용

- Unity 생명주기 연결: `Awake`, `Start`, `Update`, `OnEnable`, `OnDisable`
- View, Prefab, Scene 오브젝트 연결
- 입력, 애니메이션, 사운드, 이펙트 호출
- Core/System 객체에 명령 전달
- Unity 이벤트를 순수 C# 시스템 호출로 변환하는 Adapter 역할

### 금지

- 전투 공식, 데미지 계산, 보상 확률, 웨이브 규칙을 `MonoBehaviour` 안에 직접 작성
- `Update()` 안에 복잡한 게임 규칙을 직접 구현
- 공유 상태를 인스펙터 필드나 씬 오브젝트 안에 숨기기
- 인스펙터 값만 바꾸면 핵심 규칙이 바뀌는 구조 만들기
- `FindObjectOfType`, `GameObject.Find`, 태그 검색에 의존하는 구조 만들기

## 1.4 Core 로직 규칙

- Core 로직은 가능하면 `UnityEngine` 네임스페이스를 참조하지 않습니다.
- 시간, 난수, 입력, 저장소는 직접 호출하지 말고 인터페이스로 주입합니다.
- 랜덤 결과는 seed 기반으로 재현 가능하게 만듭니다.
- 타워 공격, 몬스터 이동, 웨이브 진행, 보상 계산은 단위 테스트 가능한 형태로 작성합니다.
- 전역 싱글톤은 최소화합니다. 필요하면 Bootstrap/Installer에서 명시적으로 구성합니다.
- 새 기능은 가능하면 `Core -> Data -> Tests -> Application -> UnityAdapter/View` 순서로 만듭니다.

## 1.5 C# 스타일

- 타입, 메서드, 프로퍼티 이름은 영어를 사용합니다.
- 도메인 개념은 명확한 이름을 사용합니다. 예: `TowerDefinition`, `EnemyState`, `WaveSystem`.
- 과한 추상화보다 MVP 완성을 우선합니다.
- 다만 테스트 가능한 경계는 유지합니다.
- nullable, defensive check, 명확한 예외 메시지를 선호합니다.
- 주석은 “무엇”보다 “왜”를 설명할 때만 작성합니다.

---

# 2. 데이터 원칙

## 2.1 Definition / Runtime State / Save Data 분리

| 구분          | 설명                           | 예시                                 |
| ------------- | ------------------------------ | ------------------------------------ |
| Definition    | 변하지 않는 설계/밸런스 데이터 | `TowerDefinition`, `EnemyDefinition` |
| Runtime State | 플레이 중 변하는 상태          | `TowerInstance`, `EnemyState`        |
| Save Data     | 저장/로드 대상 데이터          | `PlayerProgress`, `StageSaveData`    |

규칙:

- Definition은 런타임에서 직접 수정하지 않습니다.
- Runtime State는 Definition을 참조하되 별도 객체로 관리합니다.
- Save Data에는 Unity 오브젝트 참조 대신 안정적인 ID를 저장합니다.

## 2.2 ID 규칙

- 모든 주요 데이터는 고유 ID를 가집니다.
- 표시 이름을 ID로 사용하지 않습니다.
- ID는 영어 소문자와 `_`를 사용합니다.

예시:

```text
tower_basic_arrow
tower_fire_mage
enemy_slime_small
wave_stage_01_03
upgrade_attack_speed_01
```

## 2.3 데이터 파일 규칙

초기에는 Unity 친화성을 위해 `ScriptableObject`를 사용할 수 있습니다. 단, 다음 조건을 지킵니다.

- 핵심 수치와 ID는 명확히 노출합니다.
- 다른 데이터 참조는 가능하면 ID 기반으로 합니다.
- 나중에 JSON/CSV로 이전할 수 있도록 구조를 단순하게 유지합니다.
- 밸런스 값은 코드에 하드코딩하지 않습니다.

권장 Definition:

- `TowerDefinition`
- `EnemyDefinition`
- `ProjectileDefinition`
- `WaveDefinition`
- `StageDefinition`
- `UpgradeDefinition`
- `RewardTableDefinition`
- `EconomyDefinition`

## 2.4 데이터 검증 규칙

데이터는 실행 전 검증 가능해야 합니다.

검증 항목:

- ID 중복 없음
- 필수 필드 누락 없음
- 음수가 될 수 없는 수치가 음수 아님
- 확률/가중치 합계가 유효함
- 참조하는 ID가 실제로 존재함
- 순환 참조가 없어야 하는 데이터에는 순환이 없음

권장 구현:

- `OnValidate()`로 에디터 단계 검증
- 별도 `DataValidator` 에디터 도구 작성
- 테스트에서 주요 데이터셋 로드 검증

## 2.5 랜덤/보상 데이터 규칙

- 랜덤 보상은 코드에 직접 분기하지 않고 RewardTable 데이터로 관리합니다.
- 확률은 고정 확률 또는 weight 기반으로 명확히 표현합니다.
- 테스트를 위해 seed를 주입할 수 있어야 합니다.
- 보상 결과는 로그나 디버그 화면으로 추적 가능해야 합니다.

---

# 3. Git / Unity 설정 원칙

## 3.1 Unity 필수 설정

Unity 프로젝트 생성 직후 다음 설정을 적용합니다.

| 설정                     | 값                                                           |
| ------------------------ | ------------------------------------------------------------ |
| Version Control Mode     | `Visible Meta Files`                                         |
| Asset Serialization Mode | `Force Text`                                                 |
| Unity Version            | 프로젝트 단위로 고정                                         |
| Package 버전             | `Packages/manifest.json`, `Packages/packages-lock.json` 커밋 |

## 3.2 반드시 커밋할 항목

- `Assets/`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/`
- 모든 `.meta` 파일
- 프로젝트 문서: `README.md`, `docs/`, `AGENTS.md`, `CLAUDE.md`

## 3.3 커밋하지 않을 항목

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`, `Builds/`
- `Logs/`
- `UserSettings/`
- `.vs/`, `.idea/`, `.vscode/`
- IDE/Unity가 생성한 임시 파일
- 빌드 산출물: `.apk`, `.aab`, `.ipa`, `.exe`, `.dmg`, `.zip`

## 3.4 `.meta` 파일 규칙

- `.meta` 파일은 반드시 Git에 포함합니다.
- 에셋을 이동/삭제/이름 변경할 때 `.meta`도 함께 이동/삭제합니다.
- 가능하면 Unity Editor 안에서 파일 이동/이름 변경을 합니다.
- `.meta` 파일을 임의로 삭제하지 않습니다.
- 프리팹/씬에서 참조 중인 스크립트나 에셋의 GUID가 바뀌지 않도록 주의합니다.

## 3.5 Git LFS 규칙

대용량/바이너리 에셋은 Git LFS 사용을 권장합니다.

권장 LFS 대상:

- `.png`, `.jpg`, `.jpeg`, `.psd`, `.aseprite`
- `.wav`, `.mp3`, `.ogg`
- `.fbx`, `.blend`
- `.ttf`, `.otf`
- 대용량 동영상/압축 파일

기본 텍스트 관리 대상:

- `.cs`, `.json`, `.csv`, `.md`
- `.unity`, `.prefab`, `.asset`, `.mat`, `.anim`, `.controller`
- `.meta`

단, 매우 큰 바이너리 `.asset`은 별도 판단합니다.

## 3.6 Unity YAML / 씬 / 프리팹 규칙

- `.unity`, `.prefab`, `.asset`은 텍스트라도 충돌이 자주 날 수 있습니다.
- UnityYAMLMerge 또는 Smart Merge 설정을 사용합니다.
- 같은 씬/프리팹을 여러 변경에서 동시에 수정하지 않습니다.
- 씬/프리팹 변경과 C# 로직 변경은 가능하면 커밋을 분리합니다.
- 에이전트는 Unity YAML을 대량 수동 수정하지 않습니다.
- 씬/프리팹 변경이 꼭 필요하면 변경 이유와 검증 방법을 남깁니다.

---

# 4. 작업 절차

## 4.1 작업 전 체크리스트

- [ ] 관련 Linear 이슈가 있으면 확인했는가?
- [ ] 이 변경이 Core 코드로 해결 가능한가?
- [ ] 씬/프리팹 수정이 꼭 필요한가?
- [ ] 데이터 파일 수정으로 충분한가?
- [ ] `.meta` 파일 변경이 예상되는가?
- [ ] Unity 버전이나 패키지 버전이 바뀌는가?
- [ ] 테스트 가능한 순수 C# 로직으로 분리되어 있는가?

## 4.2 기능 추가 순서

1. Core 모델/규칙 정의
2. 데이터 Definition 정의
3. 단위 테스트 작성
4. Application System 작성
5. Unity Adapter / View 연결
6. Prefab 또는 Scene 연결
7. 에디터 실행 확인
8. 모바일 빌드 영향 확인

## 4.3 문서화 기준

다음 변경은 `docs/` 또는 Notion/Linear 문서에 기록합니다.

- 엔진/Unity 버전 변경
- 패키지 추가/삭제
- 아키텍처 방향 변경
- 데이터 구조 변경
- 빌드/배포 절차 변경
- MVP 범위 변경

---

# 5. 금지 목록

- 핵심 게임 규칙을 `MonoBehaviour.Update()`에 직접 작성하지 않습니다.
- 밸런스 값을 코드에 하드코딩하지 않습니다.
- 표시 이름을 데이터 ID로 사용하지 않습니다.
- `.meta` 파일을 임의 삭제하지 않습니다.
- `Library/` 또는 빌드 산출물을 Git에 올리지 않습니다.
- 씬/프리팹 YAML을 대량 수동 수정하지 않습니다.
- Unity 버전 업그레이드와 기능 개발을 한 커밋에 섞지 않습니다.
- 인스펙터 숨김 상태에 핵심 규칙을 넣지 않습니다.
- 에이전트가 검증할 수 없는 대규모 에디터 변경을 한 번에 하지 않습니다.

---

# 6. 최종 판단 기준

좋은 변경은 다음 조건을 만족합니다.

- 에이전트가 코드를 읽고 안전하게 수정할 수 있습니다.
- 핵심 게임 규칙이 테스트 가능합니다.
- 밸런스 데이터가 코드와 분리되어 있습니다.
- 씬/프리팹 변경이 최소화됩니다.
- Git diff로 변경 이유를 이해할 수 있습니다.
- 혼자서도 디버깅, 수정, 출시까지 이어갈 수 있습니다.
