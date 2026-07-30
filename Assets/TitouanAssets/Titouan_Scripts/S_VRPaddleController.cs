using UnityEngine;
using UnityEngine.InputSystem;

public class VRPaddleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanoeMotor canoeMotor;
    [SerializeField] private Transform waterSurface;

    [SerializeField] private Transform leftPaddleTip;
    [SerializeField] private Transform rightPaddleTip;

    [SerializeField] private VRAvatarController avatarController;

    [Header("Input")]
    [SerializeField]
    private InputActionReference leftGripAction;

    [SerializeField]
    private InputActionReference rightGripAction;

    [Header("Stroke detection")]
    [SerializeField] private float waterTolerance = 0.08f;
    [SerializeField] private float minimumPullSpeed = 0.15f;
    [SerializeField] private float fullPullSpeed = 1.2f;
    [SerializeField] private float minimumSideDistance = 0.15f;

    [Header("Output")]
    [SerializeField] private float propulsionMultiplier = 0.65f;
    [SerializeField] private float steeringStrength = 0.85f;
    [SerializeField] private float inputResponse = 8f;

    private Vector3 previousLeftLocalPosition;
    private Vector3 previousRightLocalPosition;

    private float currentForwardInput;
    private float currentTurnInput;

    private bool leftActionWasEnabled;
    private bool rightActionWasEnabled;

    private struct PaddleSample
    {
        public float power;
        public float turn;
    }

    private void OnEnable()
    {
        EnableAction(
            leftGripAction,
            out leftActionWasEnabled);

        EnableAction(
            rightGripAction,
            out rightActionWasEnabled);

        ResetTracking();
    }

    private void OnDisable()
    {
        RestoreActionState(
            leftGripAction,
            leftActionWasEnabled);

        RestoreActionState(
            rightGripAction,
            rightActionWasEnabled);

        currentForwardInput = 0f;
        currentTurnInput = 0f;

        if (canoeMotor != null)
            canoeMotor.StopImmediately();

        if (avatarController != null)
            avatarController.SetRowingAmount(0f);
    }

    private void Update()
    {
        if (canoeMotor == null ||
            leftPaddleTip == null ||
            rightPaddleTip == null ||
            waterSurface == null ||
            Time.deltaTime <= 0f)
        {
            return;
        }

        PaddleSample leftSample =
            EvaluatePaddle(
                leftPaddleTip,
                leftGripAction,
                ref previousLeftLocalPosition);

        PaddleSample rightSample =
            EvaluatePaddle(
                rightPaddleTip,
                rightGripAction,
                ref previousRightLocalPosition);

        float targetForward =
            Mathf.Clamp01(
                (leftSample.power +
                 rightSample.power) *
                propulsionMultiplier);

        float targetTurn =
            Mathf.Clamp(
                (leftSample.turn +
                 rightSample.turn) *
                steeringStrength,
                -1f,
                1f);

        currentForwardInput =
            Mathf.MoveTowards(
                currentForwardInput,
                targetForward,
                inputResponse * Time.deltaTime);

        currentTurnInput =
            Mathf.MoveTowards(
                currentTurnInput,
                targetTurn,
                inputResponse * Time.deltaTime);

        canoeMotor.SetDriveInput(
            currentForwardInput,
            currentTurnInput);

        if (avatarController != null)
        {
            avatarController.SetRowingAmount(
                currentForwardInput);
        }
    }

    private PaddleSample EvaluatePaddle(
        Transform paddleTip,
        InputActionReference gripAction,
        ref Vector3 previousLocalPosition)
    {
        PaddleSample result = new PaddleSample();

        Vector3 currentLocalPosition =
            canoeMotor.transform.InverseTransformPoint(
                paddleTip.position);

        Vector3 localVelocity =
            (currentLocalPosition -
             previousLocalPosition) /
            Time.deltaTime;

        previousLocalPosition =
            currentLocalPosition;

        bool gripHeld =
            gripAction != null &&
            gripAction.action != null &&
            gripAction.action.IsPressed();

        bool bladeInWater =
            paddleTip.position.y <=
            waterSurface.position.y +
            waterTolerance;

        float backwardPullSpeed =
            -localVelocity.z;

        if (!gripHeld ||
            !bladeInWater ||
            backwardPullSpeed <
            minimumPullSpeed)
        {
            return result;
        }

        result.power = Mathf.InverseLerp(
            minimumPullSpeed,
            fullPullSpeed,
            backwardPullSpeed);

        if (Mathf.Abs(currentLocalPosition.x) >=
            minimumSideDistance)
        {
            // Pagaie à gauche :
            // le bateau tourne vers la droite.
            if (currentLocalPosition.x < 0f)
                result.turn = result.power;
            else
                result.turn = -result.power;
        }

        return result;
    }

    private void ResetTracking()
    {
        if (canoeMotor == null)
            return;

        if (leftPaddleTip != null)
        {
            previousLeftLocalPosition =
                canoeMotor.transform
                    .InverseTransformPoint(
                        leftPaddleTip.position);
        }

        if (rightPaddleTip != null)
        {
            previousRightLocalPosition =
                canoeMotor.transform
                    .InverseTransformPoint(
                        rightPaddleTip.position);
        }
    }

    private static void EnableAction(
        InputActionReference actionReference,
        out bool wasAlreadyEnabled)
    {
        wasAlreadyEnabled = false;

        if (actionReference == null ||
            actionReference.action == null)
        {
            return;
        }

        wasAlreadyEnabled =
            actionReference.action.enabled;

        if (!wasAlreadyEnabled)
            actionReference.action.Enable();
    }

    private static void RestoreActionState(
        InputActionReference actionReference,
        bool wasAlreadyEnabled)
    {
        if (actionReference == null ||
            actionReference.action == null)
        {
            return;
        }

        if (!wasAlreadyEnabled)
            actionReference.action.Disable();
    }
}