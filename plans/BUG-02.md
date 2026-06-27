# BUG-02 — Pre-emphasis is a no-op in `win_fir_hamming` (native MFCC path)

**Severity:** P0 (critical, live recognition path)
**Status:** PLAN ONLY — no source edited.

---

## 1. Root cause (restated from the code)

`H_FELDOLGOZO.win_fir_hamming(double[,] win_pcmdata, int num_of_frames)` is the
"ALL-IN-1" routine meant to apply **pre-emphasis FIR** *then* **Hamming window**
to each framed PCM block. The current body (identical in both copies):

```csharp
double[,] tmparray = new double[num_of_frames, num_items_in_windowed_frame];

for (int frame = 0; frame < num_of_frames; frame++)
{
    for (int frame_item = 0; frame_item < num_items_in_windowed_frame; frame_item++)
    {
        if (frame_item == 0)
        {
            tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, 0]);
        }
        else
        {
            tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, frame_item - 1]);
        }
        tmparray[frame, frame_item] = (win_pcmdata[frame, frame_item] * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame))));
    }
}
```

Two compounding defects:

1. **Pre-emphasis reads from the wrong (empty) buffer.** The pre-emphasis lines
   read `tmparray[...]`, the freshly allocated **zero-initialized output** array,
   not the input `win_pcmdata`. So `tmparray[frame, i] = 0 - 0.95*0 = 0` for every
   cell at that moment. The PCM signal never enters the pre-emphasis arithmetic.
2. **The result is then overwritten.** The final line unconditionally assigns
   `tmparray[frame, i] = win_pcmdata[frame, i] * hamming(i)`, discarding whatever
   the pre-emphasis block produced.

Net effect: pre-emphasis is a **no-op**; the function only applies the Hamming
window — it is functionally identical to `win_hamming_ablak`. The intended
filter `y[i] = x[i] − 0.95·x[i−1]` is never computed.

**Why it matters:** without pre-emphasis the high-frequency / high-formant energy
is not boosted before FFT and mel reduction, so the native MFCC (really log-mel,
see BUG-03) features carry weaker high-band detail → degraded, silently-wrong
features on a live path.

### Live-path confirmation (caller grep)
- **Creator (template generation):** `Turan_creator/Turan_creator/Creator.cs:158`
  → `win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);`
  feeds `FFTCalcFrameByFrame` → `Window_Mel_Scale_Reduction` → serialized as `.mfcc`.
- **Engine LITE (live recognition extraction):**
  `Felismero_motor_LITE/Felismero_motor/Form1.cs:680`
  → same `win_fir_hamming` call, same downstream chain, `.mfcc`.

Both the template builder and the runtime feature extractor share this exact code
(two copies, byte-identical), so the bug is symmetric — both sides currently skip
pre-emphasis, which is why the system "works" despite the defect.

---

## 2. Exact change (before/after)

Replace the inner-loop body so pre-emphasis is computed **from the input**
`win_pcmdata` into a local, then Hamming-weighted. Frame-local convention:
`y[0] = x[0]` (no cross-frame history), matching the reference `fir_filter`
(which leaves index 0 untouched). This is the minimal, well-justified change.

### BEFORE
```csharp
public static double[,] win_fir_hamming(double[,] win_pcmdata, int num_of_frames)
{
    double[,] tmparray = new double[num_of_frames, num_items_in_windowed_frame];

    for (int frame = 0; frame < num_of_frames; frame++)
    {
        for (int frame_item = 0; frame_item < num_items_in_windowed_frame; frame_item++)
        {
            if (frame_item == 0)
            {
                tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, 0]);
            }
            else
            {
                tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, frame_item - 1]);
            }
            tmparray[frame, frame_item] = (win_pcmdata[frame, frame_item] * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame))));
        }
    }
    return tmparray;
}
```

### AFTER
```csharp
public static double[,] win_fir_hamming(double[,] win_pcmdata, int num_of_frames)
{
    double[,] tmparray = new double[num_of_frames, num_items_in_windowed_frame];

    for (int frame = 0; frame < num_of_frames; frame++)
    {
        for (int frame_item = 0; frame_item < num_items_in_windowed_frame; frame_item++)
        {
            // Pre-emphasis FIR: y[i] = x[i] - 0.95*x[i-1], read from the INPUT.
            // Frame-local: index 0 has no in-frame predecessor -> y[0] = x[0].
            double preemphasized;
            if (frame_item == 0)
            {
                preemphasized = win_pcmdata[frame, frame_item];
            }
            else
            {
                preemphasized = win_pcmdata[frame, frame_item] - (0.95 * win_pcmdata[frame, frame_item - 1]);
            }

            // Hamming window applied to the pre-emphasized sample.
            tmparray[frame, frame_item] = preemphasized * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame)));
        }
    }
    return tmparray;
}
```

Notes:
- Reads strictly from `win_pcmdata`; output buffer `tmparray` is write-only — no
  aliasing / read-before-write (also avoids the IIR-recursion class of bug seen in
  BUG-05, which this code accidentally mimicked by reading `tmparray[...]`).
- At `frame_item == 0` the new value is `x[0]·hamming(0)` = `x[0]·0.08`. The old
  code produced the same `x[0]·0.08` at index 0 (its pre-emphasis term was 0 then
  overwritten), so index 0 is numerically unchanged; only indices ≥1 change.
- Hamming coefficient expression and the `const_2pi / num_items_in_windowed_frame`
  argument are preserved verbatim — no windowing behavior change.

---

## 3. Duplicated copies requiring the SAME change

`win_fir_hamming` exists in exactly two files (grep-verified, byte-identical
bodies). Both MUST be changed identically and together:

1. `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs`
   — lines **194–214** (loop body 198–212).
2. `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`
   — lines **170–190** (loop body 174–188).

No third copy: `grep -rln "win_fir_hamming"` returns only these two source files
(plus ROADMAP/plans). `Turan_core` and `Turan_tester` do not contain this
function. `fir_filter` / `hamming_ablak` / `win_hamming_ablak` are separate
routines tracked by BUG-05/BUG-06 and are **out of scope** here — do not touch.

**Consistency requirement:** the two copies feed the *same* DTW comparison
(Creator builds templates, Engine extracts live features). They must remain
bit-for-bit identical. If only one is changed, template and runtime features
diverge → recognition collapse. Apply both in the same commit.

---

## 4. Backward / data-format compatibility

- **On-disk file format: UNCHANGED.** Serialization is `BinaryFormatter` of a
  `double[,]` (`SerializeArray` / `DeSerializeArray`). The array shape
  (`extended_num_of_frames × mfcc_lpc_vect_num`, native mfcc = 15 wide) is
  unchanged. No reader/writer change is needed; no versioned-read is required for
  the *format*.
- **Data SEMANTICS change (stale-template hazard).** Existing `.mfcc` template
  files in any deployment were produced by the buggy (no-pre-emphasis)
  extractor. After this fix the **Engine** extracts pre-emphasized live features,
  but old template files still hold non-pre-emphasized features → DTW now compares
  mismatched feature spaces → **recognition accuracy will drop until templates are
  regenerated.** This is not a code-compat break but a data-compat break.
  - **Mitigation:** re-enroll / regenerate all `.mfcc` templates with the fixed
    Creator. Because the system is **speaker-dependent**, templates are per-user
    recordings that the deployment already owns the WAVs for (or can re-record), so
    regeneration is the expected, documented step.
  - **Why not a versioned read:** the `.mfcc` payload has no header/version field
    to key off, and even a version flag cannot retro-fit pre-emphasis into already
    log-mel-reduced numbers — the raw PCM is gone by serialization time. So
    auto-migration is impossible; regeneration from source WAVs is the only correct
    path. Document this in release notes: *"BUG-02 fix changes native-MFCC feature
    content; rebuild templates."*
  - **LPC path note:** the live native LPC branch (`Creator.cs:149`) calls
    `LPCCalcFrameByFrame(win_pcmdata)` **without** `win_fir_hamming` (the Hamming
    variant is commented out at Creator.cs:145–146). So `.lpc` templates are
    unaffected by this change unless that commented branch is later enabled. No
    `.lpc` regeneration needed for this fix alone.

---

## 5. Shared contracts other bug fixes depend on

- **Serialization contract (Creator.SerializeArray ↔ Engine.DeSerializeArray):**
  This fix does **not** alter it (same shape, same `BinaryFormatter`). BUG-12
  (replace `BinaryFormatter`) owns that contract; nothing here constrains it.
- **Feature-extraction contract (Creator vs Engine H_FELDOLGOZO):** This fix
  *reinforces* the implicit contract that both copies of the DSP chain must stay
  identical. BUG-14 (de-duplicate DSP into one shared library) would make this
  guarantee structural; until then, BUG-02 (and BUG-03/04/06) each must be mirrored
  across both `H_FELDOLGOZO.cs` copies. Coordinate so all H_FELDOLGOZO edits land
  consistently in both files.
- **Downstream width contract:** native mfcc uses `mfcc_lpc_vect_num = 15` set at
  `Creator.cs:156` / Engine equivalent. This fix does not change vector width, so
  it does not interact with BUG-01's 60-dim HTK width change or BUG-09's Itakura
  order. No coupling.

---

## 6. Self-verification WITHOUT a compiler

1. **Diff symmetry:** after editing, `diff` the two `win_fir_hamming` bodies and
   confirm they are identical apart from line offsets. They must match
   character-for-character in the loop body.
2. **Read-source-only invariant:** confirm by inspection that inside the loop the
   right-hand side reads **only** `win_pcmdata[...]` and constants; `tmparray` must
   appear **only** as an assignment target (LHS). This proves the empty-buffer read
   is gone and there is no IIR recursion.
3. **Hand-trace 3 samples** of one frame with toy input `x = [x0, x1, x2]`:
   - `tmparray[f,0] = x0 · (0.54 − 0.46·cos(0)) = x0·0.08`
   - `tmparray[f,1] = (x1 − 0.95·x0) · (0.54 − 0.46·cos(2π/256))`
   - `tmparray[f,2] = (x2 − 0.95·x1) · (0.54 − 0.46·cos(4π/256))`
   Verify index 0 equals the OLD output (regression-safe) and indices ≥1 now carry
   the `−0.95·x[i−1]` term (previously absent). Compare against the standalone
   `fir_filter` definition `y[i]=x[i]−0.95·x[i−1]` to confirm coefficient and sign
   match the project's own reference.
4. **Caller compatibility:** re-read `Creator.cs:158` and `Form1.cs:680` — the
   signature `(double[,], int)` and return type `double[,]` are unchanged, so no
   call-site edits are needed; this rules out compile breaks at the callers.
5. **No accidental scope creep:** grep the edited files to confirm `fir_filter`,
   `hamming_ablak`, `win_hamming_ablak`, and `mfccszamitas` are untouched.

---

## Risk assessment

- **Code risk: LOW.** Single self-contained function; signature, return shape, and
  windowing unchanged; minimal, local diff; index-0 output preserved.
- **Behavioral/data risk: MEDIUM** due to the stale-template hazard — accuracy
  regresses until templates are regenerated. Must be flagged in release notes and
  coupled with template rebuild.

## Files to edit
- `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs` (lines 194–214)
- `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` (lines 170–190)

---

## Peer review

**Reviewer verdict: APPROVED (no blocking changes).** Plan is correct, complete, and the fix does what BUG-02 requires. Verified against the real source, not just the plan's quotes.

### Verified correct
- **Root cause is accurate.** Both copies were read: `Turan_creator/.../H_FELDOLGOZO.cs:194-214` and `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:170-190`. Loop bodies are byte-identical and match the plan's BEFORE block exactly. The pre-emphasis lines read the zero-initialized `tmparray` (so `0 - 0.95*0 = 0`), and line 210/186 then overwrites with `win_pcmdata * hamming`. Pre-emphasis is genuinely a no-op; the function is currently equivalent to `win_hamming_ablak`. Confirmed.
- **The fix actually fixes it.** AFTER reads strictly from `win_pcmdata`, computes `y[i] = x[i] - 0.95*x[i-1]`, then Hamming-weights. `tmparray` is write-only (no read-before-write, no IIR aliasing). Correct.
- **Completeness across copies.** `find -name H_FELDOLGOZO.cs` returns exactly the two files the plan names; `grep win_fir_hamming` returns only those two plus the (commented-out) caller lines. No third copy. Both must change together — the plan states this.
- **Callers unaffected.** `Creator.cs:158` and `Felismero_motor_LITE/Form1.cs:680` both call the live native-MFCC path with signature `(double[,], int) -> double[,]`, unchanged. LPC branch's `win_fir_hamming` call is commented out in both modules, so `.lpc` is genuinely untouched. Confirmed.
- **No integer-division trap introduced.** `const_2pi = 2 * Math.PI` is a `double` (line 42), so `(const_2pi * frame_item) / num_items_in_windowed_frame` is double division. The Hamming expression is preserved verbatim anyway. No new bug.
- **Index-0 regression bound.** New `tmparray[f,0] = x0 * hamming(0) = x0 * 0.08` is bit-identical to the old output at index 0; only indices >=1 change. The plan's claim holds — this tightly bounds the behavioral-change surface.
- **Frame-local pre-emphasis is EXACT for interior indices (newly verified).** Read `create_window` (`H_FELDOLGOZO.cs:46`): frames hold **contiguous** PCM. Frame 0 = `pcmdata[0..255]`; frame f's first 128 samples = frame f-1's samples 128..255, second 128 = the next contiguous PCM block. Therefore within any frame `[f,i]` and `[f,i-1]` are truly adjacent PCM samples, so `x[i]-0.95*x[i-1]` is the *exact* pre-emphasis for all indices 1..255. Only index 0 (the frame boundary) is approximate, and it is attenuated to `0.08` by the Hamming term, so the artifact is negligible. This upgrades "frame-local is acceptable" from assertion to a backed claim.
- **Index-0 convention is the right call.** The plan's `y[0]=x[0]` matches the project's own `fir_filter` reference (loop starts at i=1). The ROADMAP's alternative hint ("previous-frame tail") would actually be *wrong* under 50% overlap: the true predecessor of frame f's sample 0 lives mid-previous-frame (at index 127), not at its tail. The plan correctly rejected that hint.
- **Data-compat hazard correctly characterized.** Format (`BinaryFormatter` of `double[,]`, shape `extended_num_of_frames x 15`) is unchanged; the break is *semantic* (old `.mfcc` templates were built without pre-emphasis). Speaker-dependent => regenerate templates from owned WAVs. Auto-migration is impossible (raw PCM gone by serialization). Correct and well-reasoned.

### Non-blocking observations (record, do not gate the fix)
1. **Hamming uses `/N` not the textbook `/(N-1)`.** The denominator `num_items_in_windowed_frame` (256) differs from the canonical Hamming `2*pi*n/(N-1)`. This is **pre-existing**, preserved verbatim by the fix, identical to `win_hamming_ablak`/`hamming_ablak`, so no regression — flagged only as out-of-scope DSP debt.
2. **Index-0 boundary discontinuity is inherent, not introduced.** As above, attenuated by Hamming 0.08 and out of BUG-02's "no-op" scope. A future, more-correct pre-emphasis would carry the previous frame's sample 127 into the current frame's index 0, but that is a refinement, not a defect this fix must address.
3. **Coordination with sibling H_FELDOLGOZO fixes (BUG-03/04/06).** Those edits touch the same two files (different functions: `mfccszamitas`, `hamming_ablak`). No textual overlap with `win_fir_hamming`, but land them so both copies stay identical (the plan already notes this under BUG-14).

### Required changes
None blocking. Recommended (optional, documentation-only): add the create_window contiguity finding and the `/N` vs `/(N-1)` note to the plan's Section 6 so the implementer knows the interior-exactness guarantee and does not "fix" the Hamming denominator as scope creep.
