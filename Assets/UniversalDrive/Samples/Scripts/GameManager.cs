using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown tmpDropdown;
    
    [SerializeField] private InputManager inputManager;
    [SerializeField] private GameObject settingsMenu;
    
    private bool _isSettingsMenuActive = false;
    
    void Start()
    {
        settingsMenu.SetActive(_isSettingsMenuActive);

        tmpDropdown.onValueChanged.AddListener(delegate { SetInputType(tmpDropdown); });
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

    // Allows switching the controlled vehicle at runtime
    public void SetCar()
    {
        
    }
    
    // Allows switching the controlled vehicle at runtime
    public void SetHouse()
    {
        
    }
    
    // Allows switching the controlled vehicle at runtime
    public void SetBanana()
    {
        
    }
}