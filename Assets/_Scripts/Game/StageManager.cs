using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [SerializeField] private List<StageInfo> stages = new();
    
    private StageInfo _currentStage;
    private int _highestClearedStage = 0;
    private bool _isEndlessMode = false;
    
    private const string HIGHEST_STAGE_KEY = "HighestClearedStage";
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadProgress();
    }
    
    public void SelectStage(int stageNumber)
    {
        _isEndlessMode = false;
        _currentStage = stages.Find(s => s.stageNumber == stageNumber);
        if (_currentStage == null)
        {
            Debug.LogError($"Stage {stageNumber} not found!");
        }
    }
    
    public void SelectEndlessMode()
    {
        _isEndlessMode = true;
        _currentStage = null; // Endless mode doesn't use a specific stage
    }
    
    public bool IsEndlessMode() => _isEndlessMode;
    
    public StageInfo GetCurrentStage() => _currentStage;
    
    public List<StageInfo> GetAllStages() => stages;
    
    public int GetHighestClearedStage() => _highestClearedStage;
    
    public void SetStageCleared(int stageNumber)
    {
        if (stageNumber > _highestClearedStage)
        {
            _highestClearedStage = stageNumber;
            PlayerPrefs.SetInt(HIGHEST_STAGE_KEY, _highestClearedStage);
            PlayerPrefs.Save();
            
            // Unlock next stage if it exists
            if (stageNumber < stages.Count)
            {
                var nextStage = stages.Find(s => s.stageNumber == stageNumber + 1);
                if (nextStage != null)
                {
                    nextStage.isUnlocked = true;
                }
            }
        }
    }
    
    private void LoadProgress()
    {
        _highestClearedStage = PlayerPrefs.GetInt(HIGHEST_STAGE_KEY, 0);
        
        // Unlock stages based on progress
        if (stages.Count > 0)
        {
            var stage1 = stages.Find(s => s.stageNumber == 1);
            if (stage1 != null) stage1.isUnlocked = true; // Stage 1 always unlocked
        }
        
        for (int i = 1; i <= _highestClearedStage && i < stages.Count; i++)
        {
            var stage = stages.Find(s => s.stageNumber == i + 1);
            if (stage != null)
            {
                stage.isUnlocked = true;
            }
        }
    }
}