# Previous Sprints Summary

## Sprint 1: Core Gameplay
Pick baskets → fly to staging → auto-fill cascade → win/lose.
- `GameColor.cs`, `PuzzlePiece.cs`, `Basket.cs`, `BasketGrid.cs`, `StagingSlots.cs`, `PuzzleBoard.cs`
- `LevelData.cs` (PieceEntry, GridRow, SlotCount)

## Sprint 2: Empty Slots + Sheet Import
- `BasketEntry.IsEmpty` for empty grid slots (`E`)
- `Editor/LevelDataImporter.cs` — CSV from Google Sheet (gid 0, 129557841, 449358609)

## Sprint 3: Visual Refactor
- Pre-setup level prefabs (`Resources/Prefabs/PieceLv{N}`)
- Material swap: Balloon (`Resources/GameJam/Art/Balloon/`) for pieces, Box (`Resources/GameJam/Art/Box/`) for baskets
- `SkinnedMeshRenderer` BlendShape (index 0) with DOTween smooth transition
- AmountText from `Resources/Prefabs/AmountText`, positioned at mesh center
- 3D `BoxCollider` + `Physics.Raycast` on baskets
- Top-down camera: grid on X/Z, staging on X
- `AnchorPoint` under StagingSlots for slot origin
- Yellow + Purple colors added (`GameColor`, importer, materials)
- Debug piece stack UI in MainUI (top-left, live updates)

## Sprint 4: Filling Flow
- One basket fills at a time (sequential coroutine)
- `BasketFillingPoint` under StagingSlots — basket jumps there before fill
- Basket always destroyed after fill
- Leftover + piece not cleared = LOSE

## Sprint 5: Flexible Pipe + Pump
- `FlexiblePipe.cs` — Bézier curve (source → piece ConnectPoint)
- Perlin noise wind sway
- Pipe material auto-matches current piece color (box mats)
- Pump animation: width bulge travels source→target before piece.Fill
- `PuzzleBoard.GetCurrentPiece()` for auto-targeting

---

# Sprint 6 Tasks

1. There will be particle system play on piece cleared. It will be ref-ed from GameManager.
2. We need a sfx on piece cleared too. create a dummy sound for test.