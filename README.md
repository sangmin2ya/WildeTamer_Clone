# Wild Tamer Clone Coding

Wild Tamer를 레퍼런스로 한 2D 쿼터뷰 액션 게임 클론 코딩 프로젝트입니다.

## 개요

| 항목 | 내용 |
|------|------|
| 레퍼런스 | Wild Tamer |
| 엔진 | Unity 6000.3.10f1 (2D) |
| 시점 | 쿼터뷰 (Isometric) |
| 입력 | PC WASD (New Input System) |
| 개발 기간 | 4일 |

## 핵심 루프

탐험 → 전투 → 테이밍 → 부대 확장

## 주요 기능

- **플레이어 이동**: WASD 기반 쿼터뷰 이동, 자동 회전
- **아군 군집 이동**: 플레이어를 따라다니는 부대 시스템 (Steering Behavior 기반)
- **전투 시스템**: 사정거리 내 자동 공격, 유닛별 독립 쿨타임
- **테이밍 시스템**: 기절한 적을 아군으로 편입하는 핵심 메카닉
- **적 군집 AI**: 공전 이동, 추격, 공격 상태머신
- **일반 몬스터 3종**: 근접형 / 원거리형 / 순찰형
- **보스 2종**: 장판 패턴 / 돌진 패턴
- **맵 시스템**: 타일맵 + Fog of War + 미니맵

## 기술 특이사항

- NavMesh 미사용 — 모든 이동은 순수 코드로 구현 (`Rigidbody2D Kinematic` + `MovePosition`)
- 오브젝트 풀링 — 모든 동적 오브젝트에 적용
- 공간 분할 — `SpatialHashGrid`로 유닛 탐색 최적화
- ScriptableObject — 몬스터/유닛 스탯 데이터 관리

## 폴더 구조

```
Assets/
├── 01.Scenes/
├── 02.Scripts/
│   ├── _1.Core/
│   ├── _2.Managers/
│   ├── _3.UI/
│   ├── _4.Systems/
│   ├── _5.Utils/
│   └── _6.ScriptableObjects/
├── 03.Prefabs/
├── 04.Sprites/
├── 05.Animations/
├── 06.Audios/
├── 07.ThirdParty/
├── 08.UI/
├── 09.Data/
├── 10.Materials/
├── Resources/
└── Settings/
```
