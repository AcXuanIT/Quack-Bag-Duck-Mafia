using UnityEngine;

[System.Serializable]
public class EnemyDuckData : BaseDuckData
{
    [Header("=== Enemy Combat ===")]
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;

    [Header("=== Enemy Weapon ===")]
    public WeaponDataAsset weaponAsset;

    [Header("=== Enemy Reward ===")]
    public int RewardCoin;

    public int RewardExp;

    [Header("=== Spawn ===")]
    public int SpawnWave;

    public WeaponEntry GetWeaponEntry()
    {
        return weaponAsset != null && weaponAsset.Entry != null ? weaponAsset.Entry.Clone() : null;
    }
}
