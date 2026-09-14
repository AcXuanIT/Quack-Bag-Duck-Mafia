using UnityEngine;
using UnityEngine.UI;

public interface IShopItem
{
    ShopItemData ShopData { get; }

    string DisplayName { get; }

    Sprite DisplayIcon { get; }

    int Rarity { get; }

    int SellPrice { get; }

    void Setup(ShopItemData itemData, BattleGridManager gridManager,
               RectTransform trash = null, Image trashImage = null);

    void Discard();
}
