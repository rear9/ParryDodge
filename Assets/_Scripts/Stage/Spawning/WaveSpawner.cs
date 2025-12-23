using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

[System.Serializable]
public class Wave // wave class with assignable attacks
{
    public string waveName;
    public List<WaveEntry> waveAttacks = new();
    public float waveDelay = 2f;
}

[System.Serializable]
public class WaveEntry // variables for each assignable attack
{
    public string attackName;
    public int attackCount = 1;
    public float attackDelay = 0.5f;
    public Transform spawnPoint;
}

public class WaveSpawner : MonoBehaviour
{
    [Header("Startup")]
    public float defaultWaveStartDelay = 3f;
    public Transform[] spawnPoints;

    [Header("Endless Mode Settings")]
    [SerializeField] private List<Wave> endlessWavePool = new(); // Predefined wave patterns for endless
    [SerializeField] private float endlessWaveDelay = 2f;
    [SerializeField] private float difficultyScaling = 1.1f; // Multiply spawn delays by this each cycle
    
    private List<Wave> _currentWaves = new();
    private float _waveStartDelay;
    private Transform _lastSpawn;
    private int _endlessWaveCount = 0;
    
    private void OnEnable() 
    {
        LoadStageWaves();
        StartCoroutine(WaveRoutine());
    }
    
    private void LoadStageWaves()
    {
        if (StageManager.Instance.IsEndlessMode())
        {
            // Endless mode uses the wave pool
            _currentWaves = new List<Wave>(endlessWavePool);
            _waveStartDelay = defaultWaveStartDelay;
        }
        else
        {
            // Normal stage mode
            StageInfo currentStage = StageManager.Instance.GetCurrentStage();
            
            if (currentStage != null)
            {
                _currentWaves = new List<Wave>(currentStage.waves);
                _waveStartDelay = currentStage.waveStartDelay;
            }
            else
            {
                Debug.LogError("No stage selected!");
                _currentWaves = new List<Wave>();
                _waveStartDelay = defaultWaveStartDelay;
            }
        }
    }
    
    private IEnumerator WaveRoutine()
    {
        StatsManager.Instance.SetGameState("Play");
        
        if(MenuManager.SkipTutorial)
        {
            yield return new WaitForSeconds(_waveStartDelay);
        }
        yield return new WaitForSeconds(_waveStartDelay);
        
        UIManager _ui = FindFirstObjectByType<UIManager>();
        
        // Set the stage name at the start
        if (StageManager.Instance.IsEndlessMode())
        {
            _ui.UpdateWaveName("ENDLESS");
            yield return EndlessWaveRoutine(_ui);
        }
        else
        {
            StageInfo currentStage = StageManager.Instance.GetCurrentStage();
            if (currentStage != null)
            {
                _ui.UpdateWaveName(currentStage.stageName);
            }
            yield return StandardWaveRoutine(_ui);
        }
    }
    
    private IEnumerator StandardWaveRoutine(UIManager ui)
    {
        foreach (var wave in _currentWaves)
        {
            foreach (var entry in wave.waveAttacks)
            {
                for (int i = 0; i < entry.attackCount; i++)
                {
                    SpawnAttack(entry);
                    yield return new WaitForSeconds(entry.attackDelay);
                }
            }
            yield return new WaitForSeconds(wave.waveDelay);
        }
        StartCoroutine(WavesComplete());
    }
    
    private IEnumerator EndlessWaveRoutine(UIManager ui)
    {
        while (true)
        {
            _endlessWaveCount++;
            Wave selectedWave = endlessWavePool[Random.Range(0, endlessWavePool.Count)];
            
            foreach (var entry in selectedWave.waveAttacks)
            {
                for (int i = 0; i < entry.attackCount; i++)
                {
                    SpawnAttack(entry);
                    
                    float scaledDelay = entry.attackDelay / Mathf.Pow(difficultyScaling, _endlessWaveCount / 10f);
                    scaledDelay = Mathf.Max(scaledDelay, 0.1f);
                    yield return new WaitForSeconds(scaledDelay);
                }
            }
            float scaledWaveDelay = endlessWaveDelay / Mathf.Pow(difficultyScaling, _endlessWaveCount / 10f);
            scaledWaveDelay = Mathf.Max(scaledWaveDelay, 0.5f);
            yield return new WaitForSeconds(scaledWaveDelay);
        }
    }
    
    private void SpawnAttack(WaveEntry entry)
    {
        if (spawnPoints.Length == 0) return;

        Transform spawn = entry.spawnPoint != null ? entry.spawnPoint : GetRandomSpawnPoint();
        GameObject attackObj = AttackPoolManager.Instance.SpawnFromPool(entry.attackName, spawn.position, Quaternion.identity);
        if (!attackObj) return;
        
        if (attackObj.TryGetComponent(out EnemyAttackCore core))
        {
            core.SetPoolKey(entry.attackName);
        }
        
        if (attackObj.TryGetComponent(out IEnemyAttack attack))
        {
            attack.InitAttack(GameObject.FindGameObjectWithTag("Player")?.transform);
        }
    }
    
    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform;
        }
        if (spawnPoints.Length == 1) return spawnPoints[0];
    
        Transform spawn;
        do
        {
            spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        while (spawn == _lastSpawn);
        _lastSpawn = spawn;
        return spawn;
    }
    
    private IEnumerator WavesComplete()
    {
        UIManager _ui = FindFirstObjectByType<UIManager>();
        _ui.UpdateWaveName("COMPLETE");
        
        StageInfo currentStage = StageManager.Instance.GetCurrentStage();
        if (currentStage != null)
        {
            StageManager.Instance.SetStageCleared(currentStage.stageNumber);
        }
        
        StatsManager.Instance.SetGameState("Completion");
        StatsManager.Instance.RecordCompletion();
        StatsManager.Instance.RecordFull();
        
        yield return new WaitForSecondsRealtime(2f);
        GameManager.Instance.ReturnToMenu();
    }
}
