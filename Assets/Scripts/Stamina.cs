using UnityEngine;

/// <summary>
/// Stamina: drains while sprinting, regenerates after a delay when not sprinting.
/// Attach to the same GameObject as FirstPersonController (or reference from it).
/// </summary>
public class Stamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainPerSecond = 25f;
    [SerializeField] private float regenPerSecond = 20f;
    [SerializeField] private float regenDelayAfterStop = 2f;

    private float _currentStamina;
    private float _timeSinceStoppedSprinting;
    private bool _wasSprintingLastFrame;

    public float CurrentStamina => _currentStamina;
    public float MaxStamina => maxStamina;
    public float Normalized => maxStamina > 0 ? Mathf.Clamp01(_currentStamina / maxStamina) : 0f;
    public bool CanSprint => _currentStamina > 0f;

    /// <summary>
    /// Call each frame from movement: true when player is sprinting this frame.
    /// </summary>
    public void SetSprinting(bool sprinting)
    {
        if (sprinting)
        {
            _timeSinceStoppedSprinting = 0f;
            _currentStamina = Mathf.Max(0f, _currentStamina - drainPerSecond * Time.deltaTime);
        }
        else
        {
            _timeSinceStoppedSprinting += Time.deltaTime;
            if (_timeSinceStoppedSprinting >= regenDelayAfterStop)
                _currentStamina = Mathf.Min(maxStamina, _currentStamina + regenPerSecond * Time.deltaTime);
        }
    }

    private void Awake()
    {
        _currentStamina = maxStamina;
    }
}
