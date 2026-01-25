using UnityEngine;

namespace UniversalDrive
{
    [RequireComponent(typeof(Rigidbody))]
    internal sealed class UniversalVehicleController : MonoBehaviour
    {
        private VehicleContext _context;
        private IVehicleInput _input;

        private void Awake()
        {
            _context = new VehicleContext
            {
                Rigidbody = GetComponent<Rigidbody>()
            };

            _input = GetComponent<IVehicleInput>();

            if (_input == null)
            {
                Debug.LogError("No IVehicleInput found on vehicle.");
            }

            InitializeRigidbody();
        }

        private void FixedUpdate()
        {
            UpdateContext();
            ApplyForces();
        }

        private void InitializeRigidbody()
        {
            // TODO: Mass scaling based on bounds
            
            _context.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _context.Rigidbody.angularDamping = 1f;
        }

        private void UpdateContext()
        {
            // TODO: Populate bounds from renderers/colliders
            // TODO: Ground detection system integration

            Vector3 localVelocity = transform.InverseTransformDirection(_context.Rigidbody.linearVelocity);

            _context.ForwardSpeed = localVelocity.z;
            _context.LateralSpeed = localVelocity.x;
        }

        private void ApplyForces()
        {
            if (_input == null) return;

            // TEMP: very crude forward force to prove loop works
            Vector3 force = transform.forward * (_input.Throttle * 20f);
            _context.Rigidbody.AddForce(force, ForceMode.Acceleration);

            // TEMP: very crude steering torque
            float steerTorque = _input.Steering * 30f;
            _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);

            // TODO: Replace with DriveForces, SteeringForces modules
        }
    }
}