using UnityEngine;
using Unity.Services.Core;
using Abertay.Analytics;
using System.Collections.Generic;
using System;

[System.Serializable]
public class PlayerStats // variable stats class object
{
    public string gameState = "Play";
    public int totalDeaths;
    public int totalCompletions;
    public int stageProgression = 0;
    public int totalParries;
    public int totalDodges;
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }
    private PlayerStats _stats;
    private UIManager _ui;
    private const string TutorialKey = "TutorialCompleted";
    private const string PlayerIdKey = "PlayerID";
    private const string StatsKey = "PlayerStats";
    private string _playerID;

    private void Awake() // init singleton
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // create persistent and viewable stats using json
        if (PlayerPrefs.HasKey(StatsKey))
            _stats = JsonUtility.FromJson<PlayerStats>(PlayerPrefs.GetString(StatsKey));
        else
            _stats = new PlayerStats();

        // create persistent player ID
        if (!PlayerPrefs.HasKey(PlayerIdKey))
            PlayerPrefs.SetString(PlayerIdKey, Guid.NewGuid().ToString());
        _playerID = PlayerPrefs.GetString(PlayerIdKey);

        _ui = FindFirstObjectByType<UIManager>();
    }

    private void Start()
    {
        AnalyticsManager.Initialise("v0.9");
    }
    
    private void OnApplicationPause(bool pause) // attempt to save stats on pause
    {
        if (pause && _ui != null)
        {
            _stats.gameState = "Paused/Closing";
            RecordFull(); // pause data collection hook
            SaveStats();
        }
    }

    // --- general stats ---
    public void RecordFull()
    {
        _stats.stageProgression = StageManager.Instance.GetHighestClearedStage();
        SaveStats();
        var parameters = new Dictionary<string, object> // params for sending as event or to logs
        {
            { "player_id", _playerID }, // identifier
            { "time", Time.time }, // current time
            { "game_state", _stats.gameState }, // state (paused/dead/quit)
            { "highest_cleared_stage", _stats.stageProgression },
            { "total_deaths", _stats.totalDeaths },
            { "total_parries", _stats.totalParries },
            { "total_dodges", _stats.totalDodges },
            { "total_completions", _stats.totalCompletions }
        };
        AnalyticsManager.SendCustomEvent("Stats", parameters);
        StartCoroutine(GlobalStatLogger.SendToGoogleSheet(parameters));
    }

    private void SaveStats() // save back to json
    {
        string json = JsonUtility.ToJson(_stats);
        PlayerPrefs.SetString(StatsKey, json);
        PlayerPrefs.Save();
    }

    // --- stat increments ---
    public void RecordParry()
    {
        _stats.totalParries++;
        SaveStats();
    }

    public void RecordDodge()
    {
        _stats.totalDodges++;
        SaveStats();
    }

    public void RecordCompletion()
    {
        _stats.totalCompletions++;
        SaveStats();
    }

    public void RecordDeath(string waveName)
    {
        _stats.totalDeaths++;
        _stats.gameState = "Death";
        SaveStats();
        RecordFull();
    }

    public PlayerStats GetStats() => _stats; // get stats helper (for possible custom events)
    public void SetGameState(string state) => _stats.gameState = state; // state setter

}
