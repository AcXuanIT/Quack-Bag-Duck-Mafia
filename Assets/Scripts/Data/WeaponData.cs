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
    [Header("=== Identity ===")]
    public int    ID;
    public string Name;

    // ─── Tier sprites (dùng trong chiến đấu) ─────────────────
    // Không liên quan đến Level upgrade của weapon.
    // Tier thể hiện độ hiếm / sức mạnh cơ bản của vũ khí.
    // Tier1 là mặc định, cũng là sprite hiển thị trên UI Gear.
    [Header("=== Tier Sprites (chiến đấu, tier 1-4) ===")]
    [Tooltip("Sprite dùng khi tier 1 — cũng là icon mặc định trên UIGear")]
    public Sprite SpriteTier1;

    [Tooltip("Sprite dùng khi tier 2")]
    public Sprite SpriteTier2;

    [Tooltip("Sprite dùng khi tier 3")]
    public Sprite SpriteTier3;

    [Tooltip("Sprite dùng khi tier 4")]
    public Sprite SpriteTier4;

    // ─── Shape / Grid ─────────────────────────────────────────
    [Header("=== Shape (UI Frame) ===")]
    [Tooltip("Sprite khung hình dạng của weapon (shape frame)")]
    public Sprite ShapeSprite;

    [Tooltip("Danh sách các ô grid mà weapon này chiếm khi gắn vào lưới")]
    public WeaponGridCell[] GridCells;

    // ─── Level (1-5, upgrade trong UIGear) ───────────────────
    [Header("=== Level (upgrade 1-5) ===")]
    [Range(1, 5)]
    public int Level = 1;

    [Tooltip("XP hiện tại (số quái đã tiêu diệt bằng weapon)")]
    public int XP;

    [Tooltip("XP cần để đạt level tiếp theo")]
    public int XPToNextLevel;

    // ─── Stats ───────────────────────────────────────────────
    [Header("=== Stats ===")]
    public float Damage;
    public float HP;

    [Tooltip("Coin cần để nâng level khi XP đầy")]
    public int Coin;

    public bool IsLocked;   // true = chưa mở khoá

    // ─── Spawn ───────────────────────────────────────────────
    [Header("=== Spawn ===")]
    [Tooltip("Thời gian delay (giây) trước khi weapon được spawn vào scene")]
    public float TimeDelay;

    // ─── Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Trả về sprite theo tier (1-4).
    /// Dùng trong chiến đấu để hiển thị sprite tương ứng với tier của weapon.
    /// </summary>
    public Sprite GetSpriteByTier(int tier)
    {
        switch (tier)
        {
            case 2:  return SpriteTier2 != null ? SpriteTier2 : SpriteTier1;
            case 3:  return SpriteTier3 != null ? SpriteTier3 : SpriteTier1;
            case 4:  return SpriteTier4 != null ? SpriteTier4 : SpriteTier1;
            default: return SpriteTier1;  // tier 1 hoặc fallback
        }
    }

    /// <summary>
    /// Sprite mặc định hiển thị trên UIGear (luôn là Tier 1).
    /// </summary>
    public Sprite GetUIIcon() => SpriteTier1;

    /// <summary>Kiểm tra vị trí grid có bị weapon chiếm không.</summary>
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

    /// <summary>Giải phóng tất cả các ô grid weapon đang chiếm.</summary>
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
