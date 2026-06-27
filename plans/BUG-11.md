# FIX PLAN — BUG-11: `Engine.MatchLength` is an unimplemented stub (returns -1)

**Severity:** P2 (medium — misleading API surface / dead feature)
**File:** `/home/arsvivendi/git/Turan_engine/Turan_core/Turan_core/Engine.cs:148-155`
**Companion bug list:** `ROADMAP.md` → BUG-11
**Status of this doc:** plan only — no source edited.

---

## 1. Root cause (restated from the code)

`Engine.MatchLength` is declared with the same shape as the working
`RecognizeAndReturnIndex` (a signal file + an array of reference files → an `int`
index), but its body is a stub:

```csharp
public int MatchLength(string signal_length_filepath, string[] reference_length_filepaths)
{
    int result = -1;
    return result;
}
```

It ignores both parameters and unconditionally returns `-1`. The declared
"duration/length matching" feature does not exist; any caller would silently get
the no-match sentinel.

Supporting facts established by reading the code:

- **No callers anywhere.** `grep -rn "MatchLength"` across `--include=*.cs`
  returns only the declaration at `Engine.cs:148`. No production or test code
  invokes it.
- **`EngineMode.framenum` exists but is never handled.** `Engine.cs:39-44`
  defines `enum EngineMode { mfcc, lpc, framenum }`, yet `RecognizeAndReturnIndex`
  only branches on `mfcc`/`lpc` for feature width (`Engine.cs:112-120`) and never
  references `framenum`. The dangling enum value plus the parameter names
  (`*_length_filepath`) are the only surviving evidence of intended semantics:
  **match by utterance duration measured in frames.**
- **No dedicated "length file" format exists.** `grep` finds no length-file
  writer/reader. Therefore the inputs are the *same vector files* used by
  recognition; "length" is derived from the feature array, not a separate
  artifact.

> **Inferred-semantics flag (deliberate design decision, not a fact in code):**
> The method's intended behavior is undocumented. This plan infers
> *frame-count (duration) matching* from `EngineMode.framenum` and the
> `*_length_filepath` parameter names; no dedicated length-file format exists, so
> the inputs are the same vector files used by recognition. A reviewer may accept
> or reject that interpretation; if rejected, fall back to the "make-honest"
> alternative in §6.

---

## 2. Chosen fix

**Implement** `MatchLength` as frame-count matching, reusing the *existing*
format-aware loaders already used by `RecognizeAndReturnIndex`:

- `turan` format → `DeSerializeArray(path)` (line 162-179, already in this file).
- `htk` format → `HTK_Interface.ReadMFCC_D_A_T(path, width)` with
  `width = 12` (lpc) / `15` (mfcc), exactly mirroring `Engine.cs:111-123`.

The feature array is `double[nSamples, width]` where **dim 0 is the frame count**
(confirmed below). `MatchLength` loads the signal, then for each reference
returns the index whose frame count is closest to the signal's
(`argmin |signal_frames − ref_frames|`). Per-reference absolute differences are
written to `score_list` so `GetScoreList()` stays meaningful (mirrors how
`RecognizeAndReturnIndex` populates `score_list` from `dtwmatch.TotalCost`).

**Why implement rather than make-honest:** the change is purely **additive** —
there are zero callers, so no existing path (the live `RecognizeAndReturnIndex`
htk/lpc recognition) changes behavior. The required building blocks
(`DeSerializeArray`, `HTK_Interface.ReadMFCC_D_A_T`) already exist and are
read-only. Risk to the live engine is nil.

### Frame-orientation evidence (so "length" is computed on the right axis)
- `HTK_Interface.ReadMFCC_D_A_T` reads `nSamples` from the HTK header
  (`HTK_Interface.cs:60`) and allocates
  `vector_array = new double[nSamples, num_of_feature_vectors]` (`:71`); each loop
  iteration fills one **row** = one frame (`:74-105`).
- `dtwApp_match` indexes feature data as `data[frame, iter]`
  (`dtwApp_match.cs:184,197`) — first index is the frame.
- Therefore **frame count = `array.GetLength(0)`**. Never use `GetLength(1)`
  (that is the feature width).

---

## 3. Exact change (before / after)

**File:** `/home/arsvivendi/git/Turan_engine/Turan_core/Turan_core/Engine.cs`
**Lines:** 148-155 (replace the stub body; signature unchanged).

### BEFORE
```csharp
        public int MatchLength(string signal_length_filepath, string[] reference_length_filepaths)
        {
            int result = -1;


            return result;

        }
```

### AFTER
```csharp
        public int MatchLength(string signal_length_filepath, string[] reference_length_filepaths)
        {
            // BUG-11: implement duration matching (EngineMode.framenum intent).
            // "Length" == utterance length in frames == feature array dim 0.
            // Reuses the same format-aware loaders as RecognizeAndReturnIndex;
            // reads only the frame (row) count, so it is unaffected by feature
            // width / serialization changes (see BUG-01, BUG-12).

            if (reference_length_filepaths == null || reference_length_filepaths.Length == 0)
            {
                return -1;
            }

            // Feature width mirrors RecognizeAndReturnIndex (Engine.cs:111-123).
            int num_of_feature_vectors = 15;
            if (engine_mode == EngineMode.lpc)
            {
                num_of_feature_vectors = 12;
            }
            if (engine_mode == EngineMode.mfcc)
            {
                num_of_feature_vectors = 15;
            }

            int signal_frame_count = GetFrameCount(signal_length_filepath, num_of_feature_vectors);

            score_list.Clear();

            int result = -1;
            int best_diff = int.MaxValue;

            for (int idx = 0; idx < reference_length_filepaths.Length; idx++)
            {
                int ref_frame_count = GetFrameCount(reference_length_filepaths[idx], num_of_feature_vectors);
                int diff = Math.Abs(signal_frame_count - ref_frame_count);

                score_list.Add(diff);

                if (diff < best_diff)
                {
                    best_diff = diff;
                    result = idx;
                }
            }

            return result;
        }

        /// <summary>
        /// Loads a feature file (respecting the configured vector format / engine
        /// mode) and returns its frame count (number of rows, dim 0).
        /// </summary>
        private int GetFrameCount(string vector_filepath, int num_of_feature_vectors)
        {
            double[,] data;
            if (vector_format == VectorFileFormat.turan)
            {
                data = DeSerializeArray(vector_filepath);
            }
            else // VectorFileFormat.htk
            {
                data = HTK_Interface.ReadMFCC_D_A_T(vector_filepath, num_of_feature_vectors);
            }
            return data.GetLength(0);
        }
```

Notes on the snippet, justified:
- `System` is already imported (`Engine.cs:19`), so `Math.Abs` / `int.MaxValue`
  need no new `using`.
- The two-`if` width block is copied **verbatim** from `RecognizeAndReturnIndex`
  (not collapsed) to keep the diff a literal mirror and avoid re-deciding the
  default for any future `framenum` mode.
- The `dtwApp_match.Num_of_templates = …` line from `RecognizeAndReturnIndex` is
  **deliberately not copied** — `MatchLength` never constructs a `dtwApp_match`.
- Writing `score_list` (with `Clear()` first) is a **deliberate** choice to keep
  `GetScoreList()` usable after `MatchLength`, exactly as after
  `RecognizeAndReturnIndex`. It mutates shared engine state; flagged here so a
  reviewer can veto it. If undesired, drop the `score_list.Clear()`/`.Add(diff)`
  lines — the return value is unaffected.
- Ties (`diff` equal) resolve to the **first** reference (strict `<`), matching
  the natural argmin convention.

---

## 4. Duplicated copies needing the same change

**None.** Verified:
- `find … -name Engine.cs` → exactly one file:
  `Turan_core/Turan_core/Engine.cs`.
- `grep -rn "MatchLength"` → only that one declaration; the method exists nowhere
  else.

The project's "duplicated source" warning applies to `HTK_Interface.cs` (×3),
`H_FELDOLGOZO.cs` (×2) and `dtwApp_match.cs` (×2) — **not** to `Engine.cs`. This
fix touches a single file. The `HTK_Interface.ReadMFCC_D_A_T` we call is the
Turan_core copy at `Turan_core/Turan_core/HTK_Interface.cs`, reached via the same
namespace as `Engine`; no other copy is involved, and we are not modifying it.

---

## 5. Backward / format compatibility

- **No on-disk, template, or data-format change.** `MatchLength` only *reads*
  existing vector files through the existing loaders (`DeSerializeArray`,
  `HTK_Interface.ReadMFCC_D_A_T`). It writes nothing to disk and defines no new
  file type.
- **No public API break.** Signature of `MatchLength` is unchanged; the new
  `GetFrameCount` helper is `private`. Behavior change is additive (the method
  previously always returned `-1` and had no callers).
- **Shared `score_list` contract** (`Engine.cs:34,74-77,99-104,134-139`): the
  fix reuses the same `score_list` field and `GetScoreList()` accessor that
  `RecognizeAndReturnIndex` uses. Semantics differ per method (DTW cost vs. frame
  diff), consistent with the field being a generic "last run scores" buffer. No
  format/versioning needed.

### Shared contracts other bugs depend on (requirement 5)
- **Loader contract — frame count is dim 0.** `MatchLength` depends only on
  `array.GetLength(0)` (rows = frames). This is *why* it is insulated from:
  - **BUG-01** (HTK reader should emit 60-dim frames instead of summing into
    15): that fix changes `GetLength(1)` (feature width), **not** the row count
    `nSamples`. `MatchLength` must keep using `GetLength(0)` so it stays correct
    before *and* after BUG-01. If BUG-01 also bumps the width argument the engine
    passes, `MatchLength`'s width selection should be updated in lockstep with the
    `RecognizeAndReturnIndex` block it mirrors — but the returned frame count is
    invariant regardless.
  - **BUG-12** (replace `BinaryFormatter` in `Creator.SerializeArray` /
    `Engine.DeSerializeArray` with a versioned/length-prefixed reader): whatever
    new serializer is chosen MUST preserve the 2D `double[rows=frames, cols]`
    shape so `DeSerializeArray(...).GetLength(0)` still yields the frame count.
    `MatchLength` adds one more consumer of that return shape; BUG-12's
    replacement must honor it (already required by
    `RecognizeAndReturnIndex`/`dtwApp_match`).
- No serialization is *produced* here, so this fix introduces no
  `SerializeArray` ↔ `DeSerializeArray` round-trip obligation.

---

## 6. Minimal alternative (documented, not recommended)

If the inferred frame-count semantics are rejected, the truly-minimal way to kill
the "misleading API" impact without inventing behavior is to replace the body
with `throw new NotImplementedException("MatchLength: duration matching not implemented");`
(or delete the method if no published API contract requires it). Recommended
choice is **implement** (§2-§3): additive, zero-risk to live paths, and it gives
the dormant `EngineMode.framenum` a working realization.

---

## 7. Self-verification without a compiler

1. **Single-copy / no-caller invariants** (re-run before editing):
   - `find /home/arsvivendi/git/Turan_engine -name Engine.cs` → one path.
   - `grep -rn "MatchLength" --include=*.cs /home/arsvivendi/git/Turan_engine`
     → one hit (the declaration). Confirms no caller breaks and no duplicate.
2. **Imports present:** `Engine.cs:19` `using System;` covers `Math.Abs` /
   `int.MaxValue`. `HTK_Interface` and `DeSerializeArray` are in scope (same
   namespace `Turan_core` / same class).
3. **Frame axis trace:** read `HTK_Interface.cs:60` (`nSamples = b.ReadInt32()`)
   and `:71` (`new double[nSamples, num_of_feature_vectors]`); confirm the fill
   loop `:74-105` advances `current_frame` (the row). Therefore
   `GetLength(0) == nSamples ==` frame count. Cross-check `dtwApp_match.cs:184,197`
   index `data[frame, iter]` → first index is the frame. Confirms `GetLength(0)`,
   not `GetLength(1)`.
4. **Width-branch parity:** diff the new width block against `Engine.cs:111-120`
   — must be character-identical logic (mfcc→15, lpc→12), so the htk reader is
   handed the correct `4 × width` stride and never reads past EOF (passing 15
   against a 12-wide lpc file would over-read and throw in `ReadMFCC_D_A_T`).
5. **Argmin hand-run (2 references):** signal=100 frames; ref0=90 (diff 10),
   ref1=130 (diff 30). Trace: idx0 diff10<MAX → result=0,best=10; idx1 diff30,
   not <10 → result stays 0. Returns 0, `score_list = [10, 30]`. Matches intent.
6. **Empty-input guard:** `reference_length_filepaths` null/empty → returns `-1`
   (preserves the original sentinel; `GetScoreList()` left untouched).

---

## Peer review

**Reviewer verdict: APPROVED (with one recommendation and three flags to record).** No
blocking technical defect. Every factual claim in the plan was re-verified against the
real source; the implementation is correct *if* the inferred frame-count semantics are
accepted.

### Verified against source
- **Single copy / no callers.** `grep -rn "MatchLength" --include=*.cs` → one hit
  (`Engine.cs:148`); `find -name Engine.cs` → one file. §4 is accurate; this is genuinely
  not a duplicated-source case.
- **Frame axis = `GetLength(0)`.** Confirmed: `HTK_Interface.cs:60` reads `nSamples` from
  the HTK header, `:71` allocates `new double[nSamples, num_of_feature_vectors]`, fill loop
  `:74-105` advances `current_frame` (the row). turan arrays share the same orientation
  (both feed `dtwApp_match` which indexes `data[frame, iter]`). The plan uses `GetLength(0)`
  throughout — correct.
- **Symbols in scope.** `using System;` (line 19) covers `Math.Abs`/`int.MaxValue`;
  `DeSerializeArray` (162-179) and `HTK_Interface.ReadMFCC_D_A_T` are reachable. ✔
- **Width block** mirrors `Engine.cs:111-120` verbatim (mfcc/default→15, lpc→12). ✔
- **No off-by-one, no integer division.** `Math.Abs(int-int)` only; strict `<` argmin
  resolves ties to the first reference. ✔
- **Backward/format compat.** Read-only; writes nothing to disk; signature unchanged;
  helper is `private`. ✔

### Recommendation (please resolve before implementing — do not pass through as
### "reviewer's choice")
The plan defers the load-bearing decision (implement vs. §6 make-honest) to the reviewer.
As reviewer I register an explicit position so the choice is owned, not floated:

> **Implement (§2-§3) is acceptable, but only because** it is additive, has zero callers,
> and zero live-path risk. Be aware it carries a counter-argument: BUG-11's stated impact is
> *"misleading API surface,"* and shipping **unspecified** semantics (argmin-by-frame-count
> that no spec defines) can be a subtler trap than an honest stub. If minimizing
> misleading-API is the priority, §6 (`NotImplementedException` / removal) is the more honest
> resolution. Either is roadmap-sanctioned. **Pick one in the plan and state why**, rather
> than leaving it to implementation time. My lean: implement, *with* the XML-doc on
> `MatchLength` explicitly stating the inferred "closest utterance duration in frames"
> contract so the method is not silently mysterious.

### Flags to record (none individually blocking)
1. **`score_list` dual-semantics — strongest shared-contract concern.** After this change
   the buffer holds either DTW costs (`RecognizeAndReturnIndex`) or frame diffs
   (`MatchLength`) depending on the last call, with no discriminator. Harmless while
   MatchLength has no callers, but this **must be flagged for BUG-10** (confidence
   threshold / rejection): BUG-10 must not assume `GetScoreList()` is always cost-scaled.
   Add a cross-reference in BUG-10's plan, or scope `MatchLength`'s scores to a separate
   field/out-param.
2. **BUG-01 / BUG-12 forward-compat is correctly stated.** Frame count is invariant under
   BUG-01 (that fix changes dim-1 width, not dim-0 `nSamples`); the "update width in
   lockstep" note is right as forward-compat hygiene. BUG-12's replacement serializer must
   preserve the `double[frames, cols]` shape — already required by
   `RecognizeAndReturnIndex`/`dtwApp_match`, so MatchLength adds no new obligation.
3. **Width only guards HTK over-read; it is irrelevant to the frame count itself.** For
   turan, `DeSerializeArray` ignores the width arg. For HTK, `nSamples` is read from the
   header *before* the data loop, so even a too-small width still returns the correct
   `GetLength(0)`; a too-large width would over-read past EOF and throw. The mirrored
   15/12 selection keeps standard 4×static HTK files (4×15=60 floats/frame) reading exactly
   to EOF — no over-read, no throw. Worth one explicit line in the plan confirming this so a
   future reader does not "simplify" the width block away.

### Minor
- The empty-input guard returns `-1` **without** clearing `score_list`, so a stale buffer
  from a prior run survives. The plan already notes this; acceptable, but state it in the
  XML-doc too.
