using Unity.XR.CoreUtils;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class XRCharacterControllerSync : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Capsule")]
    [SerializeField] private float minimumHeight = 0.8f;
    [SerializeField] private float maximumHeight = 2.1f;
    [SerializeField] private float headClearance = 0.1f;
    [SerializeField] private float radius = 0.25f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (xrOrigin == null)
            xrOrigin = GetComponent<XROrigin>();
    }

    private void LateUpdate()
    {
        if (xrOrigin == null ||
            xrOrigin.Camera == null ||
            !characterController.enabled)
        {
            return;
        }

        float cameraHeight =
            xrOrigin.CameraInOriginSpaceHeight;

        float targetHeight = Mathf.Clamp(
            cameraHeight + headClearance,
            minimumHeight,
            maximumHeight);

        Vector3 cameraPosition =
            xrOrigin.CameraInOriginSpacePos;

        characterController.height = targetHeight;
        characterController.radius = Mathf.Min(
            radius,
            targetHeight * 0.5f);

        characterController.center = new Vector3(
            cameraPosition.x,
            targetHeight * 0.5f,
            cameraPosition.z);
    }
}