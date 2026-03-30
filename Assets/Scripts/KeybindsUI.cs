using UnityEngine;

public class KeybindsUI : MonoBehaviour
{
    private GUIStyle headerStyle;
    private GUIStyle keyStyle;
    private GUIStyle actionStyle;
    private GUIStyle backgroundStyle;
    private bool stylesInitialized;

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

    void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 1f, 1f, 0.9f) },
            alignment = TextAnchor.MiddleCenter
        };

        keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.85f, 0.4f, 0.9f) },
            alignment = TextAnchor.MiddleRight
        };

        actionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(1f, 1f, 1f, 0.75f) },
            alignment = TextAnchor.MiddleLeft
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

        float panelWidth = 200f;
        float lineHeight = 20f;
        float headerHeight = 26f;
        float padding = 8f;
        float panelHeight = headerHeight + keybinds.Length * lineHeight + padding * 2;
        float margin = 10f;

        Rect panelRect = new Rect(Screen.width - panelWidth - margin, margin, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, backgroundStyle);

        float y = panelRect.y + padding;
        GUI.Label(new Rect(panelRect.x, y, panelWidth, headerHeight), "Controls", headerStyle);
        y += headerHeight;

        float keyColWidth = 55f;
        float gap = 6f;

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
