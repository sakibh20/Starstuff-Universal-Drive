// NOTE:
// This controller intentionally reshapes velocity and applies stabilizing forces
// to prioritize player control over physical realism.
// Gravity and collision resolution remain fully handled by PhysX.

using UnityEngine;

namespace UniversalDrive
{
    [RequireComponent(typeof(Rigidbody))]
    internal sealed class UniversalVehicleController : MonoBehaviour
    {
        [SerializeField] private float forwardSpeedFactor = 1f;
        [SerializeField] private float turnSpeedFactor = 1f;
        [SerializeField] float maxSpeed = 15f;

        private VehicleContext _context;
        private IVehicleInput _input;

        private GroundDetector _groundDetector;

        private LateralGrip _lateralGrip;
        private UprightStabilization _uprightStabilization;
        private Downforce _downforce;
        private CenterOfMassAdjuster _centerOfMassAdjuster;
        
        private Bounds _bounds;

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

            _groundDetector = new GroundDetector(transform, _context.Rigidbody);
            _uprightStabilization = new UprightStabilization();
            _lateralGrip = new LateralGrip();
            _downforce = new Downforce();
            _centerOfMassAdjuster = new CenterOfMassAdjuster();

            InitializeRigidbody();
        }

        private void FixedUpdate()
        {
            UpdateContext();
            _groundDetector.Update();

            ApplyForces();
            _lateralGrip.Apply(_context, transform);
            _uprightStabilization.Apply(_context, transform);
            _downforce.Apply(_context);
        }

        private void InitializeRigidbody()
        {
            _bounds = ComputeBounds();
            _centerOfMassAdjuster.Apply(_context, _bounds);
            _groundDetector.UpdateRayLength(_bounds);
            
            _context.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _context.Rigidbody.angularDamping = 1f;
        }

        private void UpdateContext()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_context.Rigidbody.linearVelocity);

            _context.ForwardSpeed = localVelocity.z;
            _context.LateralSpeed = localVelocity.x;

            _context.IsGrounded = _groundDetector.IsGrounded;
            _context.GroundNormal = _groundDetector.GroundNormal;

            float speed01 = Mathf.Clamp01(Mathf.Abs(_context.ForwardSpeed) / maxSpeed);

            // Grip increases with speed but only when grounded
            _context.GripFactor = _context.IsGrounded ? Mathf.Lerp(0.6f, 1.2f, speed01) : 0.2f;
        }

        private void ApplyForces()
        {
            if (_input == null) return;

            // Forward propulsion force
            // Full authority when grounded, heavily reduced while airborne
            float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
            Vector3 force = transform.forward * (_input.Throttle * forwardSpeedFactor * driveAuthority);
            _context.Rigidbody.AddForce(force, ForceMode.Acceleration);
            
            // Clamp horizontal velocity to enforce top speed
            // Vertical velocity (gravity, jumps) is intentionally preserved
            Vector3 velocity = _context.Rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontalVelocity.magnitude > maxSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
                _context.Rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
            }

            // Steering/Yaw torque
            float steerTorque = _input.Steering * turnSpeedFactor * _context.GripFactor;
            // Reduces steering authority by 80% while airborne to prevent unrealistic mid-air yaw control
            steerTorque *= _context.IsGrounded ? 1f : 0.2f;
            _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);
            
            if (!_context.IsGrounded)
            {
                // Airborne angular cap
                _context.Rigidbody.angularVelocity = Vector3.ClampMagnitude(_context.Rigidbody.angularVelocity, 2.5f);
            }
            else
            {
                //Steering torque alone does not reliably redirect linear momentum at higher speeds.
                //To achieve arcade-style responsiveness, I introduced a controlled velocity-alignment
                //force that gently reshapes the velocity vector toward the vehicle’s facing direction while grounded.
                velocity = _context.Rigidbody.linearVelocity;
                Vector3 forward = transform.forward;
                Vector3 projected = Vector3.Project(velocity, forward);
                Vector3 correction = projected - velocity;
                // Uses grip factor to determine how aggressively velocity is reshaped
                _context.Rigidbody.AddForce(correction * 2.5f * _context.GripFactor, ForceMode.Acceleration);
            }
            
            if (_context.IsGrounded && IsUpsideDown())
            {
                // Applies corrective torque to assist flip recovery.
                // This does not auto-flip the vehicle; it biases recovery
                // while still requiring player input or momentum.
                Vector3 recoveryAxis = Vector3.Cross(transform.up, Vector3.up);
                _context.Rigidbody.AddTorque(recoveryAxis * 20f * _context.GripFactor, ForceMode.Acceleration);
            }
        }
        
        private Bounds ComputeBounds()
        {
            // Computes combined renderer bounds to represent the visual footprint
            // of the object regardless of collider configuration.
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }
        
        bool IsUpsideDown()
        {
            // Dot product below threshold indicates the object is significantly inverted
            return Vector3.Dot(transform.up, Vector3.up) < 0.2f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_context == null || _context.Rigidbody == null) return;

            Vector3 pos = transform.position;

            // Forward direction (vehicle facing)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos + transform.forward * 2f);

            // Lateral velocity visualization (sideways motion)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + transform.right * _context.LateralSpeed);

            // Grip / ground authority indicator
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos + Vector3.down * _context.GripFactor);

            // Center of mass visualization
            Gizmos.color = Color.yellow;
            Vector3 com = _context.Rigidbody.worldCenterOfMass;
            Gizmos.DrawSphere(com, 0.15f);

            // Line from object origin to center of mass
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawLine(pos, com);
        }
    }
}