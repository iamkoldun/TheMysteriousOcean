using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovingPlatformSnapper : MonoBehaviour
{
    private CharacterController controller;
    private SpawnedWorldObject currentPlatform;

    public SpawnedWorldObject CurrentPlatform => currentPlatform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (controller == null || !controller.enabled)
        {
            currentPlatform = null;
            return;
        }

        float rayLength = controller.height * 0.5f + controller.skinWidth + 0.5f;
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength))
        {
            SpawnedWorldObject platform = hit.collider.GetComponentInParent<SpawnedWorldObject>();
            if (platform != null && platform.IsWorldMoving)
            {
                Vector3 delta = platform.LastMoveDelta;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    controller.Move(delta);
                }

                currentPlatform = platform;
                return;
            }
        }

        currentPlatform = null;
    }
}
