using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn Enemy theo Wave, được BattleManager gọi trực tiếp khi bắt đầu Turn Battle
/// (truyền vào currentWavesIndex).
/// mapBattleData chứa dữ liệu Wave/Enemy của level hiện tại (level nào sẽ được gán
/// bởi hệ thống quản lý level khác — chưa xử lý ở đây).
/// </summary>
public class BattleSpawnEnemy : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Dữ liệu Wave/Enemy của level hiện tại")]
    [SerializeField] private MapBattleData mapBattleData;

    [Header("=== Spawn ===")]
    [Tooltip("Prefab EnemyObject dùng để Instantiate")]
    [SerializeField] private EnemyObject enemyPrefab;

    [Tooltip("Các điểm spawn enemy. Nếu để trống sẽ spawn tại vị trí của chính BattleSpawnEnemy")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Container chứa các enemy được spawn ra (tuỳ chọn)")]
    [SerializeField] private Transform enemyContainer;

    private readonly List<EnemyObject> _spawnedEnemies = new List<EnemyObject>();

    // ─── Public API ─────────────────────────────────────────

    /// <summary>Gán dữ liệu level hiện tại (gọi khi hệ thống quản lý level đổi map).</summary>
    public void SetMapBattleData(MapBattleData data)
    {
        mapBattleData = data;
    }

    /// <summary>
    /// Spawn toàn bộ enemy thuộc Wave có WaveIndex = waveIndex.
    /// Được BattleManager gọi khi Turn Battle bắt đầu.
    /// </summary>
    public void SpawnWave(int waveIndex)
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

        EnemyObject obj = Instantiate(enemyPrefab, point.position, point.rotation, parent);
        obj.Init(data);

        _spawnedEnemies.Add(obj);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
