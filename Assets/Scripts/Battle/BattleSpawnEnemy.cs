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
    [Tooltip("Prefab EnemyObject dùng để Instantiate")]
    [SerializeField] private EnemyDuck enemyPrefab;

    [Tooltip("Các điểm spawn enemy. Nếu để trống sẽ spawn tại vị trí của chính BattleSpawnEnemy")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Container chứa các enemy được spawn ra (tuỳ chọn)")]
    [SerializeField] private Transform enemyContainer;

    [Header("=== Timing ===")]
    [Tooltip("Thời gian delay (giây) giữa mỗi lần spawn 1 enemy trong Wave")]
    [SerializeField] private float spawnDelay;

    private readonly List<EnemyDuck> _spawnedEnemies = new List<EnemyDuck>();
    private Coroutine _spawnRoutine;

    // True kể từ khi SpawnWave() được gọi cho Wave hiện tại, dùng để phân biệt "chưa gọi
    // SpawnWave() lần nào" (đều cho _spawnRoutine == null) với "đã spawn xong Wave".
    private bool _waveSpawnStarted;

    // ─── Public API ─────────────────────────────────────────

    /// <summary>
    /// Spawn toàn bộ enemy thuộc Wave có WaveIndex = waveIndex, dữ liệu lấy từ
    /// mapBattleData được BattleManager truyền vào (BattleManager lấy MapBattsleData
    /// từ DataManager mỗi khi Turn Battle bắt đầu).
    /// </summary>
    public void SpawnWave(int waveIndex, MapBattsleData mapBattleData)
    {
        if (mapBattleData == null) return;

        WaveData wave = mapBattleData.GetWave(waveIndex);

        if (wave == null) return;

        if (_spawnRoutine != null)
            StopCoroutine(_spawnRoutine);

        // Wave mới -> danh sách enemy đang quản lý cũng reset theo, để IsWaveCleared()
        // chỉ xét đúng enemy của Wave hiện tại (không tính dồn enemy của các turn trước).
        _spawnedEnemies.Clear();
        _waveSpawnStarted = true;

        _spawnRoutine = StartCoroutine(SpawnEnemiesForWave(wave));
    }

    /// <summary>Xoá toàn bộ enemy đã spawn (dùng khi bắt đầu lại trận/màn chơi).</summary>
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

    /// <summary>
    /// True khi Wave hiện tại đã "sạch": đã spawn xong toàn bộ enemy của Wave (coroutine
    /// SpawnEnemiesForWave chạy xong, không còn nằm trong hàng đợi) VÀ toàn bộ enemy đã
    /// spawn (quản lý trong _spawnedEnemies) đều đã chết (Duck.IsDead) hoặc đã bị huỷ (null).
    ///
    /// BattleManager dùng hàm này (poll mỗi Update() khi đang TurnBattle) để tự động kết
    /// thúc Turn Battle sớm ngay khi dọn sạch quân địch, không cần đợi hết thời gian turn.
    /// </summary>
    public bool IsWaveCleared()
    {
        if (!_waveSpawnStarted) return false;      // SpawnWave() chưa được gọi cho Wave này
        if (_spawnRoutine != null) return false;   // vẫn còn đang spawn dở trong coroutine

        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && !enemy.IsDead)
                return false; // còn ít nhất 1 enemy sống
        }

        return true; // Wave rỗng (0 enemy) hoặc toàn bộ enemy đã chết
    }

    // ─── Spawn Logic ────────────────────────────────────────

    /// <summary>
    /// Spawn lần lượt từng enemy trong Wave, mỗi lần spawn cách nhau spawnDelay giây.
    /// </summary>
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

        // GetWeaponEntry() trả về 1 bản Clone() độc lập của weapon gán qua data.weaponAsset —
        // KHÔNG dùng trực tiếp weaponAsset.Entry, để tránh Enemy vô tình chia sẻ chung instance
        // WeaponEntry với hệ thống nâng cấp vũ khí của Player (WeaponManager).
        obj.Init(data, data.GetWeaponEntry(), 1, 1);

        _spawnedEnemies.Add(obj);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
