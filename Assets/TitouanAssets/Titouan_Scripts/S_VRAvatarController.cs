using Unity.XR.CoreUtils;
using UnityEngine;

public class VRAvatarController : MonoBehaviour
{
    [Header("XR")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform head;

    [Header("Avatar")]
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private Animator animator;

    [Header("Standing")]
    [SerializeField] private float bodyForwardOffset = 0.08f;
    [SerializeField] private float bodyHeightOffset = 0f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private float referenceWalkSpeed = 2f;
    [SerializeField] private float animationDamping = 0.12f;

    private bool seated;
    private Transform seatAnchor;
    private Vector3 previousPosition;

    private bool hasMoveSpeed;
    private bool hasInBoat;
    private bool hasRowing;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int InBoatHash =
        Animator.StringToHash("InBoat");

    private static readonly int RowingHash =
        Animator.StringToHash("Rowing");

    private void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = GetComponentInParent<XROrigin>();

        if (avatarRoot != null)
            previousPosition = avatarRoot.position;

        DetectAnimatorParameters();
    }

    private void LateUpdate()
    {
        if (xrOrigin == null ||
            head == null ||
            avatarRoot == null)
        {
            return;
        }

        if (seated && seatAnchor != null)
        {
            UpdateSeatedPosition();
        }
        else
        {
            UpdateStandingPosition();
        }

        UpdateAnimation();
    }

    private void UpdateStandingPosition()
    {
        Vector3 forward = Vector3.ProjectOnPlane(
            head.forward,
            Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
            forward = avatarRoot.forward;

        forward.Normalize();

        Vector3 targetPosition =
            head.position -
            forward * bodyForwardOffset;

        targetPosition.y =
            xrOrigin.transform.position.y +
            bodyHeightOffset;

        avatarRoot.position = targetPosition;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                forward,
                Vector3.up);

        float interpolation =
            1f - Mathf.Exp(
                -rotationSpeed * Time.deltaTime);

        avatarRoot.rotation = Quaternion.Slerp(
            avatarRoot.rotation,
            targetRotation,
            interpolation);
    }

    private void UpdateSeatedPosition()
    {
        avatarRoot.SetPositionAndRotation(
            seatAnchor.position,
            seatAnchor.rotation);
    }

    private void UpdateAnimation()
    {
        if (avatarRoot == null)
            return;

        if (animator == null || !hasMoveSpeed)
        {
            previousPosition = avatarRoot.position;
            return;
        }

        float normalizedSpeed = 0f;

        if (!seated && Time.deltaTime > 0f)
        {
            Vector3 displacement =
                avatarRoot.position -
                previousPosition;

            displacement.y = 0f;

            float actualSpeed =
                displacement.magnitude /
                Time.deltaTime;

            normalizedSpeed = Mathf.Clamp01(
                actualSpeed /
                Mathf.Max(referenceWalkSpeed, 0.01f));
        }

        animator.SetFloat(
            MoveSpeedHash,
            normalizedSpeed,
            animationDamping,
            Time.deltaTime);

        previousPosition = avatarRoot.position;
    }

    public void SetSeated(
        bool value,
        Transform newSeatAnchor)
    {
        seated = value;
        seatAnchor = newSeatAnchor;

        if (animator == null)
            return;

        if (hasInBoat)
        {
            animator.SetBool(
                InBoatHash,
                seated);
        }

        if (hasMoveSpeed)
        {
            animator.SetFloat(
                MoveSpeedHash,
                0f);
        }

        if (!seated && hasRowing)
        {
            animator.SetBool(
                RowingHash,
                false);
        }

        if (avatarRoot != null)
            previousPosition = avatarRoot.position;
    }

    public void SetRowingAmount(float amount)
    {
        if (animator == null || !hasRowing)
            return;

        bool isRowing =
            seated &&
            amount > 0.05f;

        animator.SetBool(
            RowingHash,
            isRowing);
    }

    private void DetectAnimatorParameters()
    {
        hasMoveSpeed = false;
        hasInBoat = false;
        hasRowing = false;

        if (animator == null ||
            animator.runtimeAnimatorController == null)
        {
            return;
        }

        hasMoveSpeed = HasParameter(
            MoveSpeedHash,
            AnimatorControllerParameterType.Float);

        hasInBoat = HasParameter(
            InBoatHash,
            AnimatorControllerParameterType.Bool);

        hasRowing = HasParameter(
            RowingHash,
            AnimatorControllerParameterType.Bool);
    }

    private bool HasParameter(
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
            return false;

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}