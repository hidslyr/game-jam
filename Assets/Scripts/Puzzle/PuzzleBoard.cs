using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton orchestrator for the puzzle gameplay.
/// Finds child GOs (BasketGrid, StagingSlots, PuzzleChain) in scene.
/// Spawns basket/slot/piece instances into those containers.
/// </summary>
public class PuzzleBoard : MonoBehaviour
{
    public static PuzzleBoard Instance { get; private set; }

    [Header("Piece Layout")]
    public float PieceSpacingX = 1.4f;

    // References to child containers (found by name)
    BasketGrid basketGrid;
    StagingSlots stagingSlots;
    Transform puzzleChainParent;

    // Runtime state
    List<PuzzlePiece> pieces = new List<PuzzlePiece>();
    int currentPieceIndex = 0;
    GameObject pieceChainInstance; // The instantiated level chain prefab

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Find child containers
        basketGrid = GetComponentInChildren<BasketGrid>();
        stagingSlots = GetComponentInChildren<StagingSlots>();

        var chainGo = transform.Find("PuzzleChain");
        if (chainGo != null) puzzleChainParent = chainGo;
    }

    void Start()
    {
        var levelData = GameManager.Instance?.CurrentLevelData;
        if (levelData != null)
            InitializeFromLevelData(levelData);
        else
            Debug.LogWarning("[PuzzleBoard] No LevelData found on GameManager.");
    }

    public void InitializeFromLevelData(LevelData data)
    {
        // Clear previous
        ClearAll();

        // Load and instantiate the level's piece chain prefab
        int levelNum = data.LevelNumber;
        var chainPrefab = Resources.Load<GameObject>($"Prefabs/PieceLv{levelNum}");
        if (chainPrefab == null)
        {
            Debug.LogError($"[PuzzleBoard] Prefab not found: Resources/Prefabs/PieceLv{levelNum}");
            return;
        }

        pieceChainInstance = Instantiate(chainPrefab, puzzleChainParent != null ? puzzleChainParent : transform);
        pieceChainInstance.transform.localPosition = Vector3.zero;

        // Find all PuzzlePiece children, sorted by GO name as int (1, 2, 3...)
        var foundPieces = pieceChainInstance.GetComponentsInChildren<PuzzlePiece>();
        var sortedPieces = new List<PuzzlePiece>(foundPieces);
        sortedPieces.Sort((a, b) =>
        {
            int.TryParse(a.gameObject.name, out int idxA);
            int.TryParse(b.gameObject.name, out int idxB);
            return idxA.CompareTo(idxB);
        });

        // Initialize each piece with LevelData color + amount
        for (int i = 0; i < sortedPieces.Count && i < data.PuzzlePieces.Length; i++)
        {
            var entry = data.PuzzlePieces[i];
            sortedPieces[i].Initialize(entry.Color, entry.Amount);
            pieces.Add(sortedPieces[i]);
        }

        // Initialize basket grid
        if (basketGrid != null)
            basketGrid.Initialize(data.GridRows);

        // Initialize staging slots
        if (stagingSlots != null)
            stagingSlots.Initialize(data.SlotCount);

        // Mark first piece as active
        currentPieceIndex = 0;
        UpdateActivePiece();

        Debug.Log($"[PuzzleBoard] Initialized: {pieces.Count} pieces from PieceLv{levelNum}, slotCount={data.SlotCount}");
    }

    /// <summary>
    /// Called by Basket when clicked.
    /// </summary>
    public void OnBasketPicked(Basket basket)
    {
        if (stagingSlots == null) return;
        StartCoroutine(HandleBasketPicked(basket));
    }

    IEnumerator HandleBasketPicked(Basket basket)
    {
        // Find column and remove from grid
        int col = FindBasketColumn(basket);
        if (col >= 0)
            basketGrid.RemoveTopBasket(col);

        // Disable picking on this basket (it's now heading to a slot)
        basket.IsPickable = false;

        // Add to staging slots (fly animation starts here)
        int slotIdx = stagingSlots.AddBasket(basket.Color, basket.Amount, basket.gameObject);
        if (slotIdx == -1)
        {
            Debug.LogWarning("[PuzzleBoard] No empty slot for picked basket!");
            yield break;
        }

        // Wait for fly animation to finish before auto-fill
        yield return new WaitForSeconds(stagingSlots.FlyInDuration);

        // Run auto-fill cascade
        AutoFill();

        // Check win/lose
        CheckWinLose();
    }

    void AutoFill()
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            if (currentPieceIndex >= pieces.Count) break;

            var piece = pieces[currentPieceIndex];
            if (piece.IsCleared)
            {
                currentPieceIndex++;
                UpdateActivePiece();
                changed = true;
                continue;
            }

            // Find a staging slot matching current piece color
            int matchIdx = stagingSlots.FindMatchingSlot(piece.Color);
            if (matchIdx == -1) break;

            int slotAmount = stagingSlots.GetSlotAmount(matchIdx);
            int leftover = piece.Fill(slotAmount);

            // Always re-check after any fill operation
            changed = true;

            if (leftover <= 0)
            {
                // Basket fully consumed
                stagingSlots.ClearSlot(matchIdx);
            }
            else
            {
                // Basket has leftover — stays in slot with updated amount
                stagingSlots.UpdateSlotAmount(matchIdx, leftover);
            }

            if (piece.IsCleared)
            {
                currentPieceIndex++;
                UpdateActivePiece();
                // Continue loop — cascade to next piece
            }
        }
    }

    void CheckWinLose()
    {
        // Win: all pieces cleared
        bool allCleared = true;
        foreach (var p in pieces)
        {
            if (!p.IsCleared) { allCleared = false; break; }
        }

        if (allCleared)
        {
            Debug.Log("[PuzzleBoard] WIN — all pieces cleared!");
            GameManager.Instance?.TriggerWin();
            return;
        }

        // Lose: all slots full + no matching slot for current piece
        if (stagingSlots.IsFull())
        {
            var currentPiece = pieces[currentPieceIndex];
            int match = stagingSlots.FindMatchingSlot(currentPiece.Color);
            if (match == -1)
            {
                Debug.Log("[PuzzleBoard] LOSE — all slots full, no match!");
                GameManager.Instance?.TriggerLose();
            }
        }
    }

    void UpdateActivePiece()
    {
        for (int i = 0; i < pieces.Count; i++)
            pieces[i].SetActive(i == currentPieceIndex && !pieces[i].IsCleared);
    }

    int FindBasketColumn(Basket basket)
    {
        if (basketGrid == null) return -1;
        for (int c = 0; c < basketGrid.ColumnCount; c++)
        {
            if (basketGrid.GetTopBasket(c) == basket)
                return c;
        }
        return -1;
    }

    void ClearAll()
    {
        if (pieceChainInstance != null)
            Destroy(pieceChainInstance);

        pieces.Clear();
        currentPieceIndex = 0;
    }
}
