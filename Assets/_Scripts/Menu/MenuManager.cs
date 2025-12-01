using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private UIButtonSFX UIButtonSFX;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle tutorialToggle;
    private bool isUsingController;

    private const string TUTORIAL_KEY = "SkipTutorial";
    public static bool SkipTutorial { get; private set; }
    private void Start()
    {
        Cursor.visible = true;
        
        playButton.onClick.AddListener(GameManager.StartGame);
        quitButton.onClick.AddListener(GameManager.QuitGame);
        
        if (tutorialToggle != null)
        {
            tutorialToggle.isOn = PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1;
            tutorialToggle.onValueChanged.AddListener(TutorialToggleChanged);
            SkipTutorial = tutorialToggle.isOn;
        }
        
        if (AudioManager.Instance != null)
        {
            masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", .5f);
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", .5f);
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", .5f);
        }
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var sfx = button.gameObject.AddComponent<UIButtonSFX>();
            sfx.hoverSFX = AudioManager.Instance.menuHoverSFX;
            sfx.clickSFX = AudioManager.Instance.menuPressSFX;
        }
    }
    
    private void TutorialToggleChanged(bool isOn)
    {
        AudioManager.PlaySFX(AudioManager.Instance.tickSFX);
        PlayerPrefs.SetInt(TUTORIAL_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        SkipTutorial = isOn;
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
                    Cursor.visible = false;
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
                Cursor.visible = true;
            }
        }
    }
    
}