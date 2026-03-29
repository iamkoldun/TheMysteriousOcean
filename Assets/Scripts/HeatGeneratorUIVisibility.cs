using UnityEngine;

/// <summary>
/// Lives on an always-active object (e.g. InGameUI). Raycasts to detect HeatGenerator and turns the panel on/off.
/// Does NOT disable itself — only toggles the panel child.
/// </summary>
public class HeatGeneratorUIVisibility : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private HeatGeneratorUI panelUI;
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 5f;
    [SerializeField] private LayerMask interactLayer = -1;

    private void LateUpdate()
    {
        if (panel == null || panelUI == null) return;

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) cam = player.GetComponentInChildren<Camera>();
            }
        }
        if (cam == null)
        {
            panel.SetActive(false);
            panelUI.SetCurrentGenerator(null);
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, interactLayer))
        {
            var gen = hit.collider.GetComponentInParent<HeatGenerator>();
            if (gen != null)
            {
                panelUI.SetCurrentGenerator(gen);
                if (!panel.activeSelf) panelUI.EnsureLayout();
                panel.SetActive(true);
                return;
            }
        }
        panelUI.SetCurrentGenerator(null);
        panel.SetActive(false);
    }
}
