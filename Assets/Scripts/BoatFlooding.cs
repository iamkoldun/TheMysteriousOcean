using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The boat sinks based on active BoatHole components on/under it. Pump raises it.
/// With zero active holes, the boat does not sink. Game over when fully submerged.
/// </summary>
public class BoatFlooding : MonoBehaviour
{
    [Header("Sinking")]
    [SerializeField] private float riseRatePerSecond = 0.2f;
    [SerializeField] private float maxSinkDepth = 4f;
    [SerializeField] private float startDelay = 10f;

    private readonly List<BoatHole> _holes = new();
    private float _startY;
    private float _sunkAmount;
    private float _elapsed;
    private bool _gameOver;

    /// <summary>0 = normal, 1 = fully sunk.</summary>
    public float WaterLevel => Mathf.Clamp01(_sunkAmount / maxSinkDepth);
    public bool IsGameOver => _gameOver;
    public float RiseRate => riseRatePerSecond;

    public void RegisterHole(BoatHole hole)
    {
        if (hole != null && !_holes.Contains(hole)) _holes.Add(hole);
    }

    public void UnregisterHole(BoatHole hole)
    {
        if (hole != null) _holes.Remove(hole);
    }

    private void Awake()
    {
        _startY = transform.position.y;
    }

    private void Update()
    {
        if (_gameOver || PauseMenuUI.IsPaused) return;
        if (GameOverUI.Instance != null && GameOverUI.Instance.IsShown) return;

        _elapsed += Time.deltaTime;
        if (_elapsed < startDelay) return;

        float rate = 0f;
        for (int i = 0; i < _holes.Count; i++)
        {
            if (_holes[i] != null) rate += _holes[i].ActiveSinkRate;
        }
        if (rate <= 0f) return;

        _sunkAmount += rate * Time.deltaTime;
        _sunkAmount = Mathf.Clamp(_sunkAmount, 0f, maxSinkDepth);

        ApplyPosition();

        if (_sunkAmount >= maxSinkDepth)
            _gameOver = true;
    }

    public void DrainWater(float amount)
    {
        _sunkAmount = Mathf.Max(0f, _sunkAmount - amount);
        ApplyPosition();
    }

    private void ApplyPosition()
    {
        var pos = transform.position;
        pos.y = _startY - _sunkAmount;
        transform.position = pos;
    }
}
