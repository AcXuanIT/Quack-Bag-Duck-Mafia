using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn Enemy theo Wave, được BattleManager gọi trực tiếp khi bắt đầu Turn Battle
/// (truyền vào currentWavesIndex và MapBattsleData — BattleManager lấy MapBattsleData
/// từ DataManager trước khi gọi).
/// </summary>
public class BattleSpawnEnemy : MonoBehaviour
{
    [Header("=== References ===")]

    [Header("=== Spawn ===")]
    [Tooltip("Prefab EnemyObject dùng để Instantiate")]
    [SerializeField] private EnemyDuck enemyPrefab;

    [Tooltip("Các điểm spawn enemy. Nếu để trống sẽ spawn tại vị trí của chính BattleSpawnEnemy")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Container chứa các enemy được spawn ra (tuỳ chọn)")]
    [SerializeField] private Transform enemyContainer;

    private readonly List<EnemyDuck> _spawnedEnemies = new List<EnemyDuck>();

    // ─── Public API ─────────────────────────────────────────

    /// <summary>
    /// Spawn toàn bộ enemy thuộc Wave có WaveIndex = waveIndex, dữ liệu lấy từ
    /// mapBattleData được BattleManager truyền vào (BattleManager lấy MapBattsleData
    /// từ DataManager mỗi khi Turn Battle bắt đầu).
    /// </summary>
    public void SpawnWave(int waveIndex, MapBattsleData mapBattleData)
    {
        if (mapBattleData == null)
        {
            Debug.LogWarning("[BattleSpawnEnemy] MapBattleData chưa được gán!");
            return;
        }

        WaveData wave = mapBattleData.GetWave(waveIndex);
        if (wave == null)
        {
            Debug.LogWarning($"[BattleSpawnEnemy] Không tìm thấy wave {waveIndex} trong MapBattleData '{mapBattleData.name}'!");
            return;
        }

        SpawnEnemiesForWave(wave);
    }

    /// <summary>Xoá toàn bộ enemy đã spawn (dùng khi bắt đầu lại trận/màn chơi).</summary>
    public void ClearSpawnedEnemies()
    {
        foreach (var e in _spawnedEnemies)
            if (e != null) Destroy(e.gameObject);
        _spawnedEnemies.Clear();
    }

    // ─── Spawn Logic ────────────────────────────────────────

    private void SpawnEnemiesForWave(WaveData wave)
    {
        if (wave.Enemies == null) return;

        foreach (var entry in wave.Enemies)
        {
            if (entry == null || entry.EnemyData == null) continue;

            for (int i = 0; i < entry.Count; i++)
                SpawnEnemy(entry.EnemyData);
        }
    }

    private void SpawnEnemy(EnemyDuckData data)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[BattleSpawnEnemy] enemyPrefab chưa được gán!");
            return;
        }

        Transform point = GetSpawnPoint();
        Transform parent = enemyContainer != null ? enemyContainer : transform;

        EnemyDuck obj = Instantiate(enemyPrefab, point.position, point.rotation, parent);
        obj.Init(data, data.weaponData, 1, 1);

        _spawnedEnemies.Add(obj);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
