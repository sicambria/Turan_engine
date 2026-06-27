# Parallel, Conflict-Free Implementation Plan (FINAL)

**Produced:** 2026-06-27 · **Branch:** `fix/roadmap-bugs` · Machine-readable twin: `plans/_grouping.json`

This section is the authoritative grouping. The original adversarial review (the file/contract
conflict matrix it is built on) is preserved verbatim below under "Plan Summary & Adversarial
Pre-Implementation Review".

## TL;DR

Every bug except **BUG-15** forms a single connected component in the bug↔file graph (the
duplicated hub files `Engine.cs`, `dtwApp_match.cs ×2`, `H_FELDOLGOZO.cs ×2`, `Creator.cs`,
LITE `Form1.cs`, `HTK_Interface.cs ×3` chain everything together). Three deliberate cuts split
that component into **three file-disjoint parallel groups**:

1. **BUG-08** is reduced to its `dtwApp_match.cs` `costRecord` part only; its `hasonlit`/
   `H_FELDOLGOZO.cs` cap edit is **dropped** because **BUG-16** deletes `hasonlit`.
2. **BUG-03**'s *optional* `Turan_core/Engine.cs` enum comment is **dropped**, keeping BUG-03
   inside Group B's four files (it is otherwise comment-only).
3. **BUG-12** (serialization) is split by **file ownership** — reader → Group A, writer → Group B —
   and bound **byte-exactly** by a shared contract (the task's own Creator/Engine example).

**BUG-14** (de-duplicate the triplicated files into a shared library) is **DEFERRED**: even its
"safe" Phase A touches every duplicated file and conflicts with all parallel work.

## Groups (run concurrently — zero shared files)

| Group | Theme | Bugs | Owned files (live copies) |
|---|---|---|---|
| **A** | DTW / Engine / HTK | 01, 08, 09, 10, 11, 12(reader), 13 | `Turan_core/dtwApp_match.cs`, `Felismero_motor_LITE/dtwApp_match.cs`, `Turan_core/Engine.cs`, `HTK_Interface.cs` ×3 (core/tester/creator), `Turan_tester/Form1.cs`, `Turan_core/lpcData.cs` (verify-only) |
| **B** | DSP feature extraction | 02, 03, 04, 05, 06, 07, 12(writer), 16 | `Turan_creator/H_FELDOLGOZO.cs`, `Felismero_motor_LITE/H_FELDOLGOZO.cs`, `Turan_creator/Creator.cs`, `Felismero_motor_LITE/Form1.cs` |
| **C** | Dead-code removal | 15 | `Felismero_motor_LITE/mfcc.cs`, `Matrix.cs`, `FFT-converted.cs` |

Disjointness is exact: no path appears in two groups. Group A owns `Turan_creator/HTK_Interface.cs`
while Group B owns `Turan_creator/Creator.cs` + `H_FELDOLGOZO.cs` — different *files* in the same
project, so no write race.

## Per-group execution order & guardrails

**Group A** — order: *decide width-source → 01 → 09 → 10 → 08 → 11 → 12(reader) → 13(last)*.
- **Width contract** (binds 01/08/09/10): width = `data.GetLength(1)` / live `Engine.mfcc_lpc_vect_num`,
  never a hard-coded 15 or 60. BUG-01 concatenates HTK streams into `4*N` cols and sets the global
  on **both** branches. BUG-09 derives Itakura order from the array (BUG-09's "BUG-01 sets static
  to 60" premise is **false** — do not rely on it). Also fix `temp3`/`temp4` at `dtwApp_match.cs:466/467`.
- **BUG-08 = `costRecord` only** (Changes 1+2); its `hasonlit` cap is dropped (16 deletes `hasonlit`).
- **BUG-10** ships the `REJECTED = -2` mechanism only; threshold **calibration is deferred** (needs
  runtime cost-scale data after 01) — do not close BUG-10 on the diff.
- **BUG-13 = last, runtime-UNVERIFIED**: process-terminating `throw`, relative `WorkingDirectory`,
  `ReadToEnd` deadlock. MUST include the `Turan_tester/Form1.cs:90` caller guard; gate merge on a
  real Windows + HCopy smoke test. Keep its risk off the diff-verifiable fixes.

**Group B** — order: *16 (delete dead) → 04 → 02 → 05 → 06 → 03 (comments) → 07 → 12(writer)*.
- Apply every `H_FELDOLGOZO.cs` change to **both copies** byte-identically.
- **BUG-03 is label-only**: drop the optional `Engine.cs` enum comment; describe the FFT-discarded
  defect **inline** (no `BUG-17` token, no `ROADMAP.md` edit). BUG-03 stays **OPEN** (naming trap
  only); the accuracy half remains tracked.
- **Template-staleness barrier**: BUG-02 changes native `.mfcc` feature content → all templates must
  be regenerated from source WAVs after merge. Sequence 02/03 into one regenerate-once release.

**Group C** — `git rm` the three dead Java-port files. No `.csproj` edit (non-SDK project, files
absent from every `<Compile Include>`). Fully isolated.

## Shared contracts (see `_grouping.json` for byte-exact text)

- **Width contract** (A-internal).
- **REJECTED = -2 sentinel** (A-internal, both dtw copies).
- **TRMS serialization format** (cross A↔B): `['TRMS'][ver=1][int32 rows][int32 cols][rows*cols
  double, LE, row-major]`; reader keeps a `BinaryFormatter` legacy fallback; both halves merge
  together before any regeneration. Fallback option: one atomic phase-2 commit if byte-parity can't
  be guaranteed across the two agents.
- **H_FELDOLGOZO duplicate parity** (B).
- **Dead-method ownership** (A↔B logical only): 16 deletes `hasonlit` → 08 drops its `hasonlit` edit.

## Deferred

- **BUG-14** — broad cross-cutting de-duplication into a `Turan.Dsp` library. Conflicts with both A
  and B (touches every duplicated file), introduces a contradicting third width-source, and is
  compiler-gated. Resume Phase A only, after A/B/C merge and a build toolchain exists.

## Commit chunks (ordered; docs separate from code)

1. `docs: add per-bug fix plans, parallel grouping and plan summary` — `plans/*` (scaffolding).
2. `fix(htk): concatenate MFCC_D_A_T delta streams instead of summing` — BUG-01.
3. `fix(dtw): derive distance width/Itakura order from live vector width` — BUG-09.
4. `fix(dtw): add confidence-threshold rejection sentinel` — BUG-10.
5. `fix(dtw): size costRecord/backtrace from actual frame count` — BUG-08.
6. `fix(engine): implement MatchLength duration matching` — BUG-11.
7. `refactor(serialization): replace BinaryFormatter with TRMS format` — BUG-12 (A+B, atomic).
8. `fix(htk): make HCopy path configurable and capture exit/stderr` — BUG-13 (runtime-unverified).
9. `refactor(dsp): remove dead native DTW hasonlit and backing fields` — BUG-16.
10. `fix(dsp): apply pre-emphasis and correct DCT scale; fir/hamming fixes` — BUG-02, 04, 05, 06.
11. `docs(dsp): label native mfcc mode as log-mel (no DCT)` — BUG-03 (comment-only).
12. `fix(creator): log and propagate per-frame FFT/LPC failures` — BUG-07.
13. `chore: remove dead Java-port mfcc/Matrix/FFT-converted scaffolding` — BUG-15.

---

# Plan Summary & Adversarial Pre-Implementation Review

> NOTE: `plans/_grouping.json` and this `_PLAN_SUMMARY.md` did **not** exist when the
> review below was performed. The review therefore treats "grouping" as *any* proposed
> partition of BUG-01..16 into parallel work units, and supplies the file/contract
> conflict matrix needed to build a safe one. **The absence of a grouping artifact is
> itself a blocking issue** (see Blocking Issue #0).

---

## Adversarial review — 2026-06-27 (reviewer: parallel-safety / contract audit)

Verdict: **approved = false.** The individual fix plans are unusually thorough and most
diagnoses are correct (several were re-verified against source below). But the plans are
written as if each lands in isolation. As a *set to be executed in parallel groups* they
contain hard file-level races, a triple-edit collision on one dead method, and at least
one shared contract (DTW vector width) that three plans implement three different,
mutually-inconsistent ways — including one plan (BUG-09) built on a false assumption
about what another (BUG-01) does. Four of the plans already self-rate approved=false
(BUG-01, BUG-08, BUG-13, BUG-14) for their own internal reasons; those are not re-litigated
here except where they interact.

### Method
Read all 16 plans plus ROADMAP. Extracted every `*.cs` target per plan, then verified the
load-bearing collisions directly in source:
- `Engine.cs:29` `public static byte mfcc_lpc_vect_num = 15;` — grep confirms it is **never
  reassigned anywhere in Turan_core**. So Turan_core's live DTW width is *always* 15.
- `dtwApp_match.cs` (Turan_core) reads that static at lines 84, 180, 193, 402, 403, 466, 467.
- `mfccszamitas` at `Turan_creator/.../H_FELDOLGOZO.cs:222` / `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198`,
  with the `Math.Sqrt(2 / 24)` defect at line 272 / 248.
- `hasonlit` at `Turan_creator/.../H_FELDOLGOZO.cs:294` / `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:330`.

### File-touch conflict matrix (which bugs edit the same physical file)

| File (live copy) | Bugs that edit it | Collision type |
|---|---|---|
| `Turan_core/.../dtwApp_match.cs` | **01, 08, 09, 10, 14** | width-contract + multi-region |
| `Felismero_motor_LITE/.../dtwApp_match.cs` | **01, 08, 09, 10, 14** | same |
| `Turan_creator/.../H_FELDOLGOZO.cs` | **02, 03, 04, 05, 06, 08, 16** | dead-method triple/double collision |
| `Felismero_motor_LITE/.../H_FELDOLGOZO.cs` | **02, 03, 04, 05, 06, 08, 16** | same |
| `Turan_core/.../Engine.cs` | **10, 11, 12, 14** (01 optional) | multi-region |
| `Turan_creator/.../Creator.cs` | **03, 07, 12, 14** (13 caller) | multi-region |
| `Felismero_motor_LITE/.../Form1.cs` | **03, 05, 07, 10, 16** | multi-region |
| `*/HTK_Interface.cs` (3 copies) | **01, 13, 14** | different methods, same file |
| `Felismero_motor_lite.csproj` | **14, 15** | both edit `<Compile Include>` set |
| `Turan_core/.../lpcData.cs` | **01** (12 reads it) | low |

Every cell with ≥2 bugs is an unsafe parallel boundary unless those bugs are placed in the
**same** group and applied **sequentially** in a defined order.

> **Ask (b) — "a duplicated copy a plan forgot": independently checked, none found among the
> named files.** Ran `find` for all copies of `H_FELDOLGOZO.cs`/`dtwApp_match.cs`/`HTK_Interface.cs`/
> `Engine.cs`/`lpcData.cs`/`mfcc.cs`/`Creator.cs`/`CV_FELDOLGOZO.cs` *independently of the plans'
> claims*: exactly H_FELDOLGOZO ×2, dtwApp_match ×2, lpcData ×2, CV_FELDOLGOZO ×2, HTK_Interface ×3,
> mfcc ×1, Engine ×1, Creator ×1. A grep for recognizer surface
> (`bestMatch|dtwApp_match|SerializeArray|win_meldata|Window_Mel_Scale_Reduction|RecognizeAndReturnIndex`)
> across `Turan_SC/`, `Turan_SC_minimal/`, `Turan_trainer_GUI/`, `HTK-TEST-CMD/` returns **nothing** —
> those projects carry no recognizer code, so no DSP/DTW/serialization plan omits a copy. Two refinements
> the plan-mention grep alone would miss: **BUG-07 also edits a live byte-for-byte duplicate of
> `FFTCalcFrameByFrame`/`LPCCalcFrameByFrame` in `Felismero_motor_LITE/.../Form1.cs`** (its §1/§3, so it
> joins the Form1 cluster above), and **BUG-14 (Phase C) and BUG-15 both edit `Felismero_motor_lite.csproj`**
> (`<Compile>`/`ProjectReference` rewrites) — keep them ordered. `mfcc.cs` is single-copy and edited only by
> BUG-15 (BUG-04 merely cross-references it; BUG-14's inventory must state it is BUG-15-owned). Most plans say "defer to
whoever lands first," which is a *serialization* requirement — it is incompatible with
running those bugs as separate parallel groups.

---

## BLOCKING ISSUES

**#0 — No grouping artifact exists; it cannot be validated, and the naive grouping is unsafe.**
`plans/_grouping.json` is absent. There is nothing to approve. Whatever grouping is produced
must respect the matrix above. In particular, grouping by ROADMAP severity (P0/P1/P2/P3) —
the obvious default — is *maximally* unsafe: it splits the `H_FELDOLGOZO.cs` cluster across
P0 (02,03), P1 (04,05,06), P2 (08) and P3 (16), and the `dtwApp_match.cs` cluster across
P0 (01), P2 (08,09,10) and P3 (14) — guaranteeing concurrent edits to the same files.

**#1 — Triple-edit collision on `mfccszamitas` (a single dead method).**
- BUG-03 (§2d, "Preferred") **deletes** `mfccszamitas` entirely (Creator 217–275 / LITE 193–251).
- BUG-04 **edits line 272/248 inside it** (`Math.Sqrt(2 / 24)` → `2.0 / 24.0`).
- BUG-08 §3.2(a) **edits its caps and widens its `byte` loop counters** (lines 224/230/231/238).
If these run in different groups, two of the three operate on text the third removes →
merge conflict or silently-lost edits. BUG-03 itself states it "subsumes BUG-04," and BUG-04
states "BUG-03 depends on BUG-04" — they are mutually entangled and **must be one work item**.
Decision required up front: *delete `mfccszamitas` (BUG-03) → then BUG-04 and BUG-08's
`mfccszamitas` edits become no-ops and must be dropped from those plans.* Do not let BUG-04
"fix" a line BUG-03 will delete, and do not let BUG-08 widen loops in a method being deleted.

**#2 — Double-edit collision on `hasonlit` (dead method).**
BUG-16 **deletes** `hasonlit` + its field block (Creator 279–373 / LITE 315–409). BUG-08
§3.2(b) **rewrites** `hasonlit` (localizes `tavtomb`/`ertomb`, adds `count_param` guards).
Same conflict class as #1. Resolution: if BUG-16 deletes `hasonlit`, BUG-08's `hasonlit`
edits (and its `count_param==0` guard that BUG-08's own peer review flagged as blocking) are
moot and must be dropped. Sequence BUG-16 before BUG-08, or fold both into one item.

**#3 — The DTW vector-width contract is implemented three inconsistent ways, and BUG-09 is
built on a false premise.** All three rewrite the *same* lines (`dtwApp_match.cs:84/180/193/402/403`
and the `ITDDistance` body at 255+):
- **BUG-01:** width comes from `data.GetLength(1)` (per-array, 60/15/12), and **explicitly
  leaves `Engine.mfcc_lpc_vect_num` at 15** (it rejects the ROADMAP "bump to 60" shorthand).
- **BUG-09:** sets Itakura `order = Engine.mfcc_lpc_vect_num`, and its shared-contract section
  asserts *"BUG-01 plans to widen the feature vector to 60 dims and update the live static
  accordingly … ITDDistance automatically follows to width 60."* **This is false.** Verified:
  `Engine.mfcc_lpc_vect_num` stays 15 (Engine.cs:29, never reassigned). So if BUG-01 + BUG-09
  both land, HTK-mode reference vectors are 60-wide (BUG-01) while `ITDDistance` uses order 15
  (BUG-09) — a width mismatch baked in by two plans. (Latent only because `distanceType` is
  hard-coded `"Euclidean"`, but it is a real, shipped inconsistency and a trap for whoever
  ever enables Itakura.)
- **BUG-14 Phase B:** replaces those same references with `Turan.Dsp.FeatureConfig.VectorLength`
  — a *third* source of truth, and its own peer review already showed the proposed writers
  change core's live LPC width 15→12 (a behavior change BUG-14 claims it does not make).
These three cannot coexist as written. A single width-source decision must be made **before**
any of 01/09/14 is implemented, and all three plans rewritten to that one decision. Recommended:
adopt BUG-01's "width from `data.GetLength(1)`" everywhere (including deriving the Itakura order
from the array, not from any static), and strike BUG-09's static-based `order` and BUG-14 Phase B's
`FeatureConfig.VectorLength` width-rewrite.

**#4 — The serialization contract (BUG-12) spans Creator + Engine + LITE and must be one
atomic unit.** `Creator.SerializeArray` (writer), `Engine.DeSerializeArray` (reader), and LITE
`Form1.cs` (both) must change to the new `TRMS` format **byte-identically and in the same
release**. The plan says so, but at the grouping level this means BUG-12 **cannot be split**
across groups by module (writer in one, reader in another): a partial landing makes templates
written by a patched Creator unreadable by an unpatched Engine. Keep all four method bodies in
one work item. Also: BUG-12 §6.5 reasons about `lpcData.getRowLength()` using
`data.Length / mfcc_lpc_vect_num`, but **BUG-01 changes that method to `data.GetLength(0)`**.
The two descriptions of the same function are inconsistent; BUG-01's version is the correct one
and is compatible with BUG-12, but BUG-12's verification step must be updated so the reviewer
doesn't validate against code BUG-01 has already removed.

**#5 — Cross-plan template-staleness hazard with no detection marker.** BUG-02 (pre-emphasis),
BUG-03 (DCT), and the new "discarded-FFT mel-input" bug BUG-03 §7 flags **each independently
require a full regeneration of every `.mfcc` template**, and **none adds a version/extension
marker** — old and new `.mfcc` files are byte-shape-identical and load without error, so a
mismatch silently degrades recognition (the dangerous failure mode for a command system). If
these land in separate groups/releases, every intermediate state ships silently-stale templates.
BUG-03's own peer review recommended an extension bump / version sentinel and the plan deferred
it. For a parallel rollout this deferral is not acceptable: a staleness barrier (bump `.mfcc`
extension or add a feature-version byte) must land **with the first** feature-changing fix, and
all feature-content fixes (02, 03, discarded-FFT) should be sequenced into a single
"regenerate once" release rather than spread across groups.

---

## NON-BLOCKING / HIGH-RISK (verify-with-compiler) ITEMS

- **BUG-13 is high-risk and self-rated approved=false.** Its peer review found a
  process-terminating regression on the *live* path (new `throw` escapes the
  `Turan_tester/Form1.cs` caller's try/catch at line ~90), a `UseShellExecute=false` +
  relative `WorkingDirectory` failure that would make HCopy fail on every run, and a
  sequential-`ReadToEnd` deadlock anti-pattern. None of these can be confirmed without
  running the app + a real HCopy. Treat BUG-13 as compiler/runtime-gated; do not land it in
  a parallel batch with the feature fixes. Note its required caller-guard edits
  `Turan_tester/Form1.cs`, which overlaps nothing else here but must be tracked.
- **BUG-14 Phases C/D** (new `Turan.Dsp` project, `<ProjectReference>` rewiring, namespace
  moves, internal→public) are explicitly compiler-required and unverifiable here. BUG-14 Phase B
  is *not* behavior-neutral (see #3). Land only Phase A now (drift reconciliation, diff-verifiable),
  and only after #1/#2/#3 owners have settled the `H_FELDOLGOZO`/`dtwApp_match` behavior — Phase A
  reconciles the very copies those bugs are rewriting, so it must run **after**, not concurrently.
- **BUG-12 legacy-fallback magic-byte detection** cannot be empirically tested (no `.lpc`/`.mfcc`
  corpus in-repo; verified from the MS-NRBF spec only). Acceptable, but flag as runtime-unverified.
- **BUG-10** is correct and default-inert, but it adds two independent `REJECTED = -2` constants
  (Engine + dtwApp_match) kept equal only by convention, and its real value (a calibrated
  threshold) needs runtime data gathered *after* BUG-01/03 change the cost scale. Do not mark
  BUG-10 "closed" on the code change alone, and sequence its calibration after the feature fixes.
- **BUG-01 residual** (per its own peer review): `temp3`/`temp4` in `lefttorightMatch`
  (`dtwApp_match.cs:466/467`) are left at the static width and remain a latent OOB on the 12-wide
  native-LPC path; its §7.5 "incidental LPC repair" claim is false until those are also fixed.
  This must be resolved inside the same width-contract decision as #3.

---

## RECOMMENDATIONS (concrete)

1. **Produce `plans/_grouping.json` and make each group file-disjoint on live copies.** Suggested
   safe clusters (each cluster = one sequential group, never split):
   - **G-DSP-HFELD** = {16 → 03(+subsumes 04) → 08 → 02 → 05 → 06}, applied in that order to both
     `H_FELDOLGOZO.cs` copies (+ Creator.cs/Form1.cs MFCC-branch call sites for 03). Deletes
     (`hasonlit`, `mfccszamitas`) first so later edits don't touch removed text.
   - **G-DTW** = {decide width-source → 01 → 09 → 10 → 08(costRecord part) → 14-PhaseA}, applied to
     both `dtwApp_match.cs` copies + `lpcData.cs`. 08's `dtwApp_match` part (costRecord) and its
     `H_FELDOLGOZO` part can be split between G-DTW and G-DSP-HFELD only if 08 is authored as two
     separate diffs; otherwise keep 08 whole in whichever group and serialize.
   - **G-SERIAL** = {12} across Creator.cs + Engine.cs + LITE Form1.cs, atomic.
   - **G-HTK** = {13} across the 3 HTK_Interface.cs copies + the BUG-01 reader edit must be
     reconciled (01 edits `ReadMFCC_D_A_T`, 13 edits `CreateMFCC_D_A_T` in the *same files*) — so
     01's HTK-reader edits and 13 must be in the same group or strictly ordered.
   - **G-MISC** = {11, 07} (Engine stub, Creator catch-blocks) — verify these don't touch 12's
     regions of the same files before parallelizing.
2. **Make one width-source decision (issue #3) before touching `dtwApp_match.cs` at all,** and
   rewrite BUG-01/09/14 to it. Strike BUG-09's false "BUG-01 updates the static to 60" premise.
3. **Resolve the dead-method ownership (issues #1/#2) by deletion-first:** BUG-16 deletes
   `hasonlit`; BUG-03 deletes `mfccszamitas`; then drop the now-moot `mfccszamitas`/`hasonlit`
   edits from BUG-04 and BUG-08.
4. **Land a template-staleness barrier with the first feature-content fix (issue #5)** and batch
   BUG-02/03/discarded-FFT into a single regenerate-once release.
5. **Keep BUG-12 (all 4 method bodies) and BUG-13 (all 3 HTK copies) each as indivisible units;**
   never split a serialization or HTK contract across groups.
6. **Gate BUG-13, BUG-14 C/D, and BUG-10 calibration behind an actual build/run;** mark them
   compiler-unverified in the grouping so they are not batched with the diff-verifiable fixes.
7. **Re-confirm there is no forgotten copy per group at apply time** with
   `find -name <file>.cs` immediately before editing (the project's duplication is the stated
   top hazard; e.g. all 3 `HTK_Interface.cs` for 01/13, both `H_FELDOLGOZO.cs` and both
   `dtwApp_match.cs` for every DSP/DTW fix).

---

## Adversarial review #2 of the FINAL grouping (`_grouping.json` + this summary) — 2026-06-27

Reviewer: parallel-safety / contract audit, round 2 (post-grouping). Scope: the grouping
artifacts now exist, so this round audits the *actual* A/B/C partition and its shared
contracts against source, hunting for races, forgotten copies, format breaks, width/off-by-one
mismatches, inconsistent cross-group contracts, and compiler-unverifiable risk.

**Verdict: approved = false.** File-disjointness of A/B/C is genuinely correct (verified: no
path appears in two groups; Group A owns `Turan_creator/HTK_Interface.cs` while Group B owns
`Turan_creator/Creator.cs`+`H_FELDOLGOZO.cs` — different files). The dead-method ownership cuts
(BUG-16 deletes `hasonlit` ⇒ BUG-08 drops its `hasonlit` cap; BUG-03 label-only) are sound, and
the BUG-08 §3 `hasonlit` OOB the per-bug peer review found is correctly mooted by deletion.
But three concrete defects remain, two of them in the *shared contracts that bind two parallel
groups* — exactly the class that file-disjointness does not protect against.

### BLOCKING #A — The BUG-12 on-disk format is specified TWO different, byte-incompatible ways, and the two specs are split across two parallel groups.
- `_grouping.json` "TRMS SERIALIZATION FORMAT" contract (line 56):
  `['TRMS'][1-byte version=1][int32 rows][int32 cols][doubles]`.
- `plans/BUG-12.md` §2 (the actual implementation spec the writer/reader bodies are copied from):
  magic `"TRA1"` (0x54 0x52 0x41 0x31), **no version byte**, `int32 rows` immediately at offset 4.
These disagree on (1) the 4-byte magic (`TRMS` vs `TRA1`) and (2) the presence of a 1-byte
version field — which shifts the rows/cols offset by one byte. BUG-12 is **split by file
ownership**: the **writer** `Creator.SerializeArray` is in **Group B** (`Turan_creator/Creator.cs`)
and the **reader** `Engine.DeSerializeArray` is in **Group A** (`Turan_core/Engine.cs`). Two
parallel agents, each reasonably trusting "their" governing document, will emit
`TRA1`-no-version (writer, from BUG-12.md) and expect `TRMS`+version (reader, from _grouping.json)
— or vice-versa. Result: every `.mfcc`/`.lpc` written by the patched Creator is **silently
unreadable** by the patched Engine (or read with rows/cols off by one byte → garbage/`OOM`).
The "atomic A+B commit" in commit-chunk #7 does NOT save this: an atomic commit of a mismatched
reader+writer pair is still broken. The grouping's own fallback ("single atomic phase-2 commit
containing all four method bodies") is the right escape hatch but is gated on noticing the
mismatch, which the contradictory specs actively prevent.
*Fix:* freeze ONE byte-exact layout in BOTH documents before implementation (recommend keeping
BUG-12.md's `TRA1`/no-version OR adding the version byte to BUG-12.md — but pick one), and author
all four method bodies (Creator writer, Engine reader, LITE Form1 writer+reader) from that single
frozen block; diff the four bodies before merge. Cross-check the magic-sniff offset and the
`ReadInt32` start byte match the writer exactly.

### BLOCKING #B — WIDTH CONTRACT "Same edits mirrored byte-identically in the LITE dtwApp_match.cs copy" contradicts BUG-01.md on scope and mismatches the live source.
Headline (the substance): the two governing docs disagree on scope — `_grouping.json` says
"mirror to LITE", `BUG-01.md` §3 says "do NOT change `Felismero_motor_LITE/.../dtwApp_match.cs`" —
and the LITE copy has its OWN real staleness bug independent of HTK: `num_of_vectoritems` is
captured once at static-init from the default `H_FELDOLGOZO.mfcc_lpc_vect_num = 12`, yet
`Form1.cs:108` reassigns that field to `15` for MFCC mode (verified at LITE `Form1.cs:104,108`),
so the LITE distance loop compares only 12 of 15 dims. The *intent* (read live width in LITE too)
is therefore correct and worth doing; "byte-identical" is simply the wrong instruction for it.
Corollary (the easy-to-self-correct part): the two `dtwApp_match.cs` copies use *different* width
symbols —
`Turan_core/.../dtwApp_match.cs:84/180/193/235/402/403/466/467` reference
**`Engine.mfcc_lpc_vect_num`**, while `Felismero_motor_LITE/.../dtwApp_match.cs` references
**`H_FELDOLGOZO.mfcc_lpc_vect_num`** and contains **no reference to `Engine` at all** (grep:
zero hits). So a literal "byte-identical mirror" of BUG-01 §2c
(`for (i=0; i<Engine.mfcc_lpc_vect_num; i++)`) and temp3/temp4 at `:466/467` into the LITE copy
references a symbol that does not exist in the `Felismero_motor` module ⇒ **compile error**,
and is unverifiable here (no toolchain). Worse, this directly contradicts `BUG-12.md`'s sibling
`BUG-01.md` §3, which states "do NOT change `Felismero_motor_LITE/.../dtwApp_match.cs`."
Note the LITE copy DOES have a genuine staleness bug worth fixing — `num_of_vectoritems`
(captured once = default 12) goes stale when `Form1.cs:108` sets
`H_FELDOLGOZO.mfcc_lpc_vect_num = 15` for MFCC mode (verified at LITE `Form1.cs:104,108`) — so
the *intent* (read live width) is correct, but the contract text is wrong on two counts.
*Fix:* replace "byte-identical" with "mirror the INTENT, using the per-module width symbol
(`H_FELDOLGOZO.mfcc_lpc_vect_num` in LITE, `Engine.mfcc_lpc_vect_num` in core)" — exactly the
pattern `BUG-09.md` §2 already uses correctly (it differs the order-source line per copy). Decide
explicitly whether the LITE `EuclideanDistance`/temp3/temp4 width edits are in scope (they fix a
real LITE staleness bug) and reconcile `BUG-01.md` §3 accordingly.

### BLOCKING #C — Feature-content fix (BUG-02) silently invalidates existing trained templates with no automatic staleness marker.
BUG-02 (pre-emphasis) changes native `.mfcc` feature *content*. Existing enrolled
speaker-dependent templates remain byte-shape-identical and load without error, so a stale
template + patched extractor **silently degrades recognition** — the dangerous failure mode for
a command/assistive system. The grouping addresses this only with a process note
("TEMPLATE-STALENESS BARRIER … regenerate from source WAVs … document in release notes"); it adds
**no machine-detectable marker**. Meanwhile BUG-12 introduces a TRMS header that *already has a
spare version byte* but does not use it to encode feature-extraction version. So the cheap,
correct barrier (bump the version byte / `.mfcc` extension when feature content changes, and have
the reader warn/reject on mismatch) is left unbuilt, and correctness depends on a human
remembering to re-enroll.
*Fix:* land a feature-version marker WITH the first feature-content fix — reuse BUG-12's TRMS
version byte to carry a feature-extraction version, and have `DeSerializeArray` surface a stale
mismatch. (This couples BUG-02/Group B with the BUG-12 header/Group A reader — sequence as the
single regenerate-once release the grouping already calls for.)

### NON-BLOCKING / reconcile-before-implement
- **Stale "BUG-09 premise is false" rationale.** `_grouping.json` meta + WIDTH CONTRACT assert
  "reject BUG-09's false premise that 01 sets the static to 60 … must NOT assume BUG-01 set the
  static to 60 (that premise is false)." This is carried over from the *pre-grouping* review of an
  OLDER BUG-01 that left the static at 15. The FINAL BUG-01 (`BUG-01.md` §2b + its mandatory
  peer-review change) **does** set `Engine.mfcc_lpc_vect_num = 4*N = 60` on the htk branch — the
  same grouping even mandates this ("sets … on BOTH branches (htk: 4*N; turan: 15)"). So the
  grouping contradicts itself: BUG-09's premise is now TRUE. Functionally harmless (both "array
  width" and "live static" equal 60 on the htk path, since `GetSignalVector` allocates
  `new double[Engine.mfcc_lpc_vect_num]`), but the contradictory text will confuse the implementer.
  Reconcile to: "derive order from the array OR the live static — they are equal post-BUG-01; do
  not hard-code."
- **BUG-10 REJECTED=-2 caller guard is cross-group but inert.** BUG-10 (Group A) ships the `-2`
  sentinel into both `dtwApp_match.cs` copies; the required caller guard is in
  `Felismero_motor_LITE/Form1.cs:510` (Group B). Default `RejectionThreshold = +∞` keeps it inert,
  so no race and no crash today — but when the threshold is ever calibrated, LITE `Form1.cs:510`
  (`lb_mfcc_files.Items[RecogResult]`) throws on `-2`. Track the guard as a Group-B follow-up.
- **Group C deletion confirmed build-safe.** `Felismero_motor_lite.csproj` is non-SDK (43 explicit
  `<Compile Include>` entries, ToolsVersion 12.0); `mfcc.cs`/`Matrix.cs`/`FFT-converted.cs` are
  absent from every include (grep: zero hits) ⇒ `git rm` needs no `.csproj` edit and cannot break
  the build. Fully isolated, as claimed.
- **File-disjointness re-verified independently.** All copies located via `find`: `dtwApp_match.cs`
  ×2, `H_FELDOLGOZO.cs` ×2, `HTK_Interface.cs` ×3, `Engine.cs` ×1, `Creator.cs` ×1, `lpcData.cs`
  ×2 (LITE copy untouched by any bug), `CV_FELDOLGOZO.cs` ×2 (untouched — BUG-03 is label-only),
  `mfcc/Matrix/FFT-converted` ×1 each. No path appears in two groups. Confirmed safe.

### Net
Approve the A/B/C *file partition* and the dead-method cuts. Do NOT start implementation until
#A (freeze one serialization byte-layout across both docs), #B (fix the "byte-identical" LITE
contract + reconcile BUG-01.md §3), and #C (add a feature-version staleness marker) are resolved,
and the stale BUG-09 rationale is reconciled.
