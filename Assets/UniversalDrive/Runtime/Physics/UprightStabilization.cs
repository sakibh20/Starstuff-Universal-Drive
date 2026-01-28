// Upright stabilization intentionally applies corrective torque
// to bias the vehicle toward a stable orientation.
// This does not override collisions or gravity — it only influences rotation.

using UnityEngine;

namespace UniversalDrive
{
    internal sealed class UprightStabilization
    {
        // Controls how aggressively the vehicle is biased upright.
        // This value must overcome collision-induced angular momentum
        // without preventing flips or recovery.
        private const float UprightTorque = 40f;
        private const float AngularDamping = 4f;

        /// <summary>
        /// Applies corrective torque to bias the vehicle upright.
        /// This does not override gravity or collision impulses;
        /// it only influences rotational stability.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="transform"></param>
        internal void Apply(VehicleContext context, Transform transform)
        {
            // Upright stabilization is only applied while grounded
            // to avoid unrealistic mid-air corrections.
            if (!context.IsGrounded) return;
            
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