using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Component on each basket prefab.
/// Displays color + amount. Clickable when IsPickable = true.
/// Click detection via Physics2D Raycast from Mouse.current.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Basket : MonoBehaviour
{
    public GameColor Color { get; private set; }
    public int Amount { get; private set; }
    public bool IsPickable { get; set; }

    SpriteRenderer spriteRenderer;
    TextMeshPro tmpText;

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        Amount = amount;
        IsPickable = false;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        tmpText = GetComponentInChildren<TextMeshPro>();

        if (spriteRenderer != null)
            spriteRenderer.color = color.ToColor();

        UpdateDisplay();
    }

    /// <summary>
    /// Update amount (e.g. after partial fill, basket stays with leftover).
    /// </summary>
    public void SetAmount(int newAmount)
    {
        Amount = newAmount;
        UpdateDisplay();
    }

    void Update()
    {
        if (!IsPickable) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Raycast from mouse position
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        var hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            PuzzleBoard.Instance?.OnBasketPicked(this);
        }
    }

    void UpdateDisplay()
    {
        if (tmpText != null)
            tmpText.text = Amount.ToString();
    }
}
