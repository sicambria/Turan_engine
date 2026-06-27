# Fix Plan — BUG-12: `BinaryFormatter` used for template serialization (deprecated/insecure)

**Severity:** P3 (⚪ low priority; modernization / migration-blocker, not a correctness bug)
**Status of code as read:** confirmed at the three locations below on branch `fix/roadmap-bugs`.

---

## 1. Root cause (restated from the code)

`double[,]` feature templates (`.lpc` / `.mfcc`) are written and read with
`System.Runtime.Serialization.Formatters.Binary.BinaryFormatter`:

- **Writer (Creator):** `Turan_creator/Turan_creator/Creator.cs:318-332` `SerializeArray(double[,], string)`
  ```csharp
  FileStream fstream = new FileStream(bin_fpath, FileMode.Create, FileAccess.Write);
  BinaryFormatter binFormat = new BinaryFormatter();
  binFormat.Serialize(fstream, arList);
  ```
- **Reader (Engine):** `Turan_core/Turan_core/Engine.cs:162-179` `DeSerializeArray(string)`
  ```csharp
  FileStream fstream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
  BinaryFormatter binFormat = new BinaryFormatter();
  binArray = (double[,])binFormat.Deserialize(fstream);
  ```
- **Duplicated write+read pair (LITE app):** `Felismero_motor_LITE/Felismero_motor/Form1.cs`
  - `SerializeArray` at `839-853`
  - `DeSerializeArray` at `861-878`

`BinaryFormatter` is deprecated and a known RCE vector when deserializing untrusted data
(`Deserialize` can instantiate arbitrary types from the stream), and it is **removed/blocked in
.NET 5+**. Even though the input here is locally-generated template files, the API is the single
hardest blocker to any .NET-Core/5+ migration. The data persisted is trivially simple — a
rectangular `double[,]` — so the heavyweight, type-graph-capable formatter is unnecessary.

**Data-flow context (verified):**
- Creator only *writes* (`Creator.cs:180` `.lpc`, `:191` `.mfcc`); it has no `DeSerializeArray`.
- Engine only *reads* (`Engine.cs:70`, `:93`); it has no `SerializeArray`.
- The on-disk `.lpc`/`.mfcc` byte format is therefore a **cross-executable contract**:
  Creator (producer) ↔ Engine (consumer). Both must agree.
- The LITE `Form1.cs` is a **self-contained** write→read pair inside one app; it does **not**
  interoperate with Creator/Engine files. It is changed only for consistency and to remove the
  same deprecated API, not because it shares files with the other two modules.

---

## 2. Replacement format (length-prefixed binary, dimension-agnostic)

A minimal, explicit, little-endian layout written with `BinaryWriter` / read with `BinaryReader`:

```
offset 0   : 4 bytes  magic  = ASCII "TRA1"  (0x54 0x52 0x41 0x31)  -- "Turán Array v1"
offset 4   : int32    rows   = arList.GetLength(0)
offset 8   : int32    cols   = arList.GetLength(1)
offset 12  : rows*cols * float64, row-major (r outer, c inner)
```

Dimensions are stored dynamically (no hard-coded width), so the format is unaffected by BUG-01's
planned column-count change (see §5).

### 2a. Writer — replace the body of `SerializeArray` (apply in Creator.cs and Form1.cs)

**Before** (Creator.cs:318-332; identical shape in Form1.cs:839-853):
```csharp
public static void SerializeArray(double[,] arList, string bin_fpath)
{
    FileStream fstream = new FileStream(bin_fpath, FileMode.Create, FileAccess.Write);
    BinaryFormatter binFormat = new BinaryFormatter();
    try
    {
        binFormat.Serialize(fstream, arList);
    }
    finally
    {
        fstream.Close();
    }
}
```

**After:**
```csharp
public static void SerializeArray(double[,] arList, string bin_fpath)
{
    // BUG-12: replaced BinaryFormatter with explicit length-prefixed binary format.
    using (FileStream fstream = new FileStream(bin_fpath, FileMode.Create, FileAccess.Write))
    using (BinaryWriter bw = new BinaryWriter(fstream))
    {
        int rows = arList.GetLength(0);
        int cols = arList.GetLength(1);
        bw.Write((byte)'T'); bw.Write((byte)'R'); bw.Write((byte)'A'); bw.Write((byte)'1');
        bw.Write(rows);   // Int32, little-endian
        bw.Write(cols);   // Int32, little-endian
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                bw.Write(arList[r, c]);  // Double, little-endian
    }
}
```
> Keep the `bin_fpath`/`fname` parameter name exactly as in each file (Creator uses `bin_fpath`,
> Form1 uses `fname`). Only the body changes.

### 2b. Reader — replace the body of `DeSerializeArray` (apply in Engine.cs and Form1.cs)

**Before** (Engine.cs:162-179; identical shape in Form1.cs:861-878):
```csharp
public static double[,] DeSerializeArray(string file_path)
{
    FileStream fstream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
    BinaryFormatter binFormat = new BinaryFormatter();
    double[,] binArray;
    try
    {
        binArray = (double[,])binFormat.Deserialize(fstream);
    }
    finally
    {
        fstream.Close();
    }
    return binArray;
}
```

**After (versioned read — new format if magic present, else legacy fallback):**
```csharp
public static double[,] DeSerializeArray(string file_path)
{
    // BUG-12: versioned reader. New files carry the "TRA1" magic and are read with BinaryReader.
    // Legacy files (written by older BinaryFormatter builds) have no magic -> fall back.
    using (FileStream fstream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
    {
        byte[] magic = new byte[4];
        int read = fstream.Read(magic, 0, 4);
        bool isNew = (read == 4 &&
                      magic[0] == (byte)'T' && magic[1] == (byte)'R' &&
                      magic[2] == (byte)'A' && magic[3] == (byte)'1');

        if (isNew)
        {
            using (BinaryReader br = new BinaryReader(fstream))
            {
                int rows = br.ReadInt32();
                int cols = br.ReadInt32();
                double[,] arr = new double[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        arr[r, c] = br.ReadDouble();
                return arr;
            }
        }

        // ---- Legacy fallback (BUG-12 Phase 1 only; see §4/§5) ----
        fstream.Seek(0, SeekOrigin.Begin);
        BinaryFormatter binFormat = new BinaryFormatter();
        return (double[,])binFormat.Deserialize(fstream);
    }
}
```
> Keep each file's parameter name (Engine uses `file_path`, Form1 uses `fname`).

### 2c. `using` directives

- **Engine.cs** and **Form1.cs (LITE)**: **keep**
  `using System.Runtime.Serialization.Formatters.Binary;` — still needed by the legacy fallback
  branch in the reader (Phase 1). `using System.IO;` is already present in both.
- **Creator.cs**: the writer no longer references `BinaryFormatter`, and Creator has no reader,
  so `using System.Runtime.Serialization.Formatters.Binary;` (`Creator.cs:23`) becomes **unused**.
  Removing it is optional and cosmetic; recommended, to make the "deprecated API gone from the
  producer" intent explicit. `using System.IO;` (`Creator.cs:22`) is already present.

---

## 3. Every duplicated copy that needs the change

| Module | File:line | Method | Change |
|---|---|---|---|
| Turan_creator | `Turan_creator/Turan_creator/Creator.cs:318-332` | `SerializeArray` | writer → §2a; drop unused `using` at `:23` |
| Turan_core | `Turan_core/Turan_core/Engine.cs:162-179` | `DeSerializeArray` | reader → §2b; keep `using` at `:23` |
| Felismero_motor_LITE | `Felismero_motor_LITE/Felismero_motor/Form1.cs:839-853` | `SerializeArray` | writer → §2a |
| Felismero_motor_LITE | `Felismero_motor_LITE/Felismero_motor/Form1.cs:861-878` | `DeSerializeArray` | reader → §2b; keep `using` at `:29` |

`grep -rln "SerializeArray\|DeSerializeArray\|BinaryFormatter" --include=*.cs` returns exactly these
three files — no other copies exist. The LITE app is the only one carrying **both** halves of the pair.

---

## 4. Backward compatibility / on-disk format impact

**This changes the on-disk byte format of `.lpc` and `.mfcc` template files.** Compatibility is
preserved asymmetrically and deliberately:

- **Old files → new code: SUPPORTED.** The reader (§2b) sniffs the 4-byte magic. Legacy
  `BinaryFormatter` streams begin with byte `0x00` (the `SerializationHeaderRecord` whose first
  byte is the record-type enum `SerializedStreamHeader = 0`); the first byte is never `0x54`
  (`'T'`). So magic detection unambiguously routes old files to the legacy branch. *(No committed
  sample templates exist in the repo to hexdump — `find` for `*.mfcc`/`*.lpc`/`*.dat` returns
  nothing — so this rests on the BinaryFormatter wire-format spec, not on a sampled artifact.)*
- **New files → old code: NOT supported.** An un-updated Engine has only the `BinaryFormatter`
  path, which throws on the `'T'` header. **Constraint: Creator and Engine must be deployed/updated
  together.** They are separate executables, so this is a real release-coupling requirement, not a
  footnote. The LITE app is self-contained, so its writer/reader must be updated in the same commit
  but has no cross-process coupling.
- **Endianness:** `BinaryWriter`/`BinaryReader` are little-endian; the engine targets Windows/x86,
  so producer and consumer are both little-endian. No portability regression.

**Two-phase migration (the fallback is temporary):**
- **Phase 1 (this fix):** dual-read (new format + legacy fallback). Stays on .NET Framework /
  VS2013. Fully backward compatible for reads.
- **Phase 2 (follow-up, gated):** delete the legacy `BinaryFormatter` branch and the
  `using System.Runtime.Serialization.Formatters.Binary;` from Engine.cs/Form1.cs. This is what
  actually unblocks .NET 5+ (the fallback branch still *references* `BinaryFormatter`, which does
  not compile there). Phase 2 is safe because Creator derives `.lpc`/`.mfcc` from source PCM/WAV
  (`Creator.cs:180/191`, reading WAVs via `GetNumberOfPCMFrames`, `Creator.cs:335+`): **if the
  source WAVs are retained, all templates can be regenerated for free**, making the legacy reader a
  convenience rather than a permanent dependency. Schedule Phase 2 only after confirming every
  deployed template set has been regenerated (or the source WAVs are archived).

---

## 5. Shared contracts other bug fixes depend on

- **Creator.SerializeArray ↔ Engine.DeSerializeArray** are the two ends of one on-disk contract;
  the magic/rows/cols/row-major layout in §2 must be byte-identical on both sides. Any future edit
  to one half must mirror the other.
- **BUG-01 dependency (ROADMAP.md:18-20):** BUG-01 changes the *shape* of the serialized array
  (allocate `double[nSamples, 4*num_of_feature_vectors]`, vector width 60, column blocks at
  offsets 0/15/30/45). BUG-12's format is intentionally **dimension-agnostic** (rows/cols are
  written into the header, not assumed), so BUG-01's column-count change flows through
  Serialize/Deserialize unchanged. **Constraint for whoever does BUG-01:** do not hard-code matrix
  dimensions in the serializer; rely on `GetLength(0)/GetLength(1)` as written here. BUG-12 and
  BUG-01 are independent and may land in either order; if BUG-01 lands first the new serializer
  still handles the wider arrays with no change.
- No other ROADMAP item touches the serialization layer (grep of ROADMAP for
  transpose/dimension/format/serial shows only BUG-01's shape change and BUG-12 itself).

---

## 6. Self-verification without a compiler

1. **Magic-collision safety (the load-bearing claim):** confirm via the BinaryFormatter wire spec
   that a legacy stream's first byte is `0x00` (record type `SerializedStreamHeader`) and therefore
   ≠ `0x54` (`'T'`). No committed `.mfcc`/`.lpc`/`.dat` exists to hexdump (verified with `find`), so
   cite the spec; if a sample template is later produced, run `xxd file | head -1` and check byte 0
   is `00` for old files and `54 52 41 31` for new files.
2. **Round-trip symmetry trace:** writer loop order `for r { for c { Write(arList[r,c]) } }` exactly
   matches reader loop order `for r { for c { arr[r,c]=ReadDouble() } }`, and `rows/cols` are
   written then read in the same order — so `DeSerializeArray(SerializeArray(x)) == x` by
   construction. Confirm both halves use `(0)`=rows, `(1)`=cols consistently.
3. **Type widths:** `BinaryWriter.Write(int)` = 4 bytes, `Write(double)` = 8 bytes; reader uses
   `ReadInt32`/`ReadDouble`. Header is fixed 12 bytes, payload `rows*cols*8`. Sanity-check expected
   file size against a known template's dimensions if one is generated.
4. **API surface unchanged:** method names, signatures, `public static`, parameter names, and
   return types are untouched — every existing caller (`Creator.cs:180/191`, `Engine.cs:70/93`,
   `Form1.cs:494/721/725`) compiles and behaves identically. Grep callers to confirm none rely on
   `BinaryFormatter`-specific exception types.
5. **`using` audit:** after the edit, grep each file for `BinaryFormatter` — Creator should have
   **zero** references (and its `using` removed); Engine and Form1 should each retain exactly the
   one fallback reference plus the `using`.
6. **No new exception contract:** I/O still throws `IOException`/`FileNotFoundException` from
   `FileStream` exactly as before; the `Creator.cs:184/195` IOException wrappers around the writer
   remain valid.

---

## Change summary (surgical scope)

- 1 writer body in `Creator.cs` (+ remove 1 unused `using`).
- 1 reader body in `Engine.cs`.
- 1 writer + 1 reader body in LITE `Form1.cs`.
- No signature, caller, or public-API changes. No new dependencies.
- On-disk format changes (versioned, backward-compatible read); Creator+Engine must ship together.
