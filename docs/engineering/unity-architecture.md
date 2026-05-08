# Unity 아키텍처 초안

## 원칙

- MVP 전까지는 단순한 MonoBehaviour 중심으로 구현한다.
- 밸런스 값은 가능한 한 ScriptableObject로 분리한다.
- 씬 오브젝트 참조는 인스펙터에서 명확하게 연결한다.
- 싱글톤은 전투 단위 매니저처럼 생명주기가 분명한 곳에만 제한적으로 쓴다.
- 나중에 서버나 원격 설정이 들어와도 전투 로직을 최대한 데이터 기반으로 유지한다.

## 권장 폴더 구조

```text
Assets/
  _Project/
    Art/
    Audio/
    Data/
      Enemies/
      Towers/
      Waves/
    Prefabs/
      Enemies/
      Towers/
      Projectiles/
      UI/
    Scenes/
    Scripts/
      Combat/
      Data/
      Economy/
      Grid/
      UI/
```

## 초기 런타임 오브젝트

- GameController: 전투 상태와 승리/패배 흐름 관리
- WaveSpawner: 웨이브 데이터에 따라 적 생성
- PathFollower: 적의 경로 이동
- Enemy: 체력, 피해, 사망 처리
- Tower: 타겟 탐색과 공격 주기 관리
- Projectile: 이동과 충돌 피해 처리
- TowerSpawner: 랜덤 타워 소환과 배치
- CombatHud: 체력, 재화, 웨이브 UI 표시

## 데이터 오브젝트

- EnemyData: 체력, 속도, 보상
- TowerData: 공격력, 사거리, 공격 속도, 타워 등급
- WaveData: 적 종류, 수량, 스폰 간격
- StageData: 웨이브 목록, 시작 체력, 시작 재화
