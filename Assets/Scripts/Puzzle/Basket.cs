using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Component on each basket prefab.
/// Displays color + amount. Clickable when IsPickable = true.
/// Click detection via Physics2D Raycast from Mouse.current.
/// </summary>
public class Basket : MonoBehaviour
{
    public GameColor Color { get; private set; }
    public int Amount { get; private set; }
    public bool IsPickable { get; set; }

    Renderer meshRenderer;
    TextMeshPro tmpText;

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        Amount = amount;
        IsPickable = false;

        meshRenderer = GetComponentInChildren<Renderer>();
        tmpText = GetComponentInChildren<TextMeshPro>();

        SwapMaterial(color);
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

        // Raycast from mouse position (3D)
        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
        {
            PuzzleBoard.Instance?.OnBasketPicked(this);
        }
    }

    void UpdateDisplay()
    {
        if (tmpText != null)
            tmpText.text = Amount.ToString();
    }

    void SwapMaterial(GameColor color)
    {
        if (meshRenderer == null) return;

        string matName;
        switch (color)
        {
            case GameColor.Red:    matName = "mat_box_red"; break;
            case GameColor.Green:  matName = "mat_box_green"; break;
            case GameColor.Blue:   matName = "mat_box_blue"; break;
            case GameColor.Yellow: matName = "mat_box_yellow"; break;
            case GameColor.Purple: matName = "mat_box_purple"; break;
            default:               matName = "mat_box_base"; break;
        }

        var mat = Resources.Load<Material>($"GameJam/Art/Box/{matName}");
        if (mat != null)
        {
            // Swap only the first material, leave the rest
            var mats = meshRenderer.materials;
            mats[0] = mat;
            meshRenderer.materials = mats;
        }
        else
        {
            Debug.LogWarning($"[Basket] Material not found: GameJam/Art/Box/{matName}");
        }
    }
}
