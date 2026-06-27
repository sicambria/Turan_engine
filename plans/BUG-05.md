# Fix Plan — BUG-05: `fir_filter` aliases caller's array and is recursive (IIR), not FIR

**Severity:** P1 (high)
**Status of path:** Partial-live. `fir_filter` is a standalone variant. The live native feature path uses `win_fir_hamming` instead (BUG-02). `fir_filter` has exactly **one** live caller — the LITE GUI button handler `btn_fir_Click` (`Felismero_motor_LITE/Felismero_motor/Form1.cs:405`). It is **not** called in `Turan_creator` at all (the creator only defines it). Fix regardless to avoid future misuse and to keep the two pre-emphasis implementations consistent.

---

## 1. Root cause (restated from the code)

Both copies are **byte-identical** (verified with `diff`):

- `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs:142-152`
- `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs:118-128`

```csharp
public static double[] fir_filter(double[] pcmdata)
{
    double[] tmparray = new double[pcmdata.Length];
    tmparray = pcmdata;                                   // (a) ALIAS

    for (int i = 1; i < pcmdata.Length - 1; i++)          // (c) off-by-one: skips last sample
    {
        tmparray[i] = tmparray[i] - (0.95 * tmparray[i - 1]);  // (b) RECURSIVE: reads filtered i-1
    }
    return tmparray;
}
```

Three defects:

- **(a) Aliasing.** `double[] tmparray = new double[pcmdata.Length];` allocates a fresh buffer, then `tmparray = pcmdata;` immediately **throws that buffer away** and rebinds `tmparray` to the *same object* the caller passed. Every write below mutates the **caller's PCM array in place**. In the LITE GUI, `pcmdata` is a member field (`Form1.cs:55`); after `btn_fir_Click` runs, `firdata` and `pcmdata` reference the **same** corrupted array, and the original PCM is destroyed until the next file load (`Form1.cs:213/650`).
- **(b) Recursion (IIR, not FIR).** Because of the alias, the loop reads `tmparray[i-1]`, which has *already been overwritten* by the previous iteration. So the recurrence is `y[i] = x[i] − 0.95·y[i−1]` — `tmparray[i]` still holds the original input at the moment it is read, but the `i−1` term is a **prior output**, giving infinite-impulse-response feedback — not the intended first-order FIR pre-emphasis `y[i] = x[i] − 0.95·x[i−1]`. The response is wrong and the error accumulates along the signal.
- **(c) Off-by-one.** The loop bound `i < pcmdata.Length - 1` never writes the final sample (index `Length-1`). With the alias removed, that cell would be left at its default `0.0`. (Same defect class as BUG-06; fixing it here is part of making the FIR correct — every output sample must be computed.)

Intended behavior (standard speech pre-emphasis, coefficient 0.95, matching `win_fir_hamming` lines 204/208): `y[0] = x[0]`, `y[i] = x[i] − 0.95·x[i−1]` for `i ≥ 1`, over the **full** length.

---

## 2. Exact change (per file)

Apply the **identical** edit to both copies.

### BEFORE
```csharp
        public static double[] fir_filter(double[] pcmdata)
        {
            double[] tmparray = new double[pcmdata.Length];
            tmparray = pcmdata;

            for (int i = 1; i < pcmdata.Length - 1; i++)
            {
                tmparray[i] = tmparray[i] - (0.95 * tmparray[i - 1]);
            }
            return tmparray;
        }
```

### AFTER
```csharp
        public static double[] fir_filter(double[] pcmdata)
        {
            double[] tmparray = new double[pcmdata.Length];

            if (pcmdata.Length > 0)
            {
                tmparray[0] = pcmdata[0];                 // y[0] = x[0]; no x[-1]
            }
            for (int i = 1; i < pcmdata.Length; i++)
            {
                tmparray[i] = pcmdata[i] - (0.95 * pcmdata[i - 1]);   // FIR: read ORIGINAL input
            }
            return tmparray;
        }
```

Key points of the fix:
- **Remove `tmparray = pcmdata;`** — never rebind the parameter to the freshly allocated buffer. The caller's array is now never mutated.
- **Read from `pcmdata[...]` (original input), not `tmparray[...]`** — makes it a true FIR (no feedback).
- **Loop to `i < pcmdata.Length`** (was `Length - 1`) and seed `tmparray[0] = pcmdata[0]` — computes every output sample, fixing the off-by-one and the now-zeroed first sample.

### Exact locations
1. `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs` — lines **142–152**.
2. `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` — lines **118–128**.

---

## 3. Duplicated copies needing the same change

Verified via `grep -rn "fir_filter"` across the tree. The function is defined in exactly the two files above (confirmed byte-identical via `diff`). `Turan_core` does **not** contain a `fir_filter`. So **2 copies**, identical edit to both. No third copy exists for this symbol (unlike `HTK_Interface.cs`).

Caller inventory (for impact, not edits):
- `Felismero_motor_LITE/.../Form1.cs:405` — live (`btn_fir_Click`).
- `Felismero_motor_LITE/.../Form1.cs:1569` — commented out (dead).
- No callers in `Turan_creator` (it uses `win_fir_hamming`, `Creator.cs:158`).

---

## 4. Backward compatibility / on-disk formats

**No format impact; non-breaking.**
- `fir_filter` operates entirely in-memory on a `double[]` PCM buffer; it does not touch template files, serialization, or any on-disk/data format.
- It is **not** on the path that produces stored templates (that path is `win_fir_hamming` + mel/LPC + `SerializeArray`). Therefore stored `.mfcc`/template files are unaffected and no versioned read is required.
- The output *values* change for the one live GUI caller, but that handler only updates an in-memory display field (`firdata`, 1-D, `Form1.cs:56`); nothing persisted depends on it. Verified by `grep -n "firdata\|hammingdata"` over LITE `Form1.cs`: `firdata`/`hammingdata` are written **only** by the debug buttons (lines 405/411) and otherwise appear solely in commented-out code (351-352, 1569-1574). The production template/DTW path uses the **2-D** `win_hammingdata`/`win_meldata` produced by `win_fir_hamming` (lines 680-688) — a completely separate buffer — so `fir_filter` touches nothing that is serialized. The previously-destructive side effect on `pcmdata` is removed — strictly an improvement, no consumer relied on the corruption.

---

## 5. Shared contract with other bug fixes

- **Coefficient consistency with BUG-02 (`win_fir_hamming`).** Both functions implement first-order pre-emphasis with the same coefficient `0.95` (confirmed at `win_fir_hamming` lines 204/208). This fix keeps `fir_filter` using `0.95` and the same `y[i] = x[i] − 0.95·x[i−1]` recurrence form, so the two pre-emphasis implementations stay semantically identical. If BUG-02's fix changes the coefficient or the `i==0` boundary convention, mirror the same choice here (and vice-versa) so they do not drift.
- **No serialization contract.** Unlike BUG-12's `SerializeArray`/`DeSerializeArray` pairing, BUG-05 shares no on-disk format contract with any other bug. It is self-contained.
- **Relation to BUG-06 (off-by-one in `hamming_ablak`).** Same defect class (`< Length - 1`). The off-by-one fixed here is local to `fir_filter`; it does not substitute for BUG-06's separate fix at `hamming_ablak:165`.

---

## 6. Self-verification without a compiler

1. **Alias removed:** Confirm the line `tmparray = pcmdata;` is gone and that no statement rebinds the `tmparray` reference. Grep both files: `grep -n "tmparray = pcmdata" <file>` must return nothing after the edit.
2. **No input mutation — trace by reads:** Every RHS array read in the loop must be `pcmdata[...]`; the only `tmparray[...]` occurrences must be on the **left** (writes). Visually scan the AFTER block: writes target `tmparray`, reads target `pcmdata`. Since `tmparray` is a distinct `new double[...]` never aliased, `pcmdata` is provably read-only → caller's buffer intact.
3. **FIR (non-recursive) by hand-trace** on a 4-sample input `x = [a, b, c, d]`:
   - `y[0] = a`
   - `y[1] = b − 0.95a`
   - `y[2] = c − 0.95b`
   - `y[3] = d − 0.95c`
   Each `y[i]` depends only on `x[i]` and `x[i−1]` (inputs), never on a prior `y` → confirms FIR. (Pre-fix trace gives `y[1]=b−0.95a`, then `y[2]=c−0.95·y[1]` — feedback — and `y[3]` never written.)
4. **Full coverage / no off-by-one:** Loop bound is `i < pcmdata.Length` and index 0 is seeded → all `Length` outputs written; none left at default 0. Confirm no `Length - 1` remains in the function.
5. **Empty-input safety:** `pcmdata.Length == 0` → `if` skipped, loop body never enters → returns empty array, no `IndexOutOfRange`. `Length == 1` → `y[0]=x[0]`, loop skipped.
6. **Both copies match:** After editing, run `diff` on the two function bodies (lines 142–152 vs 118–128 regions) to confirm they remain byte-identical, preventing drift.

---

## 7. Behavior-change summary (minimal & justified)

| Aspect | Before | After | Justification |
|---|---|---|---|
| Caller's `pcmdata` | mutated in place | untouched | correctness — function must not corrupt input |
| Filter type | IIR (feedback) | FIR `y[i]=x[i]−0.95x[i−1]` | matches the function's name/intent & `win_fir_hamming` |
| Sample 0 | (alias) kept `x[0]` | `y[0]=x[0]` | standard pre-emphasis boundary, preserves old effective value |
| Last sample | (alias) kept `x[N-1]` | computed `x[N-1]−0.95x[N-2]` | off-by-one fix; every output computed |
| On-disk/template format | — | unchanged | no persistence touched |

---

## Peer review

**Verdict: APPROVED (approved=true).** The plan is correct, complete, and safe. Verified against the live source.

### What I checked and confirmed
- **Root cause (a/b/c) is accurate.** Re-read both definitions: `Turan_creator/.../H_FELDOLGOZO.cs:142-152` and `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:118-128`. They are body-identical (only line numbers differ). The `tmparray = pcmdata;` alias, the IIR feedback via `tmparray[i-1]`, and the `i < Length-1` off-by-one are all present exactly as described.
- **Hand-trace of the bug is correct.** Original loop writes indices `1..N-2`. `tmparray[0]` stays `x[0]` (never written), so `y[1]=x[1]-0.95x[0]` (correct), but `y[2]=x[2]-0.95·y[1]` (feedback → IIR). Index `N-1` is never written and, because of the alias, retains `x[N-1]`. Matches the plan's BEFORE/AFTER table row-for-row.
- **The fix is a true FIR.** AFTER reads only from `pcmdata[...]` (RHS) and writes only to the freshly-allocated `tmparray[...]` (LHS); `tmparray` is never rebound. `y[0]=x[0]` corresponds to the standard `x[-1]=0` boundary (`x[0]-0.95·0 = x[0]`), so sample 0's effective value is preserved. Empty (`Length==0`) and single-sample (`Length==1`) inputs are safe — no `IndexOutOfRange`.
- **Completeness across copies.** `grep -rn "fir_filter" --include=*.cs` returns exactly two definitions (the two above) plus two call sites in LITE `Form1.cs` (live `:405`, commented `:1569`). No copy in `Turan_core` or `Turan_tester`. The "2 copies, identical edit" scope is correct and complete.
- **Caller / backward-compat analysis is correct.** `pcmdata` (`Form1.cs:55`) and `firdata` (`:56`) are member fields; `btn_fir_Click` (`:405`) is a manual-debug button. Nothing on the persisted template/DTW path consumes `fir_filter` output (that path is `win_fir_hamming` → mel/LPC → `SerializeArray`). Removing the destructive in-place mutation is strictly an improvement and also makes repeated button clicks idempotent. No on-disk/format impact — confirmed.
- **Cross-fix consistency.** The `y[0]=x[0]` (= `x[-1]=0`) convention and `0.95` coefficient match what BUG-02 should adopt for `win_fir_hamming`; `fir_filter` + `hamming_ablak` together decompose `win_fir_hamming`, so the two pre-emphasis implementations stay semantically aligned. Section 5's note to keep them in lock-step is the right contract. The local off-by-one fix here does **not** stand in for BUG-06's separate fix at `hamming_ablak:165` — correctly stated.

### No blocking issues found
- No new bug introduced by the AFTER block. The `if (pcmdata.Length > 0)` guard is technically redundant for safety (the `for` already short-circuits and `tmparray[0]` would only execute when reachable), but it is harmless and improves readability — keep it.

### Non-blocking notes (optional)
1. When implementing, match the existing 8-space method-body indentation in both files so the two copies remain byte-identical (the plan's self-verification step 6 already mandates a post-edit `diff` — good).
2. Consider a one-line comment at the `y[0]=x[0]` seed noting the `x[-1]=0` convention, so a future maintainer reconciling BUG-02 sees the shared boundary choice without re-deriving it.

These are cosmetic; they do not change approval.
