using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Center crosshair (round dot) and interaction prompt text below. Raycasts to show "E - ..." when looking at Item.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private Image crosshairDot;
    [SerializeField] private Text promptText;
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 3f;
    [SerializeField] private LayerMask interactLayer = -1;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (crosshairDot == null) { var t = transform.Find("CrosshairDot"); if (t != null) crosshairDot = t.GetComponent<Image>(); }
        if (promptText == null) { var t = transform.Find("PromptText"); if (t != null) promptText = t.GetComponent<Text>(); }
        if (crosshairDot != null && crosshairDot.sprite == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            crosshairDot.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        if (promptText != null) promptText.gameObject.SetActive(false);
        Layout();
    }

    private void Layout()
    {
        var panelRt = transform as RectTransform;
        if (panelRt != null)
        {
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
        }
        if (crosshairDot != null)
        {
            var rt = crosshairDot.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(8f, 8f);
            }
        }
        if (promptText != null)
        {
            var rt = promptText.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -30f);
                rt.sizeDelta = new Vector2(400f, 24f);
            }
            var t = promptText.GetComponent<Text>();
            if (t != null) { t.alignment = TextAnchor.MiddleCenter; t.fontSize = 14; }
        }
    }

    private void Update()
    {
        if (promptText == null) return;
        if (cam == null) { promptText.gameObject.SetActive(false); return; }
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, interactLayer))
        {
            var item = hit.collider.GetComponentInParent<Item>();
            if (item != null)
            {
                promptText.text = "E — " + item.DisplayName;
                promptText.gameObject.SetActive(true);
                return;
            }
        }
        promptText.gameObject.SetActive(false);
    }
}
