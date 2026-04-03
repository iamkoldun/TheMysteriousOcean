using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -15f;

    [Header("Swimming Settings")]
    [SerializeField] private float swimMoveSpeed = 3.5f;
    [SerializeField] private float swimAcceleration = 6f;
    [SerializeField] private float swimDeceleration = 5f;
    [SerializeField] private float swimAscendSpeed = 3.25f;
    [SerializeField] private float swimSinkSpeed = 1.6f;
    [SerializeField] private float swimVerticalAcceleration = 10f;
    [SerializeField] private float surfaceDampingDistance = 0.6f;
    [SerializeField] private float surfaceHopDistance = 0.18f;
    [SerializeField] private float surfaceHopSpeed = 4.35f;
    [SerializeField] private float swimStaminaDrainPerSecond = 18f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = -1;
    
    [Header("Stamina (optional)")]
    [SerializeField] private Stamina stamina;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -90f;
    [SerializeField] private float maxVerticalAngle = 90f;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSmoothSpeed = 15f;
    
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isGrounded;
    private bool isSwimming;
    private WaterVolume currentWater;
    
    private float cameraPitch = 0f;
    private float targetCameraPitch = 0f;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Lock and hide cursor for FPS experience
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (stamina == null) stamina = GetComponent<Stamina>();

        // Find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("No camera found! Assign camera transform manually.");
            }
        }
    }
    
    private void Update()
    {
        if (PauseMenuUI.IsPaused) return;
        if (GameOverUI.Instance != null && GameOverUI.Instance.IsShown) return;

        UpdateSwimmingState();
        HandleGroundCheck();
        HandleMovement();
        HandleJumpOrSwim();
        HandleMouseLook();
        ApplyVerticalForces();

        // Move the character
        controller.Move(velocity * Time.deltaTime);

        if (stamina != null)
        {
            stamina.FinishFrame(!isSwimming);
        }

        // Lock cursor on click (not when inventory is open — inventory needs free cursor)
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None && !InventoryScreenUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void HandleGroundCheck()
    {
        bool hasGroundHit = TryGetGroundHit(out RaycastHit groundHit);

        // Check if grounded using CharacterController plus a raycast for stable platform detection.
        isGrounded = controller.isGrounded || hasGroundHit;

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0f && !isSwimming)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
    }
    
    private void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Calculate movement direction relative to player rotation
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction.Normalize();

        float targetSpeed = walkSpeed;
        float smoothSpeed = direction.magnitude > 0f ? acceleration : deceleration;

        if (isSwimming)
        {
            targetSpeed = swimMoveSpeed;
            smoothSpeed = direction.magnitude > 0f ? swimAcceleration : swimDeceleration;
        }
        else
        {
            // Determine target speed (sprint or walk); sprint only if stamina allows
            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && vertical > 0f;
            bool canSprint = stamina == null || stamina.CanSprint;
            bool isSprinting = wantsSprint && canSprint;

            if (stamina != null)
            {
                stamina.SetSprinting(isSprinting);
            }

            targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }

        Vector3 targetMovement = direction * targetSpeed;
        
        // Smooth acceleration/deceleration
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, smoothSpeed * Time.deltaTime);
        
        // Apply horizontal movement
        velocity.x = currentMovement.x;
        velocity.z = currentMovement.z;
    }
    
    private void HandleJumpOrSwim()
    {
        if (isSwimming)
        {
            bool wantsAscend = Input.GetButton("Jump");
            bool canAscend = wantsAscend && (stamina == null || stamina.Use(swimStaminaDrainPerSecond));

            float targetVerticalSpeed = canAscend ? swimAscendSpeed : -swimSinkSpeed;
            float distanceToSurface = currentWater != null ? currentWater.SurfaceY - GetHeadY() : 0f;

            if (canAscend && distanceToSurface < surfaceDampingDistance)
            {
                float surfaceFactor = Mathf.Clamp01(distanceToSurface / Mathf.Max(0.01f, surfaceDampingDistance));
                targetVerticalSpeed *= Mathf.Lerp(0.2f, 1f, surfaceFactor);
            }

            if (canAscend && distanceToSurface <= surfaceHopDistance)
            {
                targetVerticalSpeed = Mathf.Max(targetVerticalSpeed, surfaceHopSpeed);
            }

            velocity.y = Mathf.MoveTowards(velocity.y, targetVerticalSpeed, swimVerticalAcceleration * Time.deltaTime);

            if (isGrounded && velocity.y < -0.35f)
            {
                velocity.y = -0.35f;
            }

            return;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Calculate jump velocity using physics formula: v = sqrt(2 * jumpHeight * gravity)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    private void HandleMouseLook()
    {
        if (cameraTransform == null || Cursor.lockState != CursorLockMode.Locked)
            return;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player body horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Calculate vertical camera rotation (pitch)
        targetCameraPitch -= mouseY;
        targetCameraPitch = Mathf.Clamp(targetCameraPitch, minVerticalAngle, maxVerticalAngle);
        
        // Apply camera rotation with optional smoothing
        if (smoothRotation)
        {
            cameraPitch = Mathf.Lerp(cameraPitch, targetCameraPitch, rotationSmoothSpeed * Time.deltaTime);
        }
        else
        {
            cameraPitch = targetCameraPitch;
        }
        
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
    
    private void ApplyVerticalForces()
    {
        if (isSwimming)
        {
            return;
        }

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void UpdateSwimmingState()
    {
        if (currentWater == null)
        {
            isSwimming = false;
            return;
        }

        float feetY = GetFeetY();
        float chestY = GetChestY();
        isSwimming = feetY < currentWater.SurfaceY && chestY <= currentWater.SurfaceY;

        if (isSwimming && velocity.y < -swimSinkSpeed)
        {
            velocity.y = -swimSinkSpeed;
        }
    }

    private float GetFeetY()
    {
        return transform.position.y + controller.center.y - (controller.height * 0.5f);
    }

    private float GetChestY()
    {
        return GetFeetY() + controller.height * 0.65f;
    }

    private float GetHeadY()
    {
        return GetFeetY() + controller.height;
    }

    private void TrySetWater(Collider other)
    {
        WaterVolume water = other.GetComponent<WaterVolume>();
        if (water == null)
        {
            water = other.GetComponentInParent<WaterVolume>();
        }

        if (water != null)
        {
            currentWater = water;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySetWater(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetWater(other);
    }

    private void OnTriggerExit(Collider other)
    {
        WaterVolume water = other.GetComponent<WaterVolume>();
        if (water == null)
        {
            water = other.GetComponentInParent<WaterVolume>();
        }

        if (water != null && water == currentWater)
        {
            currentWater = null;
            isSwimming = false;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(origin, origin + Vector3.down * (groundCheckDistance + 0.1f));
    }

    private bool TryGetGroundHit(out RaycastHit groundHit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        float castRadius = Mathf.Max(0.05f, controller.radius * 0.9f);
        float castDistance = groundCheckDistance + 0.2f;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            castRadius,
            Vector3.down,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        groundHit = default;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            groundHit = hit;
        }

        return nearestDistance < float.MaxValue;
    }

}
