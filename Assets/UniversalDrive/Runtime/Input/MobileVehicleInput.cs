using UnityEngine;

namespace UniversalDrive
{
    [RequireComponent(typeof(UniversalVehicleController))]
    internal class MobileVehicleInput : MonoBehaviour, IVehicleInput
    {
        public VirtualJoystick joystick { get; set; }

        public float Throttle { get; private set; }
        public float Steering { get; private set; }
        public Vector2 InputVector { get; private set; }

        private void Update()
        {
            if (joystick == null)
            {
                Throttle = 0f;
                Steering = 0f;
                InputVector = Vector2.zero;
                return;
            }

            Vector2 rawInput = joystick.InputVector;

            // Smooth the input for arcade feel
            InputVector = Vector2.Lerp(InputVector, rawInput, 80f * Time.deltaTime);

            // Extract components
            Throttle = InputVector.y;
            Steering = InputVector.x;
        }
    }
}