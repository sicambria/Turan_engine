# Open-Source, Local, Low-Resource, Trainable, Language-Independent Near-Real-Time Speech Recognition — A 2026 Research Report

**Date:** 2026-06-27
**Author:** Deep-research harness — primary run (6-angle → 22 sources → 103 claims → 25 verified, 23 confirmed / 2 killed) + library-landscape re-run (4-angle → 14 sources → 19 verified / 19 confirmed / 0 killed) + research-toolkits re-run (3-angle → 12 sources → 35 verified / 35 confirmed / 0 killed) + analyst synthesis. **77 claims adversarially verified across 48 sources; every named library is cited.**
**Scope:** Open-source ASR libraries that run locally, need modest resources, are trainable on custom/low-resource languages, are language-independent, and support near-real-time (streaming) recognition. Algorithms are identified explicitly; candidates are scored in exact SI units and on a 0–100 relative spectrum.
**See also:** `ASR_training_GUI_wizard_research_and_design_2026-06-27.md` — verified survey of no-code/GUI training tooling (and why none trains the best models end-to-end), plus a detailed design for a child-proof cross-platform training GUI built from reused OSS.

> **Provenance convention.** **[V]** = independently fetched and survived 3-vote adversarial verification (citation given). **[V\*]** = extracted from an authoritative *primary* source (official docs / GitHub README / peer-reviewed paper) in a follow-up run, but not put through the 3-vote stage. **[B]** = textbook/background — **no longer used for any library** after three research runs. Every candidate library in this report now carries a citation. Run totals: **primary** (6 angles, 22 sources, 25 verified / 23 confirmed) + **library-landscape re-run** (4 angles, 14 sources, **19 verified / 19 confirmed / 0 killed** — Kaldi, Vosk, Julius, Coqui/DeepSpeech, wav2letter, PocketSphinx, sherpa-onnx/k2/icefall) + **research-toolkits re-run** (3 angles, 12 sources, **35 verified / 35 confirmed / 0 killed** — ESPnet, SpeechBrain, Silero).

---

## 1. Executive summary

- **The field is fully end-to-end neural as of 2026.** The GMM-HMM and DNN-HMM hybrid pipelines that dominated the 2000s have been displaced by Conformer/FastConformer encoders paired with one of three decoder families — **CTC**, **RNN-Transducer (RNN-T / TDT)**, or **attention encoder-decoder (AED)** — plus self-supervised (wav2vec2/HuBERT/XLS-R) and weakly-supervised (Whisper-style) transformers. **[V]** (arXiv 2510.12827; IEEE/ACM TASLP 2023, doi:10.1109/TASLP.2023.3328283)
- **Compact, on-device, streaming ASR under ~1 GB is real.** NVIDIA's **Nemotron streaming 0.6B** (cache-aware streaming Conformer-Transducer) reaches **8.20 % WER** quantized to **int4 (0.67 GB)**, runs **>6× faster than real-time on CPU** (RTFx 7.20×), with **0.56 s** algorithmic latency. **[V]** (arXiv 2604.14493) **Moonshine v2 Medium** (244.93 M params) hits **258 ms** latency on edge CPUs. **[V]** (arXiv 2602.12241)
- **The accuracy leaders are not Whisper.** NVIDIA **Parakeet-TDT-0.6B-v3** (FastConformer + Token-and-Duration Transducer) leads the HuggingFace Open ASR Leaderboard at **6.32 % avg WER / RTFx 3332.74**; **Canary-1B-v2** reaches **7.15 % English** WER, beating Whisper-large-v3 (7.44 %) at ~7–10× the throughput. **[V]** (arXiv 2509.14128)
- **">99 % accuracy" (<1 % WER) is not realistically achievable** for general-purpose, multilingual, or streaming ASR. Best leaderboard averages sit at **6–8 % WER**; even "human parity" milestones land at **~5–6 % WER** on clean telephone speech. Sub-1 % is only approachable in narrow, clean, single-language, in-domain conditions, usually with a coupled language model. **[V]** (synthesis across arXiv 2509.14128, 2604.14493, 2601.19919; Microsoft/IBM parity arXiv 1610.05256, 1703.02136)
- **Language-independent low-resource trainability is well demonstrated.** XLS-R (wav2vec2, 128 languages, ~436 k pretraining hours) fine-tunes to **~32 % WER on ~4 h of Turkish**; **LoRA** adapts Whisper-tiny to Cantonese (CER **49.5 % → 11.1 %**) updating only **1.6 %** of weights, quantizable to a **60 MB** INT8 edge checkpoint. **[V]** (HF XLS-R blog; PMC12431075)

**Bottom-line recommendation by use case** is in [§8](#8-decision-guide--recommendations).

---

## 2. Evolution of ASR algorithms, 2000 → 2026

The trajectory below is the textbook consensus, corroborated by multiple surveys. **[V]** (arXiv 2510.12827 "ASR in the Modern Era"; IEEE/ACM TASLP 2023 "End-to-End Speech Recognition: A Survey"; arXiv 2212.04356 Whisper)

| Era | Years | Dominant algorithm | Key idea | Representative systems/toolkits |
|---|---|---|---|---|
| **GMM-HMM** | ~1990–2010 | Gaussian Mixture Model emission + Hidden Markov Model sequence | Hand-engineered MFCC features; HMM models temporal state transitions, GMM models acoustic emission per state; separate pronunciation lexicon + n-gram LM | HTK, Sphinx-II/III, Julius, early Kaldi |
| **DNN-HMM hybrid** | ~2010–2016 | Deep neural net replaces GMM for emission probabilities | DNN/TDNN/LSTM acoustic model scores HMM states; WFST decoder fuses AM + lexicon + LM. **>50 % relative WER reduction** vs pre-DL. **[V]** (TASLP 2023) | **Kaldi** (nnet3, chain/LF-MMI), **Vosk** |
| **CTC / end-to-end** | 2014–2018 | Connectionist Temporal Classification | Removes need for frame-level alignment; single network maps audio → characters/tokens; optional external LM | DeepSpeech (Mozilla), wav2letter, EESEN |
| **Attention encoder-decoder (AED)** | 2015–2020 | Seq2seq with attention (LAS) | Encoder summarizes audio, autoregressive decoder attends + emits tokens; strong accuracy, weak streaming | ESPnet, early SpeechBrain |
| **RNN-Transducer (RNN-T)** | 2016–2021 | Encoder + prediction net + joint net | Natively streaming, monotonic; the production workhorse for on-device dictation | Kaldi/k2 (sherpa), NeMo, on-device assistants |
| **Transformer / Conformer** | 2018–2022 | Self-attention; Conformer = conv + attention | Conformer captures local (conv) + global (attention) structure; current encoder of choice | Conformer (Google 2020), FastConformer (NVIDIA) |
| **Self-supervised (SSL)** | 2020–2022 | wav2vec 2.0 / HuBERT / XLS-R | Pretrain on unlabeled audio (contrastive or masked-prediction), fine-tune with tiny labeled sets — the low-resource breakthrough. HuBERT uses offline k-means targets + BERT-like masked loss, matching/surpassing wav2vec2. **[V]** (TASLP 2021, doi:10.1109/TASLP.2021.3122291) | fairseq, HF Transformers, SpeechBrain |
| **Weakly-supervised large multilingual** | 2022–2024 | Whisper-style encoder-decoder transformer | Train on 680 k h of weakly-labeled web audio across 90+ languages; robustness from data diversity, zero-shot multilingual. **[V]** (arXiv 2212.04356) | Whisper, whisper.cpp, faster-whisper, distil-whisper |
| **Efficient/streaming SOTA + speech-LLMs** | 2024–2026 | FastConformer+TDT/AED; speech-augmented LLMs; ergodic streaming encoders; distillation | Beat Whisper at a fraction of compute; LLM decoders (Canary-Qwen, Granite-Speech, Phi-4-multimodal) top leaderboards; sub-1 GB streaming on CPU | Parakeet, Canary, Nemotron-streaming, Moonshine v2, Moonshine, distil-whisper |

**Caveat on "hybrid is dead."** One 2019-era survey notes end-to-end models, while beating GMM-HMM, were then *still worse than or at best comparable to* DNN-HMM hybrids. **[V]** (fetched, 2019) That gap closed over 2020–2024 with SSL pretraining + Conformer + large data; by 2026 E2E is dominant. **[V]** (TASLP 2023)

---

## 3. Algorithm → library map (deliverable 1)

| Library / model | Underlying algorithm(s) | Streaming? | Trainable on custom langs? |
|---|---|---|---|
| **Kaldi** | GMM-HMM, **DNN-HMM hybrid** (**TDNN** acoustic model, **chain / LF-MMI**, cross-entropy), WFST decoding | Yes (online2) | Yes — full training recipes **[V\*]** (kaldi PR #781: "TDNN with chain LF-MMI training") |
| **Next-gen Kaldi (k2 / icefall / sherpa-onnx)** | **Pruned RNN-T transducer** over a **Conformer/Zipformer** encoder + **stateless (conv) decoder**; differentiable WFST (k2) | **Yes** (sherpa-onnx streaming Zipformer) | Yes **[V\*]** (Interspeech 2022 arXiv 2206.13236: "Conformer encoder … decoder is stateless rather than recurrent"; sherpa-onnx docs) |
| **Vosk** | **Kaldi-based DNN-HMM hybrid**, compact models, native streaming | **Yes** | Via Kaldi recipes (harder) **[V]** (official site: 50 MB portable models, streaming API, runs on Pi/Android/iOS) |
| **PocketSphinx / CMU Sphinx** | **GMM-HMM** — semi-continuous HMM (SCHMM), 5-state Bakis topology (legacy) | Yes | Yes (dated) **[V\*]** (CMU ICASSP 2006 paper) |
| **Julius** | **Context-dependent HMM + word N-gram LM**, 2-pass tree-trellis decoder | Yes (real-time) | Yes (dated) **[V]** (official GitHub README) |
| **Coqui STT / Mozilla DeepSpeech** | **RNN + CTC** (5-layer net, 4th layer forward-only recurrent → causal/streaming) + external **KenLM** n-gram scorer | Partial (forward-recurrence enables streaming) | Yes — designed for fine-tuning **[V]** (DeepSpeech/Coqui official docs; project now unmaintained) |
| **wav2letter / Flashlight** | **CNN acoustic model + CTC/ASG**, beam-search decoder | Limited | Yes **[V]** (Vivoka benchmark: LER 6.9 / WER 7.2 w/ MFCC) |
| **wav2vec 2.0 / XLS-R (fairseq/HF)** | **Self-supervised CNN+Transformer**, fine-tuned with **CTC** head | CTC chunked | **Yes — excels at low-resource** **[V]** (HF XLS-R) |
| **HuBERT** | **SSL masked-prediction** w/ offline k-means targets; CTC fine-tune | CTC chunked | Yes **[V]** (TASLP 2021) |
| **SpeechBrain** | Toolkit: **hybrid CTC/attention** (Conformer + Transformer decoder + LM), **wav2vec2+CTC**, **Conformer-Transducer (RNN-T)** streaming via Dynamic Chunk Training | **Yes** (streaming Conformer-T) | **Yes — research-friendly** **[V]** (HF model cards + docs) |
| **ESPnet** | Toolkit: **hybrid CTC/attention E2E** (joint one-pass decoding); encoders **BLSTM → Conformer / Branchformer / E-Branchformer**; 6-layer Transformer decoder; CTC & RNN-T variants | Yes (ContextualBlockConformer) | **Yes** **[V]** (arXiv 1804.00015, 2207.02971, 2305.11073 + docs) |
| **NVIDIA NeMo** | **FastConformer** encoder + CTC / **TDT-RNN-T** / AED decoders | Yes (cache-aware) | **Yes — strongest training stack** **[V]** (arXiv 2509.14128) |
| **Whisper / whisper.cpp / faster-whisper** | **Encoder-decoder Transformer**, weakly supervised, multilingual | No (chunked pseudo-stream) | Fine-tunable (incl. LoRA) **[V]** (arXiv 2212.04356; PMC12431075) |
| **distil-whisper** | Distilled Whisper (encoder-decoder) | No | Distillation pipeline **[V]** (referenced in arXiv 2601.19919 comparisons) |
| **Silero** | **CTC** acoustic model; feed-forward (grouped 1D conv + squeeze-excitation + transformer blocks), no RNN/attention/phonemes | Yes | Yes — small model 25–35 M params, trainable on 2×1080 Ti **[V]** (PyTorch Hub; thegradient.pub) |
| **Parakeet (NeMo)** | **FastConformer + Token-and-Duration Transducer (TDT / RNN-T-style)** | Yes | Yes **[V]** (arXiv 2509.14128; HF parakeet-tdt-0.6b-v3) |
| **Canary (NeMo)** | **FastConformer encoder + Transformer (AED) decoder, 8× subsampling** | No (batch) | Yes, multilingual **[V]** (arXiv 2509.14128) |
| **Moonshine / Moonshine v2** | Encoder-decoder; v2 = **"ergodic streaming encoder" (sliding-window self-attention)** | **Yes (v2)** | Yes (small models) **[V]** (arXiv 2602.12241) |
| **Nemotron streaming 0.6B** | **Cache-aware streaming Conformer-Transducer (RNN-T)** | **Yes** | Yes **[V]** (arXiv 2604.14493) |

---

## 4. Scoring in exact SI units (deliverable 2a)

Metrics are pulled from the cited sources. **They are NOT strictly apples-to-apples** — test sets, decoding configs, with/without-LM, and hardware (CPU vs A100 vs L4) differ. WER is a percentage (lower = better); **RTFx** = inverse real-time factor (audio-seconds processed per wall-second; >1 = faster than real time); **latency** = time-to-first-token / algorithmic delay; **size** = checkpoint storage; **RAM** = inference footprint.

### 4a. 2025–2026 neural SOTA (verified figures)

| Model | Params | WER (avg, %) | RTFx / speed | Latency | Disk size | Source |
|---|---|---|---|---|---|---|
| **Parakeet-TDT-0.6B-v3** | 600 M | **6.32** (Open ASR LB) | **3332.74×** | — (batch) | ~2.4 GB fp32 | **[V]** arXiv 2509.14128 |
| **Canary-1B-v2** | ~978 M–1 B | **7.15** EN / ~8.1 multiling. | 749× (~7–10× faster than Whisper) | — (batch) | ~3.5 GB | **[V]** arXiv 2509.14128 |
| **Canary-Qwen-2.5B** (speech-LLM) | 2.5 B | **5.63** (top of LB, early 2026) | high | — | ~5 GB | **[V]** (fetched 2026-01-07) |
| **Whisper-large-v3** | 1,550 M | 7.44 | 145.51× (L4) | 0.17 s p50 (turbo cfg) | ~3.9 GB RAM | **[V]** arXiv 2509.14128; (fetched 2026-02-05) |
| **Whisper-large-v3-turbo** | 809 M | 8.93 (one bench) | 41.1× (L4, bf16) | 0.173 s p50 | 2.3 GB RAM / 2,299 MB VRAM | **[V]** (fetched 2026-03-27) |
| **ASKD / FastWhisper-large** | 740 M | 6.37 (6-bench avg) | (5× claim **refuted**) | — | ~1.5 GB | **[V medium]** arXiv 2601.19919 |
| **Nemotron streaming 0.6B (int4)** | ~600 M | **8.20** streaming | **7.20× RTFx (CPU)** | **0.56 s** | **0.67 GB** | **[V]** arXiv 2604.14493 |
| Nemotron streaming 0.6B (int8) | ~600 M | 8.01 | 7.25× | 0.56 s | 1.28 GB | **[V]** arXiv 2604.14493 |
| Nemotron streaming 0.6B (batch baseline) | ~600 M | 7.07 | 6.73× (fp32) | — | 2.47 GB | **[V]** arXiv 2604.14493 |
| **Moonshine v2 Medium** | 244.93 M | matches/beats Whisper-L-v3 (EN) | "43.7× faster than Whisper-L-v3" (self-reported) | **258 ms** | sub-1 GB | **[V]** arXiv 2602.12241 |
| Moonshine v2 Small | 123.36 M | — | streaming | bounded TTFT | sub-1 GB | **[V]** arXiv 2602.12241 |
| Moonshine v2 Tiny | 33.57 M | — | streaming | bounded TTFT | sub-1 GB | **[V]** arXiv 2602.12241 |

### 4b. Whisper size ladder (RAM/params reference) **[V]** (fetched 2026-02-05)

| Whisper size | Params | Disk | Inference RAM |
|---|---|---|---|
| Tiny | 39 M | 75 MiB | ~273 MB |
| Base | 74 M | — | ~388 MB |
| Small | 244 M | — | ~852 MB |
| Medium | 769 M | — | ~2.1 GB |
| Large-v2/v3 | 1,550 M | 2.9 GiB | ~3.9 GB |
| Turbo | 809 M | 1.6 GiB | ~2.3 GB |

### 4c. Trainable toolkits & classic systems (now cited; provenance per cell)

| Library | Algorithm | Model size | Inference RAM | WER (if reported) | Streaming | Provenance |
|---|---|---|---|---|---|---|
| **Vosk** | Kaldi DNN-HMM hybrid | **small ~50 MB**; server up to ~3 GB | **~300 MB** (small); **up to 16 GB** (server) | — | **Yes** (zero-latency API) | **[V]** site / **[V\*]** model-list page |
| **Kaldi** | TDNN + chain/LF-MMI (DNN-HMM) | 50 MB – few GB | 0.5–4 GB | <4% LibriSpeech-clean (chain TDNN-F, typical) | Yes (online2) | **[V\*]** PR #781 |
| **Next-gen Kaldi (sherpa-onnx streaming Zipformer)** | Zipformer + pruned RNN-T | **~20 M-param** small model (tens of MB ONNX) | low (CPU/mobile) | LibriSpeech-grade | **Yes** | **[V\*]** sherpa-onnx docs + HF card |
| **PocketSphinx** | Semi-continuous GMM-HMM, 5-state Bakis | 10–100 MB | <200 MB | high (legacy) | Yes (very light, hand-held) | **[V\*]** CMU ICASSP 2006 |
| **Julius** | Context-dependent HMM + word N-gram | small | **<32 MB work area** (<64 MB for 20k-word + 3-gram LM) | depends on AM/LM | Yes (real-time) | **[V]** GitHub README |
| **Coqui STT / DeepSpeech** | RNN + CTC + KenLM 5-gram scorer | ~190 MB AM (+ scorer) | ~0.5 GB | **5.97% LibriSpeech-clean** | Partial (forward recurrence) | **[V]** docs + Vivoka |
| **wav2letter / Flashlight** | CNN + CTC/ASG | — | — | **WER 7.2 / LER 6.9** (MFCC) | Limited | **[V]** Vivoka benchmark |
| **wav2vec2-XLS-R-300M** | SSL Transformer + CTC head | ~1.2 GB fp32 | ~2–4 GB (train) | ~32% on 4 h Turkish | CTC chunked | **[V]** HF XLS-R |
| **ESPnet** | Hybrid CTC/attention; Conformer/Branchformer/E-Branchformer | 38–116 M params (recipe) | varies | **2.4% / 5.5%** (Branchformer 116 M, LS-960 clean/other); 6.3% / 17.0% (E-Branchformer 38 M, LS-100) | **Yes** (contextual block) | **[V]** arXiv 2207.02971, 2305.11073 |
| **SpeechBrain** | Conformer+CTC/attn+LM; wav2vec2+CTC; Conformer-Transducer (streaming) | recipe | varies | **wav2vec2+CTC 1.90% / 3.96%**; Conformer+LM 2.01% / 4.52%; **streaming 3.10% @1280 ms, 3.62% @320 ms** chunk | **Yes** (Dynamic Chunk Training) | **[V]** HF cards + docs |
| **Silero** | CTC, grouped-1D-conv + SE + transformer blocks | **xxsmall 25–50 MB (10–15 MB int8)**; small 50–200 MB; large 300–500+ MB | low (CPU) | **5.5% / 13.5%** (V5 EN, LS clean/other; 6.9% in V1) | Yes | **[V]** github.com/snakers4/silero-models |

---

## 5. Relative 0–100 spectrum scores (deliverable 2b)

Scores are **analyst-assigned on a 0–100 scale** anchored to the SI evidence above, normalized across the candidate set. Higher is always better, including **Resource-efficiency** (inverted: low resource → high score) and **Robustness to speaker/voice change** (speaker independence + acoustic resilience). These are comparative judgments, not measured quantities — treat as a decision aid, not ground truth.

| Library / model | Speed | Accuracy | Robustness (speaker/voice) | Resource-efficiency (low=high) | Trainability / customizability | Streaming |
|---|---|---|---|---|---|---|
| **Vosk** | 85 | 62 | 70 | **92** | 55 | **95** |
| **Kaldi** | 70 | 78 | 80 | 45 | **88** (full control) | 80 |
| **Next-gen Kaldi (k2/sherpa)** | 88 | 84 | 82 | 78 | 85 | **92** |
| **PocketSphinx** | 80 | 35 | 45 | **95** | 50 | 75 |
| **Coqui/DeepSpeech** | 72 | 58 | 60 | 70 | 75 | 60 |
| **wav2vec2 / XLS-R** | 55 | 80 | 78 | 40 | **95** (low-resource) | 50 |
| **HuBERT** | 55 | 82 | 80 | 40 | 85 | 45 |
| **SpeechBrain** | 65 | 80 | 80 | 55 | **90** | 70 |
| **ESPnet** | 65 | 82 | 82 | 55 | **90** | 75 |
| **NVIDIA NeMo (Parakeet)** | **99** | **92** | 85 | 55 | 85 | 80 |
| **NeMo Canary-1B-v2** | 90 | **90** | 86 | 45 | 82 | 40 |
| **Whisper large-v3** | 60 | 86 | **90** (very robust) | 35 | 70 | 30 |
| **faster-whisper / whisper.cpp** | 78 | 86 | 90 | 60 | 65 | 40 |
| **distil-whisper** | 82 | 82 | 86 | 65 | 60 | 35 |
| **Moonshine v2 (Medium)** | **95** | 84 | 80 | **88** | 70 | **90** |
| **Nemotron streaming 0.6B** | 90 | 80 | 80 | **85** | 78 | **92** |
| **Silero** | 85 | 72 | 75 | 85 | 40 | 88 |

**How to read it:** if you need *raw accuracy + throughput on a GPU*, Parakeet/Canary win. If you need *tiny-footprint streaming on a CPU/edge device*, Vosk, Moonshine v2, Nemotron-streaming, and sherpa-onnx win. If you need *custom low-resource-language training*, XLS-R/wav2vec2 + NeMo/SpeechBrain/ESPnet win. Whisper wins on *out-of-the-box multilingual robustness* but is the weakest at true low-latency streaming.

---

## 6. The ">99 % accuracy" (<1 % WER) question (deliverable 3)

**Verdict: not realistically achievable for general-purpose, multilingual, or streaming ASR in 2026.** **[V]** (synthesis, medium confidence)

Evidence:
- **Best aggregate leaderboard WER is 5.6–6.3 %** (Canary-Qwen-2.5B 5.63 %; Parakeet 6.32 %). None approach 1 % on multi-domain benchmarks. **[V]** (arXiv 2509.14128; fetched 2026-01-07)
- **"Human parity" milestones were ~5–6 % WER**, not <1 %: Microsoft 5.8 % / IBM 5.5 % on Switchboard, ~10–11 % on the harder CallHome — and parity itself is contested (human transcribers were measured at 5.9–11.3 %). **[V]** (arXiv 1610.05256, 1703.02136; AI Magazine "Human Parity?" case study)
- The **research record on Switchboard is ~2.3 %** WER after corrected scoring — still above 1 %, and on a single clean benchmark. **[V]** (fetched, 2022)
- Practitioner guidance: **real-world accuracy is 95–98 %** on clean audio; **even trained humans reach only 98–99 %**. **[V]** (fetched 2026-05-26)

**When <1 % WER *is* approachable:** narrow/constrained vocabulary (e.g., digit strings, command grammars), pristine studio audio, a single well-resourced language, in-domain adaptation, and a coupled/rescoring language model — and typically in **batch**, not streaming. For an open-vocabulary, conversational, multilingual, low-latency system, treat 5–10 % WER as the realistic target and >99 % as a marketing figure rather than an engineering spec.

WER definition for reference: **WER = (S + D + I) / N** (substitutions + deletions + insertions over reference words; word-level Levenshtein distance). For Chinese/Japanese, **CER** (character error rate) is used instead. **[V]** (Wikipedia/WER; PMC12431075)

---

## 7. State of the art, 2026 (deliverable 4)

1. **Accuracy leaders (batch, GPU):** Speech-augmented LLMs — **Canary-Qwen-2.5B** (FastConformer + Qwen3-1.7B decoder, 5.63 % WER), IBM **Granite-Speech-3.3-8B**, Microsoft **Phi-4-Multimodal** — top the Open ASR Leaderboard, with **Parakeet-TDT-0.6B-v3** (6.32 %) the best *efficiency-adjusted* model. **[V]** (fetched 2025-11-21, 2026-01-07; arXiv 2509.14128)
2. **On-device streaming leaders (CPU, <1 GB):** **Nemotron streaming 0.6B** (8.20 % WER int4, 0.56 s latency, RTFx 7.2×) and **Moonshine v2** (33–245 M params, 258 ms latency, bounded TTFT via ergodic/sliding-window attention). **[V]** (arXiv 2604.14493, 2602.12241)
3. **Low-resource / language-independent leader:** **XLS-R / wav2vec2** SSL pretraining (128 langs, 436 k h) + CTC fine-tuning; **LoRA + INT8** for edge adaptation (1.6 % of weights, 60 MB checkpoint). **[V]** (HF XLS-R; PMC12431075)
4. **Efficiency technique of the year:** **knowledge distillation** (distil-whisper, ASKD/FastWhisper-large at 6.37 % WER on 1,634 h vs Whisper's 680,000 h) — though its headline 5× speedup and "beats teacher" claims **failed verification**, so treat distillation's *accuracy retention* as solid and its *efficiency superiority* claims skeptically. **[V medium]** (arXiv 2601.19919)
5. **Best all-round open training stack:** **NVIDIA NeMo** (FastConformer + CTC/TDT/AED, cache-aware streaming, strong multilingual recipes); **ESPnet** and **SpeechBrain** are the most research-flexible; **sherpa-onnx (Next-gen Kaldi)** is the best modern streaming-on-edge deployment runtime.

---

## 8. Decision guide & recommendations

| If your priority is… | Use | Why |
|---|---|---|
| **Tiny CPU/edge footprint + streaming, fully offline** | **Vosk** or **sherpa-onnx**; **Moonshine v2 Tiny/Small** | 50 MB–250 MB models, native streaming, faster-than-real-time on CPU |
| **Lowest-latency streaming with strong accuracy** | **Nemotron streaming 0.6B** (int4) | 0.56 s latency, 8.2 % WER, 0.67 GB, RTFx >7 on CPU |
| **Best accuracy, have a GPU, batch OK** | **Parakeet-TDT-0.6B-v3** / **Canary-1B-v2** | 6.3–7.2 % WER, far faster than Whisper |
| **Out-of-the-box multilingual robustness** | **faster-whisper / whisper.cpp** (large-v3 or turbo) | 90+ languages, robust to noise/accent, easy local deploy |
| **Train a brand-new / low-resource language** | **wav2vec2 / XLS-R** fine-tuning (via HF, NeMo, SpeechBrain, or ESPnet) | ~4 h labeled audio → usable model; SSL does the heavy lifting |
| **Adapt a model on a constrained edge device** | **Whisper-tiny + LoRA + INT8** | Updates 1.6 % of weights; 60 MB deployable checkpoint |
| **Maximum control / classic hybrid pipeline** | **Kaldi** (or Next-gen Kaldi k2/icefall) | Full WFST/lexicon/LM control, mature recipes |

**For the brief's exact intersection** — open-source, local, low-resource, trainable, language-independent, near-real-time — the strongest single recommendation is a **two-track stack**: **(a)** train/fine-tune with **wav2vec2/XLS-R or NeMo FastConformer-RNN-T** on your target language, then **(b)** deploy the streaming checkpoint via **sherpa-onnx** or as a **quantized Nemotron/Moonshine-class streaming model** for on-device, sub-second-latency inference. This satisfies every constraint except the unrealistic <1 % WER target.

---

## 9. Methodology, caveats, and provenance

**Method.** The research question was decomposed into 6 angles (library landscape; quantitative benchmarks; SOTA-2026/newcomers; algorithm evolution; edge/low-resource practice; <1 % WER feasibility). 7 parallel web searches → 22 sources fetched → 103 falsifiable claims extracted → top 25 verified by **3 independent adversarial voters each** (a claim dies on ≥2/3 refutations). **23 confirmed, 2 killed.** One search angle (broad/primary library landscape) failed mid-response (API connection closed) in the first run.

**Follow-up run (library-landscape re-do).** Because the classic toolkits were missing from the verified set, a second focused harness was run: 4 angles (Kaldi-family, lightweight-edge, CTC-era, research-toolkits) → 26 unique URLs → 14 fetched → **19 claims verified by 3-vote, 19 confirmed, 0 killed**, drawing on official docs (Vosk, Julius, DeepSpeech/Coqui, Kaldi PR, sherpa-onnx), peer-reviewed papers (PocketSphinx ICASSP 2006, pruned-RNN-T Interspeech 2022), and an embedded-ASR benchmark. This upgraded Kaldi, Vosk, Julius, Coqui/DeepSpeech, wav2letter, PocketSphinx, and Next-gen Kaldi to **[V]/[V\*]**.

**Second follow-up run (research toolkits).** A third focused harness targeted the remaining three: 3 angles (ESPnet, SpeechBrain, Silero) → 18 URLs → 12 fetched (per-library balanced) → **35 claims verified by 3-vote, 35 confirmed, 0 killed**, drawing on peer-reviewed papers (ESPnet 1804.00015, Branchformer 2207.02971, E-Branchformer 2305.11073), official HuggingFace model cards + ReadTheDocs (SpeechBrain), and PyTorch Hub / GitHub / The Gradient (Silero). With this, **every named library now carries a citation** and no entry relies on background.

**Killed / refuted claims (do not rely on):**
- ❌ "ASKD/FastWhisper-large delivers a **5× latency speedup** (659 ms → 132 ms)" — **refuted 1–2** (model-name mismatch; no GPU/batch/decoding config stated; single preprint). (arXiv 2601.19919)
- ❌ "Distilled student **surpasses its teacher** by 1.07 % WER on noisy/financial data while 5× faster" — **refuted 0–3** (the 1.07 % win is one cherry-picked dataset; student loses on LibriSpeech test-other). (arXiv 2601.19919)

**Key caveats:**
- **Preprint risk.** The strongest 2026 results (Nemotron-0.6B, Moonshine v2, ASKD/FastWhisper) are **non-peer-reviewed arXiv preprints (Feb–Apr 2026)** with author/vendor self-reported, best-case benchmarks.
- **Vendor selection bias.** NVIDIA Canary/Parakeet figures are vendor-reported (though independently reproduced on the leaderboard).
- **Not apples-to-apples.** WER/CER, RTFx, and latency depend heavily on test set, decoding (LM or not), hardware (CPU vs A100 vs L4), batch size, and streaming chunk config. Cross-table comparisons are directional.
- **0–100 scores are analyst judgments**, anchored to SI evidence but not themselves measured.
- **Coverage gap (closed).** All three follow-up-targeted toolkits now carry citations; **no library in this report relies on [B] background.** Note, however, that WER figures are drawn from *heterogeneous* sources and eras: the classic-system numbers (DeepSpeech 5.97 %, wav2letter 7.2 %, Silero V1 6.9 %) and the toolkit LibriSpeech numbers (ESPnet/SpeechBrain ~1.9–2.7 % test-clean) are **not** directly comparable to each other or to the 2026 multi-domain leaderboard averages in §4a — different test sets, decoding, and LM use. Treat each within its own row.

**Open questions worth a follow-up run:**
1. Same-metric head-to-head of the classic trainable toolkits (Kaldi/k2/icefall/sherpa, Vosk, ESPnet, SpeechBrain, Coqui) on latency/RTF/size/RAM/WER.
2. Precise **inference RAM** (not just disk size) for the on-device models (Nemotron-0.6B, Moonshine v2, LoRA-INT8 Whisper-tiny).
3. Independent third-party reproduction of the 2026 preprint latency claims (Moonshine's 43.7×, Nemotron's RTFx) on standardized hardware.
4. Concrete real-world conditions (acoustic env, language, domain, LM rescoring) under which any system has actually measured <1 % WER, and whether that is ever achievable in streaming.

---

## 10. Sources

**Primary (peer-reviewed or original papers):**
- arXiv 2510.12827 — *Automatic Speech Recognition in the Modern Era: Architectures, Training, and Evaluation* (2025)
- IEEE/ACM TASLP 2023, doi:10.1109/TASLP.2023.3328283 — *End-to-End Speech Recognition: A Survey*
- IEEE/ACM TASLP 2021, doi:10.1109/TASLP.2021.3122291 / arXiv 2106.07447 — *HuBERT*
- arXiv 2212.04356 — *Whisper: Robust Speech Recognition via Large-Scale Weak Supervision* (Radford et al.)
- arXiv 2509.14128 — NVIDIA *Canary / Parakeet* (FastConformer, TDT) leaderboard paper (Sept 2025)
- arXiv 2604.14493 — *Pushing the Limits of On-Device Streaming ASR* (Nemotron-0.6B, Apr 2026)
- arXiv 2602.12241 — *Moonshine v2: Ergodic Streaming Encoder ASR* (Feb 2026)
- arXiv 2601.19919 — *FastWhisper / ASKD-Whisper: Adaptive Self-Knowledge Distillation* (Jan 2026) — **headline WER only; efficiency claims refuted**
- arXiv 1610.05256 (Microsoft human parity), 1703.02136 (IBM 5.5 %), 2206.06192 (Switchboard record)
- PMC12431075 — *LoRA-INT8 Whisper: Low-Cost Cantonese ASR for Edge Devices*
- HuggingFace blog *Fine-Tune XLSR-Wav2Vec2 for low-resource ASR*; model card *facebook/wav2vec2-xls-r-300m*

**Classic-toolkit sources (follow-up run, [V]/[V\*]):**
- alphacephei.com/vosk + /vosk/models + github.com/alphacep/vosk-api — Vosk (size, RAM, streaming, offline)
- github.com/julius-speech/julius — Julius (HMM + N-gram, 2-pass tree-trellis, <32 MB)
- deepspeech.readthedocs.io (DeepSpeech 0.9.3) + stt.readthedocs.io (Coqui STT 1.4.0) — RNN+CTC, KenLM 5-gram, 16 kHz
- vivoka.com/benchmark-comparison-embedded-asr — DeepSpeech 5.97 % WER, wav2letter LER 6.9 / WER 7.2
- github.com/kaldi-asr/kaldi PR #781 — Kaldi TDNN + chain LF-MMI
- k2-fsa.github.io/sherpa + HF csukuangfj/sherpa-onnx-streaming-zipformer-en-20M — Next-gen Kaldi streaming Zipformer-transducer
- arXiv 2206.13236 — Pruned RNN-T (k2/icefall, Conformer + stateless decoder), Interspeech 2022
- cs.cmu.edu/~dhuggins PocketSphinx — semi-continuous GMM-HMM (ICASSP 2006)

**Research-toolkit sources (third run, [V]):**
- arXiv 1804.00015 — ESPnet (hybrid CTC/attention, BLSTM/location-aware attention)
- arXiv 2207.02971 — Branchformer (cgMLP + attention); LS-960 2.4 % / 5.5 %
- arXiv 2305.11073 — E-Branchformer vs Conformer in ESPnet2; LS-100 6.3 % / 17.0 %
- espnet.github.io ContextualBlockConformerEncoder — ESPnet streaming
- HF speechbrain/asr-conformer-transformerlm-librispeech (2.01 % / 4.52 %), asr-wav2vec2-librispeech (1.90 % / 3.96 %), asr-streaming-conformer-librispeech (3.10 % @1280 ms); speechbrain.readthedocs.io streaming-Conformer tutorial
- pytorch.org/hub/snakers4_silero-models_stt; github.com/snakers4/silero-models (sizes 25–500 MB, V5 5.5 % / 13.5 %); thegradient.pub "ImageNet moment for STT"; deepwiki silero-models

**Secondary / practitioner:**
- HuggingFace *Open ASR Leaderboard* blog; HF model cards (parakeet-tdt-0.6b-v3, nemotron-speech-streaming-en-0.6b, Moonshine)
- Northflank, e2enetworks, onresonant, openwhispr, assemblyai, convertaudiototext blogs (benchmarks, edge deployment, accuracy expectations)
- AI Magazine — *Is AI at Human Parity Yet? A Case Study on Speech Recognition*
- Wikipedia — *Word Error Rate*

*Report generated by three deep-research workflow runs — primary (105 agents, 22 sources, 25 verified / 23 confirmed) + library-landscape re-run (75 agents, 14 sources, 19 verified / 19 confirmed) + research-toolkits re-run (120 agents, 12 sources, 35 verified / 35 confirmed) — with analyst synthesis. 300 agents total; 77 claims adversarially 3-vote verified; every named library carries a citation ([V]/[V\*]). Field is fast-moving — re-verify preprint numbers before relying on them.*
