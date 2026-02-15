using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Shows hotbar slots (left-bottom). If slotUIPrefab is set, spawns that prefab per slot; otherwise uses existing children.
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject slotUIPrefab;
    [SerializeField] private HotbarSlotUI[] slotUIs;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color activeColor = new Color(0.4f, 0.6f, 0.9f, 1f);

    private List<GameObject> _spawnedSlots = new List<GameObject>();

    private void Awake()
    {
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();

        if (slotUIPrefab != null)
        {
            foreach (var slot in GetComponentsInChildren<HotbarSlotUI>(true))
            {
                if (slot.transform != transform)
                    Destroy(slot.gameObject);
            }
            _spawnedSlots.Clear();
            for (int i = 0; i < Inventory.HotbarSlotCount; i++)
            {
                var go = Instantiate(slotUIPrefab, transform);
                go.name = "Slot" + i;
                _spawnedSlots.Add(go);
            }
            slotUIs = GetComponentsInChildren<HotbarSlotUI>(true);
        }
        else if (slotUIs == null || slotUIs.Length == 0)
        {
            slotUIs = GetComponentsInChildren<HotbarSlotUI>(true);
        }

        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(20f, 20f);
            rt.sizeDelta = new Vector2(320f, 70f);
        }
        LayoutSlots();
    }

    private void OnDestroy()
    {
        foreach (var go in _spawnedSlots)
        {
            if (go != null) Destroy(go);
        }
    }

    private const float SlotW = 70f;
    private const float Gap = 8f;
    private const float StartX = 4f;

    private void LayoutSlots()
    {
        if (slotUIs == null || slotUIs.Length == 0 || inventory == null) return;
        float x = StartX;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slotRt = slotUIs[i].transform as RectTransform;
            if (slotRt == null) continue;
            slotRt.anchorMin = new Vector2(0f, 0f);
            slotRt.anchorMax = new Vector2(0f, 1f);
            slotRt.pivot = new Vector2(0f, 0.5f);
            bool isSecondHalf = inventory.IsSlotOccupiedByPreviousHalf(i);
            var slot = inventory.GetSlot(i);
            float w = slotW(i);
            slotRt.anchoredPosition = new Vector2(x, 0f);
            slotRt.sizeDelta = new Vector2(w, -8f);
            slotUIs[i].SetMerged(w > SlotW);
            x += w + Gap;
        }
    }

    private float slotW(int index)
    {
        if (inventory.IsSlotOccupiedByPreviousHalf(index)) return 0f;
        var slot = inventory.GetSlot(index);
        if (slot.hasItem && slot.slotCount == 2) return SlotW * 2f + Gap;
        return SlotW;
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnSlotChanged += RefreshAll;
            inventory.OnActiveHotbarChanged += OnActiveChanged;
        }
        RefreshAll(0);
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnSlotChanged -= RefreshAll;
            inventory.OnActiveHotbarChanged -= OnActiveChanged;
        }
    }

    private void OnActiveChanged(int index)
    {
        for (int i = 0; i < Inventory.HotbarSlotCount && i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == index);
    }

    private void RefreshAll(int _)
    {
        if (inventory == null) return;
        LayoutSlots();
        for (int i = 0; i < Inventory.HotbarSlotCount && i < slotUIs.Length; i++)
        {
            var slot = inventory.GetSlot(i);
            bool isContinuation = inventory.IsSlotOccupiedByPreviousHalf(i);
            slotUIs[i].SetSlot(slot, isContinuation);
            slotUIs[i].SetSelected(i == inventory.GetActiveHotbarIndex());
        }
    }
}
