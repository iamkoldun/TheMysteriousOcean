using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay shown on game over (boat sinking, drowning, etc.).
/// Pauses the game and offers Restart / Main Menu buttons.
/// Matches the visual style of MainMenu and PauseMenu.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [SerializeField] private BoatFlooding boatFlooding;
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Sprite buttonSprite;

    private bool _shown;
    public bool IsShown => _shown;

    private void Awake()
    {
        Instance = this;
        if (boatFlooding == null) boatFlooding = FindFirstObjectByType<BoatFlooding>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (_shown || boatFlooding == null || !boatFlooding.IsGameOver) return;
        Show("The boat has sunk!");
    }

    public void Show(string reason)
    {
        if (_shown) return;
        _shown = true;
        CreatePanel(reason);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Restart()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    private void ExitToMenu()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void CreatePanel(string reason)
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        // Dark overlay
        var panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);
        var panelRt = panel.transform as RectTransform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.text = reason;
        var titleRt = titleGo.transform as RectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 80f);
        titleRt.sizeDelta = new Vector2(800f, 100f);

        CreateButton(panel.transform, "Restart", new Vector2(0f, -20f), Restart);
        CreateButton(panel.transform, "Main Menu", new Vector2(0f, -110f), ExitToMenu);
    }

    private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(label);
        btnGo.transform.SetParent(parent, false);

        var btnImg = btnGo.AddComponent<Image>();
        if (buttonSprite != null)
        {
            btnImg.sprite = buttonSprite;
            btnImg.type = Image.Type.Sliced;
        }
        btnImg.color = Color.white;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.93f, 0.78f, 0.71f, 1f);
        colors.pressedColor = new Color(0.82f, 0.63f, 0.53f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        var btnRt = btnGo.transform as RectTransform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = position;
        btnRt.sizeDelta = new Vector2(300f, 70f);

        // TMP label
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) text.font = fontAsset;
        text.fontSize = 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = label;

        var textRt = textGo.transform as RectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }
}
