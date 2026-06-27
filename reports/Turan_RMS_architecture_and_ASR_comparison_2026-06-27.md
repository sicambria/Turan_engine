# Turán RMS — Architecture, Algorithm Audit & Modern OSS Comparison

**Report date:** 2026-06-27
**Subject:** `Turan_engine` — *Turán RMS (Recognition Made Simple)* v1.1 (2015), GPL-3
**Author of subject code:** Incze Gáspár (sicambria) — original work 2010, last build VS2013 (2015)
**Codebase size:** ~42,500 LOC C# / .NET, x86, Windows
**Scope of this report:** (A) document the architecture and algorithms; (B) score the *as-built* algorithms 0–100; (C) reframe the task correctly; (D) identify and score the best modern OSS solutions for self-trained ≥99%-accurate recognition; (E) recommend a migration path.

---

## 0. Executive summary

Turán RMS is a **speaker-dependent, isolated-word (command) speech recognizer** for **Hungarian**, built on a **classic 1990s pipeline**: MFCC/LPC feature extraction → **Dynamic Time Warping (DTW) template matching** against per-user recorded templates. It is *not* a continuous-dictation / large-vocabulary system, and it should not be judged as one. Its real job is small closed command sets (e.g. assistive/medical control: *"Fej emel"* / head up, *"Nővérhívó"* / nurse-call, *"Igen/Nem"*).

For that narrow job, the **chosen algorithms were reasonable in 2010** but are **two technology generations behind** today, and — critically — the **as-built implementation degrades its own features** through at least one live bug on the recommended code path. Honest overall score of the existing engine: **≈44/100** (textbook design of the same pipeline would score ~58; the implementation defects cost it).

The modern answer to *"self-trained, ≥99% accurate"* for this use case is **not** open-domain Whisper-style dictation (best models sit at ~5–6 % WER, i.e. ~94–95 % word accuracy on *open* speech — nowhere near 99 %). It is **closed-set spoken-command classification / keyword spotting**, where ≥99 % *is* routinely achievable. Top recommendations, scored in detail in Part D:

1. **NVIDIA NeMo** (Parakeet/Canary backbone + MatchboxNet/command classifier) — **best overall, 88/100**
2. **SpeechBrain** (command classifier / fine-tuned wav2vec2/ECAPA) — **86/100**
3. **wav2vec 2.0 / XLS-R / W2V2-BERT fine-tune** (Hugging Face) — **84/100**
4. **openWakeWord** (per-command detector, fully OSS) — **80/100** for the wake/command-trigger slice
5. **Vosk / Kaldi** (DNN-HMM, streaming, CPU) — **74/100**
6. **Whisper / faster-whisper fine-tune** — **72/100** for *this* closed-command task (higher for general transcription)

> ⚠️ **Accuracy-claim hygiene.** "99 % accurate" is meaningless for a command/wake system without **both** the **False Rejection Rate (FRR)** and the **False Acceptance Rate (FAR/hour)**. Any target in this report means: *closed-set top-1 accuracy ≥99 % on held-out, same-speaker test utterances, with FAR reported separately.* Open-dictation WER and closed-set command accuracy are **different metrics and are not comparable** — the report keeps them apart deliberately.

---

# PART A — Architecture & Algorithms (as built)

## A.1 Solution / module map

The repository is a set of cooperating Visual Studio solutions, each a stage of the same pipeline:

| Module | Role | Key files |
|---|---|---|
| **Turan_core** | The recognition engine: loads feature vectors, runs DTW, returns best-match index | `Engine.cs`, `dtwApp_match.cs`, `lpcData.cs`, `HTK_Interface.cs` |
| **Turan_creator** | Feature-vector creation from WAV (native + HTK paths) | `Creator.cs`, `H_FELDOLGOZO.cs`, `CV_FELDOLGOZO.cs`, `Lpc.cs`, `mfcc.cs`, `Drft.cs`, `FftCalc.cs` |
| **Turan_trainer_GUI** | End-user app to record templates, manage profiles & command sets | `MainForm.cs`, `Train.cs`, `UserProfile.cs`, `commands.txt`, `profiles.txt` |
| **Turan_tester** | Runs recognition against trained templates and fires the mapped action | `Form1.cs` |
| **Felismero_motor_LITE** | Standalone "LITE" all-in-one recognizer (own copy of the DSP + DTW) | `Form1.cs`, `H_FELDOLGOZO.cs`, `mfcc.cs`, `dtwApp_match.cs` |
| **Turan_SC / Turan_SC_minimal** | Signal capture (mic in/out, WAV, FIFO/circular buffer) | `WaveIn.cs`, `WaveOut.cs`, `Recording.cs` |
| **HTK-TEST-CMD** | Shell glue + configs for external HTK `HCopy.exe` feature extraction | `mfcc_config.txt`, `lpc_config.txt`, `*.bat` |

**Provenance of the algorithms (from the in-file headers):**
- DTW search engine ← ISIP `match.java`, Mississippi State Univ. (1997)
- Mel-filtering ← `cvoicecontrol.c`, Daniel Kiecza (2000)
- LPC (Levinson–Durbin) ← Vorbis `lpc.c` (ymnk/JCraft, 2000)
- MFCC reference impl ← CoMIRVA `MFCC.java` (Klaus Seyerlehner)
- Preprocessing math ← `WavInit.pas` (Lécz Dezső, Zahorján András, 2003)

This is a **careful 2010 re-assembly of well-known 1997–2003 building blocks** — solid pedigree, but pre-deep-learning by design.

## A.2 End-to-end data flow

```
                 ┌─────────────────────────── TRAINING (per user) ───────────────────────────┐
  Microphone ──► WAV capture ──► [Feature extraction] ──► serialize template (.mfcc/.lpc/.mfc3)
  (WaveIn)                            │                         one file per command word
                                      │                         grouped by user "profile"
                 └────────────────────┼──────────────────────────────────────────────────────┘
                                      │
                 ┌──────────────────── RECOGNITION ───────────────────────────────────────────┐
  Microphone ──► WAV capture ──► [Feature extraction] ──► signal vector
                                                              │
                          reference templates ───────────────┤
                                                              ▼
                                                   DTW match vs EVERY template
                                                   (dtwApp_match.bestMatch)
                                                              ▼
                                                   argmin(totalCost) ──► command index ──► action
                 └────────────────────────────────────────────────────────────────────────────┘
```

There is **no acoustic model, no language model, no statistical training** — "training" literally means *recording one or more example WAVs per command and storing their feature vectors as templates*. Recognition is nearest-template-by-warped-distance. This is the canonical **template-matching / DTW** paradigm.

## A.3 Feature extraction (two parallel back-ends)

The engine supports two **feature back-ends** (`VectorFileFormat`):

### (1) Native "turan" path — own DSP (`Creator.cs` → `H_FELDOLGOZO`, `CV_FELDOLGOZO`)
1. **Framing** — 256-sample frames, **50 % overlap** (`create_window`); 128 carried from previous frame, 128 new.
2. **Pre-emphasis FIR** — `y[i] = x[i] − 0.95·x[i−1]` (high-pass, boosts highs). HTK config uses `PREEMCOEF = 0.97`.
3. **Hamming window** — `w(i) = 0.54 − 0.46·cos(2πi/N)`.
4. **FFT** — per-frame power spectrum (`FftAlgorithm.Calculate`, radix-2, N=256).
5. **Mel reduction** — 16 hand-coded triangular mel filter banks over the 128-bin spectrum (`CV_FELDOLGOZO.init_mel_filter_banks`), log-compressed, optional channel-mean subtraction (crude cepstral mean normalization).
6. **(MFCC mode) DCT** — *intended* in `mfccszamitas` (DCT-II over 24 bands) → cepstral coefficients.
   - **MFCC vector length:** 15 (mode `mfcc`).
7. **(LPC mode)** — `Lpc.lpc_from_data`: autocorrelation + **Levinson–Durbin recursion** → 12 LPC coefficients per frame; no overlap in LPC mode (`create_window_no_overlap`).
   - **LPC vector length:** 12 (mode `lpc`).

### (2) HTK path — external `HCopy.exe` (`HTK_Interface.cs`, configs in `HTK-TEST-CMD/`)
- Shells out to the **Cambridge HTK toolkit** `HCopy.exe` with `mfcc_config.txt` to produce `MFCC_D_A_T` features:
  `NUMCEPS=15, NUMCHANS=26, CEPLIFTER=22, PREEMCOEF=0.97, USEHAMMING=T, ENORMALISE=T, TARGETRATE=100000` (10 ms frames, 25 ms window).
- `MFCC_D_A_T` = 15 static **+** 15 Δ (delta) **+** 15 ΔΔ (acceleration) **+** 15 ΔΔΔ (third differential) = **60 dimensions/frame** of cepstra + dynamics. This is the README's recommended ("best MFCC results") path.

## A.4 The recognizer: DTW template matching (`dtwApp_match.cs`)

- For each reference template, compute a **frame-to-frame local distance matrix**, then a **DTW accumulated-cost** path under a **slope/Itakura band constraint** (`slope`, `minJ/maxJ` legal-region pruning), with **back-trace** to recover the optimal alignment cost.
- **Local distance metrics** (`frameDistance`): **Euclidean** (default), **Absolute** (L1), or **Itakura** (LPC log-spectral distortion — full reflection-coefficient / autocorrelation derivation present in `ITDDistance`).
- **Decision rule** (`bestMatch`): run DTW against all templates, pick `argmin(totalCost)` → `recogResult` index → mapped command/action.
- Vector width fixed via `Engine.mfcc_lpc_vect_num` (15 MFCC / 12 LPC).

This is a textbook **left-to-right constrained DTW** — exactly the Sakoe–Chiba/Itakura family from the 1970s–80s isolated-word literature.

## A.5 Application model

- **Profiles** (`profiles.txt`) = per-speaker template sets → the system is **explicitly speaker-dependent**.
- **Command sets** (`commands.txt`) = small groups, e.g. 5–8 commands (bed control, nurse-call, yes/no). Each line maps `index-word.wav;Display Label`.
- Target deployment reads as **assistive technology / hands-free medical-bed & home control** in Hungarian.

---

## A.6 Implementation audit — defects that affect the score

These were verified against the live call graph (caller grep), not assumed.

### 🔴 LIVE BUG #1 — HTK dynamic features are summed away (`HTK_Interface.ReadMFCC_D_A_T`)
On the **default, README-recommended** path (`Engine(EngineMode.mfcc, VectorFileFormat.htk)`, used by `Turan_tester`), the 60-dim `MFCC_D_A_T` HTK frame is read as:

```csharp
for i: vector[f,i]  = static[i];   // 15 static
for i: vector[f,i] += delta[i];    // += collapses Δ onto static
for i: vector[f,i] += accel[i];    // += collapses ΔΔ
for i: vector[f,i] += third[i];    // += collapses ΔΔΔ
```

The four coefficient streams are **element-wise summed into a single 15-dim vector** instead of being **concatenated into 60 dims**. The system pays HTK to compute deltas/accelerations — the most discriminative part of MFCC_D_A_T — then **destroys them by addition**. **Impact: major, on the primary path.** Effectively recognizes on smeared static cepstra only.

### 🟠 CHARACTERIZATION #2 — Native "MFCC" mode emits log-mel, not MFCC
The native DCT routine `mfccszamitas` is **dead code** (no caller anywhere in the repo). `Creator.CalculateFeatureVectors` (mfcc/turan path) calls only `Window_Mel_Scale_Reduction` and serializes that. So **native "MFCC" features are log-mel filterbank energies with no DCT** — mislabeled as MFCC. Consequence: feature dimensions stay **highly correlated**, which a plain Euclidean DTW handles worse than decorrelated cepstra. Not a crash; a silent quality reduction + naming trap.

### 🟡 LATENT #3 — DCT scale collapses to zero (dead, but telling)
Inside the dead `mfccszamitas`: `Math.Sqrt(2/24)` → **integer division** `2/24 = 0` → `Sqrt(0) = 0` → the entire MFCC array is multiplied by 0. Were the DCT path ever enabled, it would zero all coefficients. Latent (dead code) so **not** scored against accuracy, but it shows the native MFCC path was never validated end-to-end.

### Other notable smells (not separately scored)
- `BinaryFormatter` serialization for templates (`.mfcc/.lpc`) — deprecated & insecure in modern .NET.
- Magic constants (`magic13`, `costRecord = new double[120]`, 256-frame caps) limiting max utterance length.
- HTK path hard-codes `\\htk\\` working dir and shells out per call — fragile, Windows-only, slow.
- Duplicated DSP/DTW code across `Turan_core` and `Felismero_motor_LITE` (divergence risk).

---

# PART B — Scoring the existing algorithms (0–100)

**Rubric / axes** (same axes reused for the OSS table in Part D):

| Axis | What it measures |
|---|---|
| **Accuracy** | Closed-set top-1 command accuracy achievable *as built*, for a small same-speaker vocabulary |
| **Speed** | Latency & throughput, inference + (re)training cost |
| **Resilience to user voice changes** | Robustness to the *same* user's voice drift (fatigue, illness, mic, day-to-day) **and** any cross-speaker generalization |
| **Other factors** | Noise robustness, vocab scalability, maintainability, dependencies, multilingual/Hungarian fit, footprint |

Scores are for the **as-built code**, with a parenthetical for the **textbook-correct** version of the same algorithm where the gap is due to implementation, per the audit above.

### B.1 Per-component scores

| Component | Accuracy | Speed | Voice-change resilience | Other | **Overall** | Notes |
|---|---:|---:|---:|---:|---:|---|
| **DTW matcher** (`dtwApp_match`) | 55 | 70 | 30 | 50 | **52** | Sound classic algorithm; correct banded DTW + backtrace. Cost grows **linearly with #templates × frames²** — fine for ~10 commands, poor at scale. No statistical modeling of variation. |
| **HTK MFCC_D_A_T** (intended) | 78 | 65 | 55 | 60 | **66** | Excellent feature *as designed* (static+Δ+ΔΔ+ΔΔΔ). But see ↓ |
| **HTK MFCC path (as built)** | 35 | 60 | 30 | 45 | **40** | LIVE BUG #1 sums the 4 streams → throws away dynamics. Guts the recommended path. |
| **Native "MFCC" (log-mel, as built)** | 48 | 70 | 35 | 50 | **48** | No DCT → correlated features hurt Euclidean DTW; mislabeled. Workable but weak. |
| **LPC (Levinson–Durbin, 12)** | 50 | 75 | 30 | 50 | **49** | Correct LPC; more speaker/pitch-sensitive than MFCC, lower noise robustness. Reasonable for clean close-mic. |
| **Distance metrics** (Eucl/L1/Itakura) | 55 | 75 | 35 | 55 | **52** | Itakura is the right tool for LPC and is correctly derived; Euclidean default on correlated features is suboptimal. |
| **Preprocessing** (pre-emph + Hamming + 50% overlap) | 70 | 80 | 50 | 65 | **66** | Standard, correct, no real complaints. The strongest part of the stack. |
| **Pipeline / app model** (profiles, command sets) | 50 | 70 | 25 | 45 | **45** | Clean speaker-dependent template model; zero adaptation, manual retraining, no confidence/rejection threshold (no FAR control). |

### B.2 Existing engine — overall

| | Accuracy | Speed | Voice-change resilience | Other | **Overall** |
|---|---:|---:|---:|---:|---:|
| **Turán RMS (as built)** | **40** | **68** | **30** | **48** | **44 / 100** |
| *Same pipeline, bugs fixed (reference)* | *58* | *68* | *38* | *55* | *≈55–58 / 100* |

**Reading the scores:**
- **Accuracy (40):** The pipeline *can* do decent isolated-command work, but LIVE BUG #1 hobbles the recommended path and the native path skips the DCT. A correct rebuild of the *same* algorithm would reach high-80s%/low-90s% command accuracy in clean, single-speaker conditions — but **not** 99 %, and not robustly.
- **Speed (68):** DTW on a handful of templates is fast and fully offline; the per-call HTK `HCopy.exe` shell-out and O(templates × frames²) scaling are the drags.
- **Voice-change resilience (30) — the weakest axis, and the one that matters most here.** Templates are **fixed snapshots of one recording session**. They do **not** generalize across speakers at all, and they **decay as the same user's voice shifts** — exactly the failure mode for an assistive/medical user who is fatigued, ill, post-operative, or simply varying day to day. There is no adaptation, no averaging across enrollments, no confidence-based rejection. This is the single biggest reason to move off the architecture.
- **Other (48):** Offline + small footprint are genuine strengths; Windows/x86-only, deprecated serialization, HTK dependency, no Unicode-robust I/O, and no path to add commands without re-recording are liabilities.

---

# PART C — Reframing the task (so the comparison is fair)

Turán is **speaker-dependent, isolated-word, small-vocabulary, offline, Hungarian**. To compare modern OSS *honestly*, judge them through that **same lens** and on the **same four axes** — not by their headline open-dictation WER.

Two metric worlds, kept separate:

| | Open dictation (LVCSR) | Closed-set commands / KWS (**Turán's world**) |
|---|---|---|
| Metric | Word Error Rate (WER) | Top-1 accuracy + FRR + **FAR/hour** |
| Best OSS today | ~5–6 % WER (Whisper-lv3, Canary-Qwen, Parakeet) = ~94–95 % word acc. | **≥99 %** routinely (AraSpot 99.59 %/40 kw; Speech-Commands SOTA >98 %) |
| "99 % accurate"? | **Not** reachable on open vocab | **Yes**, and the right target |

**Conclusion:** the user's "≥99 % self-trained" goal is **achievable**, but only in the closed-command framing — by **fine-tuning a multilingual self-supervised backbone** (so Hungarian is covered) into a **closed-set classifier / keyword spotter** on the user's own command audio + augmentation. The reason ≥99 % is feasible now and wasn't in 2010: modern backbones are **pretrained on hundreds of thousands of hours**, so they generalize across a user's voice variation from a *tiny* amount of self-recorded data — directly fixing Turán's weakest axis.

---

# PART D — Modern OSS solutions, scored (same axes, same lens)

All scores are for **this task**: self-trained, speaker-(in/de)pendent, small Hungarian command vocabulary, ideally offline, targeting ≥99 % closed-set accuracy. Numbers drawn from vendor/leaderboard/paper summaries are marked *(reported, unverified)*.

## D.1 Capsule profiles

- **NVIDIA NeMo** — Full training/fine-tuning framework. Backbones: **Parakeet** (RNN-T, RTFx >2000, among fastest on Open-ASR leaderboard) and **Canary** (5.63 % avg WER *(reported)*). For commands, use **MatchboxNet / a small classifier head** on top, or fine-tune the encoder. Strong multilingual + augmentation tooling, ONNX/TensorRT export for offline/edge. License: Apache-2.0-ish (NeMo) — check model cards.
- **SpeechBrain** — PyTorch toolkit with **ready command-classification & KWS recipes** (Google Speech Commands), plus wav2vec2 fine-tune and ECAPA speaker-embedding recipes. Easiest path to a clean closed-set classifier with rejection. Apache-2.0.
- **wav2vec 2.0 / XLS-R / W2V2-BERT** (Hugging Face) — Self-supervised multilingual backbones; **XLS-R covers Hungarian**. Fine-tune to ASR *or* attach a classification head for commands. W2V2-BERT is single-pass, **10–30× faster and ~2.5× more memory-efficient than Whisper-lv3 at similar WER** on low-resource langs *(reported)*. Needs as little as ~20 h to slash WER (−57 % rel. *(reported)*); far less for a closed command set with augmentation. MIT/Apache.
- **openWakeWord** — OSS wake/command-phrase detector; **more accurate than Picovoice Porcupine on at least some test data** *(reported)*; runs 15–20 models in real time on one RPi-3 core *(reported)*. Per-command detectors → naturally gives you FRR/FAR knobs. Apache-2.0. Best for the *trigger/few-command* slice, less so for many mutually-exclusive commands.
- **Vosk** (Kaldi DNN-HMM) — Streaming, **CPU-only, compact, fully offline**, custom LM/vocab support; Hungarian model exists. Trails neural end-to-end on entity accuracy & noise. Apache-2.0. Best "drop-in offline" with the least ML lift.
- **Kaldi** — The most customizable classic toolkit; HMM/DNN, great for domain adaptation, but **heavy to train/operate** and architecturally older. Apache-2.0.
- **Whisper / faster-whisper** — Best general transcription; for *closed commands* it's overkill, heavier, and its open-vocab decoder can hallucinate short commands. Fine-tunable but not the efficiency or ≥99 %-command sweet spot. MIT.
- **Picovoice (Porcupine/Rhino)** — Excellent on-device command/intent engine, but **not OSS** (commercial SDK, free tier). Listed as the commercial yardstick, **excluded from OSS ranking**.

## D.2 Scorecard (0–100, this task)

| Solution | Accuracy (≥99 % feasible?) | Speed / footprint | Voice-change resilience | Other (Hungarian, license, offline, self-train ease) | **Overall** |
|---|---:|---:|---:|---:|---:|
| **NVIDIA NeMo** (Parakeet/Canary + classifier) | 95 | 88 | 90 | 82 | **88** |
| **SpeechBrain** (command-class / w2v2 fine-tune) | 93 | 84 | 88 | 84 | **86** |
| **wav2vec2 / XLS-R / W2V2-BERT** fine-tune | 93 | 86 | 88 | 80 | **84** |
| **openWakeWord** (per-command detectors) | 90 | 92 | 80 | 72 | **80** |
| **Vosk** (Kaldi DNN-HMM) | 82 | 90 | 78 | 78 | **74** |
| **Kaldi** (custom DNN/HMM) | 85 | 72 | 80 | 70 | **74** |
| **Whisper / faster-whisper** fine-tune | 88 | 66 | 85 | 74 | **72** |
| **Coqui STT** (DeepSpeech lineage) | 72 | 78 | 72 | 70 | **68** |
| *Picovoice (commercial ref, not ranked)* | *92* | *90* | *82* | *60 (not OSS)* | *—* |
| **— vs. Turán RMS (as built)** | **40** | **68** | **30** | **48** | **44** |

**Axis notes for the modern field:**
- **Accuracy:** All of the top four can be self-trained to **≥99 % closed-set top-1** on a small Hungarian command vocabulary with held-out same-speaker data + augmentation — the leap Turán cannot make. Report FRR **and** FAR/hour to substantiate any "99 %" claim.
- **Speed/footprint:** Parakeet/W2V2-BERT/openWakeWord/Vosk all run real-time; openWakeWord and Vosk are the lightest for embedded/CPU-only. NeMo/wav2vec2 export to ONNX/TensorRT for offline edge.
- **Voice-change resilience (the decisive axis):** pretrained backbones generalize across a user's day-to-day variation from *tiny* self-recorded data — and degrade gracefully — which is precisely Turán's failure mode (fixed templates, no adaptation). This is why every modern option scores 78–90 here vs Turán's 30.
- **Other:** Hungarian is the sharpest filter — **prefer multilingual backbones (XLS-R / W2V2-BERT / Whisper / Canary)**; English-first models (many KWS demos, openWakeWord's stock models) need Hungarian command audio to train but the architectures are language-agnostic. All listed OSS are permissive (Apache/MIT); Picovoice is not.

---

# PART E — Recommendation & migration path

**For the exact use case (offline Hungarian assistive/medical command control, self-trained, ≥99 %):**

1. **Primary: SpeechBrain or NeMo, closed-set command classifier on a multilingual backbone (XLS-R / W2V2-BERT).**
   - Reuse Turán's existing `commands.txt` / per-user WAV enrollment UX as the **data-collection front end** — the recording workflow is already built and is genuinely good.
   - Train a small classification head (N commands + an explicit **"unknown/reject"** class) on enrollment audio **+ augmentation** (noise, reverb, speed/pitch perturbation, SpecAugment). This is what buys cross-condition robustness and the ≥99 % target.
   - Calibrate a **confidence threshold** and **report FRR + FAR/hour** — the rejection capability Turán entirely lacks.
   - Export to **ONNX/TensorRT** (or CTranslate2 for Whisper) for fully offline inference on the target device.

2. **Lightweight / embedded alternative: openWakeWord (few commands) or Vosk (more vocab, CPU-only).** Lowest ML lift, fully offline, smallest footprint; openWakeWord gives clean per-command FRR/FAR control.

3. **If the existing C#/.NET stack must be kept short-term:** at minimum **fix LIVE BUG #1** (concatenate HTK static+Δ+ΔΔ+ΔΔΔ into 60 dims instead of `+=`-summing them) and either enable a correct DCT in the native path or relabel it as log-mel and switch DTW to a normalized/Mahalanobis distance. That alone should move the *as-built* accuracy from ~40 toward the ~58 "textbook" ceiling — but it will **not** reach 99 % or fix the voice-drift problem. Treat it as a bridge, not a destination.

**Why move at all:** the irreducible limitation is architectural — **fixed per-session templates with no statistical model of variation**. No amount of bug-fixing changes that. Modern self-supervised backbones convert a few minutes of the user's own audio into a model that generalizes across that user's voice changes, which is exactly the assistive-tech requirement and exactly where Turán scores 30/100.

---

## Sources

Open-source ASR landscape, benchmarks & WER:
- [SiliconFlow — Best Open Source Speech-to-Text Models in 2026](https://www.siliconflow.com/articles/en/best-open-source-speech-to-text-models)
- [SiliconFlow — Fastest Open Source Speech Recognition Models in 2026](https://www.siliconflow.com/articles/en/fastest-open-source-speech-recognition-models)
- [Northflank — Best open-source STT model in 2026 (with benchmarks)](https://northflank.com/blog/best-open-source-speech-to-text-stt-model-in-2026-benchmarks)
- [Gladia — Best open-source speech-to-text models in 2026](https://www.gladia.io/blog/best-open-source-speech-to-text-models)
- [AssemblyAI — Top open source STT options for voice applications (2026)](https://www.assemblyai.com/blog/top-open-source-stt-options-for-voice-applications)
- [Deepgram — Benchmarking Whisper, wav2vec 2.0, Kaldi](https://deepgram.com/learn/benchmarking-top-open-source-speech-models)

Custom training / low-resource fine-tuning:
- [Hugging Face — Fine-Tune W2V2-BERT for low-resource ASR](https://huggingface.co/blog/fine-tune-w2v2-bert)
- [Hugging Face — Fine-Tune XLSR-Wav2Vec2 for low-resource ASR](https://huggingface.co/blog/fine-tune-xlsr-wav2vec2)
- [Springer — Exploration of Whisper fine-tuning strategies for low-resource ASR](https://link.springer.com/article/10.1186/s13636-024-00349-3)
- [arXiv — Whisper Turns Stronger: Augmenting Wav2Vec 2.0 for Low-Resource ASR](https://arxiv.org/html/2501.00425v1)
- [arXiv — Improving Speech Recognition Accuracy Using Custom Language Models with the Vosk Toolkit](https://arxiv.org/html/2503.21025v1)

Keyword spotting / command recognition / wake words:
- [openWakeWord (GitHub)](https://github.com/dscripka/openWakeWord)
- [Picovoice — Benchmarking a Wake Word Detection Engine](https://picovoice.ai/blog/benchmarking-a-wake-word-detection-engine/)
- [Picovoice — Wake Word Detection Guide 2026](https://picovoice.ai/blog/complete-guide-to-wake-word/)
- [awesome-keyword-spotting (GitHub)](https://github.com/zycv/awesome-keyword-spotting)
- [arXiv — AraSpot: Arabic Spoken Command Spotting (99.59% / 40 keywords)](https://arxiv.org/pdf/2303.16621)
- [arXiv — Howl: A Deployed, Open-Source Wake Word Detection System](https://arxiv.org/pdf/2008.09606)

Subject code (this repository): `Turan_core/`, `Turan_creator/`, `Felismero_motor_LITE/`, `HTK-TEST-CMD/`, `README.TXT` — *Turán RMS v1.1 (2015), GPL-3.*

> **Caveats:** Leaderboard/vendor figures above are open-dictation metrics and are **reported, not independently re-verified** here; they are not directly comparable to the closed-set command accuracy that defines Turán's task. Scores in Parts B and D are reasoned engineering assessments on the stated rubric, calibrated to this specific use case, not measured benchmark outputs.
