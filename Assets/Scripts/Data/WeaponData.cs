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
    public int ID;
    public string Name;

    [Header("=== Sprites (Level 1 → 4) ===")]
    [Tooltip("Icon hiển thị ở Level 1")]
    public Sprite IconLevel1;

    [Tooltip("Icon hiển thị ở Level 2")]
    public Sprite IconLevel2;

    [Tooltip("Icon hiển thị ở Level 3")]
    public Sprite IconLevel3;

    [Tooltip("Icon hiển thị ở Level 4")]
    public Sprite IconLevel4;

    [Header("=== Shape (UI Frame) ===")]
    [Tooltip("Sprite khung hình dạng của weapon hiển thị bên ngoài UI (shape frame)")]
    public Sprite ShapeSprite;

    [Tooltip("Danh sách các ô grid mà weapon này chiếm khi gắn vào lưới")]
    public WeaponGridCell[] GridCells;

    [Header("=== Stats ===")]
    [Range(1, 5)] public int Level = 1;
    public int XP;              // số quái giết được bằng vũ khí này
    public int XPToNextLevel;   // XP cần để lên level tiếp theo
    public float Damage;
    public float HP;
    public int Coin;            // coin cần để nâng level khi XP đầy
    public bool IsLocked;       // true = chưa mở khóa

    [Header("=== Spawn ===")]
    [Tooltip("Thời gian delay (giây) trước khi weapon được spawn vào scene")]
    public float TimeDelay;

    /// <summary>
    /// Trả về sprite icon tương ứng với level hiện tại.
    /// </summary>
    public Sprite GetCurrentIcon()
    {
        return Level switch
        {
            1 => IconLevel1,
            2 => IconLevel2,
            3 => IconLevel3,
            4 => IconLevel4,
            _ => IconLevel1
        };
    }

    /// <summary>
    /// Kiểm tra xem một vị trí grid có bị weapon này chiếm hay không.
    /// </summary>
    public bool IsGridPositionOccupied(Vector2Int position)
    {
        if (GridCells == null) return false;
        foreach (var cell in GridCells)
        {
            if (cell.isOccupied && cell.gridPosition == position)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Đánh dấu một ô grid là đang bị chiếm.
    /// </summary>
    public void OccupyCell(Vector2Int position)
    {
        if (GridCells == null) return;
        foreach (var cell in GridCells)
        {
            if (cell.gridPosition == position)
            {
                cell.isOccupied = true;
                return;
            }
        }
    }

    /// <summary>
    /// Giải phóng tất cả các ô grid mà weapon này đang chiếm.
    /// </summary>
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
