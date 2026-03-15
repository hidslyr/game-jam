using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Manages the basket grid layout, spawning, and column-based gravity.
/// Attach to a child GO of PuzzleBoard named "BasketGrid".
/// </summary>
public class BasketGrid : MonoBehaviour
{
    [Header("Grid Layout")]
    public float BasketSpacingX = 1.2f;
    public float BasketSpacingZ = 1.2f;
    public float GravityAnimDuration = 0.3f;

    [Header("Prefab")]
    public Basket BasketPrefab;

    // columns[colIdx][depth] — depth 0 = top
    List<List<Basket>> columns = new List<List<Basket>>();
    int columnCount; // Cached for centering

    /// <summary>
    /// Spawn baskets from LevelData grid rows, converting to columns.
    /// </summary>
    public void Initialize(GridRow[] gridRows)
    {
        // Clear existing
        foreach (var col in columns)
            foreach (var b in col)
                if (b != null) Destroy(b.gameObject);
        columns.Clear();

        if (gridRows == null || gridRows.Length == 0) return;

        // Find max columns across all rows
        int maxCols = 0;
        foreach (var row in gridRows)
            if (row.Baskets != null && row.Baskets.Length > maxCols)
                maxCols = row.Baskets.Length;

        // Build columns from rows (row 0 = top)
        columnCount = maxCols;
        for (int c = 0; c < maxCols; c++)
        {
            var col = new List<Basket>();
            for (int r = 0; r < gridRows.Length; r++)
            {
                if (gridRows[r].Baskets == null) continue;
                if (c >= gridRows[r].Baskets.Length) continue;

                var entry = gridRows[r].Baskets[c];

                // Skip empty slots — they preserve column position but spawn no basket
                if (entry.IsEmpty) continue;

                var pos = GetWorldPosition(c, col.Count);
                var basket = Instantiate(BasketPrefab, transform);
                basket.transform.localPosition = pos;
                basket.Initialize(entry.Color, entry.Amount);
                col.Add(basket);
            }
            columns.Add(col);
        }

        UpdatePickable();
    }

    /// <summary>
    /// Remove the top basket from the given column. Shift remaining up.
    /// </summary>
    public Basket RemoveTopBasket(int colIdx)
    {
        if (colIdx < 0 || colIdx >= columns.Count) return null;
        if (columns[colIdx].Count == 0) return null;

        var basket = columns[colIdx][0];
        columns[colIdx].RemoveAt(0);

        // Animate remaining baskets rising up
        for (int i = 0; i < columns[colIdx].Count; i++)
        {
            var target = GetWorldPosition(colIdx, i);
            columns[colIdx][i].transform.DOLocalMove(target, GravityAnimDuration).SetEase(Ease.OutQuad);
        }

        UpdatePickable();
        return basket;
    }

    /// <summary>
    /// Get the top basket in a column (without removing).
    /// </summary>
    public Basket GetTopBasket(int colIdx)
    {
        if (colIdx < 0 || colIdx >= columns.Count) return null;
        if (columns[colIdx].Count == 0) return null;
        return columns[colIdx][0];
    }

    public int ColumnCount => columns.Count;

    public bool IsEmpty()
    {
        foreach (var col in columns)
            if (col.Count > 0) return true;
        return false;
    }

    void UpdatePickable()
    {
        foreach (var col in columns)
        {
            for (int i = 0; i < col.Count; i++)
            {
                bool isTop = (i == 0);
                col[i].IsPickable = isTop;
                col[i].SetOutline(isTop);
                col[i].SetDarken(!isTop);
            }
        }
    }

    Vector3 GetWorldPosition(int col, int depth)
    {
        // Center columns around origin on X axis
        float offsetX = -(columnCount - 1) * BasketSpacingX * 0.5f;
        float x = offsetX + col * BasketSpacingX;
        float z = -depth * BasketSpacingZ;
        return new Vector3(x, 0f, z);
    }
}
