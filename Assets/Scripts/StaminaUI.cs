using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal stamina bar at bottom-left. Fill is left-anchored; width shrinks to the right when stamina drains.
/// </summary>
public class StaminaUI : MonoBehaviour
{
    [SerializeField] private Stamina stamina;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color fillColor = new Color(0.25f, 0.85f, 0.5f, 1f);

    private void Awake()
    {
        if (stamina == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) stamina = player.GetComponent<Stamina>();
            if (stamina == null) stamina = FindFirstObjectByType<Stamina>();
        }
        if (fillImage == null)
        {
            var t = transform.Find("Fill");
            if (t != null) fillImage = t.GetComponent<Image>();
        }
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        if (backgroundImage != null) backgroundImage.color = backgroundColor;
        if (fillImage != null)
        {
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            var fillRt = fillImage.transform as RectTransform;
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
            }
        }
        Layout();
        RefreshFill();
    }

    private void Start()
    {
        if (stamina == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) stamina = player.GetComponent<Stamina>();
        }
    }

    private void Layout()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(20f, 120f);
            rt.sizeDelta = new Vector2(200f, 14f);
        }
    }

    private void LateUpdate()
    {
        RefreshFill();
    }

    private void RefreshFill()
    {
        float normalized = stamina != null ? stamina.Normalized : 0f;

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.preserveAspect = false;
            fillImage.fillAmount = normalized;
        }

        // Меняем ширину через RectTransform — полоска гарантированно сужается слева направо
        var fillRt = fillImage != null ? fillImage.transform as RectTransform : transform.Find("Fill") as RectTransform;
        if (fillRt != null)
        {
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(normalized, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }
    }
}
