# FIX PLAN — BUG-14: Duplicated DSP/DTW code across modules (drift risk)

**Severity:** P3 (hygiene / tech-debt)
**Authoritative source:** `ROADMAP.md` → BUG-14
**Companion analysis:** `reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md`
**Status of this plan:** planning only — no source edited yet.

---

## 1. Root cause (restated from the code)

The DSP/DTW source files were **physically copied** between independent VS projects instead of shared
via a referenced library. Verified by `md5sum` + `diff`: every listed pair/triple has a **different
hash, different line count, and already-drifted content** — the copies are NOT identical today.

Concrete duplication surface (confirmed by reading every file + `*.csproj` `<Compile Include>` lists):

| Logical file | Copies (namespace) | Compiled into |
|---|---|---|
| `HTK_Interface.cs` | `Turan_core/Turan_core` (`Turan_core`), `Turan_tester/Turan_tester` (`Turan_tester`), `Turan_creator/Turan_creator` (`Turan_creator`) | core.dll, tester.exe, creator.dll |
| `dtwApp_match.cs` | `Turan_core/Turan_core` (`Turan_core`), `Felismero_motor_LITE/Felismero_motor` (`Felismero_motor`) | core.dll, LITE.exe |
| `H_FELDOLGOZO.cs` | `Turan_creator/Turan_creator` (`Turan_creator`), `Felismero_motor_LITE/Felismero_motor` (`Felismero_motor`) | creator.dll, LITE.exe |
| `lpcData.cs` | `Turan_core/Turan_core` (`Turan_core`), `Felismero_motor_LITE/Felismero_motor` (`Felismero_motor`) | core.dll, LITE.exe |
| `CV_FELDOLGOZO.cs` | `Turan_creator/Turan_creator` (`Turan_creator`), `Felismero_motor_LITE/Felismero_motor` (`Felismero_motor`) | creator.dll, LITE.exe |

There are effectively **two worlds**:
- **World A (modular pipeline):** `Turan_core` + `Turan_creator` + `Turan_tester`, wired by **DLL references**
  (`Turan_tester.csproj` references `Turan_core.dll`, `Turan_creator.dll`, `Turan_SC.dll`). HTK_Interface is
  nonetheless copied into all three.
- **World B (self-contained fork):** `Felismero_motor_LITE` — an all-in-one GUI app (`namespace Felismero_motor`)
  that re-implements the entire DSP/DTW stack and references neither core nor creator.

### Why a physical "extract one shared library" (the ROADMAP's ideal fix) is NOT done in this pass

The copies have diverged **semantically**, not just cosmetically. Merging them would silently change behavior
and cannot be compiler-verified in this environment (no dotnet/mono/msbuild). Evidence from `diff`:

1. **The shared width constant has different VALUES and different owners — intentional, not drift:**
   - `Turan_core/Engine.cs:29` → `public static byte mfcc_lpc_vect_num = 15;` (MFCC mode, indices 0–14)
   - `Turan_creator/.../H_FELDOLGOZO.cs:36` and `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:36` →
     `public static byte mfcc_lpc_vect_num = 12;` (LPC mode, indices 0–11)
   - `Turan_core/dtwApp_match.cs:84` reads `Engine.mfcc_lpc_vect_num`; the LITE copy at `:84` reads
     `H_FELDOLGOZO.mfcc_lpc_vect_num`. Same split for `lpcData.cs:47` and `CV_FELDOLGOZO.cs:47`.
   Unifying the symbol would force a single value (12 vs 15) → corrupts vector width in one world.

2. **Real body differences, not just namespace:**
   - `H_FELDOLGOZO.cs`: Turan_creator has `create_window_no_overlap(...)` and writes `window_array[frame, …]`;
     LITE writes `window_array[frame-1, …]` and instead carries GUI-only debug helpers `Show2dArray(...)` that
     depend on `DataView` (a WinForms control absent from the core/creator class libs).
   - `lpcData.cs`: `num_of_frames = 500` (core) vs `5` (LITE); core nulls buffers in `catch (IOException)`,
     LITE does not and carries a `// NULLREF TO DATA!!!` note.
   - `dtwApp_match.cs`: core has a partial `magic13` refactor (relevant to BUG-09); LITE uses bare `13`/`12`
     literals.
   - `HTK_Interface.cs`: `CreateMFCC_D_A_T` has **three different signatures** across the three copies
     (`(wav,config,script)` vs `(config,script,app_path)` vs `(config,script_path,app_path)`) and different
     error handling (`throw` vs `return` vs none).

**Conclusion / chosen approach:** Perform the safe, **behavior-preserving** half of BUG-14 now — make the
duplication *managed and tractable* so the BUG-01..06/08/09 fixes provably reach every copy — and **defer**
the full physical extraction to a follow-up that requires a build environment (blueprint in §4).

---

## 2. Exact change per file (before / after)

All in-scope edits are **comment-only** (zero behavior, compiler-safe) plus **two new non-compiled files**.
The comment header is inserted immediately after the existing `namespace … {` opening of each duplicated copy
so a developer editing the file sees the sync obligation at the edit site.

### 2a. Per-file "KEEP IN SYNC" header (comment-only)

For each duplicated `.cs`, insert a block right after its `namespace … {` brace. Line numbers below are the
current `namespace` declaration lines.

**`Turan_core/Turan_core/dtwApp_match.cs`** (namespace at line 31)
Before:
```csharp
namespace Turan_core
{
    class dtwApp_match
```
After:
```csharp
namespace Turan_core
{
    // --- DUPLICATED FILE -- see reports/DUPLICATES.md (BUG-14) -------------------
    // Sibling copy: Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs (namespace Felismero_motor)
    // Width constant here = Engine.mfcc_lpc_vect_num (=15). Sibling uses H_FELDOLGOZO.mfcc_lpc_vect_num (=12).
    // Any DSP/DTW fix (BUG-08, BUG-09) MUST be PORTED (not text-copied) to the sibling. Run tools/check_duplicates.sh.
    // ----------------------------------------------------------------------------
    class dtwApp_match
```

Apply the **same pattern** (adjusting the sibling list + note) to every copy below:

| File | namespace line | Sibling(s) named in header + note |
|---|---|---|
| `Turan_core/Turan_core/HTK_Interface.cs` | 7 | tester + creator copies; note: 3 different `CreateMFCC_D_A_T` signatures — do NOT blindly unify |
| `Turan_tester/Turan_tester/HTK_Interface.cs` | 7 | core + creator copies |
| `Turan_creator/Turan_creator/HTK_Interface.cs` | 7 | core + tester copies |
| `Turan_core/Turan_core/dtwApp_match.cs` | 31 | LITE copy (constant note: `Engine.` =15 vs `H_FELDOLGOZO.` =12) |
| `Felismero_motor_LITE/Felismero_motor/dtwApp_match.cs` | 31 | core copy (constant note as above) |
| `Turan_creator/Turan_creator/H_FELDOLGOZO.cs` | 31 | LITE copy; body differs (`create_window_no_overlap`, `window_array[frame]` vs `[frame-1]`) |
| `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs` | 31 | creator copy; GUI-only `Show2dArray`/`DataView` live here only |
| `Turan_core/Turan_core/lpcData.cs` | 32 | LITE copy; note `num_of_frames` 500 vs 5, error-handling differs |
| `Felismero_motor_LITE/Felismero_motor/lpcData.cs` | 32 | core copy; carries `// NULLREF TO DATA!!!` |
| `Turan_creator/Turan_creator/CV_FELDOLGOZO.cs` | 33 | LITE copy (only namespace differs today) |
| `Felismero_motor_LITE/Felismero_motor/CV_FELDOLGOZO.cs` | 33 | creator copy (only namespace differs today) |

> Use plain ASCII (`---`) in the comment, not box-drawing chars, and keep the existing file encoding/BOM
> (`dtwApp_match.cs` starts with a UTF-8 BOM). The comment must not change any code token.

### 2b. New file — `reports/DUPLICATES.md` (manifest)

Single source of truth: the §1/§3 copy map, the **constant-source contract** (`Engine.` vs `H_FELDOLGOZO.`,
values 15 vs 12, marked **INTENTIONAL — do NOT unify**), the known body differences per pair, and the rule
**"port the fix, don't text-copy"** (because surrounding code differs, mechanical copy-paste is itself unsafe).

### 2c. New file — `tools/check_duplicates.sh` (compiler-free parity guard)

A bash/grep/diff script runnable in THIS environment. It does **not** assert "files are identical" (they
aren't, and some divergence is intentional). It performs:
- **Namespace-normalized structural diff** per pair to surface *new, undocumented* divergence (informational).
- **Per-fix signature assertions** (the load-bearing part) — one check per correctness bug, proving a fix
  reached every copy. Examples (exit non-zero on violation):
  - BUG-04: `Math.Sqrt(2.0/24.0)` present in BOTH `H_FELDOLGOZO.cs` copies AND `2/24` present in ZERO.
  - BUG-06: `firdata.Length - 1` present in ZERO `H_FELDOLGOZO.cs` copy (`hamming_ablak`).
  - BUG-09: bare `new double[13]` / `< 13` absent from `dtwApp_match.cs` copies once order is derived.
  - BUG-01: the 60-dim allocation expression present in every HTK reader on the MFCC_D_A_T path.
  - BUG-02/05: pre-emphasis reads `win_pcmdata`/`pcmdata` (the input), not `tmparray`, in both copies.
  Each assertion is tagged with its BUG id and **skipped until that bug lands** (a registry; each bug's PR
  enables its own check). With no fix applied yet, the script exits 0 and just prints current divergences.

---

## 3. Every duplicated copy that needs the same change

- **HTK_Interface.cs:** 3 copies — `Turan_core/Turan_core/`, `Turan_tester/Turan_tester/`, `Turan_creator/Turan_creator/`.
- **dtwApp_match.cs:** 2 copies — `Turan_core/Turan_core/`, `Felismero_motor_LITE/Felismero_motor/`.
- **H_FELDOLGOZO.cs:** 2 copies — `Turan_creator/Turan_creator/`, `Felismero_motor_LITE/Felismero_motor/`.
- **lpcData.cs:** 2 copies — `Turan_core/Turan_core/`, `Felismero_motor_LITE/Felismero_motor/`.
- **CV_FELDOLGOZO.cs:** 2 copies — `Turan_creator/Turan_creator/`, `Felismero_motor_LITE/Felismero_motor/`.

All 11 files receive the §2a header. The manifest (§2b) and guard (§2c) are added once.

---

## 4. Backward-compatibility / format impact

**In-scope change (this pass): NONE.** Comment headers + two new non-compiled files (`.md`, `.sh`) touch no
algorithm, no public signature, no on-disk/template/serialization format. Nothing breaks; no versioned read needed.

**Deferred full extraction (follow-up, requires a build toolchain) — WOULD break things; documented so a human
reviewer can accept this mitigation or schedule the refactor:**
- **Unified namespace** ripples to every consumer that references these classes by simple name
  (`Turan_core`, `Turan_creator`, `Turan_tester`, `Felismero_motor`) → mass `using`/qualification edits.
- **`mfcc_lpc_vect_num` 12-vs-15 split** is mode-dependent (LPC vs MFCC) and owned by different classes; a single
  shared symbol cannot hold both — it must become an instance/config parameter, a behavioral change.
- **GUI coupling:** LITE's `H_FELDOLGOZO.Show2dArray` depends on WinForms `DataView`; pulling it into a headless
  core lib drags in a UI dependency — must be split out first.
- Recommended target shape: a `Turan.Dsp` class-library project (Engine/DTW/HTK/H_FELDOLGOZO/lpcData/CV minus GUI
  helpers), referenced by all four front ends, with width as a constructor/config field. No template/disk-format
  change **iff** `SerializeArray`/`DeSerializeArray` byte layout is preserved (see §5).

---

## 5. Shared contracts other bug-fixes depend on (flag, do NOT change here)

1. **Vector-width contract (BUG-01, BUG-08, BUG-09).** Width is read from **two different symbols by world**:
   - World A (core/tester live path): `Engine.mfcc_lpc_vect_num` (=15) — read by `dtwApp_match.cs:84,150,162,350,406`.
   - World B (LITE) + creator's `CV/H_FELDOLGOZO/lpcData`: `H_FELDOLGOZO.mfcc_lpc_vect_num` (=12).
   BUG-01's "set width to 60" must update the **correct symbol in each world**, and the live MFCC_D_A_T reader is
   the **core/tester** HTK_Interface, not LITE's. The manifest records this so BUG-01 does not flatten 12→15.
2. **Template serialization format (BUG-12).** Creator writes via `Creator.SerializeArray` (`Creator.cs:318`,
   `BinaryFormatter`; emits `.lpc`/`.mfcc`); Engine reads via `Engine.DeSerializeArray` (`Engine.cs:162`). This
   write/read pair is a cross-module on-disk contract: BUG-12's replacement must change **both ends together** and
   provide a versioned read for existing templates. BUG-14's guard should later assert both ends use the same
   serializer once BUG-12 lands.

---

## 6. Self-verification WITHOUT a compiler

1. **Header edits are comment-only:** `diff` proves every added line begins with `//` and sits between the
   `namespace … {` brace and the type declaration; no existing token changed. Re-run the §1 `md5sum` to confirm
   no code line moved except the inserted comment block.
2. **Manifest accuracy:** re-run the orientation greps that produced §1/§5 (`grep -rn "mfcc_lpc_vect_num"`,
   pairwise `diff`) and confirm the manifest tables match current reality.
3. **Guard correctness:** run `tools/check_duplicates.sh` here. With no correctness bug applied, all per-fix
   checks are skipped → exit 0; the structural-diff section prints known divergences. Dry-run a future check by
   editing one copy only and confirming the script flags the missed sibling (exit non-zero).
4. **No format/contract drift:** grep confirms `Creator.SerializeArray`/`Engine.DeSerializeArray` and the two
   `mfcc_lpc_vect_num` owners are byte-identical before/after this pass.

---

## 7. Out of scope (explicitly)

- Physical library extraction / namespace unification (deferred — §4; needs a build env).
- BUG-15 (`mfcc.cs` Java scaffolding) and BUG-16 (dead `hasonlit`) removal — separate roadmap items even though
  they shrink the duplication surface; do not fold them in here.
- Any change to LITE-only GUI helpers (`Show2dArray`/`DataView`).

---

## Peer review

**Reviewer verdict: NOT approved — fix the items below, then re-submit.**

The overall approach is sound and genuinely risk-free: defer the physical extraction (correctly justified — the copies have diverged semantically and cannot be compiler-verified here), and instead ship comment headers + a manifest + a parity guard. The structural facts that I could verify against source are mostly accurate: all 11 `namespace` line numbers match exactly (HTK x3 at line 7; dtw/H_FELDOLGOZO at 31; lpcData at 32; CV at 33); the UTF-8 BOM on both `dtwApp_match.cs` copies is real (`ef bb bf`); and the headline body divergences are all confirmed — `window_array[frame,…]` (creator) vs `[frame-1,…]` (LITE), `num_of_frames = 500` (core) vs `5` (LITE), `create_window_no_overlap` creator-only, `Show2dArray`/`DataView` LITE-only, the partial `magic13` refactor in core dtw vs bare `13` in LITE.

BUT: in this pass BUG-14's *entire deliverable value* is (a) a trustworthy manifest and (b) a working guard — extraction is explicitly deferred. I found provable defects in **both**, so the plan fails its own stated purpose. Concrete required changes:

### Required changes

1. **(Blocking) Guard BUG-04 anchor is a proven false-negative — and the defect is systemic.**
   The buggy DCT scale in source is `Math.Sqrt(2 / 24)` **with spaces** (`Turan_creator/.../H_FELDOLGOZO.cs:272`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:248`). The plan's guard (§2c) asserts the literal string `2/24` is "present in ZERO" copies. That string **never appears** (grep returns 0 right now, in the *unfixed* tree), so the assertion passes today and can never detect the unfixed state. Fix: use a whitespace-tolerant pattern (`2 */ *24`) and do not assume the fix is spelled exactly `Math.Sqrt(2.0/24.0)`.
   This is not a BUG-04 one-off. The BUG-02 anchor ("pre-emphasis reads `win_pcmdata`/`pcmdata` in both copies") is equally vacuous: `win_pcmdata` already appears on the surviving Hamming line (`H_FELDOLGOZO.cs:210`), so "token present" cannot discriminate fixed from unfixed. The discriminating change is that the **pre-emphasis subtraction line stops referencing `tmparray`**. **Require every per-fix anchor to test the specific changed expression, not the presence of a token that already exists in the file.** Re-audit all listed anchors (BUG-01/02/04/06/09) against this rule. (BUG-06's `firdata.Length - 1` and BUG-09's bare `new double[13]`/`< 13` anchors are real and do discriminate — keep those.)

2. **(Blocking) The width-constant contract is mischaracterized, and the error is baked into the in-scope §2a comment headers (11 source files), not just deferred prose.**
   Accurate model from source:
   - `Engine.mfcc_lpc_vect_num` (`Engine.cs:29`): init 15, **never reassigned at runtime** → the "(=15)" annotation for the core `dtwApp_match` copy is correct.
   - `H_FELDOLGOZO.mfcc_lpc_vect_num` (`H_FELDOLGOZO.cs:36`): init 12 but **runtime-reassigned by mode** — `Creator.cs:142/156` and `Felismero_motor_LITE/Form1.cs:104/108` set it to 12 (LPC) or 15 (MFCC). So the header's flat "(=12)" is wrong; it is mode-dependent, not a fixed per-world value.
   - The **live HTK MFCC_D_A_T reader width** comes from the Engine.cs **local** `num_of_feature_vectors` (`Engine.cs:111-120`), *not* from either static symbol; `dtwApp_match` reads `Engine.mfcc_lpc_vect_num` only once into `num_of_vectoritems` (`dtwApp_match.cs:84`).
   Consequences: the §2a header text "Width constant here = Engine.mfcc_lpc_vect_num (=15). Sibling uses H_FELDOLGOZO.mfcc_lpc_vect_num (=12)" is inaccurate and would be inserted into the source files. And §5's guidance that "BUG-01 must update the correct symbol" is incomplete — BUG-01 must change the reader's 60-dim allocation/concatenation **and** the DTW width path; setting a static symbol to 60 alone does not reach the live local `num_of_feature_vectors`. Fix the header wording and §5 to say "mode-dependent (12 LPC / 15 MFCC), reassigned at runtime" for the H_FELDOLGOZO symbol, and record the local-`num_of_feature_vectors` reader as the actual BUG-01 edit site.

3. **(Blocking) Manifest factual errors — §1 "same split for `lpcData.cs:47` and `CV_FELDOLGOZO.cs:47`" is false.**
   - LITE `lpcData.cs:47` is a **comment** (`//private static int num_of_lpc_vectors = 13;`). Its real read of `H_FELDOLGOZO.mfcc_lpc_vect_num` is at **line 114**. So "the LITE copy at :47 reads H_FELDOLGOZO.mfcc_lpc_vect_num" is wrong.
   - `CV_FELDOLGOZO.cs:47` reads `H_FELDOLGOZO.mfcc_lpc_vect_num` in **both** creator and LITE — there is **no** `Engine.` vs `H_FELDOLGOZO.` split. The §2a table row for CV even says "only namespace differs today," which contradicts §1. Remove the false "same split" claim; correct the cited line.

4. **(Blocking) §5 line citations are wrong.** "`Engine.mfcc_lpc_vect_num` … read by `dtwApp_match.cs:84,150,162,350,406`": only **:84** reads it. Line 150 is blank, 162 is a commented-out line, 350 is `Math.Abs(sum) < 1e-6`, 406 is a brace. Actual downstream uses are via `num_of_vectoritems` (and `new double[Engine.mfcc_lpc_vect_num]` at 180/193/402/403/466/467). A manifest that BUG-01/08/09 will trust must cite real lines.

5. **(Minor) "Three different `CreateMFCC_D_A_T` signatures" is two.** Core is `(wav, config, script)`; tester `(config, script_file, app_path)` and creator `(config, script_file_path, app_path)` are the **same** signature — only a parameter *name* differs. The "do not blindly unify" warning still holds (core differs), but the count should read "two distinct signatures (core vs tester/creator)."

6. **(Minor, add to §2a/§6) Verify encoding/BOM on all 11 files, not just `dtwApp_match.cs`.** The plan only calls out the dtw BOM. The implementer should confirm each file's existing encoding/BOM before inserting the header so no copy's encoding is silently changed.

### Not blocking / confirmed good
- Backward-compat assessment (§4) is correct: comment-only edits + two non-compiled files (`.md`, `.sh`) touch no algorithm, signature, or on-disk/template format. No off-by-one or integer-division trap is *introduced* by this pass (the only integer-division issue, `2 / 24`, is pre-existing BUG-04 and merely under-guarded, per item 1).
- Decision to defer physical extraction and namespace unification is well-justified and should stand.
- The cross-module serialization contract flag (§5.2, `Creator.SerializeArray` ↔ `Engine.DeSerializeArray` for BUG-12) is appropriate and consistent.

Net: approach approved, deliverable not yet trustworthy. Address items 1–4 (the manifest/guard accuracy defects) and the verdict flips to approve.
