using UnityEngine;

namespace UniversalDrive
{
    internal sealed class Downforce
    {
        private const float BaseDownforce = 0f;
        private const float SpeedMultiplier = 0.5f;

        internal void Apply(VehicleContext context)
        {
            if (!context.IsGrounded) return;

            float speed = Mathf.Abs(context.ForwardSpeed);

            Vector3 force = -Vector3.up * (BaseDownforce + speed * SpeedMultiplier);

            context.Rigidbody.AddForce(force, ForceMode.Acceleration);
        }
    }
}