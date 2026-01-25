// Upright stabilization intentionally applies corrective torque
// to bias the vehicle toward a stable orientation.
// This does not override collisions or gravity — it only influences rotation.

using UnityEngine;

namespace UniversalDrive
{
    internal sealed class UprightStabilization
    {
        private const float UprightTorque = 40f;
        private const float AngularDamping = 4f;

        internal void Apply(VehicleContext context, Transform transform)
        {
            Rigidbody rb = context.Rigidbody;

            // Decide which "up" we want
            Vector3 targetUp = context.IsGrounded ? context.GroundNormal : Vector3.up;

            Vector3 currentUp = transform.up;

            // Torque needed to rotate current up -> target up
            Vector3 correctionAxis = Vector3.Cross(currentUp, targetUp);

            rb.AddTorque(correctionAxis * UprightTorque, ForceMode.Acceleration);

            // Extra angular damping to prevent oscillation
            rb.angularVelocity *= Mathf.Clamp01(1f - AngularDamping * Time.fixedDeltaTime);
        }
    }
}