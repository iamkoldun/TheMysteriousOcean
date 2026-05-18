using UnityEngine;

public class StepZone : MonoBehaviour
{
    public float stepOffsetWhileInside = 1.5f;
    private float stepOffsetWhileOutnside = 0.3f;
    private CharacterController playerController;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out playerController))
        {
            stepOffsetWhileOutnside = playerController.stepOffset;
            playerController.stepOffset = stepOffsetWhileInside;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CharacterController>(out playerController))
        {
            playerController.stepOffset = stepOffsetWhileOutnside;
        }
    }
}
