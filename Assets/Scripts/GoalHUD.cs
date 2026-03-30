using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left HUD showing the current goal. Monitors WaterPump activation.
/// </summary>
public class GoalHUD : MonoBehaviour
{
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.15f, 0.75f);

    private Text _goalText;
    private Image _bgImage;
    private WaterPump _pump;
    private bool _completed;

    private void Awake()
    {
        _pump = FindFirstObjectByType<WaterPump>();

        // Background
        _bgImage = GetComponent<Image>();
        if (_bgImage == null) _bgImage = gameObject.AddComponent<Image>();
        _bgImage.color = bgColor;

        // Text child
        var textGo = new GameObject("GoalText");
        textGo.transform.SetParent(transform, false);
        _goalText = textGo.AddComponent<Text>();
        _goalText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _goalText.fontSize = 16;
        _goalText.alignment = TextAnchor.MiddleLeft;
        _goalText.color = defaultColor;
        _goalText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var textRt = textGo.transform as RectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 0f);
        textRt.offsetMax = new Vector2(-10f, 0f);

        Layout();
        UpdateGoal();
    }

    private void Layout()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(360f, 36f);
    }

    private void LateUpdate()
    {
        if (_completed) return;
        if (_pump != null && _pump.IsActive)
        {
            _completed = true;
            UpdateGoal();
        }
    }

    private void UpdateGoal()
    {
        if (_completed)
        {
            _goalText.text = "Goal Completed: Water Pump Activated";
            _goalText.color = completedColor;
            _bgImage.color = new Color(0.05f, 0.2f, 0.05f, 0.75f);
        }
        else
        {
            _goalText.text = "Goal: Activate Water Pump";
            _goalText.color = defaultColor;
        }
    }
}
