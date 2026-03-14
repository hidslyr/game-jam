using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

/// <summary>
/// Editor-only utility to import level data from a Google Sheet CSV.
/// Right-click a LevelData asset → "Import from Google Sheet" to fetch and apply.
///
/// CSV Format:
///   Row 1: Puzzle pieces — Color:Amount (e.g. R:3,G:2,B:1,R:1)
///   Row 2: SlotCount (first cell, e.g. 5)
///   Row 3+: Grid rows — Color:Amount or empty cell for E
/// </summary>
public class LevelDataImporter : Editor
{
    const string SHEET_ID = "17mB6worBegQ2Xgi4qXcDpKXYX5M-4twZVDkbrg17KeU";

    static readonly Dictionary<int, int> LevelGids = new Dictionary<int, int>
    {
        { 1, 0 },
        { 2, 129557841 },
        { 3, 449358609 }
    };

    [MenuItem("CONTEXT/LevelData/Import from Google Sheet")]
    static void ImportFromGoogleSheet(MenuCommand command)
    {
        var levelData = command.context as LevelData;
        if (levelData == null) return;
        DoImport(levelData);
    }

    [MenuItem("Assets/Game/Import Level from Google Sheet")]
    static void ImportFromGoogleSheetAssetMenu()
    {
        var levelData = Selection.activeObject as LevelData;
        if (levelData == null)
        {
            Debug.LogError("[LevelDataImporter] Select a LevelData asset first.");
            return;
        }
        DoImport(levelData);
    }

    [MenuItem("Assets/Game/Import Level from Google Sheet", true)]
    static bool ImportFromGoogleSheetValidation()
    {
        return Selection.activeObject is LevelData;
    }

    static void DoImport(LevelData levelData)
    {
        int levelNum = levelData.LevelNumber;
        if (!LevelGids.ContainsKey(levelNum))
        {
            Debug.LogError($"[LevelDataImporter] No gid configured for level {levelNum}.");
            return;
        }

        int gid = LevelGids[levelNum];
        string url = $"https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=csv&gid={gid}";

        Debug.Log($"[LevelDataImporter] Fetching Level {levelNum} from: {url}");

        var request = UnityWebRequest.Get(url);
        var op = request.SendWebRequest();

        // Block until done (editor context, not play mode)
        while (!op.isDone) { }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[LevelDataImporter] Request failed: {request.error}");
            return;
        }

        string csv = request.downloadHandler.text;
        Debug.Log($"[LevelDataImporter] CSV received:\n{csv}");

        ParseAndApply(levelData, csv);

        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[LevelDataImporter] Level {levelNum} imported successfully!");
    }

    static void ParseAndApply(LevelData levelData, string csv)
    {
        // Split into rows, handle \r\n and \n
        var rows = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        if (rows.Length < 2)
        {
            Debug.LogError("[LevelDataImporter] CSV has fewer than 2 rows.");
            return;
        }

        // Row 0: Puzzle pieces — Color:Amount
        var pieceEntries = ParsePieceRow(rows[0]);
        levelData.PuzzlePieces = pieceEntries.ToArray();

        // Row 1: Slot count (first cell)
        var slotCells = SplitRow(rows[1]);
        if (slotCells.Length > 0 && int.TryParse(slotCells[0].Trim(), out int slotCount))
            levelData.SlotCount = slotCount;

        // Row 2+: Grid rows
        var gridRows = new List<GridRow>();
        for (int r = 2; r < rows.Length; r++)
        {
            if (string.IsNullOrWhiteSpace(rows[r])) continue;
            var gridRow = ParseGridRow(rows[r]);
            gridRows.Add(gridRow);
        }
        levelData.GridRows = gridRows.ToArray();
    }

    static List<PieceEntry> ParsePieceRow(string row)
    {
        var entries = new List<PieceEntry>();
        var cells = SplitRow(row);

        foreach (var cell in cells)
        {
            var trimmed = cell.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parsed = ParseColorAmount(trimmed);
            if (parsed.HasValue)
            {
                entries.Add(new PieceEntry
                {
                    Color = parsed.Value.color,
                    Amount = parsed.Value.amount
                });
            }
        }

        return entries;
    }

    static GridRow ParseGridRow(string row)
    {
        var cells = SplitRow(row);
        var baskets = new List<BasketEntry>();

        foreach (var cell in cells)
        {
            var trimmed = cell.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                // Empty cell = empty grid slot
                baskets.Add(new BasketEntry { IsEmpty = true });
            }
            else
            {
                var parsed = ParseColorAmount(trimmed);
                if (parsed.HasValue)
                {
                    baskets.Add(new BasketEntry
                    {
                        IsEmpty = false,
                        Color = parsed.Value.color,
                        Amount = parsed.Value.amount
                    });
                }
                else
                {
                    // Unparseable = treat as empty
                    baskets.Add(new BasketEntry { IsEmpty = true });
                }
            }
        }

        return new GridRow { Baskets = baskets.ToArray() };
    }

    static (GameColor color, int amount)? ParseColorAmount(string text)
    {
        // Format: "R:3" or "G:2" or "B:1"
        var parts = text.Split(':');
        if (parts.Length != 2) return null;

        GameColor color;
        switch (parts[0].Trim().ToUpper())
        {
            case "R": color = GameColor.Red; break;
            case "G": color = GameColor.Green; break;
            case "B": color = GameColor.Blue; break;
            case "Y": color = GameColor.Yellow; break;
            case "P": color = GameColor.Purple; break;
            default: return null;
        }

        if (!int.TryParse(parts[1].Trim(), out int amount)) return null;

        return (color, amount);
    }

    static string[] SplitRow(string row)
    {
        return row.Split(',');
    }
}
