using UnityEngine;

namespace UniversalDrive
{
    internal class KeyboardVehicleInput : MonoBehaviour, IVehicleInput
    {
        public float Throttle => Input.GetAxis("Vertical");
        public float Steering => Input.GetAxis("Horizontal");
    }
}