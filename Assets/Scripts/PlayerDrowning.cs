using UnityEngine;

/// <summary>
/// Triggers game over when the player sinks below the water bottom.
/// Attach to the Player GameObject alongside FirstPersonController.
/// </summary>
public class PlayerDrowning : MonoBehaviour
{
    [SerializeField] private float deathDepthOffset = -1f;

    private CharacterController _controller;
    private WaterVolume _currentWater;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (PauseMenuUI.IsPaused) return;
        if (_currentWater == null) return;

        float feetY = transform.position.y + _controller.center.y - _controller.height * 0.5f;
        if (feetY <= _currentWater.BottomY + deathDepthOffset)
        {
            if (GameOverUI.Instance != null)
                GameOverUI.Instance.Show("You drowned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySetWater(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetWater(other);
    }

    private void OnTriggerExit(Collider other)
    {
        var water = other.GetComponent<WaterVolume>() ?? other.GetComponentInParent<WaterVolume>();
        if (water != null && water == _currentWater)
            _currentWater = null;
    }

    private void TrySetWater(Collider other)
    {
        var water = other.GetComponent<WaterVolume>() ?? other.GetComponentInParent<WaterVolume>();
        if (water != null)
            _currentWater = water;
    }
}
