using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left HUD that drives the player through 7 sequential goals.
/// On completion of the last goal triggers GameOverUI as a win screen.
/// </summary>
public class GoalHUD : MonoBehaviour
{
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.15f, 0.75f);

    [SerializeField] private float energyTarget = 90f;
    [SerializeField] private int holesToPatch = 3;

    private Text _goalText;
    private Image _bgImage;

    private Inventory _inventory;
    private HeatGenerator _generator;
    private WaterPump _pump;
    private MovingPlatformSnapper _snapper;
    private BoatDeckAnchor _deckAnchor;

    private int _phase;
    private int _patchedHoles;
    private int _initialHoleCount = -1;

    private void Awake()
    {
        FindRefs();

        _bgImage = GetComponent<Image>();
        if (_bgImage == null) _bgImage = gameObject.AddComponent<Image>();
        _bgImage.color = bgColor;

        var textGo = new GameObject("GoalText");
        textGo.transform.SetParent(transform, false);
        _goalText = textGo.AddComponent<Text>();
        _goalText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _goalText.fontSize = 16;
        _goalText.alignment = TextAnchor.UpperLeft;
        _goalText.color = defaultColor;
        _goalText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _goalText.verticalOverflow = VerticalWrapMode.Overflow;

        var textRt = textGo.transform as RectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.offsetMin = new Vector2(PaddingLeft, PaddingBottom);
        textRt.offsetMax = new Vector2(-PaddingLeft, -PaddingTop);

        Layout();
    }

    private const float PaddingTop = 8f;
    private const float PaddingBottom = 8f;
    private const float PaddingLeft = 10f;
    private const float PanelWidth = 420f;
    private const float MinHeight = 36f;

    private void FindRefs()
    {
        if (_inventory == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _inventory = p.GetComponent<Inventory>();
            if (_inventory == null) _inventory = FindFirstObjectByType<Inventory>();
        }
        if (_generator == null) _generator = FindFirstObjectByType<HeatGenerator>();
        if (_pump == null) _pump = FindFirstObjectByType<WaterPump>();
        if (_snapper == null) _snapper = FindFirstObjectByType<MovingPlatformSnapper>();
        if (_deckAnchor == null) _deckAnchor = FindFirstObjectByType<BoatDeckAnchor>();
    }

    private void Layout()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(PanelWidth, 36f);
    }

    private void FitToText()
    {
        if (_goalText == null) return;
        var rt = transform as RectTransform;
        if (rt == null) return;
        float textHeight = _goalText.preferredHeight;
        float total = Mathf.Max(MinHeight, textHeight + PaddingTop + PaddingBottom);
        var size = rt.sizeDelta;
        if (!Mathf.Approximately(size.y, total))
        {
            size.y = total;
            rt.sizeDelta = size;
        }
    }

    private void LateUpdate()
    {
        if (PauseMenuUI.IsPaused) return;
        if (GameOverUI.Instance != null && GameOverUI.Instance.IsShown) return;

        FindRefs();
        UpdatePhase();
        Render();
    }

    private void UpdatePhase()
    {
        switch (_phase)
        {
            case 0:
                if (InventoryHasBurnable()) _phase = 1;
                break;
            case 1:
                if (_generator != null && _generator.StoredEnergy >= energyTarget) _phase = 2;
                break;
            case 2:
                if (_pump != null && _pump.IsActive) _phase = 3;
                break;
            case 3:
                if (IsOnIsland()) _phase = 4;
                break;
            case 4:
                if (IsOnBoat()) _phase = 5;
                break;
            case 5:
                if (InventoryHasPatch()) _phase = 6;
                break;
            case 6:
                if (CountPatchedHoles() >= holesToPatch) _phase = 7;
                break;
        }

        if (_phase == 7)
        {
            if (GameOverUI.Instance != null && !GameOverUI.Instance.IsShown)
                GameOverUI.Instance.Show("You win!");
        }
    }

    private void Render()
    {
        string text;
        switch (_phase)
        {
            case 0:
                text = "Goal: Pick up a red log";
                break;
            case 1:
            {
                int stored = _generator != null ? Mathf.RoundToInt(_generator.StoredEnergy) : 0;
                int target = Mathf.RoundToInt(energyTarget);
                text = $"Goal: Put the red log into the generator. Energy stored {stored}/{target}";
                break;
            }
            case 2:
                text = "Goal: Turn on the pump";
                break;
            case 3:
                text = "Goal: Explore the island";
                break;
            case 4:
                text = "Goal: Return to the boat";
                break;
            case 5:
                text = "Goal: Craft a Patch";
                break;
            case 6:
            {
                int patched = CountPatchedHoles();
                text = $"Goal: Seal the holes {patched}/{holesToPatch}";
                break;
            }
            default:
                text = "";
                break;
        }
        _goalText.text = text;
        _goalText.color = defaultColor;
        _bgImage.color = bgColor;
        FitToText();
    }

    private bool InventoryHasBurnable()
    {
        if (_inventory == null) return false;
        int n = _inventory.GetDisplaySlotCount();
        for (int i = 0; i < n; i++)
        {
            var s = _inventory.GetDisplaySlot(i);
            if (!s.hasItem || s.isSecondHalf || s.item == null) continue;
            if (s.item.GetComponent<BurnableItem>() != null) return true;
        }
        return false;
    }

    private bool InventoryHasPatch()
    {
        if (_inventory == null) return false;
        int n = _inventory.GetDisplaySlotCount();
        for (int i = 0; i < n; i++)
        {
            var s = _inventory.GetDisplaySlot(i);
            if (!s.hasItem || s.isSecondHalf || s.item == null) continue;
            if (s.item.GetComponent<Patch>() != null) return true;
        }
        return false;
    }

    private bool IsOnIsland()
    {
        if (_snapper == null) return false;
        return _snapper.CurrentPlatform != null;
    }

    private bool IsOnBoat()
    {
        if (_deckAnchor == null) return false;
        return _deckAnchor.HasPassengers;
    }

    private int CountPatchedHoles()
    {
        var holes = FindObjectsByType<BoatHole>(FindObjectsSortMode.None);
        if (_initialHoleCount < 0) _initialHoleCount = Mathf.Max(holes.Length, holesToPatch);
        int remaining = 0;
        for (int i = 0; i < holes.Length; i++) if (holes[i] != null && !holes[i].IsPatched) remaining++;
        int patched = _initialHoleCount - remaining;
        if (patched < 0) patched = 0;
        if (patched > holesToPatch) patched = holesToPatch;
        return patched;
    }
}
