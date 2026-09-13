# HT-Avoid

Unity로 만드는 비주얼 노벨(VN) 게임 프로젝트. VN 엔진을 밑바닥부터 직접 구현 중.
프로젝트명 "HT-Avoid" = **Helltaker(HT) + Avoid(피하기)**.

> **세션 시작 시 필독**: `Assets/Docs/5. Record/` 폴더에서 **가장 최근 날짜의
> 기록 파일**을 먼저 읽어라. 그 세션에서 무엇을 했고 어디까지 정해졌는지가
> 요약되어 있다.

## 게임 컨셉 (확정: 2026-09-12)
헬테이커 모티브로, **VN → 피하기 게임 → RPG(덱빌딩) → VN**을 한 사이클로
반복하다 보스전까지 도달하는 구조.
- 피하기 : 밈피하기(국내 플래시식 탄막 회피) 기초 + 메이플스토리처럼
  플레이어도 공격 가능한 보스전 액션
- RPG(덱빌딩) : 세부 규칙 아직 미정
상세는 `Assets/Docs/1. Plan/[26.09.12_17.00] 핵심 게임 루프 구조 (헬테이커 모티브).txt` 참고.

## 구조
- `Assets/Scripts/VisualNovel/Core/` — VN 엔진 본체
  - `Dialogue/` — `DialogueSystem`(진입점), `DialogueParser`(대본 한 줄 파싱), `Conversation`/`ConversationManager`(코루틴으로 대사 재생)
  - `Commands/` — `CommandManager`(리플렉션으로 `CMD_DatabaseExtension` 상속 클래스들을 자동 스캔해 명령어 등록/실행)
  - `Logical Lines/` — 조건문/선택지 등 특수 로직 줄 처리 (`ILogicalLine` 구현체, 리플렉션으로 자동 수집)
  - `Characters/` — 캐릭터 생성/관리 (Text/Sprite/Live2D/Model3D 타입별)
  - `TextArchitect/` — 타이핑 효과 등 텍스트 출력 빌더
  - `Histroy/`, `Audio/`, `Graphic Panels/`, `IO/` — 히스토리 로그, 오디오, 배경/그래픽 패널, 세이브 파일 관리
- `Assets/Scripts/Danganronpa/` — 단간론파풍 마우스/상호작용/카메라 스크립트 (VN 엔진과 별개)
- 메인 씬: `Assets/Scenes/VisualNovel.unity`

## 대본 문법
```
화자 "대사내용" [명령어(인자)]
```
`DialogueParser.Parse()`가 화자/대사/명령어로 3등분. 명령어는 `[character.expression(happy)]`처럼 점(`.`)으로 캐릭터별 서브 명령을 구분.

## 새 명령어 추가하는 법
`CMD_DatabaseExtension`을 상속한 클래스에 메서드 추가 → 리플렉션이 자동으로 찾아서 등록함 (수동 등록 불필요).

## 커밋 관례
회차별로 직접 커밋해온 이력 (`22-1`, `22-2`, `23-3`, `EP21`, `Episode26` 등 = 강의/작업 회차 번호). 브랜치 안 쓰고 `main`에 바로 커밋하는 방식.

## Assets/Docs 문서 관리 규칙 (확정: 2026-09-13)
- `1. Plan/` : 주제별로 **문서 하나만 유지**하고 새 결정이 나올 때마다 그 파일을 직접 갱신한다 (새 날짜로 파일을 또 만들지 않음). 지금은 `게임 기획 및 개발 로드맵.txt` 하나로 통합되어 있음.
  - 내용을 고칠 때마다 파일명 앞의 `[YY.MM.DD_HH.mm]` 부분만 그 시점(KST)으로 갱신하고, 뒤의 제목("게임 기획 및 개발 로드맵")은 그대로 유지한다 (파일 삭제 후 새 타임스탬프로 재생성).
- `5. Record/` : 반대로 세션마다 **새 파일**을 만드는 게 맞음 (그날 무엇을 했는지의 역사적 기록이라 덮어쓰면 안 됨).
- `2. ChangePlan/` → `3. ChangeCompleted/` : 기능 단위 생애주기(착수 전 계획 → 완료 후 보관)라서 원래부터 항목별로 하나씩.

## 관련 프로젝트: AH-Dispatch
`C:\Users\이찬열\Documents\Gifted\AH-Dispatch`는 **별개의 프로젝트**. 원래는 이것과 같은 계열의 VN이었으나, 2026-04-14에 외부 레포 [GameDispatchLike](https://github.com/ViniciusChrisosthemos/GameDispatchLike.git)를 통합하며 파견/경영 시뮬레이션 게임으로 전환됨. 대화 엔진(DialogueSystem/CommandManager 등)은 git에 커밋된 적 없고 현재 디스크에도 없음 — `AudioManager`만 VN 시절 유산으로 공유해서 재사용 중. 그 프로젝트는 git 커밋보다 `Assets/Docs/`(계획/버그분석/변경완료 텍스트 문서)에 실제 작업 로그가 훨씬 상세히 남아있음.
