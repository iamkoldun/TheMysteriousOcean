using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-center panel for HeatGenerator. Only draws data; visibility is controlled by HeatGeneratorUIVisibility on the parent.
/// </summary>
public class HeatGeneratorUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image burningFillImage;
    [SerializeField] private Image storedFillImage;
    [SerializeField] private Text energyText;
    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color burningFillColor = new Color(0.25f, 0.85f, 0.5f, 1f);
    [SerializeField] private Color storedFillColor = new Color(0.95f, 0.6f, 0.2f, 1f);

    private HeatGenerator _currentGenerator;

    /// <summary>Called by HeatGeneratorUIVisibility. Do not enable/disable this GameObject from here.</summary>
    public void SetCurrentGenerator(HeatGenerator gen)
    {
        _currentGenerator = gen;
    }

    /// <summary>Called by HeatGeneratorUIVisibility before showing the panel.</summary>
    public void EnsureLayout()
    {
        Layout();
    }

    private void Awake()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (burningFillImage == null)
        {
            var t = transform.Find("BurningProgressBar/Fill");
            if (t != null) burningFillImage = t.GetComponent<Image>();
        }
        if (storedFillImage == null)
        {
            var t = transform.Find("StoredEnergyBar/Fill");
            if (t != null) storedFillImage = t.GetComponent<Image>();
        }
        if (energyText == null)
        {
            var t = transform.Find("EnergyText");
            if (t != null) energyText = t.GetComponent<Text>();
        }
        if (energyText != null) energyText.fontSize = 12;

        if (backgroundImage != null) backgroundImage.color = backgroundColor;
        if (burningFillImage != null)
        {
            burningFillImage.color = burningFillColor;
            burningFillImage.type = Image.Type.Filled;
            burningFillImage.fillMethod = Image.FillMethod.Horizontal;
            burningFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        if (storedFillImage != null)
        {
            storedFillImage.color = storedFillColor;
            storedFillImage.type = Image.Type.Filled;
            storedFillImage.fillMethod = Image.FillMethod.Horizontal;
            storedFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        Layout();
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_currentGenerator != null) Refresh();
    }

    private void Layout()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(220f, 60f);
        }
        LayoutBar(transform.Find("BurningProgressBar"), 14f, -8f);
        LayoutBar(transform.Find("StoredEnergyBar"), 14f, -26f);
        var textRt = transform.Find("EnergyText") as RectTransform;
        if (textRt != null)
        {
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0.5f, 0f);
            textRt.anchoredPosition = new Vector2(0f, 4f);
            textRt.sizeDelta = new Vector2(-20f, 18f);
        }
    }

    private static void LayoutBar(Transform bar, float height, float yPos)
    {
        if (bar == null) return;
        var barRt = bar as RectTransform;
        if (barRt == null) return;
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, yPos);
        barRt.sizeDelta = new Vector2(-20f, height);
        var fill = bar.Find("Fill");
        if (fill != null)
        {
            var fillRt = fill as RectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }
        }
    }

    private void Refresh()
    {
        if (_currentGenerator == null) return;

        float burningNorm = _currentGenerator.NormalizedBurningRemaining; // полная шкала -> пустая по мере сгорания
        float storedNorm = _currentGenerator.NormalizedStored;

        if (burningFillImage != null)
        {
            burningFillImage.fillAmount = burningNorm;
            var fillRt = burningFillImage.transform as RectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(burningNorm, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }
        }
        if (storedFillImage != null)
        {
            storedFillImage.fillAmount = storedNorm;
            var fillRt = storedFillImage.transform as RectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(storedNorm, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }
        }
        if (energyText != null)
            energyText.text = $"Energy: {_currentGenerator.StoredEnergy:F0} / {_currentGenerator.MaxEnergy:F0}";
    }
}
