using Unity.XR.CoreUtils;
using UnityEngine;

public class XRWaterSafety : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform waterSurface;

    [Header("Safety")]
    [SerializeField] private float safeHeightAboveWater = 0.1f;
    [SerializeField] private float respawnDepth = 0.8f;
    [SerializeField] private float saveInterval = 0.2f;

    private Vector3 lastSafeOriginPosition;
    private Quaternion lastSafeOriginRotation;
    private float nextSaveTime;

    private void Start()
    {
        SaveCurrentPosition();
    }

    private void Update()
    {
        if (xrOrigin == null ||
            xrOrigin.Camera == null ||
            waterSurface == null ||
            !enabled)
        {
            return;
        }

        float feetHeight = transform.position.y;
        float waterHeight = waterSurface.position.y;

        if (feetHeight >=
            waterHeight + safeHeightAboveWater)
        {
            if (Time.time >= nextSaveTime)
            {
                SaveCurrentPosition();
                nextSaveTime =
                    Time.time + saveInterval;
            }

            return;
        }

        if (xrOrigin.Camera.transform.position.y <
            waterHeight - respawnDepth)
        {
            Respawn();
        }
    }

    private void SaveCurrentPosition()
    {
        lastSafeOriginPosition = transform.position;
        lastSafeOriginRotation = transform.rotation;
    }

    private void Respawn()
    {
        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(
            lastSafeOriginPosition,
            lastSafeOriginRotation);

        if (controllerWasEnabled)
            characterController.enabled = true;
    }
}