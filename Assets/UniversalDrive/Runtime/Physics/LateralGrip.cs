using UnityEngine;

namespace UniversalDrive
{
    internal sealed class LateralGrip
    {
        // How fast should sideways velocity decay
        // time-based, not speed-based
        // 8 → lateral velocity dies in ~0.2–0.3s
        // 3 → floaty
        private const float GripStrength = 8f;

        /// <summary>
        /// Aggressively damps sideways velocity to simulate tire grip.
        /// This directly reshapes velocity for arcade control and intentionally
        /// prioritizes player input over physical realism.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="transform"></param>
        internal void Apply(VehicleContext context, Transform transform)
        {
            if (!context.IsGrounded) return;
            Vector3 velocity = context.Rigidbody.linearVelocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, GripStrength * Time.fixedDeltaTime);
            Vector3 correctedVelocity = transform.TransformDirection(localVelocity);
            context.Rigidbody.linearVelocity = correctedVelocity;
        }
    }
}