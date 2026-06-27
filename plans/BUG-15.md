# FIX PLAN — BUG-15 (REVISED): finish the CoMIRVA MFCC port and quarantine it

**Status:** REVISED 2026-06-27 per user decision — **do NOT delete; finish the port and move it to an obviously-unused folder.**
**Severity:** P3 (housekeeping / future HTK-free native MFCC)
**Files:**
- `Felismero_motor_LITE/Felismero_motor/mfcc.cs` — CoMIRVA `MFCC` port (half-finished)
- `Felismero_motor_LITE/Felismero_motor/Matrix.cs` — **empty stub** `class Matrix {}` (must be implemented)
- `Felismero_motor_LITE/Felismero_motor/FFT-converted.cs` — CoMIRVA `FFT` (entirely commented out; must be ported)

---

## 0. Why this changed (user decision)

`mfcc.cs` is **the cleanest MFCC *design* in the repo** (proper mel-warped triangular filterbank + correct orthonormal DCT + Hanning power FFT). It is not inferior to the live native path — it is unfinished and unwired. The user wants it **finished and kept** as the basis for a future **HTK-free, cross-platform native MFCC** (which would also let us retire the fragile `HCopy.exe` dependency behind BUG-13). So instead of deleting it, we:
1. Complete the Java→C# port of all three interdependent files so they form valid, self-contained C#.
2. **Move them to a clearly-unused folder** so it is obvious they are not part of any build.
3. Leave them **out of every `.csproj` `<Compile Include>`** (they already are) so the quarantine is real.

> **Verification caveat:** there is no C# compiler in this environment, so the port is **best-effort, code-review-verified, NOT compiler-verified**. Quarantine (out of build) makes any residual port error harmless — it cannot break the solution. The README must state this.

---

## 1. Destination (obviously-unused folder)

Create repo-root **`reference/unused-native-mfcc/`** and move the three files there:
- `reference/unused-native-mfcc/mfcc.cs`
- `reference/unused-native-mfcc/Matrix.cs`
- `reference/unused-native-mfcc/FFT.cs`  *(rename `FFT-converted.cs` → `FFT.cs`; the class is `FFT`)*
- `reference/unused-native-mfcc/README.md` (new — see §5)

Keep `namespace Felismero_motor` unchanged (namespace is folder-independent in C#); the README documents how to re-include later. Use plain file operations to write the new files; the original three are removed from `Felismero_motor_LITE/Felismero_motor/`. **No `.csproj` edit is required** — re-verify with `grep -niE 'mfcc|Matrix\.cs|FFT-converted' Felismero_motor_LITE/Felismero_motor/Felismero_motor_lite.csproj` (expected: no `<Compile Include>` hits). If any hit exists, remove that include line so the project stays consistent.

---

## 2. `mfcc.cs` — Java→C# fixes (exact checklist, from the original diagnosis)

| Line(s) | Java-ism | Valid C# |
|---|---|---|
| 88, 108 | constructor chaining `this(...);` in body | `: this(...)` after the signature |
| 133,142,148,152,156,160,472,476,526,530 | `throw new IllegalArgumentException(...)` | `throw new ArgumentException(...)` |
| 138 | `Integer.MAX_VALUE` | `int.MaxValue` |
| 146,186,356 | `Math.round`, `Math.pow` | `Math.Round`, `Math.Pow` |
| 386,388 | `Math.cos` | `Math.Cos` |
| 264 | `double[,] matrix = new double[numberFilters][];` | `double[][] matrix = new double[numberFilters][];` (jagged — it is indexed `matrix[i-1] = filter;`) |
| 67–68 | `dctMatrix`/`melFilterBanks` fields commented out but used at 179,180,544,553 | declare `private Matrix melFilterBanks; private Matrix dctMatrix;` |
| 72,183 | `FFT normalizedPowerFFT = new FFT(FFT.FFT_NORMALIZED_POWER,…,FFT.WND_HANNING)` | provided by ported `FFT.cs` (§4) |
| 414,432,448 | `AudioPreProcessor.append(...)` | `AudioPreProcessor.cs` exists in the LITE tree; keep the `process(AudioPreProcessor)` overload only if its `append(double[],int,int)` matches — otherwise mark that ONE overload `// TODO: wire AudioPreProcessor` and rely on the `process(double[])` overload (the one the DCT/mel path actually needs). Do not let an unfinished overload block the core port. |
| 428 | `List<double[]> mfcc = new Vector<double[]>();` | `List<double[]> mfcc = new List<double[]>();` |
| 480,484 | `mfcc[i] = processWindow(...)` into a `double[,]` | build `double[,]` by copying the returned `double[]` row into `mfcc[i, *]` (rectangular assignment is illegal) |
| 547 vs 550 | `Log10` declared, `log10` used | unify to one identifier |

Preserve all the (correct) DSP math: `linToMelFreq`/`melToLinFreq`, `getMelFilterBankBoundaries`, `getMelFilterWeight`, `getDCTMatrix`, `processWindow` pipeline (power FFT → mel → dB → DCT).

---

## 3. `Matrix.cs` — implement the JAMA-style matrix `mfcc.cs` needs

`mfcc.cs` uses exactly these members → implement them (double-backed, row/col):
- ctors: `Matrix(double[][] a, int m, int n)`, `Matrix(double[] vals, int m)` (column-packed), `Matrix(int m, int n)`
- `void set(int i, int j, double v)`
- `Matrix getMatrix(int i0, int i1, int j0, int j1)` (submatrix, inclusive)
- `Matrix times(Matrix b)` (matrix multiply)
- `void timesEquals(double s)` (scalar scale, in place)
- `void logEquals()` (natural log each element, in place)
- `void thrunkAtLowerBoundary(double v)` (clamp each element up to `v` — i.e. `if (x < v) x = v`)
- `double[] getColumnPackedCopy()`

Port faithfully from the public-domain JAMA `Matrix` / CoMIRVA `Matrix` semantics. Keep it minimal — only the members above plus internal storage and `getRowDimension`/`getColumnDimension` helpers.

---

## 4. `FFT.cs` (was `FFT-converted.cs`) — uncomment + port the CoMIRVA FFT

The whole class is commented out. Restore it as valid C#: the windowed FFT (Harris 1978 windows) with the public constants `FFT_NORMALIZED_POWER`, `WND_HANNING` (and siblings), the ctor `FFT(int transformType, int windowSize, int windowFunction)`, and `void transform(double[] re, double[] im)`. Translate Java casing (`Math.*`), `final`, array idioms. The only members `mfcc.cs` calls are the ctor and `transform(buffer, null)`.

---

## 5. `README.md` for the quarantine folder (new)

State plainly: **"UNUSED — not part of any build."** Contents:
- What it is: a completed Java→C# port of Klaus Seyerlehner's CoMIRVA `comirva.audio.util.MFCC` (+ its `Matrix` and `FFT` deps) — a correct, mel-warped, DCT-based MFCC extractor.
- Why it's here, not deleted: kept as the basis for a future **HTK-free, cross-platform native MFCC**, which would let the project retire the external `HCopy.exe`/HTK dependency (see ROADMAP BUG-13).
- Status: **ported by code review, NOT compiler-verified** (no toolchain at port time). Excluded from every `.csproj`; compiling it requires adding the three files back to a project's `<Compile Include>` and wiring `AudioPreProcessor` for the streaming overload.
- Provenance/license note: original CoMIRVA headers retained in-file.

---

## 6. Self-verification (no compiler)

1. Re-read `mfcc.cs` end-to-end: zero Java-isms remain (grep the file for `IllegalArgumentException|Integer\.|Math\.round|Math\.pow|Math\.cos|Vector<|this\(`).
2. Every `Matrix`/`FFT` member `mfcc.cs` calls now exists in the ported `Matrix.cs`/`FFT.cs` (cross-list call-sites vs definitions).
3. `grep` confirms the three files are absent from `Felismero_motor_lite.csproj` `<Compile Include>` (quarantine real).
4. The three originals no longer exist under `Felismero_motor_LITE/Felismero_motor/`; the four files exist under `reference/unused-native-mfcc/`.

## 7. Group / parallelism

Group **C** (renamed `C-finish-native-mfcc`). File-disjoint from Groups A and B (nothing else touches these three files), so it runs fully in parallel. Single high-effort owner (the three files are interdependent — one agent ports all three for consistency). Commit chunk: `feat(mfcc): finish CoMIRVA native MFCC port and quarantine as unused reference`.
