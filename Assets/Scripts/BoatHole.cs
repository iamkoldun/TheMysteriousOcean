using UnityEngine;

/// <summary>
/// A hole in the boat. While unpatched, contributes its sink rate to BoatFlooding.
/// Self-registers with the parent BoatFlooding on Awake.
/// Spawns a water leak particle effect that activates when the boat is taking on water.
/// </summary>
public class BoatHole : MonoBehaviour
{
    [SerializeField] private float sinkRatePerSecond = 0.05f;
    [SerializeField] private ParticleSystem leakParticles;
    [SerializeField] private float activationThreshold = 0.01f;

    public bool IsPatched { get; private set; }
    public float ActiveSinkRate => IsPatched ? 0f : sinkRatePerSecond;

    private BoatFlooding _flooding;

    private void Awake()
    {
        _flooding = GetComponentInParent<BoatFlooding>();
        if (_flooding != null) _flooding.RegisterHole(this);

        if (leakParticles == null)
        {
            var t = transform.Find("LeakParticles");
            if (t != null) leakParticles = t.GetComponent<ParticleSystem>();
        }
        if (leakParticles == null) leakParticles = CreateLeakParticles();

        SetEmission(false);
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

    private void Update()
    {
        if (leakParticles == null) return;
        if (IsPatched) { SetEmission(false); return; }

        bool shouldEmit = _flooding != null && _flooding.WaterLevel > activationThreshold;
        SetEmission(shouldEmit);
    }

    private void SetEmission(bool on)
    {
        if (leakParticles == null) return;
        var emission = leakParticles.emission;
        if (emission.enabled == on) return;
        emission.enabled = on;
        if (on) leakParticles.Play(true);
        else leakParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private ParticleSystem CreateLeakParticles()
    {
        var go = new GameObject("LeakParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        // Spray water inward (toward the boat hull interior) — local +Y as default.
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 0.7f;
        main.startSpeed = 3.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.55f, 0.78f, 0.95f, 0.85f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 45f;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.04f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.material = new Material(shader);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.5f;
        }

        return ps;
    }
}
