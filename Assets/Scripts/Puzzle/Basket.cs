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

    MeshRenderer[] meshRenderer;
    TextMeshPro tmpText;

    public void Initialize(GameColor color, int amount)
    {
        Color = color;
        Amount = amount;
        IsPickable = false;

        meshRenderer = GetComponentsInChildren<MeshRenderer>();
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

    static bool pickedThisFrame;

    void LateUpdate() => pickedThisFrame = false;

    void Update()
    {
        if (!IsPickable) return;
        if (pickedThisFrame) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // Raycast from mouse position (3D, BoxCollider)
        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);

        // RaycastAll to handle overlapping colliders
        var hits = Physics.RaycastAll(ray);
        foreach (var hit in hits)
        {
            // Check if hit collider is on this GO or any child
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                pickedThisFrame = true;
                PuzzleBoard.Instance?.OnBasketPicked(this);
                break;
            }
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
            var mats = meshRenderer[0].materials;
            mats[0] = mat;
            meshRenderer[0].materials = mats;

            // Also swap for other renderers (e.g. outline)
            var mats2 = meshRenderer[1].materials;
            mats2[0] = mat;
            meshRenderer[1].materials = mats2;
        }
        else
        {
            Debug.LogWarning($"[Basket] Material not found: GameJam/Art/Box/{matName}");
        }
    }

    public void SetOutline(bool visible)
    {
        if (meshRenderer != null && meshRenderer.Length > 1)
            meshRenderer[1].enabled = visible;
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public void SetPickableVisual(bool pickable)
    {
        var circle = transform.Find("Circle");
        if (circle != null)
            circle.gameObject.SetActive(pickable);
    }
}
