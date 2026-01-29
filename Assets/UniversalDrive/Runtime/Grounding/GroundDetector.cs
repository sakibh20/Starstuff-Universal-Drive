using UnityEngine;

namespace UniversalDrive
{
    internal sealed class GroundDetector
    {
        private readonly Transform _transform;
        private readonly Rigidbody _rigidbody;

        // Dynamically updated
        private float _rayLength;
        internal float DebugRayLength => _rayLength;

        internal bool IsGrounded { get; private set; }
        internal Vector3 GroundNormal { get; private set; }

        internal GroundDetector(Transform transform, Rigidbody rigidbody)
        {
            _transform = transform;
            _rigidbody = rigidbody;
        }

        internal void UpdateRayLength(Bounds bounds)
        {
            // Half height of the vehicle + small margin
            // Bounds are world-space, so this works regardless of scale
            _rayLength = bounds.extents.y + 0.1f;

            // Safety clamp in case bounds are weird during initialization
            _rayLength = Mathf.Max(_rayLength, 0.5f);
        }

        internal void Update()
        {
            Vector3 origin = _rigidbody.worldCenterOfMass + _transform.up * 0.1f; // small lift to avoid starting inside ground
            Ray ray = new Ray(origin, -_transform.up);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayLength))
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector3.up;
            }
        }
    }
}