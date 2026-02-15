using UnityEngine;

/// <summary>
/// Raycast from camera, pickup on E (item stored, snapPoint attached to Hand), hotbar keys 1-4.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform handAnchor;
    [SerializeField] private Transform inventoryStash;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropHeight = 0.8f;
    [SerializeField] private LayerMask pickupLayer = -1;

    private Inventory _inventory;
    private Item _currentHeldItem;

    public Inventory Inventory => _inventory;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        if (_inventory == null) _inventory = gameObject.AddComponent<Inventory>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (handAnchor == null && playerCamera != null)
        {
            var hand = playerCamera.transform.Find("Hand");
            if (hand != null) handAnchor = hand;
        }
        if (inventoryStash == null)
        {
            var stash = transform.Find("InventoryStash");
            if (stash != null) inventoryStash = stash;
            else
            {
                var go = new GameObject("InventoryStash");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                inventoryStash = go.transform;
            }
        }
    }

    private void OnEnable()
    {
        _inventory.OnActiveHotbarChanged += OnActiveHotbarChanged;
        _inventory.OnSlotChanged += OnSlotChanged;
    }

    private void OnDisable()
    {
        _inventory.OnActiveHotbarChanged -= OnActiveHotbarChanged;
        _inventory.OnSlotChanged -= OnSlotChanged;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) _inventory.SetActiveHotbarIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) _inventory.SetActiveHotbarIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) _inventory.SetActiveHotbarIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) _inventory.SetActiveHotbarIndex(3);

        if (Input.GetKeyDown(KeyCode.E))
            TryPickup();
        if (Input.GetKeyDown(KeyCode.Q))
            TryDrop();
    }

    private void TryDrop()
    {
        var slot = _inventory.GetActiveSlot();
        if (!slot.hasItem || slot.item == null) return;
        var item = slot.item;
        int slotIndex = _inventory.GetActiveHotbarIndex();
        if (_inventory.IsSlotOccupiedByPreviousHalf(slotIndex))
            slotIndex--;
        _inventory.RemoveAt(slotIndex);
        if (_currentHeldItem == item)
            _currentHeldItem = null;
        Vector3 pos = transform.position + transform.forward * dropDistance;
        pos.y = transform.position.y + dropHeight;
        item.Drop(pos, transform.rotation);
        RefreshHeldItem();
    }

    private void TryPickup()
    {
        if (playerCamera == null) return;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
            return;
        var item = hit.collider.GetComponentInParent<Item>();
        if (item == null) return;
        if (!_inventory.AddToHotbar(item)) return; // нет свободных слотов — не берём
        RefreshHeldItem();
        if (_currentHeldItem != item)
            item.PutAway(inventoryStash);
    }

    private void OnActiveHotbarChanged(int index)
    {
        RefreshHeldItem();
    }

    private void OnSlotChanged(int slotIndex)
    {
        if (slotIndex < Inventory.HotbarSlotCount && (slotIndex == _inventory.GetActiveHotbarIndex() || (slotIndex + 1) == _inventory.GetActiveHotbarIndex()))
            RefreshHeldItem();
    }

    private void RefreshHeldItem()
    {
        if (_currentHeldItem != null)
        {
            _currentHeldItem.PutAway(inventoryStash);
            _currentHeldItem = null;
        }
        var slot = _inventory.GetActiveSlot();
        if (!slot.hasItem || handAnchor == null) return;
        _currentHeldItem = slot.item;
        if (_currentHeldItem != null)
            _currentHeldItem.AttachToHand(handAnchor);
    }
}
