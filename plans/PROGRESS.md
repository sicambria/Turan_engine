# Turán RMS — Bug-Fix Progress, Insights & Learnings

**Run dates:** 2026-06-27 → 2026-06-28
**Branch:** `fix/roadmap-bugs`
**Process:** `.claude/workflows/plan-and-fix-roadmap.js` — plan every ROADMAP bug → peer review → adversarial review → (advisor gate) → implement in parallel by file-disjoint groups → verify → commit in logical chunks → document.
**Verification reality:** there is **no .NET/mono/msbuild toolchain** in this environment. Every change is **code-review-verified (incl. adversarial cross-reads and cross-copy diffs), not compiled or run.** Treat this branch as a reviewed proposal, not a tested release.

---

## 1. Outcome summary

| Result | Bugs |
|---|---|
| ✅ Fixed (code-review-verified) | BUG-01, 02, 04, 05, 06, 07, 08, 09, 11, 12, 16 |
| ◐ Partially addressed | BUG-03 (label-only; accuracy half open), BUG-10 (mechanism shipped, calibration deferred) |
| ✅ Done differently (per user) | BUG-15 (finished + quarantined, not deleted) |
| ⏸ Deferred | BUG-13, BUG-14, BUG-10-calibration |

Commits (logical chunks): `fix(asr-core)` (Group A + TRMS reader) → `fix(dsp)` (Group B + TRMS writer) → `feat(mfcc)` (Group C) → this docs commit. Two earlier scaffolding commits hold the audit report + ROADMAP and the workflow.

---

## 2. How the work was parallelized (and why)

The source has **duplicated files** (`H_FELDOLGOZO.cs` ×2, `dtwApp_match.cs` ×2, `HTK_Interface.cs` ×3, `lpcData.cs` ×2). Naively parallelizing **by bug** or **by severity** would put concurrent agents in the same physical file. The coordinator instead partitioned into **file-disjoint groups**:

- **Group A** (`dtwApp_match.cs` ×2, `Engine.cs`, `HTK_Interface.cs` ×3): BUG-01, 08, 09, 10, 11.
- **Group B** (`H_FELDOLGOZO.cs` ×2, `Creator.cs`, LITE `Form1.cs`): BUG-02, 03, 04, 05, 06, 07, 16.
- **Group C** (`mfcc.cs`, `Matrix.cs`, `FFT-converted.cs`): BUG-15.
- **Sequential S1** (`Engine.cs` + `Creator.cs` + LITE `Form1.cs`): BUG-12 — **after** A & B, because it shares files with both.

Each group's agent applied its bugs in a fixed internal order and mirrored edits byte-identically across duplicated copies. Commits are **sequential** (parallel `git` would race the index).

---

## 3. Errors & incidents encountered

1. **Two workflow runs died at session boundaries.** The first plan-stage run was checkpointed and could not auto-resume; a second was stopped manually after it hung on the high-effort adversarial agent (artifacts were already written). Recovered by resuming with `resumeFromRunId` (cached agents) and, when cross-session caching missed, re-running the 16 plan agents.
2. **`args` did not reach a dynamic workflow script** (`args.bugs` arrived empty → 0 agents). Fix: hard-code the list in the script instead of relying on `args` plumbing.
3. **One implementation agent stalled mid-stream** (`revise:BUG-01`, API error). Recovered by resuming; the other four re-reviews returned cached.
4. **A planted landmine in the BUG-01 plan** ("set width to literal 15 on the turan branch") was caught by the re-reviewer and corrected to `GetLength(1)` **before** any code was written — it would have crashed the 12-wide native-LPC path.
5. **Named workflow not in the session registry** (loaded at startup) — ran by `scriptPath` instead.

---

## 4. Insights about this codebase (non-obvious, worth keeping)

- **Three independent width statics, not one.** `Engine.mfcc_lpc_vect_num` (read by `Turan_core` DTW + `lpcData`) and `H_FELDOLGOZO.mfcc_lpc_vect_num` (read by the LITE DTW + `CV_FELDOLGOZO`) are *separate* globals. `dtwApp_match.num_of_vectoritems` is a third, snapshotted at **class load** — a stale-cache trap. The fix routes every live width read to the per-array `GetLength(1)`.
- **`lpcData.getRowLength()` divides by the mutable width global** → the DTW matrix bounds ride on that global matching the array's true column count. With arrays now 15→60 wide, a stale/unpinned global would cause catastrophic OOB (was benign in the old 15-only world). The per-call pin protects this **but is not thread-safe** (Form1 recognizes on a worker thread) — flagged for BUG-14.
- **Dead code masqueraded as the implementation.** `mfccszamitas` (the DCT) and `hasonlit` (a second DTW) had **zero callers**; `mfcc.cs`/`Matrix.cs`/`FFT-converted.cs` were an unfinished Java port not in any build. The live native "MFCC" path actually emits **log-mel** (no DCT). Distinguishing dead from live (caller greps) was essential to scoring and scoping.
- **`AudioPreProcessor.cs` is an empty stub** — discovered while finishing `mfcc.cs`; its streaming overload can't be wired yet (left as a single `// TODO`).
- **`AbsDistance` hardcodes `i < 13`** and would throw on a 12-wide LPC frame, but `distanceType` is hardwired `"Euclidean"` so it's unreachable. The `byte` width cast wraps if `4*N > 255` (N≥64) — a pre-existing design ceiling.

---

## 5. Learnings for the next run

- **Plan first, adversarially, before touching code.** The adversarial pre-implementation pass paid for itself: it caught a shared-file race map, a false cross-plan premise (BUG-09 assumed BUG-01 set the static to 60 — it doesn't), a triple-edit collision on one dead method, and the BUG-01 landmine. None would have been visible from per-bug plans alone.
- **Partition parallel work by file, never by bug, when files are duplicated.** And keep any byte-exact shared contract (here: the TRMS format) in **one atomic step**, never split writer/reader across *parallel* agents.
- **Deletion-first ordering** inside a group avoids editing text another bug removes (BUG-16 deletes `hasonlit` before BUG-08 would have touched it; the dead-method edits were dropped).
- **Without a compiler, verify by cross-copy `diff` + adversarial re-reads of the highest-risk contract** (the width contract here), and **say so plainly** in every artifact. Quarantining unverifiable code (the MFCC port) out of the build makes "not compiler-verified" safe.
- **Backward-compat is a feature decision.** Replacing `BinaryFormatter` would have bricked existing templates; the TRMS reader keeps a legacy fallback and adds a `featVersion` so stale templates are *detectable* rather than silently mis-recognized.

---

## 6. Required follow-ups (gates to production)

1. **Build it.** Open in VS2013+/modern .NET and resolve any compile errors (esp. the new `Matrix`/`FFT` in the quarantine folder if ever re-included, and the `CS0414` unused-field warning on `num_of_vectoritems`).
2. **Regenerate all native `.mfcc` templates** (BUG-02 changed feature content); the `featVersion` marker now warns on stale ones.
3. **Calibrate BUG-10's `RejectionThreshold`** on real cost-scale data; until then there is **no input rejection** — not production-safe for command/medical use. Treat negative/sentinel `totalCost` (unreached DTW corner) as a non-match during calibration.
4. **BUG-13 (HTK)** and **BUG-14 (de-dup → shared `Turan.Dsp` lib, fixes the width-global thread-safety)**: do once a build/run environment exists. BUG-14 Phase A should reconcile the duplicated copies these fixes just edited.
5. **BUG-03 accuracy half**: decide true-DCT vs. normalized/Mahalanobis DTW for the native path (only the naming was fixed here).

---

## 7. Architectural reminder

Per `reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md`: these fixes **stabilize the bridge** (as-built engine ~44 → toward the ~58/100 ceiling of this DTW-template architecture). They do **not** reach ≥99% accuracy or fix voice-drift — that needs the architectural move to a self-trained multilingual backbone (closed-set command classifier). The finished `reference/unused-native-mfcc/` port is a stepping stone toward an HTK-free native path, not a substitute for that move.
