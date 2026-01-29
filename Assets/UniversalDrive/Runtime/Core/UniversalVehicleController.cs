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

        // private void ApplyForces()
        // {
        //     if (_input == null) return;
        //     
        //     Debug.Log($"Input Throttle: {_input.Throttle}, Steering: {_input.Steering}");
        //
        //     // Forward propulsion force
        //     // Full authority when grounded, heavily reduced while airborne
        //     // float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
        //     // Vector3 force = transform.forward * (_input.Throttle * forwardSpeedFactor * driveAuthority);
        //     // _context.Rigidbody.AddForce(force, ForceMode.Acceleration);
        //     
        //     // Get input vector from controller (X = left/right, Y = forward/backward)
        //     Vector2 inputVector = new Vector2(_input.Steering, _input.Throttle); 
        //
        //     // Convert to world-space movement relative to vehicle forward
        //     Vector3 forward = transform.forward;
        //     Vector3 right = transform.right;
        //
        //     // Combine input with local axes
        //     Vector3 moveDir = (forward * inputVector.y + right * inputVector.x).normalized;
        //
        //     // Scale by speed and authority
        //     float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
        //     Vector3 force = moveDir * forwardSpeedFactor * driveAuthority;
        //
        //     // Apply force
        //     _context.Rigidbody.AddForce(force, ForceMode.Acceleration);
        //
        //     
        //     // Clamp horizontal velocity to enforce top speed
        //     // Vertical velocity (gravity, jumps) is intentionally preserved
        //     Vector3 velocity = _context.Rigidbody.linearVelocity;
        //     Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        //     if (horizontalVelocity.magnitude > maxSpeed)
        //     {
        //         horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
        //         _context.Rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        //     }
        //
        //     // Steering/Yaw torque
        //     float steerTorque = _input.Steering * turnSpeedFactor * _context.ControlAuthority;
        //     _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);
        //     
        //     if (!_context.IsGrounded)
        //     {
        //         // Airborne angular cap
        //         _context.Rigidbody.angularVelocity = Vector3.ClampMagnitude(_context.Rigidbody.angularVelocity, 2.5f);
        //     }
        //     else
        //     {
        //         //Steering torque alone does not reliably redirect linear momentum at higher speeds.
        //         //To achieve arcade-style responsiveness, I introduced a controlled velocity-alignment
        //         //force that gently reshapes the velocity vector toward the vehicle’s facing direction while grounded.
        //         velocity = _context.Rigidbody.linearVelocity;
        //         //Vector3 forward = Vector3.forward;
        //         Vector3 projected = Vector3.Project(velocity, forward);
        //         Vector3 correction = projected - velocity;
        //         // Uses grip factor to determine how aggressively velocity is reshaped
        //         _context.Rigidbody.AddForce(correction * 2.5f * _context.GripFactor, ForceMode.Acceleration);
        //     }
        //     
        //     if (_context.IsGrounded && IsUpsideDown())
        //     {
        //         // Applies corrective torque to assist flip recovery.
        //         // This does not auto-flip the vehicle; it biases recovery
        //         // while still requiring player input or momentum.
        //         Vector3 recoveryAxis = Vector3.Cross(transform.up, Vector3.up);
        //         _context.Rigidbody.AddTorque(recoveryAxis * 20f * _context.GripFactor, ForceMode.Acceleration);
        //     }
        // }
        
        private void ApplyForces()
        {
            if (_input == null) return;

            Vector3 forward = transform.forward;

            if (_input is MobileVehicleInput mobile)
            {
                Vector2 inputVector = mobile.InputVector;
                
                // Forward/backward
                float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
                
                // Any joystick drag = forward intent
                float throttle = Mathf.Clamp01(inputVector.magnitude);
                
                // Apply forward force ALWAYS when dragging
                _context.Rigidbody.AddForce(forward * throttle * forwardSpeedFactor * driveAuthority, ForceMode.Acceleration);
                
                // Steering still comes from X
                float steerTorque = inputVector.x * turnSpeedFactor * _context.ControlAuthority * throttle; // scale steering by movement
                
                _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);
                
                
                // Vector2 inputVector = mobile.InputVector;
                //
                // float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
                //
                // // Always move forward if joystick is dragged
                // float throttle = Mathf.Clamp01(inputVector.magnitude);
                // _context.Rigidbody.AddForce(forward * throttle * forwardSpeedFactor * driveAuthority, ForceMode.Acceleration);
                //
                // // Detect forward vs backward intent
                // float forwardIntent = 0f;
                // if (inputVector.sqrMagnitude > 0.001f)
                // {
                //     forwardIntent = Vector2.Dot(inputVector.normalized, Vector2.up);
                // }
                //
                // // Steering strength
                // float steerStrength = inputVector.x;
                //
                // // Boost steering when dragging backward
                // float reverseTurnBoost = forwardIntent < 0f ? Mathf.Lerp(1f, 1.5f, -forwardIntent) : 1f;
                //
                // float steerTorque = steerStrength * turnSpeedFactor * _context.ControlAuthority * throttle * reverseTurnBoost;
                //
                // _context.Rigidbody.AddTorque(Vector3.up * steerTorque, ForceMode.Acceleration);
            }
            else
            {
                // Keyboard → forward/back + left/right turn (classic arcade feel)
                float throttle = _input.Throttle;  // W/S or Up/Down
                float steering = _input.Steering;  // A/D or Left/Right

                // Apply torque for turning
                _context.Rigidbody.AddTorque(Vector3.up * steering * turnSpeedFactor * _context.ControlAuthority, ForceMode.Acceleration);

                // Forward force along vehicle forward
                float driveAuthority = _context.IsGrounded ? 1f : 0.2f;
                Vector3 force = forward * throttle * forwardSpeedFactor * driveAuthority;
                _context.Rigidbody.AddForce(force, ForceMode.Acceleration);
            }

            // Grounded velocity alignment (arcade feel)
            if (_context.IsGrounded)
            {
                Vector3 velocity = _context.Rigidbody.linearVelocity;
                Vector3 projected = Vector3.Project(velocity, forward);
                Vector3 correction = projected - velocity;
                _context.Rigidbody.AddForce(correction * 2.5f * _context.GripFactor, ForceMode.Acceleration);
            }
            else
            {
                _context.Rigidbody.angularVelocity = Vector3.ClampMagnitude(_context.Rigidbody.angularVelocity, 2.5f);
            }

            ApplyUpsideDownRecovery();
            ClampHorizontalSpeed();
        }
        
        private void ApplyUpsideDownRecovery()
        {
            if (!_context.IsGrounded || !IsUpsideDown()) return;

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