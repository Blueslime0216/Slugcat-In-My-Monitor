# 음식 업데이트 상세 보고서

작성일: 2026-08-25  
대상 브랜치: `feature/food-update`  
대상 fork: `Blueslime0216/Slugcat-In-My-Monitor`

## 1. 업데이트 결과

이번 업데이트는 데스크톱 Slugcat에게 실제로 먹이를 주고, Slugcat이 먹이 쪽으로 이동해 집어 들고 세 번 베어 먹는 첫 번째 음식 시스템을 추가한다. 첫 지원 아이템은 Rain World의 파란 열매인 `DangleFruit`다.

사용 절차는 다음과 같다.

1. 시스템 트레이의 Slugcat 아이콘을 우클릭한다.
2. `먹이 주기 · 슬러그캣 N` 메뉴를 연다.
3. `파란 열매 주기`를 선택한다.
4. 현재 선택된 Slugcat 앞의 안전한 바닥에 열매가 놓인다.
5. Slugcat이 열매를 주시하고 접근한 뒤 집어 들며, 원작과 같은 3단계 bite frame으로 먹는다.

전역 단축키, 화면 위 고정 버튼, 다음 마우스 클릭으로 위치를 지정하는 모드는 추가하지 않았다. 따라서 게임, 작업 프로그램, 브라우저의 단축키와 충돌하지 않고 화면을 가리지 않는다. 먹이 자체도 마우스 히트테스트 대상이 아니므로 평상시 바탕화면 클릭을 통과시킨다.

## 2. 구현 범위

포함된 기능:

- `DangleFruit` 물리 객체
- 자유, 예약, 들기, 먹는 중, 소비, 만료 상태
- 원작의 3 bites와 1 food point 계약
- 원작에 대응하는 반지름 8, 질량 0.2, 중력 0.9, air friction 0.999, surface friction 0.7, bounce 0.2
- 로컬 Rain World 설치본의 `DangleFruit0A/B`, `1A/B`, `2A/B` atlas frame 사용
- 선택된 Slugcat 전용 먹이 예약
- `VirtualInput`을 통한 자율 접근
- 머리 위치를 기준으로 한 들기와 3단계 섭취
- 움직이는 창 표면의 이동량 적용
- 자유 상태는 Slugcat 뒤, 들거나 먹는 상태는 Slugcat 앞에 그리는 레이어 순서
- 기존 DirectComposition 배치 bounds에 음식 범위 병합
- Slugcat 한 마리당 최대 3개, 전체 최대 12개 제한
- 약 30초 동안 먹지 않은 자유 음식 자동 만료
- 선택된 Slugcat의 음식 치우기 메뉴

의도적으로 제외한 기능:

- 배고픔이나 강제적인 food meter
- 동면, cycle, karma와 연결된 생존 규칙
- 사운드 재생
- 사용자가 먹이를 직접 드래그하는 기능
- 서로 다른 Slugcat이 하나의 먹이를 두고 경쟁하는 기능
- 다른 높이의 창으로 이동하는 장거리 먹이 pathfinding
- Fly, Mushroom, WaterNut, JellyFish, KarmaFlower 등 복합 아이템

## 3. Rain World 원본 조사 결과

로컬 설치본은 `v1.11.8`이었으며, 프로젝트가 이미 사용하는 읽기 전용 Unity asset 추출 경로와 동일한 방식으로 atlas를 확인했다. 저장소나 빌드 산출물에 Rain World 이미지 파일을 복사하지 않는다.

원작 `IPlayerEdible`의 핵심 계약은 다음과 같다.

- `BitesLeft`
- `BitByPlayer(Creature.Grasp, bool)`
- `FoodPoints`
- `Edible`
- `AutomaticPickUp`
- `ThrowByPlayer()`

원작 Player의 섭취 흐름은 개념적으로 `GrabUpdate → BiteEdibleObject → ObjectEaten → AddFood/AddQuarterFood`다. `DangleFruit`는 `PlayerCarryableItem` 기반의 한 개 BodyChunk 아이템이고, 초기 bites는 3, food points는 1, automatic pickup은 true다. 마지막 bite에서 `ObjectEaten`을 호출하고 grasp를 해제한 뒤 아이템이 소멸한다.

이 데스크톱 프로젝트에는 Rain World의 `Room`, `AbstractPhysicalObject`, creature graph, cycle 시스템이 없다. 원작 전체 객체 계층을 이식하면 작은 음식 기능 때문에 결합도와 메모리 비용이 과도하게 커진다. 그래서 `IPlayerEdible`의 사용자에게 보이는 계약만 `DesktopFood`로 옮기고, 기존 `BodyChunk`와 `DesktopCollisionWorld`를 재사용했다.

기존 `IGourmandEdible`과 `GourmandCraftingFramework`는 조합법을 위한 골격이며 런타임 물리 아이템 계약이 아니다. 이번 구현은 이를 억지로 확장하지 않고 별도의 데스크톱 음식 계층을 만들었다. 나중에 Gourmand crafting을 구현할 때 `DesktopFoodKind`와 recipe adapter를 연결하는 편이 안전하다.

## 4. 구조와 데이터 흐름

### `DesktopFood`

파일: `src/RainWorldDesktopPet/Physics/DesktopFood.cs`

한 개 음식의 물리와 edible 상태를 소유한다. `BodyChunk` 한 개를 사용하며, atlas element 이름은 정적 배열에서 조회한다. 렌더 프레임마다 문자열을 조합하지 않으므로 불필요한 GC 할당이 없다.

상태 흐름:

`Free → Claimed → Held → Biting → Consumed`

예외 흐름:

- 잡힌 Slugcat이 기절하거나 사용자가 직접 들어 올리면 `Held/Biting → Free`
- 자유 상태로 1200 simulation ticks, 약 30초가 지나면 `Free/Claimed → Expired`

### `DesktopFoodManager`

파일: `src/RainWorldDesktopPet/Core/DesktopFoodManager.cs`

각 `GameLoop`가 관리자 한 개를 소유한다. 이 소유 관계가 예약 역할을 하므로 여러 Slugcat이 같은 먹이를 동시에 선택하지 않는다. 음식 접근은 기존 AI가 직접 물리를 바꾸는 방식이 아니라 최종 `VirtualInput`만 덮어쓴다. 실제 걷기, 마찰, 충돌은 기존 Slugcat movement 경로가 계속 담당한다.

먹이는 현재 지지 표면 위에서 Slugcat의 진행 방향 앞쪽 약 58 desktop pixels에 생성된다. 지지 표면을 찾지 못하면 가장 가까운 monitor work area의 floor를 사용한다. 생성 위치는 해당 표면 좌우 범위 안으로 clamp한다.

접근 거리가 충분히 가까워지고 Slugcat이 grounded 상태이면 열매를 집는다. 8 ticks 동안 들기 자세를 유지한 뒤 18 ticks 간격으로 세 번 bite한다. 완료 시 manager 통계에 1 food point를 기록하지만, 영구적인 생존 meter나 벌점에는 연결하지 않는다.

### `GameLoop`

파일: `src/RainWorldDesktopPet/Core/GameLoop.cs`

40Hz 고정 tick의 처리 순서는 다음과 같다.

1. 음식 자유 물리 업데이트
2. 기존 AI 입력 계산
3. 활성 음식이 있으면 음식 접근 입력으로 최종 intent 조정
4. 기존 Slugcat 물리와 movement 실행
5. 기존 Slugcat graphics 업데이트
6. 최신 머리 위치에 든 음식을 고정하고 bite timer 진행

기존 AI를 매 tick 계속 실행하므로 성격, 필요도, cooldown 값이 음식 섭취 중에도 멈추지 않는다. 음식 controller는 먹이가 활성화된 동안 최종 이동 intent만 제한한다.

### 렌더링과 합성

파일: `src/RainWorldDesktopPet/Graphics/SpriteRenderer.cs`, `src/RainWorldDesktopPet/UI/LayeredOverlayWindow.cs`

음식을 위한 별도 DirectComposition surface를 생성하지 않는다. 각 음식은 소유 Slugcat의 기존 render batch에 포함되고, 해당 loop의 bounds만 필요한 만큼 union한다. 기존 최소 surface 크기가 384px이고 먹이가 가까운 곳에 생기므로 대부분의 경우 surface resize도 발생하지 않는다.

렌더링은 로컬 atlas에 frame이 있으면 원본 `DangleFruit` 두 레이어를 사용한다. 로컬 설치본이 예상과 달라 frame을 찾지 못할 경우 앱 전체를 중단하지 않고 작은 파란 원형 fallback을 그린다. 정상 설치본에서는 자동 테스트가 여섯 frame의 존재와 `#rainWorld` 출처를 확인한다.

### 트레이 UI

파일: `src/RainWorldDesktopPet/UI/LayeredOverlayWindow.cs`

트레이 우클릭 메뉴에 다음 항목을 추가했다.

- `먹이 주기 · 슬러그캣 N`
  - `파란 열매 주기`
  - `선택한 슬러그캣의 먹이 치우기`

메뉴를 열 때 현재 선택 번호와 제한 상태를 갱신한다. 성공 시 balloon을 띄우지 않아 사용을 방해하지 않고, 제한에 도달했을 때만 짧은 안내를 표시한다.

## 5. 입력과 데스크톱 사용성 검토

이 프로젝트의 overlay window는 기본적으로 `WS_EX_TRANSPARENT`와 `WS_EX_NOACTIVATE`를 사용한다. 저수준 mouse hook도 Slugcat을 직접 잡는 left-click에만 개입한다. 이번 업데이트는 `GameLoop.HitTest`에 음식을 추가하지 않았으므로 음식 위 클릭은 기존처럼 아래 프로그램으로 전달된다.

채택하지 않은 UI:

- 전역 키: 게임과 편집기 단축키 충돌 가능
- Slugcat right-click 또는 middle-click: 브라우저/게임/마우스 유틸리티와 충돌 가능
- 화면 고정 food palette: 화면 가림과 항상 위 창 증가
- `먹이 배치 모드` 후 다음 클릭: 사용자가 모드 종료를 잊으면 일반 클릭을 가로챌 위험

현재 방식의 비용은 트레이를 두 번 클릭해야 한다는 점이다. 그러나 자주 반복하는 핵심 작업이 아니라 간헐적인 상호작용이고, 충돌과 화면 점유를 확실히 줄인다는 장점이 더 크다고 판단했다.

## 6. 성능과 안정성

- 물리는 기존 40Hz fixed timestep을 사용한다.
- 음식은 한 개 BodyChunk만 사용한다.
- 같은 tick의 immutable `DesktopCollisionSnapshot`을 재사용한다.
- 음식별 renderer, bitmap, composition surface를 만들지 않는다.
- atlas image는 기존 `RainWorldAtlasSet` 캐시를 공유한다.
- element 이름은 정적 문자열 배열로 캐시한다.
- 음식은 Slugcat당 3개, 전체 12개로 제한한다.
- 먹지 않은 음식은 1200 ticks 후 제거한다.
- 렌더링은 음식 수가 최대 3개인 작은 선형 loop 두 번으로 제한된다.
- 움직이는 window surface의 delta를 음식에도 적용하여 창 이동 시 떠 있거나 뒤처지는 현상을 줄였다.

## 7. 테스트와 빌드

추가된 자동 검증:

- DangleFruit 초기 bites, food points, radius, mass
- bite마다 `0 → 1 → 2` atlas frame 진행
- 마지막 bite 뒤 consumed 상태
- 음식 방향으로 생성되는 `VirtualInput`
- 소유 Slugcat의 target reservation
- food attention target
- 들기, 세 번 bite, 1 food point 완료
- 로컬 Rain World atlas의 `DangleFruit0/1/2A/B` 여섯 frame 존재
- frame이 설치된 원본 `#rainWorld` atlas에서 왔는지 확인

검증 명령:

```powershell
.\build.ps1 -Configuration Release
```

최종 Release 빌드는 경고 0개, 오류 0개로 완료했고 기존 전체 회귀 테스트와 새 음식 테스트가 모두 통과했다. 실행 파일은 `artifacts/Release/SlugcatInMyMonitor.exe`에 생성된다. 네이티브 렌더러인 `SlugcatInMyMonitor.DirectComposition.dll`도 같은 폴더에 있어야 한다.

빌드 도중 기존 실행 파일이 실행 중이면 Windows가 산출물 교체를 막는다. 이 경우 트레이에서 앱을 종료한 뒤 다시 빌드해야 한다.

## 8. 커밋 이력

- `8f71ba5` — `feat: add desktop Dangle Fruit edible model`
- `586cc63` — `feat: add tray feeding and autonomous eating flow`
- `7a78ead` — `feat: render food from local Rain World atlas`
- 문서·최종 검증 커밋 — README, 본 보고서, atlas 회귀 검증과 최적화 정리

각 커밋은 `origin/feature/food-update`에 순차적으로 push했다.

## 9. 새 음식 추가 방법

복합 동작이 없는 정적 edible부터 추가하는 것이 안전하다. 추천 순서는 다음과 같다.

1. `DesktopFoodKind`에 종류를 추가한다.
2. bites, food points, radius, mass, friction, bounce, lifetime을 immutable definition으로 분리한다.
3. 로컬 atlas element 목록을 정의하고 `RainWorldAtlasSet.TryGet`으로 availability를 검사한다.
4. 원작의 최종 bite 부가 효과를 작은 desktop effect interface로 표현한다.
5. `DesktopFoodManager`의 spawn factory와 tray submenu를 추가한다.
6. 원작 계약, 상태 전이, atlas 출처, fallback을 자동 테스트한다.
7. 음식이 기존 composition bounds와 click-through를 깨지 않는지 실제 데스크톱에서 확인한다.

다음 후보 평가:

- EggBugEgg: 비교적 단순한 edible이고 frame 확인이 쉬워 두 번째 후보로 적합
- Mushroom: 먹는 동작은 단순하지만 time slowdown을 데스크톱에서 어떻게 표현할지 제품 결정이 필요
- Fly: creature AI, 날개 animation, capture, sound가 필요하므로 별도 creature 시스템 이후로 연기
- WaterNut/JellyFish: 물, 전기, tentacle 의존성이 커서 현재 desktop terrain 모델과 맞지 않음
- KarmaFlower: karma와 death persistence가 없으므로 장식 이상의 의미를 정하기 전에는 추가하지 않음

정적 아이템이 2개 이상이 되면 `DesktopFood`의 종류별 조건문을 늘리기보다 `DesktopFoodDefinition` registry로 radius, mass, bites, atlas layers, tint, effect를 데이터화해야 한다. Fly처럼 살아 있는 먹이는 `DesktopFood`에 넣지 말고 별도 `DesktopCreature` 계층으로 분리해야 한다.

## 10. 알려진 제한과 다음 권장 작업

- 음식은 같은 높이의 가까운 표면에 생성하도록 최적화되어 있다. 사용자가 창을 급격히 옮겨 먹이가 다른 층으로 떨어지면 Slugcat이 장거리 pathfinding을 하지 못할 수 있으며, 30초 후 자동 제거된다.
- 현재 들기 위치는 head 기반 mouth anchor다. 원작처럼 grasp별 손 animation을 완전히 재현하려면 `SlugcatGraphics`에 food hand target mode를 추가해야 한다.
- bite event 이름은 남기지만 사운드는 재생하지 않는다. 프로젝트 전체 sound backend가 생길 때 event를 연결할 수 있다.
- `FoodPointsEaten`은 세션 통계이며 저장하지 않는다. desktop pet에 영구 배고픔을 넣으면 방치형 사용에서 벌점이 되므로 별도 옵션으로 설계해야 한다.
- 실제 사용 피드백에서 트레이 단계가 번거롭다는 의견이 많을 경우에만 사용자가 직접 지정하는 optional hotkey를 설정 화면에 추가한다. 기본값은 계속 비활성으로 두는 것이 좋다.

## 11. 라이선스와 배포

프로젝트 코드는 MIT License이므로 원래 copyright 및 license notice를 유지하면 수정, fork 공개, 배포가 가능하다. 이번 업데이트는 Rain World asset을 저장소에 포함하지 않는다. 사용자의 로컬 정품 설치본을 런타임에 읽는 기존 구조를 그대로 사용한다.

fork나 release에 다음 항목을 넣지 않아야 한다.

- 추출한 Rain World PNG 또는 atlas 파일
- `Assembly-CSharp.dll` 등 게임 바이너리
- Rain World 설치 데이터의 복사본

배포 ZIP에는 이 프로젝트에서 빌드한 실행 파일과 네이티브 렌더러만 넣고, README에 정품 Rain World 로컬 설치 요구 사항을 유지한다.
