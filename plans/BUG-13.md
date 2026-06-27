# BUG-13 — HTK integration hard-codes a Windows path and shells out HCopy.exe per call

Severity: P3 (hygiene / portability / robustness). No recognition-accuracy impact.

Affected files (all three copies of `HTK_Interface.cs`, plus one live caller guard):
- `/home/arsvivendi/git/Turan_engine/Turan_core/Turan_core/HTK_Interface.cs`
- `/home/arsvivendi/git/Turan_engine/Turan_tester/Turan_tester/HTK_Interface.cs`
- `/home/arsvivendi/git/Turan_engine/Turan_creator/Turan_creator/HTK_Interface.cs`
- `/home/arsvivendi/git/Turan_engine/Turan_tester/Turan_tester/Form1.cs` (caller guard, §2.4)

---

## 1. Root cause (restated from the code)

`HTK_Interface.CreateMFCC_D_A_T` launches HTK's `HCopy.exe` to turn `.wav` files into
`.mfc3` HTK feature files. Three independent defects, all visible in the source:

**(a) Hard-coded, Windows-only path.**
- Core, line 15: `static string htk_cmd_dir = "\\htk\\";` — a Windows backslash path that
  resolves to `<current-drive-root>\htk\`. Used directly as the process working directory
  (line 20: `hcopy_proc.StartInfo.WorkingDirectory = htk_cmd_dir;`).
- Tester, line 15: same constant, used as `app_path + htk_cmd_dir` (line 20) → `dat\\htk\\`.
- Creator, line 15: the constant is **commented out**; working directory is just `app_path`
  (line 20). This is the only copy whose `CreateMFCC_D_A_T` is reached at runtime (see §3).

  The three copies disagree on what the working directory even means, none makes the HCopy
  location configurable, and the backslash literal is non-portable.

**(b) `FileName = "HCopy.exe"` with no path and `.exe` baked in.**
The executable name cannot be configured without editing source, and the `.exe` suffix is
Windows-specific (HTK on Linux/mono is `HCopy`, no extension).

**(c) Fire-and-forget spawn with no error capture.**
The method calls `hcopy_proc.Start()` and returns immediately:
- No `WaitForExit()` → the caller proceeds (and `Engine.ReadMFCC_D_A_T` / the creator reads
  the `.mfc3` files) **before HCopy has necessarily finished writing them** — a latent race.
- The exit code is never inspected. A failing HCopy (missing config, bad wav, license error)
  is silently ignored (Creator copy even does `catch { return; }`), so downstream code fails
  later with an opaque file/format error.

Note on the ROADMAP wording "spawns HCopy.exe per file": the active caller
(`Creator.cs:204-219`) already **batches** all wavs into one `temp.scp` and issues a single
HCopy call, so the real defects are (a) portability, (b) configurability, and (c) missing
error handling / synchronization — not per-file spawning.

---

## 2. Exact change per file (before / after)

### Design decision — keep `UseShellExecute = true` (do NOT redirect streams)

The roadmap asks to "capture stderr/exit code." There is a real trade-off here that an
earlier draft of this plan got wrong:
- `ExitCode` is available after `WaitForExit()` **regardless** of `UseShellExecute`.
- Capturing stderr **text** requires `RedirectStandardError=true`, which forces
  `UseShellExecute=false`. On .NET Framework (this is a VS2013 / 4.x project) that flip
  changes *both* (i) how the bare `FileName="HCopy.exe"` is resolved (ShellExecute + `App
  Paths` registry → `CreateProcess` search order) **and** (ii) how a *relative*
  `WorkingDirectory` (`app_path = "dat\\"`) is honored. Either can make HCopy fail to start
  or run in the wrong directory on the *live* path.

Therefore this plan **keeps the default `UseShellExecute=true`** (and the existing
`WindowStyle.Hidden`) and captures the **exit code only**. This:
- preserves today's working executable/working-dir resolution byte-for-byte on the live
  path (zero risk of "HCopy not found" / "wrong cwd" regressions);
- still fixes the two substantive defects — the read-before-write **race** (via
  `WaitForExit`) and the **silently-swallowed failure** (via the `ExitCode` check);
- sidesteps the stream-buffer-deadlock class entirely (no redirected pipes to drain).

Capturing stderr text is documented as an explicit, opt-in enhancement in §2.5 (only safe
once an absolute HtkBinDir + absolute WorkingDirectory are in force). It is intentionally
**not** part of the committed change.

Public method **signatures** are preserved in all three copies, so no caller's argument list
changes (the one behavioral caller consequence — the new `throw` — is handled in §2.4).

### 2.1 Common field + helper (all three copies — replaces the `htk_cmd_dir` line)

Before (Core / Tester, lines 14-15):
```csharp
        //static string htk_cmd_dir = ProgramFilesx86() + "\\HTK\\bin\\";
        static string htk_cmd_dir = "\\htk\\";
```
Before (Creator, lines 14-15 — both already commented):
```csharp
        //static string htk_cmd_dir = ProgramFilesx86() + "\\HTK\\bin\\";
        //static string htk_cmd_dir = "\\htk\\";
```

After (identical in all three copies):
```csharp
        // BUG-13: HTK bin location is configurable (default = today's behavior:
        // resolve "HCopy.exe" via PATH / App Paths). Override at runtime via
        // HTK_Interface.HtkBinDir or the TURAN_HTK_BIN environment variable.
        public static string HtkBinDir = "";

        private static string HCopyPath()
        {
            string dir = !string.IsNullOrEmpty(HtkBinDir)
                ? HtkBinDir
                : (Environment.GetEnvironmentVariable("TURAN_HTK_BIN") ?? "");
            // HTK binary is "HCopy.exe" on Windows, "HCopy" elsewhere (mono/Linux).
            string exe = (Path.DirectorySeparatorChar == '\\') ? "HCopy.exe" : "HCopy";
            return string.IsNullOrEmpty(dir) ? exe : Path.Combine(dir, exe);
        }
```
(`System`, `System.IO`, `System.Diagnostics` are already imported in every copy — no new
`using` is required. `Path.Combine` accepts a full path as the second arg only when the
second arg is *not* itself rooted; `exe` here is a bare filename, so this is correct.)

Removing the `htk_cmd_dir` field obliges updating each `WorkingDirectory` line that
referenced it (Core line 20, Tester line 20); Creator never referenced it.

### 2.2 Creator copy — `CreateMFCC_D_A_T` (lines 17-39) — the LIVE path

Before:
```csharp
        public static void CreateMFCC_D_A_T(string config_file_path, string script_file_path, string app_path)
        {
            Process hcopy_proc = new Process();
            hcopy_proc.StartInfo.WorkingDirectory = app_path;
            hcopy_proc.StartInfo.FileName = "HCopy.exe";


            // HCopy -C mfcc_config.txt -S teszt.scp

            //prcs.StartInfo.Arguments = " -C mfcc_config_E_D_A_T.txt -S mfcc_E_D_A_T.scp";
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file_path;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                hcopy_proc.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
         }
```

After:
```csharp
        public static void CreateMFCC_D_A_T(string config_file_path, string script_file_path, string app_path)
        {
            Process hcopy_proc = new Process();
            // app_path is the data dir the .scp/.wav/.config entries are relative to — unchanged.
            hcopy_proc.StartInfo.WorkingDirectory = app_path;
            hcopy_proc.StartInfo.FileName = HCopyPath();   // BUG-13: configurable (was "HCopy.exe")
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file_path;

            // UseShellExecute stays at its default (true); WindowStyle.Hidden still applies.
            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                hcopy_proc.Start();
                hcopy_proc.WaitForExit();              // BUG-13: was fire-and-forget (read-before-write race)
                if (hcopy_proc.ExitCode != 0)          // BUG-13: failure was silently swallowed
                {
                    throw new Exception("HCopy failed (exit code " + hcopy_proc.ExitCode + ").");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;   // was: return; — surface failure to the (now-guarded, §2.4) caller
            }
        }
```
Rationale for `throw` instead of `return;`: the immediate caller
(`Creator.cs:217-226`) already wraps this in `try { ... } catch { throw; }`, i.e. it expects
exceptions to propagate. Silently returning was the bug. Propagation is correct **provided**
the outermost live caller is guarded — see §2.4 (mandatory companion change).

### 2.3 Tester copy — `CreateMFCC_D_A_T` (lines 17-31) — DEAD, fixed for consistency

Before:
```csharp
        public static void CreateMFCC_D_A_T(string config_file_path, string script_file, string app_path)
        {
            Process hcopy_proc = new Process();
            hcopy_proc.StartInfo.WorkingDirectory = app_path +htk_cmd_dir;
            hcopy_proc.StartInfo.FileName = "HCopy.exe";


            // HCopy -C mfcc_config.txt -S teszt.scp

            //prcs.StartInfo.Arguments = " -C mfcc_config_E_D_A_T.txt -S mfcc_E_D_A_T.scp";
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            hcopy_proc.Start();
        }
```

After (drop the broken `+ htk_cmd_dir` suffix — the scp/config are relative to the data dir,
not a `htk\` sub-folder — and mirror the Creator body):
```csharp
        public static void CreateMFCC_D_A_T(string config_file_path, string script_file, string app_path)
        {
            Process hcopy_proc = new Process();
            hcopy_proc.StartInfo.WorkingDirectory = app_path;
            hcopy_proc.StartInfo.FileName = HCopyPath();
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            hcopy_proc.Start();
            hcopy_proc.WaitForExit();
            if (hcopy_proc.ExitCode != 0)
            {
                throw new Exception("HCopy failed (exit code " + hcopy_proc.ExitCode + ").");
            }
        }
```

### 2.4 Core copy — `CreateMFCC_D_A_T` (lines 17-39) — DEAD, fixed for consistency

This copy has a different signature (`wav_file_path, config_file_path, script_file`, no
`app_path`) and used the bogus `htk_cmd_dir` as the working directory. **Do not change the
signature** (avoid ripple, even though it is unused). Use the process current directory and
apply the same exe-resolution + exit-code check.

Before:
```csharp
        public static void CreateMFCC_D_A_T(string wav_file_path, string config_file_path, string script_file)
        {
            Process hcopy_proc = new Process();
            hcopy_proc.StartInfo.WorkingDirectory = htk_cmd_dir;
            hcopy_proc.StartInfo.FileName = "HCopy.exe";


            // HCopy -C mfcc_config.txt -S teszt.scp

            //prcs.StartInfo.Arguments = " -C mfcc_config_E_D_A_T.txt -S mfcc_E_D_A_T.scp";
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                hcopy_proc.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
```

After (`wav_file_path` stays unused, exactly as today):
```csharp
        public static void CreateMFCC_D_A_T(string wav_file_path, string config_file_path, string script_file)
        {
            Process hcopy_proc = new Process();
            // Working dir = current directory; scp/config paths resolve relative to it
            // (was the bogus "\\htk\\"). This method has no live caller.
            hcopy_proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            hcopy_proc.StartInfo.FileName = HCopyPath();
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                hcopy_proc.Start();
                hcopy_proc.WaitForExit();
                if (hcopy_proc.ExitCode != 0)
                {
                    throw new Exception("HCopy failed (exit code " + hcopy_proc.ExitCode + ").");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
```

### 2.4b MANDATORY companion change — guard the live caller (`Form1.cs`)

The Creator copy now **throws** on failure (replacing the silent `return;`). Today that
failure is swallowed and surfaces *later* — when `Engine.ReadMFCC_D_A_T` does
`File.Open(missing .mfc3)` — at a point that is already inside a `try/catch { MessageBox }`.
After the fix the throw originates *earlier*, at the `CalculateFeatureVectors` call. There
are exactly two such call sites (`grep CalculateFeatureVectors`):
- `Turan_tester/Turan_tester/Form1.cs:180` — **already** wrapped in `try/catch { MessageBox }`. OK.
- `Turan_tester/Turan_tester/Form1.cs:90` (`soundDetected`) — **NOT** wrapped; the method's
  only `try` starts at line ~104. `Program.cs` installs no `Application.ThreadException` /
  `AppDomain.UnhandledException` handler, and `soundDetected` may run off the UI thread
  (it later marshals via `label1.Invoke`). An unguarded throw here can terminate the process.

Required edit at `Form1.cs:89-90`.

Before:
```csharp
            Turan_creator.Creator.Application_path = "dat\\";
            Turan_creator.Creator.CalculateFeatureVectors(signal);
```
After:
```csharp
            Turan_creator.Creator.Application_path = "dat\\";
            try
            {
                Turan_creator.Creator.CalculateFeatureVectors(signal);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return; // abort: do not attempt recognition on a missing/stale .mfc3
            }
```
This mirrors the already-guarded site at line 180 and short-circuits the rest of
`soundDetected` (lines ~95-118) so recognition is not run against a feature file HCopy
failed to (re)generate. `System` (for `Exception`) and `System.Windows.Forms` (for
`MessageBox`) are already in scope in this Form.

### 2.5 OPTIONAL (not committed) — stderr text capture

If a maintainer later wants HCopy's stderr text in the exception, it requires
`UseShellExecute=false` and is only safe with the resolution risks neutralized:
- set `hcopy_proc.StartInfo.FileName` to an **absolute** HCopy path (require non-empty
  `HtkBinDir`/`TURAN_HTK_BIN`), and
- set `hcopy_proc.StartInfo.WorkingDirectory = Path.GetFullPath(app_path);` (absolute), and
- drain stderr **asynchronously** (`BeginErrorReadLine()` into a `StringBuilder`) while
  doing `StandardOutput.ReadToEnd()`, then `WaitForExit()` — never two sequential
  `ReadToEnd()` calls (that is the documented pipe-buffer deadlock pattern).

This is deferred precisely because it perturbs the live path's resolution; the committed
change above already delivers the substantive robustness win (wait + exit-code) at near-zero
risk.

### 2.6 `ProgramFilesx86()` helper — leave as-is (all three copies)

Already unused (only referenced from a commented line). Removing it is out of scope and would
only churn an unused-method warning. Optionally folded into default resolution in a future
cleanup, not now.

---

## 3. Every duplicated copy that needs the same change

All three `HTK_Interface.cs` copies get the new `HtkBinDir` field + `HCopyPath()` helper +
the synchronous/exit-code-checking spawn body. They differ only in:
- namespace (`Turan_core` / `Turan_tester` / `Turan_creator`) — unchanged;
- `CreateMFCC_D_A_T` signature — unchanged (Core lacks `app_path`);
- the working-directory expression (Core → `Environment.CurrentDirectory`;
  Tester → `app_path`; Creator → `app_path`).

**Runtime reach (verified by grep across the repo):**
- `HTK_Interface.CreateMFCC_D_A_T` is called from exactly one place:
  `Turan_creator/Turan_creator/Creator.cs:219`
  → `CreateMFCC_D_A_T("mfcc_config.txt", "temp.scp", Application_path)`, where
  `Application_path` is set to `"dat\\"` by `Turan_tester/.../Form1.cs:89,176`.
  **Only the Creator copy executes.** The Core and Tester `CreateMFCC_D_A_T` are dead; they
  are fixed only to stop the copies from drifting (the BUG-14 duplication hazard in miniature).
- `HTK_Interface.ReadMFCC_D_A_T` (Core copy) is the live reader, called from
  `Turan_core/Turan_core/Engine.cs:123,128`. **`ReadMFCC_D_A_T` is NOT touched by this bug** —
  leave all three `ReadMFCC_D_A_T` bodies byte-for-byte unchanged.
- No fourth copy exists: `find -iname HTK_Interface.cs` and `grep -rl HCopy` both return
  exactly these three files. (`Felismero_motor_LITE` has none.)
- `CalculateFeatureVectors` has exactly two callers (`Form1.cs:90,180`); only line 90 needs
  the §2.4b guard.

---

## 4. Backward / on-disk compatibility

**No format change. Fully backward compatible.**
- The `.mfc3` HTK feature binary is produced by HCopy and read by `ReadMFCC_D_A_T`; neither
  the writer invocation arguments (`-C config -S scp`) nor the reader changes, so existing
  `.mfc3` files and templates remain valid.
- `temp.scp` (`Creator.cs:204-215`) and `mfcc_config.txt` are not touched.
- Serialized DTW templates (`Creator.SerializeArray` / `Engine.DeSerializeArray`, BUG-12)
  are unrelated to this change.
- Default runtime behavior is preserved: with `HtkBinDir` unset and no `TURAN_HTK_BIN`,
  `HCopyPath()` returns `"HCopy.exe"` on Windows — exactly today's `FileName` — and
  `UseShellExecute`/`WindowStyle.Hidden` are unchanged, so executable and working-directory
  resolution are identical to today. The only intentional behavior deltas are (i) the call
  now **blocks** until HCopy exits, and (ii) a **non-zero exit now throws** (and the live
  caller is guarded). Both are strict robustness improvements over silent fire-and-forget.

No persisted-format change ⇒ **no versioned-read shim needed.**

---

## 5. Shared contracts other fixes depend on

- **Public `CreateMFCC_D_A_T` signatures are preserved** in all three copies, so the single
  caller (`Creator.cs:219`) compiles unchanged. The only cross-module behavioral coupling is
  the new `throw`, fully contained by the §2.4b `Form1.cs` guard.
- This fix does **not** share a serialization contract (that is BUG-12's
  `SerializeArray`/`DeSerializeArray` pairing — out of scope here).
- Relationship to **BUG-14** (de-duplication): the `HtkBinDir`/`HCopyPath()` logic and spawn
  body are written identically in all three copies so that, when BUG-14 extracts a shared
  HTK/DSP library, the three bodies collapse to one with no semantic conflict.
- Relationship to the ROADMAP's "consider replacing HTK with the native extractor":
  out of scope for BUG-13. That is an architectural change gated on the native MFCC path
  (BUG-01..06) being trusted; this plan only makes the existing HTK shell-out portable,
  configurable, and fail-loud.

---

## 6. Self-verification without a compiler

Static checks (read/trace only):

1. **Imports present.** Each `HTK_Interface.cs` already has `using System;`, `using System.IO;`,
   `using System.Diagnostics;` (lines 1-5) → `Environment`, `Path`, `Process` resolve with no
   new `using`. `Form1.cs` already uses `System` and `System.Windows.Forms`.
2. **No dangling `htk_cmd_dir`.** After removing the field, grep `htk_cmd_dir` across the
   three files → must return **zero** matches (Core line 20 and Tester line 20 were the only
   live uses and are rewritten; Creator's were already commented).
3. **EXE resolution parity.** Trace `HCopyPath()` with `HtkBinDir == ""` and no env var on
   Windows (`Path.DirectorySeparatorChar == '\\'`) → returns `"HCopy.exe"`, the same string
   today's `FileName` held; with `UseShellExecute=true` unchanged, ShellExecute resolves it
   exactly as before. With `HtkBinDir = @"C:\HTK\bin"` → `C:\HTK\bin\HCopy.exe`. On Linux/mono
   (`/`) → `"HCopy"`.
4. **Working-directory parity.** Confirm the live Creator copy still sets
   `WorkingDirectory = app_path` (so relative `temp.scp`, `mfcc_config.txt`, and the
   `.wav`/`.mfc3` names inside the scp resolve in `dat\` exactly as before). The scp lines
   (`Creator.cs:212`) use bare filenames, confirming the working dir must be the data dir.
5. **No redirect ⇒ no deadlock surface.** Confirm `RedirectStandardError/Output` are NOT set
   and `UseShellExecute` is left at default; therefore the sequential-`ReadToEnd` deadlock
   class does not apply. (`ExitCode` is valid after `WaitForExit` independent of
   `UseShellExecute`.)
6. **Caller-guard trace.** Re-read `Form1.cs:83-118`: confirm the new `try/catch` wraps
   `CalculateFeatureVectors(signal)` and `return`s on catch, so a thrown HCopy failure becomes
   a `MessageBox` + abort instead of an unhandled (possibly off-UI-thread) exception. Confirm
   the second site (`Form1.cs:180`) is already wrapped — leave it.
7. **`ReadMFCC_D_A_T` untouched.** Diff each file's `ReadMFCC_D_A_T` region before/after →
   must be byte-identical (the live feature path through `Engine.cs` must not change).
8. **Three-copy parity.** Diff the three new `CreateMFCC_D_A_T` + `HCopyPath` blocks: they
   should differ only in namespace, signature, and the one working-directory expression (§3).

The change is mechanical and format-preserving; nothing here requires execution to validate
beyond these traces.

---

## 7. How this resolves the prior peer review

An earlier draft proposed flipping `UseShellExecute=false` to redirect stderr. That raised
three blockers (caller regression; sequential-`ReadToEnd` deadlock; unreliable relative
`WorkingDirectory` under `UseShellExecute=false` on .NET Framework). This revision resolves
them as follows:
- **RC1 (caught→uncaught throw):** addressed head-on by §2.4b — the unguarded live caller
  `Form1.cs:90` is wrapped in `try/catch { MessageBox; return; }`, matching `Form1.cs:180`.
- **RC2 (stream deadlock):** eliminated by not redirecting streams at all (exit-code-only).
  Stderr-text capture is deferred to §2.5 with the correct async-drain pattern documented.
- **RC3 (relative WorkingDirectory under `UseShellExecute=false`):** moot — `UseShellExecute`
  stays `true`, so the live path's executable and working-directory resolution are byte-for-
  byte unchanged. (If §2.5 is ever adopted, it mandates absolute paths.)

---

## Peer review

**Verdict: APPROVED** (with two non-blocking recommendations below). The plan is correct,
complete across all three duplicated copies plus the live-caller guard, backward-compatible,
and does not introduce a correctness regression on the live path.

### What I verified against the real source (not just the plan text)

- **Root cause is accurate.** All three `HTK_Interface.cs` copies match the plan's
  before-blocks byte-for-byte: Core line 15 `htk_cmd_dir = "\\htk\\"` used as
  `WorkingDirectory` (line 20); Tester uses `app_path + htk_cmd_dir` (line 20); Creator has
  the constant commented and uses `app_path` (line 20). All three set
  `FileName = "HCopy.exe"`, all are fire-and-forget (`Start()` with no `WaitForExit()`/exit
  check), and the Creator copy's `catch { return; }` silently swallows failures. The three
  defects (a) non-portable path, (b) hard-coded exe, (c) no sync/error capture are real.
- **Live reach is correct.** `grep` confirms `CreateMFCC_D_A_T` is called from exactly one
  site, `Creator.cs:219` (`Application_path = "dat\\"`), routing to the **Creator** copy only;
  Core/Tester copies are dead. There is no fourth copy (`find -iname HTK_Interface.cs` → 3).
  `ReadMFCC_D_A_T` is correctly excluded from this bug's scope.
- **The §2.4b mandatory caller guard is genuinely necessary and correctly placed.**
  `CalculateFeatureVectors` has exactly two callers: `Form1.cs:90` (the method's `try` only
  begins at line 104, so line 90 is **unguarded**) and `Form1.cs:180` (already wrapped). Today
  the Creator copy swallows HCopy failure (`catch { return; }`), so line 90 never throws;
  after the fix it can, and `grep` confirms there is **no** `Application.ThreadException` /
  `AppDomain.UnhandledException` handler in `Turan_tester`, so an unguarded throw (potentially
  off the UI thread, given the later `label1.Invoke`) could terminate the process. The guard
  is required, and `return;` on catch correctly aborts before recognition reads a stale/missing
  `.mfc3`.
- **Backward / data-format compatibility holds.** No `.mfc3`/`temp.scp`/`mfcc_config.txt`
  format change; HCopy args (`-C config -S scp`) unchanged. With `HtkBinDir`/`TURAN_HTK_BIN`
  unset on Windows, `HCopyPath()` returns `"HCopy.exe"` and `UseShellExecute` stays `true`, so
  executable + working-directory resolution are byte-for-byte identical to today. `ExitCode` is
  valid after `WaitForExit()` regardless of `UseShellExecute`, so the exit-code check is sound,
  and with no redirected streams the pipe-deadlock class does not apply.
- **No off-by-one or integer-division traps** exist in this change (none of the touched lines
  do arithmetic). `Path.Combine(dir, exe)` is used correctly (`exe` is a bare, non-rooted
  filename). The keeping of `UseShellExecute = true` (the §2 design decision) is the right
  call and avoids the resolution/deadlock regressions an earlier draft would have introduced.
- **Dangling-reference / three-copy parity checks pass.** Removing `htk_cmd_dir` is paired with
  rewriting every use (Core→`Environment.CurrentDirectory`, Tester→`app_path`; Creator never
  referenced it), so no stale symbol remains; the three new bodies differ only in
  namespace/signature/working-dir expression, preserving BUG-14 collapsibility.

### Recommendations (non-blocking; do not gate the commit)

1. **Bound the new `WaitForExit()`.** Switching from fire-and-forget to an *unbounded*
   `WaitForExit()` introduces a failure mode that did not exist before: if HCopy stalls (e.g.
   blocks on a license/stdin prompt), the call hangs forever. On the batch path
   (`Form1.cs:180`, `btn_analyze_active_Click`, which runs on the **UI thread**) this freezes
   the app with no recovery. Recommend `WaitForExit(timeoutMs)`; on timeout, `Kill()` and
   `throw`. At minimum, document the new UI-thread-blocking behavior of the analyze path.
2. **Redundant double-handling in the Creator copy (cosmetic).** The
   `throw new Exception("HCopy failed (exit code …)")` is thrown inside the `try` and caught by
   that same `catch` (which `Console.WriteLine`s then `throw`s). It works and preserves the
   message, but it logs once and re-throws; consider checking the exit code *after* the
   `try`/`catch` for clarity. Harmless as written.
3. **ROADMAP wording.** The roadmap's "capture stderr/exit code" is only partially delivered
   (exit code now; stderr **text** deferred to §2.5). The deferral rationale is sound — just
   ensure the ROADMAP/changelog reflects "partial" so the item is not marked fully closed.

None of these affect live recognition correctness; the substantive wins (configurable path,
portable exe name, read-before-write race closed, silent-failure surfaced + guarded) are all
delivered correctly.
