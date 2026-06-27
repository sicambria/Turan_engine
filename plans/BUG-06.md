# FIX PLAN — BUG-06: `hamming_ablak` drops the last sample (off-by-one loop bound)

- **Severity:** P1 (ROADMAP classifies as 🟠 P1)
- **Type:** Localized correctness fix, single statement per file.
- **Breaks backward compatibility:** No.
- **Risk:** Low.

---

## 1. Root cause (restated from the code)

`hamming_ablak(double[] firdata)` allocates an output array the full length of its
input and then iterates with an upper bound that stops one element short:

```csharp
public static double[] hamming_ablak(double[] firdata)
{
    double[] tmparray = new double[firdata.Length];

    for (int i = 0; i < firdata.Length - 1; i++)        // <-- off-by-one
    {
        tmparray[i] = (firdata[i] * (0.54 - (0.46 * Math.Cos((const_2pi * i) / num_items_in_windowed_frame))));
    }
    return tmparray;
}
```

Because the loop condition is `i < firdata.Length - 1`, the last index
(`firdata.Length - 1`) is never written. `tmparray` is a freshly-allocated
`double[]`, so that final cell keeps the C# default value `0.0`. The function
therefore returns a windowed frame whose last sample is silently zeroed instead
of being multiplied by its Hamming coefficient. The sibling
`win_hamming_ablak` (2-D) uses the correct full bound
(`frame_item < num_items_in_windowed_frame`) and is **not** affected — this bug
is confined to the single-array overload.

---

## 2. Exact change per file

The change is identical in both copies: remove the `- 1` from the loop bound so
all `firdata.Length` samples are windowed.

### File A — `Turan_creator/Turan_creator/H_FELDOLGOZO.cs` (line 165)

**Before:**
```csharp
            for (int i = 0; i < firdata.Length - 1; i++)
```

**After:**
```csharp
            for (int i = 0; i < firdata.Length; i++)
```

### File B — `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` (line 141)

**Before:**
```csharp
            for (int i = 0; i < firdata.Length - 1; i++)
```

**After:**
```csharp
            for (int i = 0; i < firdata.Length; i++)
```

Nothing else in either method changes. The Hamming coefficient expression, the
divisor `num_items_in_windowed_frame`, the allocation, and the return are left
exactly as-is.

---

## 3. Duplicated copies requiring the same change

`H_FELDOLGOZO.cs` exists in two locations; the `hamming_ablak` method is
byte-identical in both (differing only in line number). **Both must be patched:**

| # | File | Line | Caller status |
|---|------|------|---------------|
| 1 | `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/H_FELDOLGOZO.cs` | 165 | No live caller (Creator pipeline uses `win_fir_hamming`, not `hamming_ablak`). |
| 2 | `/home/arsvivendi/git/Turan_engine/Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` | 141 | One live caller: `Form1.cs:411` `btn_calc_hamming_Click` (debug/diagnostic button). |

A repo-wide search for the single-array `hamming_ablak(` confirms only these two
definitions plus the one Engine button caller (`Form1.cs:411`); the other
`Form1.cs:1571` reference is commented out. There is no third copy of this
particular method (unlike `HTK_Interface.cs`, which has three copies but does not
contain this function).

The `old_string` `for (int i = 0; i < firdata.Length - 1; i++)` is unique within
each of the two files, so a literal find/replace cannot be misapplied to another
loop.

---

## 4. Backward / data-format compatibility

**No format change, no compatibility break.**

- `hamming_ablak` (the single-array overload) is **not** part of the template/
  feature-generation pipeline. The Creator pipeline windows audio via
  `win_fir_hamming` (`Creator.cs:158`), and the Engine pipeline likewise uses
  `win_fir_hamming` (`Form1.cs:680`). Neither serializes the output of
  `hamming_ablak`.
- The only live invocation of `hamming_ablak` is the Engine LITE debug button
  `btn_calc_hamming_Click` (`Form1.cs:411`), which writes the private field
  `hammingdata`. Every other reference to `hammingdata` in `Form1.cs` is
  commented out (lines 351–352, 1376–1377, 1571–1574), so the result is never
  consumed by recognition, scoring, or persistence.
- Consequently the fix changes **no** on-disk template format, no serialized
  feature vector, and no DTW input. Stored templates produced before the fix
  remain valid and comparable.

**Net runtime impact is effectively nil**: this is correctness hardening on a
function that is, in practice, dead/debug-only. It should be fixed for
correctness and to prevent future misuse, but it does **not** alter recognition
output today and must not be sold as a recognition-accuracy improvement.

---

## 5. Shared contracts / cross-bug dependencies

**None.** This change touches only a local loop bound inside one method.

- It does **not** interact with the `SerializeArray` (`Creator.cs:318-332`) /
  `DeSerializeArray` (`Engine.cs:162-179`) serialization contract referenced by
  other ROADMAP items.
- It does **not** depend on, nor is it depended on by, the fixes for the
  adjacent bugs in the same file:
  - **BUG-02** (`win_fir_hamming` dead pre-emphasis, lines 204/208 overwritten by
    line 210) — a different method; **out of scope here, do not touch**.
  - **BUG-05** (`fir_filter` IIR/aliasing) — different method; out of scope.

### Observed but intentionally NOT changed (scope guard)
- **Hamming denominator:** the code computes `cos(2π·i / 256)`
  (`num_items_in_windowed_frame`), whereas the textbook window and the method's
  own doc comment ("N") imply `N-1`. This is a separate concern, **not** BUG-06;
  leave the divisor untouched.
- **`win_fir_hamming` pre-emphasis** (BUG-02): untouched.

Keeping the edit to the single `- 1` removal preserves the surgical scope.

---

## 6. Self-verification without a compiler

1. **Bounds safety (no overflow introduced).** `tmparray` is allocated as
   `new double[firdata.Length]`. The corrected loop reads `firdata[i]` and writes
   `tmparray[i]` for `i` up to a maximum of `firdata.Length - 1`. Both arrays
   have length `firdata.Length`, so the highest index is exactly the last valid
   index of each. Extending the bound from `firdata.Length - 1` to
   `firdata.Length` adds exactly one iteration (`i = firdata.Length - 1`) and
   cannot read or write out of range. Confirmed by inspection — no IndexOutOfRange
   risk.
2. **Behavioral correctness.** Before the fix, `tmparray[firdata.Length - 1]`
   was left at the default `0.0`; after the fix it is set to
   `firdata[last] * hamming(last)`, matching every other index. Trace the loop on
   a length-N input: post-fix it writes indices `0 .. N-1` (all N); pre-fix it
   wrote `0 .. N-2` (N-1). This is the entire change.
3. **Duplicate parity.** Diff the two methods to confirm they were identical
   before and remain identical after:
   `diff <(sed -n '161,170p' Turan_creator/.../H_FELDOLGOZO.cs) <(sed -n '137,146p' Felismero_motor_LITE/.../H_FELDOLGOZO.cs)`
   should show only the two corrected lines match (and otherwise be empty).
4. **Uniqueness of edit target.** `grep -n "firdata.Length - 1"` in each file
   returns exactly the one line being changed, proving the find/replace is
   unambiguous.
5. **No new format/serialization touched.** `grep` for `hammingdata` and
   `hamming_ablak` confirms no serialization, file write, or DTW call consumes the
   single-array result — re-run after editing to confirm call sites are unchanged.

---

## Peer review

**Verdict: APPROVED.** The plan is accurate, complete, and surgically scoped.

### Verified against source
- **Root cause confirmed.** Both copies have `for (int i = 0; i < firdata.Length - 1; i++)` writing into a `new double[firdata.Length]` whose last cell stays at the C# default `0.0`. The diagnosis (last sample silently zeroed) is exactly right.
- **Fix is correct and overflow-safe.** Removing `- 1` adds exactly one iteration (`i = firdata.Length - 1`), and both `firdata[i]` and `tmparray[i]` are valid at that index since both arrays have length `firdata.Length`. No IndexOutOfRange risk. Post-fix the last sample is multiplied by its Hamming coefficient (`cos(2π·255/256) ≈ 1` → coeff ≈ 0.08), matching the rest of the window.
- **Completeness confirmed.** A repo-wide search for the single-array `hamming_ablak` returns exactly two definitions (`Turan_creator/.../H_FELDOLGOZO.cs:161` and `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:137`). `diff` of the two method bodies is empty (byte-identical), and both must be patched as the plan states. The other "hamming" hits the broad grep surfaces are unrelated: `CV_FELDOLGOZO.cs` `HAMMING_SIZE` (an FFT constant), `FFT-converted.cs` `hamming()` (commented out), `FMOD_FUNCTIONS.cs getSpectrumDataHamming` (FMOD DSP, different concern). None contains this method.
- **Edit-target uniqueness confirmed.** `grep -c "for (int i = 0; i < firdata.Length - 1; i++)"` returns `1` in each of the two files, so a literal find/replace cannot be misapplied.
- **Caller analysis confirmed.** No caller exists in the Creator project. The only live caller is the Engine LITE debug button `Form1.cs:411 btn_calc_hamming_Click`; `Form1.cs:1571` is commented out. The 2-D `win_hamming_ablak` correctly uses `frame_item < num_items_in_windowed_frame` and is unaffected.
- **Backward compatibility confirmed.** The single-array overload is not in the feature/template pipeline (both pipelines window via `win_fir_hamming`), and its output is never serialized or fed to DTW. No on-disk format changes; stored templates remain valid. The plan's caveat that this is correctness hardening on an effectively dead/debug path (not a recognition-accuracy improvement) is honest and correct.

### Non-blocking observations (do NOT expand scope)
1. **Divisor is a constant, not the input length.** The coefficient uses `cos(2π·i / num_items_in_windowed_frame)` where `num_items_in_windowed_frame` is the fixed constant `256`, not `firdata.Length`. If this overload were ever called with a frame whose length ≠ 256, the window shape would be wrong independent of BUG-06. The plan already flags the `N` vs `N-1` divisor question as out of scope; this length-vs-constant point is the same family of pre-existing issue and is likewise correctly left untouched, since the function is debug-only. Worth a one-line note in ROADMAP if/when this overload is ever promoted to live use, but it is not part of BUG-06.
2. **Self-verification step 3** (the `diff` of the two methods) was executed and returns empty / exit 0 as predicted — the verification recipe in the plan is sound.

No required changes. The two-line edit (remove `- 1` in each of the two files) should be applied exactly as written.

## Peer review (independent verification — BUG-06 reviewer)

**Verdict: APPROVED.** Independently re-verified against the live source (not relying on the prior review section above). The plan's root cause, fix, completeness, and compatibility claims all hold.

### Primary-source checks performed
- **Root cause confirmed at both sites.** `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:165` and `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs:141` both read `for (int i = 0; i < firdata.Length - 1; i++)` writing into `new double[firdata.Length]`. Index `firdata.Length - 1` is never written → stays at C# default `0.0`. Diagnosis is exact. The 2-D sibling `win_hamming_ablak` correctly bounds with `frame_item < num_items_in_windowed_frame` and is untouched.
- **Fix is correct and overflow-safe.** Dropping `- 1` adds exactly one iteration (`i = firdata.Length - 1`); both `firdata[i]` and `tmparray[i]` are valid there since both arrays have length `firdata.Length`. No IndexOutOfRange introduced; no other statement changes.
- **Completeness verified by repo-wide grep.** `grep -rn "hamming_ablak"` (excluding `win_hamming_ablak`) returns exactly 4 hits: the 2 method definitions, 1 live caller (`Form1.cs:411`), and 1 commented caller (`Form1.cs:1571`). `find -name H_FELDOLGOZO.cs` returns only these two files — there is **no** copy in `Turan_core` or `Turan_tester` (note: ROADMAP BUG-14 loosely implies `Turan_core` carries `H_FELDOLGOZO`; it does not — the BUG-06 completeness claim is nonetheless correct and exhaustive).
- **Edit-target uniqueness confirmed.** `grep -c "firdata.Length - 1"` returns `1` in each file (the `fir_filter` loop uses `pcmdata.Length - 1`, a different token), so a literal find/replace cannot be misapplied.
- **No integer-division trap in this method.** The coefficient divisor is `(const_2pi * i) / num_items_in_windowed_frame` = `double * int / int` → evaluated in double arithmetic. This is *not* the `2/24` integer-division defect (that is BUG-04, a different method, correctly out of scope).
- **Backward compatibility confirmed.** Traced `hammingdata` (the single-array result): written only at `Form1.cs:411` (debug `btn_calc_hamming_Click`); every other reference (351–352, 1376–1377, 1571–1574) is commented out. It is never serialized and never reaches DTW. The live feature pipeline windows via `win_fir_hamming` (`Form1.cs:680`, `Creator.cs:158`), not `hamming_ablak`. No on-disk template format changes; existing templates remain valid. The plan's "correctness hardening on an effectively debug-only path, not a recognition-accuracy improvement" framing is accurate and honest.

### Non-blocking observation (flag only — do NOT expand scope)
- The divisor is the fixed constant `num_items_in_windowed_frame = 256`, not `firdata.Length`. If this overload were ever called with a frame whose length ≠ 256, the window shape would be wrong independent of BUG-06. This is the same pre-existing family as the `N` vs `N-1` divisor question the plan already flags; correctly out of scope for a loop-bound fix. Worth a ROADMAP note only if this overload is ever promoted to a live path.

No required changes. Apply the two-line edit (remove `- 1` in each of the two files) exactly as written.
