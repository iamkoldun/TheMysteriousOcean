using UnityEngine;

[DisallowMultipleComponent]
public class SpawnedWorldObject : MonoBehaviour
{
    public static float GlobalWorldSpeed { get; set; }

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    private Vector3 worldMoveDirection;
    private bool isWorldMoving;

    public bool IsWorldMoving => isWorldMoving;
    public Vector3 LastMoveDelta { get; private set; }

    public Bounds WorldBounds
    {
        get
        {
            if (!TryGetWorldBounds(out Bounds bounds))
            {
                bounds = new Bounds(transform.position, Vector3.one);
            }

            return bounds;
        }
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnValidate()
    {
        CacheComponents();
    }

    private void Update()
    {
        if (!isWorldMoving || GlobalWorldSpeed <= 0f)
        {
            LastMoveDelta = Vector3.zero;
            return;
        }

        Vector3 delta = worldMoveDirection * GlobalWorldSpeed * Time.deltaTime;
        transform.position += delta;
        LastMoveDelta = delta;
    }

    public void SetWorldMovement(Vector3 direction)
    {
        worldMoveDirection = direction.normalized;
        isWorldMoving = true;
    }

    public void DestroySelf()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        if (cachedRenderers == null || cachedColliders == null)
        {
            CacheComponents();
        }

        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            Collider currentCollider = cachedColliders[i];
            if (currentCollider == null || !currentCollider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(currentCollider.bounds);
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer currentRenderer = cachedRenderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(currentRenderer.bounds);
        }

        return hasBounds;
    }

    private void CacheComponents()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }
}
