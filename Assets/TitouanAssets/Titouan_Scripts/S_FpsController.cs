using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FpsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Transform viewYaw;
    [SerializeField] private Transform viewPitch;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Camera")]
    [SerializeField] private float horizontalSensitivity = 0.08f;
    [SerializeField] private float verticalSensitivity = 0.08f;
    [SerializeField] private float minimumPitch = -85f;
    [SerializeField] private float maximumPitch = 85f;

    [Header("Avatar animation")]
    [SerializeField] private Animator avatarAnimator;
    [SerializeField] private float animationDamping = 0.12f;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int GroundedHash =
        Animator.StringToHash("Grounded");
    [Header("Water")]
    [SerializeField] private Transform waterSurface;
    [SerializeField] private float groundHeightAboveWater = 0.05f;
    [Header("Water safety")]
    [SerializeField] private float respawnDepth = 1.2f;

    private Vector3 lastSafePosition;
    [Header("View positions")]
    [SerializeField]
    private Vector3 standingViewPosition = new Vector3(0f, 1.65f, 0f);

    [SerializeField]
    private Vector3 seatedViewPosition = new Vector3(0f, 1.15f, 0f);
    [Header("Avatar rotation")]
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private float avatarRotationSpeed = 12f;
    [SerializeField] private float avatarRotationOffset = 0f;
    private CharacterController characterController;
    private Transform originalParent;

    private float verticalVelocity;
    private float yaw;
    private float pitch;

    private bool movementEnabled = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        originalParent = transform.parent;

        viewPitch.localPosition = standingViewPosition;
        lastSafePosition = transform.position;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        UpdateLook();
        //UpdateAvatarRotation();

        if (movementEnabled)
        {
            UpdateMovement();
            UpdateWaterSafety();
        }

        UpdateAvatarAnimation();
    }

    private void UpdateLook()
    {
        if (input == null || viewYaw == null || viewPitch == null)
            return;

        Vector2 lookInput = input.Look;

        yaw += lookInput.x * horizontalSensitivity;
        pitch += lookInput.y * verticalSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

        viewYaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
        viewPitch.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    private bool IsValidGrounded()
    {
        if (!characterController.isGrounded)
            return false;

        // Aucun objet d'eau assigné : comportement Unity normal.
        if (waterSurface == null)
            return true;

        float feetHeight = characterController.bounds.min.y;
        float waterHeight = waterSurface.position.y;

        // Le terrain n'est considéré comme un sol que si les pieds
        // se trouvent au-dessus de la surface de l'eau.
        return feetHeight >= waterHeight + groundHeightAboveWater;
    }
    private void UpdateAvatarAnimation()
    {
        if (avatarAnimator == null)
            return;

        float normalizedSpeed = 0f;

        if (movementEnabled && characterController.enabled)
        {
            Vector3 horizontalVelocity =
                characterController.velocity;

            horizontalVelocity.y = 0f;

            float actualSpeed = horizontalVelocity.magnitude;

            normalizedSpeed = walkSpeed > 0f
                ? actualSpeed / walkSpeed
                : 0f;

            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        }

        avatarAnimator.SetFloat(
            MoveSpeedHash,
            normalizedSpeed,
            animationDamping,
            Time.deltaTime
        );

        avatarAnimator.SetBool(
            GroundedHash,
            IsValidGrounded()
        );
    }
    private void UpdateWaterSafety()
    {
        if (waterSurface == null)
            return;

        float feetHeight = characterController.bounds.min.y;
        float waterHeight = waterSurface.position.y;

        // Enregistre la dernière position valide sur une île.
        if (IsValidGrounded())
        {
            lastSafePosition = transform.position;
            return;
        }

        // Replace le joueur s'il descend trop profondément.
        if (feetHeight < waterHeight - respawnDepth)
        {
            characterController.enabled = false;

            transform.position = lastSafePosition;

            characterController.enabled = true;

            verticalVelocity = groundedVelocity;
        }
    }
    private void UpdateMovement()
    {
        if (input == null || !characterController.enabled)
            return;

        bool isGrounded = IsValidGrounded();
       
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedVelocity;

        if (isGrounded && input.JumpPressed)
        {
            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector2 moveInput = input.Move;

        Vector3 forward = viewYaw.forward;
        Vector3 right = viewYaw.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMovement =
            forward * moveInput.y +
            right * moveInput.x;

        if (horizontalMovement.sqrMagnitude > 1f)
            horizontalMovement.Normalize();

        float currentSpeed =
            input.SprintHeld ? sprintSpeed : walkSpeed;

        Vector3 velocity =
            horizontalMovement * currentSpeed;

        velocity.y = verticalVelocity;

        characterController.Move(
            velocity * Time.deltaTime);
    }
    private void UpdateAvatarRotation()
    {
        if (avatarRoot == null || viewYaw == null)
            return;

        float targetYaw =
            viewYaw.localEulerAngles.y + avatarRotationOffset;

        Quaternion targetRotation =
            Quaternion.Euler(0f, targetYaw, 0f);

        avatarRoot.localRotation = Quaternion.Slerp(
            avatarRoot.localRotation,
            targetRotation,
            avatarRotationSpeed * Time.deltaTime
        );
    }
    public void MountAt(Transform seatPoint)
    {
        if (seatPoint == null)
            return;

        movementEnabled = false;
        verticalVelocity = 0f;

        if (avatarAnimator != null)
            avatarAnimator.SetFloat(MoveSpeedHash, 0f);

        characterController.enabled = false;

        transform.SetParent(seatPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        viewPitch.localPosition = seatedViewPosition;

        ResetViewRotation();
    }

    public void DismountAt(
        Vector3 worldPosition,
        Quaternion worldRotation)
    {
        transform.SetParent(originalParent, true);
        transform.SetPositionAndRotation(
            worldPosition,
            worldRotation);

        viewPitch.localPosition = standingViewPosition;

        ResetViewRotation();

        characterController.enabled = true;
        movementEnabled = true;
        verticalVelocity = groundedVelocity;
    }

    private void ResetViewRotation()
    {
        yaw = 0f;
        pitch = 0f;

        viewYaw.localRotation = Quaternion.identity;
        viewPitch.localRotation = Quaternion.identity;
    }
}