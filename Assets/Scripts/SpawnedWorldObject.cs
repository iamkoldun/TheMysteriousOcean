using UnityEngine;

[DisallowMultipleComponent]
public class SpawnedWorldObject : MonoBehaviour
{
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

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
