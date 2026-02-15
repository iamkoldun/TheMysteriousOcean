using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages slots: hotbar (first HotbarSlotCount) + optional extra slots (e.g. from backpack upgrade).
/// Heavy items occupy 2 adjacent slots. Slots store Item reference (real object, not despawned).
/// </summary>
public class Inventory : MonoBehaviour
{
    public const int HotbarSlotCount = 4;

    [SerializeField] private int extraSlotCount = 0;

    [SerializeField] private List<InventorySlotState> _slots = new List<InventorySlotState>();
    [SerializeField] private int _activeHotbarIndex = 0;

    public int TotalSlotCount => HotbarSlotCount + extraSlotCount;
    public int ActiveHotbarIndex => _activeHotbarIndex;
    public event Action<int> OnSlotChanged;
    public event Action<int> OnActiveHotbarChanged;

    private void Awake()
    {
        _slots.Clear();
        for (int i = 0; i < TotalSlotCount; i++)
            _slots.Add(new InventorySlotState { index = i });
    }

    public void SetExtraSlots(int count)
    {
        if (count == extraSlotCount) return;
        extraSlotCount = Mathf.Max(0, count);
        int newTotal = HotbarSlotCount + extraSlotCount;
        while (_slots.Count < newTotal)
            _slots.Add(new InventorySlotState { index = _slots.Count });
        while (_slots.Count > newTotal)
            _slots.RemoveAt(_slots.Count - 1);
    }

    public InventorySlotState GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return default;
        return _slots[index];
    }

    public bool IsSlotOccupiedByPreviousHalf(int index)
    {
        if (index <= 0 || index >= _slots.Count) return false;
        var prev = _slots[index - 1];
        return prev.hasItem && prev.slotCount > 1 && prev.startIndex == index - 1;
    }

    public int FindHotbarSpaceFor(int slotCount)
    {
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (i >= _slots.Count) break;
            if (slotCount == 1)
            {
                if (!_slots[i].hasItem && !IsSlotOccupiedByPreviousHalf(i))
                    return i;
            }
            else
            {
                if (i + 1 >= HotbarSlotCount) continue;
                if (i + 1 >= _slots.Count) continue;
                if (!_slots[i].hasItem && !_slots[i + 1].hasItem
                    && !IsSlotOccupiedByPreviousHalf(i) && !IsSlotOccupiedByPreviousHalf(i + 1))
                    return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Add item to hotbar. Item is not destroyed; its snapPoint will be attached to hand when active.
    /// </summary>
    public bool AddToHotbar(Item item)
    {
        if (item == null) return false;
        int start = FindHotbarSpaceFor(item.SlotCount);
        if (start < 0) return false;
        _slots[start] = new InventorySlotState
        {
            index = start,
            hasItem = true,
            startIndex = start,
            slotCount = item.SlotCount,
            item = item
        };
        if (item.SlotCount > 1)
            _slots[start + 1] = new InventorySlotState { index = start + 1, isSecondHalf = true, startIndex = start };
        OnSlotChanged?.Invoke(start);
        if (item.SlotCount > 1) OnSlotChanged?.Invoke(start + 1);
        return true;
    }

    public void SetActiveHotbarIndex(int index)
    {
        if (index < 0 || index >= HotbarSlotCount) return;
        if (_activeHotbarIndex == index) return;
        _activeHotbarIndex = index;
        OnActiveHotbarChanged?.Invoke(index);
    }

    public int GetActiveHotbarIndex() => _activeHotbarIndex;

    public InventorySlotState GetActiveSlot()
    {
        int i = _activeHotbarIndex;
        if (IsSlotOccupiedByPreviousHalf(i))
            i--;
        return GetSlot(i);
    }

    public void RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count) return;
        var s = _slots[slotIndex];
        if (s.isSecondHalf)
        {
            int start = s.startIndex;
            _slots[start] = new InventorySlotState { index = start };
            _slots[slotIndex] = new InventorySlotState { index = slotIndex };
            OnSlotChanged?.Invoke(start);
            OnSlotChanged?.Invoke(slotIndex);
        }
        else if (s.hasItem)
        {
            int count = s.slotCount;
            _slots[slotIndex] = new InventorySlotState { index = slotIndex };
            if (count > 1 && slotIndex + 1 < _slots.Count)
            {
                _slots[slotIndex + 1] = new InventorySlotState { index = slotIndex + 1 };
                OnSlotChanged?.Invoke(slotIndex + 1);
            }
            OnSlotChanged?.Invoke(slotIndex);
        }
    }
}

[System.Serializable]
public struct InventorySlotState
{
    public int index;
    public bool hasItem;
    public bool isSecondHalf;
    public int startIndex;
    public int slotCount;
    public Item item;
}
