using UnityEngine;

/// <summary>
/// Dữ liệu 1 con vịt của người chơi (Player-owned Duck).
/// Kế thừa từ BaseDuckData, bổ sung các thông số liên quan tới việc
/// sở hữu/nâng cấp bởi người chơi.
/// </summary>
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
    [Tooltip("Level Player tối thiểu để mở khóa con vịt này (0 = không yêu cầu)")]
    [Min(0)]
    public int LevelLock;

    [Tooltip("Chưa mở khoá = true")]
    public bool IsLocked;
}
