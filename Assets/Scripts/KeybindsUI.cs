using UnityEngine;

public class KeybindsUI : MonoBehaviour
{
    private GUIStyle headerStyle;
    private GUIStyle keyStyle;
    private GUIStyle actionStyle;
    private GUIStyle hintStyle;
    private GUIStyle backgroundStyle;
    private bool stylesInitialized;
    float multiplier = 2f;

    private bool _showFullPanel;

    private readonly (string key, string action)[] keybinds = new[]
    {
        ("E", "Interact / Pick up"),
        ("Q", "Drop item"),
        ("F", "Swap hands"),
        ("1-9", "Hotbar slots"),
        ("Tab", "Inventory"),
        ("Esc", "Pause menu"),
        ("Shift", "Sprint"),
        ("Space", "Jump / Swim up"),
    };

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _showFullPanel = !_showFullPanel;
    }

    void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14 * (int)multiplier,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 1f, 1f, 0.9f) },
            alignment = TextAnchor.MiddleCenter
        };

        keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12 * (int)multiplier,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.85f, 0.4f, 0.9f) },
            alignment = TextAnchor.MiddleRight
        };

        actionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12 * (int)multiplier,
            normal = { textColor = new Color(1f, 1f, 1f, 0.75f) },
            alignment = TextAnchor.MiddleLeft
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13 * (int)multiplier,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.85f, 0.4f, 0.9f) },
            alignment = TextAnchor.MiddleRight
        };

        backgroundStyle = new GUIStyle();
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.4f));
        bgTex.Apply();
        backgroundStyle.normal.background = bgTex;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        InitStyles();
        float margin = 10f;

        if (!_showFullPanel)
        {
            float hintWidth = 140f * multiplier;
            float hintHeight = 24f * multiplier;
            Rect hintRect = new Rect(Screen.width - hintWidth - margin, margin, hintWidth, hintHeight);
            GUI.Label(hintRect, "F1 - Help", hintStyle);
            return;
        }

        float panelWidth = 200f * multiplier;
        float lineHeight = 20f * multiplier;
        float headerHeight = 26f * multiplier;
        float padding = 8f;
        float panelHeight = headerHeight + keybinds.Length * lineHeight + padding * 2;

        Rect panelRect = new Rect(Screen.width - panelWidth - margin, margin, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, backgroundStyle);

        float y = panelRect.y + padding;
        GUI.Label(new Rect(panelRect.x, y, panelWidth, headerHeight), "Controls", headerStyle);
        y += headerHeight;

        float keyColWidth = 55f * multiplier;
        float gap = 6f * multiplier;

        for (int i = 0; i < keybinds.Length; i++)
        {
            Rect keyRect = new Rect(panelRect.x + padding, y, keyColWidth, lineHeight);
            Rect actionRect = new Rect(panelRect.x + padding + keyColWidth + gap, y, panelWidth - keyColWidth - gap - padding * 2, lineHeight);
            GUI.Label(keyRect, keybinds[i].key, keyStyle);
            GUI.Label(actionRect, keybinds[i].action, actionStyle);
            y += lineHeight;
        }
    }
}
