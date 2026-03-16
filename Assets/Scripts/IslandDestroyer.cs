using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class IslandDestroyer : MonoBehaviour
{
    [SerializeField] private BoxCollider destroyerCollider;
    [SerializeField] private Renderer destroyerRenderer;
    [SerializeField] private Rigidbody destroyerRigidbody;

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
        SpawnedWorldObject worldObject = other.GetComponentInParent<SpawnedWorldObject>();
        if (worldObject != null)
        {
            worldObject.DestroySelf();
            return;
        }

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

        EnsureRigidbody();
        if (destroyerRigidbody != null)
        {
            destroyerRigidbody.isKinematic = true;
            destroyerRigidbody.useGravity = false;
        }

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

    private void EnsureRigidbody()
    {
        if (destroyerRigidbody == null)
        {
            destroyerRigidbody = GetComponent<Rigidbody>();
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
