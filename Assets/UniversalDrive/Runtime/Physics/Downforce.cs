using UnityEngine;

namespace UniversalDrive
{
    internal sealed class Downforce
    {
        // Downforce should grow with speed, and be zero when stationary.
        private const float BaseDownforce = 0f;
        private const float SpeedMultiplier = 0.05f;

        internal void Apply(VehicleContext context)
        {
            if (!context.IsGrounded) return;
            float speed = Mathf.Abs(context.ForwardSpeed);
            Vector3 force = -Vector3.up * (BaseDownforce + speed * SpeedMultiplier);
            // Downforce scales with grip instead of overpowering it
            context.Rigidbody.AddForce(force * context.GripFactor, ForceMode.Acceleration);
        }
    }
}