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
    bool isAutoFilling = false;

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
        // Check if there's an empty staging slot BEFORE removing from grid
        if (stagingSlots.IsFull())
        {
            Debug.LogWarning("[PuzzleBoard] No empty slot — basket stays in grid.");
            yield break;
        }

        // Find column and remove from grid
        int col = FindBasketColumn(basket);
        if (col >= 0)
            basketGrid.RemoveTopBasket(col);

        // Disable picking on this basket (it's now heading to a slot)
        basket.IsPickable = false;

        // SFX on basket tap
        GameManager.Instance?.PlayBasketPickSFX();

        // Add to staging slots (fly animation starts here)
        int slotIdx = stagingSlots.AddBasket(basket.Color, basket.Amount, basket.gameObject);
        if (slotIdx == -1)
        {
            Debug.LogWarning("[PuzzleBoard] No empty slot for picked basket!");
            yield break;
        }

        // Wait for fly-to-slot animation
        yield return new WaitForSeconds(stagingSlots.FlyInDuration);

        // Run auto-fill (skip if already running — it will pick up new baskets)
        if (!isAutoFilling)
            yield return StartCoroutine(AutoFillSequential());
    }

    IEnumerator AutoFillSequential()
    {
        isAutoFilling = true;
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

            // Find ALL matching baskets in staging slots
            var matchIndices = stagingSlots.FindAllMatchingSlots(piece.Color);
            if (matchIndices.Count == 0) break; // No match — done for now

            // Wait for fly-in animation
            yield return new WaitForSeconds(stagingSlots.FlyInDuration * 0.5f);

            // Start parallel pump + fill for all matching baskets
            int fillCount = 0;
            int totalFills = matchIndices.Count;

            for (int m = 0; m < matchIndices.Count; m++)
            {
                int slotIdx = matchIndices[m];
                int slotAmount = stagingSlots.GetSlotAmount(slotIdx);
                StartCoroutine(FillFromSlot(piece, slotIdx, slotAmount, () => fillCount++));
            }

            // Wait for all parallel fills to finish
            while (fillCount < totalFills)
                yield return null;

            // Update debug UI
            MainUI.Instance?.UpdateDebugPiece(currentPieceIndex, piece.RemainingAmount, piece.IsCleared);

            if (piece.IsCleared)
            {
                // Extend active pipe through cleared piece
                if (flexiblePipe != null)
                    flexiblePipe.OnPieceCleared();

                currentPieceIndex++;
                UpdateActivePiece();
                // Continue loop — cascade to next piece
            }
        }

        // Check win/lose after cascade finishes
        CheckWinLose();
        isAutoFilling = false;

        // Re-check: new baskets may have arrived while filling
        if (currentPieceIndex < pieces.Count)
        {
            var piece = pieces[currentPieceIndex];
            if (piece != null && !piece.IsCleared)
            {
                var newMatches = stagingSlots.FindAllMatchingSlots(piece.Color);
                if (newMatches.Count > 0)
                    StartCoroutine(AutoFillSequential());
            }
        }
    }

    IEnumerator FillFromSlot(PuzzlePiece piece, int slotIdx, int amount, System.Action onDone)
    {
        var basketGo = stagingSlots.GetSlotVisual(slotIdx);
        Animator basketAnimator = null;
        Basket basketComp = null;
        if (basketGo != null)
        {
            basketAnimator = basketGo.GetComponentInChildren<Animator>();
            basketComp = basketGo.GetComponent<Basket>();
        }

        var animCfg = GameManager.Instance?.AnimConfig;
        int pumpCount = animCfg != null ? animCfg.PumpCount : 1;
        float pumpDelay = animCfg != null ? animCfg.PumpRepeatDelay : 0.1f;

        int used = Mathf.Min(amount, piece.RemainingAmount);
        int totalTransferred = 0;

        for (int p = 0; p < pumpCount; p++)
        {
            // How much this pump transfers
            int pumpShare;
            if (p < pumpCount - 1)
                pumpShare = used / pumpCount;
            else
                pumpShare = used - totalTransferred; // remainder on last pump

            if (basketAnimator != null)
                basketAnimator.SetTrigger("pump");

            // Start pipe pump (non-blocking — we tick gradual fill alongside it)
            Coroutine pumpCo = null;
            if (flexiblePipe != null)
                pumpCo = flexiblePipe.PlayPump();

            // SFX per pump iteration
            GameManager.Instance?.PlayPieceFillSFX();

            // Gradual fill during this pump iteration
            int pumpStartTransferred = totalTransferred;
            float elapsed = 0f;
            float pumpDuration = flexiblePipe != null ? flexiblePipe.PumpDuration : 0.6f;
            float fillSpeed = animCfg != null ? animCfg.FillSpeedMultiplier : 2f;
            float fillTime = pumpDuration / Mathf.Max(fillSpeed, 0.1f);

            while (elapsed < pumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fillTime);
                int targetTransferred = pumpStartTransferred + Mathf.RoundToInt(pumpShare * t);
                int delta = targetTransferred - totalTransferred;

                if (delta > 0)
                {
                    piece.IncrementalFill(delta);
                    totalTransferred += delta;

                    // Update basket display
                    if (basketComp != null)
                        basketComp.SetAmount(amount - totalTransferred);
                }

                // Update piece blendshape + text every frame
                piece.UpdateFillVisual();
                yield return null;
            }

            // Ensure pipe pump finishes
            if (pumpCo != null)
                yield return pumpCo;

            // Wait for basket animator to finish
            if (basketAnimator != null)
            {
                yield return null;
                var stateInfo = basketAnimator.GetCurrentAnimatorStateInfo(0);
                float remaining = stateInfo.length - stateInfo.normalizedTime * stateInfo.length;
                if (remaining > 0f)
                    yield return new WaitForSeconds(remaining);
            }

            if (p < pumpCount - 1)
                yield return new WaitForSeconds(pumpDelay);
        }

        // Finalize — check clear
        piece.FinalizeFill();

        // Basket disappear VFX + SFX
        var disappearPos = stagingSlots.GetSlotVisual(slotIdx)?.transform.position ?? Vector3.zero;
        GameManager.Instance?.PlayBasketDisappearEffect(disappearPos);
        GameManager.Instance?.PlayBasketEmptySFX();
        stagingSlots.ClearSlot(slotIdx);

        onDone?.Invoke();
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
        yield return new WaitForSeconds(3f);

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

    public List<PuzzlePiece> GetAllPieces()
    {
        return pieces;
    }

    void UpdateActivePiece()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].SetActive(i == currentPieceIndex && !pieces[i].IsCleared);
            // Only current and next piece show text
            bool showText = (i == currentPieceIndex || i == currentPieceIndex + 1) && !pieces[i].IsCleared;
            pieces[i].SetTextVisible(showText);
        }
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
