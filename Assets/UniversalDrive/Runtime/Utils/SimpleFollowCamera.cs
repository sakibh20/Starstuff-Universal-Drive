using UnityEngine;

/// <summary>
/// Simple arcade chase camera for UniversalDrive vehicles.
/// Adjusts position and rotation based on vehicle bounds and velocity.
/// </summary>
[RequireComponent(typeof(Camera))]
public class VehicleCameraFollow : MonoBehaviour
{
    [Header("Target Vehicle")]
    public Transform Target { get; set; }

    [Header("Follow Settings")]
    [SerializeField, Tooltip("How fast the camera follows position")] private float positionSmooth = 5f;
    [SerializeField, Tooltip("How fast the camera rotates to look at target")] private float rotationSmooth = 5f;

    [SerializeField, Tooltip("Base distance behind the vehicle")] private float baseDistance = 5f;
    [SerializeField, Tooltip("Base height above the vehicle")] private float baseHeight = 3f;
    [SerializeField, Tooltip("Extra distance multiplier based on vehicle bounds extents")] private float boundsDistanceFactor = 1.5f;
    [SerializeField, Tooltip("Extra height multiplier based on vehicle bounds extents")] private float boundsHeightFactor = 1f;

    private Bounds targetBounds;

    private void LateUpdate()
    {
        if (Target == null) return;

        // Compute bounds to adapt camera distance/height
        ComputeTargetBounds();

        Vector3 vehicleCenter = targetBounds.center;

        float distance = baseDistance + targetBounds.extents.magnitude * boundsDistanceFactor;
        float height = baseHeight + targetBounds.extents.y * boundsHeightFactor;

        // Desired position: behind vehicle along its forward axis
        Vector3 desiredPosition = vehicleCenter - Target.forward * distance + Vector3.up * height;

        // Smooth position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmooth * Time.deltaTime);

        // Look at vehicle center
        Quaternion desiredRotation = Quaternion.LookRotation(vehicleCenter - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmooth * Time.deltaTime);
    }

    private void ComputeTargetBounds()
    {
        Renderer[] renderers = Target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            targetBounds = new Bounds(Target.position, Vector3.one);
            return;
        }

        targetBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            targetBounds.Encapsulate(renderers[i].bounds);
        }
    }
}