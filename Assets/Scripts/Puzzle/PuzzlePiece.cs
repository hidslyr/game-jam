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

    /// <summary>
    /// Fill percentage: 0 = empty, 1 = fully filled (cleared).
    /// </summary>
    public float FillPercentage => OriginalAmount > 0
        ? 1f - (float)RemainingAmount / OriginalAmount
        : 0f;

    Renderer meshRenderer;
    SkinnedMeshRenderer skinnedMeshRenderer;
    TextMeshPro tmpText;
    Transform tmpTextTransform; // For ignoring parent rotation

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        OriginalAmount = amount;
        RemainingAmount = amount;
        IsCleared = false;

        // Material swap (on same object as PuzzlePiece)
        meshRenderer = GetComponent<Renderer>();
        SwapMaterial(color);

        // SkinnedMeshRenderer for BlendShape (may be on this or child)
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // Instantiate amount text from Resources
        var amountPrefab = Resources.Load<GameObject>("Prefabs/AmountText");
        if (amountPrefab != null)
        {
            var textGo = Instantiate(amountPrefab, transform);

            // Position at mesh center (not pivot)
            if (meshRenderer != null)
                textGo.transform.position = meshRenderer.bounds.center;
            else
                textGo.transform.localPosition = Vector3.zero;

            tmpText = textGo.GetComponentInChildren<TextMeshPro>();

            // Set the Text child's global z = -10 for always-on-top
            if (tmpText != null)
            {
                tmpTextTransform = tmpText.transform;
                var pos = tmpTextTransform.position;
                tmpTextTransform.position = pos;
            }
        }

        UpdateDisplay();
        UpdateBlendShape(0f, true); // Start at 0% fill
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

        if (RemainingAmount <= 0)
        {
            RemainingAmount = 0;
            SetCleared();
        }

        return leftover;
    }

    public void SetCleared()
    {
        IsCleared = true;
        RemainingAmount = 0;

        UpdateBlendShape(1f, false);

        // Remove piece GO after a short delay for visual feedback
        Destroy(gameObject, BlendShapeTransitionDuration + 0.1f);
    }

    /// <summary>
    /// Highlight this piece as the current active target.
    /// </summary>
    public void SetActive(bool active)
    {
        if (IsCleared) return;
        transform.localScale = active ? Vector3.one * 1.1f : Vector3.one;
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
            tmpText.text = IsCleared ? "✓" : RemainingAmount.ToString();
    }
}
