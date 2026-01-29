using TMPro;
using UnityEngine;
using UniversalDrive;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown tmpDropdown;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private GameObject settingsMenu;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private GameObject bananaPrefab;
    
    [SerializeField] private VehicleCameraFollow vehicleCameraFollow;
    [SerializeField] private UniversalVehicleController vehicleController;
    
    [SerializeField] private Transform vehicleParent;
    
    private Transform _vehicle;
    
    private bool _isSettingsMenuActive = false;
    
    void Start()
    {
        settingsMenu.SetActive(_isSettingsMenuActive);
        tmpDropdown.onValueChanged.AddListener(delegate { SetInputType(tmpDropdown); });
        CarToggleChanged(true);
    }

    public void ToggleSettingsMenu()
    {
        _isSettingsMenuActive = !_isSettingsMenuActive;
        settingsMenu.SetActive(_isSettingsMenuActive);
    }

    private void SetInputType(TMP_Dropdown change)
    {
        string selectedOptionText = tmpDropdown.options[change.value].text;
        if(selectedOptionText == "Keyboard")
        {
            SetInputKeyboard();
        }
        else if(selectedOptionText == "Joystick")
        {
            SetInputMobile();
        }
    }

    // Set the active input method from UI (keyboard/mobile)
    private void SetInputKeyboard()
    {
        inputManager.SetupKeyboardInput();
    }

    private void SetInputMobile()
    {
        inputManager.SetupFloatingJoystick();
    }
    
    public void CarToggleChanged(bool isOn)
    {
        if (!isOn) return;
        SetNewVehicle(carPrefab);
    }
    
    public void HouseToggleChanged(bool isOn)
    {
        if (!isOn) return;
        SetNewVehicle(housePrefab);
    }
    
    public void BananaToggleChanged(bool isOn)
    {
        if (!isOn) return;
        SetNewVehicle(bananaPrefab);
    }

    private void SetNewVehicle(GameObject vehiclePrefab)
    {
        if(_vehicle != null)
        {
            Destroy(_vehicle.gameObject);
        }
        _vehicle = Instantiate(vehiclePrefab, vehicleParent).transform;
        SetTargets();
    }
    
    private void SetTargets()
    {
        vehicleCameraFollow.Target = _vehicle;
        
        vehicleController.SetVehicle(_vehicle);
    }
}