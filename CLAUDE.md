# CLAUDE.md

이 저장소의 코딩 에이전트 가드레일은 **`AGENTS.md`가 원본**입니다.

Claude/Codex/기타 코딩 에이전트는 작업 전에 반드시 다음 파일을 먼저 읽고 따라야 합니다.

- [`AGENTS.md`](./AGENTS.md)

## 핵심 요약

- 스택: **Unity 6 LTS 계열 + C#**
- 개발 전제: **1인 개발 + 코딩 에이전트 협업**
- 방향: **코드 중심 / 데이터 중심 / Git 친화적 Unity 프로젝트**
- 핵심 게임 규칙은 순수 C# `Core` 코드로 작성합니다.
- `MonoBehaviour`는 얇은 Adapter/View 역할로 제한합니다.
- 밸런스 데이터는 코드와 분리합니다.
- Unity 설정은 `Visible Meta Files`, `Force Text`를 사용합니다.
- `.meta` 파일은 반드시 Git에 포함하고 임의 삭제하지 않습니다.
- `Library/`, `Temp/`, `Build/`, `Builds/`, `Logs/`는 커밋하지 않습니다.
- 씬/프리팹/YAML 대량 수동 수정은 피합니다.

상세 규칙은 `AGENTS.md`를 따릅니다.
