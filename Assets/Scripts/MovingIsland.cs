using UnityEngine;

public class MovingIsland : MonoBehaviour
{
    [SerializeField] private Vector3 moveDirection = Vector3.left;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private IslandDestroyer destroyer;

    private Renderer[] cachedRenderers;

    private void Awake()
    {
        CacheRenderers();
    }

    private void Update()
    {
        Vector3 normalizedDirection = GetNormalizedDirection();
        transform.position += normalizedDirection * moveSpeed * Time.deltaTime;

        if (destroyer != null && destroyer.Intersects(GetWorldBounds()))
        {
            DestroyIsland();
        }
    }

    public void Initialize(Vector3 direction, float speed, IslandDestroyer islandDestroyer)
    {
        moveDirection = direction;
        moveSpeed = Mathf.Max(0f, speed);
        destroyer = islandDestroyer;
        CacheRenderers();
    }

    public void DestroyIsland()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    private Vector3 GetNormalizedDirection()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.left;
        }

        return moveDirection.normalized;
    }

    private Bounds GetWorldBounds()
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer currentRenderer = cachedRenderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = currentRenderer.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(currentRenderer.bounds);
        }

        if (!hasBounds)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        return combinedBounds;
    }
}
