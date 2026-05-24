# 자막 시스템 재설계 검토 문서

작성일: 2026-05-21
대상: Katz: Severed Realm — 자막(가사) 연출 시스템

---

## 1. 현황 진단

자막 한 줄당 전용 에셋 4종이 따로 존재 (약 30줄).

| 항목 | 개수 | 용량 | 문제 |
|---|---|---|---|
| TGA 텍스처 | 30 | 약 61MB | 무압축(textureCompression 0), 장당 2MB |
| Shader Graph | 30 | 약 4.6MB | 전부 동일 구조(159KB), 텍스처 GUID만 차이 |
| ParticleSystem 프리팹 | 30 | 약 3.6MB | 입자 연출 미사용. Emission만 ON, Billboard 1장 |
| Material | 30 | 약 128KB | 정상 범위 |

핵심 문제 세 가지.

1. ParticleSystem은 "텍스트 1장 + 셰이더 연출"을 담는 그릇일 뿐, 입자 연출이 목적이 아님.
2. 노출 시간을 ParticleSystem Duration(감각값)으로 조절 → "정확한 beat에 IN/OUT" 이라는 의도와 불일치.
3. 텍스트가 이미지로 구워져 있어 빌드 용량 61MB를 그대로 차지. 출시 빌드에 부담.

---

## 2. 목표

- 자막이 정확한 beat에 등장하고 정확한 beat에 퇴장.
- 빌드 용량에서 자막 텍스처 부담 제거.
- 자막 1개 단독 또는 여러 개 동시 표시 가능.
- 자막별 색/연출 차이 허용(현재는 균일, 추후 다양화).
- 새 자막 추가 시 에셋 4종 제작 불필요 — 텍스트 + 타이밍만 입력.

---

## 3. 새 아키텍처 개요

```
[eff.json 차트]  --- Lyric 트리거 (text, beatIn, 길이, 위치, 색)
       |
       v
[LyricConductor]  --- beat 도달 시 IN / 길이 종료 시 OUT
       |
       v
[TMP 풀]  --- World Space TextMeshPro 오브젝트 재사용
       |
       v
[TMP SDF 셰이더 1개]  --- 페이드 + finalint 베니싱
       +
[SDF 폰트 atlas 1개]
```

자막을 별도 시스템으로 완전 분리하지 않고 **기존 eff 차트에 통합**한다.
이유: eff 에디터(타임라인, 저장/로드, 카테고리 UI)를 재활용 → 출시 일정 내 현실적.
자막 타이밍을 다른 이펙트와 같은 타임라인에서 함께 편집 가능.

---

## 4. 데이터 구조

### 4-1. EffectCategory 에 Lyric 추가

```
public enum EffectCategory { Eff, Cam, Rail, Scr, Lyric }
```

### 4-2. EffectTrigger 확장

현재 EffectTrigger 는 presetId 만 들고 텍스트/위치/색을 담지 못함.
자막은 트리거마다 글자가 다르므로 트리거 자체에 데이터가 필요.

추가 필드(자막 트리거에서만 사용, 그 외 카테고리는 비워둠):

```
public string lyricText;       // 자막 글자
public float posX, posY, posZ; // 자막 월드 위치 (lane 그리드 대신)
public float colorR, colorG, colorB, colorA; // 자막 색 (선택, 기본 흰색)
public int styleId;            // 자막 스타일 프리셋 인덱스 (선택)
```

JsonUtility 는 Vector3/Color 직렬화가 불안정하므로 float 분해 권장.
기존 트리거 JSON 과 호환됨(없는 필드는 기본값).

### 4-3. 타이밍 = 기존 Sustained 트리거 재활용

EffectConductor 는 이미 `kind == Sustained` 일 때 `inBeats * secPerBeat` 로
길이를 계산한다. 자막도 동일하게:

- `beat` = 자막 등장 시점 (IN)
- `kind = Sustained`
- `inBeats` = 표시 지속 길이 (beat 단위)
- 퇴장 시점 = `beat + inBeats`

ParticleSystem Duration 의존 제거. 모든 타이밍이 beat 기준 → 정확.

### 4-4. 자막 스타일 프리셋

EffectPresetSO 에 자막용 스타일 프리셋을 소수만 생성(예: Lyric_Default, Lyric_Emphasis).
프리셋이 들고 있는 것: 폰트, 기본 색, 페이드 커브, finalint 베니싱 커브, 기본 크기.
글자(text)는 트리거가 들고, 스타일은 프리셋이 든다. 30개 프리셋 → 1~3개로 축소.

---

## 5. 런타임 — LyricConductor

EffectConductor 와 별개의 컴포넌트로 신설(또는 EffectConductor 내부에 Lyric 디스패치 분기 추가).
별도 컴포넌트 권장 — EffectConductor 가 이미 큼.

동작:

1. eff.json 로드 → Lyric 카테고리 트리거만 추출, beat 정렬.
2. RhythmConductor.SongTime 기준으로 매 프레임 디스패치 인덱스 진행.
3. 트리거 beat 도달 → TMP 풀에서 오브젝트 1개 꺼냄.
   - text, 위치, 색, 스타일 적용.
   - 페이드인 시작.
4. 활성 자막 리스트 관리. 각 자막의 경과 시간으로:
   - 페이드 알파 계산 (스타일 프리셋의 페이드 커브).
   - finalint 베니싱 값 계산 (스타일 프리셋의 베니싱 커브) → MaterialPropertyBlock 으로 셰이더에 전달.
5. `beat + inBeats` 도달 → 페이드아웃 + 베니싱 → 종료 후 풀로 반환.

동시 표시: 활성 자막 리스트가 여러 개를 동시에 들고 있으므로 자연히 지원.

finalint 베니싱: 현재 ParticleSystem Custom Data 곡선으로 셰이더에 주던 값.
TMP 에는 Custom Data 가 없으므로 LyricConductor 가 AnimationCurve 를 평가해
MaterialPropertyBlock 으로 매 프레임 셰이더 프로퍼티에 주입 → 동일 연출 재현.

TMP 풀: FxPoolManager 재활용 가능(프리팹 1종 등록). 또는 LyricConductor 내부에 소규모 전용 풀.

---

## 6. 렌더링

### 6-1. TextMeshPro (World Space)

자막 표시는 `TextMeshPro` 컴포넌트(3D, World Space). UI Canvas 아님.
현재 ParticleSystem 이 월드에 떠서 카메라 연출을 받던 것과 동일하게,
TMP 오브젝트도 월드에 두면 카메라 줌/셰이크/포스트프로세싱을 그대로 받는다.

### 6-2. TMP SDF 셰이더 1개

TMP 는 자체 SDF 셰이더 구조가 있어 일반 Shader Graph 를 그대로 못 쓴다.
TMP 용 SDF 셰이더(URP 호환) 1개를 만들어 페이드 + finalint 베니싱 연출 포팅.
30개 Shader Graph → 1개.

현재 자막 셰이더 연출 = 단순 페이드인/아웃 + finalint 베니싱.
복잡한 왜곡/글리치가 아니므로 TMP SDF 셰이더로 재현 난이도 낮음.

### 6-3. SDF 폰트 atlas 1개

TMP Font Asset(SDF) 1개 생성. 자막에 쓰는 글자(영문 위주로 보임)를 포함.
61MB TGA → 폰트 atlas 1개(보통 1~3MB 이하).

자막마다 다른 폰트/수기 디자인을 쓰고 있었다면 손실 발생 가능 →
6-4 의 마이그레이션 확인 항목 참조.

### 6-4. 폰트 디자인 확인 필요

현재 TGA 자막이 단일 폰트로 통일돼 있으면 TMP 전환에 디자인 손실 없음.
자막마다 다른 손글씨/특수 디자인이면, 그 자막만 예외적으로 텍스처 방식 유지하거나
폰트를 여러 벌 만드는 절충이 필요. (대부분 동일 폰트로 추정 — 셰이더가 전부 동일 구조이므로)

---

## 7. 에디터 통합

eff 에디터를 재활용하되 자막 전용 입력이 추가로 필요.

- EffectListUI: Lyric 카테고리 버튼 추가.
- 자막 트리거 배치 시 입력 패널 필요: 텍스트 문자열, 위치(X/Y/Z), 색, 스타일, 지속 beat.
  - 기존 트리거는 lane 그리드에 클릭으로 배치하지만,
    자막은 자유 위치이므로 별도 입력 UI(텍스트 필드 + 위치 입력)가 필요.
- EffectNoteVisuals: Lyric 트리거를 타임라인에 표시(텍스트 라벨로).
- 위치 미세 조정: 씬 뷰에서 자막 오브젝트를 직접 옮기는 프리뷰 모드가 있으면 편함(2차 작업).

이 부분이 작업량이 가장 크고 불확실. 1차로는 최소 입력 UI 만 만들고,
씬 프리뷰 편집은 출시 후로 미루는 것을 권장.

---

## 8. 마이그레이션

1. TMP Essentials 임포트(미설치 시), SDF 폰트 에셋 생성.
2. TMP SDF 셰이더 제작(페이드 + finalint).
3. Lyric 스타일 프리셋 1~3개 생성.
4. 기존 30개 자막의 텍스트 문자열을 수집 → 새 Lyric 트리거로 입력.
   - 기존 ParticleSystem Duration/위치 값을 참고해 inBeats/위치로 변환.
5. 동작 검증 후 기존 자산 폐기:
   - Pictures/Lyrics TGA 30개
   - Shader/Lyrics Shader Graph 30개
   - preFabs/Lyrics 프리팹 30개
   - Material/Lyrics 머티리얼 30개
   - EffectPresets/Eff 의 자막 프리셋 30개

빌드 용량 약 61MB 이상 감소 예상.

---

## 9. 작업 단계 분할

| 단계 | 내용 | 담당 |
|---|---|---|
| A | EffectEnums/EffectData 확장 (Lyric 카테고리, 트리거 필드) | 코드 — Claude |
| B | LyricConductor 작성 (beat 디스패치, TMP 풀, 베니싱 커브 주입) | 코드 — Claude |
| C | TMP SDF 셰이더 제작 (페이드 + finalint) | Shader Graph — 카츠 (Claude 안내) |
| D | SDF 폰트 에셋 생성 | Unity — 카츠 |
| E | Lyric 스타일 프리셋 생성 | Unity — 카츠 |
| F | 에디터 통합 (Lyric 카테고리, 자막 입력 패널, 타임라인 표시) | 코드 — Claude / 셋업 — 카츠 |
| G | 기존 30줄 자막 마이그레이션 + 검증 | Unity — 카츠 |
| H | 기존 자산 폐기 | Unity — 카츠 |

권장 순서: A → B → C → D → E → (런타임 검증) → F → G → H.
F(에디터)가 가장 무겁다. 런타임(A~E)을 먼저 끝내 자막이 정상 동작하는지 확인한 뒤
에디터를 붙이는 편이 안전.

---

## 10. 리스크 / 검토 포인트

- 폰트 손실 리스크: 자막 폰트가 전부 동일한지 확인 필요(6-4). 동일하면 무손실.
- 에디터 작업량(F)이 커서 출시 일정 압박 시 1차는 최소 입력 UI 로 한정.
- finalint 베니싱 연출이 ParticleSystem 고유 기능(파티클 분해 등)에 의존하면
  TMP 로 100% 재현이 어려울 수 있음 → 셰이더 단계(C)에서 현재 연출을 먼저 분석해 확정.
- TMP World Space 의 렌더 순서/포스트프로세싱 상호작용이 기존 ParticleSystem 과
  다를 수 있음 → 런타임 검증 단계에서 확인.

---

## 11. 대안 (참고)

빌드 용량만 급하면 단기 대안: TGA 30개의 import 설정을 압축으로 일괄 변경
(.meta 수정) → 약 61MB가 약 8MB 수준으로 감소. 단 텍스트가 다소 흐려지고,
타이밍/연출 구조 문제는 그대로 남는다. TMP 전환 시 어차피 폐기되는 임시방편.
