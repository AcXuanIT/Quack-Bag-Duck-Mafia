using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa dữ liệu 1 entry trong Shop.
/// Ba loại: Grid (hình dạng ô), Gear (vũ khí), UnitDuck (nhân vật vịt).
///
/// - Grid     : data thuần (gridCells, icon, name...) nằm ngay trong asset này,
///              vì shape của 1 grid item không tồn tại ở đâu khác.
/// - Gear     : KHÔNG còn lưu data trùng lặp (icon/name/rarity/stats) nữa.
///              Toàn bộ data thật được lấy từ WeaponEntry trong WeaponData,
///              asset này chỉ giữ reference (weaponDatabase + weaponID).
/// - UnitDuck : dữ liệu vịt được lưu trực tiếp bằng MyDuckData (hệ Duck mới),
///              vì MyDuckData là 1 class thuần (không phải ScriptableObject
///              database như Weapon), nên không cần reference + ID như Gear.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "BatteShop/Item Data")]
public class ShopItemData : ScriptableObject
{
    public enum ItemType { Grid, Gear, UnitDuck }

    [Header("Identity")]
    public string itemName = "Item";
    public ItemType itemType = ItemType.Gear;

    [Header("Shop")]
    [Tooltip("Giá bán lại khi item bị huỷ/bán trong Shop")]
    public int sellPrice = 10;

    // ─── Grid-only data ─────────────────────────────────────
    // Chỉ dùng khi itemType == Grid. Đây là nguồn data duy nhất
    // cho 1 grid item (đã tổng hợp, không tách rời nữa).
    [Header("Grid Data (chỉ dùng khi itemType == Grid)")]
    public Sprite icon;                  // icon hiển thị trên card
    public Sprite backgroundSprite;      // nền card

    [Tooltip("Mảng ô chiếm trong battle grid, dạng [row,col] relative từ origin")]
    public Vector2Int[] gridCells;       // e.g. solo={(0,0)}, hor2={(0,0),(0,1)}, ...

    public int level  = 1;
    public int rarity = 0;               // 0=Common,1=Rare,2=Epic,3=Legendary,4=Mythic

    // ─── Gear reference ─────────────────────────────────────
    // Chỉ dùng khi itemType == Gear. Data thật nằm trong WeaponEntry.
    [Header("Gear Reference (chỉ dùng khi itemType == Gear)")]
    public WeaponData weaponDatabase;
    public int        weaponID;

    // ─── UnitDuck data (hệ Duck mới) ────────────────────────
    // Chỉ dùng khi itemType == UnitDuck. MyDuckData là data thuần,
    // được lưu trực tiếp trong asset này (không qua database + ID).
    [Header("UnitDuck Data (chỉ dùng khi itemType == UnitDuck)")]
    public MyDuckData unitDuckData;

    // ─── Resolvers ──────────────────────────────────────────

    /// <summary>Lấy WeaponEntry tương ứng (null nếu không phải Gear hoặc thiếu reference).</summary>
    public WeaponEntry GetWeaponEntry()
    {
        if (itemType != ItemType.Gear || weaponDatabase == null) return null;
        foreach (var w in weaponDatabase.Weapons)
            if (w != null && w.ID == weaponID) return w;
        return null;
    }

    /// <summary>Lấy MyDuckData tương ứng (null nếu không phải UnitDuck hoặc chưa gán data).</summary>
    public MyDuckData GetUnitDuckData()
    {
        if (itemType != ItemType.UnitDuck) return null;
        return unitDuckData;
    }
}
