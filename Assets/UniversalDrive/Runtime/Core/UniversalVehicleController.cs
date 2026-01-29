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
    public sealed class UniversalVehicleController : MonoBehaviour
    {
        [SerializeField] private float forwardSpeedFactor = 30f;
        [SerializeField] private float turnSpeedFactor = 40f;
        [SerializeField] private float steeringResponse = 10f;   // How fast yaw reaches target
        [SerializeField] private float maxYawSpeed = 4f;       // Absolute yaw cap (rad/sec)
        [SerializeField] float maxSpeed = 30f;

        [SerializeField] private InputManager inputManager;

        private VehicleContext _context;
        private GroundDetector _groundDetector;
        private LateralGrip _lateralGrip;
        private UprightStabilization _uprightStabilization;
        private Downforce _downforce;
        private CenterOfMassAdjuster _centerOfMassAdjuster;
        private Bounds _bounds;
        private Transform _vehicleTransform;

        private void Awake()
        {
            // Create empty context (no vehicle yet)
            _context = new VehicleContext();

            if (inputManager == null)
            {
                Debug.LogError("No InputManager found.");
            }

            // Create systems ONCE
            _uprightStabilization = new UprightStabilization();
            _lateralGrip = new LateralGrip();
            _downforce = new Downforce();
            _centerOfMassAdjuster = new CenterOfMassAdjuster();
        }
        
        public void SetVehicle(Transform newVehicle)
        {
            if (newVehicle == null)
            {
                Debug.LogError("SetVehicle called with null transform.");
                return;
            }

            Rigidbody rb = newVehicle.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Vehicle must have a Rigidbody.");
                return;
            }

            _vehicleTransform = newVehicle;

            _context.Rigidbody = rb;

            // Create / rebind vehicle-dependent systems
            _groundDetector = new GroundDetector(_vehicleTransform, rb);

            Bounds bounds = ComputeBounds();
            _groundDetector.UpdateRayLength(bounds);
            _centerOfMassAdjuster.Apply(_context, bounds);

            InitializeRigidbodyRuntime();
        }

        private void FixedUpdate()
        {
            if (_vehicleTransform == null) return;
            if (inputManager == null || inputManager.VehicleInput == null) return;
            
            UpdateContext();
            _groundDetector.Update();

            ApplyForces();
            //ApplyAirGravity();
            _lateralGrip.Apply(_context, _vehicleTransform);
            _uprightStabilization.Apply(_context, _vehicleTransform);
            _downforce.Apply(_context);
        }

        private void InitializeRigidbodyRuntime()
        {
            _context.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _context.Rigidbody.mass = 1200f;
            _context.Rigidbody.linearDamping = 0.03f;
            _context.Rigidbody.angularDamping = 0.2f;
        }

        private void UpdateContext()
        {
            Vector3 localVelocity = _vehicleTransform.InverseTransformDirection(_context.Rigidbody.linearVelocity);

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
            if (inputManager.VehicleInput is MobileVehicleInput mobile)
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
            Vector3 forward = _vehicleTransform.forward;
            Vector2 inputVector = mobile.InputVector;

            // Full authority when grounded, heavily reduced while airborne
            float driveAuthority = _context.IsGrounded ? 1f : 0.1f;

            // Any joystick drag = forward intent
            // Magnitude-based so diagonal drag is not stronger than straight drag
            float throttle = Mathf.Clamp01(inputVector.magnitude);

            // Apply forward propulsion along vehicle forward
            _context.Rigidbody.AddForce(forward * throttle * forwardSpeedFactor * driveAuthority, ForceMode.Acceleration);

            // Steering comes purely from horizontal drag
            // Scaled by throttle so steering only happens when player intends to move
            float targetYaw = inputVector.x * turnSpeedFactor * _context.ControlAuthority * throttle;

            targetYaw = Mathf.Clamp(targetYaw, -maxYawSpeed, maxYawSpeed);

            float currentYaw = _context.Rigidbody.angularVelocity.y;
            float yawDelta = targetYaw - currentYaw;

            float response = _context.IsGrounded ? steeringResponse : steeringResponse * 0.3f;

            _context.Rigidbody.AddTorque(Vector3.up * yawDelta * response, ForceMode.Acceleration);
        }
        
        /// <summary>
        /// Handles keyboard-based vehicle control.
        /// providing arcade-style car controls.
        /// </summary>
        private void ApplyKeyboardInput()
        {
            Vector3 forward = _vehicleTransform.forward;

            float throttle = inputManager.VehicleInput.Throttle;   // W/S or Up/Down
            float steering = inputManager.VehicleInput.Steering;   // A/D or Left/Right

            float speed01 = Mathf.Clamp01(Mathf.Abs(_context.ForwardSpeed) / maxSpeed);

            // Steering authority based on speed
            float steerStrength = Mathf.Lerp(0.3f, 1f, speed01);

            float targetYaw = steering * turnSpeedFactor * steerStrength * _context.ControlAuthority;

            targetYaw = Mathf.Clamp(targetYaw, -maxYawSpeed, maxYawSpeed);

            float currentYaw = _context.Rigidbody.angularVelocity.y;
            float yawDelta = targetYaw - currentYaw;

            float response = _context.IsGrounded ? steeringResponse : steeringResponse * 0.01f;

            _context.Rigidbody.AddTorque(Vector3.up * yawDelta * response, ForceMode.Acceleration);
            
            // Full authority when grounded, heavily reduced while airborne
            float driveAuthority = _context.IsGrounded ? 1f : 0f;

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
                Vector3 forward = _vehicleTransform.forward;

                // Project velocity onto forward axis
                Vector3 projected = Vector3.Project(velocity, forward);

                // Correction force nudges velocity toward forward direction
                Vector3 correction = projected - velocity;

                float steerInfluence = Mathf.Abs(inputManager.VehicleInput?.Steering ?? 0f);
                float gripBoost = Mathf.Lerp(1f, 1.2f, steerInfluence);

                _context.Rigidbody.AddForce(correction * 4.5f * _context.GripFactor * gripBoost, ForceMode.Acceleration);
                
                Vector3 av = _context.Rigidbody.angularVelocity;
                av.y = Mathf.Clamp(av.y, -maxYawSpeed, maxYawSpeed);
                _context.Rigidbody.angularVelocity = av;
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
            Vector3 recoveryAxis = Vector3.Cross(_vehicleTransform.up, Vector3.up);
            _context.Rigidbody.AddTorque(recoveryAxis * 20f * _context.GripFactor, ForceMode.Acceleration);
        }
        
        private void ApplyAirGravity()
        {
            if (_context.IsGrounded) return;

            // Extra gravity to counter floatiness
            _context.Rigidbody.AddForce(Physics.gravity * 1.2f, ForceMode.Acceleration);
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
            Bounds bounds = new Bounds(_vehicleTransform.position, Vector3.zero);
            foreach (Renderer r in _vehicleTransform.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }
        
        bool IsUpsideDown()
        {
            // Dot product below threshold indicates the object is significantly inverted
            return Vector3.Dot(_vehicleTransform.up, Vector3.up) < 0.2f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_context == null || _context.Rigidbody == null) return;

            Vector3 pos = _vehicleTransform.position;

            // Forward direction (vehicle facing)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos + _vehicleTransform.forward * 2f);

            // Lateral velocity visualization (sideways motion)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + _vehicleTransform.right * _context.LateralSpeed);

            // Grip / ground authority indicator
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos + Vector3.down * _context.GripFactor);
            
            // Ground detection ray
            Gizmos.color = _context.IsGrounded ? Color.green : Color.magenta;
            Gizmos.DrawLine(_vehicleTransform.position, _vehicleTransform.position - _vehicleTransform.up * _groundDetector.DebugRayLength);
            
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