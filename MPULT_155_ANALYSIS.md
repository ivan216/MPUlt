# MPUlt Code Evolution Analysis

## Overview

This document details the differences across three versions:
1. **Old code** (commit `1478e8f`) — The original MPUlt source
2. **MPUlt_155** (`MPUlt_de/`) — Decompiled code from MPUlt_155.exe
3. **multi-format-generators branch** — Based on old code + MPUlt_155 improvements

---

## Part 1: MPUlt_155 vs Old Code — Complete Diff

### 1. Data Structure Changes

#### 1.1 PuzzleStructure.Group
| Old | MPUlt_155 |
|---|---|
| `double[][]` — twist vectors | `List<double[]>` — flat matrices, `[0]` = identity |
| No identity element | First element is identity matrix |
| `Group.Length` = generator count | `Group.Count` includes all closure elements |

#### 1.2 PuzzleStructure.Axes
| Old | MPUlt_155 |
|---|---|
| `PAxis[]` fixed array | `List<PAxis>` dynamic list |
| `MaxNAxes=1320` hard limit | No hard limit |
| Generated iteratively | Generated via orbit computation |

#### 1.3 PuzzleStructure.Faces
| Old | MPUlt_155 |
|---|---|
| `PFace[]` fixed array | `List<PFace>` dynamic list |
| `MaxNVert=14400` hard limit | No hard limit |

#### 1.4 PBaseAxis
| Old | MPUlt_155 |
|---|---|
| No `SMatrices` | `List<double[]> SMatrices` — axis-stabilizing group elements |
| No `AddSMatrix()` | `AddSMatrix(double[])` — store stabilizer |
| Old `ExpandPrimaryTwists()` | SMatrices-based conjugation algorithm |
| No `GenerateTwists()` | `GenerateTwists()` stub (not implemented) |
| No `FindTwist4D()` | `FindTwist4D(double[] pt, int type)` — type-preferring 4D detection |

#### 1.5 PBaseTwist
| Field | Old | MPUlt_155 |
|---|---|---|
| `Dim` | — | twist dimension |
| `Orig` | — | original twist vector (variable length) |
| `Matr` | — | flat matrix form (dim×dim) |
| `NTwist` | — | segment count (2=standard, 1=odd/pure reflection) |
| `MaxAngle` | — | maximum rotation angle |
| `Pole` | — | 4D twist pole |
| `Dir` | twist vector | twist vector (backward compat) |

New constructors:
- `PBaseTwist(double[] twist, double[] axis)` — initialization with axis
- `PBaseTwist(PBaseTwist tw, double[] matr, double[] axis)` — conjugation via SMatrices

`Init()` method handles unified initialization of all fields.

#### 1.6 PBaseFace.SMatrices
| Old | MPUlt_155 |
|---|---|
| `ArrayList` of `double[,]` | `List<double[]>` of flat matrices |
| Stores difference matrices `G×M_X×M_Y^T` | Stores group matrices G directly |
| Has `CloseSMatrixSet()` closure | No closure (group closure done in CloseGroup) |
| `AddSMatrix(double[,], double[,])` | `AddSMatrix(double[])` |

#### 1.7 PAxis
| Old | MPUlt_155 |
|---|---|
| `Matrix` is `double[,]` | `Matrix` is `double[]` (flat) |
| `PAxis(PBaseAxis)` | `PAxis(PBaseAxis, double[], int)` — from flat matrix |
| `FindTwist(double[], double[,], out bool)` | `FindTwist(PAxis, int, double[], out bool)` — new signature |

#### 1.8 PFace
| Old | MPUlt_155 |
|---|---|
| `Matrix` is `double[,]` | `Matrix` is `double[]` (flat) |
| `PFace(PFace, double[])` | `PFace(PBaseFace, double[], int)` — from flat matrix |

### 2. Architecture Changes

#### 2.1 PuzzleStructure Construction Order
```
Old:                           MPUlt_155:
FillFromStrings(def)           FillFromStrings(def)
ExpandAxes()                   CloseGroup()           ← new step
ExpandFaces()                  ExpandAxes()           ← uses flat matrices
CutFaces()                     ExpandFaces()          ← uses flat matrices
EnumerateStickers()            CutFaces()
SortStickersForAxes()          EnumerateStickers()
CreateTwistMaps()              SortStickersForAxes()
                               CreateTwistMaps()
```

#### 2.2 SourceCode Field
| Old | MPUlt_155 |
|---|---|
| — | `string[] SourceCode` — preserves original definition text |
| Serialization via `GetDescription()` loses precision | Serialization uses `SourceCode` directly |

#### 2.3 New PGeom Functions (~20)
| Function | Purpose |
|---|---|
| `gcd(int, int)` | Greatest common divisor |
| `GetOrder(double[], int)` | Generalized N-segment order (LCM computation) |
| `GetOrder(double[], int, out double)` | With maxAng output |
| `ApplyTwistN(double[], double[], int)` | Generalized: iterate all dim-sized segments |
| `ApplyMirror(double[], int, double, double[], int)` | Single Householder reflection |
| `CreateMatrixIdent(int)` | Flat identity matrix |
| `CreateMatrixFromTwist(double[], int)` | Twist vector → flat matrix |
| `ApplyMatrix(double[], double[], int)` | Flat matrix × vector |
| `ApplyInvMatrix(double[], double[], int)` | Flat matrix transpose × vector |
| `MatrixEqual(double[], double[], out bool, int)` | Flat matrix comparison with reversal detection |
| `CloseMatrixSet(List<double[]>, int)` | Group closure via multiplication |
| `Angle(double[], int, int)` | Angle between two vectors in twist |
| `GetMatrixForTwist(double[], double, int)` | Fractional rotation matrix (reflection composition) |
| `GetTwistPole(double[], double[])` | 4D twist pole calculation |
| `det3(double[], double[], int, int, int)` | 3×3 determinant |
| `InvMatrix(double[], int)` | Flat matrix transpose |

Householder reflection sign fix:
- Old formula: `res = r × scale - res` (equivalent to `-(v - 2(v·r)/(r·r)r)`)
- New formula: `res -= r × scale` (standard: `v - 2(v·r)/(r·r)r`)
- For 2-segment twists both give same result (double negation cancels)
- For odd-segment twists the old formula is WRONG

### 3. Algorithm Changes

#### 3.1 ExpandAxes
Old: Create base axes, then iteratively apply Group generators to find new axes.

MPUlt_155: For each base axis, iterate full closed Group (flat matrices), compute orbit:
1. Identity → first axis (direction = base axis direction)
2. Record stabilizers (group elements preserving axis direction → `SMatrices`)
3. Remaining elements → additional axes

#### 3.2 ExpandPrimaryTwists
Old: Apply twists to each other:
```
for each twist i:
    for each twist j:
        w = ApplyTwist(twist[i].Dir, twist[j].Dir)
        if w is new → add w
```

MPUlt_155: Conjugation via SMatrices:
```
Step 1: identity conjugates → preserve original primary twists (index order)
Step 2: remaining SMatrices conjugates → generate variants
```

#### 3.3 ExpandFaces
Old: Iterative — apply Group generators to existing faces to find new ones.

MPUlt_155: For each base face, iterate full Group flat matrices:
- Identity → first face
- Others → subsequent faces
- Stabilizers → `PBaseFace.SMatrices`

#### 3.4 CloseGroup
New step: Matrix multiplication closure of the group:
```
for each element S[i]:
    for each generator S[j]:
        p = S[j] × S[i]
        if p is new → add
```

#### 3.5 CreateTwistMaps
Same algorithm in both. MPUlt_155 handles variable-length `tw.Dir` from conjugated twists.

#### 3.6 SortStickersForAxes
Same semantics, adapted for `Axes.Count` / `Faces.Count`.

#### 3.7 CutFaces
Algorithm unchanged, adapted for `Axes.Count`.

### 4. New Features

#### 4.1 FindStickerAndFace
```csharp
public int FindStickerAndFace(int st, out int relStk)
```
Fast lookup from sticker ID to face index and relative sticker index.

#### 4.2 CheckMacroAnchor / CheckMacroStart
Macro system helpers:
- `CheckMacroAnchor` — checks if sticker sequence forms a stable macro anchor
- `CheckMacroStart` — checks if current sticker selection matches an existing macro

#### 4.3 FindTwist4D
```csharp
internal int FindTwist4D(double[] pt, int type)
```
4D twist selection with `NTwist` type preference. `type=2` prefers pure rotations.

#### 4.4 New FindTwist Overload
```csharp
internal bool FindTwist(int nf, double[] pt, int type, out int ax, out int tw)
```
With type parameter, delegates to `FindTwist4D`.

### 5. Complete Difference Table

| Category | Item | Old | MPUlt_155 |
|---|---|---|---|
| Data | Group storage | `double[][]` twist vectors | `List<double[]>` flat matrices |
| Data | Group has identity | No | Yes ([0]=I) |
| Data | Axes | `PAxis[]` | `List<PAxis>` |
| Data | Faces | `PFace[]` | `List<PFace>` |
| Data | PBaseAxis.SMatrices | — | `List<double[]>` |
| Data | PAxis.Matrix | `double[,]` | `double[]` (flat) |
| Data | PFace.Matrix | `double[,]` | `double[]` (flat) |
| Data | PBaseFace.SMatrices | `ArrayList` of `double[,]` | `List<double[]>` |
| Data | PBaseTwist fields | Dir, Order | +Dim, Orig, Matr, NTwist, MaxAngle, Pole |
| Algorithm | Constructor flow | No CloseGroup | +CloseGroup() |
| Algorithm | ExpandAxes | Iterative | Orbit + SMatrices |
| Algorithm | ExpandPrimaryTwists | Self-application | SMatrices conjugation |
| Algorithm | ExpandFaces | Iterative | Per-base-face orbit |
| Algorithm | Group closure | — | CloseMatrixSet |
| Algorithm | Face SMatrices closure | CloseSMatrixSet | — (not needed) |
| Algorithm | Householder formula | `r·s - v` | `v -= r·s` |
| Macro | CheckMacroAnchor | — | Yes |
| Macro | CheckMacroStart | — | Yes |
| 4D | FindTwist4D | — | Yes (with type) |
| 4D | FindStickerAndFace | — | Yes |
| Utils | PGeom functions | ~5 | ~25 |
| Serialization | SourceCode | — | Yes |
| Serialization | GetDescription | Rebuild from data | Use SourceCode when available |
| Save | Version | "MPUltimate v1" | "MPUltimate v1.5" |
| Save | CRC constant | `0x12345675` | `305419893` |
| Scramble | Seed | `0x1010005 + 1` | `16842757 + 1` |
| Macro | STATUS_WAIT_MACRO_RECORD | — | Yes (=4) |
| 4D UI | ActionCtrlShiftClick(type=1) | — | Yes |
| Animation | GetTwistGeom | Normalize + orthogonalize | Segment-by-segment (no normalize) |
| Animation | GetMatrixForTwist | Rodrigues formula | Reflection composition |

---

## Part 2: multi-format-generators Branch Changes

### Ported from MPUlt_155

| Status | Feature |
|---|---|
| ✅ | Multi-format generator support (1/2/3-segment Group & Twist) |
| ✅ | PGeom: flat matrix functions + fixed Householder sign |
| ✅ | PBaseAxis: SMatrices + AddSMatrix |
| ✅ | PBaseTwist: new fields (Dim/Orig/Matr/NTwist/MaxAngle/Pole) |
| ✅ | PBaseTwist: new constructors (with axis / SMatrices conjugate) |
| ✅ | PBaseFace: SMatrices as `List<double[]>` + AddSMatrix overload |
| ✅ | ExpandPrimaryTwists: SMatrices-based conjugation |
| ✅ | CloseGroup / CloseMatrixSet |
| ✅ | ExpandAxes: matrix-based orbit |
| ✅ | ExpandFaces: matrix-based algorithm |
| ✅ | SourceCode field |
| ✅ | GetDescription uses SourceCode when available |
| ✅ | Puzzle.Save uses SourceCode |
| ✅ | FindStickerAndFace |
| ✅ | CheckMacroAnchor / CheckMacroStart |
| ✅ | FindTwist4D |
| ✅ | FindTwist(nf, pt, type, out ax, out tw) |
| ✅ | GetBestMatrix returns `double[]` |
| ✅ | ApplyMacro / MakeMacroStep use `double[]` |
| ✅ | PAxis.FindTwist(PAxis, int, double[], out bool) |
| ✅ | FindAxis(double[], double[]) overload |
| ✅ | CheckTwist uses `p.Base.Twists.Length` |
| ✅ | Startup null reference fix (Puz/PuzzleList) |
| ✅ | NormTwist 1-based → 0-based conversion |
| ❌ | GetUndo/GetRedo raw angles | Uses NormAngle (original behavior) |
| ❌ | GetTwistGeom Rodrigues normalization | Uses MaxAngle-based angle (open issue) |

### Not Ported

| Status | Feature | Reason |
|---|---|---|
| ❌ | Macro recording enhancement (STATUS_WAIT_MACRO_RECORD) | Basic A/B macros work; enhancement only |
| ❌ | Save version "v1.5" | "v1" still loads correctly |
| ❌ | Scramble seed different | Cosmetic — only affects shuffle order |
| ❌ | CRC constant different | Doesn't affect functionality |
| ❌ | 4D Ctrl+Shift click (type=1) | Mirror-class feature, can be added later |

### Bugs Found and Fixed

| # | Symptom | Root Cause | Fix |
|---|---|---|---|
| 1 | Mirror Cube crash `IndexOutOfRange` | `GetTwistGeom` odd-segment reversal out-of-bounds | Loop bound `vec.Length - dim` |
| 2 | `{3,3}^2_v2` wrong geometry | `ExpandPrimaryTwists` called before SMatrices populated | Move after GroupMat loop |
| 3 | Wrong face geometry | ExpandFaces stored group matrix instead of difference matrix | Reverted to old algorithm (later new+fixed SMatrices) |
| 4 | Startup NullReference | Puz/PuzzleList not initialized | Added null checks |
| 5 | 3^5 converges in 2 clicks | CheckTwist used pre-expansion twist count | Changed to `p.Base.Twists.Length` (matches MPUlt_155) |
| 6 | 120-cell sticker picking inaccurate | Multiple factors (see below) | Not resolved |
| 7 | PBaseTwist.Dir null (Init missing assignment) | New constructor didn't set Dir field | Added `Dir = twist` to Init() |

### Sticker Picking Accuracy Issue (Unresolved)

The current branch has slightly worse 120-cell sticker ray-test accuracy vs master.

Checked and ruled out:
- `TstRay`/`FindSticker`/`RecalcCoord`/`SetCoord`/`AbovePlane`/`BPln` — all identical to master
- Rendering code (`s3d/`, `lib/`) — not modified
- `ExpandPrimaryTwists` — tested in isolation, not the cause

Suspected (unverified):
1. `ExpandFaces` new algorithm using flat matrices — floating-point path differences in face matrix computation
2. `PBaseFace.SMatrices` closure removal — aggregate effect on overall structure
3. `CloseGroup` generating 3840+ elements — different iteration order in ExpandAxes/ExpandFaces changes floating-point accumulation

---

## Part 3: File Change Summary

### PuzzleParts.cs (+483/-83)

```
Old structure:
  PBaseAxis { Dir, Cut, Twists, ... }
  PBaseTwist { Dir, Order, Map }
  PBaseFace { SMatrices(ArrayList), ... }
  PAxis { Matrix(double[,]), Twists, ... }
  PFace { Matrix(double[,]), ... }
  PGeom { ~5 utility functions }

New structure:
  PBaseAxis { +SMatrices(List<double[]>), +AddSMatrix(), 
              +GenerateTwists(), +FindTwist4D() }
  PBaseTwist { +Dim, +Orig, +Matr, +NTwist, +MaxAngle, +Pole,
               +PBaseTwist(twist, axis), +PBaseTwist(tw, matr, axis) }
  PBaseFace { SMatrices(List<double[]>), +AddSMatrix(double[]) }
  PAxis { +PAxis(PBaseAxis, double[], int), 
          +FindTwist(PAxis, int, double[], out bool) }
  PFace { +PFace(PBaseFace, double[], int) }
  PGeom { +~20 new functions }
```

### PuzzleStructure.cs (+397/-165)

```
- Group: double[][] → List<double[]>
- Axes: PAxis[] → List<PAxis>
- Faces: PFace[] → List<PFace>
- +SourceCode, +GroupMat
- +CloseGroup()
- ExpandAxes: old → matrix orbit + SMatrices
- ExpandFaces: old → matrix version
- +FindStickerAndFace
- +CheckMacroAnchor, CheckMacroStart
- GetBestMatrix: double[,] → double[]
- +FindAxis(double[], double[])
- CheckTwist: p.Twists.Length → p.Base.Twists.Length
- GetDescription: prefer SourceCode
```

### Puzzle.cs (+62/-52)

```
- Axes.Length → Axes.Count
- Faces.Length → Faces.Count
- +UnpackCode changed to internal
- GetUndo/GetRedo: raw angles (no NormAngle)
- ApplyMacro: double[,] → double[]
- MakeMacroStep: double[,] → double[]
- GetTwistGeom: segment-wise + normalization
```

### rubikHT.cs (+36/-21)

```
- 4D click: FindTwist(nf,pt,out ax,out tw) → FindTwist(nf,pt,2,out ax,out tw)
- GetBestMatrix: double[,] → double[]
- Puz null check on startup
- try-catch wrappers around LoadSettings/ApplyGeomSettings
- PuzzleList = new Hashtable() pre-init
```

### MeshObj.cs (2 lines)

```
- NF = Cube.Str.Faces.Length → Cube.Str.Faces.Count
```
