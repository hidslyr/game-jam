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
    FlexiblePipe flexiblePipe;

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
        flexiblePipe = FindAnyObjectByType<FlexiblePipe>();

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

        // Wait for fly-to-slot animation
        yield return new WaitForSeconds(stagingSlots.FlyInDuration);

        // Run sequential auto-fill
        yield return StartCoroutine(AutoFillSequential());
    }

    IEnumerator AutoFillSequential()
    {
        while (true)
        {
            if (currentPieceIndex >= pieces.Count) break;

            var piece = pieces[currentPieceIndex];

            // Skip cleared pieces
            if (piece == null || piece.IsCleared)
            {
                currentPieceIndex++;
                UpdateActivePiece();
                continue;
            }

            // Find matching basket in staging slots
            int matchIdx = stagingSlots.FindMatchingSlot(piece.Color);
            if (matchIdx == -1) break; // No match — done for now

            int slotAmount = stagingSlots.GetSlotAmount(matchIdx);

            // Move basket from slot to BasketFillingPoint
            var basketGo = stagingSlots.MoveBasketToFillingPoint(matchIdx);
            if (basketGo == null) break;

            // Wait for jump animation
            yield return new WaitForSeconds(stagingSlots.FillingJumpDuration);

            // Short delay before filling
            yield return new WaitForSeconds(stagingSlots.FillDelay);

            // Play pump animation through pipe
            if (flexiblePipe != null)
                yield return flexiblePipe.PlayPump();

            // Fill the piece (BlendShape updates after pump finishes)
            int leftover = piece.Fill(slotAmount);

            // Update debug UI
            MainUI.Instance?.UpdateDebugPiece(currentPieceIndex, piece.RemainingAmount, piece.IsCleared);

            // Basket always destroyed after filling (fully consumed or leftover)
            Destroy(basketGo);

            if (leftover > 0 && !piece.IsCleared)
            {
                // Leftover but piece not cleared — LOSE
                Debug.Log("[PuzzleBoard] LOSE — basket has leftover but can't fill piece!");
                GameManager.Instance?.TriggerLose();
                yield break;
            }

            if (piece.IsCleared)
            {
                currentPieceIndex++;
                UpdateActivePiece();
                // Continue loop — cascade to next piece
            }
        }

        // Check win/lose after cascade finishes
        CheckWinLose();
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
            StartCoroutine(DelayedEndScreen(true));
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
                StartCoroutine(DelayedEndScreen(false));
            }
        }
    }

    IEnumerator DelayedEndScreen(bool isWin)
    {
        yield return new WaitForSeconds(5f);

        if (isWin)
            GameManager.Instance?.TriggerWin();
        else
            GameManager.Instance?.TriggerLose();
    }
    /// <summary>
    /// Returns the current active piece (used by FlexiblePipe).
    /// </summary>
    public PuzzlePiece GetCurrentPiece()
    {
        if (currentPieceIndex >= 0 && currentPieceIndex < pieces.Count)
            return pieces[currentPieceIndex];
        return null;
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
