# FIX PLAN — BUG-08: Hard-coded array caps (120, 256) limit utterance length / template count

**Severity:** P2 (medium — robustness/limits)
**Status of evidence:** Established by code reading only (no build toolchain). Every literal and every caller below was grep-verified in the working tree on branch `fix/roadmap-bugs`. NO source edited yet.
**Group / order (`plans/_grouping.json`):** Group **A-dtw-engine**, internal order `width-source → BUG-01 → BUG-09 → BUG-10 → BUG-08 → BUG-11`. BUG-08 lands **after** BUG-01/09/10 in the same `dtwApp_match.cs`, so absolute line numbers below will have drifted by implementation time — **match on the snippet text, not the line number.** Those earlier edits touch unrelated regions (`EuclideanDistance` ~235, `temp3/temp4` 466/467, `ITDDistance` 255–352, `num_of_vectoritems` ~84); none collide with the `costRecord` sites (147/164/534). Ships in commit chunk `fix(dtw): unify DTW feature-vector width … and add rejection sentinel`.
**Scope (post-revision):** `dtwApp_match.cs` `costRecord` ONLY (Changes 1+2, both copies). The H_FELDOLGOZO/`hasonlit` 256-cap edit (former Change 3) is **DROPPED** — that file is deleted by BUG-16 in the file-disjoint Group B. BUG-08 edits no `H_FELDOLGOZO.cs`.

---

## 1. Root cause (restated from the code)

Two distinct magic caps, in two distinct subsystems:

### (A) `costRecord = new double[120]` — `dtwApp_match.cs` (both copies)

- `Turan_core/Turan_core/dtwApp_match.cs:147` and `:164` (the two constructors).
- `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs:147` and `:164` (identical).

`costRecord` is the per-frame accumulated-cost-along-the-warped-path buffer, declared `public double[] costRecord;` (line 121). It is meant to be indexed by **signal frame** `i ∈ [0, testlength)`, as shown by the only code that ever fills it — the commented "FIX IT CODE x01" block in the LITE copy (`dtwApp_match.cs:596-601`):

```
//for (i = 0; i < testlength; i++)
//{
//    int temp = pathRecord[templateIndex, i];
//    costRecord[i] = cost2[i, temp];
//}
```

`testlength` is the **signal frame count** = `I = signal.getRowLength()` (set in `lefttorightMatch`, `:378`, and passed to `backTrace` at `:522`). Hard-coding the buffer to `120` therefore caps an utterance to 120 frames; the fill loop would throw `IndexOutOfRangeException` for any longer utterance.

**Current liveness:** LATENT, not live. `costRecord` is **never read or written** on any live path:
- `Turan_core`: only the two `new double[120]` allocations exist — no index, anywhere.
- `LITE`: the only fill loop (`:596-601`) and the only reader (`Form1.cs:524` `ShowArray(dtwmatch.costRecord,…)`) are both commented out.

So the `120` is presently a dead allocation, but it is the exact cap the roadmap names, and it becomes a real `IndexOutOfRangeException` the moment anyone re-enables the (already-written) cost-tracing code. The minimal correct fix is to size it from the actual signal length.

> Adjacent (same root cause, same dead "FIX IT CODE x01" pair): `pathRecord = new int[num_of_templates, 1]` (`:146/:163`). Its width `1` is the analogous cap for the per-frame path record; the commented writer `pathRecord[templateIndex, iter]` for `iter < temppath.Length` would also overflow. Handled as an optional, clearly-scoped extra in §2 (Change 2b).

### (B) `tavtomb / ertomb / refcounters / mfccrefs = new[256…]` — `H_FELDOLGOZO.cs` (both copies)

- `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:279-281,291`
- `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs:315-317,327` (the hasonlit region of the two copies is byte-identical — verified by `diff`).

```
public static double[,] tavtomb = new double[256, mfcc_lpc_vect_num];   // DTW distance matrix
public static double[,] ertomb  = new double[256, mfcc_lpc_vect_num];   // DTW accumulator
public static int[]     refcounters = new int[256];
public static double[]  mfccrefs   = new double[256];
```

These belong exclusively to `hasonlit(...)` — the **native hand-written DTW** (`H_FELDOLGOZO.cs:294` Creator / `:330` LITE). The `256` is the cap on the number of frames the matrix can hold (its row dimension). Inside `hasonlit` the cap is re-asserted as literals: the clear loop `for (i = 0; i < 255; i++)` (Creator `:303` / LITE `:339`) and the inner walk `while (… && (seged1 <= 255))` (Creator `:351` / LITE `:387`).

**Current liveness:** DEAD. Every caller of `hasonlit` is commented out (`Felismero_motor_LITE/Form1.cs:1472,1491,1505`). `hasonlit` is the dead native DTW that BUG-16 deletes outright; the live recognizer uses `dtwApp_match` instead.

A second latent defect compounds the cap here: the column dimension `mfcc_lpc_vect_num` is captured at **static-field-initialization time** (when it is `12`). Runtime code re-assigns `mfcc_lpc_vect_num` to `15` (`Creator.cs:156`, `Form1.cs:108`), but `tavtomb/ertomb` keep their col width of `12` — so even the column dimension is a stale hard cap, not just the `256` rows.

> **DISPOSITION (decided in `plans/_grouping.json`).** The `256`/`255` caps in this subsystem are **NOT edited by BUG-08.** Per the `_grouping.json` "DEAD-METHOD OWNERSHIP" shared contract and Group B's internal order, **BUG-16 (Group B) deletes `hasonlit` together with `tavtomb`, `ertomb`, `refcounters`, `mfccrefs` and the commented driver in LITE `Form1.cs`.** The whole 256-row cap (plus its `for(i<255)` clear loop and `<=255` walk bounds) therefore disappears with the dead method — there is nothing left for BUG-08 to resize. BUG-08 is consequently **scoped to `dtwApp_match.cs` `costRecord` only** and **does not touch `H_FELDOLGOZO.cs` at all** (those two files are owned by the parallel, file-disjoint Group B). This is also what the peer review's "Net" recommended ("preferably delete `hasonlit` and drop Change 3 entirely"). See the dropped Change 3 in §2 and the Revision note at the end.

### (C) NOT a cap — leave untouched: `num_items_in_windowed_frame = 256`

`H_FELDOLGOZO.cs:37` `num_items_in_windowed_frame = 256` is the **analysis window length in samples** (128 from the previous frame + 128 from the current, per the inline comment and the overlap loop at `:64-69`). It is a legitimate DSP constant, not a count cap. **It must NOT be changed:** it determines the frame width used by feature extraction and therefore the on-disk template geometry. (Likewise the dead `mfccarr = new double[256,256]` at Creator `:224` / LITE `:200` lives in the dead, separately-broken `mfccszamitas` DCT — BUG-03/04 — and is out of scope here.) This is called out so a future maintainer does not "fix" the wrong 256.

---

## 2. Exact change per file/line (before → after)

### Change 1 — `dtwApp_match.cs` constructors: drop the `120` cap (BOTH copies)

Files & lines (identical in both):
- `Turan_core/Turan_core/dtwApp_match.cs:147` and `:164`
- `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs:147` and `:164`

**Before** (each of the four sites):
```csharp
            costRecord = new double[120];
```
**After:**
```csharp
            // BUG-08: sized from actual signal length in backTrace(); no fixed 120-frame cap.
            costRecord = null;
```

Rationale for `null` rather than deleting the line: `costRecord` is a `public` field; keeping an explicit initializer documents intent and keeps the two constructors symmetric. Nothing reads `costRecord` between construction and `backTrace` (grep-verified), so `null` is safe.

### Change 2 — `dtwApp_match.cs` `backTrace(...)`: allocate from `testlength` (BOTH copies)

Insert one line immediately after the existing `temppath` allocation inside `backTrace`. Anchor line (identical in both copies, `:534`):

**Before:**
```csharp
            int[] temppath = new int[testlength + 1];
            int temppathlength = 0;
```
**After:**
```csharp
            int[] temppath = new int[testlength + 1];
            int temppathlength = 0;

            // BUG-08: size cost trace to the real signal length (frames), removing the 120-frame cap.
            costRecord = new double[testlength];
```

`testlength` is `backTrace`'s first parameter and equals `minX + 1` (`minX = testlength - 1`, `:537`), i.e. the signal frame count. `costRecord[i]` for `i ∈ [0, testlength)` is then always in range, so this change makes **`costRecord`** safe for the commented "FIX IT CODE x01" fill (LITE `:597-601`).

> **Caveat (closes peer-review secondary item).** This does NOT, by itself, make re-enabling that fill safe: the same loop also reads `pathRecord[templateIndex, i]` for `i < testlength`, and `pathRecord` stays width `1` until the deferred Change 2b. So re-enabling the FIX-IT fill requires **both** Change 2 (this) **and** Change 2b. Change 2 on its own is correct and complete for BUG-08's purpose (sizing `costRecord` from the data, killing the `120` literal); it simply is not a green light to uncomment the fill on its own.

> This is the minimal, behaviour-preserving fix: today nothing reads `costRecord`, so allocation size is observationally irrelevant; the change removes the magic literal and makes the buffer correctly data-sized for the (already-written) future reader.

### Change 2b — OPTIONAL, same root cause: `pathRecord` width (BOTH copies)

Only adopt if you also intend to re-enable the per-frame path trace. Keep separate from Change 1/2 if you want the tightest scope.

`dtwApp_match.cs:146` and `:163` (both copies) currently read:
```csharp
            pathRecord = new int[num_of_templates, 1];
```
Leave as-is for now (width `1` is consumed only by the commented writer). If re-enabling: size the per-frame path from `testlength` exactly as `costRecord`, e.g. `pathRecord = new int[num_of_templates, testlength];` moved to where templates/length are known. Flagged, not applied, to keep BUG-08 surgical.

### Change 3 — DROPPED (was: data-size the `hasonlit` DTW matrices)

> **DROPPED in this revision. Do not apply.** Authority: `plans/_grouping.json` → shared contract *DEAD-METHOD OWNERSHIP* ("BUG-16 (Group B) deletes hasonlit + its backing fields from both H_FELDOLGOZO.cs copies and the commented driver in LITE Form1.cs. Consequently BUG-08 (Group A) MUST drop its hasonlit/tavtomb/ertomb cap edits and ship only the dtwApp_match.cs costRecord fix") and Group A note ("its H_FELDOLGOZO/hasonlit cap (Change 3) is DROPPED because BUG-16 (Group B) deletes hasonlit — do NOT edit H_FELDOLGOZO.cs from this group").

**Why dropped (and how this closes the peer-review BLOCKER):** The peer review proved the former Change 3 shipped a **guaranteed `IndexOutOfRangeException`**: it bounded only the inner `seged1` walk (`<= 255` → `< count_param - 1`) but left the *earlier* column-0 walk (Creator `:331` / LITE `:367`, `while ((tavtomb[seged1 + 1, seged2] != -1) && …)`) unbounded. That loop relies on a `-1` sentinel in rows `count_param..255` of a `[256,…]` buffer; allocating exactly `count_param` rows and filling all of them removes every sentinel, so `tavtomb[count_param, 0]` is read out of range on the first call. Rather than *repair* the edit (e.g. bound the `seged1 + 1` walk with `&& (seged1 + 1 < count_param)`), BUG-08 **removes the edit entirely** — the whole `hasonlit` method, its `tavtomb/ertomb/refcounters/mfccrefs` fields, and all 256/255 literals are deleted by **BUG-16** in the file-disjoint Group B. No buggy DTW-matrix edit is ever produced, which is the cleanest possible closure of the blocker and is exactly what both the review's "Net" and `_grouping.json` mandate.

**Net effect:** BUG-08 no longer edits `H_FELDOLGOZO.cs` at all. The `256`-row cap, the `for (i < 255)` clear loop, and the `<= 255` walk bounds named in ROADMAP BUG-08 are dispositioned by **deletion (BUG-16)**, not by resizing here. The remaining ROADMAP-named 256 — `num_items_in_windowed_frame = 256` — is a legitimate window length and is deliberately left untouched (see §1C).

---

## 3. Every duplicated copy needing the same change

| Change | Turan_core | Felismero_motor_LITE | Turan_creator |
|---|---|---|---|
| 1+2 `costRecord` (dtwApp_match) | `Turan_core/Turan_core/dtwApp_match.cs` ✅ | `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` ✅ | (no dtwApp_match here) |
| ~~3 `tavtomb/ertomb` (H_FELDOLGOZO)~~ | — | DROPPED — owned by BUG-16 (Group B) | DROPPED — owned by BUG-16 (Group B) |

Only the two `dtwApp_match.cs` copies are BUG-08 targets, and they are line-identical at the edit sites (`:147,:164` `costRecord = new double[120]`; `:121` field decl; `:534` `temppath`; `:537` `minX`). Verified in this revision: both copies match byte-for-byte at those lines; Turan_core's `backTrace` ends at `:568` with no cost-fill, while the LITE copy carries the commented FIX-IT fill at `:597-601` — neither reads `costRecord` on a live path. `Turan_tester` has no `dtwApp_match.cs`. **No `H_FELDOLGOZO.cs` copy is a BUG-08 target** (Change 3 dropped; BUG-16 deletes those files' `hasonlit` region in Group B).

---

## 4. Backward-compatibility / on-disk format impact

**No format change. No backward-compatibility break.**

- The only buffer BUG-08 now touches — `costRecord` — is an **in-memory scratch buffer**, never serialized. It never touches `Creator.SerializeArray` / `Engine.DeSerializeArray`, the `.mfcc`/`.lpc` template files, or any HTK artifact (grep-verified — it appears only in `dtwApp_match.cs` plus commented Form1 lines). (The `tavtomb/ertomb/refcounters/mfccrefs` scratch buffers are likewise unserialized, but they are no longer BUG-08's concern — BUG-16 deletes them.) BUG-08 is orthogonal to the TRMS serialization format added by BUG-12 (sequential group S1).
- The one constant that *would* break template compatibility — `num_items_in_windowed_frame = 256`, the window/frame width — is **deliberately left unchanged** (§1C). Template geometry on disk is therefore unaffected.
- No versioned-read shim is needed because no persisted format moves.

---

## 5. Shared contracts other fixes depend on

- **WIDTH CONTRACT (BUG-01 / BUG-09):** No interaction. `costRecord` is **frame-indexed**, sized from `testlength` (the signal frame count), and is **completely independent of the DTW feature-vector width.** It does not read `Engine.mfcc_lpc_vect_num`, `data.GetLength(1)`, or any per-array width, so the Group A width contract (width = `data.GetLength(1)` / live `Engine.mfcc_lpc_vect_num`, never a bumped static 60) neither binds nor is affected by BUG-08's edits. The earlier `tavtomb/ertomb` column-width discussion is moot now that Change 3 is dropped (BUG-16 deletes those arrays). BUG-08 therefore commits cleanly alongside BUG-01/09/10 in the same `dtwApp_match.cs` without sharing any width read-site.
- **Serialization contract (BUG-12):** Untouched. BUG-08 changes nothing that `SerializeArray`/`DeSerializeArray` read or write, so the two fixes are orthogonal and can land in any order.
- **BUG-16 (dead-DTW deletion):** Owns the entire `hasonlit`/`tavtomb`/`ertomb`/`256` machinery. Per `_grouping.json` DEAD-METHOD OWNERSHIP, BUG-16 (Group B) **deletes** it, so BUG-08's former Change 3 is **dropped** — no cross-group double-edit, no sequencing dependency, and `H_FELDOLGOZO.cs` is not in BUG-08's file set. Group A and Group B are file-disjoint and run in parallel.

---

## 6. Self-verification WITHOUT a compiler

**dtwApp_match (Changes 1, 2):**
1. `grep -rn "\[120\]" Turan_core Felismero_motor_LITE` → expect **zero** hits after the edit.
2. `grep -rn "costRecord" --include=*.cs` → the only non-comment occurrences should be: the field decl (`:121`), the two `= null` initializers, and the single `new double[testlength]` in `backTrace`. Confirm no live reader exists outside comments (already true today).
3. Trace the size contract: `lefttorightMatch:378` `I = signal.getRowLength()` → `backTrace(path,cost,I,J)` (`:522`) → `backTrace`'s `testlength == I` → `costRecord = new double[testlength]`. Confirm `minX = testlength-1` (`:537`) so every index the commented fill uses (`i ∈ [0,testlength)`) is `< costRecord.Length`. ✔
4. Confirm allocation precedes any use: `backTrace` runs once per template inside `bestMatch` (`:586`); `costRecord` is allocated at the top of `backTrace` before the (commented) fill. No path reads it before allocation.

**Line-drift guard (BUG-08 runs 4th in Group A's `dtwApp_match.cs` sequence):**
5. After BUG-01/09/10 land, re-locate the four edit sites by **snippet text**, not by the line numbers above: `grep -n "costRecord = new double\[120\]"` (expect exactly 2 hits per copy = the two constructors) and `grep -n "int\[\] temppath = new int\[testlength + 1\]"` (expect 1 per copy). Apply Changes 1/2 at those matches. Confirm BUG-01/09/10's sites (`EuclideanDistance`, `temp3/temp4` ~466/467, `ITDDistance` 255–352, `num_of_vectoritems` ~84) are disjoint from the `costRecord` sites — no overlapping hunk.

**Confirm the non-changes (H_FELDOLGOZO is NOT a BUG-08 target):**
6. `grep -rn "tavtomb\|ertomb\|hasonlit" Turan_creator Felismero_motor_LITE --include=H_FELDOLGOZO.cs` → BUG-08 must leave every hit untouched (they belong to BUG-16/Group B). The git diff for the BUG-08 commit must contain **no `H_FELDOLGOZO.cs` hunk.**
7. `grep -rn "num_items_in_windowed_frame *= *256" --include=*.cs` → still present, unchanged, in both H_FELDOLGOZO copies (the legitimate window length stays put → template format preserved; this is BUG-16/Group B's file, not ours, but the constant must survive regardless).

---

## Scope summary

- **The whole of BUG-08 (removes the named 120 cap):** Changes 1 + 2 in both `dtwApp_match.cs` copies. This is the entire deliverable.
- **DROPPED (the named 256 cap):** former Change 3 in `H_FELDOLGOZO.cs` — owned by **BUG-16 (Group B)**, which deletes `hasonlit` and its `tavtomb/ertomb/[256]` machinery outright. BUG-08 edits no `H_FELDOLGOZO.cs`.
- **Optional adjacency:** Change 2b (`pathRecord` width) — defer unless re-enabling the path trace; required *together with* Change 2 before the FIX-IT cost-fill may be uncommented.
- **Explicitly excluded:** `num_items_in_windowed_frame` (window length, not a cap; touching it breaks template compatibility) and `mfccarr[256,256]` (dead DCT, BUG-03/04).

---

## Peer review

**Verdict: NOT APPROVED (approved=false).** The live-path fix (Changes 1 + 2) is correct and well-scoped, but **Change 3 introduces a guaranteed `IndexOutOfRangeException`** and must be revised before it is applied. Source re-verified against the working tree on `fix/roadmap-bugs`.

### What is correct (no change needed)
- **Change 1 + 2 (`costRecord`, both `dtwApp_match.cs` copies).** Line refs verified: `:147`/`:164` (`new double[120]`) and `:534` (`int[] temppath = new int[testlength + 1]`) are identical in both copies. `costRecord` has no live reader (only field decl `:121`, the two allocations, the commented FIX-IT fill `:596-602` LITE, and commented `Form1.cs:524`), so `costRecord = null` in the constructors is safe. `backTrace` is `private`, called once per template from `bestMatch`, and `testlength == I == signal.getRowLength()`, so `new double[testlength]` correctly sizes the frame buffer; the commented fill indexes `costRecord[i]` for `i ∈ [0,testlength)` → in range. Sound.
- **Change 2b deferred, §1C non-change, §4 format analysis.** Confirmed: `tavtomb/ertomb/refcounters/mfccrefs` are referenced nowhere outside `H_FELDOLGOZO.cs` except one commented `Form1.cs` line; nothing is serialized; `num_items_in_windowed_frame = 256` is a live DSP/window constant that must stay. No on-disk format risk.

### BLOCKER — Change 3 removes the sentinel that an un-edited loop depends on
`hasonlit`'s `seged1` (test-frame row) dimension is walked by **two** loops, not one. The plan only bounds the inner loop (Creator `:351` / LITE `:387`, `seged1 <= 255` → `< count_param - 1`) and never touches the earlier column-0 walk (Creator `:331` / LITE `:367`):

```csharp
while ((tavtomb[seged1 + 1, seged2] != -1) && (seged2 < num_items_in_windowed_frame - 1))
{ seged1++; ertomb[seged1, seged2] = ertomb[seged1 - 1, seged2] + tavtomb[seged1, seged2]; }
```

Here `seged2` is pinned at `0`, so the `< num_items_in_windowed_frame - 1` (i.e. `0 < 255`) clause is vacuously true and supplies **no bound**. Termination depends entirely on hitting a `-1` sentinel cell. Today that works only because the buffer is `[256, …]` and rows `count_param..255` are left `-1` by the clear loop.

Change 3 allocates **exactly `count_param` rows and fills all of them**, so no cell is ever `-1` (reinforced by the pre-existing never-reset `tmp` accumulator at `:322`/`:358` — `tavtomb` values are monotonic and never `-1`). The loop then increments `seged1` until it reads `tavtomb[count_param, 0]` → **out of range, every call**. This fires *before* the inner loop the plan fixed, so the `< count_param - 1` bound is never even reached. The plan's claim that Change 3 "remove[s] ONLY the fixed caps and change[s] nothing else" and self-verification step 7's "✔" are therefore both false. (Dead code today — gated behind BUG-16 — but the plan presents Change 3 as a concrete, verified, applyable edit, so it must be corrected, not shipped as-is.)

**Required change:** also bound the `seged1 + 1` walk at Creator `:331` / LITE `:367`, e.g. `&& (seged1 + 1 < count_param)`. This reproduces the original stop point with a `count_param`-row buffer and is bounds-safe. (Equivalent alternative: allocate `count_param + 1` rows and keep the clear loop covering all rows so the sentinel survives — more churn, same effect.) Update self-verification step 7 to trace *both* `seged1` loops.

### Secondary (non-blocking) — plan text contradicts itself on Change 2
§2 Change 2 (line ~99) states that re-enabling the commented FIX-IT fill "becomes safe with no further change." It does not: the same fill reads `pathRecord[templateIndex, i]` for `i < testlength`, and `pathRecord` is width `1` until Change 2b (which the plan defers). So Change 2 makes `costRecord` safe but the fill still overflows `pathRecord`. Reword: re-enabling the fill requires Change 2b as well. No code impact — Change 2 itself stays as written.

### Net
- Apply Changes 1, 2 as written (live module — the actual BUG-08 deliverable).
- Do **not** apply Change 3 until the `:331`/`:367` walk is bounded; keep it subordinate to BUG-16 (preferably delete `hasonlit` and drop Change 3 entirely).
- Fix the two plan-text claims (§2 "no further change", step 7 "✔").

---

## Revision 2026-06-27

Revised to (a) resolve every blocking item in the Peer review above and (b) conform to `plans/_grouping.json`. Source re-verified in the working tree on `fix/roadmap-bugs`: both `dtwApp_match.cs` copies are byte-identical at the edit sites — `:147`/`:164` (`costRecord = new double[120]`), `:121` (field decl), `:534` (`int[] temppath = new int[testlength + 1]`), `:537` (`minX = testlength - 1`). Turan_core `backTrace` ends at `:568` with no cost-fill; LITE carries the commented FIX-IT fill at `:597-601`; neither reads `costRecord` on a live path. No `.cs` source was edited — plan only.

**What changed, and which blocking issue each change closes:**

1. **Former Change 3 (`H_FELDOLGOZO.hasonlit` matrix resize) → DROPPED entirely** (§2, plus disposition notes in §1B, §3 table, §4, §5, §6, Scope summary).
   - **Closes the Peer-review BLOCKER** ("Change 3 introduces a guaranteed `IndexOutOfRangeException`": the unbounded column-0 `seged1 + 1` walk at Creator `:331` / LITE `:367` loses its `-1` sentinel once the buffer is exactly `count_param` rows). The blocker is closed by **removal, not repair** — no DTW-matrix edit is ever produced.
   - **Conforms to `_grouping.json`**: shared contract *DEAD-METHOD OWNERSHIP* and the Group A note both mandate that BUG-08 drop the hasonlit/tavtomb/ertomb cap and ship only the `dtwApp_match.cs` `costRecord` fix, because **BUG-16 (Group B, file-disjoint)** deletes `hasonlit` and its backing fields. BUG-08 now edits no `H_FELDOLGOZO.cs`.

2. **§2 Change 2 text reworded** — removed the false claim that re-enabling the commented FIX-IT fill "becomes safe with no further change"; added the caveat that the fill also reads `pathRecord` (width `1`) and so additionally requires the deferred Change 2b.
   - **Closes the Peer-review Secondary (non-blocking) item** ("plan text contradicts itself on Change 2"). Change 2 itself is unchanged (still correct/complete for sizing `costRecord`).

3. **§6 self-verification rewritten** — deleted H_FELDOLGOZO steps 5–8 (including the false step-7 "✔" that the review flagged) since Change 3 is gone; added a line-drift guard (BUG-08 runs 4th in Group A's `dtwApp_match.cs` sequence, so match edit sites by snippet text, not line number) and a non-change guard asserting the BUG-08 diff contains no `H_FELDOLGOZO.cs` hunk.
   - **Closes the Peer-review step-7 "✔" item** and conforms to the Group A internal order `01 → 09 → 10 → 08 → 11`.

4. **Conformance scaffolding added** (header + §5): declared group **A-dtw-engine**, the internal order, the commit chunk (`fix(dtw): unify DTW feature-vector width … and add rejection sentinel`), and a clarification that the Group A WIDTH CONTRACT does not bind BUG-08 because `costRecord` is frame-indexed (independent of vector width). Removed the now-moot §5 sentence about moving `tavtomb/ertomb` allocation to per-call.

**Unchanged (already correct, peer-review-approved):** Changes 1 + 2 verbatim; Change 2b deferred; §1C non-change of `num_items_in_windowed_frame = 256`; §4 "no on-disk format impact" conclusion. **Carried-over deferrals** from `_grouping.json`: none owned by BUG-08 (BUG-12 serialization is sequential group S1; BUG-10 calibration, BUG-13, BUG-14 deferred — all out of BUG-08 scope).

---

## Re-review 2026-06-27

**Verdict: APPROVED (approved=true).** Independent re-review against the live working tree on `fix/roadmap-bugs` (source re-read, not trusting the plan's own claims). Every previously-blocking issue is genuinely resolved, no new defect was introduced, and the plan is consistent with `plans/_grouping.json` and Group A's scope.

### (1) Previously-blocking issue resolved
- The peer-review BLOCKER was Change 3 shipping a guaranteed `IndexOutOfRangeException` in `hasonlit` (unbounded column-0 `seged1+1` walk losing its `-1` sentinel once the buffer is exactly `count_param` rows). **Change 3 is now DROPPED entirely**, not repaired, so no buggy DTW-matrix edit is ever produced. This is the cleanest possible closure. Confirmed the drop is reflected consistently across §1B, §2, §3 table, §4, §5, §6, and the Scope summary — no residual instruction anywhere to resize `tavtomb/ertomb` or touch any `255`/`256` literal.
- The secondary self-contradiction (Change 2 text claiming the FIX-IT fill "becomes safe with no further change") is fixed: §2 now correctly states the fill additionally needs Change 2b because it reads `pathRecord` (still width 1). Verified in source: the commented fill at LITE `:597-601` does read `pathRecord[templateIndex, i]`, and `pathRecord = new int[num_of_templates, 1]` at `:146/:163` — so the caveat is accurate.

### (2) No new defect / off-by-one / integer-division / vector-width / duplicated-copy omission
- **Live-source line refs confirmed identical in both copies:** `costRecord = new double[120]` at `:147` and `:164`; field decl `public double[] costRecord;` at `:121`; `int[] temppath = new int[testlength + 1]` at `:534`; `minX = testlength - 1` at `:537`. Tree-wide grep finds `[120]` at exactly those 4 sites and nowhere else.
- **Size contract verified end-to-end:** `lefttorightMatch:378 I = signal.getRowLength()` → `backTrace(path, cost, I, J)` at `:522/:524` → `backTrace`'s `testlength == I`. `costRecord = new double[testlength]` then makes `costRecord[i]` for the commented fill's `i ∈ [0, testlength)` strictly in range (`Length == testlength`). No off-by-one. `new double[0]` for an empty signal is legal C# and the fill would not execute.
- **No integer division, no vector-width coupling:** Change 2 introduces only `new double[testlength]`; `testlength` is an `int` parameter, no division. `costRecord` is frame-indexed and never reads `mfcc_lpc_vect_num`/`GetLength(1)`, so it is correctly independent of the Group A WIDTH CONTRACT — confirmed by grep (costRecord appears only in dtwApp_match.cs + one commented Form1.cs line).
- **`costRecord = null` in constructors is safe:** the only external reader (`Form1.cs:524`) is commented; `TotalCost` returns the separate `totalCost` field; nothing reads `costRecord` between construction and `backTrace` on any live path (reference==null guards return before any read). No NRE risk on a live path.
- **Both duplicated copies covered;** `Turan_tester` has no `dtwApp_match.cs`. No omission.
- **Allocation precedes use:** `costRecord` is allocated at the top of `backTrace` (right after `temppath`), before the later (commented) fill in the same method. Correct ordering.

### (3) Consistency with shared contracts and group scope
- `H_FELDOLGOZO.cs` appears **only** in Group B's file list; BUG-08 (Group A) edits no `H_FELDOLGOZO.cs`. The DEAD-METHOD OWNERSHIP contract explicitly mandates BUG-08 drop the hasonlit cap and ship only the `dtwApp_match.cs costRecord` fix — the plan now matches that verbatim. All `hasonlit` callers are commented (dead), owned by BUG-16.
- BUG-08's edit sites (`:147/:164/:534`) are disjoint from BUG-01/09/10's sites (EuclideanDistance ~235, temp3/temp4 466/467, ITDDistance 255–352, num_of_vectoritems ~84); the plan correctly instructs snippet-text matching given line drift (BUG-08 runs 4th in Group A). Note the two copies differ at the non-edit-site line 84 (`Engine.` vs `H_FELDOLGOZO.mfcc_lpc_vect_num`); this is outside BUG-08's scope and does not affect the byte-identical edit sites.
- No on-disk format impact (costRecord is unserialized scratch); orthogonal to BUG-12/S1. `num_items_in_windowed_frame = 256` correctly left untouched.

### Remaining issues
None blocking. Minor, optional: §6 step 2 says "the only non-comment occurrences should be ... the two `= null` initializers" — harmless cosmetic phrasing (a `null` literal is not strictly a buffer "occurrence"), no code impact. Safe to implement as written.

---

## Re-review 2026-06-27 (independent, second pass)

**Verdict: APPROVED (approved=true).** Independent re-verification of the *revised* plan against the live working tree on `fix/roadmap-bugs`. Source re-read directly (grep + sed); the plan's own claims and the prior Re-review section were not trusted. The plan is correct, surgical, and safe to implement.

### (1) Previously-blocking issues genuinely resolved
- **BLOCKER (Change 3 → guaranteed `IndexOutOfRangeException` in `hasonlit`) is closed by removal.** Change 3 is dropped everywhere (§1B, §2, §3 table, §4, §5, §6, Scope summary) — no residual instruction to resize `tavtomb/ertomb` or touch any `255`/`256` literal remains. Independently confirmed `hasonlit`/`tavtomb`/`ertomb` live only in `H_FELDOLGOZO.cs` (both copies) and LITE `Form1.cs` — all in Group B's file set, none in Group A's. BUG-08 produces no `H_FELDOLGOZO.cs` hunk.
- **Secondary (Change 2 self-contradiction) is fixed.** §2 now correctly states the commented FIX-IT fill additionally needs Change 2b because it reads `pathRecord` (still width 1). Verified against source: the fill at LITE `:597-601` reads `pathRecord[templateIndex, i]`, and `pathRecord = new int[num_of_templates, 1]` at `:146/:163`.

### (2) No new defect introduced
- **Edit sites confirmed live and identical in both copies:** tree-wide `grep "\[120\]"` returns exactly 4 hits — `dtwApp_match.cs:147` and `:164` in each copy, nowhere else. `diff` of the constructor region (118–170) and the backTrace anchor (527–535) between the two copies: **identical**. Field decl `public double[] costRecord;` at `:121`; `int[] temppath = new int[testlength + 1]` at `:534`; `minX = testlength - 1` at `:537` — all confirmed byte-identical in both copies.
- **Size contract end-to-end, no off-by-one:** `lefttorightMatch:378 I = signal.getRowLength()` → `backTrace(path, cost, I, J)` at `:522` → `testlength == I`. The trace loop is `for (i = minX; i >= 0; i--)` with `minX = testlength - 1`, so frame indices span `[0, testlength)`. `costRecord = new double[testlength]` (Length `testlength`) makes the commented fill's `costRecord[i]`, `i ∈ [0, testlength)`, strictly in range. No off-by-one.
- **No integer division, no vector-width coupling:** Change 2 adds only `new double[testlength]`; `testlength` is an `int` parameter (no division). `costRecord` is frame-indexed and never reads `mfcc_lpc_vect_num`/`GetLength(1)` — confirmed `costRecord` appears only in `dtwApp_match.cs` plus one commented `Form1.cs:524` line — so it is correctly independent of the Group A WIDTH CONTRACT.
- **`costRecord = null` in constructors is safe:** grep confirms no live reader anywhere (only field decl, the two allocations, the `backTrace` allocation, and commented lines). `TotalCost` returns the separate `totalCost` field. No NRE on any live path. `new double[0]` for an empty signal is legal and the fill would not execute.
- **Insertion point valid:** Change 2 places the allocation after `int temppathlength = 0;` inside `backTrace`, where `testlength` (the parameter) is in scope and `costRecord` is allocated before the later commented fill in the same method. Correct ordering.

### (3) Consistent with shared contracts and group scope
- **File-disjointness holds:** Group A files = the two `dtwApp_match.cs` + `Engine.cs` + `lpcData.cs`; `H_FELDOLGOZO.cs` is in Group B only. BUG-08 touches only `dtwApp_match.cs`. Matches the DEAD-METHOD OWNERSHIP contract (BUG-08 ships only the `costRecord` fix; BUG-16/Group B deletes `hasonlit`).
- **Duplicate parity / inventory:** exactly two `dtwApp_match.cs` copies exist; `Turan_tester` has none. Both copies are BUG-08 targets and both are addressed. No omission.
- **Line-drift guidance correct:** BUG-08 runs 4th in Group A's `dtwApp_match.cs` order; the plan correctly mandates snippet-text matching. The `costRecord` sites (147/164/534) are disjoint from BUG-01/09/10's regions.
- **No on-disk format impact:** `costRecord` is unserialized scratch — orthogonal to BUG-12/S1. `num_items_in_windowed_frame = 256` correctly left untouched (legitimate window length).

### Remaining issues
None blocking. The two cosmetic nits already noted by the prior pass stand (harmless §6 phrasing). Safe to implement as written.
