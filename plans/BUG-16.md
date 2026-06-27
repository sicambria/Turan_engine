# Fix Plan — BUG-16: Dead/commented native DTW (`hasonlit`) retained alongside active `dtwApp_match`

- **Severity:** P3 (cleanup / dead-code removal; no runtime behavior change)
- **Files in scope (per ROADMAP):**
  - `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs`
  - `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`
  - `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/Form1.cs`

---

## 1. Root cause (restated from the code)

`H_FELDOLGOZO.hasonlit(ref double[,] parameter, int count_param)` is an early, hand-rolled
native DTW distance routine (squared-Euclidean local cost in `tavtomb`, accumulated cost in
`ertomb`, classic 3-neighbor min recursion). It was superseded by the active DTW implementation
`dtwApp_match` (`Turan_core/Turan_core/dtwApp_match.cs` and
`Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs`), which is the one actually instantiated
in the recognition path:

- `Turan_core/Turan_core/Engine.cs:89` and `:124` — `new dtwApp_match(win_signal_data)`
- `Felismero_motor_LITE/Felismero_motor/Form1.cs:477` — `new dtwApp_match(win_lpcdata)`

Evidence that `hasonlit` is dead:

- Repo-wide `grep -rn "hasonlit" --include=*.cs .` returns exactly **five** hits:
  - the two method **definitions** (`Turan_creator/.../H_FELDOLGOZO.cs:294`,
    `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:330`);
  - three **commented-out** call sites, all inside a single commented-out method
    `CalcDTWDistances()` in `Felismero_motor_LITE/Felismero_motor/Form1.cs`
    (lines 1472, 1491, 1505).
- There is **no uncommented caller** of `hasonlit` anywhere in the solution.
- The supporting static state declared immediately above `hasonlit`
  (`tavtomb`, `ertomb`, `refcounters`, `refcounter1`, `counter_ref`, `mfccref1`, `mfccref2`,
  `mfccref3`, `referencia`, `Refs`, `mfccrefs`) is referenced **only** by `hasonlit` and by the
  same commented-out `CalcDTWDistances()` block. (Several — `refcounters`, `refcounter1`,
  `mfccref1/2/3`, `Refs`, `mfccrefs` — are not even read by `hasonlit`; they are pre-existing
  orphan state.) Active-code grep for every one of these identifiers returns only their
  declarations in the two `H_FELDOLGOZO.cs` copies.

So we have a dead public method plus its dead backing fields, kept alongside a commented-out
driver that called it. Removing all of it is a pure cleanup.

**Out of scope / explicitly KEEP** (these live in the same files but are used by active code):
- `H_FELDOLGOZO.mfcc_lpc_vect_num` (line 36) and `H_FELDOLGOZO.num_items_in_windowed_frame`
  (line 37) — referenced throughout the active pipeline (`Form1.cs:104/108`, `Creator.cs:142/156`,
  `dtwApp_match`, etc.). Do **not** touch.
- `H_FELDOLGOZO.Show2dArray(...)` (both the `double[,]` and `int[,]` overloads) — present **only in
  the Felismero copy** (`Felismero_motor_LITE/.../H_FELDOLGOZO.cs:254` and `:274`), actively used at
  `Form1.cs:532`. Do **not** touch. **Note:** `Turan_creator/.../H_FELDOLGOZO.cs` contains **no**
  `Show2dArray` (and no `ShowArray`) at all — in that copy the method directly above the removed
  block is `mfccszamitas` (ends line 275). This asymmetry matters for the File-A self-check below.

---

## 2. Exact change per file (before / after)

### File A — `Turan_creator/Turan_creator/H_FELDOLGOZO.cs`

Delete the contiguous region from the orphan field block through the end of `hasonlit`,
**lines 279–373** inclusive (the field declarations on 279–291, the `// itt a paraméter a "mivel" :`
comment on 292, the method on 294–371, and the trailing `// end Comp` comment on 373).
Keep the three blank lines on 276–278 (source has `}` ending `mfccszamitas` on 275, then blanks on
276/277/278) and the class/namespace closing braces on 376–377.

**Before** (anchor — start of region, line 279, and end of region, line 373):
```csharp
        public static double[,] tavtomb = new double[256, mfcc_lpc_vect_num];
        public static double[,] ertomb = new double[256, mfcc_lpc_vect_num];
        public static int[] refcounters = new int[256];

        public static int refcounter1 = 0;
        public static int counter_ref = 0;
        public static double[] mfccref1;
        public static double[] mfccref2;
        public static double[] mfccref3;
        public static double[,] referencia;

        public static byte Refs = 0;
        public static double[] mfccrefs = new double[256];
        // itt a paraméter a "mivel" :

        public static int hasonlit(ref double[,] parameter, int count_param)
        {
            // ... full body, lines 296–370 ...
            return result;
        }

        // end Comp
```

**After** (region removed; in this copy the class now ends right after `mfccszamitas`, which ends
at line 275 — `Turan_creator/.../H_FELDOLGOZO.cs` has **no** `Show2dArray`/`ShowArray`):
```csharp
        // (native hasonlit DTW and its backing state removed — superseded by dtwApp_match; see BUG-16)


    }
}
```
(The replacement comment line is an optional breadcrumb; the only functional requirement is that
lines 279–373 are gone and the file still closes the class `}` and namespace `}`.)

### File B — `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`

Same change, different line numbers. Delete the orphan field block + `hasonlit`,
**lines 315–409** inclusive (fields 315–327, `// itt a paraméter a "mivel" :` comment 328,
method 330–407, `// end Comp` comment 409). Keep blank lines 313–314 and the class/namespace
closing braces 412–413.

**Before** (anchor — line 315 start, line 409 end):
```csharp
        public static double[,] tavtomb = new double[256, mfcc_lpc_vect_num];
        public static double[,] ertomb = new double[256, mfcc_lpc_vect_num];
        public static int[] refcounters = new int[256];

        public static int refcounter1 = 0;
        public static int counter_ref = 0;
        public static double[] mfccref1;
        public static double[] mfccref2;
        public static double[] mfccref3;
        public static double[,] referencia;

        public static byte Refs = 0;
        public static double[] mfccrefs = new double[256];
        // itt a paraméter a "mivel" :

        public static int hasonlit(ref double[,] parameter, int count_param)
        {
            // ... full body, lines 332–406 ...
            return result;
        }

        // end Comp
```

**After** (region removed):
```csharp
        // (native hasonlit DTW and its backing state removed — superseded by dtwApp_match; see BUG-16)


    }
}
```

> NOTE: Files A and B are byte-for-byte identical in this region except for line numbers and a
> couple of unrelated helpers above. The deleted text is the same in both — this is one of the
> duplicated-file situations described in the project brief. Apply the identical removal to both.

### File C — `Felismero_motor_LITE/Felismero_motor/Form1.cs`

Remove the entire commented-out `CalcDTWDistances()` method, **lines 1433–1506** inclusive
(`//private void CalcDTWDistances()` through its closing `//}`). This is the only place that
referenced `hasonlit` outside the definitions, and it also sets `H_FELDOLGOZO.referencia` /
`H_FELDOLGOZO.counter_ref` (lines 1462, 1466) — fields being removed in Files A/B, so they must
go together. Keep the unrelated commented FFT block above (ends at 1429) and the unrelated
commented `notifyIcon1_MouseDoubleClick` / `aboutToolStripMenuItem_Click` / etc. blocks below
(start at 1510).

**Before** (anchor — first and last lines of the block to delete):
```csharp
        //private void CalcDTWDistances()
        //{
        //    if (!dtw_ref_ready)
        //    {
        //        MessageBox.Show("Referencia nincs betöltve!");
        //        return;
        //    }
        //    ... (sets H_FELDOLGOZO.referencia, H_FELDOLGOZO.counter_ref;
        //         calls H_FELDOLGOZO.hasonlit(...) at lines 1472, 1491, 1505) ...
        //    //lb_dtw_data.Items.Add(H_FELDOLGOZO.hasonlit(ref ref_array, ref_array.Length).ToString());
        //}
```

**After:** the whole block (lines 1433–1506) is deleted. Leave surrounding blank lines tidy.

> This is the *only* of the three named files where a `Form1.cs` copy is touched. The other
> `Form1.cs` files in the solution (`Turan_SC_minimal/.../Form1.cs`,
> `Turan_tester/.../Form1.cs`) contain **no** `hasonlit` reference (verified by grep) and must
> **not** be modified.

---

## 3. All duplicated copies needing the same change

| Logical edit | Concrete locations |
|---|---|
| Remove `hasonlit` method + its orphan backing field block | `Turan_creator/Turan_creator/H_FELDOLGOZO.cs` (lines 279–373) **and** `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` (lines 315–409) |
| Remove commented `CalcDTWDistances()` driver (the only `hasonlit` call sites) | `Felismero_motor_LITE/Felismero_motor/Form1.cs` (lines 1433–1506) |

There are exactly **two** tracked `H_FELDOLGOZO.cs` copies (`git ls-files` confirms — Turan_core
has no `H_FELDOLGOZO.cs`; it uses `Engine.cs`). Both must be edited. `Turan_core` and the other
projects need no change for this bug.

---

## 4. Backward-compatibility / on-disk format impact

**None.** `hasonlit` is a pure in-memory routine over `double[,]` arrays. It does **not** read or
write any template (`.mfcc`/`.lpc`), serialization, or settings file. The active on-disk template
format is produced/consumed by `SerializeArray` / `DeSerializeArray` and `dtwApp_match`, none of
which are touched here. Removing the dead method and its private static state changes no public
data contract that is exercised at runtime, and changes no file format. Existing template
databases remain fully usable. No versioned-read shim is required.

(The removed fields are `public static`, so they are technically part of the type's surface, but
since nothing in the solution references them, removing them cannot break any caller within this
repo. There is no external consumer of this assembly.)

---

## 5. Grouping & shared-contract dependencies with other bug fixes

Per `plans/_grouping.json`, BUG-16 is **not** a free-floating independent fix; it has a defined
place and two binding cross-bug contracts. (The earlier "can land in any order" framing is
**superseded** by the grouping decisions below.)

**Group membership & internal order.** BUG-16 belongs to **Group B (`B-dsp-features`)** — files
`Turan_creator/.../H_FELDOLGOZO.cs`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs`,
`Turan_creator/.../Creator.cs`, `Felismero_motor_LITE/.../Form1.cs`. BUG-16's three target files are
all inside this set (Creator.cs is **not** touched by BUG-16). The mandated **internal order is
BUG-16 FIRST**, then 04 → 02 → 05 → 06 → 03 → 07. Because BUG-16 runs first it operates on pristine
source, so the line ranges in §2/§3 (verified against the current tree) remain valid as written;
and because BUG-16 only deletes regions physically **below** `mfccszamitas` (≤275) and the
FIR/window/DCT methods, it does not shift the line numbers that the later Group-B bugs (02/04/05/06)
target above it.

**Contract 1 — DEAD-METHOD OWNERSHIP (cross A↔B coordination, no file overlap).** BUG-16 (Group B)
**owns** removal of `hasonlit` + its backing fields from both `H_FELDOLGOZO.cs` copies *and* the
commented `CalcDTWDistances()` driver in LITE `Form1.cs`. Consequently **BUG-08 (Group A) drops its
`hasonlit`/`tavtomb`/`ertomb` cap edit (its old "Change 3")** and ships only the `dtwApp_match.cs`
`costRecord` fix (Changes 1+2). This is a **scope partition**, not a runtime ordering dependency:
Groups A and B are file-disjoint and run **in parallel**; A simply must not edit `H_FELDOLGOZO.cs`
because B deletes the relevant code there.

**Contract 2 — H_FELDOLGOZO DUPLICATE PARITY (Group B).** The BUG-16 deletion must be applied to
**both** `H_FELDOLGOZO.cs` copies byte-identically in the edited region (Files A and B here), so the
template-builder and live-extractor DSP stay bit-identical. Diff the two copies' edited regions to
confirm parity after the cut.

**Commit chunk.** BUG-16 lands in the Group-B commit
`fix(dsp): restore pre-emphasis, fix FIR/window/DCT-scale, remove dead native DTW; clarify
log-mel labeling` (bugIds BUG-02/03/04/05/06/16), alongside the other DSP fixes.

**What BUG-16 still does NOT touch** (unchanged from before):
- the `SerializeArray` (Creator) / `DeSerializeArray` (Engine) serialization contract — that is
  BUG-12's territory in sequential group **S1**, which runs only after A/B/C merge. BUG-16's
  deletion of the commented `CalcDTWDistances()` block in LITE `Form1.cs` (lines 1433–1506) is in a
  disjoint region from BUG-12's writer/reader edits in the same file, so the two do not collide;
- the feature-vector dimension constants `mfcc_lpc_vect_num` and `num_items_in_windowed_frame`
  (kept verbatim);
- `dtwApp_match` (the active DTW), `Show2dArray`/`ShowArray`, or any MFCC/LPC extraction code.

---

## 6. Self-verification without a compiler

Do all of the following by reading/grepping (no build available):

1. **Dead method fully gone:**
   `grep -rn "hasonlit" --include=*.cs .` → must return **0** hits after the edits
   (was 5: 2 defs + 3 commented calls).

2. **Orphan fields fully gone (no dangling references):** for each removed identifier
   `grep -rn "\btavtomb\b\|\bertomb\b\|\brefcounters\b\|\brefcounter1\b\|\bcounter_ref\b\|\bmfccref1\b\|\bmfccref2\b\|\bmfccref3\b\|\breferencia\b\|\bmfccrefs\b" --include=*.cs .`
   → must return **0** code hits. (Note: the string literals "Fő referencia:" /
   "...referencia adatbázist." in `Form1.Designer.cs` are UI text, not the field — they are
   unaffected and may still appear; confirm any remaining `referencia` hit is inside a quoted
   string, not an identifier.) Also confirm `\bRefs\b` returns 0 code hits.

3. **Kept symbols still present and intact:**
   - `grep -n "mfcc_lpc_vect_num\s*=" H_FELDOLGOZO.cs` still shows line 36 in both copies.
   - `grep -n "num_items_in_windowed_frame\s*=" H_FELDOLGOZO.cs` still shows line 37 in both.
   - `grep -n "Show2dArray" Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` still shows
     both overloads; `Form1.cs:532` call is untouched.

4. **Active DTW path untouched:**
   `grep -rn "dtwApp_match" --include=*.cs .` → unchanged set of hits
   (`Engine.cs:89/124`, `Form1.cs:451/477`, plus the two `dtwApp_match.cs` files).

5. **Brace/structure balance per edited file:** after each edit, eyeball that the file still has its
   closing class `}` and namespace `}`. **The landmark differs per copy:**
   - **File A (`Turan_creator/.../H_FELDOLGOZO.cs`):** the class body now ends right after
     `mfccszamitas` (ends line 275). This copy has **no** `Show2dArray`/`ShowArray` — do **not**
     look for them here.
   - **File B (`Felismero_motor_LITE/.../H_FELDOLGOZO.cs`):** the method **immediately** above the
     removed block is `ShowArray` (one-dim overload, ends line 312); the two `Show2dArray` overloads
     (lines 254/274) and `ShowArray` are all **retained** above the cut.

   For `Form1.cs`, confirm the deletion sits cleanly between the commented FFT block
   above (ends ~1429) and the commented `notifyIcon1_MouseDoubleClick` block below (starts ~1510),
   with no stray opening/closing brace left behind. Optionally run
   `grep -c "{" file && grep -c "}" file` before/after to confirm the per-file `{`/`}` counts
   each drop by the same amount.

6. **No new references created:** confirm the replacement breadcrumb comment (if added) contains
   no code, so it cannot affect compilation.

---

## Summary

Pure dead-code removal: delete the `hasonlit` native DTW method plus its exclusively-used static
backing fields from **both** `H_FELDOLGOZO.cs` copies, and delete the commented-out
`CalcDTWDistances()` driver (its only call sites) from `Felismero_motor_LITE/.../Form1.cs`.
No runtime behavior change, no data/template/serialization format change, no backward-compat
concern, and no shared contract that other fixes depend on. Verifiable entirely by grep.

---

## Peer review

**Reviewer verdict: approved = false** — the *executable* removal is correct and complete, but the
plan's verification narrative for File A (`Turan_creator/.../H_FELDOLGOZO.cs`) is factually wrong in
a way that matters for this no-compiler, grep-only project, where the self-checks are the only
safety net. One small documentation correction is required before implementing.

### What I verified (holds — do not reopen)

- **Edit instructions are exact and correct.** Line ranges and anchors match the real source byte
  for byte:
  - File A delete **279–373** (anchor `public static double[,] tavtomb …` → `// end Comp`); after
    deletion lines 374–377 (two blanks, class `}`, namespace `}`) remain — balanced.
  - File B (`Felismero_motor_LITE/.../H_FELDOLGOZO.cs`) delete **315–409** (same anchors); 410–413
    remain — balanced.
  - File C (`Felismero_motor_LITE/.../Form1.cs`) delete **1433–1506** (`//private void
    CalcDTWDistances()` → `//}`); the whole block is commented, so no brace imbalance is possible.
- **Completeness across duplicated copies.** `git ls-files` shows exactly **two** `H_FELDOLGOZO.cs`
  copies (Turan_core has none — it uses `Engine.cs`/`dtwApp_match`). Repo-wide `grep hasonlit` =
  exactly **5** hits (2 defs + 3 commented calls in the single `CalcDTWDistances()` block). No
  untracked copy. The `Form1.cs` copies in `Turan_SC_minimal`/`Turan_tester` contain no `hasonlit`
  and are correctly left alone.
- **Orphan fields are truly dead.** Every removed identifier (`tavtomb`, `ertomb`, `refcounters`,
  `refcounter1`, `counter_ref`, `mfccref1/2/3`, `referencia`, `Refs`, `mfccrefs`) is referenced only
  inside `hasonlit` itself and inside the commented `CalcDTWDistances()` block. The two `referencia`
  hits in `Form1.Designer.cs` (345/458) are UI string literals ("Fő referencia:", "…referencia
  adatbázist.") — the plan already anticipates this in step 2. Accurate.
- **No cross-bug contract breakage.** `mfccszamitas` (BUG-03/BUG-04 territory, incl. the
  `Math.Sqrt(2/24)` integer-division line) sits at 222–275 — disjoint from the 279–373 deletion —
  and references none of the removed fields. `mfcc_lpc_vect_num` / `num_items_in_windowed_frame`
  (line 36/37) are kept. No conflict with BUG-01/03/04/08.
- **Backward-compat / format.** `hasonlit` is pure in-memory; the removed `public static` fields are
  not part of any `BinaryFormatter` payload (only `double[,]` arrays passed to
  `SerializeArray`/`DeSerializeArray` are). The "no on-disk format change, no backward-compat
  concern" claim is correct.

### Issue requiring change (the reason for approved = false)

1. **File A does not contain `Show2dArray`; the deleted region is preceded by `mfccszamitas`, not
   `Show2dArray`.** `Show2dArray` (both overloads, used at `Form1.cs:532`) exists **only** in the
   Felismero copy (lines 254/274). In `Turan_creator/.../H_FELDOLGOZO.cs` the method immediately
   above the removed block is `mfccszamitas` (222–275); after deleting 279–373 the class ends right
   after `mfccszamitas`. Three places state or imply otherwise and would mislead an implementer's
   File-A self-check:
   - **§2 File A "After"** snippet: "the class now ends right after `Show2dArray`" → should read
     "the class now ends right after `mfccszamitas` (which ends at line 275)".
   - **§1 KEEP note** about `Show2dArray` ("both the `double[,]` and `int[,]` overloads … actively
     used at `Form1.cs:532`") → scope it to the **Felismero copy only**; note Turan_creator has no
     `Show2dArray`.
   - **§6 step 5**: "eyeball that the class body ends right after `Show2dArray`" → for File A this
     landmark is `mfccszamitas`; keep `Show2dArray` only as the File-B landmark. (§6 step 3's
     `Show2dArray` grep is already correctly scoped to the Felismero path — leave it.)

### Required changes

- Correct §2 (File A "After"), §1 (KEEP note), and §6 step 5 so that File A's removed region is
  described as preceded by `mfccszamitas` (ends line 275), and `Show2dArray` is referenced only for
  the Felismero copy.

No change to the line ranges, anchors, or scope is needed — only the structural/verification prose.
With that one correction the plan is sound and safe to implement.

---

## Revision 2026-06-27

Re-verified every target region against the live tree (all duplicated copies) and revised the plan
to resolve the peer-review blocking issue and to conform to `plans/_grouping.json`. Line ranges,
anchors, and scope are unchanged — only structural/verification prose and the grouping section were
corrected.

**Source re-verification (this revision):**
- File A `Turan_creator/.../H_FELDOLGOZO.cs`: dead block confirmed at **279–373**; method directly
  above is `mfccszamitas` (222–**275**); **no** `Show2dArray`/`ShowArray` in this file; tail braces
  at 376–377. The BUG-04 target (`Math.Sqrt(2/24)`) is line **272**, inside `mfccszamitas`, above
  the deletion — unaffected.
- File B `Felismero_motor_LITE/.../H_FELDOLGOZO.cs`: dead block confirmed at **315–409**; method
  directly above is `ShowArray` (ends **312**); `Show2dArray` overloads at **254/274** (kept); tail
  braces at 412–413.
- File C `Felismero_motor_LITE/.../Form1.cs`: commented `CalcDTWDistances()` confirmed at
  **1433–1506**, between the commented FFT block (ends 1429) and the commented
  `notifyIcon1_MouseDoubleClick` block (starts 1510). `Show2dArray` use at `Form1.cs:532` confirmed
  (untouched). Repo-wide `grep hasonlit` = exactly 5 hits (2 defs + 3 commented calls); `git ls-files`
  = exactly 2 `H_FELDOLGOZO.cs` copies.

**Changes and the blocking issue each closes:**

1. **§1 KEEP note** — scoped `Show2dArray` to the **Felismero copy only** (lines 254/274) and added
   that `Turan_creator/.../H_FELDOLGOZO.cs` has **no** `Show2dArray`/`ShowArray`, so its post-cut
   landmark is `mfccszamitas` (ends 275).
   *Closes:* peer-review blocking issue (1), bullet 2 (the §1 KEEP-note inaccuracy).

2. **§2 File A "After"** — changed "the class now ends right after `Show2dArray`" to "ends right
   after `mfccszamitas` (line 275)", with an explicit "this copy has no `Show2dArray`/`ShowArray`"
   note.
   *Closes:* peer-review blocking issue (1), bullet 1 (the §2 File-A "After" inaccuracy).

3. **§6 step 5** — split the brace/structure self-check per copy: File A landmark = `mfccszamitas`
   (ends 275, no Show2dArray here); File B landmark = `ShowArray` (immediately above the cut, ends
   312), with `Show2dArray` (254/274) and `ShowArray` retained. This is **more precise than the
   reviewer's own suggested fix**, which named `Show2dArray` as the File-B landmark — primary-source
   reading shows the method *immediately* above File B's deleted block is `ShowArray`, not
   `Show2dArray`. §6 step 3's `Show2dArray` grep (already scoped to Felismero) was left untouched as
   the reviewer directed.
   *Closes:* peer-review blocking issue (1), bullet 3 (the §6 step-5 inaccuracy). With items 1–3 the
   sole reason for `approved = false` is fully resolved.

4. **§5 retitled "Grouping & shared-contract dependencies" and rewritten** — the prior claim
   "BUG-16 is independent and can land in any order" **contradicted `plans/_grouping.json`** and was
   replaced with the grouping's actual decisions: Group **B (`B-dsp-features`)** membership; internal
   order **BUG-16 first**; **DEAD-METHOD OWNERSHIP** contract (BUG-16 owns the hasonlit/backing-field/
   driver removal, so **BUG-08 in Group A drops its hasonlit-cap edit** and ships only the
   `dtwApp_match.cs` `costRecord` fix — a *scope partition* between file-disjoint, parallel groups,
   not a runtime ordering dependency); **H_FELDOLGOZO DUPLICATE PARITY** contract (both copies cut
   byte-identically, diff to confirm); and the Group-B commit-chunk membership. Also clarified that
   BUG-16's Form1.cs deletion (1433–1506) is in a region disjoint from BUG-12's S1 serialization
   edits, so they do not collide.
   *Closes:* the grouping-conformance requirement of the task (group, internal order, shared
   contracts, deferrals/scope cuts). This was a task requirement distinct from the peer review.

**Untouched (per reviewer "do not reopen" and to avoid overreach):** line ranges/anchors/scope, the
§2 "Before" snippets, §6 step 3's grep, the orphan-field deadness analysis, backward-compat (§4),
and completeness-across-copies. The `approved = false` verdict line is left as the reviewer's record;
this Revision section is the resolution mechanism.

---

## Re-review 2026-06-27

**Verdict: approved = true.** Independently re-verified against the live tree (not the plan's own
claims) and against `plans/_grouping.json`. The sole blocking issue from the prior peer review (the
File-A `Show2dArray` inaccuracy) is genuinely resolved, no new defect was introduced, and the plan
conforms to its Group-B scope and the shared contracts.

**(1) Previously-blocking issue resolved.** The peer review blocked on §2/§1/§6 wrongly implying
`Turan_creator/.../H_FELDOLGOZO.cs` contains `Show2dArray`. Re-read of the live file confirms it does
**not**: the method directly above the deletion is `mfccszamitas`, which ends at **line 275** (`}` on
275). The Revision corrected §1 (KEEP note scoped to the Felismero copy), §2 File-A "After" (now
"ends right after `mfccszamitas` (line 275)"), and §6 step 5 (File-A landmark = `mfccszamitas`,
File-B landmark = `ShowArray`). All three corrections match source. The plan's claim that File-B's
method immediately above the cut is `ShowArray` (ends **312**) — more precise than the reviewer's own
`Show2dArray` suggestion — is also confirmed (line 312 closes the one-dim `ShowArray` overload;
`Show2dArray` overloads sit higher at 254/274 and are retained).

**(2) No new defect / off-by-one / duplicated-copy omission.**
- Line ranges re-confirmed byte-exact: File A delete **279–373** (`tavtomb` decl → `// end Comp`),
  tail braces 376–377; File B delete **315–409** (same anchors), tail braces 412–413; File C delete
  commented `CalcDTWDistances()` **1433–1506** (`//private void CalcDTWDistances()` → `//}`), between
  the commented FFT block ending 1429 and the commented `notifyIcon1_MouseDoubleClick` starting 1510.
- Deadness independently re-grepped: `referencia`, `counter_ref`, `tavtomb`, `ertomb`, `refcounters`,
  `refcounter1`, `mfccref1/2/3`, `Refs`, `mfccrefs` are referenced **only** inside the regions being
  removed (the two `hasonlit` bodies and the commented `CalcDTWDistances()` driver, incl. its
  `H_FELDOLGOZO.ertomb`/`counter_ref`/`referencia` lines) plus the Designer.cs UI string literals.
  Zero live references survive the cut — no dangling reference, no compile hazard.
- `hasonlit` repo-wide grep = exactly 5 hits (2 defs + 3 commented calls); `git ls-files` = exactly 2
  `H_FELDOLGOZO.cs` copies. Both copies are in scope; the cut is byte-identical in the edited region
  (satisfies H_FELDOLGOZO DUPLICATE PARITY). No untracked copy; the `Form1.cs` copies in
  `Turan_SC_minimal`/`Turan_tester` carry no `hasonlit` and are correctly excluded.
- No vector-width or integer-division surface here: BUG-16 is a pure deletion. The kept width
  constants `mfcc_lpc_vect_num`/`num_items_in_windowed_frame` (lines 36/37) are untouched; the BUG-04
  `Math.Sqrt(2/24)` integer-division line is at **272** inside `mfccszamitas` (above the cut) and is
  correctly left for BUG-04. The removed Form1.cs block's `win_REF_meldata.Length / 15` is dead
  commented code, not introduced logic.
- All deletions are at the bottom of each region (File A/B ≥279/≥315; File C commented), so they do
  not shift the line numbers the other Group-B bugs target above them — consistent with "BUG-16 first"
  being safe.

**(3) Consistent with shared contracts and group scope.** Group **B** membership and internal order
"**16 first** → 04 → 02 → 05 → 06 → 03 → 07" match `_grouping.json`. BUG-16 touches only the two
`H_FELDOLGOZO.cs` copies and LITE `Form1.cs` (not Creator.cs), staying inside Group B's four files.
The **DEAD-METHOD OWNERSHIP** contract is honored (BUG-16 owns the `hasonlit`/backing-field/driver
removal; BUG-08 in Group A correspondingly drops its cap edit — also reflected in `_grouping.json`'s
Group-A note and sharedContracts). The Form1.cs deletion (1433–1506) is disjoint from BUG-12's S1
serialization writer/reader edits, so no collision. No on-disk/template/serialization format change
(§4 accurate). The §6 self-checks are grep-executable and match what I ran.

**Remaining issues:** none blocking. Minor/optional only: (a) the breadcrumb-comment "After" snippets
show fewer trailing blank lines than will actually remain (3 blanks above + 2 below the cut survive) —
purely cosmetic, zero compile impact; (b) the historical `approved = false` peer-review verdict line
is intentionally retained as a record (the §2 "After" prose was itself corrected to the `mfccszamitas`
version), which could momentarily confuse a skim-reader, but the Revision + this Re-review supersede
it. Safe to implement.

---

## Revision 2026-06-27 (independent re-verification)

This pass independently re-read the **live source** at every target location (all duplicated copies)
and re-checked conformance to `plans/_grouping.json`, rather than trusting the plan's own prior claims.
Result: the prior Revision's resolution of the sole peer-review blocker **holds**, no new defect was
found, and the grouping section conforms. Only one genuine source-accuracy gap was corrected (§2 File
A blank-line count); everything else was confirmed already-correct and left untouched (no churn).

**Re-verified against live source (this pass):**
- **File A `Turan_creator/.../H_FELDOLGOZO.cs` (377 lines):** dead block = **279–373** (`tavtomb` decl
  on 279 → `// end Comp` on 373); method directly above is `mfccszamitas`, whose `}` is on **275**;
  blank lines **276/277/278**; tail braces **376–377**. **No** `Show2dArray`/`ShowArray` in this copy
  (confirmed by full read of 255–377 and grep). BUG-04 target `Math.Sqrt(2 / 24)` is on **272**, inside
  `mfccszamitas`, above the cut — correctly left for BUG-04.
- **File B `Felismero_motor_LITE/.../H_FELDOLGOZO.cs` (413 lines):** dead block = **315–409** (same
  anchors); method directly above is `ShowArray`, whose `}` is on **312**; `Show2dArray` overloads on
  **254** (`double[,]`) and **274** (`int[,]`), both retained; blank lines **313/314**; tail braces
  **412–413**.
- **File C `Felismero_motor_LITE/.../Form1.cs` (1639 lines):** commented `CalcDTWDistances()` =
  **1433–1506** (`//private void CalcDTWDistances()` on 1433 → closing `//}` on 1506), between the
  commented FFT block ending **1429** and the commented `notifyIcon1_MouseDoubleClick` starting
  **1510**. The whole block is commented, so no brace imbalance is possible. Live `Show2dArray` call at
  `Form1.cs:532` confirmed untouched.
- **Deadness re-grepped per identifier:** `tavtomb`, `ertomb`, `refcounters`, `refcounter1`,
  `counter_ref`, `mfccref1/2/3`, `referencia`, `Refs`, `mfccrefs` are referenced **only** inside the two
  `hasonlit` bodies and the commented `CalcDTWDistances()` driver (`H_FELDOLGOZO.referencia` on 1462,
  `H_FELDOLGOZO.counter_ref` on 1466, `H_FELDOLGOZO.ertomb` on 1481), plus the `Form1.Designer.cs` UI
  string literals on 345/458 ("Fő referencia:" / "…referencia adatbázist."). Zero live references
  survive the cut.
- **Census:** repo-wide `grep hasonlit` = exactly **5** hits (2 defs at A:294 / B:330, 3 commented calls
  at Form1.cs 1472/1491/1505); `git ls-files` = exactly **2** `H_FELDOLGOZO.cs` copies (Turan_core has
  none). Active DTW path intact: `new dtwApp_match` at `Engine.cs:89/124` and `Form1.cs:477` (the 451
  hit is the static `dtwApp_match.Num_of_templates`, also live; 456/458/463/467 are commented).
- **Grouping conformance:** Group **B (`B-dsp-features`)** membership and internal order
  "**16 first** → 04 → 02 → 05 → 06 → 03 → 07" match `_grouping.json`. BUG-16 touches only the two
  `H_FELDOLGOZO.cs` copies + LITE `Form1.cs` (not Creator.cs) — inside Group B's four files.
  **DEAD-METHOD OWNERSHIP** (BUG-16 owns the hasonlit/backing-field/driver removal; BUG-08 in Group A
  drops its cap edit) and **H_FELDOLGOZO DUPLICATE PARITY** (both copies cut byte-identically) match the
  `sharedContracts`. The Form1.cs deletion (1433–1506) is disjoint from BUG-12's S1 serialization edits.

**Changed this pass (the only genuine source gap found):**
1. **§2 File A "Keep the blank lines"** — corrected "**277–278**" to "**276–278** (three blank lines:
   `}` ending `mfccszamitas` on 275, then blanks on 276/277/278)". Source has three blank lines above
   the cut, not two. Cosmetic (deletion range 279–373 is unaffected), but now byte-accurate.
   *Closes:* the last residual source-accuracy imprecision in the edit instructions.
2. **Re-review "Remaining issues (b)"** — removed the inaccurate clause stating the "original §2 'After'
   prose [was] intentionally retained": §2's "After" was in fact rewritten to the `mfccszamitas`
   version by the prior Revision. Self-contradiction tidied (documentation only).

**Confirmed already-resolved (no edit needed):** the sole peer-review blocker — §2/§1/§6 wrongly
implying File A contains `Show2dArray` — was fully corrected by the prior Revision (File-A landmark =
`mfccszamitas` ends 275; File-B landmark = `ShowArray` ends 312; `Show2dArray` 254/274 scoped to File B
only). All three corrections match live source. With this re-verification the plan is correct,
complete, grouping-conformant, and safe to implement; `approved = true` stands.

---

## Re-review 2026-06-27 (independent second pass)

**Verdict: approved = true.** I independently re-read every target region in the live tree and
re-checked `plans/_grouping.json` myself, without trusting the plan's prior claims. The edit
instructions are byte-accurate, the deleted state is provably dead, no new defect was introduced,
and the plan stays inside its Group-B scope and the shared contracts. Safe to implement.

**(1) Previously-blocking issue genuinely resolved.** The original peer-review blocker — §2/§1/§6
implying `Turan_creator/.../H_FELDOLGOZO.cs` contains `Show2dArray` — is fixed. I confirmed against
source: File A has **no** `Show2dArray`/`ShowArray` (`grep` for both, excluding Designer, returns
hits only in the Felismero copy and Felismero `Form1.cs`); the method whose `}` sits immediately
above the cut is `mfccszamitas`, closing on **line 275**. `Show2dArray` overloads live only in File B
at **254** (`double[,]`) and **274** (`int[,]`), with the one-dim `ShowArray` closing at **312**; all
three are retained above File B's cut, and the live `Show2dArray` call at `Form1.cs:532` is untouched.
The plan's §2/§1/§6 now state exactly this.

**(2) No new defect / off-by-one / integer-division / vector-width / duplicated-copy omission.**
- Line ranges verified byte-exact by direct read: File A delete **279–373** (`tavtomb` decl on 279 →
  `// end Comp` on 373; `hasonlit` def 294, returns 370, `}` 371), keeping blanks 374–375 and braces
  376–377. File B delete **315–409** (same anchors; `hasonlit` def 330, `}` 407, `// end Comp` 409),
  keeping 410–413. File C delete the fully-commented `CalcDTWDistances()` **1433–1506**, sitting
  between the commented FFT block ending 1429 and the commented `notifyIcon1_MouseDoubleClick`
  starting 1510 — no brace imbalance possible since the whole block is commented.
- Deadness re-grepped per identifier (`tavtomb`, `ertomb`, `refcounters`, `refcounter1`,
  `counter_ref`, `mfccref1/2/3`, `referencia`, `Refs`, `mfccrefs`): every code reference falls inside
  the two `hasonlit` bodies (incl. the `//counter_ref` comment at A:314/B:350) or the commented
  `CalcDTWDistances()` driver (`H_FELDOLGOZO.referencia` 1460/1462, `counter_ref` 1466, `ertomb`
  1481). The only out-of-region `referencia` hits are `Form1.Designer.cs:345/458` UI string literals
  (`"Fő referencia:"`, `"…referencia adatbázist."`) — not identifiers, correctly left alone. The
  seven never-read orphan fields (`refcounters`, `refcounter1`, `mfccref1/2/3`, `Refs`, `mfccrefs`)
  have no reader anywhere. Zero live reference survives the cut → no dangling reference, no compile
  hazard.
- Census: repo-wide `grep hasonlit --include=*.cs` = exactly **5** hits (defs A:294 / B:330; commented
  calls Form1.cs 1472/1491/1505); `git ls-files` = exactly **2** `H_FELDOLGOZO.cs` copies (Turan_core
  has none). Both copies are in scope and cut byte-identically in the edited region. No untracked
  copy; `Turan_SC_minimal`/`Turan_tester` `Form1.cs` carry no `hasonlit` and are excluded.
- No width/integer-division surface: this is a pure deletion. The kept width constants
  `mfcc_lpc_vect_num`/`num_items_in_windowed_frame` (lines 36/37) are untouched; the BUG-04
  `Math.Sqrt(2 / 24)` integer-division line is at A:**272** inside `mfccszamitas`, above the cut, and
  is correctly left for BUG-04. The removed Form1.cs `win_REF_meldata.Length / 15` is dead commented
  code, not introduced logic. All removals are at the bottom of each region, so they do not shift the
  line numbers that the other Group-B bugs (02/04/05/06) target above them — "BUG-16 first" is safe.

**(3) Consistent with shared contracts and group scope.** Group **B (`B-dsp-features`)** membership
and internal order "**16 first** → 04 → 02 → 05 → 06 → 03 → 07" match `_grouping.json`. BUG-16 edits
only the two `H_FELDOLGOZO.cs` copies + LITE `Form1.cs` (not Creator.cs) — inside Group B's four
files. **DEAD-METHOD OWNERSHIP** honored (BUG-16 owns the hasonlit/backing-field/driver removal;
Group A's BUG-08 correspondingly drops its hasonlit cap — reflected in Group A's note and
`sharedContracts`). **H_FELDOLGOZO DUPLICATE PARITY** satisfied (byte-identical cut in both copies).
The Form1.cs deletion (1433–1506) is disjoint from BUG-12's S1 serialization writer/reader edits in
the same file → no collision. No on-disk/template/serialization format change (§4 accurate).

**Note (outside BUG-16's scope, non-blocking):** `_grouping.json` was just revised so Group C is now
`C-finish-native-mfcc` (finish+quarantine the CoMIRVA port, BUG-15) rather than a deletion; the S1
`runAfter` array still names the old `"C-dead-code-removal"`. This is a stale label in the grouping
file, not a BUG-16 defect, and does not affect BUG-16's correctness or scheduling (BUG-16 has no
dependency on what Group C does). Flagging for whoever maintains `_grouping.json`.

**Remaining issues:** none blocking. Cosmetic only: the breadcrumb "After" snippets show fewer
trailing blank lines than actually remain (276–278 above + 374–375 below the File-A cut survive), and
the historical `approved = false` peer-review verdict line is retained as a record — both are
documentation-only and have zero compile impact. The plan is correct, complete, grouping-conformant,
and safe to implement.
