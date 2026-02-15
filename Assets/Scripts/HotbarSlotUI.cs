using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single hotbar slot: background, optional icon/label, selection border.
/// Can be on a prefab instantiated by HotbarUI.
/// </summary>
public class HotbarSlotUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Text label;
    [SerializeField] private Image selectionBorder;
    [SerializeField] private int slotIndex;

    public void SetMerged(bool merged)
    {
        if (icon == null) return;
        icon.gameObject.SetActive(true);
        var iconRt = icon.transform as RectTransform;
        if (iconRt == null) return;
        if (merged)
        {
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(60f, 60f);
        }
        else
        {
            iconRt.anchorMin = new Vector2(0.1f, 0.25f);
            iconRt.anchorMax = new Vector2(0.9f, 0.75f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
        }
    }

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (icon == null)
        {
            var t = transform.Find("Icon");
            if (t != null)
                icon = t.GetComponent<Image>();
            else
                icon = CreateIconImage();
        }
    }

    private Image CreateIconImage()
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.25f);
        rt.anchorMax = new Vector2(0.9f, 0.75f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    private void EnsureIcon()
    {
        if (icon != null) return;
        var t = transform.Find("Icon");
        if (t != null)
            icon = t.GetComponent<Image>();
        if (icon == null)
            icon = CreateIconImage();
    }

    public void SetSlot(InventorySlotState slot, bool isContinuation)
    {
        if (isContinuation)
        {
            if (background != null) background.enabled = false;
            if (label != null) label.text = "";
            if (icon != null) icon.enabled = false;
            return;
        }
        if (background != null) background.enabled = true;
        if (slot.hasItem && slot.item != null)
        {
            if (label != null) label.text = slot.item.DisplayName;
            EnsureIcon();
            if (icon != null)
            {
                icon.sprite = slot.item.Icon;
                icon.enabled = true;
                icon.color = Color.white;
                icon.preserveAspect = true;
                if (slot.slotCount == 2)
                    SetMerged(true);
            }
        }
        else
        {
            if (label != null) label.text = "";
            if (icon != null) { icon.sprite = null; icon.enabled = false; SetMerged(false); }
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null) selectionBorder.enabled = selected;
    }
}
