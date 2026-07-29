using UnityEngine;

/// <summary>
/// Data lưu số lượng từng loại Enemy cần spawn trong 1 Wave.
/// </summary>
[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Data của loại enemy (EnemyDuckData)")]
    public EnemyDuckData EnemyData;

    [Tooltip("Số lượng enemy loại này cần spawn trong wave")]
    [Min(0)]
    public int Count = 1;
}

/// <summary>
/// Dữ liệu 1 Wave (đợt spawn enemy) trong màn chơi.
/// Mỗi Turn Battle sẽ tương ứng với 1 Wave (theo currentWavesIndex trong BattleManager).
/// </summary>
[System.Serializable]
public class WaveData
{
    [Header("=== Identity ===")]
    [Tooltip("Số thứ tự Wave (thường bắt đầu từ 1, khớp với currentWavesIndex trong BattleManager)")]
    public int WaveIndex = 1;

    [Header("=== Enemies ===")]
    [Tooltip("Danh sách các loại enemy + số lượng xuất hiện trong wave này")]
    public EnemySpawnEntry[] Enemies;

    /// <summary>Tổng số enemy (tất cả loại cộng lại) trong wave này.</summary>
    public int GetTotalEnemyCount()
    {
        if (Enemies == null) return 0;
        int total = 0;
        foreach (var e in Enemies)
            total += Mathf.Max(0, e.Count);
        return total;
    }
}

/// <summary>
/// ScriptableObject lưu dữ liệu enemy của 1 màn chơi (level):
/// gồm danh sách các Wave, mỗi Wave chứa danh sách loại enemy + số lượng.
///
/// Việc quản lý nhiều Level (chọn/khởi tạo MapBattleData nào cho level nào)
/// sẽ do các DataBattle khác đảm nhiệm — chưa xử lý ở đây.
/// </summary>
[CreateAssetMenu(fileName = "MapBattleData", menuName = "Game/Map Battle Data")]
public class MapBattleData : ScriptableObject
{
    [Header("=== Waves ===")]
    [Tooltip("Danh sách các Wave trong màn chơi này")]
    public WaveData[] Waves;

    /// <summary>Tổng số wave trong màn chơi.</summary>
    public int WaveCount => Waves != null ? Waves.Length : 0;

    /// <summary>Lấy Wave theo WaveIndex (giá trị field, không phải vị trí mảng). Null nếu không tìm thấy.</summary>
    public WaveData GetWave(int waveIndex)
    {
        if (Waves == null) return null;
        foreach (var w in Waves)
            if (w != null && w.WaveIndex == waveIndex) return w;
        return null;
    }

    /// <summary>Tổng số enemy của toàn bộ màn chơi (cộng tất cả wave).</summary>
    public int GetTotalEnemyCount()
    {
        if (Waves == null) return 0;
        int total = 0;
        foreach (var w in Waves)
            total += w.GetTotalEnemyCount();
        return total;
    }
}
