using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Single hotbar slot. Assign keyLabel, background, icon, label in the prefab;
/// display and layout are configured on the prefab, not in code.
/// </summary>
public class HotbarSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (keyLabel == null) { var t = transform.Find("KeyLabel"); if (t != null) keyLabel = t.GetComponent<TMP_Text>(); }
        if (icon == null) { var t = transform.Find("Icon"); if (t != null) icon = t.GetComponent<Image>(); }
    }

    /// <summary>Set the key hint text (e.g. "RMB", "LMB", "1", "2"). Only updates text; styling is from prefab.</summary>
    public void SetKeyLabel(string keyText)
    {
        if (keyLabel == null) return;
        keyLabel.text = keyText ?? "";
        keyLabel.enabled = !string.IsNullOrEmpty(keyText);
    }

    public void SetSlot(InventorySlotState slot, bool isContinuation)
    {
        if (isContinuation)
        {
            if (background != null) background.enabled = false;
            if (keyLabel != null) keyLabel.enabled = false;
            if (label != null) label.text = "";
            if (icon != null) icon.enabled = false;
            return;
        }
        if (keyLabel != null) keyLabel.enabled = true;
        if (background != null) background.enabled = true;
        if (slot.hasItem && slot.item != null)
        {
            if (label != null) label.text = slot.item.DisplayName;
            if (icon != null)
            {
                icon.sprite = slot.item.Icon;
                icon.enabled = true;
                icon.color = Color.white;
                icon.preserveAspect = true;
            }
        }
        else
        {
            if (label != null) label.text = "";
            if (icon != null) { icon.sprite = null; icon.enabled = false; }
        }
    }

    public void SetSelected(bool selected) { }
}
