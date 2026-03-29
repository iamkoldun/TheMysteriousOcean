using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// One hotbar section (e.g. hands or one expansion): background + procedurally spawned slots.
/// Each section can use its own Slot and WideSlot prefabs.
/// </summary>
public class HotbarSectionUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject wideSlotPrefab;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private HotbarSlotUI[] _slotUIs;

    public const float SlotW = 70f;
    public const float SlotH = 70f;
    public const float Gap = 8f;
    public const float PaddingH = 4f;
    public const float PaddingV = 4f;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (slotsContainer == null) slotsContainer = transform as RectTransform;
    }

    public void SetBackgroundColor(Color color)
    {
        if (background != null) background.color = color;
    }

    /// <summary>
    /// Build slots for this section. widePattern[i] = true means use wide prefab for that logical slot (only first half; second half is hidden).
    /// </summary>
    public void BuildSlots(int slotCount, bool[] widePattern)
    {
        foreach (var go in _spawned)
        {
            if (go != null) Destroy(go);
        }
        _spawned.Clear();

        Transform parent = slotsContainer != null ? slotsContainer : transform;
        GameObject normalPrefab = slotPrefab != null ? slotPrefab : wideSlotPrefab;
        if (normalPrefab == null) return;

        for (int i = 0; i < slotCount; i++)
        {
            bool useWide = widePattern != null && i < widePattern.Length && widePattern[i] && wideSlotPrefab != null;
            var prefab = useWide ? wideSlotPrefab : normalPrefab;
            var go = Instantiate(prefab, parent);
            go.name = "Slot" + i;
            _spawned.Add(go);
        }

        _slotUIs = parent.GetComponentsInChildren<HotbarSlotUI>(true);
    }

    public HotbarSlotUI[] GetSlotUIs() => _slotUIs;

    /// <summary>
    /// Total width needed for content (slots + gaps + padding). widePattern[i]=true = first half of wide slot.
    /// </summary>
    public float GetPreferredWidth(int slotCount, bool[] widePattern)
    {
        float w = PaddingH * 2f;
        for (int i = 0; i < slotCount; i++)
        {
            bool isSecondHalf = i > 0 && widePattern != null && i - 1 < widePattern.Length && widePattern[i - 1];
            if (isSecondHalf) continue;
            bool isWide = widePattern != null && i < widePattern.Length && widePattern[i];
            w += (isWide ? SlotW * 2f + Gap : SlotW) + Gap;
        }
        if (slotCount > 0) w -= Gap;
        return w;
    }

    /// <summary>
    /// Layout slots inside this section. Uses this GameObject's RectTransform width.
    /// </summary>
    public void LayoutSlots(int slotCount, bool[] widePattern)
    {
        if (_slotUIs == null) return;
        var sectionRt = transform as RectTransform;
        if (sectionRt == null) return;
        float sectionWidth = sectionRt.rect.width;
        float startX = -sectionWidth * 0.5f + PaddingH;
        float x = startX;
        for (int i = 0; i < _slotUIs.Length && i < slotCount; i++)
        {
            var slotRt = _slotUIs[i].transform as RectTransform;
            if (slotRt == null) continue;
            bool isSecondHalf = i > 0 && widePattern != null && i - 1 < widePattern.Length && widePattern[i - 1];
            bool isWide = widePattern != null && i < widePattern.Length && widePattern[i];
            float w = isSecondHalf ? 0f : (isWide ? SlotW * 2f + Gap : SlotW);
            slotRt.anchorMin = new Vector2(0.5f, 0f);
            slotRt.anchorMax = new Vector2(0.5f, 0f);
            slotRt.pivot = new Vector2(0.5f, 0f);
            if (isSecondHalf)
            {
                slotRt.anchoredPosition = new Vector2(-9999f, 0f);
                slotRt.sizeDelta = new Vector2(0f, SlotH);
            }
            else
            {
                slotRt.anchoredPosition = new Vector2(x + w * 0.5f, 0f);
                slotRt.sizeDelta = new Vector2(w, SlotH);
                x += w + Gap;
            }
        }
    }
}
