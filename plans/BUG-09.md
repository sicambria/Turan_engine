# BUG-09 — Itakura distance hard-codes LPC order 13 (`magic13`)

**Severity:** P2 (medium). **Status of code path:** DEAD (latent) — see §4.
**Files:**
- `/home/arsvivendi/git/Turan_engine/Turan_core/Turan_core/dtwApp_match.cs` (method `ITDDistance`, lines 255–352)
- `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` (method `ITDDistance`, lines 257–352)

---

## 1. Root cause (restated from the code)

`ITDDistance(double[] ar2, double[] ar1)` is a Voicebox-style Itakura distortion
routine that assumes an LPC analysis whose **vector width is exactly 13**. That
assumption is baked in as the literal `13` everywhere (and as `int magic13 = 13;`
in the Turan_core copy), plus the dependent literals `12` (= width−1) and `11`
(= width−2) inside the recurrences.

The actual per-frame vector width is **configurable and is NOT 13**. Two
*different* statics govern it, one per module:

| Module      | Static `ITDDistance` binds to     | Declared | Reassigned at runtime? |
|-------------|-----------------------------------|----------|------------------------|
| Turan_core  | `Engine.mfcc_lpc_vect_num`        | `15` (`Engine.cs:29`) | **No** — `grep -rn mfcc_lpc_vect_num Turan_core/` shows only the declaration; the mode-select code in `Engine.cs:111–119` writes a **local** `num_of_feature_vectors`, never the static. So the live width is **always 15**. |
| LITE        | `H_FELDOLGOZO.mfcc_lpc_vect_num`  | `12` (`H_FELDOLGOZO.cs:36`) | **Yes** — set to `12` (LPC) or `15` (MFCC) in `Felismero_motor/Form1.cs:104,108`. Live width is **12 or 15**. |

The vectors actually handed to `ITDDistance` come from `GetReferenceVector` /
`GetSignalVector`, which allocate to the **live** static — `new double[Engine.mfcc_lpc_vect_num]`
(Turan_core lines 180/193) and `new double[H_FELDOLGOZO.mfcc_lpc_vect_num]`
(LITE lines 180/193). So the input arrays are 15-wide in Turan_core and 12/15-wide
in LITE, never the 13 the body assumes.

Consequences of the `13` assumption — **distinct per module:**
- **LITE, width 12 (LPC mode):** out-of-bounds **reads**. `rf[i] = ar1[i]` and
  `m2[i] += ar2[j]*ar2[i+j]` walk index `12` of a 12-element array →
  `IndexOutOfRangeException` on the only mode where Itakura is even meaningful.
- **Turan_core, width 15 (always):** no OOB read (15 > 13), but the copy carries
  a latent OOB **write** introduced by a botched partial refactor — see below.
- **Internal drift between the two copies** (the duplication has diverged):
  - **Turan_core copy** was partially refactored to `magic13` but
    - line 283 still uses the literal `11` (the reflection-coef loop), and
    - line 310 still uses the literal `12` (the main recurrence), and
    - line 331 was **wrongly** changed to `for (i = 0; i < magic13; i++) ar[i+1] = a[i];`
      — this writes `ar[13]` into `double[] ar = new double[13]` (valid indices
      0–12) → **latent OOB write**, a regression unique to this copy. (It throws
      regardless of input width, because `ar` is sized to `magic13`.)
  - **LITE copy** is all-literal `13`/`12`/`11`, internally consistent for
    width 13 (no OOB write), but still wrong for the live widths 12/15.

The fix replaces the magic numbers with a single derived order so the routine is
self-consistent and reads/writes strictly within the input vector width.

### Constant → parameter mapping (load-bearing)

Let `order` be the live vector width (coefficient count N = LPC order p + 1).
The original constants map as:

| Literal in code | Meaning | Replacement |
|-----------------|---------|-------------|
| `13` / `magic13` (array sizes, full-length loops, `ar1[i]`/`ar2[i]` access) | vector width `N` | `order` |
| `12` (lines 310, 331) | `N − 1` | `order - 1` |
| `11` (line 283) | `N − 2` | `order - 2` |

Index-safety check for the replacement (max index accessed must be `< order`):
reflection loop `j = order-2 … 1`, touches `rf[j+1] = rf[order-1]` ✔; main loop
`i = 1 … order-2`, touches `rf[i+1]/rr[i+1] = […order-1]` ✔; `ar` fill
`i = 0 … order-2`, touches `ar[i+1] = ar[order-1]` ✔; `ar1[i]/ar2[i]` touch
`[order-1]` = matches input width ✔. The only fixed-index accesses are
`a[0]=rf[1]`, `rr[1]=-a[0]`, which just require `order ≥ 2`. Holds for
order ∈ {12, 13, 15} (and any future width ≥ 2, e.g. BUG-01's 60).

**Regression guarantee:** for the historical `order == 13` case the rewritten
body is byte-for-byte equivalent in behaviour to the original LITE literals
(`order=13` → arrays[13], `order-2=11`, `order-1=12`). So this is a pure refactor
for legacy width-13 data and a correctness fix (no OOB, width-matched) for the
actual widths 12/15.

### Order source: use the LIVE static, not cached `num_of_vectoritems`

`EuclideanDistance` loops on the cached field `num_of_vectoritems`
(Turan_core/LITE line 84), which is initialised **once** from
`Engine.mfcc_lpc_vect_num` / `H_FELDOLGOZO.mfcc_lpc_vect_num` at static-init time
and can be **stale** relative to the live value the mode-selection code later
writes (in LITE). The airtight reason to avoid it: `ITDDistance` must be sized by
the **same** source that sizes the vectors it is handed —
`GetReferenceVector`/`GetSignalVector` allocate `new double[<live static>]`, so
`order` must equal that live static for the loops to span exactly the array the
getters produced, no more, no less. (If `order` ever exceeded the input width —
e.g. cached `num_of_vectoritems = 15` while live width = 12 — `rf[i] = ar1[i]`
would read past the end → OOB; the stale-cache field is exactly the kind of value
that can drift above the live width, so it is the wrong source.) Derive the order
from the **live** static, exactly as the getters do:
- Turan_core: `int order = Engine.mfcc_lpc_vect_num;`
- LITE: `int order = H_FELDOLGOZO.mfcc_lpc_vect_num;`

(Both statics are `byte`; assigning to `int` and doing `order - 1`/`order - 2`
is fine — C# promotes `byte` arithmetic to `int`.)

---

## 2. Exact change — canonical replacement body

Replace the **entire `ITDDistance` method body** in both copies with the
following canonical version (the only per-file difference is the order-source
line, marked). This simultaneously removes `magic13`, fixes the Turan_core
line-331 latent OOB write, and converts the still-literal `11`/`12` in Turan_core.

### AFTER (canonical, both files)

```csharp
// method to compute the Itakura distance between two vectors
private double ITDDistance(double[] ar2, double[] ar1)
{
    // BUG-09: width derived from the live vector width, not magic 13.
    // NB: this is the coefficient COUNT N (= LPC order p + 1), not p itself.
    int order = Engine.mfcc_lpc_vect_num;        // LITE: H_FELDOLGOZO.mfcc_lpc_vect_num

    double[] m2 = new double[order];
    double[] rf = new double[order];
    double[] rf1 = new double[order];
    double k, d;

    int i, j;

    for (i = 0; i < order; i++)
    {
        m2[i] = 0;
        rf[i] = ar1[i];
    }

    //autocorrelation of ar2 (lpcar2ra)
    for (i = 0; i < order; i++)
    {
        for (j = 0; j < order - i; j++)
            m2[i] += ar2[j] * ar2[i + j];
    }

    //reflection coefficients from ar1 (lpcar2rf)
    for (j = order - 2; j > 0; j--)
    {
        k = rf[j + 1];
        d = 1.0 / (1.0 - k * k);
        for (i = 1; i <= j; i++)
        {
            rf1[i] = (rf[i] - k * rf[j - i + 1]) * d;
        }
        for (i = 1; i <= j; i++)
            rf[i] = rf1[i];
    }

    // autocorrelation coefs from rf (lpcrf2rr)
    double[] rr = new double[order];
    double[] a = new double[order];
    double sum;
    for (i = 0; i < order; i++)
    {
        rr[i] = 0.0;
        a[i] = 0.0;
    }
    a[0] = rf[1];
    rr[0] = 1.0;
    rr[1] = -a[0];
    double e = a[0] * a[0] - 1.0;

    for (i = 1; i < order - 1; i++)
    {
        k = rf[i + 1];
        sum = 0.0;
        for (j = i; j >= 1; j--)
            sum += rr[j] * a[i - j];

        rr[i + 1] = k * e - sum;

        double[] aa = new double[order];
        for (j = 0; j < i; j++)
            aa[j] = a[j] + k * a[i - j - 1];
        for (j = 0; j < i; j++)
            a[j] = aa[j];
        a[i] = k;

        e = e * (1.0 - k * k);
    }

    double[] ar = new double[order];
    ar[0] = 1.0;
    for (i = 0; i < order - 1; i++)
        ar[i + 1] = a[i];

    sum = 0.0;
    for (i = 0; i < order; i++)
        sum += rr[i] * ar[i];

    double r0 = 1.0 / sum;

    for (i = 0; i < order; i++)
        rr[i] *= r0;

    m2[0] *= 0.5;
    sum = 0.0;
    for (i = 0; i < order; i++)
        sum += rr[i] * m2[i];
    sum *= 2;
    sum = Math.Log10(sum);

    if (Math.Abs(sum) < 1e-6) return 0.0;
    return sum;
}
```

### Per-file before → after deltas (for reviewer cross-check)

**Turan_core/Turan_core/dtwApp_match.cs** (the method spans lines 255–352):
- Line 257 `int magic13 = 13;` → **replace** with
  `int order = Engine.mfcc_lpc_vect_num;`
- Every `magic13` (lines 259–261, 267, 275, 278, 296, 297, 300, 319, 329, 335,
  340, 345) → `order`.
- Line 283 `for (j = 11; j > 0; j--)` → `for (j = order - 2; j > 0; j--)`.
- Line 310 `for (i = 1; i < 12; i++)` → `for (i = 1; i < order - 1; i++)`.
- Line 331 `for (i = 0; i < magic13; i++)` → `for (i = 0; i < order - 1; i++)`
  **(fixes the latent OOB write into `ar`)**.

**Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs** (lines 257–352):
- Insert at top of body: `int order = H_FELDOLGOZO.mfcc_lpc_vect_num;`
- Lines 259–261, 267, 275, 278, 296, 297, 300, 319, 329, 335, 340, 345: literal
  `13` → `order`.
- Line 283 `for (j = 11; j > 0; j--)` → `for (j = order - 2; j > 0; j--)`.
- Line 310 `for (i = 1; i < 12; i++)` → `for (i = 1; i < order - 1; i++)`.
- Line 331 `for (i = 0; i < 12; i++)` → `for (i = 0; i < order - 1; i++)`.

> Note line numbers reference the current file; `ITDDistance` starts at 255 in
> Turan_core and 257 in LITE (the latter lacks the `int magic13` line and one
> blank line). Match on the literals, not just the line numbers.

---

## 3. Every duplicated copy that needs the change

`find . -name dtwApp_match.cs` returns exactly two paths (both listed above). No
third `.cs` copy. **Both must receive the identical canonical body** (modulo the
`order`-source line). After the change the two `ITDDistance` bodies are
functionally identical, eliminating the current drift.

> A dead **Java reference port** also contains an `ITDDistance`:
> `Felismero_motor_LITE/Felismero_motor/kódok/match.java`. It is **not** a
> `dtwApp_match.cs`, is non-compiling scaffolding (BUG-15 family), is not in any
> build, and is intentionally **left untouched** here.

---

## 4. Backward / on-disk / data-format compatibility

**No format change. No backward-compatibility break.** Reasons:
- `ITDDistance` is invoked only from `frameDistance` under
  `else if (distanceType == "Itakura")`. `distanceType` is a private field
  hard-coded to `"Euclidean"` in both copies (Turan_core line 102, LITE line 102)
  and has **no setter** anywhere (grep confirms the only `distanceType =` is the
  field initialiser; no `DistanceType` property; the only other mentions are the
  three `if/else if` comparisons). Therefore the Itakura branch is **unreachable
  at runtime today** — this is a latent-code fix.
- The change touches no serialized template format. Template files (`.lpc`,
  `.mfcc`) are written/read by `Creator.SerializeArray` /
  `Engine.DeSerializeArray` (`BinaryFormatter` of the `double[,]`); their layout
  and width are unaffected by this distance function.
- For legacy width-13 data the rewrite is behaviour-preserving (see §1
  regression guarantee), so even if Itakura were enabled on old 13-wide
  templates, results are unchanged.

`breaksCompat = false`.

---

## 5. Shared contract another bug's fix depends on

- **Width contract:** `ITDDistance`'s internal order must equal the width of the
  vectors produced by `GetReferenceVector`/`GetSignalVector`, which is the live
  `*.mfcc_lpc_vect_num`. This fix binds `ITDDistance` to that same live static,
  keeping it aligned with `EuclideanDistance`'s intent and with BUG-08 (magic
  array sizes). Anyone fixing BUG-08 (removing fixed `120`/`256` caps and the
  stale `num_of_vectoritems` capture) should keep this single source of truth.
- **BUG-01 (HTK width → 60):** that fix widens the live feature vector and updates
  the live static. Because this fix binds `order` to that same live static,
  `ITDDistance` automatically follows to width 60 with no further edit, and the
  body is bounds-safe at `order = 60` (every index ≤ `order-1`; fixed-index
  accesses only need `order ≥ 2`). A positive argument for binding to the live
  static rather than the cached `num_of_vectoritems`.
- **No coupling to serialization** (`Creator.SerializeArray` /
  `Engine.DeSerializeArray`, BUG-12) — this fix neither reads nor writes the
  template format.
- **De-dup (BUG-14):** when the modules are later merged into one shared DSP
  library, this method already converges to a single canonical body (modulo the
  `Engine.`/`H_FELDOLGOZO.` token), so no extra reconciliation is needed.

---

## 6. Self-verification WITHOUT a compiler

1. **Read both methods post-edit** and confirm the only token differences from
   the canonical block above are: (a) Turan_core uses `Engine.mfcc_lpc_vect_num`,
   LITE uses `H_FELDOLGOZO.mfcc_lpc_vect_num`; (b) nothing else.
2. **No stray magic numbers:** within the method range, search for `magic`,
   `13`, `12`, `11` — should return only `order`, `order - 1`, `order - 2`
   (and the unrelated `1e-6`, `0.5`, `2`, `1.0` math constants). Confirm zero
   occurrences of `magic13`.
3. **Bounds trace** (do by hand for order = 12, 15, and 60): list the maximum
   index touched for `m2, rf, rf1, rr, a, aa, ar, ar1, ar2`; verify each is
   `≤ order-1` (i.e. in range) and that fixed accesses need only `order ≥ 2`.
   Already tabulated in §1.
4. **Regression check for order = 13:** substitute `order = 13` into the new
   loops; confirm they reproduce `13`, `11` (= order−2), `12` (= order−1)
   exactly, matching the original LITE literals → no behaviour change on legacy
   data.
5. **Reachability check:** confirm `distanceType` remains `"Euclidean"` with no
   setter in either file → live recognition behaviour is provably unchanged.
6. **Copy-parity check:** diff the two `ITDDistance` bodies (ignoring the single
   `Engine.`/`H_FELDOLGOZO.` token) → they must be identical.

---

## 7. Out of scope (do not do here)

- Do **not** enable Itakura (do not add a `distanceType` setter or change the
  default). That is a separate behavioural decision and would require validating
  the metric numerically against a reference (Voicebox `distitar`).
- Do **not** touch `num_of_vectoritems`, the `120`/`256` caps, or the
  `EuclideanDistance` loop bound — those are BUG-08.
- Do **not** edit the dead Java port `kódok/match.java` — BUG-15.
- **Sibling, optional:** `AbsDistance` (Turan_core line 247 / LITE line 249) has
  the same hard-coded `for (i = 0; i < 13; i++)` and the same latent
  width-mismatch (it is the dead `"Absolute"` branch). The identical `order`
  substitution applies if the team wants it folded in, but to keep BUG-09 minimal
  it is left out by default and noted here explicitly.

---

## Peer review

**Reviewer verdict: APPROVED.** Correct, complete across the two live C# copies,
behaviour-preserving for the historical width-13 case, introduces no new bug. The
two non-blocking prose corrections from the prior review round have been **folded
into the body** (§1 now attributes the OOB-read to LITE/width-12 and the OOB-write
to Turan_core/always-15; §3/§6 now say "two **C# copies**" and call out the dead
`kódok/match.java` port). Verified against source 2026-06-27.

### What was confirmed against the real source
- **Turan_core static is never reassigned.** `grep -rn mfcc_lpc_vect_num Turan_core/`
  → only `Engine.cs:29` (`= 15`). `Engine.cs:111–119` uses a local
  `num_of_feature_vectors`. ⇒ Turan_core Itakura width is always 15.
- **LITE static is 12/15** via `Form1.cs:104,108`. ⇒ width-12 OOB read is a LITE
  symptom; Turan_core's defect is the line-331 OOB write only. Both fixed.
- **Turan_core line 331** `for (i = 0; i < magic13; i++) ar[i+1] = a[i];` writes
  `ar[13]` into `new double[13]` = genuine latent OOB write. Fixed by `order-1`.
- **LITE body** all-literal `13`/`12`/`11`, internally consistent for 13, lacks
  the line-331 regression. Per-file deltas in §2 are accurate.
- **Bounds for order ∈ {12,15,60} hold**; max array index touched is `order-1`.
- **Completeness:** `find -name dtwApp_match.cs` → exactly two C# files;
  `ITDDistance` also appears only in the dead `kódok/match.java`, out of scope.
- **No data-format / backward-compat risk:** `distanceType` is `"Euclidean"` with
  no setter → Itakura branch unreachable; no serialized layout touched.
  `breaksCompat = false`.

---

## Peer review (independent, round 2 — verdict: APPROVED)

Re-verified the plan line-by-line against the live source on 2026-06-27. Every
factual claim the plan rests on checks out; the proposed change is correct,
complete across both C# copies, behaviour-preserving for legacy width-13 data,
and introduces no new defect. **`approved = true`, no required changes.**

### Confirmed against the real source
- **Two C# copies only.** `find -name dtwApp_match.cs` → exactly
  `Turan_core/Turan_core/dtwApp_match.cs` and
  `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs`. `ITDDistance` otherwise
  appears only in the dead `kódok/match.java` (out of scope). Complete.
- **Per-file literal map is accurate.** Turan_core `ITDDistance` (255–352): line
  257 `int magic13 = 13;`, `magic13` at 259–261/267/275/278/296/297/300/319/329/
  331/335/340/345, literal `11` at 283, literal `12` at 310. LITE (257–352): all
  `13`, with `11` at 283, `12` at 310 and 331. The §2 deltas match these exactly.
- **Turan_core line 331 OOB write is real.** `for (i = 0; i < magic13; i++) ar[i+1]
  = a[i];` over `ar = new double[13]` writes `ar[13]` → genuine latent
  IndexOutOfRange. `order - 1` fixes it. LITE line 331 already uses `12`, no OOB.
- **Order source is correct.** Turan_core static is never reassigned
  (`Engine.cs:111–119` writes a *local* `num_of_feature_vectors`; static stays
  15 from `Engine.cs:29`); LITE static is 12/15 via `Form1.cs:104,108`.
  `GetReferenceVector`/`GetSignalVector` (180/193) and the `cost[0,0]`/`tempcost`
  feeders (402–403, 466–467, 487) all allocate `new double[<live static>]`, so
  binding `order` to that same static makes the loop span exactly the array width
  handed in — never short, never past the end. Choosing the live static over the
  stale-capturable `num_of_vectoritems` is the right call.
- **Bounds are safe for order ∈ {12,15,60}.** Hand-traced every array
  (`m2,rf,rf1,rr,a,aa,ar,ar1,ar2`): max index touched is `order-1`; fixed accesses
  (`a[0]=rf[1]`, `rr[1]=-a[0]`) need only `order ≥ 2`. The width-12 OOB *read* in
  LITE (`rf[i]=ar1[i]` reaching index 12 of a 12-array) is eliminated.
- **order = 13 regression check holds.** Substituting 13 reproduces the original
  LITE literals exactly (`order=13`, `order-1=12`, `order-2=11`, arrays[13]) ⇒
  byte-identical behaviour on legacy width-13 data. The only behavioural delta is
  Turan_core's dead Itakura path moving from "throws" to "computes", which is the
  intended fix and unreachable at runtime (`distanceType="Euclidean"`, no setter).
- **Cross-fix contract consistent with BUG-01.** BUG-01 §2b sets
  `Engine.mfcc_lpc_vect_num = (byte)(4*num_of_feature_vectors)` (=60). Because
  BUG-09 binds `order` to that same static, `ITDDistance` follows to width 60 with
  no further edit and stays bounds-safe. No conflict; single source of truth honoured.
- **Backward-compat:** confirmed `breaksCompat = false` — Itakura branch
  unreachable, no serialized format touched.

### Non-blocking observations (do not gate approval)
1. **Duplicate heading.** The file now carries two `## Peer review` sections (the
   earlier round-1 verdict plus this one). Harmless, but the implementer may want
   to consolidate them before archiving the plan.
2. **`AbsDistance` is the same bug, same severity for LITE width-12.** Its
   `for (i = 0; i < 13; i++) … frame1[i]/frame2[i]` over 12-wide vectors is an
   identical latent OOB read on the dead `"Absolute"` branch. §7 already defers it;
   re-stated here only so the deferral is a conscious choice, not an oversight. The
   identical `order` substitution would fold it in cleanly if desired.
3. **Numerical validity of Itakura on non-13 / MFCC widths is intentionally NOT
   established** (§7). Correct to leave out — this fix is strictly de-magic + OOB
   removal; enabling the metric requires a separate numerical validation against
   Voicebox `distitar`.
</content>
</invoke>
