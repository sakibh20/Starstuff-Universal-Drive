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
            _rayLength = bounds.extents.y + 0.05f;

            // Safety clamp in case bounds are weird during initialization
            _rayLength = Mathf.Max(_rayLength, 0.5f);
        }

        internal void Update()
        {
            Ray ray = new Ray(_transform.position, Vector3.down);

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