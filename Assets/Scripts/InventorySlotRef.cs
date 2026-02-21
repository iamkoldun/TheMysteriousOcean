using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to inventory slot (same GameObject as HotbarSlotUI or child). Holds displayIndex and notifies panel on pointer down.
/// </summary>
public class InventorySlotRef : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private int displayIndex;
    private InventoryPanelUI _panel;

    public int DisplayIndex
    {
        get => displayIndex;
        set => displayIndex = value;
    }

    public void SetPanel(InventoryPanelUI panel)
    {
        _panel = panel;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_panel != null)
            _panel.OnSlotPointerDown(displayIndex);
    }
}
