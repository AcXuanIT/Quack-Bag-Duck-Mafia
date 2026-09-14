using UnityEngine;


[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Data của loại enemy (EnemyDuckDataAsset)")]
    public EnemyDuckDataAsset EnemyData;

    [Tooltip("Số lượng enemy loại này cần spawn trong wave")]
    [Min(0)]
    public int Count = 1;
}

[System.Serializable]
public class WaveData
{
    [Header("=== Identity ===")]
    public int WaveIndex = 1;

    [Header("=== Enemies ===")]
    public EnemySpawnEntry[] Enemies;

    public int GetTotalEnemyCount()
    {
        if (Enemies == null) return 0;
        int total = 0;
        foreach (var e in Enemies)
            total += Mathf.Max(0, e.Count);
        return total;
    }
}

[CreateAssetMenu(fileName = "MapBattleData", menuName = "Game/Map Battle Data")]
public class MapBattsleData : ScriptableObject
{
    [Header("=== Waves ===")]
    public WaveData[] Waves;

    public int WaveCount => Waves != null ? Waves.Length : 0;

    public WaveData GetWave(int waveIndex)
    {
        if (Waves == null) return null;
        foreach (var w in Waves)
            if (w != null && w.WaveIndex == waveIndex) return w;
        return null;
    }
    public int GetTotalEnemyCount()
    {
        if (Waves == null) return 0;
        int total = 0;
        foreach (var w in Waves)
            total += w.GetTotalEnemyCount();
        return total;
    }
}
