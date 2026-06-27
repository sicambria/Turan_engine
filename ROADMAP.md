# Turán RMS — Bug & Remediation Roadmap

**Created:** 2026-06-27
**Scope:** All defects found during the architecture/algorithm audit of *Turán RMS v1.1*.
**Companion doc:** `reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md`

Items are ranked by severity. "Live" = on a code path actually reached at runtime (verified by caller grep). "Dead/latent" = currently unreachable but wrong if enabled. Each item lists **location → problem → impact → fix**.

Legend: 🔴 P0 critical (breaks accuracy on a live path) · 🟠 P1 high · 🟡 P2 medium · ⚪ P3 hygiene/tech-debt.

---

## Status (2026-06-28)

Fixes were planned, peer- and adversarially-reviewed, implemented by parallel agents in three file-disjoint groups + one atomic serialization step, and committed in logical chunks on branch `fix/roadmap-bugs`. **All fixes are code-review-verified, NOT compiler-verified** — there is no .NET/mono/msbuild toolchain in this environment, so nothing was built or run. See `plans/PROGRESS.md` for the full record (errors, insights, learnings) and `plans/BUG-*.md` for per-bug plans + reviews.

| Bug | Status | Where committed |
|---|---|---|
| BUG-01 HTK Δ/ΔΔ/ΔΔΔ summed away | ✅ Fixed | `fix(asr-core)` |
| BUG-02 pre-emphasis no-op | ✅ Fixed | `fix(dsp)` |
| BUG-03 native "MFCC" is log-mel (no DCT) | ◐ Partially — **label-only** (naming clarified); accuracy half (true DCT / Mahalanobis DTW) **still open** | `fix(dsp)` |
| BUG-04 DCT `Sqrt(2/24)`→0 | ✅ Fixed | `fix(dsp)` |
| BUG-05 `fir_filter` aliases input / IIR | ✅ Fixed | `fix(dsp)` |
| BUG-06 `hamming_ablak` off-by-one | ✅ Fixed | `fix(dsp)` |
| BUG-07 empty catch blocks | ✅ Fixed | `fix(dsp)` |
| BUG-08 `costRecord[120]` cap | ✅ Fixed | `fix(asr-core)` |
| BUG-09 Itakura magic-13 order | ✅ Fixed | `fix(asr-core)` |
| BUG-10 no rejection/FAR control | ◐ **Mechanism** shipped (REJECTED=-2 + threshold, **inert**); **calibration deferred** (needs runtime cost data) | `fix(asr-core)` |
| BUG-11 `MatchLength` stub | ✅ Fixed (frame-count duration match) | `fix(asr-core)` |
| BUG-12 `BinaryFormatter` | ✅ Fixed — versioned **TRMS** format + `featVersion` staleness marker + legacy read fallback | `fix(asr-core)` (reader) + `fix(dsp)` (writer) |
| BUG-13 HTK process integration | ⏸ **Deferred** — regression-risk + needs Windows/HCopy runtime test (uncompilable here) |
| BUG-14 de-duplicate triplicated files | ⏸ **Deferred** — broad refactor, conflicts with all parallel work, needs a build |
| BUG-15 dead Java-port `mfcc.cs` | ✅ Done — **finished & quarantined** (per user) to `reference/unused-native-mfcc/`, not deleted | `feat(mfcc)` |
| BUG-16 dead `hasonlit` DTW | ✅ Fixed (deleted) | `fix(dsp)` |

> ⚠️ **Two follow-ups gate production use:** (1) **BUG-02 changes native `.mfcc` feature content** → all native templates must be regenerated (the new `featVersion` flags stale ones). (2) **BUG-10 ships with rejection inert** and the old accidental cost cap removed → the build performs **no input rejection** until BUG-10's threshold is calibrated on runtime data; for the assistive/medical command use case, do not treat the pre-calibration build as production-ready.

---

## P0 — Critical correctness (live recognition paths)

### 🔴 BUG-01 — HTK dynamic features (Δ/ΔΔ/ΔΔΔ) are summed away
- **Where:** `Turan_core/Turan_core/HTK_Interface.cs:85-103` (`ReadMFCC_D_A_T`); duplicated in `Turan_tester/Turan_tester/HTK_Interface.cs` and `Turan_creator/Turan_creator/HTK_Interface.cs`.
- **Live:** Yes — default README-recommended path (`Engine(EngineMode.mfcc, VectorFileFormat.htk)`), called from `Engine.cs:123,128`, used by `Turan_tester/Form1.cs:106`.
- **Problem:** HTK produces a 60-dim `MFCC_D_A_T` frame (15 static + 15 Δ + 15 ΔΔ + 15 ΔΔΔ). The reader uses `+=` to **element-wise sum all four streams into one 15-dim vector** instead of concatenating them into 60 dims.
- **Impact:** The most discriminative features (the dynamics) are destroyed by addition. Recognition runs on smeared static cepstra only. **Major accuracy loss on the primary path.**
- **Fix:** Allocate `double[nSamples, 4*num_of_feature_vectors]` and write each stream to its own column block (offsets 0/15/30/45). Update `Engine.mfcc_lpc_vect_num` / `dtwApp_match` vector width to 60. Add a unit test that reads a known HTK `.mfc` and asserts 60 distinct columns.

### 🔴 BUG-02 — Pre-emphasis is a no-op on the native MFCC path
- **Where:** `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:194-214` (`win_fir_hamming`); duplicated in `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`.
- **Live:** Yes — native MFCC path, called from `Creator.cs:158`.
- **Problem:** The pre-emphasis lines (204/208) read from `tmparray`, which is **zero-initialized** and never holds PCM, then line 210 **overwrites** the cell with `win_pcmdata * hamming`. The pre-emphasis computation is discarded; only Hamming windowing survives.
- **Impact:** No high-frequency pre-emphasis on native MFCC features → weaker high-formant energy, degraded features. Silent (no error).
- **Fix:** Compute pre-emphasis from the **input** (`win_pcmdata`) into a temp, then apply Hamming to the pre-emphasized value:
  `pe = win_pcmdata[f,i] - 0.95*win_pcmdata[f, i-1]; tmparray[f,i] = pe * hamming(i);` (handle `i==0` with previous-frame tail or 0).

### 🔴 BUG-03 — Native "MFCC" mode never applies the DCT (emits log-mel, mislabeled)
- **Where:** DCT lives in `H_FELDOLGOZO.mfccszamitas` (`Turan_creator/.../H_FELDOLGOZO.cs:218+`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198+`) but **has no caller** (verified). Live native path (`Creator.cs:152-167`) ends at `CV_FELDOLGOZO.Window_Mel_Scale_Reduction` and serializes that as `.mfcc`.
- **Live:** Yes (the omission) — native `mfcc` mode.
- **Problem:** Output labeled "MFCC" is actually **log-mel filterbank energies** (no cepstral DCT). Dimensions stay highly correlated.
- **Impact:** Plain Euclidean DTW on correlated features is suboptimal; also a naming trap for future maintainers. (Workable, but weaker than true MFCC.)
- **Fix:** Either (a) wire in a **correct** DCT step after mel reduction (see BUG-04 first), or (b) explicitly rename the feature to `logmel` and switch DTW to a normalized/Mahalanobis distance suited to correlated features. Decide one; document it.

---

## P1 — High (correctness; currently dead but wrong, or live data-corruption)

### 🟠 BUG-04 — DCT scale factor collapses to zero (integer division)
- **Where:** `H_FELDOLGOZO.mfccszamitas` — `mfccarr[...] *= Math.Sqrt(2/24)` (`Turan_creator/.../H_FELDOLGOZO.cs:248`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:~224`).
- **Live:** No (dead code — see BUG-03), but **blocks** any fix that enables the native DCT.
- **Problem:** `2/24` is **integer division → 0**, so `Math.Sqrt(0) = 0`; the whole MFCC array is multiplied by 0.
- **Impact:** If the DCT path is ever enabled, every coefficient becomes 0 → total failure.
- **Fix:** Use `Math.Sqrt(2.0/24.0)`. Audit the whole file for other integer-division-in-double-context errors.

### 🟠 BUG-05 — `fir_filter` aliases its input and is recursive (IIR), not FIR
- **Where:** `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:142-152` (and LITE copy).
- **Live:** Partial — `fir_filter` is the standalone variant; the live native path uses `win_fir_hamming` (BUG-02). Flag regardless; remove or fix to avoid future misuse.
- **Problem:** (a) `tmparray = pcmdata` discards the freshly-allocated buffer and **aliases the caller's array**, mutating input in place; (b) the loop reads the **already-filtered** `tmparray[i-1]`, making it a recursive filter, not `y[i]=x[i]-0.95·x[i-1]`.
- **Impact:** Caller's PCM buffer corrupted; filter response wrong.
- **Fix:** Write into the new buffer reading from the **original** input: `out[i] = pcmdata[i] - 0.95*pcmdata[i-1];`. Never reassign the allocated array to the parameter.

### 🟠 BUG-06 — `hamming_ablak` drops the last sample (off-by-one)
- **Where:** `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:165` — `for (i = 0; i < firdata.Length - 1; i++)`.
- **Problem:** The final sample is never written → left as 0. (`win_hamming_ablak` is correct.)
- **Impact:** Minor edge artifact per frame; compounds with overlap.
- **Fix:** `i < firdata.Length`.

---

## P2 — Medium (robustness, limits, silent failures)

### 🟡 BUG-07 — Empty catch blocks swallow FFT/LPC failures
- **Where:** `Turan_creator/Turan_creator/Creator.cs:251-254` (FFT per frame) and `:292-295` (LPC per frame).
- **Problem:** Exceptions are caught and ignored; on failure the frame silently keeps zeros/garbage.
- **Impact:** Corrupted feature frames pass downstream undetected → degraded templates with no signal to the user.
- **Fix:** Log + propagate, or substitute a defined fallback and count failures. Never `catch {}` silently.

### 🟡 BUG-08 — Hard-coded array caps limit utterance length / template count
- **Where:** `dtwApp_match.cs:147,164` `costRecord = new double[120]`; `H_FELDOLGOZO` `tavtomb/ertomb = new double[256, …]` and `[256]` loop bounds (`hasonlit`, dead but illustrative); 256-frame window assumptions throughout.
- **Problem:** Magic fixed sizes (120, 256) cap how long an utterance or how many templates can be processed; some are mismatched to actual data sizes.
- **Impact:** Longer commands or larger template sets risk index-out-of-range or truncation.
- **Fix:** Size all working arrays from actual frame/template counts; remove magic literals.

### 🟡 BUG-09 — Itakura distance hard-codes order 13 (`magic13`)
- **Where:** `dtwApp_match.cs:255-352` (`ITDDistance`), `int magic13 = 13;`.
- **Problem:** LPC order is configurable (12), but the Itakura distortion assumes 13 throughout; mismatched with `num_of_vectoritems`/`mfcc_lpc_vect_num`.
- **Impact:** Itakura metric inconsistent with the actual LPC vector width → wrong distances when LPC order ≠ 13.
- **Fix:** Derive the order from `Engine.mfcc_lpc_vect_num`; remove the literal.

### 🟡 BUG-10 — No confidence threshold / rejection (no FAR control)
- **Where:** `Engine.RecognizeAndReturnIndex` / `dtwApp_match.bestMatch` — always returns the `argmin` template.
- **Problem:** Every input is forced to the nearest command; there is no "unknown/reject" outcome.
- **Impact:** Out-of-vocabulary speech and noise are mapped to a real command → uncontrolled false-accept rate. Critical for an assistive/medical command system.
- **Fix:** Add a normalized-cost threshold and an explicit reject result; expose FRR/FAR-per-hour tuning.

### 🟡 BUG-11 — `Engine.MatchLength` is a stub
- **Where:** `Turan_core/Turan_core/Engine.cs:148-155` — returns `-1` unconditionally.
- **Problem:** Declared duration-matching feature is unimplemented; callers may assume it works.
- **Impact:** Dead feature; misleading API surface.
- **Fix:** Implement or remove; document status.

---

## P3 — Hygiene / tech debt (portability, security, maintainability)

### ⚪ BUG-12 — `BinaryFormatter` for template serialization
- **Where:** `Creator.cs:318-332` (`SerializeArray`), `Engine.cs:162-179` (`DeSerializeArray`).
- **Problem:** `BinaryFormatter` is **deprecated and insecure** (RCE risk on untrusted input) and removed in modern .NET.
- **Fix:** Replace with a simple length-prefixed binary writer, `System.Text.Json`, or a vetted format; blocks any .NET-Core/5+ migration.

### ⚪ BUG-13 — HTK integration hard-codes path and shells out per call
- **Where:** `HTK_Interface.cs:15` `htk_cmd_dir = "\\htk\\"`; `CreateMFCC_D_A_T` spawns `HCopy.exe` per file.
- **Problem:** Windows-only absolute path; process spawn per recognition is slow and fragile; no error capture from HCopy.
- **Fix:** Make path configurable; batch the SCP; capture stderr/exit code; consider replacing HTK with the (fixed) native extractor to drop the external dependency.

### ⚪ BUG-14 — Duplicated DSP/DTW code across modules
- **Where:** `Turan_core` vs `Felismero_motor_LITE` vs `Turan_creator` each carry their own `H_FELDOLGOZO`, `dtwApp_match`, `HTK_Interface`, `mfcc.cs`.
- **Problem:** Fixes (BUG-01..06) must be applied in 2–3 places; copies will drift.
- **Fix:** Extract a single shared DSP/recognition library referenced by all front ends.

### ⚪ BUG-15 — `mfcc.cs` is non-compiling Java-port scaffolding
- **Where:** `Felismero_motor_LITE/.../mfcc.cs`, `Turan_creator/.../? ` — uses Java idioms (`this(...)` ctor chaining, `IllegalArgumentException`, `double[][]` vs `double[,]`, `Vector<>`), not valid C#.
- **Problem:** Dead, non-building reference port left in tree; misleads readers about the active code.
- **Fix:** Remove or move to a clearly-marked `reference/` folder.

### ⚪ BUG-16 — Dead/commented native DTW (`hasonlit`) retained
- **Where:** `H_FELDOLGOZO.hasonlit` (active DTW is `dtwApp_match`); call sites commented out in `Felismero_motor_LITE/Form1.cs:1472-1505`.
- **Problem:** Two DTW implementations, one dead, cause confusion.
- **Fix:** Delete the dead path or document why it's kept.

---

## Suggested execution order

1. **BUG-01, BUG-02** — restore the two live feature defects (biggest accuracy wins, low effort).
2. **BUG-03 + BUG-04** — decide log-mel vs true-MFCC and make it correct.
3. **BUG-10** — add rejection/confidence (safety-critical for the command use case).
4. **BUG-05, BUG-06, BUG-07, BUG-09** — remaining correctness/robustness.
5. **BUG-08, BUG-11** — limits & stubs.
6. **BUG-12 … BUG-16** — consolidate, de-dup, modernize (also unblocks .NET migration).

> **Reality check.** Fixing every item above lifts the *as-built* engine from ~44/100 toward the ~58/100 ceiling of this DTW-template architecture (see companion report, Part B). It will **not** reach ≥99 % accuracy or solve voice-drift — those require the architectural move described in the companion report's Part E (self-trained multilingual backbone → closed-set command classifier). Treat this roadmap as *stabilize the bridge*, not *the destination*.
