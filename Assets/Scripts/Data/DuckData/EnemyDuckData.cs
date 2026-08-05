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

    [Header("=== Enemy Weapon ===")]
    [Tooltip("Data vũ khí của Enemy này. Lưu trực tiếp (không qua WeaponData database + ID) " +
             "để Level/Stats của weapon Enemy hoàn toàn độc lập với hệ thống " +
             "nâng cấp weapon của Player (ShopItemData.weaponDatabase/weaponID).")]
    public WeaponEntry weaponData;

    [Header("=== Enemy Reward ===")]
    [Tooltip("Coin nhận được khi tiêu diệt con vịt này")]
    public int RewardCoin;

    [Tooltip("EXP nhận được khi tiêu diệt con vịt này")]
    public int RewardExp;

    [Header("=== Spawn ===")]
    [Tooltip("Wave/level mà con vịt này bắt đầu xuất hiện")]
    public int SpawnWave;
}
