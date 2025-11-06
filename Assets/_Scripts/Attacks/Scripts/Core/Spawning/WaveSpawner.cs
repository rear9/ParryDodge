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
        yield return new WaitForSeconds(waveStartDelay);
        UIManager _ui = FindFirstObjectByType<UIManager>();
        foreach (var wave in waves)
        {
            Debug.Log($"Wave: {wave.waveName}");
            _ui.UpdateWaveName(wave.waveName);
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
        // waves done
        _ui.UpdateWaveName("COMPLETE");
        StatsManager.Instance.RecordCompletion();
        StatsManager.Instance.RecordFull(_ui.GetCurrentWaveName());
        yield return new WaitForSeconds(3f);
        GameManager.ReturnToMenu();
    }
    private void SpawnAttack(WaveEntry entry) // attack spawning
    {
        if (waves.Count == 0 || spawnPoints.Length == 0) return;

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
        } while (spawn == _lastSpawn);
    
        _lastSpawn = spawn;
        return spawn;
    }
}
