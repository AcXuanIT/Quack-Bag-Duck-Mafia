using UnityEngine;


[CreateAssetMenu(fileName = "ShopItemData", menuName = "BatteShop/Grid Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Item";

    [Header("Shop")]
    public int sellPrice = 10;

    [Header("Grid Data")]
    public Sprite icon;                 
    public Sprite backgroundSprite;    
    public Sprite fill;

    public Vector2Int[] gridCells;       

    public int level  = 1;
    public int rarity = 0;               
}
