using UnityEngine;

/// <summary>
/// A hole in the boat. While unpatched, contributes its sink rate to BoatFlooding.
/// Self-registers with the parent BoatFlooding on Awake.
/// </summary>
public class BoatHole : MonoBehaviour
{
    [SerializeField] private float sinkRatePerSecond = 0.05f;

    public bool IsPatched { get; private set; }
    public float ActiveSinkRate => IsPatched ? 0f : sinkRatePerSecond;

    private BoatFlooding _flooding;

    private void Awake()
    {
        _flooding = GetComponentInParent<BoatFlooding>();
        if (_flooding != null) _flooding.RegisterHole(this);
    }

    private void OnDestroy()
    {
        if (_flooding != null) _flooding.UnregisterHole(this);
    }

    public void Patch()
    {
        if (IsPatched) return;
        IsPatched = true;
        Destroy(gameObject);
    }
}
