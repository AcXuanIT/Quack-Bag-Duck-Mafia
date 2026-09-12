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
    [Tooltip("Kéo 1 WeaponDataAsset vào đây để gán weapon cho Enemy này — KHÔNG cần copy tay " +
             "từng field như trước. Toàn bộ Level/Stats hiển thị ở đây chỉ là DỮ LIỆU GỐC " +
             "(tham chiếu chung với WeaponDataAsset); khi spawn, code PHẢI gọi GetWeaponEntry() " +
             "(Clone() một bản riêng) để Level/Stats runtime của Enemy hoàn toàn độc lập với hệ " +
             "thống nâng cấp weapon của Player (WeaponManager mutate trực tiếp WeaponEntry khi " +
             "lên cấp) — không dùng trực tiếp weaponAsset.Entry.")]
    public WeaponDataAsset weaponAsset;

    [Header("=== Enemy Reward ===")]
    [Tooltip("Coin nhận được khi tiêu diệt con vịt này")]
    public int RewardCoin;

    [Tooltip("EXP nhận được khi tiêu diệt con vịt này")]
    public int RewardExp;

    [Header("=== Spawn ===")]
    [Tooltip("Wave/level mà con vịt này bắt đầu xuất hiện")]
    public int SpawnWave;

    /// <summary>
    /// Trả về 1 bản copy độc lập (Clone()) của weapon gán qua weaponAsset, an toàn để dùng làm
    /// weaponData runtime của EnemyDuck (không chia sẻ chung instance với WeaponManager/Player).
    /// Null nếu chưa gán weaponAsset.
    /// </summary>
    public WeaponEntry GetWeaponEntry()
    {
        return weaponAsset != null && weaponAsset.Entry != null ? weaponAsset.Entry.Clone() : null;
    }
}
