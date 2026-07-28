using UnityEngine;

public class CanoeSeat : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private CanoeController canoeController;
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform[] exitPoints;

    [Header("Safe exit")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float raycastHeight = 1.5f;
    [SerializeField] private float raycastDistance = 3f;
    [SerializeField] private float maximumGroundSlope = 50f;
    [SerializeField] private float footOffset = 0.08f;

    private PlayerInteractor occupant;

    public string InteractionPrompt =>
        occupant == null
            ? "Monter dans la pirogue"
            : string.Empty;

    public void Interact(PlayerInteractor interactor)
    {
        if (interactor == null || occupant != null)
            return;

        Mount(interactor);
    }

    private void Mount(PlayerInteractor interactor)
    {
        if (canoeController == null || seatPoint == null)
        {
            Debug.LogError(
                "La pirogue n'est pas correctement configurée.",
                this);

            return;
        }

        occupant = interactor;

        interactor.SetCurrentSeat(this);
        interactor.FpsController.MountAt(seatPoint);

        canoeController.SetDriver(
            interactor.InputReader,
            interactor.AvatarAnimator);
    }

    public bool TryExit(PlayerInteractor interactor)
    {
        if (interactor == null || occupant != interactor)
            return false;

        if (!TryFindSafeExit(
                out Vector3 exitPosition,
                out Quaternion exitRotation))
        {
            Debug.LogWarning(
                "Impossible de descendre : " +
                "aucun sol valide à proximité de la pirogue.",
                this);

            return false;
        }

        canoeController.RemoveDriver();

        interactor.SetCurrentSeat(null);

        interactor.FpsController.DismountAt(
            exitPosition,
            exitRotation);

        occupant = null;

        return true;
    }

    private bool TryFindSafeExit(
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (exitPoints == null || exitPoints.Length == 0)
            return false;

        foreach (Transform point in exitPoints)
        {
            if (point == null)
                continue;

            Vector3 rayOrigin =
                point.position +
                Vector3.up * raycastHeight;

            float totalDistance =
                raycastHeight + raycastDistance;

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

            float groundSlope =
                Vector3.Angle(hit.normal, Vector3.up);

            if (groundSlope > maximumGroundSlope)
                continue;

            position =
                hit.point +
                Vector3.up * footOffset;

            rotation = Quaternion.Euler(
                0f,
                point.eulerAngles.y,
                0f);

            return true;
        }

        return false;
    }
}