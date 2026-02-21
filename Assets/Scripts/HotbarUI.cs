using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Shows hotbar as sections: [Hands] [Expansion1] [Expansion2] ...
/// Each section has its own background and can use custom Slot/WideSlot prefabs.
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject handsSectionPrefab;
    [SerializeField] private GameObject expansionSectionPrefab;

    private readonly List<HotbarSectionUI> _sections = new List<HotbarSectionUI>();
    private readonly List<GameObject> _spawnedSections = new List<GameObject>();
    private HotbarSlotUI[] slotUIs;
    private int _lastSlotCount;
    private bool[] _lastWidePattern;

    private const float SectionGap = 12f;
    private const float PanelPaddingX = 20f;
    private const float PanelPaddingY = 20f;

    private void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
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
    }

    private void Start()
    {
        Refresh();
    }

    private void RebuildSlots()
    {
        if (inventory == null) return;

        int count = inventory.GetDisplaySlotCount();
        bool[] widePattern = GetWideSlotPattern(count);
        bool patternChanged = _lastWidePattern == null || _lastWidePattern.Length != count;
        if (!patternChanged)
        {
            for (int i = 0; i < count; i++)
            {
                if (_lastWidePattern[i] != widePattern[i]) { patternChanged = true; break; }
            }
        }
        if (slotUIs != null && slotUIs.Length == count && count == _lastSlotCount && !patternChanged)
            return;

        foreach (var go in _spawnedSections)
        {
            if (go != null) Destroy(go);
        }
        _spawnedSections.Clear();
        _sections.Clear();

        GameObject handsPrefab = handsSectionPrefab != null ? handsSectionPrefab : expansionSectionPrefab;
        GameObject expPrefab = expansionSectionPrefab != null ? expansionSectionPrefab : handsPrefab;
        if (handsPrefab == null) return;

        int displayIndex = 0;
        // Hands section
        int handsCount = Inventory.HandSlotCount;
        if (handsCount > 0 && displayIndex < count)
        {
            int sectionSlots = Mathf.Min(handsCount, count - displayIndex);
            bool[] sectionWide = SlicePattern(widePattern, displayIndex, sectionSlots);
            var sectionGo = Instantiate(handsPrefab, transform);
            sectionGo.name = "Section_Hands";
            _spawnedSections.Add(sectionGo);
            var section = sectionGo.GetComponent<HotbarSectionUI>();
            if (section != null)
            {
                section.BuildSlots(sectionSlots, sectionWide);
                _sections.Add(section);
            }
            displayIndex += sectionSlots;
        }

        // Expansion sections — each expansion can use its own sectionUIPrefab from Inventory
        for (int e = 0; e < inventory.Expansions.Count; e++)
        {
            var expansion = inventory.Expansions[e];
            int expSlots = expansion.slotCount;
            if (displayIndex >= count) break;
            int sectionSlots = Mathf.Min(expSlots, count - displayIndex);
            bool[] sectionWide = SlicePattern(widePattern, displayIndex, sectionSlots);
            GameObject prefabToUse = expansion.sectionUIPrefab != null ? expansion.sectionUIPrefab : expPrefab;
            if (prefabToUse == null) prefabToUse = handsPrefab;
            var sectionGo = Instantiate(prefabToUse, transform);
            sectionGo.name = "Section_" + (expansion.name ?? ("Expansion" + e));
            _spawnedSections.Add(sectionGo);
            var section = sectionGo.GetComponent<HotbarSectionUI>();
            if (section != null)
            {
                section.BuildSlots(sectionSlots, sectionWide);
                _sections.Add(section);
            }
            displayIndex += sectionSlots;
        }

        var slotList = new List<HotbarSlotUI>();
        foreach (var s in _sections)
        {
            var uis = s.GetSlotUIs();
            if (uis != null) slotList.AddRange(uis);
        }
        slotUIs = slotList.ToArray();
        _lastSlotCount = count;
        _lastWidePattern = widePattern;

        LayoutSections();
        RefreshSlots();
    }

    private static bool[] SlicePattern(bool[] full, int start, int length)
    {
        if (full == null || start >= full.Length) return null;
        var slice = new bool[length];
        for (int i = 0; i < length && start + i < full.Length; i++)
            slice[i] = full[start + i];
        return slice;
    }

    private void LayoutSections()
    {
        if (inventory == null || _sections.Count == 0) return;
        var panelRt = transform as RectTransform;
        if (panelRt == null) return;

        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 0f);
        panelRt.pivot = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(PanelPaddingX, PanelPaddingY);

        int displayIndex = 0;
        float x = 0f;
        float maxH = 0f;
        int count = inventory.GetDisplaySlotCount();
        bool[] widePattern = GetWideSlotPattern(count);

        for (int s = 0; s < _sections.Count; s++)
        {
            int sectionSlotCount = 0;
            if (s == 0)
            {
                sectionSlotCount = Mathf.Min(Inventory.HandSlotCount, count);
            }
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
            float sectionHeight = 90f;
            if (sectionHeight > maxH) maxH = sectionHeight;

            var sectionRt = section.transform as RectTransform;
            if (sectionRt != null)
            {
                sectionRt.anchorMin = new Vector2(0f, 0f);
                sectionRt.anchorMax = new Vector2(0f, 0f);
                sectionRt.pivot = new Vector2(0f, 0f);
                sectionRt.anchoredPosition = new Vector2(x, 0f);
                sectionRt.sizeDelta = new Vector2(sectionWidth, sectionHeight);
            }
            section.LayoutSlots(sectionSlotCount, sectionWide);
            x += sectionWidth + SectionGap;
        }
        if (x > 0f) x -= SectionGap;
        panelRt.sizeDelta = new Vector2(x, maxH);
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
        if (slotUIs == null || slotUIs.Length != needed || patternChanged)
        {
            RebuildSlots();
            return;
        }
        LayoutSections();
        RefreshSlots();
    }

    private static string GetKeyLabelForDisplayIndex(int displayIndex)
    {
        if (displayIndex == Inventory.RightHand) return "RMB";
        if (displayIndex == Inventory.LeftHand) return "LMB";
        return (displayIndex - Inventory.HandSlotCount + 1).ToString();
    }

    private void RefreshSlots()
    {
        if (inventory == null || slotUIs == null) return;
        int displayCount = inventory.GetDisplaySlotCount();
        for (int i = 0; i < slotUIs.Length && i < displayCount; i++)
        {
            bool isContinuation = inventory.IsDisplaySlotSecondHalf(i);
            slotUIs[i].SetKeyLabel(isContinuation ? "" : GetKeyLabelForDisplayIndex(i));

            var slot = inventory.GetDisplaySlot(i);
            slotUIs[i].SetSlot(slot, isContinuation);

            bool selected = false;
            if (i == Inventory.RightHand)
                selected = true;
            else if (i == Inventory.LeftHand && inventory.IsHoldingHeavy())
                selected = true;
            slotUIs[i].SetSelected(selected);
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
