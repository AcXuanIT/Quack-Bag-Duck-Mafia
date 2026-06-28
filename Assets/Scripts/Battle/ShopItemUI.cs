using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào prefab item trong Component (Shop).
/// Hiển thị icon, tên, loại item theo ShopItemData.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Image bgImage;
    [SerializeField] public Image iconImage;
    [SerializeField] public Image frameImage;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI typeLabel;

    [Header("Data")]
    [SerializeField] public ShopItemData data;

    // Rarity frame sprites (index = rarity value)
    [Header("Rarity Frames")]
    [SerializeField] private Sprite[] rarityFrames;   // common, rare, epic, legendary, mythic

    // ─────────────────────────────────────────────
    public void Setup(ShopItemData itemData)
    {
        data = itemData;
        if (data == null) return;

        if (iconImage  != null) { iconImage.sprite = data.icon; iconImage.enabled = data.icon != null; }
        if (bgImage    != null && data.backgroundSprite != null) bgImage.sprite = data.backgroundSprite;
        if (nameText   != null) nameText.text = data.itemName;
        if (typeLabel  != null) typeLabel.text = data.itemType.ToString();

        // Rarity frame
        if (frameImage != null && rarityFrames != null && data.rarity < rarityFrames.Length)
            frameImage.sprite = rarityFrames[data.rarity];
    }

    public ShopItemData.ItemType ItemType => data != null ? data.itemType : ShopItemData.ItemType.Gear;
}
