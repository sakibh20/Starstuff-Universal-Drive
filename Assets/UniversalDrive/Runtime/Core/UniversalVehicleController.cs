// DESIGN PHILOSOPHY
// -----------------
// This vehicle controller prioritizes player control, clarity, and extensibility
// over strict physical realism.
//
// PhysX remains responsible for gravity, collision resolution, and integration.
// This system selectively reshapes forces and velocities to achieve predictable,
// arcade-style handling across different vehicle sizes and platforms.
//
// All authority (drive, steering, stabilization) is intentionally scaled based
// on grounded state and grip to prevent unrealistic behavior while airborne.

using UnityEngine;

namespace UniversalDrive
{
    [RequireComponent(typeof(Rigidbody))]
    internal sealed class UniversalVehicleController : MonoBehaviour
    {
        [SerializeField] private float forwardSpeedFactor = 20f;
        [SerializeField] private float turnSpeedFactor = 40f;
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

            if (_input is MobileVehicleInput mobile)
            {
                ApplyMobileInput(mobile);
            }
            else
            {
                ApplyKeyboardInput();
            }

            ApplyGroundStabilization();
            ApplyUpsideDownRecovery();
            ClampHorizontalSpeed();
        }

        // MOBILE JOYSTICK CONTROL — DESIGN NOTES
        // -------------------------------------
        // Joystick input is handled separately from keyboard input because touch controls
        // represent directional intent rather than discrete throttle + steering commands.
        // Known limitations / future improvement areas:
        // - Backward drag currently behaves as forward drive with steering bias
        //   rather than a true reverse or braking state.
        // - Steering response could be improved by introducing turn-rate curves
        //   or angle-based steering instead of linear X input.
        // - A dedicated brake / reverse threshold could improve precision at low speeds.
        // - Input smoothing and dead zones may be tuned per device for better feel.
        //
        // These tradeoffs were chosen to keep the control model simple, predictable,
        // and extensible while establishing a solid baseline for mobile driving behavior.

        /// <summary>
        /// Handles joystick-based vehicle control.
        /// Any joystick drag implies forward intent.
        /// Steering is derived from horizontal drag and scaled by movement intensity.
        /// </summary>
        private void ApplyMobileInput(MobileVehicleInput mobile)
        {
            Vector3 forward = transform.forward;
            Vector2 inputVector = mobile.InputVector;

            // Full authority when grounded, heavily reduced while airborne
            float driveAuthority = _context.IsGrounded ? 1f : 0.2f;

            // Any joystick drag = forward intent
            // Magnitude-based so diagonal drag is not stronger than straight drag
            float throttle = Mathf.Clamp01(inputVector.magnitude);

            // Apply forward propulsion along vehicle forward
            _context.Rigidbody.AddForce(forward * throttle * forwardSpeedFactor * driveAuthority, ForceMode.Acceleration);

            // Steering comes purely from horizontal drag
            // Scaled by throttle so steering only happens when player intends to move
            float steerTorque = inputVector.x * turnSpeedFactor * _context.ControlAuthority * throttle;
            _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);
        }
        
        /// <summary>
        /// Handles keyboard-based vehicle control.
        /// providing arcade-style car controls.
        /// </summary>
        private void ApplyKeyboardInput()
        {
            Vector3 forward = transform.forward;

            float throttle = _input.Throttle;   // W/S or Up/Down
            float steering = _input.Steering;   // A/D or Left/Right

            // Apply steering torque regardless of throttle
            _context.Rigidbody.AddTorque(Vector3.up * steering * turnSpeedFactor * _context.ControlAuthority, ForceMode.Acceleration);

            // Full authority when grounded, heavily reduced while airborne
            float driveAuthority = _context.IsGrounded ? 1f : 0.2f;

            // Apply forward propulsion along vehicle forward
            _context.Rigidbody.AddForce(forward * throttle * forwardSpeedFactor * driveAuthority, ForceMode.Acceleration);
        }
        
        /// <summary>
        /// Reshapes velocity toward the vehicle's forward direction while grounded.
        /// This improves responsiveness and prevents excessive sideways sliding.
        /// </summary>
        private void ApplyGroundStabilization()
        {
            if (_context.IsGrounded)
            {
                Vector3 velocity = _context.Rigidbody.linearVelocity;
                Vector3 forward = transform.forward;

                // Project velocity onto forward axis
                Vector3 projected = Vector3.Project(velocity, forward);

                // Correction force nudges velocity toward forward direction
                Vector3 correction = projected - velocity;

                _context.Rigidbody.AddForce(correction * 2.5f * _context.GripFactor, ForceMode.Acceleration);
            }
            else
            {
                // Prevent excessive spinning while airborne
                _context.Rigidbody.angularVelocity = Vector3.ClampMagnitude(_context.Rigidbody.angularVelocity, 2.5f);
            }
        }
        
        private void ApplyUpsideDownRecovery()
        {
            if (!_context.IsGrounded || !IsUpsideDown()) return;

            // Bias torque toward upright orientation
            // Does not auto-flip — only assists recovery
            Vector3 recoveryAxis = Vector3.Cross(transform.up, Vector3.up);
            _context.Rigidbody.AddTorque(recoveryAxis * 20f * _context.GripFactor, ForceMode.Acceleration);
        }

        private void ClampHorizontalSpeed()
        {
            Vector3 vel = _context.Rigidbody.linearVelocity;
            Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
            if (horiz.magnitude > maxSpeed)
            {
                horiz = horiz.normalized * maxSpeed;
                _context.Rigidbody.linearVelocity = new Vector3(horiz.x, vel.y, horiz.z);
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
            
            // Ground detection ray
            Gizmos.color = _context.IsGrounded ? Color.green : Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * _groundDetector.DebugRayLength);

            // Center of mass visualization
            Gizmos.color = Color.yellow;
            Vector3 com = _context.Rigidbody.worldCenterOfMass;
            Gizmos.DrawSphere(com, 0.15f);

            // // Line from object origin to center of mass
            // Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            // Gizmos.DrawLine(pos, com);
        }
    }
}