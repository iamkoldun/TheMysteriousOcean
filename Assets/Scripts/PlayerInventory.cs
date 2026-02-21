using UnityEngine;

/// <summary>
/// Raycast from camera, pickup on E (item goes into expansion), drop on Q (from hands).
/// Number keys 1-9 move expansion slot item into right hand (swap if occupied).
/// F swaps right and left hand.
/// Heavy items occupy both hands.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform inventoryStash;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropHeight = 0.8f;
    [SerializeField] private LayerMask pickupLayer = -1;

    private Inventory _inventory;
    private Item _rightHeldItem;
    private Item _leftHeldItem;

    public Inventory Inventory => _inventory;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        if (_inventory == null) _inventory = gameObject.AddComponent<Inventory>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            if (rightHandAnchor == null)
            {
                var right = playerCamera.transform.Find("RightHand");
                if (right != null) rightHandAnchor = right;
            }
            if (leftHandAnchor == null)
            {
                var left = playerCamera.transform.Find("LeftHand");
                if (left != null) leftHandAnchor = left;
            }
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
        _inventory.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDisable()
    {
        _inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    private void Update()
    {
        HandleNumberKeys();

        if (Input.GetKeyDown(KeyCode.F))
        {
            _inventory.SwapHands();
            RefreshHeldItem();
        }

        if (Input.GetKeyDown(KeyCode.E))
            TryPickup();
        if (Input.GetKeyDown(KeyCode.Q))
            TryDrop();
    }

    private static readonly KeyCode[] NumberKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
    };

    private void HandleNumberKeys()
    {
        int totalExpSlots = _inventory.GetTotalExpansionSlotCount();
        for (int i = 0; i < NumberKeys.Length && i < totalExpSlots; i++)
        {
            if (Input.GetKeyDown(NumberKeys[i]))
            {
                if (!_inventory.ResolveExpansionSlotIndex(i, out int ei, out int si)) break;
                var slot = _inventory.GetExpansionSlot(ei, si);
                bool slotEmpty = !slot.hasItem && !slot.isSecondHalf;
                bool hasInHand = _inventory.GetRightHandItem() != null || _inventory.IsHoldingHeavy();
                if (slotEmpty && hasInHand)
                {
                    if (_inventory.MoveHandsToExpansionSlot(ei, si))
                        RefreshHeldItem();
                }
                else
                {
                    _inventory.MoveExpansionSlotToHands(ei, si);
                    RefreshHeldItem();
                }
                break;
            }
        }
    }

    private void TryDrop()
    {
        Item dropped = _inventory.DropFromHands();
        if (dropped == null) return;

        if (_rightHeldItem == dropped) _rightHeldItem = null;
        if (_leftHeldItem == dropped) _leftHeldItem = null;

        Vector3 pos = transform.position + transform.forward * dropDistance;
        pos.y = transform.position.y + dropHeight;
        dropped.Drop(pos, transform.rotation);
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
        if (_inventory.TryAddToHands(item))
        {
            RefreshHeldItem();
            return;
        }
        if (_inventory.AddToExpansion(item))
            item.PutAway(inventoryStash);
    }

    private void OnInventoryChanged()
    {
        RefreshHeldItem();
    }

    private void PutAwayAllHandItems()
    {
        if (_rightHeldItem != null)
        {
            _rightHeldItem.PutAway(inventoryStash);
            _rightHeldItem = null;
        }
        if (_leftHeldItem != null)
        {
            _leftHeldItem.PutAway(inventoryStash);
            _leftHeldItem = null;
        }
        var right = _inventory.GetHandSlot(Inventory.RightHand);
        if (right.hasItem && right.item != null)
            right.item.PutAway(inventoryStash);
        var left = _inventory.GetHandSlot(Inventory.LeftHand);
        if (left.hasItem && !left.isSecondHalf && left.item != null)
            left.item.PutAway(inventoryStash);
    }

    /// <summary>
    /// Убирает только предмет из правой руки в сташ (для обмена с доп. инвентарём по цифре; левая рука не трогается).
    /// </summary>
    private void PutAwayRightHandItemOnly()
    {
        if (_rightHeldItem != null)
        {
            _rightHeldItem.PutAway(inventoryStash);
            _rightHeldItem = null;
        }
        var right = _inventory.GetHandSlot(Inventory.RightHand);
        if (right.hasItem && right.item != null)
            right.item.PutAway(inventoryStash);
    }

    private Transform GetAnchorForHand(int handIndex)
    {
        Transform anchor = handIndex == Inventory.RightHand ? rightHandAnchor : leftHandAnchor;
        if (anchor == null && playerCamera != null)
        {
            anchor = playerCamera.transform.Find(handIndex == Inventory.RightHand ? "RightHand" : "LeftHand");
            if (handIndex == Inventory.RightHand) rightHandAnchor = anchor;
            else leftHandAnchor = anchor;
        }
        return anchor;
    }

    private void RefreshHeldItem()
    {
        var rightSlot = _inventory.GetHandSlot(Inventory.RightHand);
        var leftSlot = _inventory.GetHandSlot(Inventory.LeftHand);
        bool leftIsIndependent = leftSlot.hasItem && !leftSlot.isSecondHalf;

        // Правая рука: один предмет (или первый пол тяжёлого)
        if (_rightHeldItem != null && (!rightSlot.hasItem || rightSlot.item != _rightHeldItem))
        {
            _rightHeldItem.PutAway(inventoryStash);
            _rightHeldItem = null;
        }
        Transform rightAnchor = GetAnchorForHand(Inventory.RightHand);
        if (rightAnchor != null && rightSlot.hasItem && rightSlot.item != null)
        {
            _rightHeldItem = rightSlot.item;
            _rightHeldItem.AttachToHand(rightAnchor);
        }
        else if (!rightSlot.hasItem)
            _rightHeldItem = null;

        // Левая рука: отдельный предмет (не вторая половина тяжёлого)
        if (_leftHeldItem != null && (!leftIsIndependent || leftSlot.item != _leftHeldItem))
        {
            _leftHeldItem.PutAway(inventoryStash);
            _leftHeldItem = null;
        }
        Transform leftAnchor = GetAnchorForHand(Inventory.LeftHand);
        if (leftAnchor != null && leftIsIndependent && leftSlot.item != null)
        {
            _leftHeldItem = leftSlot.item;
            _leftHeldItem.AttachToHand(leftAnchor);
        }
        else if (!leftIsIndependent)
            _leftHeldItem = null;
    }
}
