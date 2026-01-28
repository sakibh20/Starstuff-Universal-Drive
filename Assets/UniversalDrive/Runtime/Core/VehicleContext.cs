using UnityEngine;

namespace UniversalDrive
{
    internal sealed class VehicleContext
    {
        internal Rigidbody Rigidbody;

        // Geometry
        internal Bounds WorldBounds;
        internal Vector3 CenterOfMass;

        // Grounding
        internal bool IsGrounded;
        internal float GroundedRatio;
        internal Vector3 GroundNormal;

        // Motion (local space)
        internal float ForwardSpeed;
        internal float LateralSpeed;
        
        // Represents how much lateral authority the vehicle currently has
        // Derived from speed, grounding, and surface contact.
        public float GripFactor;
        
        internal float ControlAuthority
        {
            get
            {
                if (!IsGrounded) return 0.2f;
                return GripFactor;
            }
        }
    }
}
