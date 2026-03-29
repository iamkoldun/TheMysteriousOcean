using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Floating tooltip panel that displays item name and description near the cursor.
/// Created programmatically by InventoryPanelUI.
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    private RectTransform _rect;
    private RectTransform _canvasRect;
    private TMP_Text _nameText;
    private TMP_Text _descText;
    private CanvasGroup _canvasGroup;

    private const float PaddingX = 12f;
    private const float PaddingY = 8f;
    private const float MaxWidth = 250f;
    private const float CursorOffsetX = 16f;
    private const float CursorOffsetY = -16f;

    public static ItemTooltipUI Create(Transform canvasTransform)
    {
        var go = new GameObject("ItemTooltip", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasTransform, false);

        var rt = go.GetComponent<RectTransform>();
        // Use center anchors so anchoredPosition matches ScreenPointToLocalPointInRectangle output
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 1f);

        // Don't block raycasts so hover events on slots behind still work
        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Background
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Name
        var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        var nameTmp = nameGo.GetComponent<TMP_Text>();
        nameTmp.fontSize = 16f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.enableWordWrapping = true;
        nameTmp.raycastTarget = false;

        // Description
        var descGo = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(go.transform, false);
        var descRt = descGo.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 1f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.pivot = new Vector2(0f, 1f);
        var descTmp = descGo.GetComponent<TMP_Text>();
        descTmp.fontSize = 13f;
        descTmp.color = new Color(0.78f, 0.78f, 0.78f, 1f);
        descTmp.enableWordWrapping = true;
        descTmp.raycastTarget = false;

        var tooltip = go.AddComponent<ItemTooltipUI>();
        tooltip._rect = rt;
        tooltip._nameText = nameTmp;
        tooltip._descText = descTmp;
        tooltip._canvasRect = canvasTransform.GetComponent<RectTransform>();
        tooltip._canvasGroup = cg;

        go.SetActive(false);
        return tooltip;
    }

    public void Show(string itemName, string description)
    {
        _nameText.text = itemName ?? "";
        bool hasDesc = !string.IsNullOrEmpty(description);
        _descText.text = hasDesc ? description : "";
        _descText.gameObject.SetActive(hasDesc);

        gameObject.SetActive(true);
        // Render on top of everything
        transform.SetAsLastSibling();
        LayoutTooltip();
        UpdatePosition();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdatePosition()
    {
        if (!gameObject.activeSelf || _canvasRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, null, out Vector2 local))
        {
            local.x += CursorOffsetX;
            local.y += CursorOffsetY;

            // Keep tooltip inside canvas bounds
            Vector2 size = _rect.sizeDelta;
            Vector2 canvasSize = _canvasRect.sizeDelta;
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            if (local.x + size.x > halfW)
                local.x = local.x - size.x - CursorOffsetX * 2f;
            if (local.y - size.y < -halfH)
                local.y = local.y + size.y - CursorOffsetY * 2f;

            _rect.anchoredPosition = local;
        }
    }

    private void LayoutTooltip()
    {
        float contentWidth = MaxWidth - PaddingX * 2f;

        var nameRt = _nameText.rectTransform;
        nameRt.anchoredPosition = new Vector2(PaddingX, -PaddingY);
        nameRt.sizeDelta = new Vector2(contentWidth, 0f);
        _nameText.ForceMeshUpdate();
        float nameH = _nameText.preferredHeight;
        nameRt.sizeDelta = new Vector2(contentWidth, nameH);

        float totalH = PaddingY + nameH;

        if (_descText.gameObject.activeSelf)
        {
            var descRt = _descText.rectTransform;
            descRt.anchoredPosition = new Vector2(PaddingX, -(PaddingY + nameH + 4f));
            descRt.sizeDelta = new Vector2(contentWidth, 0f);
            _descText.ForceMeshUpdate();
            float descH = _descText.preferredHeight;
            descRt.sizeDelta = new Vector2(contentWidth, descH);
            totalH += 4f + descH;
        }

        totalH += PaddingY;
        _rect.sizeDelta = new Vector2(MaxWidth, totalH);
    }
}
