using TMPro;
using UnityEngine;

/// <summary>
/// Component on each puzzle piece prefab.
/// Displays color + remaining amount. Filled by baskets via AutoFill.
/// </summary>
public class PuzzlePiece : MonoBehaviour
{
    public GameColor Color { get; private set; }
    public int RemainingAmount { get; private set; }
    public bool IsCleared { get; private set; }

    SpriteRenderer spriteRenderer;
    TextMeshPro tmpText;

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        RemainingAmount = amount;
        IsCleared = false;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        tmpText = GetComponentInChildren<TextMeshPro>();

        if (spriteRenderer != null)
            spriteRenderer.color = color.ToColor();

        UpdateDisplay();
    }

    /// <summary>
    /// Fill this piece with the given amount.
    /// Returns leftover amount (0 if basket fully consumed, >0 if piece cleared and basket has remainder).
    /// </summary>
    public int Fill(int amount)
    {
        if (IsCleared) return amount;

        int used = Mathf.Min(amount, RemainingAmount);
        RemainingAmount -= used;
        int leftover = amount - used;

        if (RemainingAmount <= 0)
        {
            RemainingAmount = 0;
            SetCleared();
        }

        UpdateDisplay();
        return leftover;
    }

    public void SetCleared()
    {
        IsCleared = true;
        RemainingAmount = 0;

        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 0.3f;
            spriteRenderer.color = c;
        }

        transform.localScale = Vector3.one * 0.85f;
        UpdateDisplay();
    }

    /// <summary>
    /// Highlight this piece as the current active target.
    /// </summary>
    public void SetActive(bool active)
    {
        if (IsCleared) return;
        transform.localScale = active ? Vector3.one * 1.1f : Vector3.one;
    }

    void UpdateDisplay()
    {
        if (tmpText != null)
            tmpText.text = IsCleared ? "✓" : RemainingAmount.ToString();
    }
}
