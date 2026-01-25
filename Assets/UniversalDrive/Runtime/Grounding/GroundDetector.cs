using UnityEngine;

namespace UniversalDrive
{
    internal sealed class GroundDetector
    {
        private readonly Transform _transform;
        private readonly Rigidbody _rigidbody;

        private const float RayLength = 1.2f;

        internal bool IsGrounded { get; private set; }
        internal Vector3 GroundNormal { get; private set; }

        internal GroundDetector(Transform transform, Rigidbody rigidbody)
        {
            _transform = transform;
            _rigidbody = rigidbody;
        }

        internal void Update()
        {
            // ray length might have to be based on bounds extents + some margin
            Ray ray = new Ray(_transform.position, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, RayLength))
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