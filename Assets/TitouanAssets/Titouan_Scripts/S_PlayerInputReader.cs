using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference interactAction;

    public Vector2 Move
    {
        get
        {
            return moveAction != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;
        }
    }

    public Vector2 Look
    {
        get
        {
            return lookAction != null
                ? lookAction.action.ReadValue<Vector2>()
                : Vector2.zero;
        }
    }

    public bool JumpPressed
    {
        get
        {
            return jumpAction != null
                && jumpAction.action.WasPressedThisFrame();
        }
    }

    public bool SprintHeld
    {
        get
        {
            return sprintAction != null
                && sprintAction.action.IsPressed();
        }
    }

    public bool InteractPressed
    {
        get
        {
            return interactAction != null
                && interactAction.action.WasPressedThisFrame();
        }
    }

    private void OnEnable()
    {
        SetActionEnabled(moveAction, true);
        SetActionEnabled(lookAction, true);
        SetActionEnabled(jumpAction, true);
        SetActionEnabled(sprintAction, true);
        SetActionEnabled(interactAction, true);
    }

    private void OnDisable()
    {
        SetActionEnabled(moveAction, false);
        SetActionEnabled(lookAction, false);
        SetActionEnabled(jumpAction, false);
        SetActionEnabled(sprintAction, false);
        SetActionEnabled(interactAction, false);
    }

    private static void SetActionEnabled(
        InputActionReference actionReference,
        bool enabled)
    {
        if (actionReference == null || actionReference.action == null)
            return;

        if (enabled)
            actionReference.action.Enable();
        else
            actionReference.action.Disable();
    }
}
