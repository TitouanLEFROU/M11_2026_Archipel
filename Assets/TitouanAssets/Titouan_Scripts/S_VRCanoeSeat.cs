using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRCanoeSeat : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private VRAvatarController avatarController;

    [Header("Boat")]
    [SerializeField] private CanoeMotor canoeMotor;
    [SerializeField] private VRPaddleController paddleController;

    [Header("Seat anchors")]
    [SerializeField] private Transform seatRoot;
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform avatarRootAnchor;

    [Header("Exit")]
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform waterSurface;

    [SerializeField] private float exitRayHeight = 1.5f;
    [SerializeField] private float exitRayDistance = 3f;
    [SerializeField] private float maximumGroundSlope = 50f;
    [SerializeField] private float minimumGroundAboveWater = 0.05f;
    [SerializeField] private float footOffset = 0.03f;

    [Header("Systems disabled while seated")]
    [SerializeField] private Behaviour[] onFootBehaviours;
    [SerializeField] private GameObject[] onFootObjects;

    [Header("Input")]
    [SerializeField]
    private InputActionReference exitBoatAction;

    [SerializeField] private float exitInputCooldown = 0.5f;

    private Transform originalParent;
    private bool occupied;
    private bool exitActionWasEnabled;
    private float mountTime;

    private void OnEnable()
    {
        if (exitBoatAction == null ||
            exitBoatAction.action == null)
        {
            return;
        }

        exitActionWasEnabled =
            exitBoatAction.action.enabled;

        if (!exitActionWasEnabled)
            exitBoatAction.action.Enable();
    }

    private void OnDisable()
    {
        if (exitBoatAction == null ||
            exitBoatAction.action == null)
        {
            return;
        }

        if (!exitActionWasEnabled)
            exitBoatAction.action.Disable();
    }

    private void Update()
    {
        if (!occupied ||
            Time.time <
            mountTime + exitInputCooldown)
        {
            return;
        }

        if (exitBoatAction != null &&
            exitBoatAction.action != null &&
            exitBoatAction.action
                .WasPressedThisFrame())
        {
            TryDismount();
        }
    }

    public void Mount()
    {
        if (occupied ||
            xrOrigin == null ||
            seatRoot == null ||
            headAnchor == null)
        {
            return;
        }

        occupied = true;
        mountTime = Time.time;

        originalParent =
            xrOrigin.transform.parent;

        SetOnFootSystemsEnabled(false);

        if (characterController != null)
            characterController.enabled = false;

        xrOrigin.transform.SetParent(
            seatRoot,
            true);

        // Aligne le repère du joueur avec
        // l'avant de la pirogue.
        xrOrigin.MatchOriginUpOriginForward(
            seatRoot.up,
            seatRoot.forward);

        // Positionne le casque au point d'assise
        // tout en conservant le tracking local.
        xrOrigin.MoveCameraToWorldLocation(
            headAnchor.position);

        if (avatarController != null)
        {
            avatarController.SetSeated(
                true,
                avatarRootAnchor);
        }

        if (paddleController != null)
            paddleController.enabled = true;
    }

    public void TryDismount()
    {
        if (!occupied)
            return;

        if (!TryFindSafeExit(
                out Vector3 exitPosition,
                out Vector3 exitForward))
        {
            Debug.LogWarning(
                "Aucune sortie valide proche " +
                "de la pirogue.",
                this);

            return;
        }

        StartCoroutine(
            DismountRoutine(
                exitPosition,
                exitForward));
    }

    private IEnumerator DismountRoutine(
        Vector3 exitPosition,
        Vector3 exitForward)
    {
        if (paddleController != null)
            paddleController.enabled = false;

        if (canoeMotor != null)
            canoeMotor.StopInput();

        xrOrigin.transform.SetParent(
            originalParent,
            true);

        xrOrigin.MatchOriginUpOriginForward(
            Vector3.up,
            exitForward);

        float trackedHeight = Mathf.Clamp(
            xrOrigin.CameraInOriginSpaceHeight,
            0.8f,
            2.2f);

        Vector3 targetHeadPosition =
            exitPosition +
            Vector3.up * trackedHeight;

        xrOrigin.MoveCameraToWorldLocation(
            targetHeadPosition);

        if (avatarController != null)
        {
            avatarController.SetSeated(
                false,
                null);
        }

        // Attend une frame avant de réactiver
        // les collisions et la locomotion.
        yield return null;

        if (characterController != null)
            characterController.enabled = true;

        SetOnFootSystemsEnabled(true);

        occupied = false;
    }

    private bool TryFindSafeExit(
        out Vector3 exitPosition,
        out Vector3 exitForward)
    {
        exitPosition = Vector3.zero;
        exitForward = Vector3.forward;

        if (exitPoints == null)
            return false;

        foreach (Transform exitPoint in exitPoints)
        {
            if (exitPoint == null)
                continue;

            Vector3 rayOrigin =
                exitPoint.position +
                Vector3.up * exitRayHeight;

            float totalDistance =
                exitRayHeight +
                exitRayDistance;

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    totalDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float slope =
                Vector3.Angle(
                    hit.normal,
                    Vector3.up);

            if (slope > maximumGroundSlope)
                continue;

            if (waterSurface != null &&
                hit.point.y <
                waterSurface.position.y +
                minimumGroundAboveWater)
            {
                continue;
            }

            exitPosition =
                hit.point +
                Vector3.up * footOffset;

            exitForward =
                Vector3.ProjectOnPlane(
                    exitPoint.forward,
                    Vector3.up);

            if (exitForward.sqrMagnitude < 0.001f)
                exitForward = Vector3.forward;

            exitForward.Normalize();

            return true;
        }

        return false;
    }

    private void SetOnFootSystemsEnabled(
        bool enabled)
    {
        if (onFootBehaviours != null)
        {
            foreach (
                Behaviour behaviour
                in onFootBehaviours)
            {
                if (behaviour != null)
                    behaviour.enabled = enabled;
            }
        }

        if (onFootObjects != null)
        {
            foreach (
                GameObject target
                in onFootObjects)
            {
                if (target != null)
                    target.SetActive(enabled);
            }
        }
    }
}