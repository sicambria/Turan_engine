# BUG-07 — Empty catch blocks swallow per-frame FFT/LPC failures

**Severity:** P2 (robustness / silent failure)
**Primary file (ROADMAP scope):** `Turan_creator/Turan_creator/Creator.cs`
**Additional duplicated copy (beyond ROADMAP scope, see §3):** `Felismero_motor_LITE/Felismero_motor/Form1.cs`
**Compiler available?** No — verification is by code reading/tracing only (see §6).

---

## 1. Root cause (restated from the actual code)

Two per-frame DSP loops wrap the single failing call in a `try` whose `catch` body is
empty, so any exception is discarded and the frame proceeds with whatever happens to be
in the buffer.

`Turan_creator/Turan_creator/Creator.cs`:

- `FFTCalcFrameByFrame` (method 231–266). Per-frame call at line 249:
  ```csharp
  try
  {
      temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
  }
  catch (Exception)
  {

  }                                   // lines 251–254 — SILENT
  ```
  On failure `temp_frame_line` retains the *pre-FFT windowed PCM* (not a spectrum), and
  the write-back loop (257–260) copies that into `win_fftdata`. Downstream
  `CV_FELDOLGOZO.Window_Mel_Scale_Reduction` (Creator.cs:166) then builds mel/`.mfcc`
  features from non-spectral garbage with **no signal to the caller**.

- `LPCCalcFrameByFrame` (method 270–308). Per-frame call at line 290:
  ```csharp
  try
  {
      Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
  }
  catch (Exception)
  {

  }                                   // lines 292–295 — SILENT
  ```
  On failure `lpc_frame_line` stays all-zeros (freshly allocated at 279) and the
  write-back loop (299–302) emits a zero LPC vector. A corrupted/zero frame silently
  enters the `.lpc` template.

**Net impact:** corrupted feature frames pass into templates and recognition with no
diagnostic, degrading accuracy invisibly. The existing code *already* signals other
failures by `throw` (IOException rethrows at 184/195, HTK rethrow at 225); these two
catch blocks are the inconsistent silent path.

**Orthogonal observation (do NOT fix here — belongs to BUG-08 array-sizing):**
`FftAlgorithm.Calculate` (`FftCalc.cs:28`) returns an array of length
`1 << bitsInLength`, which is *shorter than the input* whenever
`num_items_in_windowed_frame` is not a power of two. The write-back loop at 257–260
reads `temp_frame_line[frame_item]` up to `num_items_in_windowed_frame` and lives
*outside* the `try`, so that mismatch would throw `IndexOutOfRangeException` the catch
never sees. This is a separate failure path; BUG-07 must not change `win_fftdata`'s
width or the write-back bounds. The chosen fallback (§2) deliberately keeps the
fallback array at `num_items_in_windowed_frame` length so it never *introduces* this
mismatch.

---

## 2. Exact change per file/line (before / after)

**Design (per advisor):** the requirement is "a signal that reaches a human."
`Console.Error`/`Debug.WriteLine` from a class library that a WinForms app consumes is
effectively invisible and would NOT satisfy the roadmap. Therefore:

- Per frame: replace the empty catch with **(a)** a deterministic zero fallback,
  **(b)** a *local* (non-static) failure counter, **(c)** capture of the first
  exception.
- After the loop: if any frame failed, **surface it** — by `throw` in the library
  (`Creator.cs`, in-style with its existing throws, caught and shown via `MessageBox`
  by `Turan_tester/Form1.cs:183-185`) and by `MessageBox.Show` in the GUI copy
  (`Felismero_motor_LITE/Form1.cs`, a `Form` instance method).

The counter MUST be a method-local, not a static field: `Creator` is fully static and
`CalculateFeatureVectors` loops `foreach` over files, so a static counter would
accumulate across files. Per-call local → accurate per-file report.

### 2a. `Turan_creator/Turan_creator/Creator.cs` — `FFTCalcFrameByFrame`

**Before (lines 246–254):**
```csharp
                // FFT per frame
                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception)
                {

                }
```
**After:**
```csharp
                // FFT per frame
                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception ex)
                {
                    // BUG-07: never swallow silently. Deterministic zero fallback
                    // (same width as expected), record the cause, surface after loop.
                    fft_failed_frames++;
                    if (first_fft_failure == null) first_fft_failure = ex;
                    temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];
                }
```

**Also add, immediately after line 233** (`win_fftdata = new double[...]`), the local
declarations:
```csharp
            int fft_failed_frames = 0;          // per-call (NOT static)
            Exception first_fft_failure = null;
```

**Also add, immediately before the `return win_fftdata;` (line 265):**
```csharp
            if (fft_failed_frames > 0)
            {
                throw new InvalidOperationException(
                    "FFT failed on " + fft_failed_frames + " of " + extended_num_of_frames
                    + " frame(s); MFCC features are unreliable. First error: "
                    + first_fft_failure.Message, first_fft_failure);
            }

```

### 2b. `Turan_creator/Turan_creator/Creator.cs` — `LPCCalcFrameByFrame`

**Before (lines 288–295):**
```csharp
                // FFT per frame

                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception)
                {

                }
```
**After:**
```csharp
                // LPC per frame

                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception ex)
                {
                    // BUG-07: never swallow silently. lpc_frame_line is freshly
                    // allocated zeros (deterministic fallback); record + surface.
                    lpc_failed_frames++;
                    if (first_lpc_failure == null) first_lpc_failure = ex;
                }
```

**Also add, immediately after line 272** (`win_lpcdata = new double[...]`):
```csharp
            int lpc_failed_frames = 0;          // per-call (NOT static)
            Exception first_lpc_failure = null;
```

**Also add, immediately before `return win_lpcdata;` (line 307):**
```csharp
            if (lpc_failed_frames > 0)
            {
                throw new InvalidOperationException(
                    "LPC failed on " + lpc_failed_frames + " of " + extended_num_of_frames
                    + " frame(s); LPC features are unreliable. First error: "
                    + first_lpc_failure.Message, first_lpc_failure);
            }

```

> Note: the stale `// FFT per frame` comment at line 286 is corrected to
> `// LPC per frame` in the snippet above — cosmetic, optional, but it is a copy-paste
> error in the LPC method.

---

## 3. Every duplicated copy that needs the same change

`grep` for `FftAlgorithm.Calculate` / `Lpc.lpc_from_data` shows one genuine structural
duplicate of these two methods:

`Felismero_motor_LITE/Felismero_motor/Form1.cs`
- `FFTCalc_FrameByFrame` (751–788), empty catch at **772–775**.
- `LPC_Calc_FrameByFrame` (792–830), empty catch at **814–817**.

**These are LIVE** — called at 682 (`win_fftdata = FFTCalc_FrameByFrame(...)`) and 675
(`win_lpcdata = LPC_Calc_FrameByFrame(...)`) inside `AnalyzeSignal(...)`. ROADMAP lists
only `Creator.cs`; the task requires fixing **every duplicated copy**, so this plan
extends ROADMAP's stated scope to include the LITE copy, and states so explicitly.

Because these are instance methods on a `Form` and `AnalyzeSignal` is not internally
wrapped in try/catch, the visible-signal mechanism here is `MessageBox.Show` (the GUI's
idiom — same as the form's other handlers), not `throw`.

**3a. LITE `FFTCalc_FrameByFrame` — before (768–775):**
```csharp
                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception)
                {

                }
```
**after:**
```csharp
                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception ex)
                {
                    fft_failed_frames++;
                    if (first_fft_failure == null) first_fft_failure = ex;
                    temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];
                }
```
Add after line 753 (`win_fftdata = new double[...]`):
```csharp
            int fft_failed_frames = 0;
            Exception first_fft_failure = null;
```
Add before `return win_fftdata;` (787):
```csharp
            if (fft_failed_frames > 0)
            {
                MessageBox.Show("FFT failed on " + fft_failed_frames + " of "
                    + extended_num_of_frames + " frame(s); MFCC features are unreliable. "
                    + "First error: " + first_fft_failure.Message);
            }

```

**3b. LITE `LPC_Calc_FrameByFrame` — before (810–817):**
```csharp
                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception)
                {

                }
```
**after:**
```csharp
                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception ex)
                {
                    lpc_failed_frames++;
                    if (first_lpc_failure == null) first_lpc_failure = ex;
                }
```
Add after line 794 (`win_lpcdata = new double[...]`):
```csharp
            int lpc_failed_frames = 0;
            Exception first_lpc_failure = null;
```
Add before `return win_lpcdata;` (829):
```csharp
            if (lpc_failed_frames > 0)
            {
                MessageBox.Show("LPC failed on " + lpc_failed_frames + " of "
                    + extended_num_of_frames + " frame(s); LPC features are unreliable. "
                    + "First error: " + first_lpc_failure.Message);
            }

```

> `System` (for `Exception`) and `System.Windows.Forms` (for `MessageBox`) are already
> in use in `Form1.cs`; `System` is already imported in `Creator.cs`. No new `using`s.

**No other copies.** `Turan_core` and `Turan_tester` do not reimplement these loops —
they call `Turan_creator.Creator.CalculateFeatureVectors`. Other empty-ish catches found
repo-wide (`fmod.cs`, `WAVFile.cs`, `MathLib.cs`, etc.) are unrelated to per-frame
FFT/LPC and are out of scope for BUG-07.

---

## 4. Backward compatibility / on-disk format impact

**No format change. No backward-incompatibility. No versioned read needed.**

- `win_fftdata`, `win_meldata`, `win_lpcdata` keep identical shapes
  (`[extended_num_of_frames, num_items_in_windowed_frame]` /
  `[..., mfcc_lpc_vect_num]`).
- `.lpc` / `.mfcc` / `.mfc3` file formats are untouched — `SerializeArray` is not
  modified.
- The only data-path difference occurs **on the already-broken failure path**: a failed
  FFT frame now yields explicit zeros instead of pre-FFT windowed PCM. On the *success*
  path (the normal case) output is byte-identical to before, so existing templates remain
  fully compatible and need no regeneration.
- Behavior change on failure: previously silent degraded output; now the run aborts with
  an exception (`Creator`) or shows a `MessageBox` (LITE) and, in `Creator`, no partial
  `.mfcc`/`.lpc` is written for that file (the throw happens before `SerializeArray` is
  reached at 191/180). This is the intended, minimal, justified change.

---

## 5. Shared contract another bug's fix depends on

- **BUG-12 (`Creator.SerializeArray` ↔ `Engine.DeSerializeArray`, BinaryFormatter):**
  BUG-07 is **independent** of that serialization contract — it touches neither
  `SerializeArray` nor the on-disk vector format, so the two fixes do not interact.
- **BUG-01 / BUG-08 (vector width = `num_items_in_windowed_frame` /
  `mfcc_lpc_vect_num`):** BUG-07 deliberately preserves these widths. The FFT fallback
  array is sized `num_items_in_windowed_frame` precisely so a later BUG-01/BUG-08 width
  change stays the single source of truth; do not hard-code a numeric length in the
  fallback. If BUG-08 later sizes `win_fftdata` from the actual returned spectrum length,
  the fallback expression `new double[H_FELDOLGOZO.num_items_in_windowed_frame]` must be
  updated in lockstep — but that is a BUG-08 concern, flagged here only for coordination.
- **BUG-14 (de-duplication):** the `Creator.cs` and LITE `Form1.cs` copies must receive
  semantically identical fixes so they don't drift before BUG-14 merges them into one
  shared library. The only intentional divergence is the surfacing mechanism
  (`throw` vs `MessageBox`), dictated by library-vs-Form context.

---

## 6. Self-verification without a compiler

Read/trace, per edited method:

1. **No empty catch remains.** Re-grep `catch` in both files; confirm every
   FFT/LPC catch body now has statements:
   `grep -n -A4 "FftAlgorithm.Calculate\|lpc_from_data" Creator.cs Form1.cs`.
2. **Fallback is deterministic.** FFT catch reassigns `temp_frame_line` to a fresh
   zero array of length `num_items_in_windowed_frame` → the unchanged write-back loop
   (bounds `< num_items_in_windowed_frame`) cannot read out of range on the failure
   path. LPC catch leaves `lpc_frame_line` at its freshly-allocated zeros → write-back
   bounds `< mfcc_lpc_vect_num` are safe. Trace both write-back loops by eye.
3. **Counter scope is local.** Confirm `fft_failed_frames` / `lpc_failed_frames` /
   `first_*_failure` are declared *inside* each method (after the `new double[...]`
   allocation), not as class fields → no cross-file accumulation across the
   `foreach (pcm_filepath ...)` loop in `CalculateFeatureVectors`.
4. **Signal reaches a human.** Trace `Creator` throw → `CalculateFeatureVectors`
   propagates (it does not catch around `FFTCalcFrameByFrame`/`LPCCalcFrameByFrame`) →
   caller `Turan_tester/Form1.cs:180` is wrapped in `try { ... } catch (Exception ex)
   { MessageBox.Show(ex.Message); }` (183–185) → visible. (Note: the other caller at
   `Turan_tester/Form1.cs:90` is *not* wrapped — a pre-existing caller gap, unchanged by
   this fix; worth a follow-up but out of BUG-07 scope.) For LITE, the `MessageBox.Show`
   is in-method, so it is unconditionally visible.
5. **Format untouched.** Confirm `SerializeArray` and the array allocations
   (`new double[extended_num_of_frames, ...]`) are not edited → on success, output is
   bit-identical; existing templates still load.
6. **Symmetry.** Diff the `Creator.cs` and LITE snippets side by side; confirm identical
   logic except the surfacing call (throw vs MessageBox), so the copies stay in sync for
   BUG-14.

---

## Documented alternative (if abort-on-failure proves too strict)

The chosen approach already implements BOTH roadmap options (defined fallback **and**
count). To switch to "robust continue + warning" instead of aborting, keep the per-frame
zero fallback and counters, but downgrade `Creator`'s `throw` to a non-fatal surfaced
warning (e.g. an out-param / event the caller renders). That requires a small public-API
addition and is therefore *not* the minimal change; recorded here only as the deliberate
fallback if the abort behavior is later judged too aggressive.
