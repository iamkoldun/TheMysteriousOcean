using UnityEngine;

/// <summary>
/// Attach to pickable objects. Light = 1 slot, Heavy = 2 adjacent slots.
/// snapPoint (empty child) is parented to player Hand when item is held.
/// </summary>
public class Item : MonoBehaviour
{
    public enum ItemSize { Light = 1, Heavy = 2 }

    [SerializeField] private string displayName = "Item";
    [SerializeField] private ItemSize size = ItemSize.Light;
    [SerializeField] private Transform snapPoint;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int SlotCount => (int)size;
    public bool IsHeavy => size == ItemSize.Heavy;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
    }

    /// <summary>
    /// Called when item is added to inventory and attached to hand.
    /// </summary>
    public void AttachToHand(Transform hand)
    {
        if (hand == null) return;
        if (_rb != null) _rb.isKinematic = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        SnapPoint.SetParent(hand);
        SnapPoint.localPosition = Vector3.zero;
        SnapPoint.localRotation = Quaternion.identity;
        SnapPoint.localScale = Vector3.one;
        if (SnapPoint != transform)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when item stays in inventory but is no longer the active (held) one.
    /// putAwayParent: e.g. player's inventory stash (hidden transform).
    /// </summary>
    public void PutAway(Transform putAwayParent)
    {
        if (putAwayParent == null) putAwayParent = transform;
        if (_rb != null) _rb.isKinematic = true;
        SnapPoint.SetParent(putAwayParent);
        SnapPoint.localPosition = Vector3.zero;
        SnapPoint.localRotation = Quaternion.identity;
        SnapPoint.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Выброс на землю: открепляем от игрока, включаем Rigidbody (падает), коллайдер, ставим позицию в мире.
    /// </summary>
    public void Drop(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(null);
        SnapPoint.SetParent(transform);
        SnapPoint.localPosition = Vector3.zero;
        SnapPoint.localRotation = Quaternion.identity;
        SnapPoint.localScale = Vector3.one;
        transform.position = position;
        transform.rotation = rotation;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        gameObject.SetActive(true);
    }
}
