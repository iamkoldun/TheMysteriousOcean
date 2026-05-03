using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Crafting recipe list. Built programmatically. Singleton — opened by PlayerInventory on E-key.
/// </summary>
public class CraftingPanelUI : MonoBehaviour
{
    public static CraftingPanelUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private RectTransform rowsContainer;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    [SerializeField] private Color slotColor = new Color(0.22f, 0.22f, 0.28f, 0.95f);
    [SerializeField] private Color buttonColor = new Color(0.25f, 0.55f, 0.85f, 1f);
    [SerializeField] private Color buttonDisabledColor = new Color(0.35f, 0.35f, 0.4f, 1f);

    private Workbench _current;
    private readonly List<RecipeRow> _rows = new List<RecipeRow>();
    private bool _layoutBuilt;
    private ItemTooltipUI _tooltip;

    private const float PanelWidth = 600f;
    private const float RowHeight = 96f;
    private const float RowGap = 12f;
    private const float SlotSize = 70f;
    private const float SlotGap = 8f;

    public void Open(Workbench wb)
    {
        if (wb == null) return;
        EnsureLayout();
        gameObject.SetActive(true);
        SetCurrent(wb, forceRebuild: true);
        RefreshAvailability();
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (_tooltip != null) _tooltip.Hide();
        gameObject.SetActive(false);
        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetCurrent(Workbench wb, bool forceRebuild)
    {
        if (!forceRebuild && _current == wb) return;
        _current = wb;
        BuildRowsForCurrent();
    }

    private void EnsureLayout()
    {
        if (!_layoutBuilt) BuildLayout();
    }

    private void Awake()
    {
        Instance = this;
        if (inventory == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) inventory = p.GetComponent<Inventory>();
        }
        if (playerInventory == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerInventory = p.GetComponent<PlayerInventory>();
        }
        BuildLayout();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) IsOpen = false;
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnInventoryChanged += RefreshAvailability;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= RefreshAvailability;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            Close();
        if (_tooltip != null && _tooltip.gameObject.activeSelf)
            _tooltip.UpdatePosition();
    }

    private void BuildLayout()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(PanelWidth, 380f);
        }

        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage == null) backgroundImage = gameObject.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        if (titleText == null)
        {
            var t = transform.Find("Title");
            if (t == null)
            {
                var go = new GameObject("Title", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(transform, false);
                titleText = go.GetComponent<Text>();
            }
            else titleText = t.GetComponent<Text>();
        }
        var titleRt = titleText.transform as RectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(-20f, 28f);
        titleText.text = "Workbench";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (rowsContainer == null)
        {
            var t = transform.Find("Rows");
            if (t == null)
            {
                var go = new GameObject("Rows", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                rowsContainer = go.GetComponent<RectTransform>();
            }
            else rowsContainer = t as RectTransform;
        }
        rowsContainer.anchorMin = new Vector2(0f, 0f);
        rowsContainer.anchorMax = new Vector2(1f, 1f);
        rowsContainer.pivot = new Vector2(0.5f, 1f);
        rowsContainer.offsetMin = new Vector2(20f, 20f);
        rowsContainer.offsetMax = new Vector2(-20f, -44f);

        if (_tooltip == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            Transform canvasTransform = canvas != null ? canvas.transform : transform;
            _tooltip = ItemTooltipUI.Create(canvasTransform);
        }

        _layoutBuilt = true;
    }

    private void BuildRowsForCurrent()
    {
        foreach (var row in _rows)
        {
            if (row != null && row.root != null) Destroy(row.root);
        }
        _rows.Clear();

        if (_current == null || _current.Recipes == null) return;

        float y = 0f;
        for (int i = 0; i < _current.Recipes.Count; i++)
        {
            var recipe = _current.Recipes[i];
            if (recipe == null) continue;
            var row = BuildRow(recipe, y);
            _rows.Add(row);
            y -= RowHeight + RowGap;
        }
    }

    private RecipeRow BuildRow(CraftingRecipe recipe, float yOffset)
    {
        var rowGo = new GameObject("Row_" + recipe.RecipeName, typeof(RectTransform));
        var rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.SetParent(rowsContainer, false);
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, yOffset);
        rowRt.sizeDelta = new Vector2(0f, RowHeight);

        var rowBg = rowGo.AddComponent<Image>();
        rowBg.color = new Color(0f, 0f, 0f, 0.25f);

        var row = new RecipeRow { root = rowGo, recipe = recipe };
        int ingCount = recipe.IngredientItemIds != null ? recipe.IngredientItemIds.Length : 0;
        row.ingredientCounts = new Text[ingCount];

        float x = 8f;
        for (int i = 0; i < ingCount; i++)
        {
            Sprite icon = (recipe.IngredientIcons != null && i < recipe.IngredientIcons.Length) ? recipe.IngredientIcons[i] : null;
            Item prefab = (recipe.IngredientPrefabs != null && i < recipe.IngredientPrefabs.Length) ? recipe.IngredientPrefabs[i] : null;
            BuildSlot(rowRt, x, icon, prefab, out var countText);
            row.ingredientCounts[i] = countText;
            x += SlotSize + SlotGap;
        }

        var arrow = BuildText(rowRt, "→", 28, FontStyle.Bold);
        var arrowRt = arrow.rectTransform;
        arrowRt.anchorMin = new Vector2(0f, 0.5f);
        arrowRt.anchorMax = new Vector2(0f, 0.5f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.anchoredPosition = new Vector2(x + 16f, 0f);
        arrowRt.sizeDelta = new Vector2(40f, 32f);
        arrow.alignment = TextAnchor.MiddleCenter;
        x += 40f;

        BuildSlot(rowRt, x, recipe.OutputIcon, recipe.OutputPrefab, out _);
        x += SlotSize + 16f;

        var btnGo = new GameObject("CraftButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.SetParent(rowRt, false);
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-8f, 0f);
        btnRt.sizeDelta = new Vector2(110f, 56f);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = buttonColor;
        var btn = btnGo.GetComponent<Button>();
        var btnLabel = BuildText(btnRt, "Craft", 16, FontStyle.Bold);
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color = Color.white;
        var btnLabelRt = btnLabel.rectTransform;
        btnLabelRt.anchorMin = Vector2.zero;
        btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = Vector2.zero;
        btnLabelRt.offsetMax = Vector2.zero;
        btn.targetGraphic = btnImg;
        var capturedRecipe = recipe;
        btn.onClick.AddListener(() => OnCraftClicked(capturedRecipe));
        row.button = btn;
        row.buttonImage = btnImg;

        return row;
    }

    private void BuildSlot(RectTransform parent, float xOffset, Sprite icon, Item tooltipPrefab, out Text countText)
    {
        var slotGo = new GameObject("Slot", typeof(RectTransform), typeof(Image));
        var rt = slotGo.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(xOffset, 0f);
        rt.sizeDelta = new Vector2(SlotSize, SlotSize);
        slotGo.GetComponent<Image>().color = slotColor;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.SetParent(rt, false);
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, 6f);
        iconRt.sizeDelta = new Vector2(SlotSize - 18f, SlotSize - 18f);
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = icon;
        iconImg.raycastTarget = false;
        if (icon == null) iconImg.color = new Color(1f, 1f, 1f, 0.15f);

        var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text), typeof(Outline));
        var countRt = countGo.GetComponent<RectTransform>();
        countRt.SetParent(rt, false);
        countRt.anchorMin = new Vector2(0f, 0f);
        countRt.anchorMax = new Vector2(1f, 0f);
        countRt.pivot = new Vector2(0.5f, 0f);
        countRt.anchoredPosition = new Vector2(0f, 2f);
        countRt.sizeDelta = new Vector2(-2f, 18f);
        countText = countGo.GetComponent<Text>();
        countText.alignment = TextAnchor.MiddleCenter;
        countText.fontSize = 14;
        countText.fontStyle = FontStyle.Bold;
        countText.color = Color.white;
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.text = "";
        countText.raycastTarget = false;
        var countOutline = countGo.GetComponent<Outline>();
        countOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        countOutline.effectDistance = new Vector2(1f, -1f);

        if (tooltipPrefab != null)
        {
            var trigger = slotGo.AddComponent<EventTrigger>();
            string name = tooltipPrefab.DisplayName;
            string desc = tooltipPrefab.Description;

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (_tooltip != null) _tooltip.Show(name, desc); });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (_tooltip != null) _tooltip.Hide(); });
            trigger.triggers.Add(exit);
        }
    }

    private Text BuildText(RectTransform parent, string text, int fontSize, FontStyle style)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        return t;
    }

    private void RefreshAvailability()
    {
        if (inventory == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) inventory = p.GetComponent<Inventory>();
            if (inventory == null) return;
        }
        for (int r = 0; r < _rows.Count; r++)
        {
            var row = _rows[r];
            if (row == null || row.recipe == null) continue;
            bool canCraft = CraftingService.CanCraft(inventory, row.recipe);
            if (row.button != null) row.button.interactable = canCraft;
            if (row.buttonImage != null) row.buttonImage.color = canCraft ? buttonColor : buttonDisabledColor;

            var ids = row.recipe.IngredientItemIds;
            if (ids == null || row.ingredientCounts == null) continue;

            var needed = new Dictionary<string, int>();
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (needed.ContainsKey(id)) needed[id]++; else needed[id] = 1;
            }
            for (int i = 0; i < ids.Length && i < row.ingredientCounts.Length; i++)
            {
                string id = ids[i];
                if (row.ingredientCounts[i] == null) continue;
                if (string.IsNullOrEmpty(id)) { row.ingredientCounts[i].text = ""; continue; }
                int totalNeeded = needed.ContainsKey(id) ? needed[id] : 1;
                int totalHave = inventory.CountItemsById(id);
                row.ingredientCounts[i].text = $"{totalHave}/{totalNeeded}";
                row.ingredientCounts[i].color = totalHave >= totalNeeded ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            }
        }
    }

    private void OnCraftClicked(CraftingRecipe recipe)
    {
        if (recipe == null) return;
        if (CraftingService.TryCraft(inventory, playerInventory, recipe))
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayObject();
        }
    }

    private class RecipeRow
    {
        public GameObject root;
        public CraftingRecipe recipe;
        public Button button;
        public Image buttonImage;
        public Text[] ingredientCounts;
    }
}
