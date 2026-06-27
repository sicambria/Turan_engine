# FIX PLAN — BUG-04: DCT scale factor collapses to zero via integer division

**Severity:** P1 (correctness; currently dead code, but wrong and blocks future work)
**Files (both copies of `H_FELDOLGOZO.cs`):**
- `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs`
- `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`

---

## 1. Root cause (restated from the code)

In `public static void mfccszamitas(ref uint[,] osszegek, int dbszam)`, the orthonormal
DCT-II normalization scale is computed as:

```csharp
mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));
```

`2` and `24` are both `int` literals, so `2 / 24` is **integer division**, which evaluates to
`0`. `Math.Sqrt(0) = 0.0`. The accumulated DCT sum (correct up to that point) is then multiplied
by `0.0`, so **every MFCC coefficient is forced to zero**.

The intended value is the standard DCT-II orthonormal scale `sqrt(2/N)` where `N` is the number of
mel-filterbank channels. Here `N = 24`, justified directly by the inner loop bound in the same
function: `for (i = 0; i <= 23; i++)` iterates over 24 channels (the `osszegek`/`sumfloat` second
dimension is `24`). Correct value: `Math.Sqrt(2.0 / 24.0) ≈ 0.288675`.

**Audit of the rest of the function (ROADMAP asks for this).** The only other `/ <int>` in the DCT
block is the cosine argument:

```csharp
Math.Cos((m * (i - 0.5) * Math.PI) / 24)
```

This is **safe**: `i - 0.5` promotes the entire expression to `double` before the `/ 24`, so that
division is floating-point. A targeted integer-division grep over each file
(`grep -nE "/ ?[0-9]+\b"`, filtering out comments / `0.5` / `.0` / cosine-style terms) returns the
**single** line below in each file — the sqrt scale is the only integer-division-in-double error in
`H_FELDOLGOZO.cs`. Both copies were audited, not just the Creator copy.

---

## 2. Exact change per file/line (before / after)

The changed line and its enclosing DCT block are identical in both copies; only line numbers differ.
(The surrounding files diverge elsewhere — namespace, `create_window_no_overlap`, `Show2dArray`,
a `frame` vs `frame-1` difference in `win_overlap` — but none of that touches this fix.)

### File A — `Turan_creator/Turan_creator/H_FELDOLGOZO.cs`, line 272

Before:
```csharp
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));
```
After:
```csharp
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2.0 / 24.0));
```

### File B — `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs`, line 248

Before:
```csharp
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));
```
After:
```csharp
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2.0 / 24.0));
```

That is the entire fix: one character-level change (`2 / 24` → `2.0 / 24.0`) in two files. No
control-flow, signature, or surrounding-code changes.

> Optional hardening (NOT required, do not do unless BUG-03 is being implemented in the same pass):
> if the native DCT path is later enabled, replace the magic `24` with a named constant tied to the
> filterbank size so the cosine denominator and the sqrt scale cannot drift apart. Out of scope for a
> surgical BUG-04 fix; noted only for the BUG-03 follow-up.

---

## 3. Every duplicated copy that needs the change

`H_FELDOLGOZO.cs` exists in exactly **two** locations (verified by
`find -name H_FELDOLGOZO.cs` → 2 hits, matching the task's named files). Both are listed in §2 and
**both must be edited**. No third copy of this file exists. (`mfcc.cs` is a *different* class in a
different module and is addressed only as a cross-reference in §5, not edited here.)

---

## 4. Backward / data-format compatibility

**`breaksCompat: false`.** No on-disk, template, or serialization format changes. Three independent
reasons:

1. **Dead code.** `mfccszamitas` is defined in both files but **never called anywhere** (verified:
   `grep -rn mfccszamitas --include=*.cs` returns only the two definitions, no call sites). Changing
   it cannot alter any current runtime behavior.
2. **No format touched.** The change is a numeric scale inside a local computation; it writes only to
   the in-memory `mfccarr`. It does not touch any file I/O, struct layout, or serialization.
3. **Live templates were not produced by this path.** Existing templates/feature vectors are produced
   by the live extraction path, not by `mfccszamitas`. So no stored data was generated with the
   zero-scale, and nothing on disk needs migration or versioned reads.

If/when BUG-03 enables this DCT path, that change (not this one) is what would alter produced feature
vectors; at that point feature/template regeneration is a BUG-03 concern, not a BUG-04 compatibility
issue.

---

## 5. Shared contracts / cross-bug dependencies

- **BUG-03 depends on BUG-04.** Per ROADMAP, BUG-04 is dead but "blocks any fix that enables the
  native DCT." Enabling the native DCT path (BUG-03) without this fix would zero every coefficient,
  so this fix is a prerequisite for BUG-03 to yield usable features. **It is necessary, not
  sufficient:** `mfccszamitas` also contains `byte`-counter loops `for (byte i = 0; i <= 255; i++)`
  (Creator lines 231 & 238; LITE lines 207 & 214) that never terminate — when `i` reaches 255 the
  `i++` wraps to 0 in C#'s default unchecked context — plus frame loops `<= dbszam` over `byte`
  counters that misbehave past 255 frames. Those are out of scope for BUG-04 (not integer-division,
  not introduced here, dead at runtime) but a BUG-03 implementer who wires in `mfccszamitas` after
  applying only BUG-04 will hang. They must be fixed under BUG-03 before the DCT path is usable.
- **Normalization-convention consistency with the live MFCC module.**
  `Felismero_motor_LITE/Felismero_motor/mfcc.cs:375` uses the same orthonormal DCT-II scale,
  `Math.Sqrt(2.0 / numberFilters)`. Our fix `Math.Sqrt(2.0 / 24.0)` is the **same formula** with
  `N = 24`, the filterbank size hardwired into `mfccszamitas` (loop `i = 0..23`). Note that
  `mfcc.cs`'s `numberFilters` is a constructor parameter whose documented default is 40 — i.e. the
  two modules use *different* filterbank sizes. That is a pre-existing latent inconsistency between
  two independent code paths, not something this fix should silently "reconcile" by hardcoding. For
  `mfccszamitas` itself, `24` is unambiguously correct because that is this function's own channel
  count. Flag the 24-vs-40 divergence for whoever unifies the feature paths (BUG-03), but do not
  change it here.
- **No interaction with `SerializeArray` / `DeSerializeArray`.** Checked: this fix does not touch the
  Creator↔Engine array serialization contract (the example shared format in the task). It is confined
  to the local DCT scaling and writes only to `mfccarr` in memory.

---

## 6. Self-verification without a compiler

1. **Integer-division trace (the bug):** `2 / 24` with both operands `int` → C# integer division →
   `0`. `Math.Sqrt(0) → 0.0`. Multiplying the DCT sum by `0.0` zeros every coefficient. Confirms the
   defect.
2. **Fixed-value trace:** `2.0 / 24.0 → 0.0833333…`; `Math.Sqrt(0.0833333…) → 0.288675…`. A non-zero,
   correct DCT-II orthonormal scale. Confirms the fix.
3. **Type-promotion audit of the neighbouring division:** in `Math.Cos((m * (i - 0.5) * Math.PI)/24)`,
   `i` is promoted via `i - 0.5` to `double`, so the expression is `double` before `/ 24` → that
   division is floating-point and correct. Establishes that the sqrt scale is the *only* such bug in
   the function.
4. **Dead-code confirmation:** `grep -rn "mfccszamitas" --include=*.cs` → only the two definitions, no
   callers → confirms no runtime/format impact (item 4).
5. **Duplicate-coverage confirmation:** `find -name H_FELDOLGOZO.cs` → exactly the two files in §2,
   both edited.
6. **Cross-reference check:** compare the fixed scale to `mfcc.cs:375`
   (`Math.Sqrt(2.0 / numberFilters)`) — same formula, `N = 24` here matches the `i = 0..23` loop.
7. **Post-edit grep:** after editing, `grep -n "Math.Sqrt(2" H_FELDOLGOZO.cs` in both files should show
   `2.0 / 24.0` and no remaining `2 / 24`.

---

## Peer review

**Reviewer verdict: APPROVED.** The fix is correct, minimal, complete, and introduces no new
defects. Every load-bearing claim in the plan was verified against the live source; details below.

### Confirmed against source
- **Root cause correct.** `Turan_creator/.../H_FELDOLGOZO.cs:272` and
  `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:248` both read
  `mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));`. Both operands are `int` literals →
  integer division → `0` → `Math.Sqrt(0) = 0.0` → every coefficient zeroed. Verified.
- **Fix value correct.** `2.0 / 24.0 = 0.08333…`, `Math.Sqrt(…) = 0.288675…` — the standard DCT-II
  orthonormal scale `sqrt(2/N)` with `N = 24`. Note (not a defect): only `m = 1..mfcc_lpc_vect_num`
  is computed; the `m = 0` (c0) term that would normally use `sqrt(1/N)` is never produced, so the
  uniform `sqrt(2/24)` is correct for every coefficient this function emits.
- **Line numbers exact.** Plan §2 cites Creator:272 and LITE:248; `grep` confirms both verbatim
  (ROADMAP's older 248/~224 numbers are stale — the plan's are right).
- **N = 24 justified.** Inner loop `for (i = 0; i <= 23; i++)` (24 channels), `sumfloat` second dim
  is `24`. Confirmed in both copies.
- **Cosine line genuinely safe.** `Math.Cos((m * (i - 0.5) * Math.PI) / 24)` — `i - 0.5` promotes the
  subexpression to `double` before `/ 24`, so that division is floating-point. Verified.
- **Integer-division audit holds.** `grep -nE "/ ?[0-9]+"` (filtering comments/`.0`/`0.5`) over each
  whole file returns **only** the sqrt line. The §6 claim "the only integer-division-in-double error"
  is literally true in both copies.
- **Dead code / no callers.** `grep -rn mfccszamitas --include=*.cs` → only the two definitions, no
  call sites → `breaksCompat: false` is correct; no on-disk/template/serialization impact.
- **Duplicate coverage complete.** `find -name H_FELDOLGOZO.cs` → exactly the two files; both edited.
- **Cross-reference accurate.** `mfcc.cs:375` is `double w2 = Math.Sqrt(2.0 / numberFilters);` — same
  orthonormal formula, confirming the convention. The 24-vs-`numberFilters` divergence is correctly
  flagged as a pre-existing latent issue to defer to BUG-03, not reconcile here.

### Advisory (non-blocking) — sharpen §5's BUG-03 dependency
§5 states BUG-04 is "a prerequisite for BUG-03 to yield usable features." Accurate, but it is
**necessary, not sufficient.** `mfccszamitas` also contains two unconditional infinite loops:
`for (i = 0; i <= 255; i++)` with `byte i` (Creator lines 231 & 238; LITE lines 207 & 214) — when
`i` reaches 255 the `i++` wraps to 0 in C#'s default unchecked context, so the condition is never
false. Additionally the frame loops `<= dbszam` over `byte indx`/`byte kersz` will misbehave if frame
count ever reaches 255. These are **out of scope for BUG-04** (not introduced here, not
integer-division, and dead at runtime), so they do not block this fix — but a BUG-03 implementer who
wires in `mfccszamitas` after applying only BUG-04 will hang. Recommend adding one sentence to §5:
fixing BUG-04 is required but additional `byte`-loop/overflow fixes are also needed under BUG-03
before the DCT path is usable.

### Minor wording
- §2's "byte-for-byte identical in both copies" is loose: the *changed line and its DCT block* are
  identical (verified), but the surrounding files diverge substantially (namespace,
  `create_window_no_overlap`, `Show2dArray`, a `frame` vs `frame-1` difference in `win_overlap`).
  Harmless for this fix; consider rewording to "the changed line and its enclosing DCT block are
  identical."
- §5's "documented default is 40" for `numberFilters` was not independently verified; immaterial,
  since the divergence point holds for any `N ≠ 24`.

None of the above changes the verdict. The one-character change `2 / 24` → `2.0 / 24.0` in both files
is the correct and complete BUG-04 fix.

---

## Peer review (independent verification — 2026-06-27)

**Verdict: APPROVED.** I re-verified every load-bearing claim directly against the live source
(no compiler, code reading only). The fix is correct, minimal, complete across all copies, and
introduces no regressions.

### Re-confirmed against source
- **Bug present, exact lines.** `Turan_creator/.../H_FELDOLGOZO.cs:272` and
  `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:248` both read
  `mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));`. Both operands `int` →
  integer division → `0` → `Math.Sqrt(0)=0.0` → all emitted coefficients zeroed. Verified.
- **Fix value correct.** `2.0/24.0 = 0.08333…`, `Math.Sqrt = 0.288675…` = DCT-II orthonormal
  scale `sqrt(2/N)`, `N=24`. Justified by inner loop `i = 0..23` and `sumfloat` second dim `24`.
- **No c0 concern.** Output loop is `m = 1..mfcc_lpc_vect_num` (`mfcc_lpc_vect_num = 12` in both
  `H_FELDOLGOZO.cs`); the `m=0` term that would need `sqrt(1/N)` is never emitted, so a uniform
  `sqrt(2/24)` is correct for every coefficient this function produces. No index overflow: `mfccarr`
  is `[256,256]`, `m ≤ 12`.
- **Cosine division safe.** `Math.Cos((m * (i - 0.5) * Math.PI) / 24)` — `(i - 0.5)` promotes to
  `double` before `/ 24`; floating-point. The sqrt scale is the only integer-division-in-double
  defect in the function.
- **Dead code / no callers.** `grep -rn mfccszamitas --include=*.cs` → only the two definitions; the
  apparent extra hits (`Creator.cs`, `Form1.cs`) are for `win_fir_hamming`, not this function.
  `breaksCompat: false` is correct; nothing on disk was produced by this path.
- **Duplicate coverage exhaustive.** `find -name H_FELDOLGOZO.cs` → exactly the two files in §2; no
  third copy in `Turan_core`/`Turan_tester`. Both edited.
- **Cross-reference holds.** `mfcc.cs:375` `Math.Sqrt(2.0 / numberFilters)` is the same convention;
  the 24-vs-`numberFilters` divergence is correctly deferred to BUG-03, not reconciled here.

### Advisory (non-blocking, out of BUG-04 scope)
- **DCT basis off-by-one is *not* fixed by this change and is not claimed to be — but §1's phrasing
  "the accumulated DCT sum (correct up to that point)" overstates correctness.** The basis uses
  `cos(π·m·(i − 0.5)/24)` with `i = 0..23`, i.e. arguments `−0.5 … 22.5`. The canonical HTK DCT-II
  uses `(i − 0.5)` with `i = 1..N` (arguments `0.5 … N−0.5`), so this loop is shifted by one channel
  and starts at `−0.5`. This affects feature *values* but is orthogonal to the integer-division bug,
  so it must not block BUG-04. Recommend softening §1 to "the accumulated DCT sum (modulo a separate,
  out-of-scope basis-indexing question)" and flagging the indexing for the BUG-03 implementer.
- The infinite `byte`-counter loops (`for (i = 0; i <= 255; i++)` at Creator 231/238, LITE 207/214)
  and `<= dbszam` byte frame loops are real and already correctly captured in §5 / the prior review
  as BUG-03 prerequisites. Confirmed present; out of scope here.

### Process note
- A prior "## Peer review (APPROVED)" section already existed in this plan; this section is an
  independent second pass and reaches the same verdict. No content above changes it.

The change `2 / 24` → `2.0 / 24.0` in both `H_FELDOLGOZO.cs` copies is correct and complete.
