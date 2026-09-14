using UnityEngine;


[System.Serializable]
public class MyDuckData : BaseDuckData
{
    [Header("=== Level / Upgrade ===")]
    [Range(1, 5)]
    public int Level = 1;

    public int XP;
    public int XPToNextLevel;

    [Tooltip("Coin cần để nâng level khi XP đầy")]
    public int UpgradeCoin;

    [Header("=== Unlock ===")]
    [Min(0)]
    public int LevelLock;

    [Tooltip("Chưa mở khoá = true")]
    public bool IsLocked;
}
