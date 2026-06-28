using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa dữ liệu 1 loại item trong Shop.
/// Ba loại: Grid (hình dạng ô), Gear (vũ khí), UnitDuck (nhân vật vịt).
/// </summary>
[CreateAssetMenu(fileName = "ShopItemData", menuName = "BatteShop/Item Data")]
public class ShopItemData : ScriptableObject
{
    public enum ItemType { Grid, Gear, UnitDuck }

    [Header("Identity")]
    public string itemName = "Item";
    public ItemType itemType = ItemType.Gear;

    [Header("Visuals")]
    public Sprite icon;                  // icon hiển thị trên card
    public Sprite backgroundSprite;      // nền card (tuỳ loại)

    [Header("Grid Shape (chỉ dùng khi itemType == Grid)")]
    [Tooltip("Mảng ô chiếm trong battle grid, dạng [row,col] relative từ origin")]
    public Vector2Int[] gridCells;       // e.g. solo={(0,0)}, hor2={(0,0),(0,1)}, ...

    [Header("Stats")]
    public int level  = 1;
    public int rarity = 0;               // 0=Common,1=Rare,2=Epic,3=Legendary,4=Mythic
    public int sellPrice = 10;
}
