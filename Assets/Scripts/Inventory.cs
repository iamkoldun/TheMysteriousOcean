using System;
using System.Collections.Generic;
using UnityEngine;

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

[System.Serializable]
public class InventoryExpansion
{
    public string name;
    public int slotCount;
    [Tooltip("UI prefab for this expansion section in the hotbar. Must have HotbarSectionUI. If null, HotbarUI uses its default expansion prefab.")]
    public GameObject sectionUIPrefab;
    [SerializeField] internal List<InventorySlotState> slots = new List<InventorySlotState>();
}

/// <summary>
/// Two hand slots (0 = right / main, 1 = left) plus a list of InventoryExpansion,
/// each with its own isolated slot array. Heavy items (2 slots) must fit entirely
/// within a single expansion (or both hands).
/// </summary>
public class Inventory : MonoBehaviour
{
    public const int HandSlotCount = 2;
    public const int RightHand = 0;
    public const int LeftHand = 1;

    [SerializeField] private InventorySlotState[] _handSlots = new InventorySlotState[2];
    [SerializeField] private List<InventoryExpansion> _expansions = new List<InventoryExpansion>();

    public IReadOnlyList<InventoryExpansion> Expansions => _expansions;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        InitializeHandSlots();
        InitializeExpansionSlots();
        OnInventoryChanged?.Invoke();
    }

    private void InitializeHandSlots()
    {
        _handSlots = new InventorySlotState[HandSlotCount];
        for (int i = 0; i < HandSlotCount; i++)
            _handSlots[i] = new InventorySlotState { index = i };
    }

    private void InitializeExpansionSlots()
    {
        for (int e = 0; e < _expansions.Count; e++)
        {
            var exp = _expansions[e];
            exp.slots.Clear();
            for (int i = 0; i < exp.slotCount; i++)
                exp.slots.Add(new InventorySlotState { index = i });
        }
    }

    // ── Display helpers (unified index: 0,1 = hands, 2+ = expansion slots in order) ──

    public int GetDisplaySlotCount()
    {
        int total = HandSlotCount;
        for (int i = 0; i < _expansions.Count; i++)
            total += _expansions[i].slotCount;
        return total;
    }

    public int GetTotalExpansionSlotCount()
    {
        int total = 0;
        for (int i = 0; i < _expansions.Count; i++)
            total += _expansions[i].slotCount;
        return total;
    }

    public InventorySlotState GetDisplaySlot(int displayIndex)
    {
        if (displayIndex < 0) return default;
        if (displayIndex < HandSlotCount)
            return _handSlots[displayIndex];
        if (ResolveExpansionIndex(displayIndex, out int ei, out int si))
            return _expansions[ei].slots[si];
        return default;
    }

    public bool IsDisplaySlotSecondHalf(int displayIndex)
    {
        if (displayIndex < 0) return false;
        if (displayIndex < HandSlotCount)
        {
            if (displayIndex == LeftHand)
            {
                var r = _handSlots[RightHand];
                return r.hasItem && r.slotCount > 1;
            }
            return false;
        }
        if (ResolveExpansionIndex(displayIndex, out int ei, out int si))
        {
            var slot = _expansions[ei].slots[si];
            return slot.isSecondHalf;
        }
        return false;
    }

    /// <summary>
    /// Map flat expansion-slot index (0-based, hands excluded) to (expansionIndex, slotInExpansion).
    /// expansionSlotIndex 0 = first slot of first expansion.
    /// </summary>
    public bool ResolveExpansionSlotIndex(int expansionSlotIndex, out int expansionIndex, out int slotInExpansion)
    {
        expansionIndex = -1;
        slotInExpansion = -1;
        int cursor = 0;
        for (int e = 0; e < _expansions.Count; e++)
        {
            int count = _expansions[e].slotCount;
            if (expansionSlotIndex < cursor + count)
            {
                expansionIndex = e;
                slotInExpansion = expansionSlotIndex - cursor;
                return true;
            }
            cursor += count;
        }
        return false;
    }

    private bool ResolveExpansionIndex(int displayIndex, out int ei, out int si)
    {
        return ResolveExpansionSlotIndex(displayIndex - HandSlotCount, out ei, out si);
    }

    // ── Hand slots ──

    public InventorySlotState GetHandSlot(int handIndex)
    {
        if (handIndex < 0 || handIndex >= HandSlotCount) return default;
        return _handSlots[handIndex];
    }

    public void SetHandItem(int handIndex, Item item)
    {
        if (handIndex < 0 || handIndex >= HandSlotCount) return;
        if (item == null)
        {
            _handSlots[handIndex] = new InventorySlotState { index = handIndex };
        }
        else
        {
            _handSlots[handIndex] = new InventorySlotState
            {
                index = handIndex,
                hasItem = true,
                startIndex = handIndex,
                slotCount = 1,
                item = item
            };
        }
        NotifyChanged();
    }

    /// <summary>
    /// Place a heavy (2-slot) item into both hands. Caller must ensure hands are empty first.
    /// </summary>
    public void SetBothHandsItem(Item item)
    {
        if (item == null) return;
        _handSlots[RightHand] = new InventorySlotState
        {
            index = RightHand,
            hasItem = true,
            startIndex = RightHand,
            slotCount = 2,
            item = item
        };
        _handSlots[LeftHand] = new InventorySlotState
        {
            index = LeftHand,
            isSecondHalf = true,
            startIndex = RightHand
        };
        NotifyChanged();
    }

    public void ClearHand(int handIndex)
    {
        if (handIndex < 0 || handIndex >= HandSlotCount) return;
        _handSlots[handIndex] = new InventorySlotState { index = handIndex };
        NotifyChanged();
    }

    public void ClearBothHands()
    {
        _handSlots[RightHand] = new InventorySlotState { index = RightHand };
        _handSlots[LeftHand] = new InventorySlotState { index = LeftHand };
        NotifyChanged();
    }

    public bool IsHoldingHeavy()
    {
        return _handSlots[RightHand].hasItem && _handSlots[RightHand].slotCount > 1;
    }

    /// <summary>
    /// Returns the item currently held: right-hand item, or the heavy item spanning both hands.
    /// </summary>
    public InventorySlotState GetActiveSlot()
    {
        var right = _handSlots[RightHand];
        if (right.hasItem) return right;
        return _handSlots[LeftHand];
    }

    /// <summary>
    /// Returns the right-hand item specifically.
    /// </summary>
    public Item GetRightHandItem()
    {
        var s = _handSlots[RightHand];
        return s.hasItem ? s.item : null;
    }

    public Item GetLeftHandItem()
    {
        var s = _handSlots[LeftHand];
        return (s.hasItem && !s.isSecondHalf) ? s.item : null;
    }

    // ── Expansion slots ──

    public InventorySlotState GetExpansionSlot(int expansionIndex, int slotIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return default;
        var exp = _expansions[expansionIndex];
        if (slotIndex < 0 || slotIndex >= exp.slots.Count) return default;
        return exp.slots[slotIndex];
    }

    public void SetExpansionSlot(int expansionIndex, int slotIndex, Item item)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return;
        var exp = _expansions[expansionIndex];
        if (slotIndex < 0 || slotIndex >= exp.slots.Count) return;
        if (item == null)
        {
            exp.slots[slotIndex] = new InventorySlotState { index = slotIndex };
        }
        else
        {
            exp.slots[slotIndex] = new InventorySlotState
            {
                index = slotIndex,
                hasItem = true,
                startIndex = slotIndex,
                slotCount = item.SlotCount,
                item = item
            };
            if (item.SlotCount > 1 && slotIndex + 1 < exp.slots.Count)
            {
                exp.slots[slotIndex + 1] = new InventorySlotState
                {
                    index = slotIndex + 1,
                    isSecondHalf = true,
                    startIndex = slotIndex
                };
            }
        }
        NotifyChanged();
    }

    public void ClearExpansionSlot(int expansionIndex, int slotIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return;
        var exp = _expansions[expansionIndex];
        if (slotIndex < 0 || slotIndex >= exp.slots.Count) return;

        var s = exp.slots[slotIndex];
        if (s.isSecondHalf)
        {
            int start = s.startIndex;
            exp.slots[start] = new InventorySlotState { index = start };
            exp.slots[slotIndex] = new InventorySlotState { index = slotIndex };
        }
        else if (s.hasItem)
        {
            int count = s.slotCount;
            exp.slots[slotIndex] = new InventorySlotState { index = slotIndex };
            if (count > 1 && slotIndex + 1 < exp.slots.Count)
                exp.slots[slotIndex + 1] = new InventorySlotState { index = slotIndex + 1 };
        }
        NotifyChanged();
    }

    // ── Find space & add to expansion ──

    /// <summary>
    /// Find first expansion with enough consecutive free slots (compacts per expansion if needed). Returns false if none found.
    /// </summary>
    public bool FindSpaceInAnyExpansion(int slotCount, out int expansionIndex, out int startSlot)
    {
        expansionIndex = -1;
        startSlot = -1;
        for (int e = 0; e < _expansions.Count; e++)
        {
            int found = FindSpaceInExpansionWithCompact(e, slotCount);
            if (found >= 0)
            {
                expansionIndex = e;
                startSlot = found;
                return true;
            }
        }
        return false;
    }

    private int CountFreeSlotsInExpansion(int expansionIndex)
    {
        var exp = _expansions[expansionIndex];
        int n = 0;
        for (int i = 0; i < exp.slots.Count; i++)
        {
            var s = exp.slots[i];
            if (!s.hasItem && !s.isSecondHalf) n++;
        }
        return n;
    }

    private int FindSpaceInExpansion(int expansionIndex, int slotCount)
    {
        var exp = _expansions[expansionIndex];
        for (int i = 0; i <= exp.slots.Count - slotCount; i++)
        {
            bool fits = true;
            for (int j = 0; j < slotCount; j++)
            {
                var s = exp.slots[i + j];
                if (s.hasItem || s.isSecondHalf) { fits = false; break; }
            }
            if (fits) return i;
        }
        return -1;
    }

    /// <summary>
    /// Compacts one expansion so all items are at the start, free slots at the end.
    /// Keeps expansion isolated (no cross-expansion moves).
    /// </summary>
    public void CompactExpansion(int expansionIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return;
        var exp = _expansions[expansionIndex];
        var items = new List<(Item item, int slotCount)>();
        for (int i = 0; i < exp.slots.Count; i++)
        {
            var s = exp.slots[i];
            if (s.hasItem && !s.isSecondHalf && s.item != null)
                items.Add((s.item, s.slotCount));
        }
        for (int i = 0; i < exp.slots.Count; i++)
            exp.slots[i] = new InventorySlotState { index = i };
        int pos = 0;
        foreach (var (item, slotCount) in items)
        {
            if (pos + slotCount <= exp.slots.Count)
            {
                PlaceItemInExpansionRaw(exp, pos, item);
                pos += slotCount;
            }
        }
        NotifyChanged();
    }

    /// <summary>
    /// Find consecutive free slots in one expansion; if none, compact and try again. Returns start index or -1.
    /// </summary>
    private int FindSpaceInExpansionWithCompact(int expansionIndex, int slotCount)
    {
        int found = FindSpaceInExpansion(expansionIndex, slotCount);
        if (found >= 0) return found;
        CompactExpansion(expansionIndex);
        return FindSpaceInExpansion(expansionIndex, slotCount);
    }

    /// <summary>
    /// Try to put item in hands first (right hand for 1-slot, both for 2-slot). Returns true if placed.
    /// </summary>
    public bool TryAddToHands(Item item)
    {
        if (item == null) return false;
        if (item.SlotCount == 1)
        {
            if (GetRightHandItem() != null || IsHoldingHeavy()) return false;
            _handSlots[RightHand] = new InventorySlotState
            {
                index = RightHand,
                hasItem = true,
                startIndex = RightHand,
                slotCount = 1,
                item = item
            };
            NotifyChanged();
            return true;
        }
        if (item.SlotCount == 2)
        {
            if (GetRightHandItem() != null || GetLeftHandItem() != null) return false;
            SetBothHandsItem(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add an item to the first expansion that has space. Returns true on success.
    /// </summary>
    public bool AddToExpansion(Item item)
    {
        if (item == null) return false;
        if (!FindSpaceInAnyExpansion(item.SlotCount, out int ei, out int si))
            return false;
        SetExpansionSlot(ei, si, item);
        return true;
    }

    /// <summary>
    /// Move item from hands into the given expansion. For 1-slot uses slotIndex if free; for 2-slot finds 2 consecutive in that expansion (compacts if needed).
    /// Returns true on success.
    /// </summary>
    public bool MoveHandsToExpansionSlot(int expansionIndex, int slotIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return false;
        var exp = _expansions[expansionIndex];
        if (slotIndex < 0 || slotIndex >= exp.slots.Count) return false;

        Item handItem = null;
        int slotCount = 0;
        if (IsHoldingHeavy())
        {
            handItem = _handSlots[RightHand].item;
            slotCount = 2;
        }
        else
        {
            handItem = GetRightHandItem();
            if (handItem == null) return false;
            slotCount = handItem.SlotCount;
        }

        int placeAt = -1;
        if (slotCount == 1)
        {
            if (!exp.slots[slotIndex].hasItem && !exp.slots[slotIndex].isSecondHalf)
                placeAt = slotIndex;
        }
        else
        {
            placeAt = FindSpaceInExpansionWithCompact(expansionIndex, 2);
        }
        if (placeAt < 0) return false;

        if (IsHoldingHeavy())
            ClearBothHandsRaw();
        else
            _handSlots[RightHand] = new InventorySlotState { index = RightHand };
        PlaceItemInExpansionRaw(exp, placeAt, handItem);
        NotifyChanged();
        return true;
    }

    // ── Expansion management ──

    public void AddExpansion(string expansionName, int slotCount)
    {
        var exp = new InventoryExpansion { name = expansionName, slotCount = slotCount };
        for (int i = 0; i < slotCount; i++)
            exp.slots.Add(new InventorySlotState { index = i });
        _expansions.Add(exp);
        NotifyChanged();
    }

    public void RemoveExpansion(int expansionIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return;
        _expansions.RemoveAt(expansionIndex);
        NotifyChanged();
    }

    // ── Swap logic ──

    /// <summary>
    /// Move item from an expansion slot into hands. If hands have items, swap them back
    /// into the same expansion slots.
    /// </summary>
    public void MoveExpansionSlotToHands(int expansionIndex, int slotIndex)
    {
        if (expansionIndex < 0 || expansionIndex >= _expansions.Count) return;
        var exp = _expansions[expansionIndex];
        if (slotIndex < 0 || slotIndex >= exp.slots.Count) return;

        var slot = exp.slots[slotIndex];
        if (slot.isSecondHalf)
        {
            slotIndex = slot.startIndex;
            slot = exp.slots[slotIndex];
        }
        if (!slot.hasItem || slot.item == null) return;

        Item expItem = slot.item;
        int expItemSlots = slot.slotCount;

        if (expItemSlots > 1)
        {
            Item rightItem = GetRightHandItem();
            Item leftItem = GetLeftHandItem();
            bool holdingHeavy = IsHoldingHeavy();

            ClearExpansionSlotRaw(exp, slotIndex, expItemSlots);

            if (holdingHeavy)
            {
                Item heavyItem = _handSlots[RightHand].item;
                ClearBothHandsRaw();
                SetBothHandsItem(expItem);
                PlaceItemInExpansionRaw(exp, slotIndex, heavyItem);
            }
            else
            {
                if (rightItem != null)
                    PlaceItemInExpansionRaw(exp, slotIndex, rightItem);
                if (leftItem != null && slotIndex + 1 < exp.slots.Count && !exp.slots[slotIndex + 1].hasItem && !exp.slots[slotIndex + 1].isSecondHalf)
                    PlaceItemInExpansionRaw(exp, slotIndex + 1, leftItem);
                else if (leftItem != null)
                    TryPutItemBackInAnyExpansion(leftItem);

                ClearBothHandsRaw();
                SetBothHandsItem(expItem);
            }
        }
        else
        {
            Item rightItem = GetRightHandItem();
            bool holdingHeavy = IsHoldingHeavy();

            if (holdingHeavy)
            {
                int putStart = FindSpaceInExpansionWithCompact(expansionIndex, 2);
                if (putStart >= 0)
                {
                    Item heavyItem = _handSlots[RightHand].item;
                    ClearBothHandsRaw();
                    PlaceItemInExpansionRaw(exp, putStart, heavyItem);
                    ClearExpansionSlotRaw(exp, slotIndex, 1);
                }
                else
                {
                    // Два предмета в руки только если в этом экстра-инвентаре нет двух свободных слотов (даже после компакта)
                    int freeInExpansion = CountFreeSlotsInExpansion(expansionIndex);
                    if (freeInExpansion >= 2)
                    {
                        CompactExpansion(expansionIndex);
                        int slotForHeavy = FindSpaceInExpansion(expansionIndex, 2);
                        if (slotForHeavy >= 0)
                        {
                            Item heavyToStore = _handSlots[RightHand].item;
                            ClearBothHandsRaw();
                            PlaceItemInExpansionRaw(exp, slotForHeavy, heavyToStore);
                            ClearExpansionSlotRaw(exp, slotIndex, 1);
                            _handSlots[RightHand] = new InventorySlotState { index = RightHand, hasItem = true, startIndex = RightHand, slotCount = 1, item = expItem };
                            NotifyChanged();
                            return;
                        }
                    }
                    int otherIndex = -1;
                    for (int i = 0; i < exp.slots.Count; i++)
                    {
                        if (i == slotIndex) continue;
                        var s = exp.slots[i];
                        if (s.hasItem && !s.isSecondHalf && s.slotCount == 1) { otherIndex = i; break; }
                    }
                    if (otherIndex < 0) return;
                    Item light2 = exp.slots[otherIndex].item;
                    Item heavyItem = _handSlots[RightHand].item;
                    ClearBothHandsRaw();
                    ClearExpansionSlotRaw(exp, slotIndex, 1);
                    ClearExpansionSlotRaw(exp, otherIndex, 1);
                    _handSlots[RightHand] = new InventorySlotState { index = RightHand, hasItem = true, startIndex = RightHand, slotCount = 1, item = expItem };
                    _handSlots[LeftHand] = new InventorySlotState { index = LeftHand, hasItem = true, startIndex = LeftHand, slotCount = 1, item = light2 };
                    CompactExpansion(expansionIndex);
                    int twoFree = FindSpaceInExpansion(expansionIndex, 2);
                    if (twoFree < 0) return;
                    PlaceItemInExpansionRaw(exp, twoFree, heavyItem);
                    NotifyChanged();
                    return;
                }
            }
            else
            {
                ClearExpansionSlotRaw(exp, slotIndex, 1);
                if (rightItem != null)
                    PlaceItemInExpansionRaw(exp, slotIndex, rightItem);
            }

            _handSlots[RightHand] = new InventorySlotState
            {
                index = RightHand,
                hasItem = true,
                startIndex = RightHand,
                slotCount = 1,
                item = expItem
            };
        }

        NotifyChanged();
    }

    /// <summary>
    /// Swap right and left hand contents. Heavy items stay put.
    /// </summary>
    public void SwapHands()
    {
        if (IsHoldingHeavy()) return;

        var tmp = _handSlots[RightHand];
        _handSlots[RightHand] = _handSlots[LeftHand];
        _handSlots[LeftHand] = tmp;

        _handSlots[RightHand].index = RightHand;
        _handSlots[RightHand].startIndex = _handSlots[RightHand].hasItem ? RightHand : 0;
        _handSlots[LeftHand].index = LeftHand;
        _handSlots[LeftHand].startIndex = _handSlots[LeftHand].hasItem ? LeftHand : 0;

        NotifyChanged();
    }

    /// <summary>
    /// Drop whatever is in the right hand (or both hands for heavy item).
    /// Returns the dropped item, or null if hands were empty.
    /// </summary>
    public Item DropFromHands()
    {
        if (IsHoldingHeavy())
        {
            Item item = _handSlots[RightHand].item;
            ClearBothHandsRaw();
            NotifyChanged();
            return item;
        }

        var right = _handSlots[RightHand];
        if (right.hasItem && right.item != null)
        {
            Item item = right.item;
            _handSlots[RightHand] = new InventorySlotState { index = RightHand };
            NotifyChanged();
            return item;
        }

        return null;
    }

    /// <summary>
    /// Remove item from a display slot (hand or expansion). Does not move the Item GameObject; caller handles PutAway/Drop.
    /// For second-half slot of heavy item, use the main hand slot (displayIndex 0). Returns null if slot empty or invalid.
    /// </summary>
    public Item RemoveItemFromDisplaySlot(int displayIndex)
    {
        if (displayIndex < 0) return null;
        if (displayIndex < HandSlotCount)
        {
            if (displayIndex == LeftHand && IsDisplaySlotSecondHalf(LeftHand))
                return null;
            if (IsHoldingHeavy())
            {
                if (displayIndex != RightHand) return null;
                Item item = _handSlots[RightHand].item;
                ClearBothHandsRaw();
                NotifyChanged();
                return item;
            }
            var slot = _handSlots[displayIndex];
            if (!slot.hasItem || slot.item == null) return null;
            Item handItem = slot.item;
            _handSlots[displayIndex] = new InventorySlotState { index = displayIndex };
            NotifyChanged();
            return handItem;
        }
        if (!ResolveExpansionIndex(displayIndex, out int ei, out int si)) return null;
        var exp = _expansions[ei];
        var s = exp.slots[si];
        if (s.isSecondHalf)
        {
            si = s.startIndex;
            s = exp.slots[si];
        }
        if (!s.hasItem || s.item == null) return null;
        Item expItem = s.item;
        int count = s.slotCount;
        ClearExpansionSlotRaw(exp, si, count);
        NotifyChanged();
        return expItem;
    }

    /// <summary>
    /// Place item into a display slot. Hands must be free for hand slot; expansion slot(s) must be free. Returns false if cannot place.
    /// </summary>
    public bool TryPlaceItemInDisplaySlot(int displayIndex, Item item)
    {
        if (item == null || displayIndex < 0) return false;
        if (displayIndex < HandSlotCount)
        {
            if (item.SlotCount == 1)
            {
                var slot = _handSlots[displayIndex];
                if (slot.hasItem || slot.isSecondHalf) return false;
                SetHandItem(displayIndex, item);
                return true;
            }
            if (item.SlotCount == 2)
            {
                if (GetRightHandItem() != null || GetLeftHandItem() != null) return false;
                SetBothHandsItem(item);
                return true;
            }
            return false;
        }
        if (!ResolveExpansionIndex(displayIndex, out int ei, out int si)) return false;
        var exp = _expansions[ei];
        if (si < 0 || si >= exp.slots.Count) return false;
        int needSlots = item.SlotCount;
        if (needSlots > 1 && exp.slots[si].isSecondHalf) return false;
        for (int i = 0; i < needSlots; i++)
        {
            int idx = si + i;
            if (idx >= exp.slots.Count) return false;
            var s = exp.slots[idx];
            if (s.hasItem || s.isSecondHalf) return false;
        }
        if (needSlots == 1)
        {
            SetExpansionSlot(ei, si, item);
            return true;
        }
        PlaceItemInExpansionRaw(exp, si, item);
        NotifyChanged();
        return true;
    }

    // ── Internal raw operations (no NotifyChanged) ──

    private void ClearBothHandsRaw()
    {
        _handSlots[RightHand] = new InventorySlotState { index = RightHand };
        _handSlots[LeftHand] = new InventorySlotState { index = LeftHand };
    }

    private void ClearExpansionSlotRaw(InventoryExpansion exp, int slotIndex, int count)
    {
        for (int i = 0; i < count && slotIndex + i < exp.slots.Count; i++)
            exp.slots[slotIndex + i] = new InventorySlotState { index = slotIndex + i };
    }

    private void PlaceItemInExpansionRaw(InventoryExpansion exp, int slotIndex, Item item)
    {
        if (item == null || slotIndex < 0 || slotIndex >= exp.slots.Count) return;
        exp.slots[slotIndex] = new InventorySlotState
        {
            index = slotIndex,
            hasItem = true,
            startIndex = slotIndex,
            slotCount = item.SlotCount,
            item = item
        };
        if (item.SlotCount > 1 && slotIndex + 1 < exp.slots.Count)
        {
            exp.slots[slotIndex + 1] = new InventorySlotState
            {
                index = slotIndex + 1,
                isSecondHalf = true,
                startIndex = slotIndex
            };
        }
    }

    private void TryPutItemBackInAnyExpansion(Item item)
    {
        if (item == null) return;
        if (FindSpaceInAnyExpansion(item.SlotCount, out int ei, out int si))
        {
            PlaceItemInExpansionRaw(_expansions[ei], si, item);
        }
    }

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
