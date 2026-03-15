using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Component on each puzzle piece (pre-setup in level prefab).
/// Piece GO name = index in chain (1, 2, 3...).
/// Visual: material color + SkinnedMeshRenderer BlendShape for fill progress.
/// Amount text instantiated by script from AmountTextPrefab.
/// </summary>
public class PuzzlePiece : MonoBehaviour
{
    [Header("BlendShape")]
    public float BlendShapeTransitionDuration = 0.3f;

    public GameColor Color { get; private set; }
    public int RemainingAmount { get; private set; }
    public int OriginalAmount { get; private set; }
    public bool IsCleared { get; private set; }
    public int VisualFilledBonus { get; set; } // Shared by parallel fill coroutines

    /// <summary>
    /// Fill percentage: 0 = empty, 1 = fully filled (cleared).
    /// </summary>
    public float FillPercentage => OriginalAmount > 0
        ? 1f - (float)RemainingAmount / OriginalAmount
        : 0f;

    public Vector3 TextOffset = Vector3.zero;

    Renderer meshRenderer;
    SkinnedMeshRenderer skinnedMeshRenderer;
    TextMeshPro tmpText;
    Transform tmpTextTransform; // For ignoring parent rotation
    MaterialPropertyBlock propBlock;
    static readonly int ColorId = Shader.PropertyToID("_BaseColor");
    const float AlphaStart = 1f;        // 255/255
    const float AlphaEnd   = 0.55f;     // ~140/255

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        OriginalAmount = amount;
        RemainingAmount = amount;
        IsCleared = false;

        // Material swap (on same object as PuzzlePiece)
        meshRenderer = GetComponent<Renderer>();
        SwapMaterial(color);
        propBlock = new MaterialPropertyBlock();

        // SkinnedMeshRenderer for BlendShape (may be on this or child)
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // Instantiate amount text from Resources
        var amountPrefab = Resources.Load<GameObject>("Prefabs/AmountText");
        if (amountPrefab != null)
        {
            var textGo = Instantiate(amountPrefab, transform);

            // Position at mesh center + offset
            if (meshRenderer != null)
                textGo.transform.position = meshRenderer.bounds.center + TextOffset;
            else
                textGo.transform.localPosition = TextOffset;

            tmpText = textGo.GetComponentInChildren<TextMeshPro>();

            // Set the Text child's global z = -10 for always-on-top
            if (tmpText != null)
            {
                tmpTextTransform = tmpText.transform;
                var pos = tmpTextTransform.position;
                tmpTextTransform.position = pos + new Vector3(0, 0.2f, 0);
            }
        }

        UpdateDisplay();
        UpdateBlendShape(0f, true); // Start at 0% fill
    }

    void LateUpdate()
    {
        if (tmpTextTransform != null)
            tmpTextTransform.rotation = Quaternion.Euler(25f, 0f, 0f);
    }

    /// <summary>
    /// Fill this piece with the given amount.
    /// Returns leftover (0 = basket fully consumed, >0 = piece cleared with basket remainder).
    /// </summary>
    public int Fill(int amount)
    {
        if (IsCleared) return amount;

        int used = Mathf.Min(amount, RemainingAmount);
        RemainingAmount -= used;
        int leftover = amount - used;

        UpdateDisplay();
        UpdateBlendShape(FillPercentage, false);
        GameManager.Instance?.PlayPieceFillSFX();

        if (RemainingAmount <= 0)
        {
            RemainingAmount = 0;
            SetCleared();
        }

        return leftover;
    }

    /// <summary>
    /// Apply a partial fill without triggering clear check.
    /// Used during gradual pump animation.
    /// </summary>
    public void IncrementalFill(int delta)
    {
        if (IsCleared || delta <= 0) return;
        RemainingAmount = Mathf.Max(0, RemainingAmount - delta);
    }

    /// <summary>
    /// Update blendshape and display for current fill state.
    /// Call each frame during pump animation.
    /// </summary>
    public void UpdateFillVisual()
    {
        float percent = FillPercentage + (float)VisualFilledBonus / Mathf.Max(1, OriginalAmount);
        percent = Mathf.Clamp01(percent);
        UpdateBlendShape(percent, true);
        UpdateAlpha(percent);
        UpdateDisplay();
    }

    void UpdateAlpha(float fillPercent)
    {
        if (meshRenderer == null || propBlock == null) return;
        meshRenderer.GetPropertyBlock(propBlock);
        var baseColor = meshRenderer.sharedMaterial.color;
        float alphaEnd = GameManager.Instance?.AnimConfig != null
            ? GameManager.Instance.AnimConfig.PieceFillAlphaEnd / 255f
            : AlphaEnd;
        float alpha = Mathf.Lerp(AlphaStart, alphaEnd, fillPercent);
        propBlock.SetColor(ColorId, new UnityEngine.Color(baseColor.r, baseColor.g, baseColor.b, alpha));
        meshRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// Finalize fill — check if cleared after all pumps done.
    /// Blendshape is already at target from per-frame UpdateFillVisual().
    /// </summary>
    public void FinalizeFill()
    {
        VisualFilledBonus = 0;
        UpdateDisplay();
        if (RemainingAmount <= 0)
        {
            RemainingAmount = 0;
            SetCleared();
        }
    }

    public void SetCleared()
    {
        IsCleared = true;
        RemainingAmount = 0;

        // Blendshape already at 100% from per-frame updates — just ensure final value
        UpdateBlendShape(1f, true);

        // VFX + SFX + destroy immediately (no delay needed)
        var matColor = meshRenderer != null ? meshRenderer.material.color : UnityEngine.Color.white;
        var vfxPos = meshRenderer != null ? meshRenderer.bounds.center : transform.position;

        // Screen shake only on last piece cleared
        bool isLastPiece = true;
        var allPieces = PuzzleBoard.Instance?.GetAllPieces();
        if (allPieces != null)
        {
            foreach (var p in allPieces)
            {
                if (p != this && !p.IsCleared) { isLastPiece = false; break; }
            }
        }
        GameManager.Instance?.PlayPieceClearEffect(vfxPos, matColor, isLastPiece);
        Destroy(gameObject);
    }

    /// <summary>
    /// Highlight this piece as the current active target.
    /// </summary>
    public void SetActive(bool active)
    {
        if (IsCleared) return;
        // No visual change — kept for future use
    }

    void SwapMaterial(GameColor color)
    {
        if (meshRenderer == null) return;

        string matName;
        switch (color)
        {
            case GameColor.Red:    matName = "mat_Balloon_red"; break;
            case GameColor.Green:  matName = "mat_Balloon_green"; break;
            case GameColor.Blue:   matName = "mat_Balloon_blue"; break;
            case GameColor.Yellow: matName = "mat_Balloon_yellow"; break;
            case GameColor.Purple: matName = "mat_Balloon_purple"; break;
            default:               matName = "mat_Balloon_base"; break;
        }

        // Materials under Assets/Resources/GameJam/Art/Balloon/
        var mat = Resources.Load<Material>($"GameJam/Art/Balloon/{matName}");
        if (mat != null)
            meshRenderer.material = mat;
        else
            Debug.LogWarning($"[PuzzlePiece] Material not found: GameJam/Art/Balloon/{matName}");
    }

    void UpdateBlendShape(float fillPercent, bool instant)
    {
        if (skinnedMeshRenderer == null) return;
        if (skinnedMeshRenderer.sharedMesh == null) return;
        if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0) return;

        float targetWeight = fillPercent * 100f; // BlendShape weight is 0-100

        if (instant)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(0, targetWeight);
        }
        else
        {
            float current = skinnedMeshRenderer.GetBlendShapeWeight(0);
            DOTween.To(
                () => current,
                v => {
                    current = v;
                    skinnedMeshRenderer.SetBlendShapeWeight(0, v);
                },
                targetWeight,
                BlendShapeTransitionDuration
            ).SetEase(Ease.OutQuad);
        }
    }

    void UpdateDisplay()
    {
        if (tmpText != null)
        {
            if (IsCleared)
                tmpText.text = "✓";
            else
            {
                int filled = OriginalAmount - RemainingAmount + VisualFilledBonus;
                if (filled <= 0)
                    tmpText.text = OriginalAmount.ToString();
                else
                    tmpText.text = $"{filled}/{OriginalAmount}";
            }
        }
    }

    public void SetTextVisible(bool visible)
    {
        if (tmpText != null)
            tmpText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Set display text directly (used for gradual fill animation).
    /// </summary>
    public void SetDisplayText(string text)
    {
        if (tmpText != null)
            tmpText.text = text;
    }
}
