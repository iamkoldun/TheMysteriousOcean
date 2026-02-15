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
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        HandleMouseLook();
        ApplyGravity();
        
        // Move the character
        controller.Move(velocity * Time.deltaTime);
        
        // Unlock cursor with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Lock cursor on click
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void HandleGroundCheck()
    {
        // Check if grounded using CharacterController
        isGrounded = controller.isGrounded;
        
        // Additional raycast check for more reliable ground detection
        if (!isGrounded)
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundMask);
        }
        
        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
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
        
        // Determine target speed (sprint or walk); sprint only if stamina allows
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && vertical > 0;
        bool canSprint = stamina == null || stamina.CanSprint;
        bool isSprinting = wantsSprint && canSprint;
        if (stamina != null) stamina.SetSprinting(isSprinting);
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetMovement = direction * targetSpeed;
        
        // Smooth acceleration/deceleration
        float smoothSpeed = direction.magnitude > 0 ? acceleration : deceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, smoothSpeed * Time.deltaTime);
        
        // Apply horizontal movement
        velocity.x = currentMovement.x;
        velocity.z = currentMovement.z;
    }
    
    private void HandleJump()
    {
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
    
    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(origin, origin + Vector3.down * (groundCheckDistance + 0.1f));
    }
}