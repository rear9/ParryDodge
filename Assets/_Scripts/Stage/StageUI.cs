using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject stageSelectionPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Transform endlessButtonContainer; // Separate container for endless mode
    [SerializeField] private Button exitButton;
    
    [Header("Button Prefabs")]
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private GameObject endlessButtonPrefab; // Optional: different visual for endless
    
    [Header("Visual Settings")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private Color clearedColor = new Color(0.5f, 1f, 0.5f, 1f);
    [SerializeField] private Color endlessColor = new Color(1f, 0.5f, 0.2f, .9f); // Orange for endless
    
    private List<StageButton> _stageButtons = new();
    private EndlessButton _endlessButton;
    private bool _initialized = false;
    
    private void Start()
    {
        stageSelectionPanel.SetActive(false);
        exitButton.onClick.AddListener(CloseStageSelection);
        
        // Create buttons once at start
        InitializeStageButtons();
        InitializeEndlessButton();
    }
    
    public void OpenStageSelection()
    {
        if (!_initialized)
        {
            InitializeStageButtons();
            InitializeEndlessButton();
        }
        
        RefreshStageButtons();
        stageSelectionPanel.SetActive(true);
        Cursor.visible = true;
    }
    
    public void CloseStageSelection()
    {
        stageSelectionPanel.SetActive(false);
    }
    
    private void InitializeStageButtons()
    {
        // Clear existing buttons if reinitializing
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        _stageButtons.Clear();
        
        var stages = StageManager.Instance.GetAllStages();
        
        foreach (var stage in stages)
        {
            GameObject buttonObj = Instantiate(stageButtonPrefab, buttonContainer);
            StageButton stageBtn = buttonObj.GetComponent<StageButton>();
            
            if (stageBtn == null)
            {
                stageBtn = buttonObj.AddComponent<StageButton>();
            }
            
            stageBtn.Initialize(stage.stageNumber, this);
            _stageButtons.Add(stageBtn);
        }
        
        _initialized = true;
    }
    
    private void InitializeEndlessButton()
    {
        // Clear existing endless button if it exists
        if (_endlessButton != null)
        {
            Destroy(_endlessButton.gameObject);
        }
        
        // Use endless prefab if assigned, otherwise use stage prefab
        GameObject prefab = endlessButtonPrefab != null ? endlessButtonPrefab : stageButtonPrefab;
        GameObject buttonObj = Instantiate(prefab, endlessButtonContainer);
        
        _endlessButton = buttonObj.GetComponent<EndlessButton>();
        if (_endlessButton == null)
        {
            _endlessButton = buttonObj.AddComponent<EndlessButton>();
        }
        
        _endlessButton.Initialize(this);
    }
    
    private void RefreshStageButtons()
    {
        var stages = StageManager.Instance.GetAllStages();
        int highestCleared = StageManager.Instance.GetHighestClearedStage();
        
        foreach (var stageBtn in _stageButtons)
        {
            var stage = stages.Find(s => s.stageNumber == stageBtn.StageNumber);
            if (stage != null)
            {
                bool isCleared = stage.stageNumber <= highestCleared;
                stageBtn.UpdateVisuals(stage.isUnlocked, isCleared, unlockedColor, lockedColor, clearedColor);
                
                // Add/remove SFX based on unlock status
                UpdateButtonSFX(stageBtn.GetComponent<Button>(), stage.isUnlocked);
            }
        }
        
        // Endless mode unlocks after clearing all 16 stages
        if (_endlessButton != null)
        {
            bool endlessUnlocked = highestCleared >= 16;
            _endlessButton.UpdateVisuals(endlessUnlocked, endlessUnlocked ? endlessColor : lockedColor);
            
            // Add/remove SFX for endless button
            UpdateButtonSFX(_endlessButton.GetComponent<Button>(), endlessUnlocked);
        }
        
        // Always add SFX to exit button
        UpdateButtonSFX(exitButton, true);
    }
    
    private void UpdateButtonSFX(Button button, bool isUnlocked)
    {
        if (button == null) return;
        
        UIButtonSFX sfx = button.GetComponent<UIButtonSFX>();
        
        if (isUnlocked)
        {
            // Add SFX if unlocked and not already present
            if (sfx == null)
            {
                sfx = button.gameObject.AddComponent<UIButtonSFX>();
                sfx.hoverSFX = AudioManager.Instance.menuHoverSFX;
                sfx.clickSFX = AudioManager.Instance.menuPressSFX;
            }
        }
        else
        {
            // Remove SFX if locked
            if (sfx != null)
            {
                Destroy(sfx);
            }
        }
    }
    
    public void OnStageButtonClicked(int stageNumber)
    {
        CloseStageSelection(); // Close the canvas before starting
        StageManager.Instance.SelectStage(stageNumber);
        AudioManager.Instance.StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.levelMusic, 1f));
        GameManager.Instance.StartGame();
    }
    
    public void OnEndlessModeClicked()
    {
        CloseStageSelection();
        StageManager.Instance.SelectEndlessMode();
        AudioManager.Instance.StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.levelMusic, 1f));
        GameManager.Instance.StartGame();
    }
}

// Separate component for each stage button
public class StageButton : MonoBehaviour
{
    private Button _button;
    private TextMeshProUGUI _text;
    private Image _image;
    private int _stageNumber;
    private StageUI _parentUI;
    
    public int StageNumber => _stageNumber;
    
    public void Initialize(int stageNumber, StageUI parentUI)
    {
        _stageNumber = stageNumber;
        _parentUI = parentUI;
        
        _button = GetComponent<Button>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponent<Image>();
        
        if (_text != null)
        {
            _text.text = _stageNumber.ToString();
        }
        
        // Remove old listeners and add new one
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }
    
    public void UpdateVisuals(bool isUnlocked, bool isCleared, Color unlockedColor, Color lockedColor, Color clearedColor)
    {
        _button.interactable = isUnlocked;
        
        if (_image != null)
        {
            if (isCleared)
            {
                _image.color = clearedColor;
            }
            else if (isUnlocked)
            {
                _image.color = unlockedColor;
            }
            else
            {
                _image.color = lockedColor;
                _text.color = lockedColor;
            }
        }
    }
    
    private void OnClick()
    {
        _parentUI.OnStageButtonClicked(_stageNumber);
    }
}

// Separate component for endless mode button
public class EndlessButton : MonoBehaviour
{
    private Button _button;
    private TextMeshProUGUI _text;
    private Image _image;
    private StageUI _parentUI;
    
    public void Initialize(StageUI parentUI)
    {
        _parentUI = parentUI;
        
        _button = GetComponent<Button>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponent<Image>();
        
        // Remove old listeners and add new one
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }
    
    public void UpdateVisuals(bool isUnlocked, Color colorToUse)
    {
        _button.interactable = isUnlocked;
        
        if (_image != null)
        {
            _image.color = colorToUse;
            _text.color = colorToUse;
        }
        
    }
    
    private void OnClick()
    {
        _parentUI.OnEndlessModeClicked();
    }
}