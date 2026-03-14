using UnityEngine;

/// <summary>
/// ScriptableObject for per-level configuration.
/// Create instances via Assets > Create > Game > Level Data.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int LevelNumber = 1;
    public string LevelName = "Level 1";

    [Header("Puzzle Pieces (ordered, clear left to right)")]
    public PieceEntry[] PuzzlePieces;

    [Header("Basket Grid (rows, index 0 = top row)")]
    public GridRow[] GridRows;

    [Header("Staging Slots")]
    public int SlotCount = 5;
}

/// <summary>
/// A single puzzle piece: color + amount to fill.
/// </summary>
[System.Serializable]
public struct PieceEntry
{
    public GameColor Color;
    public int Amount;
}

/// <summary>
/// A single basket: color + amount it holds.
/// If IsEmpty = true, this is an empty grid slot (no basket spawned).
/// </summary>
[System.Serializable]
public struct BasketEntry
{
    public bool IsEmpty;
    public GameColor Color;
    public int Amount;
}

/// <summary>
/// One row of baskets in the grid.
/// </summary>
[System.Serializable]
public struct GridRow
{
    public BasketEntry[] Baskets;
}
