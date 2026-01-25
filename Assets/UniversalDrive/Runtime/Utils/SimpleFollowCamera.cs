using UnityEngine;

namespace UniversalDrive
{
    internal sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Position
            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );

            // Rotation (look at target)
            Vector3 lookDirection = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}