> ## 📦 This project is **archived**
>
> Turán RMS is kept here for historical reference and is **no longer actively
> developed**. The code targets the .NET Framework / WinForms era (2011–2015)
> and is Windows-only.
>
> ### ➡️ Looking for the active projects?
>
> Two successor projects carry these ideas forward:
>
> - **[TalkTeach ASR](https://github.com/sicambria/talkteach-asr)** — an
>   easy-to-use, offline, cross-platform desktop app that trains
>   state-of-the-art ASR models end-to-end through a four-step
>   *Record → Check → Teach → Try* wizard (Whisper-LoRA fine-tuning, ~99
>   languages, no ML expertise required). **Start here for the modern training
>   wizard and more advanced solutions.**
> - **[SpeechAngel](https://github.com/sicambria/speechangel)** — on-device,
>   trainable, language-independent voice control for **Android**, built for
>   people who cannot use their hands and whose speech may be atypical. It is
>   the direct spiritual descendant of Turán: speaker-dependent **MFCC + DTW**
>   acoustic template matching, fully offline, no language model.

---

# Turán RMS — *Recognition Made Simple*

[![Status: Archived](https://img.shields.io/badge/status-archived-lightgrey.svg)](#-this-project-is-archived)
[![License: GPL v3](https://img.shields.io/badge/license-GPLv3-blue.svg)](LICENCE-GPL3.TXT)
[![Language: C#](https://img.shields.io/badge/language-C%23-178600.svg)](#)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D6.svg)](#)
[![Framework: .NET Framework](https://img.shields.io/badge/.NET-Framework-512BD4.svg)](#)
[![Successor: TalkTeach ASR](https://img.shields.io/badge/successor-TalkTeach_ASR-brightgreen.svg)](https://github.com/sicambria/talkteach-asr)
[![Successor: SpeechAngel](https://img.shields.io/badge/successor-SpeechAngel-brightgreen.svg)](https://github.com/sicambria/speechangel)

A speaker-dependent, isolated-word (command) **speech recognition engine** for
Windows, written in C#. It captures audio, extracts acoustic features
(LPC or MFCC), and recognizes spoken commands by **Dynamic Time Warping (DTW)
template matching** against a user-trained vocabulary.

The engine was designed with assistive / hands-free command control in mind:
a small, personalized command set trained per user rather than large-vocabulary
dictation.

- **License:** GNU General Public License v3 — see [`LICENCE-GPL3.TXT`](LICENCE-GPL3.TXT) (Hungarian: [`LICENCE-GPL3-HU.TXT`](LICENCE-GPL3-HU.TXT))
- **Original author / contact:** https://github.com/sicambria · sicambria@users.sourceforge.net
- **First release:** v1.0 (2011-01-03) · **Last historical build:** v1.1 (2015-06-05, Visual Studio 2013)

---

## How it works

```
  microphone / WAV
        │
        ▼
  ┌───────────────┐   pre-emphasis, framing, Hamming window
  │  DSP front-end │──────────────────────────────────────────┐
  └───────────────┘                                            │
        │                                                      │
        ├── LPC analysis ───────────────┐                      │
        └── MFCC  (native log-mel, or    │   feature vectors   │
                   HTK MFCC_D_A_T)       │   per frame          │
                                          ▼                     │
                                  ┌───────────────┐             │
   trained templates ──────────▶ │  DTW matching  │ ◀───────────┘
   (.mfcc / .lpc files)          │ (Euclidean /   │
                                  │  Itakura)      │
                                  └───────────────┘
                                          │
                                          ▼
                              best-matching command index
                              (or reject, when calibrated)
```

1. **Capture** audio from the microphone or a WAV file.
2. **Front-end DSP** applies pre-emphasis, frames the signal, and windows it.
3. **Feature extraction** produces per-frame vectors via **LPC** or **MFCC**.
   MFCC can use the bundled native extractor or external **HTK** `HCopy.exe`
   (`MFCC_D_A_T`: static + Δ + ΔΔ + ΔΔΔ).
4. **Training** records several examples per command and stores them as feature
   **templates**.
5. **Recognition** time-aligns the input against every template with **DTW**
   and returns the closest command.

> **Feature mode:** LPC mode is recommended for the native pipeline. For the
> best MFCC results, use the **[HTK toolkit](http://htk.eng.cam.ac.uk/)**
> (`HCopy.exe`) as the feature extractor.

---

## Repository layout

| Module | Role |
|---|---|
| **`Turan_core/`** | The recognition engine: `Engine.cs`, DTW matcher (`dtwApp_match.cs`), HTK feature reader (`HTK_Interface.cs`). |
| **`Turan_creator/`** | Feature/template creation and the DSP front-end (`H_FELDOLGOZO.cs`, `CV_FELDOLGOZO.cs`, LPC, FFT, WAV I/O). |
| **`Turan_trainer_GUI/`** | WinForms training application — record samples and build per-user command templates. |
| **`Turan_tester/`** | WinForms app for live recognition testing against trained templates. |
| **`Turan_SC/`**, **`Turan_SC_minimal/`** | Sound-capture / audio I/O components (recording, WAVE in/out, FFT). |
| **`Felismero_motor_LITE/`** | Standalone "recognition engine lite" build (self-contained DSP + DTW). |
| **`HTK-TEST-CMD/`** | Helper scripts/config for running HTK feature extraction from the command line. |
| **`reference/unused-native-mfcc/`** | Quarantined CoMIRVA-derived native MFCC port — reference only, **not** wired into the build. |
| **`reports/`**, **`plans/`**, **`ROADMAP.md`** | Audit findings, remediation plans, and the bug roadmap (see below). |

> Some DSP/DTW code is currently duplicated across `Turan_core`,
> `Turan_creator`, and `Felismero_motor_LITE`. Consolidating it into a single
> shared library is tracked in the roadmap.

---

## Building & running

> ⚠️ **This is a .NET Framework / WinForms project and builds on Windows only.**
> There is no .NET-Core/cross-platform build, and it has not been ported to
> modern .NET.

1. Open the relevant `*.sln` in **Visual Studio** (last built with VS 2013).
2. **On x64 systems, set the build architecture to x86.**
3. Build the modules and update inter-project references as needed.
4. (Optional, for MFCC) install the **HTK toolkit** and make `HCopy.exe`
   reachable so the engine can use HTK-quality features.
5. Use **`Turan_trainer_GUI`** to record and train a user's command set, then
   **`Turan_tester`** to try recognition.

---

## Recent changes (high level)

This archive received a focused **correctness audit and remediation pass**.
Highlights:

- **Recognition-core fixes** — unified the DTW feature-vector width, repaired
  the HTK feature-file reader (dynamic Δ/ΔΔ/ΔΔΔ streams were previously summed
  away), and added a **rejection sentinel** so out-of-vocabulary input can be
  rejected once a threshold is calibrated.
- **DSP math repaired** — restored pre-emphasis on the native path, fixed a
  zero-collapsing DCT scale factor and an off-by-one window, de-aliased the FIR
  filter, and removed dead native DTW code.
- **Native MFCC port** — the CoMIRVA-derived MFCC implementation was finished
  and **quarantined** under `reference/unused-native-mfcc/` as unused reference
  material.
- **Documentation** — verified the bundled licenses, recorded known
  limitations, and added an architecture/ASR comparison report plus a full
  bug roadmap.

> **Verification status:** the fixes are **code-review-verified, not
> compiler-verified** — there is no .NET build toolchain in the maintenance
> environment, so nothing was compiled or run. See `plans/PROGRESS.md` for the
> full record and `ROADMAP.md` for per-bug status.

---

## Known limitations

- **Speaker-dependent**, isolated-command recognition — not large-vocabulary or
  continuous dictation; templates must be trained per user.
- The DTW/template architecture has an accuracy ceiling; it will not reach
  modern end-to-end ASR quality. See
  [`reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md`](reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md).
- Two follow-ups gate any production use (see `ROADMAP.md`): native `.mfcc`
  templates must be **regenerated** after the pre-emphasis fix, and the
  **rejection threshold is uncalibrated** (the build performs no input
  rejection until tuned on runtime data).
- Windows-only; uses the now-deprecated `BinaryFormatter` in places.

---

## Documentation

- **[`ROADMAP.md`](ROADMAP.md)** — ranked bug roadmap with remediation status.
- **[`reports/`](reports/)** — architecture & ASR comparison, real-time ASR
  library survey, and training-wizard research/design notes.
- **[`plans/`](plans/)** — per-bug plans, reviews, and the progress log.
- **[`README.TXT`](README.TXT)** — the original historical build notes.

---

## License

This project is licensed under the **GNU General Public License v3**.
See [`LICENCE-GPL3.TXT`](LICENCE-GPL3.TXT) for the full text.
