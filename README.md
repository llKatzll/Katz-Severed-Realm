# KSR

<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=waving&color=0A0A0A&customColorList=00f3ff,7b2cbf,ff00aa&height=280&section=header&text=SEVERANCE&fontSize=80&fontAlignY=35&desc=3D%20Rhythm%20%7C%20Cinematic%20Music%20Experience&descAlignY=58&fontColor=ffffff" width="100%" alt="Severance"/>
</p>

<p align="center">
  <b>음악 속으로 들어가다.</b><br><br>
  <img src="https://img.shields.io/badge/Unity-6%20%2F%20URP-black?style=for-the-badge&logo=unity&logoColor=white"/>
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white"/>
</p>

---

## 🎮 게임 소개

**Katz: Severed Realm (KSR)**, 줄여서 **KSR**. 음악의 분위기와 매력을 **시각 연출로 풀어내는 데 집중**한 3D 리듬 게임입니다.

단순히 노트를 맞추는 것을 넘어서, 플레이어가 그 음악의 주인공이 되어 곡을 **느끼고 경험**할 수 있게 만드는 것이 목표예요.

곡마다 다른 컨셉과 연출을 설계하고, 음악의 구조에 맞춰 시각적인 피드백을 구성하고 있습니다. 유저가 단순히 플레이하는 것을 넘어, "이 곡은 이런 매력이 있구나"를 시각적으로 느낄 수 있도록.

- **장르**: 3D Rhythm
- **방향성**: 곡별 시네마틱 연출 · Music Visualization
- **개발 형태**: 1인 개발

---

## 🎮 조작법

총 **9개 키**로 플레이합니다. 모든 키는 설정에서 리바인드 가능.

| 행동 | 기본 입력 | 비고 |
|------|------|------|
| Ground 노트 | `A` `S` `L` `;` | 아래 레일 4키 |
| Upper 노트 | `Q` `W` `O` `P` | 위 레일 4키 |
| Dimension 노트 | `Space` | 차원 노트 처리 |
| 일시정지 | `ESC` ×2 | 2초 안에 두 번 (오입력 방지) |

설정 메뉴에서 9개 키 모두 개별 리바인드 가능합니다.

---

## ▶️ 다운로드 & 실행

1. [Releases](https://github.com/llKatzll/Katz-Severed-Realm/releases)에서 최신 빌드 다운로드
2. 압축 해제 후 exe파일 실행
3. **전체화면 + 헤드폰 권장** — 시각/청각 연출 모두 충분히 즐길 수 있습니다

### 시스템 요구사항

- Windows 10 / 11
- DirectX 11 이상
- 약 500MB 이상 여유 공간
- 60Hz 이상 모니터 권장

---

## 📹 미디어

### 플레이 영상
https://youtu.be/vyXo3-WclCc (일부공개 링크)

---

## 💡 개발 스토리

처음엔 그냥 "예쁜 리듬게임을 만들어보자"는 생각으로 시작했어요.

평소에 동인 음악을 자주 듣는데, 그 곡들이 가진 분위기랑 매력을 정말 잘 살린 케이스를 리듬게임에서 본 적이 없었어요. 곡은 좋은데, 곡의 진가를 게임의 시스템이 제한하는 느낌.

그래서 **연출에 치중된 리듬게임**을 만들고 싶다는 생각이 점점 커졌습니다. 단순히 노트가 떨어지는 게임이 아니라, 곡을 처음 듣는 사람도 "와" 소리와 함께 곡에 집중, 감동을 하게 되는 그런 게임을요.

만들면서 점점 **"이 곡을 어떻게 하면 시각적으로 더 잘 전달할 수 있을까"** 에 집중하게 됐습니다. 단순히 화려한 이펙트를 넣는 게 아니라, **곡의 구조와 감성에 맞춰 연출을 설계**하는 과정이 생각보다 훨씬 어려웠어요. 특히 "이 부분에서 이런 느낌을 주고 싶다"는 의도를 카메라 움직임, 레일 변형, 화면 후처리, 입자 이펙트로 어떻게 풀어낼지 고민하면서 — 음악을 듣는 것과 시각적으로 느끼는 것의 차이를 많이 생각하게 됐어요.

결국 제가 만들고 싶은 건 **이 곡을 모르는 사람도 게임을 플레이하고 나서 그 곡과 작곡가를 좋아하게 되는 경험**이었습니다. 좋은 곡이 있다는 걸 한 명이라도 더 알게 되면 그걸로 충분한 가치가 있다고 생각해요.

아직 부족한 점이 많지만, 앞으로도 곡의 매력을 시각적으로 전달하는 데 더 집중하면서 개발을 이어갈 생각입니다.

---

## 🛠️ 사용 기술

- **엔진**: Unity 6 (Universal Render Pipeline 17)
- **언어**: C#
- **비주얼**: Shader Graph · 직접 작성한 URP 셰이더 · 자체 제작 이펙트,노트 차트 시스템
- **연출 시스템**: PlayableGraph 기반 카메라/레일/스크린 애니메이션 디스패치
- **오디오**: Unity AudioMixer (Master/Music/SFX/Hit 4분리)
- **타이밍**: AudioSettings.dspTime 기반 정밀 박자 (DSP-aware Pause 지원)

---

## 👤 개발자

**Katz (Kachesis)**

- GitHub: [@llKatzll](https://github.com/llKatzll)

---

## ⚠️ 주의사항

이 게임은 **강한 빛**, **빠른 움직임**, **점멸 효과**가 포함되어 있습니다.
광과민성(광과민성 발작)이 있으신 분은 플레이에 주의해 주세요.

---

## 📬 피드백

버그 제보, 플레이 소감, 개선 의견 모두 환영합니다.
GitHub Issues에 남겨주시면 확인하겠습니다.

---

<p align="center">
  <sub>Thank you for playing. Severance awaits.</sub>
</p>
