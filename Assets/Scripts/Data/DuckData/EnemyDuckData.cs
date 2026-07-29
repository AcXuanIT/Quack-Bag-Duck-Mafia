using UnityEngine;

/// <summary>
/// Dữ liệu 1 con vịt địch (Enemy).
/// Kế thừa từ BaseDuckData, bổ sung các thông số riêng cho AI/địch.
/// </summary>
[System.Serializable]
public class EnemyDuckData : BaseDuckData
{
    [Header("=== Enemy Combat ===")]
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;

    [Header("=== Enemy Reward ===")]
    [Tooltip("Coin nhận được khi tiêu diệt con vịt này")]
    public int RewardCoin;

    [Tooltip("EXP nhận được khi tiêu diệt con vịt này")]
    public int RewardExp;

    [Header("=== Spawn ===")]
    [Tooltip("Wave/level mà con vịt này bắt đầu xuất hiện")]
    public int SpawnWave;
}
