using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IslandDestroyer : MonoBehaviour
{
    [SerializeField] private BoxCollider destroyerCollider;
    [SerializeField] private Renderer destroyerRenderer;

    public Bounds WorldBounds
    {
        get
        {
            EnsureCollider();
            EnsureRenderer();

            if (destroyerRenderer != null)
            {
                return destroyerRenderer.bounds;
            }

            return destroyerCollider.bounds;
        }
    }

    private void Reset()
    {
        ApplyDefaults();
    }

    private void OnValidate()
    {
        ApplyDefaults();
    }

    private void OnTriggerEnter(Collider other)
    {
        MovingIsland movingIsland = other.GetComponentInParent<MovingIsland>();
        if (movingIsland != null)
        {
            movingIsland.DestroyIsland();
        }
    }

    public bool Intersects(Bounds islandBounds)
    {
        return WorldBounds.Intersects(islandBounds);
    }

    private void ApplyDefaults()
    {
        EnsureCollider();
        destroyerCollider.isTrigger = true;

        EnsureRenderer();
        if (destroyerRenderer != null)
        {
            destroyerRenderer.enabled = false;
        }
    }

    private void EnsureCollider()
    {
        if (destroyerCollider == null)
        {
            destroyerCollider = GetComponent<BoxCollider>();
        }
    }

    private void EnsureRenderer()
    {
        if (destroyerRenderer == null)
        {
            destroyerRenderer = GetComponent<Renderer>();
        }
    }
}
