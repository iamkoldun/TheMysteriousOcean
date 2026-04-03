using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal progress bar showing boat flood level as a percentage.
/// Blue below 85%, red at 85%+.
/// </summary>
public class BoatWaterLevelUI : MonoBehaviour
{
    [SerializeField] private BoatFlooding boatFlooding;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color normalColor = new Color(0.2f, 0.5f, 0.9f, 1f);
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    private Text _text;

    private void Awake()
    {
        if (boatFlooding == null) boatFlooding = FindFirstObjectByType<BoatFlooding>();

        // Background
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage != null) backgroundImage.color = backgroundColor;

        // Bar container + fill
        Transform barTransform = transform.Find("Bar");
        if (barTransform == null)
        {
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(transform, false);
            var barBg = barGo.AddComponent<Image>();
            barBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            barTransform = barGo.transform;

            var barRt = barGo.transform as RectTransform;
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.offsetMin = new Vector2(8f, 6f);
            barRt.offsetMax = new Vector2(-8f, -20f);
        }

        if (fillImage == null)
        {
            var t = barTransform.Find("Fill");
            if (t != null)
            {
                fillImage = t.GetComponent<Image>();
            }
            else
            {
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(barTransform, false);
                fillImage = fillGo.AddComponent<Image>();
                var fillRt = fillGo.transform as RectTransform;
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }
        }

        if (fillImage != null)
        {
            fillImage.color = normalColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        // Label text on top
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(transform, false);
        var label = labelGo.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 11;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        label.text = "\u0412\u043e\u0434\u0430";
        var labelRt = labelGo.transform as RectTransform;
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(8f, 0f);
        labelRt.sizeDelta = new Vector2(-8f, 18f);

        // Percent text inside the bar
        var textGo = new GameObject("Percent");
        textGo.transform.SetParent(barTransform, false);
        _text = textGo.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 12;
        _text.fontStyle = FontStyle.Bold;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.color = Color.white;
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(1f, -1f);
        var textRt = textGo.transform as RectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Layout();
        RefreshFill();
    }

    private void Layout()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 160f);
        rt.sizeDelta = new Vector2(160f, 38f);
    }

    private void LateUpdate()
    {
        RefreshFill();
    }

    private void RefreshFill()
    {
        if (boatFlooding == null) return;
        float level = boatFlooding.WaterLevel;
        int pct = Mathf.RoundToInt(level * 100f);

        if (fillImage != null)
        {
            fillImage.fillAmount = level;

            var fillRt = fillImage.transform as RectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(level, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }

            fillImage.color = pct >= 85 ? dangerColor : normalColor;
        }

        if (_text != null)
            _text.text = pct + "%";
    }
}
