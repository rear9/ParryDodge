using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;


    private bool isUsingController = false;

    private void Start()
    {
        Cursor.visible = true;
        
        playButton.onClick.AddListener(GameManager.StartGame);
        quitButton.onClick.AddListener(GameManager.QuitGame);
        
        if (AudioManager.Instance != null)
        {
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", .5f);
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", .5f);
        }
    } 
    void Update()
    {
        // check for controller input (stick movement or button press)
        if (Gamepad.current != null)
        {
            bool controllerInputDetected = 
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.2f ||
                Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.2f ||
                Gamepad.current.dpad.up.wasPressedThisFrame ||
                Gamepad.current.dpad.down.wasPressedThisFrame ||
                Gamepad.current.dpad.left.wasPressedThisFrame ||
                Gamepad.current.dpad.right.wasPressedThisFrame ||
                Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame ||
                Gamepad.current.buttonWest.wasPressedThisFrame ||
                Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (controllerInputDetected && !isUsingController)
            {
                isUsingController = true;
                if (eventSystem.currentSelectedGameObject == null)
                {
                    eventSystem.SetSelectedGameObject(quitButton.gameObject);
                }
            }
        }
        
        // check for mouse movement
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f)
        {
            if (isUsingController)
            {
                isUsingController = false;
                eventSystem.SetSelectedGameObject(null);
            }
        }
    }
    
}