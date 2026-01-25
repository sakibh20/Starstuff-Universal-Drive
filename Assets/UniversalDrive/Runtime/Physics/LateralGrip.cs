using UnityEngine;

namespace UniversalDrive
{
    internal sealed class LateralGrip
    {
        private const float GripStrength = 8f;

        public void Apply(VehicleContext context, Transform transform)
        {
            if (!context.IsGrounded) return;

            Vector3 velocity = context.Rigidbody.linearVelocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            // Kill sideways motion aggressively
            float lateral = localVelocity.x;
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, GripStrength * Time.fixedDeltaTime);

            Vector3 correctedVelocity = transform.TransformDirection(localVelocity);
            
            // intentionally reshapes velocity for arcade control.
            // does NOT cancel gravity or collision impulses.
            context.Rigidbody.linearVelocity = correctedVelocity;
        }
    }
}