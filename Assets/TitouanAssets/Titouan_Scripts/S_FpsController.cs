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
    [Header("Water Respawn")]
    [SerializeField] private LayerMask islandGroundLayer;
    [SerializeField] private float respawnSearchRadius = 30f;
    [SerializeField] private float respawnSearchStep = 1f;
    [SerializeField] private float respawnRayHeight = 100f;
    [SerializeField] private float respawnHeightOffset = 0.1f;
    [SerializeField] private float minimumGroundAboveWater = 0.5f;
    [SerializeField] private float maximumRespawnSlope = 35f;
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
    //private void UpdateWaterSafety()
    //{
    //    if (waterSurface == null)
    //        return;

    //    float feetHeight = characterController.bounds.min.y;
    //    float waterHeight = waterSurface.position.y;

    //    // Enregistre la dernière position valide sur une île.
    //    if (IsValidGrounded())
    //    {
    //        lastSafePosition = transform.position;
    //        return;
    //    }

    //    // Replace le joueur s'il descend trop profondément.
    //    if (feetHeight < waterHeight - respawnDepth)
    //    {
    //        characterController.enabled = false;

    //        transform.position = lastSafePosition;

    //        characterController.enabled = true;

    //        verticalVelocity = groundedVelocity;
    //    }
    //}
    private void UpdateWaterSafety()
    {
        if (waterSurface == null)
            return;

        float feetHeight =
            characterController.bounds.min.y;

        float waterHeight =
            waterSurface.position.y;

        // Le joueur est sur une île :
        // on mémorise sa position comme position sûre.
        if (IsValidGrounded())
        {
            lastSafePosition = transform.position;
            return;
        }

        // Le joueur est suffisamment profondément
        // sous la surface de l'eau.
        if (feetHeight < waterHeight - respawnDepth)
        {
            Vector3 waterContactPosition =
                transform.position;

            if (FindNearestIslandGround(
                waterContactPosition,
                out Vector3 nearestGround))
            {
                characterController.enabled = false;

                transform.position =
                    nearestGround +
                    Vector3.up * respawnHeightOffset;

                characterController.enabled = true;
            }
            else
            {
                // Sécurité : si aucune île n'est trouvée,
                // revenir à la dernière position sûre.
                characterController.enabled = false;

                transform.position =
                    lastSafePosition;

                characterController.enabled = true;
            }

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
    private bool FindNearestIslandGround(
       Vector3 waterContactPosition,
       out Vector3 groundPosition)
    {
        groundPosition = Vector3.zero;

        if (waterSurface == null)
        {
            Debug.LogError(
                "[FpsController] Water Surface n'est pas assignée.");

            return false;
        }

        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogError(
                "[FpsController] Aucun Terrain actif trouvé.");

            return false;
        }

        float waterHeight =
            waterSurface.position.y;

        float closestDistance =
            float.MaxValue;

        bool found = false;

        foreach (Terrain terrain in terrains)
        {
            // Sécurité : Terrain inexistant
            if (terrain == null)
                continue;

            // Sécurité : TerrainData inexistant
            TerrainData terrainData =
                terrain.terrainData;

            if (terrainData == null)
            {
                Debug.LogWarning(
                    "[FpsController] Le Terrain " +
                    terrain.name +
                    " n'a pas de TerrainData.");

                continue;
            }

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3 terrainSize =
                terrainData.size;

            // Vérification de la taille du terrain
            if (terrainSize.x <= 0f ||
                terrainSize.z <= 0f)
            {
                continue;
            }

            float minX =
                terrainPosition.x;

            float maxX =
                terrainPosition.x +
                terrainSize.x;

            float minZ =
                terrainPosition.z;

            float maxZ =
                terrainPosition.z +
                terrainSize.z;

            // On trouve le point du terrain
            // horizontalement le plus proche du joueur.
            float closestX =
                Mathf.Clamp(
                    waterContactPosition.x,
                    minX,
                    maxX);

            float closestZ =
                Mathf.Clamp(
                    waterContactPosition.z,
                    minZ,
                    maxZ);

            // Coordonnées normalisées du terrain
            float normalizedX =
                Mathf.InverseLerp(
                    minX,
                    maxX,
                    closestX);

            float normalizedZ =
                Mathf.InverseLerp(
                    minZ,
                    maxZ,
                    closestZ);

            // Hauteur réelle du terrain
            float terrainHeight =
                terrain.SampleHeight(
                    new Vector3(
                        closestX,
                        0f,
                        closestZ));

            terrainHeight +=
                terrainPosition.y;

            // Le terrain doit être suffisamment
            // au-dessus de l'eau.
            if (terrainHeight <
                waterHeight +
                minimumGroundAboveWater)
            {
                continue;
            }

            // Vérification de la pente
            Vector3 normal =
                terrainData.GetInterpolatedNormal(
                    normalizedX,
                    normalizedZ);

            float slope =
                Vector3.Angle(
                    normal,
                    Vector3.up);

            if (slope > maximumRespawnSlope)
                continue;

            Vector3 candidate =
                new Vector3(
                    closestX,
                    terrainHeight,
                    closestZ);

            float distance =
                Vector3.Distance(
                    waterContactPosition,
                    candidate);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                groundPosition = candidate;
                found = true;
            }
        }

        return found;
    }
}