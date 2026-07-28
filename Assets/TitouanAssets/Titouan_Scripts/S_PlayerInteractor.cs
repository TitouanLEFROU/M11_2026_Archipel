using TMPro;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private FpsController fpsController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator avatarAnimator;
    [SerializeField] private TMP_Text promptText;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private string interactionKeyLabel = "[E] ";

    private CanoeSeat currentSeat;

    public PlayerInputReader InputReader => inputReader;
    public FpsController FpsController => fpsController;
    public Animator AvatarAnimator => avatarAnimator;

    private void Update()
    {
        if (inputReader == null)
            return;

        // Lorsque le joueur est dans la pirogue,
        // la touche d'interaction sert à descendre.
        if (currentSeat != null)
        {
            SetPrompt(interactionKeyLabel + "Descendre");

            if (inputReader.InteractPressed)
                currentSeat.TryExit(this);

            return;
        }

        IInteractable interactable = FindInteractable();

        if (interactable != null)
        {
            SetPrompt(
                interactionKeyLabel +
                interactable.InteractionPrompt);

            if (inputReader.InteractPressed)
                interactable.Interact(this);
        }
        else
        {
            SetPrompt(string.Empty);
        }
    }

    public void SetCurrentSeat(CanoeSeat seat)
    {
        currentSeat = seat;
    }

    private IInteractable FindInteractable()
    {
        if (playerCamera == null)
            return null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionMask,
                QueryTriggerInteraction.Collide))
        {
            return null;
        }

        MonoBehaviour[] behaviours =
            hit.collider.GetComponentsInParent<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
                return interactable;
        }

        return null;
    }

    private void SetPrompt(string message)
    {
        if (promptText != null)
            promptText.text = message;
    }
}