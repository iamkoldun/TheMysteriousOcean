using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Builds inventory overlay: vertical sections (Hands + Expansions), same slot prefabs, no key labels.
/// Handles drag-and-drop: pick from slot, drop on slot or drop outside (world).
/// </summary>
public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject handsSectionPrefab;
    [SerializeField] private GameObject expansionSectionPrefab;
    [SerializeField] private RectTransform dragGhostRoot;
    [SerializeField] private Image dragGhostImage;

    private readonly List<GameObject> _spawnedSections = new List<GameObject>();
    private readonly List<InventorySectionUI> _sections = new List<InventorySectionUI>();
    private readonly List<(HotbarSlotUI slot, int displayIndex)> _slotBindings = new List<(HotbarSlotUI, int)>();
    private Item _draggedItem;
    private int _draggedSourceDisplayIndex;
    private int _lastSlotCount;
    private bool[] _lastWidePattern;
    private const float SectionGap = 12f;
    private const float PanelPaddingX = 20f;
    private const float PanelPaddingY = 20f;

    private void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (dragGhostRoot != null) dragGhostRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
        RebuildSlots();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
        if (_draggedItem != null)
            EndDragDropInWorld();
    }

    private void Update()
    {
        if (_draggedItem != null)
        {
            if (dragGhostRoot != null)
            {
                var rt = dragGhostRoot.parent as RectTransform;
                if (rt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, null, out Vector2 local))
                    dragGhostRoot.anchoredPosition = local;
            }
            if (Input.GetMouseButtonUp(0))
                OnPointerUpWhileDragging();
        }
    }

    private void OnPointerUpWhileDragging()
    {
        if (_draggedItem == null) return;
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var refSlot = r.gameObject.GetComponentInParent<InventorySlotRef>();
            if (refSlot != null)
                return;
        }
        DropItemInWorld();
    }

    public void OnSlotPointerDown(int displayIndex)
    {
        if (inventory == null || playerInventory == null) return;

        if (_draggedItem != null)
        {
            if (displayIndex == _draggedSourceDisplayIndex)
            {
                inventory.TryPlaceItemInDisplaySlot(displayIndex, _draggedItem);
                EndDrag();
                return;
            }
            if (inventory.TryPlaceItemInDisplaySlot(displayIndex, _draggedItem))
            {
                EndDrag();
                return;
            }
            return;
        }

        var slot = inventory.GetDisplaySlot(displayIndex);
        if (slot.isSecondHalf) return;
        if (!slot.hasItem || slot.item == null) return;

        Item item = inventory.RemoveItemFromDisplaySlot(displayIndex);
        if (item == null) return;
        playerInventory.PutAwayItem(item);
        _draggedItem = item;
        _draggedSourceDisplayIndex = displayIndex;
        if (dragGhostImage != null)
        {
            dragGhostImage.sprite = item.Icon;
            dragGhostImage.enabled = true;
            dragGhostImage.color = Color.white;
        }
        if (dragGhostRoot != null) dragGhostRoot.gameObject.SetActive(true);
    }

    private void EndDrag()
    {
        _draggedItem = null;
        if (dragGhostRoot != null) dragGhostRoot.gameObject.SetActive(false);
        if (dragGhostImage != null) dragGhostImage.enabled = false;
    }

    private void DropItemInWorld()
    {
        if (_draggedItem != null && playerInventory != null)
        {
            playerInventory.DropItemInWorld(_draggedItem);
        }
        EndDrag();
    }

    private void EndDragDropInWorld()
    {
        if (_draggedItem != null && inventory != null)
            inventory.TryPlaceItemInDisplaySlot(_draggedSourceDisplayIndex, _draggedItem);
        EndDrag();
    }

    private void RebuildSlots()
    {
        if (inventory == null) return;

        int count = inventory.GetDisplaySlotCount();
        bool[] widePattern = GetWideSlotPattern(count);

        foreach (var go in _spawnedSections)
        {
            if (go != null) Destroy(go);
        }
        _spawnedSections.Clear();
        _sections.Clear();
        _slotBindings.Clear();

        GameObject handsPrefab = handsSectionPrefab != null ? handsSectionPrefab : expansionSectionPrefab;
        GameObject expPrefab = expansionSectionPrefab != null ? expansionSectionPrefab : handsPrefab;
        if (handsPrefab == null) return;

        int displayIndex = 0;
        int handsCount = Inventory.HandSlotCount;
        if (handsCount > 0 && displayIndex < count)
        {
            int sectionSlots = Mathf.Min(handsCount, count - displayIndex);
            bool[] sectionWide = SlicePattern(widePattern, displayIndex, sectionSlots);
            var sectionGo = Instantiate(handsPrefab, transform);
            sectionGo.name = "Section_Hands";
            _spawnedSections.Add(sectionGo);
            var section = sectionGo.GetComponent<InventorySectionUI>();
            if (section != null)
            {
                section.BuildSlots(sectionSlots, sectionWide);
                section.LayoutSlots(sectionSlots, sectionWide);
                _sections.Add(section);
                BindSlotRefs(section, ref displayIndex);
            }
            else
                displayIndex += sectionSlots;
        }

        for (int e = 0; e < inventory.Expansions.Count; e++)
        {
            var expansion = inventory.Expansions[e];
            int expSlots = expansion.slotCount;
            if (displayIndex >= count) break;
            int sectionSlots = Mathf.Min(expSlots, count - displayIndex);
            bool[] sectionWide = SlicePattern(widePattern, displayIndex, sectionSlots);
            var sectionGo = Instantiate(expPrefab, transform);
            sectionGo.name = "Section_" + (expansion.name ?? ("Expansion" + e));
            _spawnedSections.Add(sectionGo);
            var section = sectionGo.GetComponent<InventorySectionUI>();
            if (section != null)
            {
                section.BuildSlots(sectionSlots, sectionWide);
                section.LayoutSlots(sectionSlots, sectionWide);
                _sections.Add(section);
                BindSlotRefs(section, ref displayIndex);
            }
            else
                displayIndex += sectionSlots;
        }

        LayoutSectionsVertical();
        _lastSlotCount = count;
        _lastWidePattern = widePattern;
        RefreshSlots();
    }

    private void BindSlotRefs(InventorySectionUI section, ref int displayIndex)
    {
        var uis = section.GetSlotUIs();
        if (uis == null) return;
        int sectionStart = displayIndex;
        for (int i = 0; i < uis.Length; i++)
        {
            int bindIndex = sectionStart + i;
            _slotBindings.Add((uis[i], bindIndex));
            var slotRef = uis[i].gameObject.GetComponent<InventorySlotRef>();
            if (slotRef == null) slotRef = uis[i].gameObject.AddComponent<InventorySlotRef>();
            slotRef.SetPanel(this);
            slotRef.DisplayIndex = bindIndex;
        }
        displayIndex = sectionStart + uis.Length;
    }

    private static bool[] SlicePattern(bool[] full, int start, int length)
    {
        if (full == null || start >= full.Length) return null;
        var slice = new bool[length];
        for (int i = 0; i < length && start + i < full.Length; i++)
            slice[i] = full[start + i];
        return slice;
    }

    private void LayoutSectionsVertical()
    {
        if (inventory == null || _sections.Count == 0) return;
        var panelRt = transform as RectTransform;
        if (panelRt == null) return;

        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(PanelPaddingX, -PanelPaddingY);

        int displayIndex = 0;
        float y = 0f;
        float totalWidth = 0f;
        int count = inventory.GetDisplaySlotCount();
        bool[] widePattern = GetWideSlotPattern(count);

        for (int s = 0; s < _sections.Count; s++)
        {
            int sectionSlotCount = 0;
            if (s == 0)
                sectionSlotCount = Mathf.Min(Inventory.HandSlotCount, count);
            else
            {
                int expIdx = s - 1;
                sectionSlotCount = expIdx < inventory.Expansions.Count ? inventory.Expansions[expIdx].slotCount : 0;
                sectionSlotCount = Mathf.Min(sectionSlotCount, count - displayIndex);
            }
            bool[] sectionWide = SlicePattern(widePattern, displayIndex, sectionSlotCount);
            displayIndex += sectionSlotCount;

            var section = _sections[s];
            float sectionWidth = section.GetPreferredWidth(sectionSlotCount, sectionWide);
            float sectionHeight = section.GetPreferredHeight(sectionSlotCount, sectionWide);
            if (sectionWidth > totalWidth) totalWidth = sectionWidth;

            var sectionRt = section.transform as RectTransform;
            if (sectionRt != null)
            {
                sectionRt.anchorMin = new Vector2(0f, 1f);
                sectionRt.anchorMax = new Vector2(0f, 1f);
                sectionRt.pivot = new Vector2(0f, 1f);
                sectionRt.anchoredPosition = new Vector2(0f, y);
                sectionRt.sizeDelta = new Vector2(sectionWidth, sectionHeight);
            }
            section.LayoutSlots(sectionSlotCount, sectionWide);
            y -= sectionHeight + SectionGap;
        }

        if (y < 0) y += SectionGap;
        panelRt.sizeDelta = new Vector2(totalWidth, -y);
    }

    private bool[] GetWideSlotPattern(int count)
    {
        var pattern = new bool[count];
        for (int i = 0; i < count; i++)
        {
            if (inventory.IsDisplaySlotSecondHalf(i)) continue;
            var slot = inventory.GetDisplaySlot(i);
            pattern[i] = slot.hasItem && slot.slotCount == 2;
        }
        return pattern;
    }

    private void Refresh()
    {
        if (inventory == null) return;
        int needed = inventory.GetDisplaySlotCount();
        bool[] pattern = GetWideSlotPattern(needed);
        bool patternChanged = _lastWidePattern == null || _lastWidePattern.Length != needed;
        if (!patternChanged && _lastWidePattern != null)
        {
            for (int i = 0; i < needed && i < _lastWidePattern.Length; i++)
            {
                if (_lastWidePattern[i] != pattern[i]) { patternChanged = true; break; }
            }
        }
        if (_slotBindings.Count != needed || patternChanged)
        {
            RebuildSlots();
            return;
        }
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (inventory == null) return;
        foreach (var (slot, displayIndex) in _slotBindings)
        {
            slot.SetKeyLabel("");
            bool isContinuation = inventory.IsDisplaySlotSecondHalf(displayIndex);
            var state = inventory.GetDisplaySlot(displayIndex);
            slot.SetSlot(state, isContinuation);
            slot.SetSelected(false);
        }
    }

    private void OnDestroy()
    {
        foreach (var go in _spawnedSections)
        {
            if (go != null) Destroy(go);
        }
    }
}
