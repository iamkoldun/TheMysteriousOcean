using UnityEngine;

/// <summary>
/// Pump that consumes energy from a HeatGenerator. Toggle on/off with E. When active and energy available, runs water particles on top.
/// </summary>
public class WaterPump : MonoBehaviour
{
    [SerializeField] private HeatGenerator generator;
    [SerializeField] private float energyConsumptionRate = 5f;
    [SerializeField] private ParticleSystem waterParticles;

    private bool _isActive;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (generator == null) generator = FindFirstObjectByType<HeatGenerator>();
        if (waterParticles == null)
        {
            var t = transform.Find("WaterParticles");
            if (t != null) waterParticles = t.GetComponent<ParticleSystem>();
            if (waterParticles == null) waterParticles = GetComponentInChildren<ParticleSystem>();
        }
        if (waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.enabled = false;
            waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void Toggle()
    {
        if (!_isActive && (generator == null || generator.StoredEnergy <= 0f)) return;
        _isActive = !_isActive;
        if (!_isActive && waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.enabled = false;
            waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void Update()
    {
        if (!_isActive || generator == null)
        {
            if (waterParticles != null && waterParticles.emission.enabled)
            {
                var emission = waterParticles.emission;
                emission.enabled = false;
                waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            return;
        }

        float consumed = generator.ConsumeEnergy(energyConsumptionRate * Time.deltaTime);
        if (waterParticles != null)
        {
            var emission = waterParticles.emission;
            if (consumed > 0f)
            {
                if (!emission.enabled)
                {
                    emission.enabled = true;
                    waterParticles.Play(true);
                }
            }
            else
            {
                if (emission.enabled)
                {
                    emission.enabled = false;
                    waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}
