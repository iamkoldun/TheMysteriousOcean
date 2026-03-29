using UnityEngine;

/// <summary>
/// Consumes burnable items and converts them to stored energy. One item at a time.
/// </summary>
public class HeatGenerator : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float storedEnergy = 0f;
    [SerializeField] private float energyExtractionSpeed = 10f;

    private Item _currentBurningItem;
    private float _remainingEnergy;
    private float _currentItemTotalEnergy;

    public float MaxEnergy => maxEnergy;
    public float StoredEnergy => storedEnergy;
    public float NormalizedStored => maxEnergy > 0 ? Mathf.Clamp01(storedEnergy / maxEnergy) : 0f;
    /// <summary>0 = item fully burned, 1 = just started (full bar). Use for UI so bar empties as item burns.</summary>
    public float NormalizedBurningRemaining => _currentItemTotalEnergy > 0 ? Mathf.Clamp01(_remainingEnergy / _currentItemTotalEnergy) : 0f;
    public float BurningProgress => _currentItemTotalEnergy > 0 ? Mathf.Clamp01(1f - _remainingEnergy / _currentItemTotalEnergy) : 0f;
    public bool IsBurning => _currentBurningItem != null;

    private void Update()
    {
        if (_currentBurningItem == null) return;
        if (storedEnergy >= maxEnergy) return; // пауза переработки при достижении максимума

        float extract = Mathf.Min(energyExtractionSpeed * Time.deltaTime, _remainingEnergy);
        float space = maxEnergy - storedEnergy;
        extract = Mathf.Min(extract, space);
        _remainingEnergy -= extract;
        storedEnergy += extract;

        if (_remainingEnergy <= 0f)
        {
            Destroy(_currentBurningItem.gameObject);
            _currentBurningItem = null;
            _currentItemTotalEnergy = 0f;
        }
    }

    /// <summary>
    /// Try to feed an item into the generator. Returns true if accepted (caller must remove from inventory).
    /// </summary>
    public bool TryFeedItem(Item item)
    {
        if (item == null || _currentBurningItem != null) return false;

        var burnable = item.GetComponent<BurnableItem>();
        if (burnable == null) return false;

        item.transform.SetParent(transform);
        item.gameObject.SetActive(false);
        _currentBurningItem = item;
        _currentItemTotalEnergy = burnable.EnergyCount;
        _remainingEnergy = _currentItemTotalEnergy;
        return true;
    }

    /// <summary>
    /// Take energy from the generator. Returns amount actually consumed (capped by stored and maxAmount).
    /// </summary>
    public float ConsumeEnergy(float maxAmount)
    {
        float take = Mathf.Min(maxAmount, Mathf.Max(0f, storedEnergy));
        storedEnergy -= take;
        return take;
    }
}
