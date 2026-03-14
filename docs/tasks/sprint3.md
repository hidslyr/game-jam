Puzzle pieces will be pre-setup (like Resources/Prefabs/PieceLv1, PieceLv2, PieceLv3)

For each level, load the corresponding puzzle pieces prefab, and instantiate them in the scene.

PuzzlePiece.cs will be pre-setup in prefab too, don't need to instantiate it.

Each pre-setup PuzzlePiece will has name as index (1, 2, 3, ...), where it should be order in the chain

PuzzlePiece will has child class, PieceVisual, where we can control how it look like after subtracting. Sending the fill percentage (1 - (current amount / original amount)) to PieceVisual, and it will update the visual accordingly.

The amount textmesh pro will be instantiated by the script. And attach to PuzzlePiece. I'll create the prefab for amount in editor.

Instead of change the color of sprite, we'll update the material color (in the same object as PuzzlePiece).

When updating PieceVisual, we'll update the SkinnedMeshRenderer BlendShape, to be the same with input receive from puzzle, with a smooth transition.