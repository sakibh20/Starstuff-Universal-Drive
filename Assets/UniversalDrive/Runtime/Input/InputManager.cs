using UnityEngine;
using UniversalDrive;

public class InputManager : MonoBehaviour
{
    [SerializeField] private VirtualJoystick virtualJoystick; // UI for mobile
    private IVehicleInput _vehicleInput;

    public IVehicleInput VehicleInput => _vehicleInput;

    private void Start()
    {
        SetupKeyboardInput();
    }

    public void SetupFloatingJoystick()
    {
        virtualJoystick.gameObject.SetActive(true);

        RemoveExistingInput();
        _vehicleInput = gameObject.AddComponent<MobileVehicleInput>();
        if (_vehicleInput is MobileVehicleInput mobileInput)
        {
            mobileInput.joystick = virtualJoystick;
        }
    }

    public void SetupKeyboardInput()
    {
        virtualJoystick.gameObject.SetActive(false);

        RemoveExistingInput();
        _vehicleInput = gameObject.AddComponent<KeyboardVehicleInput>();
    }

    private void RemoveExistingInput()
    {
        // Remove any existing IVehicleInput component
        var existingInputs = GetComponents<MonoBehaviour>();
        foreach (var comp in existingInputs)
        {
            if (comp is IVehicleInput)
            {
                Destroy(comp);
            }
        }
    }
}