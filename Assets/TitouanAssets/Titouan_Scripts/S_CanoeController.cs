using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CanoeController : MonoBehaviour
{
    [Header("Propulsion")]
    [SerializeField] private float forwardAcceleration = 5f;
    [SerializeField] private float reverseAcceleration = 2.5f;
    [SerializeField] private float maximumSpeed = 6f;

    [Header("Steering")]
    [SerializeField] private float turnAcceleration = 2.5f;
    [SerializeField] private float turnAccelerationAtRest = 0.8f;

    [Header("Water resistance")]
    [SerializeField] private float lateralDamping = 4f;

    private Rigidbody body;
    private PlayerInputReader driverInput;
    private Animator driverAnimator;

    private Vector2 cachedMoveInput;

    private static readonly int InBoatHash =
        Animator.StringToHash("InBoat");

    private static readonly int RowingHash =
        Animator.StringToHash("Rowing");

    private static readonly int RowSpeedHash =
        Animator.StringToHash("RowSpeed");

    private static readonly int TurnAmountHash =
        Animator.StringToHash("TurnAmount");

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        cachedMoveInput = driverInput != null
            ? driverInput.Move
            : Vector2.zero;

        UpdateDriverAnimation();
    }

    private void FixedUpdate()
    {
        if (driverInput == null)
            return;

        ApplyPropulsion();
        ApplySteering();
        ApplyLateralResistance();
        LimitSpeed();
    }

    private void ApplyPropulsion()
    {
        float inputValue = cachedMoveInput.y;

        if (Mathf.Abs(inputValue) < 0.01f)
            return;

        float acceleration = inputValue >= 0f
            ? forwardAcceleration
            : reverseAcceleration;

        float currentForwardSpeed =
            Vector3.Dot(body.velocity, transform.forward);

        bool belowMaximumSpeed =
            Mathf.Abs(currentForwardSpeed) < maximumSpeed;

        bool tryingToReverse =
            Mathf.Sign(inputValue) !=
            Mathf.Sign(currentForwardSpeed);

        if (belowMaximumSpeed || tryingToReverse)
        {
            body.AddForce(
                transform.forward *
                inputValue *
                acceleration,
                ForceMode.Acceleration);
        }
    }

    private void ApplySteering()
    {
        float turnInput = cachedMoveInput.x;

        if (Mathf.Abs(turnInput) < 0.01f)
            return;

        float forwardSpeed = Mathf.Abs(
            Vector3.Dot(body.velocity, transform.forward));

        float speedFactor = Mathf.InverseLerp(
            0f,
            maximumSpeed,
            forwardSpeed);

        float currentTurnAcceleration = Mathf.Lerp(
            turnAccelerationAtRest,
            turnAcceleration,
            speedFactor);

        body.AddTorque(
            Vector3.up *
            turnInput *
            currentTurnAcceleration,
            ForceMode.Acceleration);
    }

    private void ApplyLateralResistance()
    {
        float lateralSpeed =
            Vector3.Dot(body.velocity, transform.right);

        Vector3 lateralVelocity =
            transform.right * lateralSpeed;

        body.AddForce(
            -lateralVelocity * lateralDamping,
            ForceMode.Acceleration);
    }

    private void LimitSpeed()
    {
        Vector3 flatVelocity = new Vector3(
            body.velocity.x,
            0f,
            body.velocity.z);

        if (flatVelocity.sqrMagnitude >
            maximumSpeed * maximumSpeed)
        {
            flatVelocity =
                flatVelocity.normalized * maximumSpeed;

            body.velocity = new Vector3(
                flatVelocity.x,
                body.velocity.y,
                flatVelocity.z);
        }
    }

    private void UpdateDriverAnimation()
    {
        if (driverAnimator == null)
            return;

        float inputMagnitude =
            Mathf.Clamp01(cachedMoveInput.magnitude);

        bool isRowing = inputMagnitude > 0.05f;

        driverAnimator.SetBool(RowingHash, isRowing);

        float animationSpeed =
            Mathf.Lerp(0.8f, 1.25f, inputMagnitude);

        driverAnimator.SetFloat(
            RowSpeedHash,
            animationSpeed,
            0.1f,
            Time.deltaTime);

        driverAnimator.SetFloat(
            TurnAmountHash,
            cachedMoveInput.x,
            0.1f,
            Time.deltaTime);
    }

    public void SetDriver(
        PlayerInputReader inputReader,
        Animator animator)
    {
        driverInput = inputReader;
        driverAnimator = animator;
        cachedMoveInput = Vector2.zero;

        if (driverAnimator != null)
        {
            driverAnimator.SetBool(InBoatHash, true);
            driverAnimator.SetBool(RowingHash, false);
            driverAnimator.SetFloat(RowSpeedHash, 1f);
            driverAnimator.SetFloat(TurnAmountHash, 0f);
        }
    }

    public void RemoveDriver()
    {
        if (driverAnimator != null)
        {
            driverAnimator.SetBool(InBoatHash, false);
            driverAnimator.SetBool(RowingHash, false);
            driverAnimator.SetFloat(TurnAmountHash, 0f);
        }

        driverInput = null;
        driverAnimator = null;
        cachedMoveInput = Vector2.zero;
    }
}