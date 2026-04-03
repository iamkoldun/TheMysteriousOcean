using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay shown when the boat is fully flooded.
/// Pauses the game and offers Restart / Main Menu buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private BoatFlooding boatFlooding;

    private GameObject _panel;
    private bool _shown;

    private void Awake()
    {
        if (boatFlooding == null) boatFlooding = FindFirstObjectByType<BoatFlooding>();
        CreatePanel();
    }

    private void LateUpdate()
    {
        if (_shown || boatFlooding == null || !boatFlooding.IsGameOver) return;
        Show();
    }

    private void Show()
    {
        _shown = true;
        _panel.SetActive(true);
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

    private void CreatePanel()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);
        var panelRt = _panel.transform as RectTransform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(_panel.transform, false);
        var titleText = titleGo.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        titleText.text = "\u041b\u043e\u0434\u043a\u0430 \u0437\u0430\u0442\u043e\u043d\u0443\u043b\u0430!";
        var titleRt = titleGo.transform as RectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 60f);
        titleRt.sizeDelta = new Vector2(500f, 50f);

        // Restart button
        CreateButton("\u041d\u0430\u0447\u0430\u0442\u044c \u0437\u0430\u043d\u043e\u0432\u043e", new Vector2(0f, -20f), Restart);

        // Main Menu button
        CreateButton("\u0413\u043b\u0430\u0432\u043d\u043e\u0435 \u043c\u0435\u043d\u044e", new Vector2(0f, -70f), ExitToMenu);

        _panel.SetActive(false);
    }

    private void CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(label);
        btnGo.transform.SetParent(_panel.transform, false);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        var btnRt = btnGo.transform as RectTransform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = position;
        btnRt.sizeDelta = new Vector2(240f, 40f);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        var textRt = textGo.transform as RectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }
}
