using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa dữ liệu 1 GridItem trong Shop (hình dạng ô để unlock Battle Grid).
///
/// LƯU Ý CẤU TRÚC MỚI: ShopItemData giờ CHỈ dùng cho Grid item.
/// - Gear   : lấy thẳng từ WeaponData (Assets/Resources/Data/WeaponDatabase) qua DataManager,
///            không còn ShopItemData wrapper cho Gear nữa.
/// - UnitDuck: lấy thẳng từ MyDuckData (Assets/Data/MyDuck) qua DataManager,
///            không còn ShopItemData wrapper cho UnitDuck nữa.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "BatteShop/Grid Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Item";

    [Header("Shop")]
    [Tooltip("Giá bán lại khi item bị huỷ/bán trong Shop")]
    public int sellPrice = 10;

    [Header("Grid Data")]
    public Sprite icon;                  // icon hiển thị trên card
    public Sprite backgroundSprite;      // nền card

    [Tooltip("Mảng ô chiếm trong battle grid, dạng [row,col] relative từ origin")]
    public Vector2Int[] gridCells;       // e.g. solo={(0,0)}, hor2={(0,0),(0,1)}, ...

    public int level  = 1;
    public int rarity = 0;               // 0=Common,1=Rare,2=Epic,3=Legendary,4=Mythic
}
