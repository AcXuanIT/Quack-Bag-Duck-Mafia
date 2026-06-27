using UnityEngine;

/// <summary>
/// Lưu trạng thái của một ô (cell) trong grid khi weapon được gắn vào.
/// </summary>
[System.Serializable]
public class WeaponGridCell
{
    [Tooltip("Vị trí ô trong grid (cột, hàng)")]
    public Vector2Int gridPosition;

    [Tooltip("True = ô này đang bị chiếm bởi weapon")]
    public bool isOccupied;
}

[System.Serializable]
public class WeaponEntry
{
    // ─── Identity ────────────────────────────────────────────
    [Header("=== Identity ===")]
    public int    ID;
    public string Name;

    // ─── Tier Sprites (dùng trong chiến đấu) ─────────────────
    [Header("=== Tier Sprites (tier 1-4) ===")]
    [Tooltip("Sprite tier 1 — icon mặc định trên UIGear")]
    public Sprite SpriteTier1;

    [Tooltip("Sprite tier 2")]
    public Sprite SpriteTier2;

    [Tooltip("Sprite tier 3")]
    public Sprite SpriteTier3;

    [Tooltip("Sprite tier 4")]
    public Sprite SpriteTier4;

    // ─── Shape / Grid ─────────────────────────────────────────
    [Header("=== Shape & Grid ===")]
    [Tooltip("Sprite khung hình dạng của weapon")]
    public Sprite ShapeSprite;

    [Tooltip("Các ô grid mà weapon chiếm khi gắn vào lưới")]
    public WeaponGridCell[] GridCells;

    // ─── Level ───────────────────────────────────────────────
    [Header("=== Level (1-5) ===")]
    [Range(1, 5)]
    public int Level = 1;

    [Tooltip("XP hiện tại")]
    public int XP;

    [Tooltip("XP cần để đạt level tiếp theo (0 nếu đã max)")]
    public int XPToNextLevel;

    // ─── Stats per Level ──────────────────────────────────────
    [Header("=== Stats per Level (index 0=Lv1 … 4=Lv5) ===")]
    [Tooltip("Damage tại mỗi Level — index 0=Lv1, 1=Lv2, 2=Lv3, 3=Lv4, 4=Lv5")]
    public float[] DamagePerLevel = new float[5];

    [Tooltip("HP tại mỗi Level — index 0=Lv1, 1=Lv2, 2=Lv3, 3=Lv4, 4=Lv5")]
    public float[] HPPerLevel = new float[5];

    // ─── Unlock Requirement ───────────────────────────────────
    [Header("=== Unlock ===")]
    [Tooltip("Level Player tối thiểu để mở khóa weapon này (0 = không yêu cầu)")]
    [Min(0)]
    public int LevelLock;

    // ─── Upgrade Cost ─────────────────────────────────────────
    [Header("=== Upgrade ===")]
    [Tooltip("Coin cần để nâng level khi XP đầy")]
    public int Coin;

    [Tooltip("Chưa mở khoá = true")]
    public bool IsLocked;

    // ─── Spawn ───────────────────────────────────────────────
    [Header("=== Spawn ===")]
    [Tooltip("Thời gian delay (giây) trước khi weapon spawn")]
    public float TimeDelay;

    // ─── Public API ───────────────────────────────────────────

    /// <summary>Damage tại level chỉ định (1-5).</summary>
    public float GetDamage(int level)
    {
        if (DamagePerLevel == null || DamagePerLevel.Length < 5) return 0f;
        return DamagePerLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    /// <summary>Damage tại Level hiện tại.</summary>
    public float GetCurrentDamage() => GetDamage(Level);

    public float GetNextLevelDamage()
    {
        if (Level >= 5) return GetCurrentDamage();
        return GetDamage(Level + 1);
    }

    /// <summary>HP tại level chỉ định (1-5).</summary>
    public float GetHP(int level)
    {
        if (HPPerLevel == null || HPPerLevel.Length < 5) return 0f;
        return HPPerLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    /// <summary>HP tại Level hiện tại.</summary>
    public float GetCurrentHP() => GetHP(Level);

    public float GetNextLevelHP()
    {
        if (Level >= 5) return GetCurrentHP();
        return GetHP(Level + 1);
    }

    /// <summary>Sprite theo tier (1-4), fallback về Tier1.</summary>
    public Sprite GetSpriteByTier(int tier)
    {
        switch (tier)
        {
            case 2:  return SpriteTier2 != null ? SpriteTier2 : SpriteTier1;
            case 3:  return SpriteTier3 != null ? SpriteTier3 : SpriteTier1;
            case 4:  return SpriteTier4 != null ? SpriteTier4 : SpriteTier1;
            default: return SpriteTier1;
        }
    }

    /// <summary>Icon mặc định trên UIGear (Tier 1).</summary>
    public Sprite GetUIIcon() => SpriteTier1;

    /// <summary>Kiểm tra ô grid có bị weapon chiếm không.</summary>
    public bool IsGridPositionOccupied(Vector2Int position)
    {
        if (GridCells == null) return false;
        foreach (var cell in GridCells)
            if (cell.isOccupied && cell.gridPosition == position)
                return true;
        return false;
    }

    /// <summary>Đánh dấu ô grid là đang bị chiếm.</summary>
    public void OccupyCell(Vector2Int position)
    {
        if (GridCells == null) return;
        foreach (var cell in GridCells)
            if (cell.gridPosition == position) { cell.isOccupied = true; return; }
    }

    /// <summary>Giải phóng tất cả ô grid.</summary>
    public void ReleaseAllCells()
    {
        if (GridCells == null) return;
        foreach (var cell in GridCells)
            cell.isOccupied = false;
    }
}

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponData : ScriptableObject
{
    public WeaponEntry[] Weapons;
}
