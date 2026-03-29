using UnityEngine;

/// <summary>
/// Attach to the same GameObject as Item. Defines how much energy this item yields when burned in a HeatGenerator.
/// </summary>
public class BurnableItem : MonoBehaviour
{
    [SerializeField] private float energyCount = 50f;

    public float EnergyCount => energyCount;
}
