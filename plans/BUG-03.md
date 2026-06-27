# FIX PLAN — BUG-03

**Title:** Native "MFCC" mode emits log-mel filterbank energies (DCT never applied), mislabeled
**Severity:** P0 (naming trap + suboptimal-but-workable live features; not a crash)
**Status of this plan:** decision + surgical change specified; **no source edited yet**.

**Plan scope per `plans/_grouping.json`:** BUG-03 is in **Group B (B-dsp-features)**, internal
order `16 → 04 → 02 → 05 → 06 → 03 → 07` (so BUG-04's `Math.Sqrt(2.0/24.0)` fix lands **before**
this comment-only change). Group B owns exactly **4 files**:
`Turan_creator/.../H_FELDOLGOZO.cs`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs`,
`Turan_creator/.../Creator.cs`, `Felismero_motor_LITE/.../Form1.cs`. Per grouping, BUG-03 is
**LABEL-ONLY** (comments / XML-doc, **zero executable change**); it does **not** touch
`Turan_core/Engine.cs` (that file belongs to Group A / sequential S1), introduces **no `BUG-17`
token** and **no ROADMAP.md edit** (the FFT-discarded defect is described inline), and **stays
OPEN / "partially addressed (naming trap only)"** — the accuracy half (true MFCC, or
normalized/Mahalanobis DTW) remains tracked, not closed by this diff.

---

## 1. Root cause, restated from the code

The native (non-HTK) feature path labels its output `.mfcc` / `EngineMode.mfcc`, but the
output is **not** MFCC. Tracing the live path:

**Creator (template creation) — `Turan_creator/Turan_creator/Creator.cs:152-167`:**
```
win_pcmdata   = create_window(...)                       // framing
mfcc_lpc_vect_num = 15
win_hammingdata = win_fir_hamming(win_pcmdata, ...)      // Hamming window (pre-emph broken, BUG-02)
win_fftdata     = FFTCalcFrameByFrame(win_hammingdata)   // (*) computed then DISCARDED
win_meldata     = new double[frames, 15]
init_mel_filter_banks()
Window_Mel_Scale_Reduction(win_hammingdata, ref win_meldata, frames)   // (*) note: HAMMING, not FFT
```
`win_meldata` (15 log-mel band values per frame) is then serialized as `.mfcc`
(`Creator.cs:191`). The engine side is symmetric: `Felismero_motor_LITE/Felismero_motor/Form1.cs:678-688`
computes the identical `win_meldata` and serializes `.mfcc` (`Form1.cs:725`).

**The DCT exists but is dead.** `H_FELDOLGOZO.mfccszamitas` (the cepstral DCT) is the only
place a DCT is implemented, and it has **zero callers** anywhere in the repo
(verified: `grep -rn "mfccszamitas"` returns only the two definitions). Therefore the
on-disk "MFCC" feature is log-mel filterbank energies with **no cepstral DCT** → dimensions
remain highly correlated, which is suboptimal for the plain Euclidean DTW used at
`dtwApp_match.cs:232 EuclideanDistance` over `num_of_vectoritems = mfcc_lpc_vect_num`.

**The dead `mfccszamitas` is, moreover, structurally incompatible with the real mel output**
(so it cannot simply be "wired in"). At `Turan_creator/.../H_FELDOLGOZO.cs:222-275` and the
identical `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198-251` it:
- takes `ref uint[,] osszegek` (integer mel sums), but the live mel reduction emits
  `double[,] win_meldata`;
- iterates **24** input bands (`for i = 0..23`), but `Window_Mel_Scale_Reduction` emits only
  **15** bands (`FEAT_VEC_SIZE = mfcc_lpc_vect_num = 15`, `CV_FELDOLGOZO.cs:150-191`);
- writes into `mfccarr[256,256]` with **1-based** output index `m = 1..mfcc_lpc_vect_num`
  (column 0 never written), not `[frames, vectsize]`;
- had the integer-division scale bug `Math.Sqrt(2/24)` → `Math.Sqrt(0)` → every coefficient ×0
  (line `:272` creator / `:248` LITE). This is **BUG-04, fixed earlier in Group B** (order
  `… → 04 → … → 03`), so by the time this banner lands the scale bug is gone — but the *structural*
  mismatches below (type / band-count / shape) still make `mfccszamitas` unusable as-is.

**Related DSP defect (described inline; deliberately NOT given a bug ID).**
At `Creator.cs:160/166` and `Form1.cs:682/688`, `win_fftdata` is computed and then **thrown
away**; the **time-domain** `win_hammingdata` is passed to `Window_Mel_Scale_Reduction` as
`power_spec`. So the mel bands are sums of *Hamming-windowed PCM samples*, not of an FFT power
spectrum. Consequence for BUG-03: the stored feature is not even an honest "log-mel" — it is a
**malformed mel feature (spectrum step bypassed)**. This is a *fourth, independent* blocker to
"just enable the DCT": a DCT of a non-spectral quantity is meaningless. Per `plans/_grouping.json`
this defect is described **inline only** — no new `BUG-17` token and no ROADMAP.md edit are
introduced by this change. It is **out of scope for this label-only BUG-03 change** but gates the
true-MFCC alternative (§6).

---

## 2. Decision

**Chosen change = ROADMAP option (b), label-only form: tell the truth in code, change no math
and no on-disk format.**

Rationale (why NOT option (a) "wire in the DCT" as the immediate fix):
1. `mfccszamitas` is incompatible on three axes (type/band-count/shape) → (a) requires writing
   a *new* DCT, not enabling the dead one.
2. (a) depends on the DCT scale being correct (BUG-04) — that fix lands earlier in Group B, but
   (a) needs more than it (see point 1).
3. (a) silently breaks **every existing `.mfcc` template on disk**: `Form1.cs:569`
   `Directory.GetFiles(..., "*.mfcc")` loads stored templates and DTW-compares them against
   freshly extracted live features; if extraction starts emitting DCT-MFCC while stored
   templates are log-mel, the two become incommensurable → recognition silently degrades until
   every command is re-recorded.
4. The feature fed to mel reduction is time-domain, not spectral (the FFT output is discarded —
   the inline-described defect in §1), so a DCT would be applied to a malformed input anyway.

Option (b) (label-only) keeps recognition behavior bit-identical and the `.mfcc` format
unchanged → zero backward-compat impact → the minimal, well-justified P0 response: remove the
"naming trap" and document the real semantics. The true-MFCC route (option (a)) is specified in
full in §6 as a decision-ready, sequenced alternative for the human reviewer.

**Explicitly deferred:** the "switch DTW to a normalized/Mahalanobis distance" half of ROADMAP
option (b). That is a real behavior change (needs per-dimension variance / covariance
estimation across the template set) and is out of scope for a surgical label fix. Deferred with
reason, not omitted.

---

## 3. Exact changes per file (before / after)

All edits are comments / XML-doc / dead-code banners. **No executable statement changes.**
Do **not** rename the `.mfcc` extension and do **not** rename the `EngineMode.mfcc` enum member
(both are load-bearing — see §4).

### 3a. `Turan_creator/Turan_creator/Creator.cs`

At the `mfcc` extraction block (currently `Creator.cs:152-167`), document the real semantics and
the two known omissions, immediately after the `else if (engine_mode == EngineMode.mfcc)` line.

Before:
```csharp
                    else if (engine_mode == EngineMode.mfcc)
                    {
                        win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);
```
After:
```csharp
                    else if (engine_mode == EngineMode.mfcc)
                    {
                        // NOTE (BUG-03): "mfcc" here is a misnomer kept for on-disk/API
                        // compatibility. The serialized .mfcc file holds 15 LOG-MEL filterbank
                        // values per frame, NOT cepstra: no DCT is applied (H_FELDOLGOZO.mfccszamitas
                        // is dead code). Additionally win_fftdata below is computed but unused;
                        // Window_Mel_Scale_Reduction is fed time-domain win_hammingdata, so the mel
                        // bands are not from an FFT power spectrum (a separate, still-open DSP defect).
                        // Creator and Engine extract identically, so the system stays internally
                        // consistent. To produce TRUE MFCC see plans/BUG-03.md §6 (BREAKS existing
                        // .mfcc templates).
                        win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);
```

### 3b. `Turan_creator/Turan_creator/Creator.cs` — enum (currently `:76-80`)

Before:
```csharp
        public enum EngineMode
        {
            lpc,
            mfcc
        }
```
After:
```csharp
        public enum EngineMode
        {
            lpc,
            mfcc   // BUG-03: produces LOG-MEL filterbank features (no DCT), not true MFCC. Name/extension kept for compat.
        }
```

### 3c. `Felismero_motor_LITE/Felismero_motor/Form1.cs` — extraction block (currently `:678-688`)

Before:
```csharp
            else if (engine_mode == EngineMode.mfcc)
            {
                win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);
```
After:
```csharp
            else if (engine_mode == EngineMode.mfcc)
            {
                // NOTE (BUG-03): "mfcc" is a misnomer kept for compat. .mfcc holds 15 LOG-MEL
                // filterbank values/frame, no DCT (H_FELDOLGOZO.mfccszamitas is dead). win_fftdata
                // below is unused; Window_Mel_Scale_Reduction is fed time-domain win_hammingdata
                // (FFT power spectrum is computed but discarded — a separate, still-open DSP defect).
                // Must stay byte-identical to Creator.cs extraction. See plans/BUG-03.md.
                win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);
```

### 3d. `Felismero_motor_LITE/Felismero_motor/Form1.cs` — enum (currently `:83-87`)

Before:
```csharp
        public enum EngineMode
        {
            mfcc,
            lpc
        }
```
After:
```csharp
        public enum EngineMode
        {
            mfcc,  // BUG-03: LOG-MEL filterbank features (no DCT), not true MFCC. Name/extension kept for compat.
            lpc
        }
```

### 3e. `H_FELDOLGOZO.cs` — dead-code banner on `mfccszamitas` (BOTH copies, see §4)

Replace the existing XML-doc summary above `public static void mfccszamitas(...)`.

Before (creator `:217-222`, identical in LITE `:193-198`):
```csharp
        /// <summary>
        /// DCT végrehajtása, az mfccarr (globális) tömböt tölti fel
        /// </summary>
        /// <param name="osszegek">The osszegek.</param>
        /// <param name="dbszam">The dbszam.</param>
        public static void mfccszamitas(ref uint[,] osszegek, int dbszam)        //ushort dbszam
```
After:
```csharp
        /// <summary>
        /// DCT végrehajtása, az mfccarr (globális) tömböt tölti fel
        /// DEAD CODE (BUG-03): not called from anywhere. DO NOT wire in as-is — it is incompatible
        /// with the live mel output: expects uint[,] (live = double[,]), 24 input bands (live = 15),
        /// and writes mfccarr 1-based into a [num_items_in_windowed_frame, num_items_in_windowed_frame]
        /// (~256x256) buffer (live frame layout differs). A correct DCT must be written fresh; see
        /// plans/BUG-03.md §6.
        /// </summary>
        /// <param name="osszegek">The osszegek.</param>
        /// <param name="dbszam">The dbszam.</param>
        public static void mfccszamitas(ref uint[,] osszegek, int dbszam)        //ushort dbszam
```

> The companion `Turan_core/Turan_core/Engine.cs` enum (`:39-44`, members `mfcc, lpc, framenum`)
> has its own `mfcc` member. The peer review's non-blocking note suggested adding the same one-line
> comment there for parity, but **`plans/_grouping.json` overrides that suggestion**: Group B owns
> exactly its 4 files and `Engine.cs` belongs to Group A / sequential S1, so the Engine.cs enum
> comment is **deliberately DROPPED** (not forgotten) to keep this change inside Group B's file set.
> Accepting one of three `EngineMode` enums lacking the comment is the intentional trade-off.

---

## 4. Every duplicated copy that needs the same change

| Logical change | Files (apply to ALL listed) |
|---|---|
| `mfccszamitas` dead-code banner (§3e) | `Turan_creator/Turan_creator/H_FELDOLGOZO.cs:217-222`, `Felismero_motor_LITE/Felismero_motor/H_FELDOLGOZO.cs:193-198` — **byte-identical bodies**, must both get the banner. Banner edits the XML-doc above the method; BUG-04 (applied earlier in Group B) edits the `Math.Sqrt(2.0/24.0)` line inside it (`:272`/`:248`) — different lines, no conflict. `mfccszamitas` is **not** deleted (only `hasonlit` is, by BUG-16), so both edits land |
| Extraction-site NOTE (§3a / §3c) | `Turan_creator/Turan_creator/Creator.cs:152` (write side) and `Felismero_motor_LITE/Felismero_motor/Form1.cs:678` (read/recognize side) |
| Enum member comment (§3b / §3d) | `Turan_creator/.../Creator.cs:76`, `Felismero_motor_LITE/.../Form1.cs:83`. `Turan_core/.../Engine.cs:39` is **DROPPED** per `plans/_grouping.json` (out of Group B's file set) |

`CV_FELDOLGOZO.cs` exists in two copies (`Turan_creator`, `Felismero_motor_LITE`) and is
relevant for §6 and the inline-described FFT-discarded defect, but needs **no edit** for this
label-only fix.

---

## 5. Backward-compat / on-disk format impact

**This (label-only) change: NONE.** No executable line changes, so:
- `.mfcc` serialized payload (BinaryFormatter `double[,]`, width 15) is unchanged.
- Existing recorded templates remain valid and commensurable with live extraction.
- `Directory.GetFiles(..., "*.mfcc")` (`Form1.cs:569`), the `ofd_load_mfcc` filter
  (`Form1.Designer.cs:432`), and all `EngineMode.mfcc` callers (`Engine.cs`,
  `Turan_tester/Form1.cs:106`, both front ends) are untouched.

**Why we did NOT rename to `logmel`:** renaming the `.mfcc` extension would break the glob,
the open-file dialog filter, and all existing template filenames; renaming the
`EngineMode.mfcc` enum member would break every caller. Truth-in-labeling is therefore done via
comments/XML-doc, not identifiers — preserving compatibility is the whole point.

**If option (a) is later chosen (see §6): it BREAKS on-disk compatibility.** Stored `.mfcc`
templates are log-mel; new extraction would emit DCT-MFCC. Mitigations, pick one:
- **Forced re-record:** bump a feature-version constant and require re-recording all command
  templates (simplest; matches a speaker-dependent system that already re-trains per user).
- **Versioned read:** prepend a small version/header tag to the serialized blob (or write a
  sidecar) so `DeSerializeArray` can detect log-mel-v1 vs mfcc-v2 and refuse/recompute rather
  than silently mixing. This must be coordinated with BUG-12 (BinaryFormatter replacement),
  which is the natural place to introduce a header.

---

## 6. Decision-ready alternative — option (a), TRUE MFCC (specified, not executed)

Only pursue after the human accepts the compat break. Prerequisites and steps:
1. **DSP prerequisites:** the DCT scale bug (BUG-04, `Math.Sqrt(2.0/24.0)`) is **already fixed in
   Group B**, so it is no longer a blocker. The remaining DSP prerequisite is the inline-described
   FFT-discarded defect: feed `win_fftdata` (a real power spectrum) into
   `Window_Mel_Scale_Reduction` instead of `win_hammingdata` — a DCT of a non-spectral feature is
   meaningless.
2. **Write a NEW DCT** (do not resurrect `mfccszamitas`): input `double[,] win_meldata`
   `[frames, 15]`, output `double[,]` `[frames, C]` of cepstral coefficients
   `c[k] = scale * Σ_{n=0..N-1} mel[n] * cos(π·k·(n+0.5)/N)`, with `N = 15`,
   `scale = sqrt(2.0/N)`, `k = 0..C-1` (decide whether to keep `c0` energy). Use 0-based,
   `[frames, C]` layout matching the rest of the pipeline.
3. **Apply identically at all four sites** — both `H_FELDOLGOZO` copies (the new function) and
   both call sites (`Creator.cs` write path, `Form1.cs` recognize path) — or creator-written
   templates and engine-extracted live vectors will disagree.
4. **Set the chosen `C` consistently across all THREE per-module width statics** (see §7 — there
   is no single field): `Engine.mfcc_lpc_vect_num` (Turan_core) and both
   `H_FELDOLGOZO.mfcc_lpc_vect_num` copies (Turan_creator + LITE). They gate serialized width, the
   `EuclideanDistance` loop length (`dtwApp_match.cs:235`, bound cached at `:84`), and array sizing.
5. **Compat:** versioned read or forced re-record per §5.

---

## 7. Shared contracts other bug fixes depend on

- **`Creator.SerializeArray` (`Creator.cs:318`) ↔ `Engine.DeSerializeArray` (`Engine.cs:162`)**
  and the engine front-end's own `SerializeArray`/loader: serialized `double[,]`, semantics =
  "feature vector per frame". Creator-write and Engine-read must agree on **both width and
  feature semantics**. Option (a) would change the *semantics* (and possibly width) on this same
  channel — the very channel BUG-01 (60-dim Δ/ΔΔ/ΔΔΔ concat) and BUG-12 (TRMS / replace
  BinaryFormatter) also modify. Per `plans/_grouping.json` the TRMS format stores **both dims**
  (`rows`, `cols`) so it is width-agnostic, plus a `featVersion` byte so BUG-02's feature change is
  detectable. Coordinate any width/semantics/version change across BUG-01, BUG-03(a), BUG-12.
- **DTW width is NOT a single static — and is NOT a compile-time constant.** Per the grouping
  **WIDTH CONTRACT**, the authoritative width is the **per-array width via `data.GetLength(1)`**,
  i.e. the *live* value at match time — **NOT** a compile-time 15 and **NOT** a hard-bumped static.
  The relevant live values live in **three independent per-module statics** that must be set in
  lockstep — and they **do NOT all share one default**: `Engine`'s is 15, both `H_FELDOLGOZO`
  copies default to **12** (the LPC width, per the field's own comment
  `// 15 MFCC vector, 0-14; 12 LPC vector, 0-11`). These are **mode-shared** statics reassigned at
  runtime — the native mfcc path sets `H_FELDOLGOZO.mfcc_lpc_vect_num = 15` (`Creator.cs:156`, LITE
  writer) before extraction — so any reader that snapshots the value at **class load** captures the
  raw **12** default unless a runtime set preceded first access. This is precisely why the WIDTH
  CONTRACT mandates the *live* per-array `data.GetLength(1)`, not any static:
  - `Engine.mfcc_lpc_vect_num` (`Turan_core/Engine.cs:29`, **default 15**) — read by
    `Turan_core/dtwApp_match.cs:84` (`num_of_vectoritems = Engine.mfcc_lpc_vect_num`) and
    `Turan_core/lpcData.cs:47`.
  - `H_FELDOLGOZO.mfcc_lpc_vect_num` (**Turan_creator** copy, `H_FELDOLGOZO.cs:36`, **default 12**) —
    read by `Turan_creator/CV_FELDOLGOZO.cs:47` (`VECSIZE`/`FEAT_VEC_SIZE`, **class-load snapshot**);
    set to 15 in `Creator.cs:156` (mfcc path).
  - `H_FELDOLGOZO.mfcc_lpc_vect_num` (**Felismero_motor_LITE** copy, `H_FELDOLGOZO.cs:36`, **default 12**)
    — read by `Felismero_motor_LITE/dtwApp_match.cs:84` (`= H_FELDOLGOZO.mfcc_lpc_vect_num`, **not**
    `Engine.`, **class-load snapshot**) and `Felismero_motor_LITE/CV_FELDOLGOZO.cs:47`.
  Consequently the two DTW engines' cached bounds differ **by default**: Turan_core caches 15,
  LITE caches 12 — they are not all 15 under any reading. (For the native mfcc feature itself the
  effective mel width is 15: `Creator.cs:156` sets the static to 15 and `:162` allocates
  `win_meldata` 15-wide before `Window_Mel_Scale_Reduction` runs — see §3e's "24 vs 15".)
  So the **LITE DTW keys off `H_FELDOLGOZO`'s copy while Turan_core's DTW keys off `Engine`'s copy**;
  a fix that bumps only one leaves the other engine's width untouched. The
  `num_of_vectoritems = …` initializer at `dtwApp_match.cs:84` is captured at **class load**
  (documented hazard) while `EuclideanDistance` (`:235`) loops on that cached copy — BUG-01 must
  reconcile read-sites to the live per-array width. BUG-09 (`magic13` Itakura order) and BUG-08
  (caps) also key off vector width: derive it from the array / live value, not a hard-bumped static.

---

## 8. Self-verification without a compiler

1. **Confirm DCT is dead:** `grep -rn "mfccszamitas" --include=*.cs` → exactly two hits, both
   `public static void` *definitions*, zero call sites. (Done.)
2. **Confirm the live feature is log-mel, written then read symmetrically:** trace
   `Creator.cs:152→166→191` (write `win_meldata` → `.mfcc`) and
   `Form1.cs:678→688→725` (engine writes identical `win_meldata`) and `Form1.cs:569`
   (`*.mfcc` templates loaded for DTW). Both call `Window_Mel_Scale_Reduction`, whose body
   (`CV_FELDOLGOZO.cs:179`) ends at `Math.Log(...)` with no cosine/DCT term.
3. **Confirm the FFT-discarded defect (described inline, no bug ID):** read `Creator.cs:160` vs
   `:166` and `Form1.cs:682` vs `:688` — `win_fftdata` is assigned then never used;
   `win_hammingdata` is the argument.
4. **Confirm incompatibility claims about `mfccszamitas`:** signature `ref uint[,]`
   (H_FELDOLGOZO.cs:222/198); input loop `i = 0..23` (24 bands) vs `FEAT_VEC_SIZE = 15`
   (`CV_FELDOLGOZO.cs:47-48,150`); output `mfccarr = new double[num_items_in_windowed_frame,
   num_items_in_windowed_frame]` (~256×256), index `m = 1..15`.
5. **Verify the edit is behavior-neutral:** the diff must contain only comment/`///` lines and
   the dead-code banner — `git diff` should show **no change to any executable statement**.
   Re-read each hunk to confirm no token outside a comment moved.
6. **Verify duplicate parity:** `diff` the two `mfccszamitas` regions and the two extraction
   blocks after editing to confirm the banners/notes match across copies (prevents drift,
   BUG-14).

---

## Peer review

**Reviewer verdict: changes requested (approved = false).** The root-cause analysis is correct
and well-evidenced, the chosen label-only change is safe, and all duplicated copies are covered.
Three problems below should be fixed before implementation. None require a rewrite — the core
decision and all the before/after edit hunks are sound.

### What I independently verified against the real source (all confirmed)
- **DCT is dead:** `grep -rn mfccszamitas` → exactly 2 hits, both `public static void`
  *definitions* (`Turan_creator/.../H_FELDOLGOZO.cs:222`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198`),
  zero call sites. ✓
- **Live feature is log-mel, no DCT:** `CV_FELDOLGOZO.Window_Mel_Scale_Reduction` body ends at
  `result[frame,i] = Math.Log(...)` (creator `CV_FELDOLGOZO.cs:179`); no cosine term anywhere. ✓
- **BUG-17 (FFT discarded):** `Creator.cs:160` assigns `win_fftdata`, `:166` passes
  `win_hammingdata`; `Form1.cs:682` assigns `win_fftdata`, `:688` passes `win_hammingdata`. ✓
- **`mfccszamitas` incompatibility:** `ref uint[,]` signature; `i = 0..23` (24 bands) vs
  `FEAT_VEC_SIZE = 15`; `mfccarr = new double[256,256]` written 1-based `m = 1..15` (col 0 never
  written). ✓
- **BUG-04 line numbers:** `Math.Sqrt(2/24)` is at creator `H_FELDOLGOZO.cs:272` and LITE `:248`.
  The plan's §1 numbers are correct here (and more accurate than ROADMAP, which mislabels the
  creator line as `:248`). ✓
- **All edit anchors are byte-exact** against the current files: Creator.cs `152-154` and enum
  `76-80`; Form1.cs `678-680` and enum `83-87` (order `mfcc, lpc`); both `mfccszamitas` XML-doc
  summaries. Using before/after text anchors (not raw line numbers) makes the edits robust to the
  minor line drift elsewhere in the narrative. ✓
- **Duplicate enumeration is complete:** `H_FELDOLGOZO.cs` ×2, `Creator.cs` ×1, `EngineMode`
  enum ×3 (Creator, Engine.cs, Form1) — all accounted for; the two `Window_Mel_Scale_Reduction`
  call sites (`Creator.cs`, `Form1.cs`) and all `.mfcc` read/write/dialog sites match §5. ✓

### Issue 1 (blocking) — §7 shared-contract claim about `mfcc_lpc_vect_num` is factually wrong
§7 states `mfcc_lpc_vect_num` "is a shared global ... keep it the single source of truth." It is
**not** a single field. There are **three independent statics**, one per module:
- `Engine.mfcc_lpc_vect_num` (`Turan_core/Engine.cs:29`) — read by `Turan_core/dtwApp_match.cs:84`
  and `lpcData.cs:47`.
- `H_FELDOLGOZO.mfcc_lpc_vect_num` (Turan_creator) — set in `Creator.cs:142/156`, read by
  `Turan_creator/CV_FELDOLGOZO.cs:47`.
- `H_FELDOLGOZO.mfcc_lpc_vect_num` (Felismero_motor_LITE) — read by
  `Felismero_motor_LITE/dtwApp_match.cs:84` (`H_FELDOLGOZO.mfcc_lpc_vect_num`, **not** `Engine.`)
  and `CV_FELDOLGOZO.cs:47`.

They merely happen to all equal 15 today. This is exactly the kind of cross-module contract the
plan says BUG-01 (width → 60) and BUG-09 (`magic13`) must coordinate on — and getting it wrong in
the contract section would mislead those fixes (e.g. BUG-01 bumping only `Engine.mfcc_lpc_vect_num`
would leave the LITE engine's DTW width untouched). **Required:** correct §7 to state these are
three separate per-module statics that must be changed in lockstep, and that the LITE DTW keys off
`H_FELDOLGOZO`'s copy while Turan_core's DTW keys off `Engine`'s.

### Issue 2 (blocking) — source comments reference a bug ID ("BUG-17") not in the authoritative list
The §3a/§3c NOTE text and §1 introduce `BUG-17`, which does **not** exist in ROADMAP.md (the stated
authoritative list ends at BUG-16). Committing comments that cite `BUG-17` creates a dangling
reference in shipped source. **Required:** either (a) add BUG-17 to ROADMAP.md as part of this
change set (the plan already says "promote to visible status" but no ROADMAP edit is included in
§3/§4), or (b) drop the `BUG-17` token from the source comments and describe the defect inline
("FFT power spectrum is computed but unused; mel reduction is fed time-domain Hamming data"). Pick
one; do not leave an undefined ID in the code.

### Issue 3 (should-fix) — the change must not be recorded as "BUG-03 resolved"
The chosen edit changes **zero executable statements**, so the substantive half of BUG-03 — log-mel
dimensions are highly correlated and suboptimal under the plain Euclidean DTW (`dtwApp_match.cs:230
EuclideanDistance`) — is **not fixed**, only documented. The plan discloses this honestly (§2, §4),
which is good, but the workflow must not close BUG-03 on the strength of a comment-only diff.
**Required:** state explicitly that BUG-03 remains OPEN / "partially addressed (naming trap only)"
and that the accuracy fix (option (a) true MFCC, or the full option (b) normalized/Mahalanobis DTW)
stays tracked. Otherwise the P0 is silently downgraded to done while accuracy is unchanged.

### Non-blocking notes
- Minor line-number drift in narrative prose (e.g. `EuclideanDistance` cited as `:232`/`:237`;
  actual definition `:230`, loop `:235`). Harmless because edits are text-anchored, but tidy if
  convenient.
- §3e optional Engine.cs enum comment: leaving it out is fine for a label-only change, but for full
  truth-in-labeling parity across all three `EngineMode` definitions, adding the one-liner there too
  is cheap and avoids one of three enums silently disagreeing.
- The before/after hunks correctly place the inline enum comment **after the comma**
  (`mfcc,  // ...`) for the `mfcc, lpc` order in Form1/Engine, and after the bare member for
  Creator's `lpc, mfcc` order — no syntax break. ✓

Once Issues 1–3 are addressed, the plan is sound to implement: the decision is well-justified,
backward-compat impact is genuinely nil, and no new bug is introduced (comment/`///`-only diff).

---

## Revision 2026-06-27

This revision resolves every blocking issue from the Peer review and conforms the plan to
`plans/_grouping.json` (Group B; internal order `16 → 04 → 02 → 05 → 06 → 03 → 07`; label-only,
4-file scope; no `BUG-17` token; no ROADMAP edit; BUG-03 stays OPEN). All source-comment snippets
were re-checked against the live files (`Creator.cs`, LITE `Form1.cs`, both `H_FELDOLGOZO.cs`,
both `CV_FELDOLGOZO.cs`, `Engine.cs`, `dtwApp_match.cs` ×2, `lpcData.cs`) before editing.

**Closes Issue 1 (blocking — §7 `mfcc_lpc_vect_num` factually wrong + WIDTH-CONTRACT non-conformance).**
- Rewrote the §7 width bullet. Removed the false "shared global … single source of truth" claim.
  It now states there is **no single field**: three independent per-module statics —
  `Engine.mfcc_lpc_vect_num` (`Turan_core/Engine.cs:29`, read by `Turan_core/dtwApp_match.cs:84` +
  `lpcData.cs:47`), `H_FELDOLGOZO.mfcc_lpc_vect_num` (Turan_creator, read by
  `CV_FELDOLGOZO.cs:47`), and `H_FELDOLGOZO.mfcc_lpc_vect_num` (LITE, read by LITE
  `dtwApp_match.cs:84` + `CV_FELDOLGOZO.cs:47`) — verified by grep. Documents that the **LITE DTW
  keys off `H_FELDOLGOZO`'s copy while Turan_core's DTW keys off `Engine`'s copy**.
- Conformance (per advisor): the bullet now also states the grouping **WIDTH CONTRACT** — width is
  the per-array `data.GetLength(1)` / live value at match time, NOT a compile-time 15 and NOT a
  hard-bumped static — and flags the class-load cache at `dtwApp_match.cs:84` as the documented
  hazard. Removed the stale "width = `H_FELDOLGOZO.mfcc_lpc_vect_num` (15)" assertion from the
  serialization bullet and aligned it with the TRMS both-dims + `featVersion` contract. §6 step 4
  updated to "set all THREE statics in lockstep" with the corrected loop line `:235`.

**Closes Issue 2 (blocking — `BUG-17` is a dangling reference).**
- Removed the `BUG-17` token from **every source-comment snippet** (these are what get committed):
  the §3a Creator NOTE, the §3c LITE Form1 NOTE, and the §3e `mfccszamitas` banner now describe the
  defect inline ("FFT power spectrum is computed but discarded; mel reduction is fed time-domain
  Hamming data — a separate, still-open DSP defect"). Per grouping, **no ROADMAP.md edit** is made.
- Removed `BUG-17` from prose too: §1 ("promote to visible status / proposed BUG-17" → "described
  inline, no bug ID"), §2 point 4, §4 (`CV_FELDOLGOZO` note), §6 step 1, §8 step 3. The only
  remaining `BUG-17` mentions in the plan body are explicit statements that the token is **not**
  introduced (§ Status, §1); occurrences inside the Peer review section are the untouched record.
- `grep -n "BUG-17" plans/BUG-03.md` outside the Peer review section yields only those two
  "no BUG-17 token" meta-statements; zero appear in any before/after source snippet.

**Closes Issue 3 (should-fix — must not be recorded as "BUG-03 resolved").**
- Added an explicit status line (top of file) stating BUG-03 is **LABEL-ONLY**, **zero executable
  change**, and **stays OPEN / "partially addressed (naming trap only)"**; the accuracy half
  (true MFCC, or normalized/Mahalanobis DTW) remains tracked, not closed by this diff — matching
  the grouping note "BUG-03 stays OPEN/partially-addressed."

**Grouping conformance beyond the three issues.**
- **Engine.cs enum comment DROPPED.** The peer review's non-blocking note *suggested adding* the
  one-liner to `Turan_core/Engine.cs:39` for parity; `plans/_grouping.json` *mandates dropping* it
  to keep Group B inside its 4 files. §3e's note and the §4 table now record this as a deliberate,
  grouping-driven drop (not an omission), and note Engine's enum has 3 members (`mfcc, lpc, framenum`).
- **BUG-04 ordering reconciled.** Because Group B applies `04` before `03`, the §3e banner no longer
  claims `Math.Sqrt(2/24)==0` as a live defect (it is fixed by then); the banner now focuses on the
  surviving structural mismatches (`uint[,]` vs `double[,]`, 24 vs 15 bands, 1-based `[~256×256]`
  shape). §1, §2 point 2 and §6 step 1 updated to note BUG-04 is already fixed in-group, not an open
  prerequisite. §4 records that the banner (XML-doc) and BUG-04 (the `:272`/`:248` line) edit
  different lines of the same surviving method (`mfccszamitas` is **not** deleted — only `hasonlit`
  is, by BUG-16), so both edits coexist.

**Non-blocking tidies.** Corrected `EuclideanDistance` line references to `:235` (loop) / `:84`
(cached bound); replaced the literal `mfccarr = new double[256,256]` with the actual
`new double[num_items_in_windowed_frame, num_items_in_windowed_frame]` (~256×256) in the banner and
§8. All edits remain comment/`///`-only — the diff still changes **no executable statement**.

---

## Re-review 2026-06-27

**Verdict: APPROVED — safe to implement.** Re-verified independently against the live source and
`plans/_grouping.json` (not against the plan's own claims). All three previously-blocking issues are
genuinely resolved, no new defect was introduced, and the plan is consistent with Group B's scope
and the shared contracts.

### Evidence re-checked against live source (every claim confirmed)
- **DCT is dead:** `grep -rn mfccszamitas --include=*.cs` → exactly 2 hits, both `public static void`
  *definitions* (`Turan_creator/.../H_FELDOLGOZO.cs:222`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198`),
  zero call sites. ✓
- **Live feature is log-mel, no DCT:** `Window_Mel_Scale_Reduction` body ends at
  `result[frame,i] = Math.Log(...)` (`CV_FELDOLGOZO.cs:179`, both copies); no cosine term. ✓
- **FFT-discarded defect:** Creator.cs `:160` assigns `win_fftdata`, `:166` passes `win_hammingdata`;
  Form1.cs `:682` assigns `win_fftdata`, `:688` passes `win_hammingdata`. ✓
- **`mfccszamitas` incompatibility (all three axes):** signature `ref uint[,]` (live = `double[,]`);
  input loop `i = 0..23` = 24 bands (live `FEAT_VEC_SIZE = H_FELDOLGOZO.mfcc_lpc_vect_num = 15`,
  `CV_FELDOLGOZO.cs:47-48`); `mfccarr = new double[num_items_in_windowed_frame, num_items_in_windowed_frame]`
  written 1-based `m = 1..mfcc_lpc_vect_num` (`:224,233,265`, col 0 unwritten). ✓
- **BUG-04 line:** `Math.Sqrt(2/24)` at creator `:272`, LITE `:248`. Distinct from the XML-doc the
  BUG-03 banner edits (`:217-222` / `:193-198`) → no conflict; both edits land. ✓
- **All edit anchors byte-exact:** Creator.cs enum `76-80` (`lpc, mfcc`) and `mfcc` block `152-154`;
  Form1.cs enum `83-87` (`mfcc, lpc`) and block `678-680`; both `mfccszamitas` XML-doc summaries. ✓

### Blocking issues from the Peer review — all resolved
1. **§7 three-statics correction (was factually wrong):** RESOLVED. Verified
   `Engine.mfcc_lpc_vect_num` (`Engine.cs:29`) is read by `Turan_core/dtwApp_match.cs:84` and
   `lpcData.cs:47`, while LITE `dtwApp_match.cs:84` reads `H_FELDOLGOZO.mfcc_lpc_vect_num` (**not**
   `Engine.`) and both `CV_FELDOLGOZO.cs:47` read their local `H_FELDOLGOZO` copy. The plan now
   states there is no single field and that LITE DTW keys off `H_FELDOLGOZO` while Turan_core's DTW
   keys off `Engine` — matches source, and conforms to the grouping WIDTH CONTRACT
   (`data.GetLength(1)` / live value, not a bumped static). ✓
2. **Dangling `BUG-17` token:** RESOLVED. `grep -n "BUG-17" plans/BUG-03.md` returns hits only in
   meta-statements ("introduces no BUG-17 token") and the historical Peer-review/Revision sections;
   **zero** occurrences inside any before/after source-comment snippet (§3a/§3c/§3e). The FFT-discard
   is described inline with no bug ID, matching the grouping mandate. No ROADMAP.md edit. ✓
3. **Not recorded as resolved:** RESOLVED. Status line (top) and §2 declare BUG-03 LABEL-ONLY, zero
   executable change, stays OPEN / "partially addressed (naming trap only)"; the accuracy half stays
   tracked. ✓

### New-defect / contract scan
- **No executable change:** every §3 hunk is `//` or `///` only; no token outside a comment moves.
  No off-by-one, integer-division, or vector-width change is introduced (none possible in a
  comment-only diff). ✓
- **Duplicated-copy coverage exact:** touches precisely Group B's 4 files — `mfccszamitas` banner in
  BOTH `H_FELDOLGOZO.cs` copies, NOTE+enum in `Creator.cs` and LITE `Form1.cs`. `Turan_core/Engine.cs`
  enum comment is deliberately DROPPED per the grouping (keeps Group B inside its file set), so one of
  three `EngineMode` enums lacking the comment is an intentional, contract-driven trade-off, not an
  omission. ✓
- **In-group ordering safe:** BUG-03 runs last-but-one (`…→04→02→…→03→07`). After BUG-02
  (pre-emphasis in `win_fir_hamming`) and BUG-04 (`Math.Sqrt` fix) land, the banner's claims remain
  true — `win_fftdata` is still discarded and `mfccszamitas` is still uncalled — so the comments do
  not go stale. BUG-16 deletes only `hasonlit` (creator `:294`, LITE `:330`), not `mfccszamitas`, so
  the banner's host method survives. ✓
- **Group scope / contract consistency:** no Engine.cs / Creator.SerializeArray edit (those belong to
  S1/BUG-12); §7 aligns with the TRMS both-dims + `featVersion` contract; option (a) is correctly
  parked as decision-ready, not executed. ✓

### Remaining issues
None blocking. Minor non-blocking narrative residue: a few line citations describe the Turan_core
copy's values for sites that differ by ±2 lines in the LITE copy (e.g. `EuclideanDistance` loop is
LITE `:237` / core `:235`; def LITE `:232` / core `:230`). BUG-03 edits none of those files, and all
actual edits are text-anchored, so this has zero implementation impact. Optional tidy only.

---

## Revision 2026-06-27 — pass 2 (source re-verification)

Independent re-verification of **every** edit anchor and factual claim against the live `.cs`
source (not against the plan's own prior re-review, which had asserted "matches source"). All §3
before/after snippets re-confirmed byte-exact: `Creator.cs` enum `76-80` / mfcc block `152-167` /
serialize `:191`; LITE `Form1.cs` enum `83-87` / mfcc block `678-688` / serialize `:725`; both
`mfccszamitas` XML-doc summaries (`Turan_creator/H_FELDOLGOZO.cs:217-222`,
`Felismero_motor_LITE/H_FELDOLGOZO.cs:193-198`) and the `Math.Sqrt(2/24)` BUG-04 line
(creator `:272` / LITE `:248`). `grep -rn mfccszamitas` still returns exactly the two
`public static void` definitions, zero callers.

**Correction (re-opens and closes the residue of Issue 1 — §7 width-contract facts).** The prior
revision fixed the "single field → three statics" error but introduced a **new factual error** the
re-review missed: §7 claimed the three statics "merely all equal 15 today." Source disproves this:
- `Engine.mfcc_lpc_vect_num` defaults to **15** (`Turan_core/Engine.cs:29`).
- **Both** `H_FELDOLGOZO.mfcc_lpc_vect_num` copies default to **12** (`H_FELDOLGOZO.cs:36`, comment
  `// 15 MFCC vector, 0-14; 12 LPC vector, 0-11`) — they are mode-shared statics set to 15 at
  runtime only on the native mfcc path (`Creator.cs:156`, LITE writer).
- The class-load-snapshot read sites (LITE `dtwApp_match.cs:84`, both `CV_FELDOLGOZO.cs:47`) capture
  the raw **12** default unless a runtime set preceded first access, so the two DTW engines' cached
  bounds differ **by default** (Turan_core 15, LITE 12) — not all 15 under any reading.

Why this is in-scope, not a flourish: §7 is the authoritative width-contract that BUG-01/08/09 key
off; the whole purpose of blocking Issue 1 was that wrong §7 facts mislead those fixes. A BUG-01
implementer trusting "all start at 15" would bump only the 15-defaulting `Engine` static to 60 and
leave the two 12-defaulting `H_FELDOLGOZO` copies (incl. the LITE DTW's cached bound) untouched —
exactly the failure Issue 1 exists to prevent. The corrected §7 now states the per-static defaults
(15 / 12 / 12), the runtime reassignment, and the class-load-snapshot hazard, which **reinforces**
the WIDTH CONTRACT (use live `data.GetLength(1)`, never a static).

Reconciliation with §3e (no contradiction): the §3e/§1 band-count claim "live = 15" remains correct
and is about the **native mfcc feature width**, where `Creator.cs:156` sets the static to 15 and
`:162` allocates `win_meldata` 15-wide before mel reduction. The §7 "default 12" is about the raw
static default. Both are true and describe different things; §7 now says so explicitly.

**Grouping conformance re-confirmed (unchanged).** Still LABEL-ONLY / zero executable change; Group
B's exact 4 files; `Turan_core/Engine.cs` enum comment deliberately DROPPED; FFT-discard described
inline with **no `BUG-17` token** in any source snippet (verified: `grep -n BUG-17` hits only
meta-statements and the historical Peer-review/Revision/Re-review audit trail) and **no ROADMAP.md
edit**; BUG-03 stays **OPEN / "partially addressed (naming trap only)."** Internal order
`16 → 04 → 02 → 05 → 06 → 03 → 07` preserved; the §3e banner edits the XML-doc above `mfccszamitas`
while BUG-04 edits the `:272`/`:248` line inside it (distinct lines, no conflict), and `mfccszamitas`
survives BUG-16 (which deletes only `hasonlit`). This pass changed **only** the §7 narrative; no §3
before/after snippet, and therefore no committed source comment, was altered.

---

## Re-review 2026-06-27 (independent, post-pass-2)

**Verdict: APPROVED — safe to implement.** I re-read the live source and `plans/_grouping.json`
directly (not the plan's own claims) and re-verified every load-bearing fact, anchor, and contract.
The label-only change is behavior-neutral, all duplicated copies are covered, and it conforms to
Group B's scope.

### Independently re-verified against live source (each confirmed)
- **DCT dead:** `grep -rn mfccszamitas --include=*.cs` → exactly 2 hits, both `public static void`
  definitions (`Turan_creator/.../H_FELDOLGOZO.cs:222`, `Felismero_motor_LITE/.../H_FELDOLGOZO.cs:198`),
  zero callers. ✓
- **`mfccszamitas` incompatibility (all three axes), creator `:222-275`:** signature `ref uint[,]`
  (live mel = `double[,]`); input loop `i = 0..23` = 24 bands (live width 15); output
  `mfccarr = new double[num_items_in_windowed_frame, num_items_in_windowed_frame]` (`:224`) written
  1-based `m = 1..mfcc_lpc_vect_num` (`:233,265`, col 0 unwritten). ✓
- **BUG-04 line vs banner:** `Math.Sqrt(2 / 24)` at creator `:272` (LITE `:248`); banner edits the
  XML-doc summary at `:217-222` / `:193-198` — disjoint lines, no edit conflict; banner correctly
  omits the scale bug (already fixed earlier in-group). ✓
- **Live feature is log-mel, no DCT:** `Window_Mel_Scale_Reduction` tail ends at
  `result[frame,i] = Math.Log(...)` (`CV_FELDOLGOZO.cs:~179`, both copies); no cosine term. ✓
- **FFT discarded:** Creator `:160` assigns `win_fftdata`, `:166` passes `win_hammingdata`; LITE
  Form1 `:682` assigns `win_fftdata`, `:688` passes `win_hammingdata`. ✓
- **§7 three-statics + defaults (the pass-2 correction):** confirmed by grep —
  `Engine.mfcc_lpc_vect_num = 15` (`Engine.cs:29`), both `H_FELDOLGOZO.mfcc_lpc_vect_num = 12`
  (`H_FELDOLGOZO.cs:36`). Turan_core `dtwApp_match.cs:84` reads `Engine.`; LITE `dtwApp_match.cs:84`
  reads `H_FELDOLGOZO.`; both `CV_FELDOLGOZO.cs:47` and both `lpcData.cs` read their module-local
  source. Native mfcc path sets the static to 15 at runtime (Creator `:156`; LITE `Form1.cs:108`).
  The §7 narrative is now factually accurate and reinforces the WIDTH CONTRACT. ✓
- **Edit anchors byte-exact:** Creator enum `76-80` (`lpc, mfcc`), mfcc block `152-167`, serialize
  `:191`; LITE Form1 enum `83-87` (`mfcc, lpc`), mfcc block `678-688`, serialize `:725`; both
  `mfccszamitas` XML-doc summaries. The two §3 "Before" snippets correctly reflect that Creator's
  block opens with `create_window` while LITE Form1's opens with `win_fir_hamming` (real structural
  difference between the copies). ✓

### Previously-blocking issues — all genuinely resolved
1. **§7 single-field error → fixed:** now states three independent per-module statics with defaults
   15/12/12 and distinct read-sites. Matches source. ✓
2. **Dangling `BUG-17` token → fixed:** `grep -n BUG-17 plans/BUG-03.md` shows hits only in
   meta-statements ("introduces no BUG-17 token") and the historical Peer-review/Revision audit
   trail; **zero** occurrences inside any §3a/§3c/§3e before/after source snippet. FFT-discard is
   described inline with no bug ID. No ROADMAP.md edit. ✓
3. **Not recorded as resolved → fixed:** status line and §2 declare BUG-03 LABEL-ONLY / zero
   executable change / stays OPEN ("partially addressed — naming trap only"); accuracy half tracked. ✓

### New-defect / contract scan
- **No executable change possible:** every §3 hunk is `//` or `///` only. Enum inline comments are
  syntactically valid in both member orders (`mfcc   // ...` trailing member in Creator; `mfcc,  // ...`
  comma-then-comment in Form1). No off-by-one, integer-division, or vector-width change introduced. ✓
- **Duplicate coverage exact:** banner in BOTH `H_FELDOLGOZO.cs` copies; NOTE+enum comment in
  `Creator.cs` and LITE `Form1.cs` — precisely Group B's 4 files. `Turan_core/Engine.cs` enum comment
  deliberately DROPPED per the grouping (keeps Group B inside its file set); one of three `EngineMode`
  enums lacking the comment is an intentional, contract-driven trade-off. ✓
- **In-group ordering safe:** after BUG-16/04/02 land, the banner's claims stay true — `mfccszamitas`
  is uncalled and survives (BUG-16 deletes only `hasonlit`: creator `:294`, LITE `:330`, disjoint from
  `mfccszamitas`), `win_fftdata` is still discarded. Comments do not go stale. ✓
- **Scope/contract consistency:** no Engine.cs / Creator.SerializeArray edit (those belong to S1);
  §7 aligns with the TRMS both-dims + `featVersion` contract; option (a) parked decision-ready. ✓

### Remaining issues
None blocking. Only cosmetic residue: a handful of LITE line citations in narrative prose differ by
±2 lines from the Turan_core copy (e.g. `EuclideanDistance` loop). BUG-03 edits none of those files
and all real edits are text-anchored, so this has zero implementation impact. Optional tidy only.
