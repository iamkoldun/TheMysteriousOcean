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

    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color dangerBgColor = new Color(0.25f, 0.05f, 0.05f, 0.75f);

    private Text _goalText;
    private Image _bgImage;
    private WaterPump _pump;
    private BoatFlooding _flooding;
    private int _goalPhase; // 0 = activate pump, 1 = keep afloat

    private void Awake()
    {
        _pump = FindFirstObjectByType<WaterPump>();
        _flooding = FindFirstObjectByType<BoatFlooding>();

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
        if (_goalPhase == 0)
        {
            if (_pump != null && _pump.IsActive)
            {
                _goalPhase = 1;
                _goalText.color = completedColor;
                _bgImage.color = new Color(0.05f, 0.2f, 0.05f, 0.75f);
            }
        }

        if (_goalPhase == 1 && _flooding != null)
        {
            int pct = Mathf.RoundToInt(_flooding.WaterLevel * 100f);
            _goalText.text = $"\u0426\u0435\u043b\u044c: \u041d\u0435 \u0434\u0430\u0439 \u043b\u043e\u0434\u043a\u0435 \u0443\u0442\u043e\u043d\u0443\u0442\u044c \u2014 \u0412\u043e\u0434\u0430: {pct}%";
            if (pct > 75)
            {
                _goalText.color = dangerColor;
                _bgImage.color = dangerBgColor;
            }
            else
            {
                _goalText.color = defaultColor;
                _bgImage.color = bgColor;
            }
        }
    }

    private void UpdateGoal()
    {
        _goalText.text = "\u0426\u0435\u043b\u044c: \u0412\u043a\u043b\u044e\u0447\u0438 \u0432\u043e\u0434\u044f\u043d\u0443\u044e \u043f\u043e\u043c\u043f\u0443";
        _goalText.color = defaultColor;
    }
}
