using DG.Tweening;
using UnityEngine;

/// <summary>
/// Manages the staging slot array above the grid.
/// Baskets fly into slots, auto-fill runs to match pieces.
/// Attach to a child GO of PuzzleBoard named "StagingSlots".
/// </summary>
public class StagingSlots : MonoBehaviour
{
    [Header("Slot Layout")]
    public float SlotSpacingX = 1.2f;
    public float FlyInDuration = 0.5f;

    [Header("Prefab")]
    public GameObject SlotPrefab; // Visual slot outline

    int slotCount;
    SlotData[] slots;
    Transform[] slotTransforms;
    Transform anchorPoint;

    struct SlotData
    {
        public bool Filled;
        public GameColor Color;
        public int Amount;
        public GameObject Visual; // basket visual sitting in slot
    }

    public void Initialize(int count)
    {
        slotCount = count;
        slots = new SlotData[slotCount];
        slotTransforms = new Transform[slotCount];

        // Find AnchorPoint child as slot origin
        var anchor = transform.Find("AnchorPoint");
        anchorPoint = anchor != null ? anchor : transform;

        // Spawn slot outlines
        for (int i = 0; i < slotCount; i++)
        {
            if (SlotPrefab != null)
            {
                var slotGo = Instantiate(SlotPrefab, transform);
                slotGo.transform.localPosition = GetSlotPosition(i);
                slotTransforms[i] = slotGo.transform;
            }
        }
    }

    /// <summary>
    /// Add a basket (color + amount) to the first empty slot.
    /// Returns slot index, or -1 if full.
    /// Animates the basket GO flying to the slot.
    /// </summary>
    public int AddBasket(GameColor color, int amount, GameObject basketGo)
    {
        int idx = FindEmptySlot();
        if (idx == -1) return -1;

        slots[idx].Filled = true;
        slots[idx].Color = color;
        slots[idx].Amount = amount;
        slots[idx].Visual = basketGo;

        // Reparent and animate fly to slot position
        basketGo.transform.SetParent(transform);
        var targetPos = GetSlotPosition(idx);
        targetPos.y = 0.1f; // Slight Y offset so basket sits above slot (top-down camera)
        basketGo.transform.DOLocalMove(targetPos, FlyInDuration).SetEase(Ease.OutBack);

        return idx;
    }

    /// <summary>
    /// Clear a slot (basket consumed or moved to piece).
    /// </summary>
    public void ClearSlot(int idx)
    {
        if (idx < 0 || idx >= slotCount) return;

        if (slots[idx].Visual != null)
            Destroy(slots[idx].Visual);

        slots[idx] = default;
    }

    /// <summary>
    /// Update the amount on a slot (basket partially consumed).
    /// </summary>
    public void UpdateSlotAmount(int idx, int newAmount)
    {
        if (idx < 0 || idx >= slotCount) return;
        slots[idx].Amount = newAmount;

        // Update basket visual text
        if (slots[idx].Visual != null)
        {
            var basket = slots[idx].Visual.GetComponent<Basket>();
            if (basket != null) basket.SetAmount(newAmount);
        }
    }

    /// <summary>
    /// Find first slot matching the given color. Returns index, or -1 if none.
    /// </summary>
    public int FindMatchingSlot(GameColor color)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].Filled && slots[i].Color == color)
                return i;
        }
        return -1;
    }

    public GameColor GetSlotColor(int idx) => slots[idx].Color;
    public int GetSlotAmount(int idx) => slots[idx].Amount;
    public bool IsSlotFilled(int idx) => slots[idx].Filled;

    public bool IsFull()
    {
        for (int i = 0; i < slotCount; i++)
            if (!slots[i].Filled) return false;
        return true;
    }

    int FindEmptySlot()
    {
        for (int i = 0; i < slotCount; i++)
            if (!slots[i].Filled) return i;
        return -1;
    }

    Vector3 GetSlotPosition(int idx)
    {
        // Offset from AnchorPoint, not StagingSlots center
        var origin = anchorPoint != null ? anchorPoint.localPosition : Vector3.zero;
        float x = origin.x + idx * SlotSpacingX;
        return new Vector3(x, origin.y, origin.z);
    }
}
