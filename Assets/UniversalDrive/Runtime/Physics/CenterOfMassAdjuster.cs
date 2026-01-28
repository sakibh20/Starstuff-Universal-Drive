using UnityEngine;

namespace UniversalDrive
{
    // Dynamically adjusts the rigidbody center of mass
    // based on the object's visual bounds to improve stability
    // across arbitrary shapes and sizes.
    internal sealed class CenterOfMassAdjuster
    {
        // Percentage of object height used to lower the center of mass.
        // Higher values increase stability but reduce flip potential.
        private const float HeightBias = 0.55f;

        internal void Apply(VehicleContext context, Bounds bounds)
        {
            // Lower the center of mass relative to the object's height
            // to counteract tall or top-heavy shapes.
            Vector3 com = context.Rigidbody.centerOfMass;
            com.y = -bounds.extents.y * HeightBias;
            context.Rigidbody.centerOfMass = com;
        }
    }
}