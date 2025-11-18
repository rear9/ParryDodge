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
    public float waveStartDelay = 3f;
    public Transform[] spawnPoints; // empties to spawn attacks from

    [Header("Waves")] 
    public List<Wave> waves = new();
    private Transform _lastSpawn;
    
    private void OnEnable() { StartCoroutine(WaveRoutine());  }
    
    private IEnumerator WaveRoutine() // main loop
    {
        StatsManager.Instance.SetGameState("Play");
        yield return new WaitForSeconds(waveStartDelay);
        UIManager _ui = FindFirstObjectByType<UIManager>();
        foreach (var wave in waves) // for each wave, update UI
        {
            _ui.UpdateWaveName(wave.waveName);
            foreach (var entry in wave.waveAttacks) // spawn each attack in the wave with delay between each attack
            {
                for (int i = 0; i < entry.attackCount; i++)
                {
                    SpawnAttack(entry);
                    yield return new WaitForSeconds(entry.attackDelay);
                }
            }
            yield return new WaitForSeconds(wave.waveDelay); // delay between waves
        }
        StartCoroutine(WavesComplete());
    }
    private void SpawnAttack(WaveEntry entry) // attack spawning by using AttackPoolManager
    {
        if (waves.Count == 0 || spawnPoints.Length == 0) return;

        Transform spawn = entry.spawnPoint != null ? entry.spawnPoint : GetRandomSpawnPoint(); // take transform of a random index out of spawn points array if not defined
        GameObject attackObj = AttackPoolManager.Instance.SpawnFromPool(entry.attackName, spawn.position, Quaternion.identity); // pull attack out of pool
        if (!attackObj) return;
        
        if (attackObj.TryGetComponent(out EnemyAttackCore core))
        {
            core.SetPoolKey(entry.attackName); // sets an id for the attack and returns it to pool after its' lifetime
        }
        
        if (attackObj.TryGetComponent(out IEnemyAttack attack))
        {
            attack.InitAttack(GameObject.FindGameObjectWithTag("Player")?.transform); // initializes attack's startup
        }
    }
    
    private Transform GetRandomSpawnPoint() // generating random seed/spawn for attacks
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform; // fallback
        }
        if (spawnPoints.Length == 1) return spawnPoints[0];
    
        Transform spawn;
        do
        {
            spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        while (spawn == _lastSpawn); // stops repeat spawn points from generating
        _lastSpawn = spawn;
        return spawn;
    }
    private IEnumerator WavesComplete()
    {
        UIManager _ui = FindFirstObjectByType<UIManager>();
        _ui.UpdateWaveName("COMPLETE"); // if all waves complete
        StatsManager.Instance.SetGameState("Completion");
        StatsManager.Instance.RecordCompletion();
        StatsManager.Instance.RecordFull(_ui.GetCurrentWaveName()); // stat-tracking
        StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.menuMusic,1f));
        yield return StartCoroutine(TransitionHandler.Instance.FadeOut(2f));
        GameManager.ReturnToMenu();
    }
}
