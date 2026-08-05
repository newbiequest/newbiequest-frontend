# NewbieQuest Frontend (뉴비퀘스트)

Unity 기반의 3D 사무실 적응 퀘스트 게임 프론트엔드입니다. 사용자는 사무실 맵을 탐색하며 NPC에게 업무 미션을 받고, 미니게임을 수행해 점수와 랭킹을 경쟁할 수 있습니다.

백엔드 API 및 WebSocket 서버와 연동하여 로그인, 미션, 채팅, 랭킹 기능을 제공합니다.

---

## 한 줄 요약

Unity 3D 환경에서 사무실 업무 미션을 수행하는 게임형 서비스 — 로그인 → NPC 미션 수령 → 미니게임 수행 → 점수 및 랭킹 확인 워크플로우 제공.

---

## 주요 기능

- 회원가입 및 로그인
- 3D 사무실 맵 탐색 및 플레이어 이동
- NPC 상호작용 및 미션 수령
- 업무형 미니게임
  - 커피 제조
  - 문서 출력
  - 문서 정리
  - 택배 전달
  - 컴퓨터 업무
  - 소·대회의실 미션
- 실시간 채팅
- 미션 완료에 따른 점수 저장
- 랭킹 조회
- 미니맵 및 게임 HUD

---

## 사용된 기술

- **언어** : C#
- **게임 엔진** : Unity `6000.3.12f1`
- **렌더링** : Universal Render Pipeline (URP)
- **UI** : Unity UI, TextMeshPro
- **입력 처리** : Unity Input System
- **서버 통신** : UnityWebRequest, NativeWebSocket
- **버전 관리** : Git / GitHub, Git LFS

---

## 저장소 구조 (요약)

```text
.
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Player.cs                 # 플레이어 이동
│  │  ├─ LoginUI.cs                # 로그인 및 회원가입
│  │  ├─ MissionManager.cs         # 미션 진행 및 점수 관리
│  │  ├─ MissionUI.cs              # 미션 UI
│  │  ├─ ChatUI.cs                 # 실시간 채팅
│  │  ├─ RankingUI.cs              # 랭킹 UI
│  │  ├─ MapUI.cs                  # 미니맵 UI
│  │  ├─ coffeeMission.cs          # 커피 미션
│  │  ├─ PrintMission.cs           # 문서 출력 미션
│  │  ├─ DocStorageMission.cs      # 문서 정리 미션
│  │  ├─ ParcelMission.cs          # 택배 미션
│  │  └─ ComputerMission.cs        # 컴퓨터 미션
│  ├─ Scenes/                      # Unity 씬
│  └─ ...                          # 3D 모델, UI, 사운드 등 에셋
├─ Packages/
│  ├─ manifest.json                # Unity 패키지 목록
│  └─ packages-lock.json
└─ ProjectSettings/                # Unity 프로젝트 설정
```

---

## 핵심 구현 포인트

- 플레이어 이동 및 3D 사무실 맵 탐색
  - `Assets/Scripts/Player.cs`
- 로그인 및 회원가입
  - `Assets/Scripts/LoginUI.cs`
- 미션 수령, 진행, 완료 및 점수 저장
  - `Assets/Scripts/MissionManager.cs`
- 실시간 채팅
  - `Assets/Scripts/ChatUI.cs`
- 랭킹 조회
  - `Assets/Scripts/RankingUI.cs`
- 업무형 미니게임
  - `Assets/Scripts/coffeeMission.cs`
  - `Assets/Scripts/PrintMission.cs`
  - `Assets/Scripts/DocStorageMission.cs`
  - `Assets/Scripts/ParcelMission.cs`

---

## 실행 방법

Unity Hub 및 Unity `6000.3.12f1` 설치가 필요합니다.

저장소 클론

```bash
git clone https://github.com/newbiequest/newbiequest-frontend.git
cd newbiequest-frontend
```

Unity Hub에서 프로젝트 열기

```text
Unity Hub → Add → newbiequest-frontend 폴더 선택
```

프로젝트 실행

```text
Unity Editor에서 시작 씬 열기 → Play 버튼 클릭
```

프로젝트를 처음 열면 패키지 설치와 에셋 임포트가 진행될 수 있습니다.

---

## 백엔드 연결

이 프로젝트는 백엔드 서버와 연동됩니다.

- 로그인 및 회원가입
- 미션 생성 및 완료 처리
- 점수 저장
- 랭킹 조회
- 실시간 채팅

서버 주소를 변경해야 할 경우 아래 파일의 `baseUrl` 또는 WebSocket 주소를 수정합니다.

```text
Assets/Scripts/LoginUI.cs
Assets/Scripts/MissionManager.cs
Assets/Scripts/ChatUI.cs
```

---

## 주의

- Unity `6000.3.12f1` 버전 사용을 권장합니다.
- `Library`, `Temp`, `Logs`, `.vs` 폴더는 Unity가 자동 생성하므로 GitHub에 포함하지 않습니다.
- 100MB를 초과하는 `Interiors_A_blend.zip` 파일은 Git LFS로 관리합니다.
- 백엔드 서버가 실행 중이어야 로그인, 채팅, 미션, 랭킹 기능을 정상적으로 사용할 수 있습니다.

---

## 개발 메모

- UnityWebRequest를 사용하여 REST API와 통신합니다.
- NativeWebSocket을 사용하여 실시간 채팅을 구현했습니다.
- 미션 완료 여부와 점수는 `MissionManager`에서 관리합니다.
- NPC 상호작용을 통해 상황별 업무 미션을 제공합니다.
- 사무실 환경과 업무 요소를 게임화하여 직관적인 사용자 경험을 구성했습니다.
