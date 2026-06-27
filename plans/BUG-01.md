# FIX PLAN — BUG-01

**HTK dynamic features (Δ / ΔΔ / ΔΔΔ) are summed away instead of concatenated**
Severity: **P0 (critical, live primary path)** · Risk: **medium** · Breaks on-disk/template format: **No**

---

## 1. Root cause (restated from the code)

`HTK_Interface.ReadMFCC_D_A_T(path, num_of_feature_vectors)` reads an HTK
`MFCC_D_A_T` parameter file. For the README-default configuration each frame is
`sampSize = 240` bytes = **60 floats = 4 streams × 15 static coefficients**
(static MFCC, Δ, ΔΔ, ΔΔΔ). The file's own embedded HTK header dump, quoted in the
source comments, confirms this: `Sample Bytes: 240`, `Num Comps: 60`,
`Sample Kind: MFCC_D_A_T`.

The reader, however, allocates only an `N`-wide row and **adds** every stream into
the same `N` columns:

```
Turan_core/Turan_core/HTK_Interface.cs
 71   vector_array = new double[nSamples, num_of_feature_vectors];     // width = 15, should be 60
 ...
 80   vector_array[current_frame, i]  = ...ReadBytes(4)...   // MFCC  → col i
 87   vector_array[current_frame, i] += ...ReadBytes(4)...   // Δ     ADDED onto col i
 94   vector_array[current_frame, i] += ...ReadBytes(4)...   // ΔΔ    ADDED onto col i
101   vector_array[current_frame, i] += ...ReadBytes(4)...   // ΔΔΔ   ADDED onto col i
```

Result: `out[i] = static[i] + Δ[i] + ΔΔ[i] + ΔΔΔ[i]`. The most discriminative
features (the dynamics) are destroyed by the summation, and DTW runs on a single
smeared 15-dim static vector. Note the byte cursor is correct (it still consumes
all 240 bytes/frame); only the **destination columns** are wrong.

### Why the rest of the pipeline currently "agrees" on 15 (the coupling that must change)

The DTW width is governed by one global, `Engine.mfcc_lpc_vect_num` (a `byte`,
default `15`, never reassigned in `Turan_core`). It is read live by:

- `lpcData.data` allocation and `lpcData.getRowLength()` → `data.Length / Engine.mfcc_lpc_vect_num`
  (`Turan_core/Turan_core/lpcData.cs:60,113`)
- `dtwApp_match` frame extractors `GetReferenceVector` / `GetSignalVector`
  (`:180,193`) and the live `temp1/temp2` used for `cost[0,0]` (`:402,403`)
- `dtwApp_match.EuclideanDistance` loop bound — but **indirectly via the stale
  static** `num_of_vectoritems` (`:84,235`), which is captured **once at type
  initialization** from `Engine.mfcc_lpc_vect_num` and never refreshed.

So the fix is three-sided, and it is bound by the **WIDTH CONTRACT** in
`plans/_grouping.json` (Group A, sharedContracts): *the single DTW width source is
the per-array width via `data.GetLength(1)`, equivalently the **live** value of
`Engine.mfcc_lpc_vect_num` at match time — NOT a compile-time `15` and NOT a
hard-bumped constant `60`.* Concretely:

- **(a)** make the reader **concatenate** the four HTK streams into a `4·N`-wide
  array (`60` for HTK-MFCC `N=15`, `48` for HTK-LPC `N=12`);
- **(b)** set `Engine.mfcc_lpc_vect_num` from the **live per-array width**
  (`win_signal_data.GetLength(1)`) **on BOTH the `htk` and the `turan` branches**,
  before `bestMatch()`, so the process-global is *order-independent* (an HTK call
  followed by a native call can never leave a stale `60` behind — the peer-review
  BLOCKER, see §2b/§4);
- **(c)** make the per-frame distance and the leftover scratch buffers read the
  **live per-array width** (`frame1.Length`, `reference.data.GetLength(1)`,
  `signal.data.GetLength(1)`) instead of the class-load-cached static
  `num_of_vectoritems`, so the distance honours all `4·N` dims and the dead
  `temp3/temp4` buffers can never overrun a narrower array.

### Live-path confirmation (caller trace)

`Turan_tester/Turan_tester/Form1.cs:106` constructs
`new Engine(EngineMode.mfcc, VectorFileFormat.htk)` and calls
`RecognizeAndReturnIndex` → `Turan_core/Turan_core/Engine.cs:123,128` →
`Turan_core/Turan_core/HTK_Interface.ReadMFCC_D_A_T`. Therefore the **`Turan_core`
copy is the single live one.** The `Turan_tester` and `Turan_creator` copies of
`ReadMFCC_D_A_T` are dead today (tester delegates to the core Engine;
`Turan_creator/Creator.cs:219` only calls `CreateMFCC_D_A_T`/HCopy, never the
reader) but MUST receive the identical fix to prevent drift (BUG-14).

---

## 2. Exact changes per file (before / after)

### 2a. `Turan_core/Turan_core/HTK_Interface.cs` (LIVE), and identical edits in
### `Turan_tester/Turan_tester/HTK_Interface.cs` and `Turan_creator/Turan_creator/HTK_Interface.cs`

All three copies have a byte-identical `ReadMFCC_D_A_T` body (only the surrounding
`namespace` differs). Apply the same change to each. Line numbers below are for the
core copy; the tester copy is offset by −8 lines, the creator copy matches core.

**(i) Allocation — core line 71 / tester line 63 / creator line 71**

Before:
```csharp
vector_array = new double[nSamples, num_of_feature_vectors];
```
After:
```csharp
// BUG-01: MFCC_D_A_T = static + Δ + ΔΔ + ΔΔΔ, four streams concatenated.
vector_array = new double[nSamples, 4 * num_of_feature_vectors];
```

**(ii) Per-frame stream writes — core lines 84-103 / tester 76-95 / creator 84-103**

Before (the three `+=` blocks; the first `=` block is already correct but is shown
for context):
```csharp
                    // MFCC - Mel-frequency cepstral coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // D - Delta coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // A - Accelerator coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // T - Third differential coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }
```
After (each stream writes its own column block; `+=` → `=` with a column offset):
```csharp
                    // MFCC - static cepstral coefficients   -> columns [0 .. N-1]
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // D - Delta coefficients                -> columns [N .. 2N-1]
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, num_of_feature_vectors + i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // A - Acceleration coefficients         -> columns [2N .. 3N-1]
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, 2 * num_of_feature_vectors + i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // T - Third-differential coefficients   -> columns [3N .. 4N-1]
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, 3 * num_of_feature_vectors + i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }
```

No other line in `ReadMFCC_D_A_T` changes (header read, `nSamples` loop, `pos`
accounting all stay). The number of `ReadBytes(4)` calls per frame is unchanged
(`4·N`), so the byte cursor stays in sync — only the destination column moves.

### 2b. `Turan_core/Turan_core/Engine.cs` — pin the live width on BOTH branches (MANDATORY)

`RecognizeAndReturnIndex` must set `Engine.mfcc_lpc_vect_num` to the **live
per-array width** of the just-loaded signal — `win_signal_data.GetLength(1)` — at
the top of **both** the `turan` and the `htk` branch, before `bestMatch()`. Driving
the global from `data.GetLength(1)` is exactly the WIDTH CONTRACT source: it equals
`4·N` (60 / 48) on the HTK path and the native template width (15, or 12 for a
future native-LPC mode) on the `turan` path, with no hard-coded literal that could
drift from what the reader actually produced. Setting it on **both** branches makes
the process-global order-independent and closes the peer-review BLOCKER (§4): an
`htk` call (width 60) followed in the same process by a `turan` call can no longer
leave a stale 60 behind.

**(i) `turan` branch — line 88 (`win_signal_data = GetSignalData(...)`)**

Before:
```csharp
                win_signal_data = GetSignalData(signal_vector_filepath);
                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);
```
After:
```csharp
                win_signal_data = GetSignalData(signal_vector_filepath);

                // BUG-01: pin the DTW width to the live per-array width so the
                // process-global is order-independent (15 native-MFCC / 12 native-LPC);
                // never inherits a stale 60 left by a prior HTK call.
                Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);

                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);
```

**(ii) `htk` branch — line 123 (`win_signal_data = HTK_Interface.ReadMFCC_D_A_T(...)`)**

Before:
```csharp
                win_signal_data = HTK_Interface.ReadMFCC_D_A_T(signal_vector_filepath, num_of_feature_vectors);
                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);
```
After:
```csharp
                win_signal_data = HTK_Interface.ReadMFCC_D_A_T(signal_vector_filepath, num_of_feature_vectors);

                // BUG-01: HTK MFCC_D_A_T frames carry 4 concatenated streams
                // (static + Δ + ΔΔ + ΔΔΔ); the per-frame width is now 4*N.
                // Pin the DTW width to the live per-array width (== 4*num_of_feature_vectors
                // == 60 for MFCC / 48 for LPC) before any template is read or matched.
                Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);

                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);
```
(`4*15 = 60` and `4*12 = 48` both fit in `byte`; `15`/`12` trivially do.) Placement
on each branch: immediately after the signal array is obtained and before
`dtwmatch.bestMatch()`, since every global-reading consumer
(`lpcData.getRowLength`, `GetReferenceVector`/`GetSignalVector`, `temp1`/`temp2`)
reads `Engine.mfcc_lpc_vect_num` **live** at match time.

> Both branches are now set explicitly. This is the **required** dual-branch fix
> from the peer review (no longer "optional/defensive"). Note that the
> class-load-time capture `num_of_vectoritems = Engine.mfcc_lpc_vect_num`
> (`dtwApp_match.cs:84`, triggered by `dtwApp_match.Num_of_templates` at
> `Engine.cs:84`, *before* these assignments) still latches the default 15 — which
> is precisely why §2c stops reading that cached static and reads the live
> per-array width instead.

### 2c. `dtwApp_match.cs` (core **and** LITE) — read the live PER-ARRAY width

Per the WIDTH CONTRACT, the two remaining width sites that do **not** already track
the live width must read it from the **per-array** dimension, not from any static.
Using the per-array width (`frame1.Length`, `…data.GetLength(1)`) rather than the
class global has a second, decisive benefit: the edits become **byte-identical in
both copies** — the core copy and `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs`
(which reads `H_FELDOLGOZO.mfcc_lpc_vect_num`, not `Engine.mfcc_lpc_vect_num`). The
grouping requires the LITE copy to be mirrored byte-for-byte; that is only possible
if the replacement text references no namespace-specific static. Both edits below
satisfy that.

**(i) `EuclideanDistance` loop bound — core line 235 / LITE line 237**

`EuclideanDistance` is the only distance used on the live path
(`distanceType = "Euclidean"`, line 102). Its loop bound is the static
`num_of_vectoritems`, captured **once** at type-init from the class global (= default
15, latched *before* §2b runs) and therefore blind to the real per-frame width.

Before:
```csharp
            for (i = 0; i < num_of_vectoritems; i++)  //13
```
After:
```csharp
            for (i = 0; i < frame1.Length; i++)   // BUG-01: live per-array width (60 HTK-MFCC / 48 HTK-LPC / 15-12 native); was stale class-load static num_of_vectoritems
```
`frame1` is the reference frame passed by `frameDistance` (from `GetReferenceVector`
at :487, or `temp1` at :417); its `.Length` already equals the live row width, so
the bound is exact and self-contained. `frame2` has the same length, so the
`frame2[i]` access stays in bounds.

**(ii) `temp3`/`temp4` scratch buffers — core lines 466-467 / LITE lines 466-467**
(folds in the BUG-01 peer-review residual called out in the grouping: *latent OOB on
the narrow-array / mode-switch path*).

These two buffers are **dead** (their values are discarded — line 487 recomputes the
cost via `GetReferenceVector(j)`/`GetSignalVector(i)`), but they are still populated
by a loop that reads `reference.data[j, iter]` / `signal.data[i, iter]` up to their
allocated length. Sized from a *static* (`Engine.mfcc_lpc_vect_num` in core,
`H_FELDOLGOZO.mfcc_lpc_vect_num` in LITE) they overrun whenever that static exceeds
the actual array width — e.g. a 12-wide native-LPC template while the global is 15,
or any residual mode-switch skew. Size them from the **array itself** so they can
never overrun, regardless of the global:

Before (core):
```csharp
                        double[] temp3 = new double[Engine.mfcc_lpc_vect_num];
                        double[] temp4 = new double[Engine.mfcc_lpc_vect_num];
```
Before (LITE):
```csharp
                        double[] temp3 = new double[H_FELDOLGOZO.mfcc_lpc_vect_num];
                        double[] temp4 = new double[H_FELDOLGOZO.mfcc_lpc_vect_num];
```
After (identical in BOTH copies):
```csharp
                        double[] temp3 = new double[reference.data.GetLength(1)]; // BUG-01: per-array width, never overruns
                        double[] temp4 = new double[signal.data.GetLength(1)];    // BUG-01: per-array width, never overruns
```
The trailing copy loops are already bounded by `temp3.Length`/`temp4.Length`, so
they follow the corrected sizes automatically; no further edit there.

**No other `dtwApp_match.cs` line needs editing.** The remaining width-bearing sites
already read the live class global and the §2b pin makes that global equal the
per-array width during the call:
- `GetReferenceVector` (:180) and `GetSignalVector` (:193) → length = pinned width ✓
- `temp1`/`temp2` (:402-403, used for `cost[0,0]`) → length = pinned width ✓
- `AbsDistance`/`ITDDistance` hard-code 13 but are off the live path
  (`distanceType != "Itakura"/"Absolute"`); deriving their order **from the array**
  is **BUG-09**'s job (see §5.1), not this fix.

> **LITE mirror.** Apply (i) and (ii) to
> `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` with the *identical* text
> (same lines: EuclideanDistance 237, temp3/temp4 466-467). The LITE copy does not
> get §2a (it has no `HTK_Interface`) or §2b (it has no `Engine`); only the two
> `dtwApp_match.cs` edits above mirror across. `Felismero_motor_LITE/.../lpcData.cs`
> is **not** in Group A and stays untouched.

### 2d. `Turan_core/Turan_core/lpcData.cs` — **NO edit required** (verify only)

`getRowLength()` returns `data.Length / Engine.mfcc_lpc_vect_num`. With the reader
now producing `[nSamples, 60]` and §2b pinning `Engine.mfcc_lpc_vect_num` to the
live per-array width (`= 60` on the HTK call), this evaluates to
`nSamples·60 / 60 = nSamples` — the correct frame count. Because §2b drives the
global from `data.GetLength(1)`, the denominator and the array width are guaranteed
equal for the duration of the call, so the file stays consistent without
modification. This is why `lpcData.cs` is verify-only in Group A. **It must,
however, be re-verified** (trace in §6) because if §2b is omitted or the global
diverges from the data width, `getRowLength` silently returns an inflated frame
count → out-of-bounds in `lefttorightMatch`.

> **Optional (grouping-sanctioned) robustness.** The grouping permits one edit here:
> change `getRowLength()` to return `data.GetLength(0)` (the true row count) instead
> of `data.Length / Engine.mfcc_lpc_vect_num`. That removes the *last* dependency of
> the frame count on the global and makes it immune to any width skew. It is **not
> required** once §2b pins both branches from `data.GetLength(1)`, and BUG-01 keeps
> `lpcData.cs` verify-only to honour the grouping default; record the `GetLength(0)`
> option for the BUG-14 consolidation pass.

---

## 3. Every duplicated copy that needs the same change

| File | Function | Change | Live? |
|---|---|---|---|
| `Turan_core/Turan_core/HTK_Interface.cs` | `ReadMFCC_D_A_T` | §2a (alloc 4·N + offset writes) | **Yes** |
| `Turan_tester/Turan_tester/HTK_Interface.cs` | `ReadMFCC_D_A_T` | §2a (identical) | No (dead dup) |
| `Turan_creator/Turan_creator/HTK_Interface.cs` | `ReadMFCC_D_A_T` | §2a (identical) | No (dead dup) |
| `Turan_core/Turan_core/Engine.cs` | `RecognizeAndReturnIndex` (**both** branches) | §2b (pin live width, htk + turan) | **Yes** |
| `Turan_core/Turan_core/dtwApp_match.cs` | `EuclideanDistance` + `temp3`/`temp4` | §2c (per-array width) | **Yes** |
| `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` | `EuclideanDistance` + `temp3`/`temp4` | §2c (byte-identical mirror) | Mirror (Group A) |

Apply §2a to **all three** HTK copies even though only the core one is reached
today, so the duplicates do not silently diverge (BUG-14). Apply §2c to **both**
`dtwApp_match.cs` copies (core + LITE) with byte-identical text — this is mandated
by the grouping ("Mirror every dtwApp_match.cs edit byte-identically in the LITE
copy"), which is why §2c is written against the per-array width (no
namespace-specific static).

> **Grouping file-list reconciliation (action for the implementer / Group A lead).**
> `plans/_grouping.json` lists Group A's files as the two `dtwApp_match.cs` copies,
> core `Engine.cs`, and core `lpcData.cs`, and its BUG-01 `commitChunk` lists the
> two `dtwApp_match.cs` copies + `Engine.cs`. **Neither list names the three
> `HTK_Interface.cs` copies**, yet the WIDTH CONTRACT explicitly states "BUG-01
> concatenates HTK MFCC_D_A_T into 4*N columns". The concatenation (§2a) is the
> defining fix of BUG-01 and is **not** deferred or scope-cut anywhere in the
> grouping; the omission is a file-list oversight, not a decision. **The §2a edits
> to all three `HTK_Interface.cs` copies must ship inside the BUG-01 commit**
> (`fix(dtw): unify DTW feature-vector width …`); add those three paths to that
> commit's file set when implementing.

**Not in scope / do NOT change for BUG-01:**
- `Felismero_motor_LITE/.../lpcData.cs`: not in Group A; the LITE module has no
  `HTK_Interface` and never reads HTK files. Untouched. (Relevant only if BUG-14
  later unifies the modules.)
- `lpcData.cs` (core): verify-only, §2d.

---

## 4. Backward compatibility / data-format impact

**No on-disk, template, or serialization format changes; no re-enrollment needed.**

- The HTK `.mfc` files are produced by the external `HCopy.exe` and are read raw at
  match time for **both** the test signal and every reference template
  (`Engine.cs:123` and `:128`). The fix changes only the **in-memory
  interpretation**, applied symmetrically to signal and references, so existing
  `.mfc` template files remain valid and need no regeneration.
- The native `turan` path (BinaryFormatter `Creator.SerializeArray` ↔
  `Engine.DeSerializeArray`) is **not touched**; its templates stay 15-/12-wide.
- Recognition **results will change** (that is the intended P0 correction): scores
  and the argmin template can differ once the dynamics are actually used. This is a
  behavior change, not a data-format break.

**The one compatibility caveat — a shared mutable global (now fully contained).**
§2b makes `Engine.mfcc_lpc_vect_num` runtime-mutated. It is a process-wide `static`
shared by every `Engine` instance, `lpcData`, and `dtwApp_match`. The peer review
correctly identified that mutating it on the `htk` branch **only** creates a
*reachable* crash: `new Engine(mfcc, htk).RecognizeAndReturnIndex(...)` leaves the
global at 60, after which `new Engine(mfcc, turan).RecognizeAndReturnIndex(...)`
reads 15-wide native templates with the global still at 60 →
`getRowLength = nframes·15/60 = nframes/4` **and** `temp1 = new double[60]` indexes
`reference.data[0,15]` → `IndexOutOfRangeException`. This is a public-library hazard,
not merely a desktop-UI one.

**Resolution (mandatory, per §2b and the grouping):** the width is pinned from the
**live per-array width** at the top of **both** branches
(`Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);`), so every
`RecognizeAndReturnIndex` call re-establishes the correct width for its own data
before `bestMatch()` regardless of what ran before it. The interleaving ordering is
eliminated; the global is order-independent. (This was the peer-review BLOCKER; it
is now closed, not downgraded to "optional".) Per-array reads in §2c
(`frame1.Length`, `…GetLength(1)`) add a second, static-free layer of safety on the
distance and scratch buffers.

A residual *thread-safety* note (non-blocking, out of BUG-01 scope): the single
process-global is safe only for non-concurrent calls of the same `(mode, format)`;
`Form1` recognizes on a worker thread. Record a comment at the field declaration
(`Engine.cs:29`) when implementing; true concurrency hardening belongs to BUG-14.

---

## 5. Shared contracts other bug-fixes depend on

1. **The WIDTH CONTRACT: the single DTW width source is the per-array width via
   `data.GetLength(1)`, equivalently the live `Engine.mfcc_lpc_vect_num` at match
   time — NOT a compile-time 15 and NOT a hard-bumped 60.** BUG-01 promotes the
   global from "compile-time constant 15" to "runtime per-call width pinned from
   `data.GetLength(1)` on both branches", and moves the distance/scratch sites
   (`EuclideanDistance`, `temp3/4`) to read the per-array width directly. **Any fix
   that reasons about feature width must read it from the array (or the live
   global), never assume 15 and never assume a fixed 60:**
   - **BUG-09** (Itakura `magic13`) must **derive its order from the array itself
     (`data.GetLength(1)` / the passed `frame.Length`), NOT from the static
     `Engine.mfcc_lpc_vect_num`.** The grouping explicitly rejects the false premise
     that "BUG-01 sets the static to 60": BUG-01 does pin the *live* global to the
     per-array width during a call, but the Itakura distortion needs the **LPC order
     of the actual frame it is handed**, which it must take from that frame, not
     from a process-global that may carry a concatenated (48/60) width. BUG-09's fix
     therefore distinguishes "this frame's order" (from the array) from "total DTW
     width" — and reads the former from the array.
   - **BUG-08** (hard-coded array caps in `dtwApp_match.costRecord`) sizing should
     likewise key off actual frame/template counts, not magic literals — scoped to
     `dtwApp_match.cs` `costRecord` only (the `hasonlit` cap is dropped: BUG-16
     deletes `hasonlit`).
2. **The summed-vs-concatenated decision is itself a contract** between the reader
   and `EuclideanDistance`: both must agree the row is `4·N` and that all `4·N`
   dims participate in the distance. §2a and §2c are a matched pair — neither is
   safe alone.
3. **The BinaryFormatter contract** (`Creator.SerializeArray` ↔
   `Engine.DeSerializeArray`, BUG-12) is independent of BUG-01 but shares the same
   width contract: whatever replacement format BUG-12 introduces must preserve the
   per-frame width so the deserialized `double[,]` still matches the width Engine
   pins for the `turan` branch.
4. **Module duplication (BUG-14):** the three HTK copies (and the LITE DTW) are the
   classic drift surface; §3 keeps the HTK copies in lock-step, which is the
   pre-condition any later consolidation will assume.

---

## 6. Self-verification WITHOUT a compiler (what to read / trace)

1. **Ground-truth dimensionality from HTK itself.** The source comments embed the
   actual `HCopy` header dump: `Sample Bytes: 240`, `Num Comps: 60`,
   `Sample Kind: MFCC_D_A_T` for `N=15`. `240 bytes = 60 floats = 4 × 15`. After
   §2a the array width is `4·N = 60 = Num Comps`. This proves concatenation (not
   summation) is the correct reconstruction — the file's own metadata says the
   frame is 60-dimensional.
2. **Byte-cursor accounting.** Count `ReadBytes(4)` calls per frame: still
   `4·N = 60` reads = 240 bytes = `sampSize`. The loop runs `nSamples` times;
   `12 (header) + nSamples·240` must equal the file length. The fix changes only
   the destination column index, never the read count, so there is no stream
   desync. Confirm each block's index range is disjoint and covers `[0,4N)`:
   `i`, `N+i`, `2N+i`, `3N+i` for `i∈[0,N)`.
3. **Frame-count trace through `lpcData.getRowLength`.** `data.Length = nSamples·60`;
   `Engine.mfcc_lpc_vect_num = 60` (from §2b); quotient `= nSamples`. Then in
   `lefttorightMatch`, `I = signal.getRowLength()` and `J = reference.getRowLength()`
   are true frame counts (not 4× inflated) → no index overrun.
4. **Distance-width trace.** With §2b pinning the global to `GetLength(1)` (= 60 on
   the HTK call): `GetSignalVector`/`GetReferenceVector` return length-60 arrays;
   `EuclideanDistance` (after §2c) iterates `i < frame1.Length` where `frame1` is
   that length-60 array; `temp1/temp2` for `cost[0,0]` are length 60; `temp3/temp4`
   are sized to `reference.data.GetLength(1)`/`signal.data.GetLength(1)` = 60. All
   consistent → all 60 dims (static + Δ + ΔΔ + ΔΔΔ) contribute, none out of bounds.
   Repeat the trace for a hypothetical 12-wide native-LPC array: `frame1.Length` and
   `GetLength(1)` both yield 12, so no site overruns even if the global were stale —
   the per-array reads are self-protecting.
5. **Stale-static check.** Confirm `num_of_vectoritems` has exactly one remaining
   read (the `EuclideanDistance` loop) and that §2c removes it; grep
   `num_of_vectoritems` in **both** `dtwApp_match.cs` copies should show only the
   declaration/commented lines afterward. This proves the type-init capture order
   (`dtwApp_match.Num_of_templates` at `Engine.cs:84` runs before §2b sets the
   width) no longer matters on either copy.
6. **Three-copy / mirror diff.** After editing, diff the `ReadMFCC_D_A_T` bodies of
   the core, tester, and creator copies; they must be identical except for the
   `namespace`. Separately, diff the §2c edited regions of the core vs LITE
   `dtwApp_match.cs`; the `EuclideanDistance` loop line and the `temp3/temp4`
   allocation lines must be **byte-identical** (no `Engine.`/`H_FELDOLGOZO.`
   reference remains in those lines — that is the whole reason §2c uses
   `frame1.Length` and `…data.GetLength(1)`).
7. **Optional unit assertion (if any test harness is later available — no full
   build needed for the trace above).** Read a known `.mfc` and assert
   `arr.GetLength(1) == 4 * N`, and that at least one frame has
   `arr[f, k] != arr[f, k] + arr[f, N+k] + arr[f, 2N+k] + arr[f, 3N+k]` for some
   `k` (i.e. the columns are genuinely distinct, not the old collapsed sum).

---

## Summary of edits
- `Turan_core/Turan_core/HTK_Interface.cs` — §2a (live; alloc 4·N + offset writes)
- `Turan_tester/Turan_tester/HTK_Interface.cs` — §2a (dead dup, keep byte-identical)
- `Turan_creator/Turan_creator/HTK_Interface.cs` — §2a (dead dup, keep byte-identical)
- `Turan_core/Turan_core/Engine.cs` — §2b (pin live width on **both** branches, htk + turan)
- `Turan_core/Turan_core/dtwApp_match.cs` — §2c (EuclideanDistance → `frame1.Length`; `temp3/temp4` → `…data.GetLength(1)`)
- `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` — §2c (byte-identical mirror of the two edits above)
- `Turan_core/Turan_core/lpcData.cs` — verify only, no edit (§2d)

> Note for the implementer: the three `HTK_Interface.cs` edits are required by the
> WIDTH CONTRACT but are absent from `_grouping.json`'s Group-A file list / BUG-01
> commitChunk — see the reconciliation callout in §3 and add them to the BUG-01
> commit's file set.

---

## Peer review

**Reviewer verdict: NOT approved (1 blocker).** The root-cause diagnosis, the
concatenation fix, the width-pin, the stale-static removal, and the 3-copy
completeness are all correct and were re-verified against the live source. One
change introduces a *new*, reachable crash that the plan downgrades to "optional";
that must become mandatory before implementation.

### Verified correct (no change needed)
- **Root cause.** `HTK_Interface.cs:80/87/94/101` are exactly `=`,`+=`,`+=`,`+=`
  into the same 15 columns — the four streams are summed, not concatenated. Header
  comment confirms `Sample Bytes: 240 / Num Comps: 60`. Line numbers in §1 are exact.
- **§2a concatenation.** Offsets `i`, `N+i`, `2N+i`, `3N+i` for `i∈[0,N)` are
  disjoint and cover `[0,4N)`; `ReadBytes(4)` count per frame is unchanged (`4N`),
  so the byte cursor stays in sync. Correct.
- **§2b is *required*, not cosmetic.** Confirmed: without it, `lpcData.getRowLength`
  = `data.Length / Engine.mfcc_lpc_vect_num` = `nSamples·60 / 15` = `4·nSamples`,
  which over-allocates `path/cost` and drives `reference.data[j,…]` out of bounds in
  `lefttorightMatch`. The pin is load-bearing.
- **§2c is *required*.** `num_of_vectoritems` is captured once at the line-84 static
  trigger (=15) and is the loop bound of the only live distance (`EuclideanDistance`);
  `grep` confirms line 235 is its sole non-comment use. Without §2c the distance
  silently ignores the 45 dynamic dims even after they are stored. Matched pair with §2a.
- **3-copy completeness.** `diff` confirms the `ReadMFCC_D_A_T` bodies of the core,
  tester, and creator copies are byte-identical (only `namespace`/`CreateMFCC_D_A_T`
  differ). Claimed offsets check out: core alloc L71, tester L63 (−8), creator L71.
- **Live path.** `Turan_tester/Form1.cs:106` builds `Turan_core.Engine` (csproj
  references `Turan_core.dll`); `Engine.cs` exists only in `Turan_core`. So the core
  copy is the single live one, as claimed.
- **§2d (lpcData no-edit).** Correct: on the HTK path `data` is replaced by the
  `lpcData(double[,])` ctor, and `getRowLength` reads `Engine.mfcc_lpc_vect_num`
  *live*, so the file tracks the pinned width without modification.
- **"never reassigned" premise.** `grep "mfcc_lpc_vect_num *="` returns only the
  line-29 default; the lpcData `Num_of_lpc_vectors` setter writes a *different*
  static. So after §2b, the Engine global is genuinely write-once-default-then-§2b —
  the statefulness analysis below is airtight.

### BLOCKER — the mutable global must be set on *both* branches, not "optionally"
§2b promotes `Engine.mfcc_lpc_vect_num` from an immutable `15` to a process-global
that an HTK call leaves at `60`. The `turan` branch still relies on the implicit
default `15`. Because the field is now stateful and the default only holds until the
first HTK call, this is a **reachable crash via the public library API**, not a
theoretical one:

> `new Engine(mfcc, htk).RecognizeAndReturnIndex(...)` (sets global = 60), then in
> the same process `new Engine(mfcc, turan).RecognizeAndReturnIndex(...)`:
> native templates are 15-wide, so `getRowLength` = `nframes·15 / 60` = `nframes/4`
> (truncated frame count) **and** `temp1 = new double[60]` reads
> `reference.data[0,15]` → `IndexOutOfRangeException`.

Pre-fix the global was immutably 15, so this path could not break. The fix *creates*
the hazard, which is exactly the "introduces new bugs" criterion. The plan correctly
diagnoses this in §4/§5.1 but downgrades the mitigation to "optional / defensive,"
justified by "the desktop app fixes one (mode, format) per session." That holds for
*today's tester UI* but not for `Turan_core` as a reusable library.

**Required change:** set the DTW width *explicitly at the top of both branches*,
making the global order-independent — not "add the mirror line if cheap". **Source it
from the live per-array width on each branch** (this superseded the early draft that
used a literal `= 15` on the `turan` branch — see §2b, which is authoritative):
- `turan` branch: `Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);`
  **mandatory**, placed before the `dtwApp_match` ctor / `bestMatch`. (Do **not** use a
  literal `15`: native templates can be 12-wide LPC, and the `turan` branch does not
  switch on `EngineMode`, so a literal 15 would re-introduce the exact OOB this fix
  kills — `temp1 = new double[15]` over a 12-wide array.)
- `htk` branch: `Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);`
  (== `4 * num_of_feature_vectors` == 60/48).

Two lines total; eliminates every interleaving ordering. Update §4 (drop "optional")
and the Summary-of-edits line for `Engine.cs` (drop "optional defensive turan line"
→ "set width on both branches") to match.

### Non-blocking notes (do not gate the verdict; record for the implementer)
1. **LPC-mode width = 48 is an assumption.** `4*num_of_feature_vectors` for
   `EngineMode.lpc` (=48) presumes the HTK LPC config also emits a 4-stream
   `_D_A_T` frame. The *existing* code already reads 4 streams unconditionally, so
   this is not a regression either way; but the LPC path is not live today, so the
   48 figure is unverified against a real LPC `.mfc`. Verify if/when LPC HTK is
   enabled.
2. **Thread-safety.** `Form1` recognizes on a worker thread (`label1.Invoke`). A
   single mutable process-global is safe only for non-concurrent calls of the same
   (mode, format). Out of scope for BUG-01, but worth a comment at the field
   declaration when implementing the dual-branch set above.
3. **Cosmetic, optional:** §2c could equivalently use `frame1.Length` (already equal
   to the pinned width); the plan's choice of `Engine.mfcc_lpc_vect_num` is fine and
   consistent with the rest of the file. No action needed.

---

## Revision 2026-06-27

This revision makes BUG-01 fully resolve the peer-review BLOCKER and conform to
`plans/_grouping.json` (Group A; internal order *width-source → 01 → 09 → 10 → 08 →
11*; the WIDTH CONTRACT; the LITE-mirror requirement; the BUG-09 premise rejection).
All target locations were re-read in the live source to confirm line numbers and
correctness. Each change below names the blocking issue or grouping decision it
closes.

**1. Peer-review BLOCKER closed — the mutable global is now set on BOTH branches
(§1, §2b, §4, §3 table, Summary).**
Previously §2b pinned `Engine.mfcc_lpc_vect_num` on the `htk` branch only and
called the `turan` mirror "optional/defensive". That left the documented reachable
`IndexOutOfRangeException` when an `htk` call (global→60) is followed by a `turan`
call (15-wide templates) in one process. §2b now sets the width at the top of
**both** branches, *before* `bestMatch()`, making the global order-independent. The
"optional" language is removed from §4 and the Summary. → **Closes the sole peer-
review blocker.**

**2. Width is now sourced from `data.GetLength(1)`, not a hard literal (§1, §2b,
§5.1) — conforms to the WIDTH CONTRACT.** The grouping's contract states the single
width source is the per-array width via `data.GetLength(1)` (equivalently the live
global), and "NOT a hard-bumped 60". §2b therefore pins
`Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1)` on each branch
(yielding 4·N=60/48 on htk and the native width 15/12 on turan automatically),
instead of the literal `(byte)(4 * num_of_feature_vectors)`. → **Closes the contract
mismatch with `_grouping.json` sharedContracts[0].**

**3. §2c rewritten to read the PER-ARRAY width, and the LITE copy is now an in-scope
byte-identical mirror (§2c, §3 table, Summary, §6.5-6.6).** Verified against source:
in *both* `dtwApp_match.cs` copies the only stale-width read is the
`EuclideanDistance` loop bound `num_of_vectoritems` (core :235 / LITE :237). §2c now
changes it to `frame1.Length`. This (a) honours the contract's per-array source, and
(b) is **byte-identical** across core (`Engine.`-based) and LITE
(`H_FELDOLGOZO.`-based) copies — the only way to satisfy the grouping's "mirror every
dtwApp_match.cs edit byte-identically in the LITE copy". The earlier plan wrongly
excluded the LITE `dtwApp_match.cs`; it is now listed as a Group-A target. → **Closes
the LITE-mirror requirement and the EuclideanDistance stale-static defect on both
copies.**

**4. `temp3`/`temp4` fix folded in (§2c) — the grouping's BUG-01 peer-review
residual.** The grouping mandates "fix temp3/temp4 at dtwApp_match.cs:466/467 (latent
OOB on 12-wide path)". Re-reading the source: these buffers are **dead** (overwritten
at :487) but still loop-read `reference.data[j,…]`/`signal.data[i,…]` up to a
*static*-derived length, which overruns a narrower array (e.g. 12-wide native-LPC
while the global is 15, or any mode-switch skew). §2c sizes them from
`reference.data.GetLength(1)` / `signal.data.GetLength(1)` so they can never overrun,
in both copies (byte-identical). The earlier plan's "leave them, cleanup is a
separate pass" stance is reversed to match the grouping. → **Closes the folded-in
latent-OOB residual.**

**5. BUG-09 dependency premise corrected (§5.1) — the grouping rejects the old
claim.** The earlier §5.1 said BUG-09 must derive its Itakura order *from
`Engine.mfcc_lpc_vect_num`*. The grouping explicitly rejects this ("reject BUG-09's
false premise that 01 sets the static to 60; derive Itakura order from the array, not
a static"). §5.1 now directs BUG-09 to read the order from the **array**
(`data.GetLength(1)` / the passed `frame.Length`), distinguishing "this frame's
order" from "total DTW width". BUG-08's scope note is also aligned (costRecord only;
`hasonlit` cap dropped because BUG-16 deletes `hasonlit`). → **Closes the contract
conflict with the grouping's BUG-09/BUG-08 scope decisions.**

**6. HTK_Interface file-list reconciliation flagged (§3, Summary).** Re-reading
`_grouping.json`: the §2a concatenation fix lands in `HTK_Interface.cs` (×3), but
that file appears in **no** group file-list or commitChunk, while the WIDTH CONTRACT
explicitly says "BUG-01 concatenates HTK MFCC_D_A_T into 4*N columns". Since no
deferral/scope-cut covers it, §2a is retained (all three copies) and an explicit
reconciliation callout instructs the implementer to add the three `HTK_Interface.cs`
paths to the BUG-01 commit. The §2a body itself was re-verified correct against all
three copies (core alloc L71 & writes 78-103; tester alloc L63 & writes 70-95;
creator alloc L71 & writes 78-103 — confirmed identical). → **Resolves the
plan-vs-grouping file-set gap without dropping the defining fix.**

**7. Thread-safety note relocated into §4** (was peer-review non-blocking note 2) and
the LPC-width=48 assumption is retained as a non-blocking caveat. No code-behaviour
change; documentation only.

**Net edit set after this revision:** `HTK_Interface.cs` ×3 (§2a), core `Engine.cs`
(§2b, both branches), core + LITE `dtwApp_match.cs` (§2c, byte-identical), core
`lpcData.cs` (verify-only, §2d). No on-disk/template-format change; consistent with
BUG-12 (S1) which later adds the versioned TRMS format.

---

## Re-review 2026-06-27

**Verdict: APPROVED — safe to implement.** Every target location was re-read in the
live source (not taken from the plan's own claims). All previously-blocking issues
are genuinely resolved; no new defect, off-by-one, integer-division, width, or
duplicated-copy omission was introduced; the plan is consistent with the WIDTH
CONTRACT and stays inside Group A's scope (plus the three `HTK_Interface.cs` copies,
which are file-disjoint from Groups B/C).

### (1) Previously-blocking issues — all resolved (verified against source)
- **Peer-review BLOCKER (stateful global → mode-switch `IndexOutOfRangeException`).**
  Closed. §2b sets `Engine.mfcc_lpc_vect_num` at the top of **both** branches
  (turan = `Engine.cs:88`, htk = `Engine.cs:123`) before the `dtwApp_match` ctor and
  `bestMatch()`. An htk(60)→turan(15) sequence now re-pins to 15, so `temp1 = new
  double[15]` reads `reference.data[0,0..14]` in-bounds and `getRowLength =
  nframes·15/15 = nframes`. Re-verified the crash precondition is eliminated.
- **WIDTH CONTRACT source (`data.GetLength(1)`, not a hard literal).** Conforms.
  §2b uses `(byte)win_signal_data.GetLength(1)` → 60/48 on htk, 15/12 on turan, with
  no hard-coded 60. `60`/`48`/`15`/`12` all fit in `byte` (≤255).
- **LITE byte-identical mirror.** Confirmed feasible: §2c's replacement text
  (`frame1.Length`; `reference.data.GetLength(1)` / `signal.data.GetLength(1)`)
  contains no namespace-specific static, so the edited lines are byte-identical in
  core (235/466-467) and LITE (237/466-467). `reference.data`/`signal.data` are
  accessible in both copies (already dereferenced at the existing copy loops).
- **temp3/temp4 latent OOB residual.** Folded into §2c; sized from the per-array
  width, and the trailing copy loops are bounded by `temp3.Length`/`temp4.Length`, so
  they can never overrun regardless of the global.
- **BUG-09 false-premise / BUG-08 scope.** §5.1 now directs BUG-09 to derive the
  Itakura order from the array, and drops BUG-08's `hasonlit` cap (BUG-16 deletes
  `hasonlit`). Matches `_grouping.json` sharedContracts + DEAD-METHOD OWNERSHIP.
- **HTK_Interface file-list gap.** Correctly flagged: the concatenation is BUG-01's
  defining fix yet `_grouping.json`'s Group-A file list / BUG-01 commitChunk omit
  `HTK_Interface.cs`. The plan retains §2a on all three copies and instructs adding
  the three paths to the BUG-01 commit — the right resolution (the contract itself
  mandates "concatenates HTK MFCC_D_A_T into 4*N columns").

### (2) No new defect introduced (independently checked)
- Concatenation offsets `i, N+i, 2N+i, 3N+i` (i∈[0,N)) are disjoint and exactly
  cover `[0,4N)`; the number of `ReadBytes(4)` calls per frame is unchanged (4·N),
  so the byte cursor stays in sync — only the destination column moves.
- `EuclideanDistance` bound `frame1.Length`: `frame1`/`frame2` are always an
  equal-length pair (both from `GetReferenceVector`/`GetSignalVector` at :487, or both
  `temp1`/`temp2` at :417), so `frame2[i]` stays in bounds. Confirmed there are no
  other `frameDistance` callers.
- `num_of_vectoritems` has exactly one non-comment use (235/237); §2c removes it, so
  the class-load static-init capture order is rendered irrelevant on both copies.
- All live width-read sites are reconciled: `GetReferenceVector`/`GetSignalVector`
  (180/193) and `temp1`/`temp2` (402/403) read the live global (pinned by §2b);
  `EuclideanDistance` and `temp3/temp4` read the per-array width (§2c);
  `lpcData.getRowLength` reads the live global. `GetVector` (203) is dead (no caller)
  and correctly left out of scope, despite its own latent `GetUpperBound(1)`
  off-by-one.
- File-disjointness preserved: the three `HTK_Interface.cs` copies and Group A's
  files appear in no other parallel group.

### (3) Remaining issues — non-blocking, do not gate approval
- **Doc inconsistency (cosmetic).** The older "Peer review → Required change" text
  (the `turan` branch: `Engine.mfcc_lpc_vect_num = 15;` literal) is superseded by the
  authoritative §2b / "Revision" item 2, which correctly uses
  `(byte)win_signal_data.GetLength(1)`. An implementer must follow §2b (the change
  spec), not the stale literal in the Peer-review narrative. Worth a one-line note
  but no code impact.
- **LPC width = 48 unverified** against a real LPC `.mfc` (LPC path not live today);
  already recorded as a non-blocking caveat.
- **Thread-safety** of the single mutable process-global is out of BUG-01 scope
  (BUG-14); a field-declaration comment is suggested when implementing.
