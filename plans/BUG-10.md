# BUG-10 — No confidence threshold / rejection (no FAR control) — FIX PLAN

**Severity:** P2 (medium) · **Class:** safety / robustness (false-accept control)
**Status:** plan only — no source edited.
**Group:** A-dtw-engine (`plans/_grouping.json`). Internal order 01 → 09 → **10** → 08 → 11.
**Scope (per grouping):** this fix lands the **REJECTED = -2 sentinel + normalized-cost
threshold MECHANISM only**. The calibrated threshold **value** is deferred
(`BUG-10-calibration`): it needs runtime cost-scale data that cannot be gathered
without running the app, and the cost scale itself shifts once BUG-01 widens the
feature vector (01 lands before 10). **Do NOT mark BUG-10 closed on the code diff** —
the mechanism ships inert (threshold `= +∞`) and BUG-10 stays open pending calibration.
**Companion:** `ROADMAP.md` (BUG-10), `reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md`

---

## 1. Root cause (restated from the code)

The DTW search engine always returns the `argmin`-cost template; there is no
"unknown / reject" outcome and no normalized-cost gate.

Trace:

- `dtwApp_match.bestMatch()` (Turan_core `dtwApp_match.cs:576-600`, LITE
  `dtwApp_match.cs:613-637`) loops over every template, calls
  `lefttorightMatch()`, and keeps the smallest `totalCost[templateIndex]`:

  ```csharp
  double temp = 10000.0;                       // <-- accidental, un-normalized reject cap
  for (templateIndex = 0; templateIndex < num_of_templates; templateIndex++)
  {
      setReference(templateIndex);
      lefttorightMatch();
      pathRecordList.Add(pathRecord);
      if (totalCost[templateIndex] < temp)     // only updates if best raw cost < 10000
      {
          recogResult = templateIndex;
          temp = totalCost[templateIndex];
      }
  }
  if (recogResult != -1)
      reference = template[recogResult];
  ```

  **The `-1` outcome is OVERLOADED (corrected — see Peer review).** `recogResult`
  is **not** always a valid index when `num_of_templates >= 1`. The seed
  `temp = 10000.0` (core `dtwApp_match.cs:579`, LITE `:616`) is an accidental,
  un-normalized absolute cap: if **every** template's raw `totalCost` is
  `>= 10000.0`, the `if` never fires and `bestMatch()` returns `-1` even with
  templates present. `recogResult` is also never reset at the top of `bestMatch()`
  (it is set to `-1` only in the two constructors, lines 142 / 159), so a *reused*
  object can additionally carry a stale index. Therefore today `-1` means
  **either** "zero templates" **or** "every template matched worse than the 10000
  seed", and a real index can be stale on reuse. This overloading is a pre-existing
  defect that this fix resolves at its source (§2, §3).

- `totalCost[templateIndex]` is set in `backTrace()` to the raw accumulated
  cost `cost2[minX, minY]` — a **sum of per-frame distances along the warping
  path**, never divided by length. So it is not comparable across utterances of
  different duration, and there is no absolute scale on which to threshold.

- `Engine.RecognizeAndReturnIndex` (`Engine.cs:79-145`) returns
  `dtwmatch.RecogResult` verbatim for both the `turan` and `htk` paths. The only
  way it returns `-1` is the no-templates / unsupported-format fallthrough
  (`Engine.cs:144`).

**Consequence:** out-of-vocabulary speech, silence, and noise that match below the
10000 seed are forced to the nearest command; those that match *above* it fall out
as a misleading `-1` ("no commands configured"). There is no *tunable, normalized*
mechanism to say "no command matched well enough," so the false-accept rate (FAR)
is uncontrolled, and the one accidental gate that exists (the 10000 cap) is
un-normalized, duration-dependent, and shifts arbitrarily once BUG-01 changes the
feature width / cost scale — the documented impact.

---

## 2. Design (single normalized rejection authority, opt-in threshold)

Add a confidence gate **inside `bestMatch()`** so both consumers benefit:
the core `Engine` path (Turan_core) and the standalone LITE Form1 path. The
**rejection decision** is disabled by default (`RejectionThreshold = +∞`), so no
input is ever flipped to `REJECTED` until a caller calibrates a finite threshold.
This satisfies the grouping contract "default behavior inert until calibrated":
the REJECTED outcome cannot fire while the threshold is `+∞`.

**Make the normalized gate the SOLE rejection authority (resolves the blocking
review).** Raise the loop seed `double temp = 10000.0;` to `double.PositiveInfinity;`
so the `argmin` template is **always** selected whenever `num_of_templates >= 1`,
and add `recogResult = -1;` at the top of `bestMatch()` so `-1` once again means
**only** "no templates" (including the reused-object case). The 10000 seed was an
accidental, un-normalized, duration-dependent reject cap that (a) overloaded `-1`,
(b) could never be tuned, and (c) silently truncated the new gate's reach — and
whose firing point shifts arbitrarily once BUG-01 widens the vector. Removing it
hands all accept/reject authority to the **normalized** gate below.

**Default-behavior change — stated plainly (not "bit-for-bit identical").** With
the threshold left at `+∞`, *rejection is still inert* (nothing returns `REJECTED`).
But raising the seed **does** change one default outcome: inputs whose best raw
`totalCost >= 10000` previously returned `-1` now return the `argmin` index. This
is a **deliberate removal of an accidental cap**, NOT enabling rejection — and it
is *honest*: when no rejection is configured the engine should pick the nearest
template, not masquerade a poor match as "no commands configured." For every input
that already produced an index (best cost `< 10000`), the result is unchanged.
Note Group A is already past "bit-for-bit identical" regardless: BUG-01 lands
before BUG-10, changes the feature width to 60 and the cost scale, and mandates
template regeneration. Preserving the old 10000-cap behavior would be meaningless
(that magic number was accidentally tuned for 15-dim costs and is garbage at 60).

**Normalization.** Compare the best template's cost normalized by the number of
signal frames `I = signal.getRowLength()`:

```
normalizedBestCost = totalCost[recogResult] / I
```

Justification from the code: in `lefttorightMatch()` the accumulated path cost
`cost2[I-1, J-1]` is the sum of exactly `I` per-frame distances (`cost[0,0]` plus
one `tempcost` added per signal column `i = 1..I-1`). Dividing by `I` yields the
**mean per-frame distance**, which is duration-independent and the right quantity
to threshold. Because `I` is the **same constant** for every template in a given
recognition, dividing by it **does not change the `argmin`** — so accepted
results are unchanged; only the accept/reject decision uses the normalized value.

**Reject sentinel.** Introduce a distinct constant `REJECTED = -2`. We must NOT
reuse `-1`: `-1` already means "templates not configured" (LITE `Form1.cs:510`
prints "A hangparancsok még nincsenek beállítva!"), a different condition. A
separate value keeps the two outcomes distinguishable.

**Tuning surface.** Expose the threshold as a settable static and expose the last
normalized best cost as a getter, so FRR/FAR can be tuned offline: run the engine
over labeled in-vocabulary and OOV/noise clips, collect `BestNormalizedCost` for
each, and pick the operating point. (No data/tooling for that exists in-repo; the
getter is what makes such tuning possible. The *value* picked is the deferred
`BUG-10-calibration` work — this fix ships the getter, not a number.)

**Getter is now always populated (resolves review caveat #3).** `bestNormalizedCost`
is written inside `if (recogResult != -1)`. Because the raised seed guarantees
`recogResult` is set whenever `num_of_templates >= 1`, `GetLastBestNormalizedCost()`
returns a real value for **every** input that has templates — including the worst
OOV/noise clips that the old 10000 cap would have capped to a stale `+∞`. The
offline FRR/FAR sweep can therefore collect costs across the full range, which is
the whole point of the tuning surface. (Had the seed been kept, the getter would
read stale `+∞` precisely on the worst inputs — the cases the sweep most needs.)

**Normalization is by frame count, orthogonal to the WIDTH CONTRACT.** The divisor
is `signal.getRowLength()` = number of signal **rows/frames** `I`, independent of
the per-frame vector **width** (15 / 48 / 60). The grouping WIDTH CONTRACT
(BUG-01/08/09/10) governs the column width fed to `EuclideanDistance`; this fix
touches none of that — it divides the accumulated path cost by the row count only.
The per-frame *scale* (which does grow with width after BUG-01) is intentionally
left to be absorbed by the later-calibrated threshold, not by this normalization.

---

## 3. Exact changes per file

### 3.1 `Turan_core/Turan_core/dtwApp_match.cs`

**(a) New fields** — add right after the `TotalCost` property (after line 132).

BEFORE:
```csharp
        public double[] TotalCost
        {
            get { return totalCost; }
            set { totalCost = value; }
        }
```
AFTER:
```csharp
        public double[] TotalCost
        {
            get { return totalCost; }
            set { totalCost = value; }
        }

        // BUG-10: optional confidence-based rejection.
        // Sentinel returned in RecogResult when the best match is too poor.
        // Distinct from -1 (which means "no templates configured").
        public const int REJECTED = -2;

        // Reject when (best totalCost / signal frame count) exceeds this.
        // Default +Infinity => rejection disabled => identical legacy behavior.
        public static double RejectionThreshold = double.PositiveInfinity;

        // Mean per-frame DTW cost of the winning template from the last
        // bestMatch() call. Used to tune RejectionThreshold (FRR/FAR sweep).
        private double bestNormalizedCost = double.PositiveInfinity;
        public double BestNormalizedCost { get { return bestNormalizedCost; } }
```

**(b) Reset `recogResult` + raise the seed** at the top of `bestMatch()`
(lines 578-579). This claims the `temp = 10000.0` line for BUG-10 so BUG-08 (array
caps, order 10 → 08) does not also touch it — no intra-Group-A edit conflict.

BEFORE:
```csharp
            pathRecordList.Clear();
            double temp = 10000.0;
```
AFTER:
```csharp
            pathRecordList.Clear();
            recogResult = -1;                          // BUG-10: -1 now means ONLY "no templates"
            double temp = double.PositiveInfinity;     // BUG-10: drop accidental 10000 reject cap;
                                                       // normalized gate below is the sole reject authority
```

**(c) Apply the gate** in `bestMatch()` (lines 596-599).

BEFORE:
```csharp
            if (recogResult != -1)
            {
                reference = template[recogResult];
            }
        }
```
AFTER:
```csharp
            if (recogResult != -1)
            {
                reference = template[recogResult];

                // BUG-10: normalized-cost rejection gate (opt-in).
                int sigFrames = (signal != null) ? signal.getRowLength() : 0;
                if (sigFrames <= 0) sigFrames = 1;            // avoid div-by-zero
                bestNormalizedCost = totalCost[recogResult] / (double)sigFrames;

                if (bestNormalizedCost > RejectionThreshold)
                {
                    recogResult = REJECTED;
                }
            }
        }
```
Note ordering: `reference` is assigned to the nearest template **before**
`recogResult` may be flipped to `REJECTED`, so `getReference()` still returns the
closest template (useful for diagnostics) even on a reject. With the raised seed,
the gate body now runs for **every** input that has templates (not just the
sub-10000 band), so the reject decision and the `bestNormalizedCost` capture cover
the full input range — including the worst OOV/noise clips.

### 3.2 `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs`

**Byte-identical change in the touched regions**, in this copy's namespace
(`Felismero_motor`). Note the two copies are **not** wholesale identical — this
copy reads width via `H_FELDOLGOZO.mfcc_lpc_vect_num` (lines 180/193/402/403/466/467)
whereas core reads `Engine.mfcc_lpc_vect_num`. But every line BUG-10 inserts or
changes references only `signal.getRowLength()`, `totalCost`, `recogResult`, and
the literal seed — none of the diverging symbols — so the BUG-10 edits are
character-for-character the same as 3.1. (Reconciling the width read-sites is
BUG-01's job, not BUG-10's.)

- (a) Insert the same field block after the `TotalCost` property (this copy:
  lines 128-132).
- (b) Same reset + seed-raise at the top of `bestMatch()` (this copy: lines
  615-616 — `pathRecordList.Clear();` then `double temp = 10000.0;`) as 3.1(b).
- (c) Replace the same
  `if (recogResult != -1) { reference = template[recogResult]; }` block
  (this copy: lines 633-636) with the gated version from 3.1(c).

### 3.3 `Turan_core/Turan_core/Engine.cs`

Expose the tuning surface and the public reject constant so external callers
(e.g. `Turan_tester`) can configure the gate and recognize a rejection. The
return path needs **no change** — `RecognizeAndReturnIndex` already returns
`dtwmatch.RecogResult`, which will carry `REJECTED` when the gate fires.

**(a) Public constant + threshold passthrough** — add after the `vector_format`
field (after line 31).

BEFORE:
```csharp
        public static byte mfcc_lpc_vect_num = 15;
        EngineMode engine_mode = EngineMode.mfcc;
        VectorFileFormat vector_format = VectorFileFormat.htk;
```
AFTER:
```csharp
        public static byte mfcc_lpc_vect_num = 15;
        EngineMode engine_mode = EngineMode.mfcc;
        VectorFileFormat vector_format = VectorFileFormat.htk;

        // BUG-10: confidence-based rejection surface.
        // REJECTED is returned by RecognizeAndReturnIndex when no template is a
        // good enough match (distinct from -1 = "no templates / bad format").
        public const int REJECTED = dtwApp_match.REJECTED;

        // Mean per-frame DTW cost threshold; +Infinity disables rejection.
        public static double RejectionThreshold
        {
            get { return dtwApp_match.RejectionThreshold; }
            set { dtwApp_match.RejectionThreshold = value; }
        }

        // Normalized best cost from the most recent recognition (tuning aid).
        private double last_best_normalized_cost = double.PositiveInfinity;
        public double GetLastBestNormalizedCost() { return last_best_normalized_cost; }
```

**(b) Capture the normalized cost** after each `bestMatch()`. Two edit sites
(turan path ~line 97, htk path ~line 132). The two blocks are textually
identical, so apply the edit to **both** occurrences.

BEFORE (each site):
```csharp
                dtwmatch.bestMatch();

                score_list.Clear();
```
AFTER (each site):
```csharp
                dtwmatch.bestMatch();
                last_best_normalized_cost = dtwmatch.BestNormalizedCost;

                score_list.Clear();
```

---

## 4. Duplicated copies that need the same change

| File | Change |
|---|---|
| `Turan_core/Turan_core/dtwApp_match.cs` | 3.1 (a) fields + (b) reset/seed + (c) gate |
| `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` | 3.2 (mirror of 3.1 a/b/c) |
| `Turan_core/Turan_core/Engine.cs` | 3.3 (a)+(b, ×2 blocks) |

These are exactly Group A's `dtwApp_match.cs` ×2 + `Engine.cs` (`_grouping.json`).
`lpcData.cs` is verify-only for BUG-10 (no edit). Group B/S1 own the `Form1.cs`
caller-guard follow-up (§5) — not edited here.

There is **no** `Engine.cs` in `Felismero_motor_LITE` (LITE drives
`dtwApp_match` directly from `Felismero_motor/Form1.cs`). `dtwApp_match.cs` exists
in exactly these two modules (confirmed via `find . -name dtwApp_match.cs`);
`Turan_tester` has no `dtwApp_match.cs` — it goes through `Turan_core.Engine`.

---

## 5. Backward compatibility / data formats

- **On-disk template / vector format:** UNCHANGED. Templates remain `double[,]`
  blobs read by `Engine.DeSerializeArray` (BinaryFormatter) and the LITE
  `DeSerializeArray`; HTK `.mfc` reading is untouched. No new file is read or
  written. The gate operates purely on in-memory DTW costs.
- **`score_list` / `TotalCost` semantics:** UNCHANGED — still the raw
  per-template accumulated costs. The normalized value is a *separate* exposed
  number, so existing score displays (`Turan_tester/Form1.cs:131`,
  LITE `FillScoreListbox`) keep working.
- **Default rejection behavior:** INERT. `RejectionThreshold = +∞` ⇒
  `bestNormalizedCost > +∞` is always false ⇒ no input is ever flipped to
  `REJECTED`. Rejection does not fire until a finite threshold is calibrated
  (`BUG-10-calibration`, deferred).
- **Default outcome change (one, deliberate):** raising the seed to `+∞` means
  inputs whose best raw `totalCost >= 10000` now return the `argmin` index instead
  of `-1`. This is the removal of an accidental cap, **not** enabling rejection
  (see §2). Inputs that already produced an index (best cost `< 10000`) are
  unchanged. The claim "bit-for-bit identical default" from earlier drafts is
  therefore **retracted** and replaced by this precise statement — and is moot
  inside Group A anyway, since BUG-01 (ordered before BUG-10) already shifts the
  feature width and cost scale.
- **API additions only:** new public `const REJECTED`, static
  `RejectionThreshold`, and getters. No existing signature changes. No compat
  break.

### Caller guard REQUIRED before enabling rejection (out of scope to edit now)

Raising the seed already **fixes the LITE misleading-message defect at its source**:
previously a poor (above-10000) match returned `-1`, surfacing
*"A hangparancsok még nincsenek beállítva!"* (`Form1.cs:512`) for an utterance that
was really just a bad match. With the seed at `+∞`, `-1` appears only for the
genuine no-templates case, so that message is now correct. The remaining precondition
is only for the *enabled* gate.

If/when `RejectionThreshold` is set to a finite value, callers that index by the
result MUST handle `REJECTED (-2)`:

- `Felismero_motor_LITE/Felismero_motor/Form1.cs` (confirmed this session):
  line 510 `if (dtwmatch.RecogResult == -1)` prints the "not configured" message;
  the `else` (line 516) does `lb_mfcc_files.Items[dtwmatch.RecogResult].ToString()`.
  With `RecogResult == -2` that `else` throws `ArgumentOutOfRangeException`. Before
  enabling the gate in LITE, extend line 510 to
  `if (dtwmatch.RecogResult == -1 || dtwmatch.RecogResult == dtwApp_match.REJECTED)`
  (or add a dedicated "nem ismerhető fel" branch).
- `Turan_tester/Turan_tester/Form1.cs` (confirmed this session): the call at
  line 107 feeds `doRecognizedAction(int number)` (line 120), which only does
  `word_recognized = number;` (123) and prints `word_recognized.ToString()` (129),
  so it shows `-2` harmlessly (no crash). A friendly "rejected" label is optional.

These `Form1.cs` files are deliberately **not** edited in this plan. Per
`_grouping.json` they belong to **Group B / sequential S1**, not Group A — editing
them here would break file-disjointness, and they are unnecessary while the gate is
inert. They are documented here as the precondition for turning the gate on.

---

## 6. Shared contracts other bug fixes depend on / interact with

- **Serialization contract (BUG-12):** `Creator.SerializeArray` ↔
  `Engine.DeSerializeArray` (BinaryFormatter `double[,]`). This fix neither reads
  nor alters that format, so it is independent of and does not block the BUG-12
  migration.
- **Feature dimensionality (BUG-01/02/03/04) & calibration deferral.** The *value*
  of a useful `RejectionThreshold` is empirical and scale-dependent: `totalCost`
  magnitude depends on the per-frame distance, hence on feature dimension/extraction.
  BUG-01 (ordered **before** BUG-10 in Group A) widens the vector to 60 dims and
  raises the cost scale; BUG-02/03 change feature content. This is exactly why
  `_grouping.json` defers `BUG-10-calibration`: the threshold value must be measured
  on the *post-BUG-01/02* cost scale, which needs runtime data unavailable in this
  environment. This fix therefore ships the **mechanism inert** (`+∞`) and BUG-10
  stays open. Normalizing by frame count (not by dimension) is intentional: it
  isolates duration, leaving per-frame scale to be absorbed by the calibrated
  threshold. The same coupling is why the accidental 10000 seed had to go — a
  fixed cost cap is meaningless once the width changes.
- **Reject-sentinel contract (`_grouping.json` → sharedContracts "REJECTED
  SENTINEL"):** `Engine.REJECTED == dtwApp_match.REJECTED == -2`, distinct from the
  `-1` "no templates" sentinel. This fix additionally makes `-1` mean *only*
  no-templates (seed-raise + reset), so `-1` and `-2` are now cleanly separated.
  Any future caller-side handling (BUG-11 stub work, UI, the Group B/S1 `Form1.cs`
  guard) should treat `-2` as the reject outcome. Keep both `dtwApp_match` copies'
  `REJECTED` equal to `-2` and the gate logic byte-identical.

---

## 7. Self-verification without a compiler

1. **Rejection inertness + scoped default change (most important).** Confirm
   `RejectionThreshold` defaults to `double.PositiveInfinity` in both `dtwApp_match`
   copies. Read the gate: `bestNormalizedCost > +∞` is always `false`, so the
   `REJECTED` flip never fires by default — rejection is inert (satisfies the
   grouping "inert until calibrated" contract). The **one** default outcome that
   changes is from the seed-raise: inputs whose best raw `totalCost >= 10000` now
   return `argmin` instead of `-1` (§2/§5). Verify no other path is affected:
   inputs with best cost `< 10000` keep the same `argmin`; no data/format change;
   no new exception when `signal` is non-null (div-by-zero guarded, §7.4).
2. **`argmin` preservation.** Verify the divisor `sigFrames` is constant across
   templates within one `bestMatch()` (it is `signal.getRowLength()`, independent
   of `templateIndex`). Dividing all candidate costs by the same constant cannot
   change which template is smallest ⇒ accepted results unchanged; only the
   reject decision uses the normalized number.
3. **Frame-count counting check.** Re-read `lefttorightMatch()`: `cost[0,0]` adds
   one distance; the `for (i=1; i<I; i++)` loop adds one `tempcost` per column.
   So `cost2[I-1,J-1]` is a sum of `I` distances ⇒ dividing by `I` = mean
   per-frame cost, with `I = signal.getRowLength()`.
4. **Div-by-zero guard.** Trace `sigFrames <= 0 ⇒ sigFrames = 1`. Cross-check
   `lpcData.getRowLength()` returns `data.Length / mfcc_lpc_vect_num`; for a real
   utterance `I >= 1`. The guard covers the degenerate empty-signal case.
5. **Sentinel non-collision + `-1` now unambiguous.** Grep confirmed no existing
   `-2` / `REJECTED` / `RejectionThreshold` usage in the tree, so the new sentinel
   is free. **After this fix** `-1` is produced **only** by the no-templates case:
   `recogResult = -1` at the top of `bestMatch()` plus the raised seed mean that
   with `num_of_templates >= 1` the first template always satisfies
   `totalCost[0] < +∞` and sets a real index; `recogResult` stays `-1` iff the loop
   never runs (`num_of_templates == 0`). The reused-object stale-index path (old
   §non-blocking note) is closed by the top-of-method reset. The Engine
   bad-format/unsupported fallthrough still returns its own literal `-1`
   (`Engine.cs:144`), independent of `bestMatch()`. Confirm the old overloaded
   reading ("`-1` also = all costs ≥ 10000") no longer holds.
6. **Duplicate parity.** Diff all three edited regions between the two
   `dtwApp_match.cs` copies — the field block (a), the reset/seed lines (b), and
   the gate block (c) must be character-identical (they reference no module-specific
   symbol; the width-read divergence `Engine.` vs `H_FELDOLGOZO.` is outside these
   regions). Confirm `double temp = 10000.0;` is gone from **both** copies and
   `recogResult = -1;` precedes it in **both**.
7. **Both Engine blocks edited.** Grep `last_best_normalized_cost =` in
   `Engine.cs` must return **two** hits (turan + htk), not one.
8. **`const` initializer legality.** `Engine.REJECTED = dtwApp_match.REJECTED`
   compiles only if `dtwApp_match.REJECTED` is itself a `const`. It is declared
   `public const int` ⇒ compile-time constant ⇒ legal. (If a reviewer prefers
   zero cross-type coupling, use a literal `public const int REJECTED = -2;` with
   a comment that it must equal `dtwApp_match.REJECTED`.)

---

## 8. Summary of edits

- `Turan_core/Turan_core/dtwApp_match.cs` — add reject fields (a); reset
  `recogResult = -1` + raise seed `10000.0 → +∞` (b); normalized gate in
  `bestMatch()` (c).
- `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` — same (mirror copy,
  byte-identical in all three regions).
- `Turan_core/Turan_core/Engine.cs` — public `REJECTED`, `RejectionThreshold`
  passthrough, `GetLastBestNormalizedCost()`, capture after both `bestMatch()`
  calls.

The **rejection decision** is inert by default (threshold `+∞`); calibration is
deferred (`BUG-10-calibration`) — **do not mark BUG-10 closed on the code diff**.
Raising the seed removes an accidental, un-tunable 10000 cost cap (one deliberate
default-outcome change: above-10000 inputs go `-1 → argmin`) and makes `-1` mean
only "no templates". No data/format break. The only required follow-up when
*enabling* the gate is a caller-side `REJECTED` guard in `Felismero_motor/Form1.cs`
(Group B/S1, documented, not edited here).

---

## Peer review

**Reviewer verdict: NOT approved (changes required).** The mechanical edits are
correct, complete, and backward-compatible, but the plan rests on a root-cause
claim that the source contradicts. That gap materially weakens the FAR-control
goal and must be addressed before implementation.

### What I verified as CORRECT
- **Line numbers / anchors all match.** Core `dtwApp_match.cs` `TotalCost`
  property at 128-132, gate block at 596-599; LITE copy at 128-132 and 633-636;
  `Engine.cs` fields at 29-31 and the two identical `bestMatch();` + blank +
  `score_list.Clear();` blocks at ~97 and ~132. LITE has `private lpcData signal;`
  (line 59) and uses `signal.getRowLength()` (line 378), so the inserted gate is
  genuinely module-agnostic and character-identical across both copies.
- **Frame-count normalization is sound.** `I = signal.getRowLength()` (line 378);
  `cost[0,0]` is one distance and each `i = 1..I-1` adds exactly one `tempcost`
  (line 513), so `cost2[I-1,J-1]` is a sum of `I` distances ⇒ `/I` = mean
  per-signal-frame cost. `I` is constant across templates ⇒ argmin preserved.
- **No integer-division trap.** `totalCost[recogResult] / (double)sigFrames` —
  divisor is cast to `double`; the `sigFrames <= 0 ⇒ 1` guard is correct.
- **Completeness across duplicates.** `find` confirms exactly two
  `dtwApp_match.cs` and one `Engine.cs`; the plan covers all three. `Turan_tester`
  goes through `Engine`, so it inherits the change. Correct.
- **Sentinel is free.** Grep confirms no pre-existing `REJECTED` /
  `RejectionThreshold` / meaningful `-2` usage. `const int REJECTED =
  dtwApp_match.REJECTED` is legal (const initialized from a const).
- **Default behavior is bit-for-bit identical.** `RejectionThreshold = +∞` ⇒
  `cost > +∞` always false. No data/format change. Confirmed.

### BLOCKING issue — the "-1 means only no templates" claim is false

`bestMatch()` seeds `double temp = 10000.0;` (line 579) and only assigns
`recogResult` when some `totalCost[templateIndex] < temp` (line 589). Therefore:

> **If every template's raw `totalCost` is ≥ 10000.0, the loop never updates
> `recogResult`, and a fresh `dtwApp_match` returns `-1`** — even though
> `num_of_templates >= 1`.

`totalCost` is the *un-normalized* sum of `I` per-frame Euclidean distances over
the whole utterance; for a long or poorly-matching input it can easily exceed
10000. So `temp = 10000.0` is already an **accidental, un-normalized, magic
absolute rejection cap**. This breaks two load-bearing statements in the plan:

1. §1 ("It stays `-1` only when there are zero templates") and §7.5 ("`-1` is
   still produced only by no-templates / bad-format paths … and `recogResult`
   left `-1` when `num_of_templates == 0`") are **incorrect**. `-1` is overloaded:
   it also means "every template matched worse than the 10000 seed."
2. The new normalized gate is placed *inside* `if (recogResult != -1)` and runs
   *after* the loop. Consequently the gate **can never fire for the worst inputs**
   — a match so bad that the best `totalCost ≥ 10000` already returns `-1` before
   reaching the gate. The REJECTED path only covers the narrow band
   `bestTotalCost < 10000  AND  normalizedCost > threshold`. For the FAR-control
   use case this is exactly backwards: the most obvious OOV/noise rejects are the
   ones that get classified as `-1` ("no commands configured") instead of
   REJECTED, and in LITE `Form1.cs:510` that surfaces the misleading message
   *"A hangparancsok még nincsenek beállítva!"* for what is really a rejected
   utterance.

This is a pre-existing defect, not one the plan introduces, and the opt-in code
itself is safe. But because BUG-10's stated purpose is *meaningful, tunable
rejection*, shipping a gate whose reach is silently truncated by an unnoticed
hard-coded cap — and documenting an invariant that is false — is not acceptable
as-is.

### Required changes
1. **Correct the analysis.** In §1 and §7.5, state plainly that `recogResult`
   is `-1` both when `num_of_templates == 0` *and* when all `totalCost ≥ 10000.0`
   (the `temp` seed at line 579). Drop the "-1 is unambiguous" justification or
   re-base it.
2. **Resolve the seed/gate interaction.** Pick one and document it:
   - **(preferred)** Make the normalized gate the *sole* rejection authority:
     raise the `temp` seed to `double.PositiveInfinity` (or `double.MaxValue`) so
     the argmin template is always selected, then let the normalized gate decide
     accept/REJECTED. Note this DOES change default behavior (inputs that today
     return `-1` would, with the gate disabled, return the argmin index), so it
     must be called out as a deliberate, justified change — and it means the
     §2/§5 "bit-for-bit identical default" claim must be qualified accordingly.
     This also fixes the `-1` overloading at its source.
   - **(if compat must be preserved)** Keep the seed, but explicitly scope the
     gate in the plan as covering only the sub-10000 band, and add the
     no-good-match `-1` case to the LITE caller-guard precondition in §5 (the
     `== -1` branch will already mis-message rejects today).
3. **Tuning getter caveat.** `bestNormalizedCost` is only written inside
   `if (recogResult != -1)`, so `GetLastBestNormalizedCost()` returns a stale
   `+∞` precisely on the worst inputs (those capped to `-1`). Note this in §2's
   tuning workflow — an FRR/FAR sweep over noisy OOV clips cannot collect costs
   for the capped cases unless change #2 (preferred) is taken.

### Non-blocking notes
- `recogResult` is not reset to `-1` at the top of `bestMatch()`; a *reused*
  `dtwApp_match` whose later call has all-poor (or zero) templates can carry a
  stale index. `Engine` allocates a fresh object per call so it is unaffected;
  LITE reuse should be spot-checked but this is outside BUG-10's scope.
- §3.3(b) edits two identical blocks — implementation must use a replace-all (or
  two explicit edits) and the §7.7 "two hits" check is the right guard.
- §7.8's literal-fallback option (`const int REJECTED = -2`) is fine; either form
  compiles.

---

## Revision 2026-06-27

This revision adopts the peer review's **preferred** resolution and conforms the
plan to `plans/_grouping.json` (Group A; internal order 01 → 09 → **10** → 08 → 11;
REJECTED-sentinel and WIDTH contracts; `BUG-10-calibration` deferral). All line
numbers re-confirmed against live source this session. Source `.cs` is unchanged
(plans only).

**Core resolution — make the normalized gate the sole rejection authority.** In
both `dtwApp_match.cs` copies, `bestMatch()` now (i) resets `recogResult = -1;` at
the top and (ii) raises the loop seed `double temp = 10000.0;` →
`double.PositiveInfinity;`. The gate body (3.1c) is unchanged. New edit step (b)
added to §3.1, mirrored in §3.2; §4/§8 updated to list edit (c).

| Blocking item (Peer review → Required changes) | How this revision closes it |
|---|---|
| **#1 — "`-1` means only no templates" is false (overloaded by the 10000 seed)** | §1 rewritten: `-1` is documented as overloaded today (zero templates **or** all `totalCost ≥ 10000`, plus stale-on-reuse). §7.5 rewritten: *after* the seed-raise + top-of-method reset, `-1` is produced **only** when `num_of_templates == 0`. The false invariant is removed and then re-established for the right reason. |
| **#2 — resolve seed/gate interaction (pick one, document)** | Took the **preferred** option: seed → `+∞` so `argmin` is always selected and the normalized gate alone decides accept/REJECTED (§2 "single normalized rejection authority"; edit §3.1(b)/§3.2(b)). The default-outcome change is stated plainly in §2 and §5: above-10000 inputs go `-1 → argmin`; this is removal of an accidental cap, **not** enabling rejection; rejection stays inert at `+∞`. The earlier "bit-for-bit identical default" claim is explicitly **retracted** in §5 and qualified in §2 (moot anyway since BUG-01 precedes BUG-10 and shifts the cost scale). |
| **#3 — tuning getter returns stale `+∞` on the worst inputs** | New §2 paragraph: because the raised seed guarantees `recogResult` is set for `num_of_templates ≥ 1`, `bestNormalizedCost` (written inside `if (recogResult != -1)`) and `GetLastBestNormalizedCost()` are now populated for **every** templated input, so the FRR/FAR sweep can collect the worst OOV/noise costs. §7.1 updated accordingly. |

**Non-blocking notes closed.**
- Reused-object stale-index note → fixed by the top-of-`bestMatch()`
  `recogResult = -1;` reset (now applies to LITE reuse too, not just Engine's
  fresh-per-call object). §1, §7.5, §7.6 updated.
- §3.3(b) two-block replace-all guard and the §7.8 literal-fallback option are
  retained as written.

**Grouping conformance added/asserted.**
- **Scope:** new Status block + §2/§6/§8 state this fix ships the **mechanism
  only**; threshold stays `+∞`; `BUG-10-calibration` is deferred; **BUG-10 is not
  closed on the code diff**.
- **Seed line ownership:** §3.1(b) explicitly claims the `temp = 10000.0` line for
  BUG-10 so BUG-08 (array caps, order 10 → 08) will not also edit it — avoids an
  intra-Group-A conflict.
- **WIDTH CONTRACT:** §2 + §6 note normalization is by `signal.getRowLength()`
  (frame **rows**), orthogonal to the per-array column **width** that BUG-01/08/09
  govern; BUG-01's cost-scale shift is cited as extra justification for deleting the
  fixed 10000 cap.
- **REJECTED SENTINEL:** §6 ties `Engine.REJECTED == dtwApp_match.REJECTED == -2`
  to the grouping contract and to the now-clean `-1`/`-2` separation.
- **File-disjointness:** §4 + §5 state the `Form1.cs` caller-guard follow-up belongs
  to Group B / sequential S1 and is **not** edited here; §5 also records that
  raising the seed already fixes the LITE *"…nincsenek beállítva!"* misleading
  message at its source for poor matches.

**Line numbers re-confirmed this session.** Core `dtwApp_match.cs`: `TotalCost`
128-132, `bestMatch()` 576-600, seed `temp = 10000.0` at 579, `pathRecordList.Clear()`
578, gate block 596-599, `recogResult = -1` only in ctors 142/159, `signal.getRowLength()`
378. LITE `dtwApp_match.cs`: `TotalCost` 128-132, `bestMatch()` 613-637, seed at 616,
clear at 615, gate block 633-636 (LITE reads width via `H_FELDOLGOZO.mfcc_lpc_vect_num`,
but no BUG-10-touched line references it). `Engine.cs`: fields 29-31, fresh
`dtwmatch` per call, two `bestMatch();`+blank+`score_list.Clear();` blocks at 97-99
and 132-134, unsupported-format `return -1;` at 144. Callers: LITE `Form1.cs:510`
(`== -1`), `:512` (message), `:516` (index); `Turan_tester/Form1.cs:107/120/123/129`.

---

## Re-review 2026-06-27

**Independent re-review against live source + `plans/_grouping.json` (not the plan's
own claims). Verdict: APPROVED — safe to implement.**

### Line/anchor re-confirmation (live source, this session)
All anchors verified by reading the files, not trusting the plan:
- Core `dtwApp_match.cs`: `TotalCost` property **128-132**; `bestMatch()` **576-600**;
  `pathRecordList.Clear()` **578**, seed `double temp = 10000.0;` **579**, gate block
  `if (recogResult != -1){ reference = template[recogResult]; }` **596-599**;
  `recogResult = -1` only in ctors **142/159**; `I = signal.getRowLength()` **378**;
  `totalCost[templateIndex] = cost2[minX,minY]` **568**.
- LITE `dtwApp_match.cs`: `TotalCost` **128-132**; `bestMatch()` **613-637**; clear
  **615**, seed **616**, gate block **633-636**; ctors **142/159**; `private lpcData
  signal;` **59**; `I = signal.getRowLength()` **378**. The gate-inserted symbols
  (`signal`, `getRowLength`, `totalCost`, `recogResult`, literal seed) all exist and
  are module-agnostic — character-identical edits to core are valid here. Confirmed
  the width divergence (LITE `H_FELDOLGOZO.mfcc_lpc_vect_num` vs core `Engine.…`) lies
  OUTSIDE every BUG-10-touched line (those are at 180/402/403/466/467, untouched).
- `Engine.cs`: fields **29-31**; two `bestMatch();`+blank+`score_list.Clear();` blocks
  **97-99** and **132-134**; unsupported-format `return -1;` **144**; `dtwmatch.RecogResult`
  returned at **106/141**. Fresh `dtwApp_match` per call (89/124).

### (1) Every previously-blocking issue is genuinely resolved
- **Blocker #1 (`-1` overloaded by the 10000 seed):** RESOLVED at source. With the
  top-of-`bestMatch()` `recogResult = -1;` reset **and** seed → `+∞`, the first template
  satisfies `totalCost[0] < +∞` (costs are sums of non-negative finite Euclidean
  distances — verified `cost[0,0]` at 417 + one `tempcost` per `i=1..I-1` at 513), so
  `recogResult` is always set when `num_of_templates ≥ 1`; it stays `-1` iff the loop
  never runs. The §1/§7.5 invariant is now true.
- **Blocker #2 (seed/gate interaction):** RESOLVED via the peer review's preferred
  option (seed → `+∞`, normalized gate is sole reject authority). Implemented in
  §3.1(b)/§3.2(b). The resulting default-outcome change (best raw cost ≥ 10000 goes
  `-1 → argmin`) is disclosed honestly in §2/§5; the old "bit-for-bit identical" claim
  is explicitly retracted.
- **Blocker #3 (getter stale `+∞` on worst inputs):** RESOLVED — `bestNormalizedCost`
  is written for every templated input now that `recogResult` is always set, so the
  FRR/FAR sweep can collect the worst OOV/noise costs.
- **Non-blocking reuse note:** the top-of-method `recogResult = -1;` reset closes the
  stale-index-on-reuse path for both Engine (fresh-per-call) and LITE (reuse).

### (2) No new defect / off-by-one / integer-division / vector-width / duplicate omission
- **Integer division:** `totalCost[recogResult] / (double)sigFrames` casts the divisor
  to `double`; `sigFrames <= 0 ⇒ 1` guard present. No integer-division trap.
- **Frame-count / off-by-one:** divisor `I = signal.getRowLength()` is the SAME value
  used as the `lefttorightMatch()` row bound (line 378) and the accumulated cost is a
  sum of exactly `I` distances — so `/I` is a well-defined mean and is robust even if
  BUG-01 later redefines `getRowLength()` to `data.GetLength(0)` (loop bound and divisor
  share the one call ⇒ stay consistent). No off-by-one.
- **Vector width:** normalization is by frame ROWS, never by per-frame column WIDTH;
  no BUG-10-touched line reads any width symbol. Orthogonal to the WIDTH CONTRACT and
  to BUG-01's `temp3/temp4` (466/467) / `EuclideanDistance` reconciliation. No collision.
- **argmin preserved:** `sigFrames` is constant across templates within one call ⇒
  dividing all candidate costs by it cannot change the minimum ⇒ accepted results
  unchanged.
- **No new crash:** the gate sets `reference = template[recogResult]` before any flip
  to `REJECTED`; no `-2` indexing inside `bestMatch()`. With threshold `+∞`,
  `x > +∞` is always false ⇒ `REJECTED` never fires ⇒ no caller ever sees `-2`.
  Independently confirmed via grep that the ONLY `RecogResult` consumers are
  `Engine.cs:106/141` (return) and LITE `Form1.cs:510/516`; both are safe under the
  inert default (LITE `:516` indexes a valid `argmin`, not `-2`).
- **Sentinel free:** grep across all `*.cs` shows no pre-existing `REJECTED`,
  `RejectionThreshold`, `BestNormalizedCost`, or meaningful `-2` usage.
- **Duplicate coverage:** exactly two `dtwApp_match.cs` (core + LITE) and one
  `Engine.cs` (no LITE `Engine.cs`); §3/§4 cover all three. Both Engine blocks
  (97-99, 132-134) are flagged for the replace-all/two-edit guard (§3.3b/§7.7).
- **`const` legality:** `Engine.REJECTED = dtwApp_match.REJECTED` is a const-from-const
  in the same assembly — legal; literal fallback offered.
- **Intra-Group-A conflict:** BUG-10 claims the seed line (579/616); BUG-08 is scoped
  to `costRecord` (147/164) only — disjoint. Internal order 01→09→10→08→11 matches the
  grouping.

### (3) Consistency with shared contracts and group scope
- **REJECTED SENTINEL contract:** `Engine.REJECTED == dtwApp_match.REJECTED == -2`,
  distinct from `-1`; default inert until calibrated. Met.
- **WIDTH CONTRACT:** untouched (frame-row normalization only). Met.
- **TRMS / BUG-12:** no on-disk format read/written; independent of S1. Met.
- **Scope:** ships mechanism only, threshold `+∞`, `BUG-10-calibration` deferred,
  BUG-10 NOT closed on the diff; `Form1.cs` caller-guard deferred to Group B/S1 (keeps
  file-disjointness). Met. Note the two per-module `RejectionThreshold` statics are
  independent (LITE's is unwired until its caller-guard lands) — correct and documented.
- **Seed-raise is in-bounds w.r.t. "default behavior inert" — grounded in the contract,
  not just engineering judgment.** `_grouping.json` `commitChunks[0]` ships **BUG-01 and
  BUG-10 in the SAME commit**, and BUG-01 changes the feature width/cost scale and
  mandates template regeneration. Therefore "default behavior inert until calibrated"
  provably **cannot** mean "this commit changes no observed output" — it can only mean
  "the *rejection outcome* stays inert," which `RejectionThreshold = +∞` satisfies. The
  seed-raise (poor matches `-1 → argmin`) is thus unambiguously inside the contract's
  intent. The same fact disposes of the stricter "keep the seed" alternative: at the
  post-BUG-01 cost scale the `10000` literal would fire arbitrarily, so retaining it is
  strictly worse than removing it.
- **LITE duplicate accumulation re-read directly (not inferred).** Confirmed LITE
  `dtwApp_match.cs` `lefttorightMatch()` has the identical DTW recurrence the
  normalization depends on: `cost[0,0] = frameDistance(...)` at **417**, the
  `for (i=1; i<I; i++)` loop at **420**, `cost[i,j] = minc + tempcost;` at **513**, and
  `totalCost[templateIndex] = cost2[minX,minY];` at **590** — i.e. a sum of exactly `I`
  distances, matching core. The frame-count normalization is therefore valid in BOTH
  copies, not just core (closing the "copies drift" risk this codebase warns about).

### Remaining issues
None blocking. Minor/informational only:
- §7.4 cites `getRowLength() = data.Length / mfcc_lpc_vect_num`; if BUG-01 edits
  `lpcData.getRowLength()` to `data.GetLength(0)` this prose goes slightly stale, but
  the normalization stays correct (divisor and loop bound share the one call). No action
  required by BUG-10.
- Shipping the gate inert removes the accidental 10000 cap, so LITE temporarily has
  ZERO rejection until `BUG-10-calibration`. This is deliberate and disclosed (§2/§5);
  acceptable because the cap was un-normalized and meaningless once BUG-01 shifts the
  cost scale. **Interim safety posture (state explicitly):** between this merge and
  `BUG-10-calibration` the build performs no input rejection at all — including the
  accidental net it previously had. For the assistive/medical command use case this is
  a real interim regression, so the post-merge/pre-calibration build must NOT be treated
  as production-ready for that use case until the calibrated threshold lands.

**Approved (approved=true).** The edits are mechanically correct, complete across all
duplicated copies, contract-consistent, and inert-by-default. Implement as written,
honoring the §7.7 two-Engine-block guard and byte-identical parity between the two
`dtwApp_match.cs` copies.

---

## Revision 2026-06-27 (pass 2 — independent live-source re-verification)

This pass re-opened **every** target `.cs` location and re-derived the facts the plan
depends on, rather than trusting the prior Revision/Re-review. No source was edited
(plans only). **Outcome: the plan already FULLY resolves all three blocking issues
from "## Peer review → Required changes" and conforms to `plans/_grouping.json`; no
substantive body change was required.** The only edit in this pass is this section,
which records the verification and the one factual point that had been stated loosely
in earlier drafts but is now confirmed exactly true.

### Every anchor re-confirmed by reading the files this pass

- **Core `dtwApp_match.cs`** — `TotalCost` property **128-132**; ctor
  `recogResult = -1` at **142** and **159**; `bestMatch()` **576-600** with
  `pathRecordList.Clear()` **578**, seed `double temp = 10000.0;` **579**, gate block
  `if (recogResult != -1){ reference = template[recogResult]; }` **596-599**;
  `totalCost[templateIndex] = cost2[minX, minY]` **568**; `I = signal.getRowLength()`
  **378**; `cost[0,0] = frameDistance(...)` **417**; `for (i=1; i<I; i++)` **420**;
  `cost[i,j] = minc + tempcost;` **513**. Width read-sites (`Engine.mfcc_lpc_vect_num`)
  at **180/193/402/403** — all OUTSIDE every BUG-10-touched line.
- **LITE `dtwApp_match.cs`** — `TotalCost` **128-132**; ctor resets **142/159**;
  `private lpcData signal;` **59**; `bestMatch()` **613-637** with clear **615**, seed
  **616**, gate block **633-636**; `totalCost[...] = cost2[minX, minY]` **590**;
  `I = signal.getRowLength()` **378**; `cost[0,0]` **417**; `for (i=1; i<I; i++)`
  **420**. Width read-sites use `H_FELDOLGOZO.mfcc_lpc_vect_num` (**180/193/402/403**),
  again OUTSIDE every BUG-10-touched line — so the three inserted regions are
  character-identical to core. (Note: the two copies are line-identical through the
  `lefttorightMatch` body up to ~420, then LITE runs +22/+37 lines longer because of a
  commented "FIX IT CODE x01" block before `backTrace`; this is why the `bestMatch()`
  anchors differ, 576→613, while the cost-recurrence anchors coincide.)
- **`Engine.cs`** — fields **29-31**; `RecognizeAndReturnIndex` **79-145** with the two
  textually identical `bestMatch();` + blank + `score_list.Clear();` blocks at **97-99**
  (turan) and **132-134** (htk); `return dtwmatch.RecogResult;` at **106/141**; a fresh
  `dtwApp_match` allocated per call at **89/124**; the unsupported-format `return -1;`
  at **144**; `MatchLength` stub **148-155**; `DeSerializeArray` (BinaryFormatter)
  **162-179**. `dtwApp_match` is reachable from `Engine` (already used at 84/89/124), so
  the `public const int REJECTED = dtwApp_match.REJECTED;` const-from-const in the same
  assembly is legal.

### The "sum of exactly I distances" justification is now verified exactly (not just plausibly)

Earlier drafts justified the `/I` normalization on `cost2[I-1, J-1]`. This pass
confirms that is precisely what runs: `backTrace(path, cost, I, J)` is called with the
live `cost` array, and inside it `minX = testlength-1 = I-1`, `minY = reflength-1 = J-1`
(core **537-538**, LITE same), so `totalCost[templateIndex] = cost2[minX, minY] =
cost[I-1, J-1]`. The `bestMatch` outer loop advances the signal index `i` by exactly 1
per step, so the accumulated-cost path holds exactly one `frameDistance` per signal
frame — `cost[0,0]` (one distance) plus one `tempcost` for each `i = 1..I-1` — i.e. a
sum of exactly `I` distances. Therefore `/I` is the true mean per-frame cost, and since
`I = signal.getRowLength()` is **constant across all templates within one
`bestMatch()`**, the division cannot change the `argmin` (accepted results unchanged;
only the reject decision uses the normalized value). The §2/§7.2/§7.3 claims hold as
written.

### Per blocking issue — confirmed closed in the plan body (not only in the prior revision log)

| Peer-review Required change | Where the body now closes it (re-read this pass) | Status |
|---|---|---|
| **#1 — "`-1` means only no templates" is false (10000 seed overloads `-1`)** | §1 (lines 44-54) documents `-1` as overloaded today (zero templates **or** all `totalCost ≥ 10000`, plus stale-on-reuse); §3.1(b)/§3.2(b) add `recogResult = -1;` reset **and** seed → `+∞`; §7.5 re-establishes `-1` = no-templates-only. Confirmed against source: with the reset + `+∞` seed, `totalCost[0] < +∞` always holds for `num_of_templates ≥ 1` (costs are sums of finite non-negative Euclidean distances), so `-1` survives iff the loop never runs. | CLOSED |
| **#2 — resolve seed/gate interaction; pick one and document** | Preferred option implemented: §3.1(b)/§3.2(b) raise the seed to `+∞` making the normalized gate the sole reject authority; the deliberate default-outcome change (best raw cost `≥ 10000` goes `-1 → argmin`) is stated plainly in §2 (96-106) and §5 (354-361), and the old "bit-for-bit identical default" claim is explicitly retracted there. | CLOSED |
| **#3 — getter returns stale `+∞` on the worst inputs** | §2 (135-142) + §7.1: because the raised seed guarantees `recogResult` is set whenever `num_of_templates ≥ 1`, `bestNormalizedCost` (written inside `if (recogResult != -1)`) and `GetLastBestNormalizedCost()` are populated for every templated input, so the FRR/FAR sweep can collect the worst OOV/noise costs. | CLOSED |
| Non-blocking: reused-object stale index | Closed by the top-of-`bestMatch()` `recogResult = -1;` reset (applies to LITE reuse, not just Engine's fresh-per-call object). | CLOSED |

### `plans/_grouping.json` conformance — re-checked this pass

- **Group / files / order:** BUG-10 is in **Group A-dtw-engine**, edits exactly
  `Turan_core .../dtwApp_match.cs`, `Felismero_motor_LITE .../dtwApp_match.cs`, and
  `Turan_core .../Engine.cs` (lpcData.cs verify-only) — matches the group file list;
  internal order `01 → 09 → **10** → 08 → 11` matches the `notes`.
- **REJECTED SENTINEL contract:** `Engine.REJECTED == dtwApp_match.REJECTED == -2`,
  distinct from `-1`; `RejectionThreshold` default `+∞` ⇒ inert until calibrated;
  both `dtwApp_match.cs` copies use identical sentinel + gate. Met.
- **WIDTH CONTRACT:** BUG-10 normalizes by frame **rows** (`signal.getRowLength()`),
  never by per-frame column width; no BUG-10-touched line reads any width symbol —
  orthogonal to BUG-01/08/09's `data.GetLength(1)` reconciliation. Met.
- **Scope cut / deferral:** ships the **mechanism only** (sentinel + threshold field);
  `BUG-10-calibration` is deferred (needs post-BUG-01/02 runtime cost-scale data);
  **BUG-10 is NOT closed on the code diff** — asserted in the Status block, §2, §6, §8.
- **Intra-Group-A disjointness:** BUG-10 claims the seed line (579/616); BUG-08 is
  scoped to `costRecord` (147/164) only — disjoint, no edit conflict. Confirmed both
  `costRecord = new double[120];` sit at 147/164 in each copy.
- **File-disjointness vs Group B/S1:** the `Form1.cs` caller-`REJECTED`-guard
  precondition is documented (§5) but **not** edited here; it belongs to Group B / S1.
- **Commit chunk:** `commitChunks[0]` ships BUG-01+08+09+10+11 in one commit over the
  same three files; the "default behavior inert" contract therefore means the
  *rejection outcome* stays inert (`RejectionThreshold = +∞`), not "no observable
  output change" — consistent with the seed-raise being in-bounds (§Re-review §3).

### No remaining blocking issues

Confirmed the only open items are the ones the plan already discloses and defers:
the calibrated threshold **value** (`BUG-10-calibration`), and the interim posture
that between this merge and calibration the build performs **no** input rejection at
all (the accidental 10000 cap is removed) — flagged as a real interim regression for
the assistive/medical use case (§Re-review, Remaining issues). Both are intentional
and out of scope for this mechanism-only fix. Implement as written.

---

## Re-review 2026-06-27 (independent adversarial pass — final gate)

**Verdict: APPROVED — safe to implement.** This pass re-derived every load-bearing
fact from the live `.cs` files and `plans/_grouping.json` directly, distrusting the
plan's own prior Revision/Re-review logs. All three originally-blocking peer-review
issues are genuinely closed *at source*, and no new defect was introduced.

### Anchors re-read this pass (not trusted from the plan)
- **Core `dtwApp_match.cs`** (namespace `Turan_core`): `TotalCost` **128-132**; ctor
  `recogResult = -1` at **142/159**; `bestMatch()` **576-600** — `pathRecordList.Clear()`
  **578**, seed `double temp = 10000.0;` **579**, gate `if (recogResult != -1){ reference
  = template[recogResult]; }` **596-599**; `totalCost[templateIndex] = cost2[minX,minY]`
  **568**; `cost[i,j]` initialized to **-1.0** at **392**; `cost[0,0]=frameDistance(...)`
  **417**; outer loop `for (i=1; i<I; i++)` **420** with predecessor read `cost[i-1,k]`
  **498** and `cost[i,j]=minc+tempcost` **513**; `I=signal.getRowLength()` **378**.
  `costRecord = new double[120]` (BUG-08's target) at **147/164** — disjoint from the
  seed line BUG-10 claims.
- **LITE `dtwApp_match.cs`** (namespace `Felismero_motor`): identical edited regions —
  `TotalCost` **128-132**, ctors **142/159**, `private lpcData signal;` **59**,
  `bestMatch()` **613-637** (clear **615**, seed **616**, gate **633-636**), recurrence
  identical (predecessor `cost[i-1,k]`, `I=signal.getRowLength()` **378**). Width read
  divergence (`H_FELDOLGOZO.mfcc_lpc_vect_num`) is outside every BUG-10-touched line.
- **`Engine.cs`**: fields **29-31**; two textually identical `bestMatch();`+blank+
  `score_list.Clear();` blocks at **97-99/132-134**; `return dtwmatch.RecogResult;`
  **106/141**; fresh `dtwApp_match` per call **89/124**; unsupported-format `return -1`
  **144**. Same assembly/namespace as `dtwApp_match` ⇒ `const REJECTED = dtwApp_match.REJECTED`
  legal.
- **Grep**: zero pre-existing `REJECTED` / `RejectionThreshold` / `BestNormalizedCost`
  / `last_best_normalized` usage anywhere ⇒ sentinel and surface are free.

### (1) Previously-blocking issues — genuinely resolved
- **#1 (`-1` overloaded by the 10000 seed):** CLOSED at source. `totalCost[templateIndex]
  = cost[I-1,J-1]` is always a *finite* value — either a sum of non-negative `frameDistance`
  terms, or the finite `-1.0` init sentinel if the path never reached the corner; never
  `±∞`/NaN. So with `recogResult=-1` reset at the top of `bestMatch()` **and** seed `+∞`,
  `totalCost[0] < +∞` always holds for `num_of_templates ≥ 1` ⇒ `recogResult` is always
  assigned; it survives as `-1` iff the loop body never executes (zero templates). The
  invariant the plan re-establishes is true.
- **#2 (seed/gate interaction):** CLOSED via the peer review's preferred option (seed →
  `+∞`, normalized gate is the sole reject authority). The one deliberate default-outcome
  change (best raw cost ≥ 10000 goes `-1 → argmin`) is disclosed honestly in §2/§5 and the
  "bit-for-bit identical default" claim is explicitly retracted.
- **#3 (getter stale `+∞` on worst inputs):** CLOSED — `bestNormalizedCost` is now written
  for every templated input, so the deferred FRR/FAR sweep can collect worst-case costs.
- **Non-blocking reuse note:** CLOSED by the top-of-method `recogResult=-1` reset.

### (2) No new defect introduced
- **Integer division:** `totalCost[recogResult] / (double)sigFrames` — divisor cast to
  `double`; `sigFrames <= 0 ⇒ 1` guard present. Clean.
- **Off-by-one / frame count:** the divisor `I = signal.getRowLength()` is the *same call*
  used as the recurrence's outer-loop bound, and the predecessor is strictly column `i-1`
  (line 498) so the path holds exactly one `frameDistance` per signal frame — `cost[0,0]`
  plus one `tempcost` for `i=1..I-1` = exactly `I` distances. `/I` is the true mean; divisor
  and loop bound share the call ⇒ stay consistent even if BUG-01 later redefines
  `getRowLength()`. No off-by-one.
- **Vector width:** no BUG-10-touched line reads any width symbol; normalization is by
  frame ROWS only. Orthogonal to the WIDTH CONTRACT and BUG-01's temp1/temp3/temp4 (466/467)
  / `EuclideanDistance` reconciliation. No collision.
- **argmin preserved:** `sigFrames` constant across templates within one call ⇒ dividing all
  candidate costs by it cannot move the minimum. Accepted results unchanged.
- **No new crash:** `reference = template[recogResult]` is assigned with a valid index
  *before* any flip to `REJECTED`; no `-2` indexing occurs inside `bestMatch()`. With
  threshold `+∞`, `finite > +∞` is always false ⇒ `REJECTED` never fires ⇒ no consumer
  (`Engine.cs:106/141`, LITE `Form1.cs:510/516`) ever sees `-2` under the inert default.
- **Duplicate coverage:** exactly two `dtwApp_match.cs` + one `Engine.cs`; all three covered;
  both Engine blocks flagged for the two-edit/replace-all guard. Edited regions are
  character-identical across the two `dtwApp_match.cs` copies (no module-specific symbol in
  any inserted line).

### (3) Contract / scope consistency
- **REJECTED SENTINEL contract:** `Engine.REJECTED == dtwApp_match.REJECTED == -2`, distinct
  from `-1`, default `+∞` ⇒ inert. Met.
- **WIDTH CONTRACT:** untouched (row normalization only). Met.
- **BUG-12 / TRMS:** no on-disk format read or written. Independent of S1. Met.
- **Scope:** ships mechanism only; `BUG-10-calibration` deferred; BUG-10 not closed on the
  diff; `Form1.cs` caller-guard deferred to Group B/S1 preserving file-disjointness. Met.
- **Intra-Group-A disjointness:** BUG-10 owns the seed line (579/616); BUG-08 owns
  `costRecord` (147/164) — disjoint, no edit conflict. Order 01→09→10→08→11 honored.

### Remaining issues — none blocking
- **Informational (for the deferred calibration phase, not this diff):** a DTW path that
  fails to reach the `[I-1,J-1]` corner leaves `totalCost = -1.0` (the array init sentinel,
  line 392). Such a value is `< 0`, so it would win the `argmin` AND, once a finite positive
  `RejectionThreshold` is calibrated, would be *accepted* (negative mean cost < threshold).
  This is a PRE-EXISTING pathology (the old 10000 seed had the same `-1 < 10000` behavior),
  not introduced by BUG-10, and is masked while the gate is inert. It should be considered
  during `BUG-10-calibration` (e.g. treat negative/sentinel costs as non-matches), but it is
  out of scope for this mechanism-only fix and does not block it.
- The §7.4 prose referencing `getRowLength() = data.Length / mfcc_lpc_vect_num` will read
  slightly stale if BUG-01 rewrites `getRowLength()` to `data.GetLength(0)`; the normalization
  stays correct regardless (divisor and loop bound share the one call). No action required.
- Interim safety posture stands: between merge and calibration the build performs no input
  rejection at all; the post-merge/pre-calibration build is not production-ready for the
  assistive/medical use case until the calibrated threshold lands. Disclosed; acceptable.

**Approved (approved=true).** Mechanically correct, complete across all duplicated copies,
contract-consistent, inert by default. Implement as written, honoring the §7.7 two-Engine-block
guard and byte-identical parity between the two `dtwApp_match.cs` copies.
