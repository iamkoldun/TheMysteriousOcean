using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoatDeckAnchor : MonoBehaviour
{
    [SerializeField] private Transform anchorTarget;
    [SerializeField] private BoxCollider deckTrigger;

    private readonly HashSet<CharacterController> passengers = new HashSet<CharacterController>();
    private Vector3 lastAnchorPosition;

    public bool HasPassengers
    {
        get
        {
            CleanupPassengers();
            return passengers.Count > 0;
        }
    }

    private void Awake()
    {
        ApplyDefaults();
        lastAnchorPosition = anchorTarget != null ? anchorTarget.position : Vector3.zero;
    }

    private void OnEnable()
    {
        lastAnchorPosition = anchorTarget != null ? anchorTarget.position : Vector3.zero;
    }

    private void Reset()
    {
        ApplyDefaults();
    }

    private void OnValidate()
    {
        ApplyDefaults();
    }

    private void LateUpdate()
    {
        if (anchorTarget == null)
        {
            return;
        }

        Vector3 anchorDelta = anchorTarget.position - lastAnchorPosition;
        if (anchorDelta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        CleanupPassengers();

        CharacterController[] cachedPassengers = new CharacterController[passengers.Count];
        passengers.CopyTo(cachedPassengers);

        for (int i = 0; i < cachedPassengers.Length; i++)
        {
            CharacterController controller = cachedPassengers[i];
            if (controller == null || !controller.enabled)
            {
                passengers.Remove(controller);
                continue;
            }

            controller.Move(anchorDelta);
        }

        lastAnchorPosition = anchorTarget.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddPassenger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAddPassenger(other);
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController controller = ResolveController(other);
        if (controller != null)
        {
            passengers.Remove(controller);
        }
    }

    private void TryAddPassenger(Collider other)
    {
        CharacterController controller = ResolveController(other);
        if (controller != null)
        {
            passengers.Add(controller);
        }
    }

    private void CleanupPassengers()
    {
        passengers.RemoveWhere(controller => controller == null || !controller.enabled);
    }

    private CharacterController ResolveController(Collider other)
    {
        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = other.GetComponentInParent<CharacterController>();
        }

        return controller;
    }

    private void ApplyDefaults()
    {
        if (anchorTarget == null && transform.parent != null)
        {
            anchorTarget = transform.parent;
        }

        if (deckTrigger == null)
        {
            deckTrigger = GetComponent<BoxCollider>();
        }

        if (deckTrigger != null)
        {
            deckTrigger.isTrigger = true;
        }

        Renderer deckRenderer = GetComponent<Renderer>();
        if (deckRenderer != null)
        {
            deckRenderer.enabled = false;
        }
    }
}
