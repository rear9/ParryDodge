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
    [SerializeField] private Slider colorSlider;
    [SerializeField] private Image playerHeart;
    [SerializeField] private Toggle tutorialToggle;
    private bool isUsingController;

    private const string TUTORIAL_KEY = "SkipTutorial";
    private const string COLOR_HUE_KEY = "PlayerColorHue";
    
    public static bool SkipTutorial { get; private set; }
    
    private void Start() // start-up, sets all slider/boolean values from PlayerPrefs & adds listeners
    {
        Cursor.visible = true;
        
        playButton.onClick.AddListener(GameManager.Instance.StartGame);
        quitButton.onClick.AddListener(GameManager.Instance.QuitGame);
        
        if (tutorialToggle != null) // tutorial setup
        {
            tutorialToggle.isOn = PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1;
            tutorialToggle.onValueChanged.AddListener(TutorialToggleChanged);
            SkipTutorial = tutorialToggle.isOn;
        }
        
        if (AudioManager.Instance != null) // audio setup
        {
            masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", .5f);
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", .5f);
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", .5f);
        }
        
        if (colorSlider != null && playerHeart != null) // color slider setup
        {
            float savedHue = PlayerPrefs.GetFloat(COLOR_HUE_KEY, 0f);
            colorSlider.value = savedHue;
            UpdatePlayerColor(savedHue);
            colorSlider.onValueChanged.AddListener(OnColorSliderChanged);
        }
        
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None)) // adds sfx to all buttons in scene
        {
            var sfx = button.gameObject.AddComponent<UIButtonSFX>();
            sfx.hoverSFX = AudioManager.Instance.menuHoverSFX;
            sfx.clickSFX = AudioManager.Instance.menuPressSFX;
        }
    }
    void Update() // input detection for seamless keyboard > controller switching + vice versa
    {
        // check for controller input (stick movement or button press)
        if (Gamepad.current != null)
        {
            bool controllerInputDetected =  // checks for standard controller inputs, stick magnitudes to prevent switching due to stick drift
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
                isUsingController = true; // switch inputs to controller
                if (eventSystem.currentSelectedGameObject == null)
                {
                    eventSystem.SetSelectedGameObject(quitButton.gameObject);
                    Cursor.visible = false;
                }
            }
        }
        
        // check for mouse movement
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f) // checks for mouse movement
        {
            if (isUsingController)
            {
                isUsingController = false; // switch inputs to mouse
                eventSystem.SetSelectedGameObject(null);
                Cursor.visible = true;
            }
        }
    }
    
    private void TutorialToggleChanged(bool isOn) // tutorial toggle function
    {
        AudioManager.PlaySFX(AudioManager.Instance.tickSFX);
        PlayerPrefs.SetInt(TUTORIAL_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        SkipTutorial = isOn;
    }
    private void UpdatePlayerColor(float hue) // slider update function
    {
        if (playerHeart != null)
        {
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);
            playerHeart.color = newColor;
        }
    }
    
    // static method to get the saved color for use in other scenes
    public static Color GetSavedPlayerColor()
    {
        float savedHue = PlayerPrefs.GetFloat(COLOR_HUE_KEY, 0f);
        return Color.HSVToRGB(savedHue, 1f, 1f);
    }
    
    private void OnColorSliderChanged(float hueValue) // this is what the slider subscribes to
    {
        UpdatePlayerColor(hueValue);
        PlayerPrefs.SetFloat(COLOR_HUE_KEY, hueValue);
        PlayerPrefs.Save();
    }
}