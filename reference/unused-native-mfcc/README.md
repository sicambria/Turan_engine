# UNUSED — not part of any build

These files are **NOT compiled by any project** in this repository. They live
under `reference/` precisely so it is obvious they are quarantined out of every
build. Removing them, or leaving them as-is, cannot affect the solution.

## What this is

A completed Java→C# port of **Klaus Seyerlehner's CoMIRVA MFCC extractor**
(`comirva.audio.util.MFCC`) together with its two dependencies:

| File         | Class    | Role |
|--------------|----------|------|
| `mfcc.cs`    | `MFCC`   | Mel-frequency cepstral coefficient extractor: mel-warped triangular filterbank → dB → orthonormal DCT. |
| `Matrix.cs`  | `Matrix` | Minimal JAMA-style dense `double` matrix — only the members `MFCC` calls. |
| `FFT.cs`     | `FFT`    | Windowed FFT (Harris 1978 window functions), used by `MFCC` for the Hanning normalized-power spectrum. |

It is a correct, mel-warped, DCT-based MFCC design (proper triangular mel
filterbank + orthonormal DCT + Hanning power FFT).

## Why it is kept (not deleted)

This is retained as the basis for a future **HTK-free, cross-platform native
MFCC**. A finished native MFCC would let the project retire the external,
fragile `HCopy.exe` / HTK dependency (see **ROADMAP BUG-13**). It was finished
and quarantined rather than deleted per an explicit project decision (BUG-15).

## Status: code-review-verified, NOT compiler-verified

There was no C# toolchain available at port time, so the port was verified by
**code review only**, not by compilation. Because the files are excluded from
every build, any residual port issue is harmless — it cannot break the solution.

Known limitation: the `MFCC.process(AudioPreProcessor)` streaming overload is
left as a `// TODO: wire AudioPreProcessor`. The LITE-tree `AudioPreProcessor`
is currently an empty stub with no `append(double[], int, int)` method, so the
overload throws `NotImplementedException`. The `MFCC.process(double[])` overload
(the mel/DCT path) is fully implemented.

## How to re-enable

1. Add the three files to a project's compile set, e.g. in the `.csproj`:

   ```xml
   <Compile Include="path\to\mfcc.cs" />
   <Compile Include="path\to\Matrix.cs" />
   <Compile Include="path\to\FFT.cs" />
   ```

2. To use the streaming `process(AudioPreProcessor)` overload, implement
   `AudioPreProcessor` with `int append(double[] buffer, int offset, int length)`
   and restore the streaming pipeline preserved in the comment inside that method.
   The `process(double[])` overload works without any further wiring.

The classes keep `namespace Felismero_motor` (namespaces are folder-independent
in C#), so they can be re-included from anywhere.

## Provenance / license

These are derived from Klaus Seyerlehner's **CoMIRVA** (Collection of Music
Information Retrieval and Visualization Applications) `comirva.audio.util`
package. The original author/package headers are retained in each file. The
`Matrix` class follows the public-domain JAMA matrix semantics. No explicit
license text accompanied the original sources in this repository; consult the
upstream CoMIRVA distribution for licensing terms before redistributing.
