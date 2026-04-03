using UnityEngine;

/// <summary>
/// The boat slowly sinks over time. When the pump is active, it rises back up.
/// Game over when the boat is fully submerged.
/// </summary>
public class BoatFlooding : MonoBehaviour
{
    [Header("Sinking")]
    [SerializeField] private float sinkRatePerSecond = 0.08f;
    [SerializeField] private float riseRatePerSecond = 0.2f;
    [SerializeField] private float maxSinkDepth = 4f;
    [SerializeField] private float startDelay = 10f;

    private float _startY;
    private float _sunkAmount;
    private float _elapsed;
    private bool _gameOver;

    /// <summary>0 = normal, 1 = fully sunk.</summary>
    public float WaterLevel => Mathf.Clamp01(_sunkAmount / maxSinkDepth);
    public bool IsGameOver => _gameOver;
    public float RiseRate => riseRatePerSecond;

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

        _sunkAmount += sinkRatePerSecond * Time.deltaTime;
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
