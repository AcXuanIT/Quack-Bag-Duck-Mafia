using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn Enemy theo Wave, được BattleManager gọi trực tiếp khi bắt đầu Turn Battle
/// (truyền vào currentWavesIndex và MapBattsleData — BattleManager lấy MapBattsleData
/// từ DataManager trước khi gọi).
/// </summary>
public class BattleSpawnEnemy : MonoBehaviour
{
    [Header("=== Spawn ===")]
    [SerializeField] private EnemyDuck enemyPrefab;

    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private Transform enemyContainer;

    [Header("=== Timing ===")]
    [SerializeField] private float spawnDelay;

    private readonly List<EnemyDuck> _spawnedEnemies = new List<EnemyDuck>();
    private Coroutine _spawnRoutine;

    private bool _waveSpawnStarted;

    // ─── Public API ──
    public void SpawnWave(int waveIndex, MapBattsleData mapBattleData)
    {
        if (mapBattleData == null) return;

        WaveData wave = mapBattleData.GetWave(waveIndex);

        if (wave == null) return;

        if (_spawnRoutine != null)
            StopCoroutine(_spawnRoutine);

        _spawnedEnemies.Clear();
        _waveSpawnStarted = true;

        _spawnRoutine = StartCoroutine(SpawnEnemiesForWave(wave));
    }

    public void ClearSpawnedEnemies()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        foreach (var e in _spawnedEnemies)
            if (e != null) Destroy(e.gameObject);
        _spawnedEnemies.Clear();
        _waveSpawnStarted = false;
    }
    public bool IsWaveCleared()
    {
        if (!_waveSpawnStarted) return false;     
        if (_spawnRoutine != null) return false;   

        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && !enemy.IsDead)
                return false; 
        }

        return true; 
    }

    // ─── Spawn Logic ─
    private IEnumerator SpawnEnemiesForWave(WaveData wave)
    {
        if (wave.Enemies == null) yield break;

        foreach (var entry in wave.Enemies)
        {
            if (entry == null || entry.EnemyData == null || entry.EnemyData.Data == null) continue;

            for (int i = 0; i < entry.Count; i++)
            {
                SpawnEnemy(entry.EnemyData.Data);
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        _spawnRoutine = null;
    }

    private void SpawnEnemy(EnemyDuckData data)
    {
        if (enemyPrefab == null) return;

        Transform point = GetSpawnPoint();
        Transform parent = enemyContainer != null ? enemyContainer : transform;

        EnemyDuck obj = Instantiate(enemyPrefab, point.position, point.rotation, parent);

        obj.Init(data, data.GetWeaponEntry(), 1, 1);

        _spawnedEnemies.Add(obj);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
