using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface chung cho mọi loại item UI trong Shop:
/// GridShopItemUI (Grid), GearItemUI (Gear), UnitPlayerItemUI (UnitDuck).
///
/// Mỗi loại item tự lấy data hiển thị (icon, tên, rarity, sellPrice...) từ
/// nguồn dữ liệu tương ứng:
///   - Grid          → lấy thẳng từ ShopItemData (gridCells, icon, name...)
///   - Gear          → lấy từ WeaponEntry (qua ShopItemData.GetWeaponEntry())
///   - UnitDuck      → lấy từ UnitEntry   (qua ShopItemData.GetUnitEntry())
/// </summary>
public interface IShopItem
{
    /// <summary>ShopItemData gốc (chứa itemType + reference tới WeaponData/UnitData).</summary>
    ShopItemData ShopData { get; }

    /// <summary>Tên hiển thị trên card.</summary>
    string DisplayName { get; }

    /// <summary>Icon hiển thị trên card.</summary>
    Sprite DisplayIcon { get; }

    /// <summary>Rarity dùng để chọn khung viền (frame).</summary>
    int Rarity { get; }

    /// <summary>Giá bán lại (sellPrice) khi item bị huỷ/bán.</summary>
    int SellPrice { get; }

    /// <summary>
    /// Khởi tạo UI từ ShopItemData. gridManager/trash/trashImage chỉ thật sự
    /// cần với GridShopItemUI, các loại khác có thể bỏ qua các tham số dư.
    /// </summary>
    void Setup(ShopItemData itemData, BattleGridManager gridManager,
               RectTransform trash = null, Image trashImage = null);

    /// <summary>Huỷ item (bán/bỏ vào trash) khỏi Shop.</summary>
    void Discard();
}
