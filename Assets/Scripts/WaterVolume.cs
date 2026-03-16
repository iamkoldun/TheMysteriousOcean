using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WaterVolume : MonoBehaviour
{
    [Header("Water Volume")]
    [SerializeField] private float depth = 4f;

    public float SurfaceY => transform.position.y;
    public float BottomY => SurfaceY - depth;

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, -depth * 0.5f, 0f);
        trigger.size = new Vector3(10f, depth, 10f);
    }

    private void OnValidate()
    {
        depth = Mathf.Max(0.5f, depth);

        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger == null)
        {
            return;
        }

        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, -depth * 0.5f, 0f);
        trigger.size = new Vector3(10f, depth, 10f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.55f, 1f, 0.25f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(new Vector3(0f, -depth * 0.5f, 0f), new Vector3(10f, depth, 10f));
        Gizmos.matrix = previousMatrix;
    }
}
