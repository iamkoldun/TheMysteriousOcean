using UnityEngine;

/// <summary>
/// Toggles full-screen inventory overlay on Tab/Esc. Hides in-game UI root when open;
/// restores it and locks cursor when closed.
/// </summary>
public class InventoryScreenUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject inGameUIRoot;
    [SerializeField] private GameObject inventoryOverlayRoot;

    private void Awake()
    {
        if (inventoryOverlayRoot != null)
            inventoryOverlayRoot.SetActive(false);
    }

    private void Update()
    {
        if (PauseMenuUI.IsPaused) return;

        bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
        bool escPressed = Input.GetKeyDown(KeyCode.Escape);
        if (!tabPressed && !escPressed) return;
        if (inventoryOverlayRoot == null) return;

        bool isCurrentlyOpen = inventoryOverlayRoot.activeSelf;
        if (isCurrentlyOpen)
            Close();
        else if (tabPressed)
            Open();
    }

    private void OnDestroy()
    {
        IsOpen = false;
    }

    public void Open()
    {
        if (inventoryOverlayRoot == null) return;
        IsOpen = true;
        inventoryOverlayRoot.SetActive(true);
        if (inGameUIRoot != null) inGameUIRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (inventoryOverlayRoot == null) return;
        IsOpen = false;
        inventoryOverlayRoot.SetActive(false);
        if (inGameUIRoot != null) inGameUIRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
