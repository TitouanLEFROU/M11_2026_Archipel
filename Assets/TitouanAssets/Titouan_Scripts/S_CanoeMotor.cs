using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CanoeMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float forwardAcceleration = 4f;
    [SerializeField] private float reverseAcceleration = 2f;
    [SerializeField] private float maximumSpeed = 5f;

    [Header("Stable rotation")]
    [SerializeField] private float turnSpeed = 55f;

    [Header("Input smoothing")]
    [SerializeField] private float inputRiseSpeed = 5f;
    [SerializeField] private float inputFallSpeed = 8f;

    [Header("Water resistance")]
    [SerializeField] private float lateralDamping = 5f;

    private Rigidbody body;

    private Vector2 targetInput;
    private Vector2 currentInput;

    public Rigidbody Body => body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateSmoothedInput();
        ApplyPropulsion();
        ApplySteering();
        ApplyLateralResistance();
        LimitSpeed();
    }

    public void SetDriveInput(
        float forwardInput,
        float turnInput)
    {
        targetInput = new Vector2(
            Mathf.Clamp(turnInput, -1f, 1f),
            Mathf.Clamp(forwardInput, -1f, 1f));
    }

    public void StopInput()
    {
        targetInput = Vector2.zero;
    }

    public void StopImmediately()
    {
        targetInput = Vector2.zero;
        currentInput = Vector2.zero;

        if (body != null)
        {
            Vector3 angularVelocity =
                body.angularVelocity;

            angularVelocity.y = 0f;
            body.angularVelocity = angularVelocity;
        }
    }

    private void UpdateSmoothedInput()
    {
        currentInput.x = MoveInputAxis(
            currentInput.x,
            targetInput.x);

        currentInput.y = MoveInputAxis(
            currentInput.y,
            targetInput.y);
    }

    private float MoveInputAxis(
        float current,
        float target)
    {
        float speed =
            Mathf.Abs(target) > Mathf.Abs(current)
                ? inputRiseSpeed
                : inputFallSpeed;

        return Mathf.MoveTowards(
            current,
            target,
            speed * Time.fixedDeltaTime);
    }

    private void ApplyPropulsion()
    {
        float throttle = currentInput.y;

        if (Mathf.Abs(throttle) < 0.001f)
            return;

        float acceleration =
            throttle >= 0f
                ? forwardAcceleration
                : reverseAcceleration;

        float currentForwardSpeed =
            Vector3.Dot(
                body.linearVelocity,
                transform.forward);

        bool belowMaximumSpeed =
            Mathf.Abs(currentForwardSpeed) <
            maximumSpeed;

        bool changingDirection =
            Mathf.Sign(throttle) !=
            Mathf.Sign(currentForwardSpeed);

        if (belowMaximumSpeed ||
            changingDirection)
        {
            body.AddForce(
                transform.forward *
                throttle *
                acceleration,
                ForceMode.Acceleration);
        }
    }

    private void ApplySteering()
    {
        Vector3 angularVelocity =
            body.angularVelocity;

        angularVelocity.y = 0f;
        body.angularVelocity = angularVelocity;

        float turnInput = currentInput.x;

        if (Mathf.Abs(turnInput) < 0.001f)
            return;

        float yaw =
            turnInput *
            turnSpeed *
            Time.fixedDeltaTime;

        Quaternion targetRotation =
            body.rotation *
            Quaternion.Euler(0f, yaw, 0f);

        body.MoveRotation(targetRotation);
    }

    private void ApplyLateralResistance()
    {
        float lateralSpeed =
            Vector3.Dot(
                body.linearVelocity,
                transform.right);

        Vector3 lateralVelocity =
            transform.right * lateralSpeed;

        body.AddForce(
            -lateralVelocity * lateralDamping,
            ForceMode.Acceleration);
    }

    private void LimitSpeed()
    {
        Vector3 flatVelocity =
            new Vector3(
                body.linearVelocity.x,
                0f,
                body.linearVelocity.z);

        if (flatVelocity.sqrMagnitude <=
            maximumSpeed * maximumSpeed)
        {
            return;
        }

        flatVelocity =
            flatVelocity.normalized *
            maximumSpeed;

        body.linearVelocity =
            new Vector3(
                flatVelocity.x,
                body.linearVelocity.y,
                flatVelocity.z);
    }
}
