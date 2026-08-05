using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI của một Gear (vũ khí) trong Shop.
/// KHÔNG còn dùng ShopItemData — WeaponData là nguồn dữ liệu DUY NHẤT cho Gear.
/// Setup() nhận thẳng WeaponEntry (do ShopBatteManager lấy qua DataManager.GetWeaponEntry()).
/// Toàn bộ ID/Name/Sprite/HP/Damage đều đọc trực tiếp từ WeaponEntry.
///
/// SHAPE / SIZING:
///   - Mỗi Weapon có shape riêng qua WeaponEntry.GridCells (nguồn dữ liệu tự thêm ở Data Weapon).
///   - Quy đổi GridCells (WeaponGridCell[]) -> Vector2Int[] rồi áp qua ShopItemSizing (kế thừa từ
///     TierShopItemUI.ApplyShapeSize) để đảm bảo 1 ô luôn cùng kích thước vật lý với GridItem/UnitItem.
///   - Nếu Weapon chưa có GridCells (rỗng/null), fallback về shape 1 ô [0,0].
///
/// ĐẶT (PLACE) LÊN GRID:
///   - Ngoài hành vi unlock 1 ô Locked (kế thừa từ TierShopItemUI, shape 1 ô), Gear còn
///     có thể được kéo đè lên các ô ĐÃ Unlock (UnlockedEmpty) để thực sự "gắn" weapon
///     vào bàn cờ theo ĐÚNG shape nhiều ô của nó — xem CanPlaceShapeAt()/PlaceShapeAt(),
///     dùng BattleGridManager.CanPlaceGear()/PlaceGear() (đã tự gọi weapon.OccupyCell()).
/// </summary>
public class GearItemUI : TierShopItemUI
{
    [Header("Rarity Frames (chỉ riêng Gear — index theo Tier)")]
    [SerializeField] private Image    frameImage;
    [SerializeField] private Sprite[] tierFrames; // index 0=Tier1 .. 3=Tier4

    private WeaponEntry _weapon;
    public  WeaponEntry Weapon => _weapon;

    // ─── Display ────────────────────────────────────────────
    public override string DisplayName => _weapon != null ? _weapon.Name : string.Empty;
    public override Sprite DisplayIcon => _weapon != null ? _weapon.GetUIIcon() : null;

    /// <summary>Damage/HP hiện tại của Gear này, dùng khi merge lên Tier cao hơn để tính hiệu ứng trong Battle.</summary>
    public float CurrentDamage => _weapon != null ? _weapon.GetCurrentDamage() : 0f;
    public float CurrentHP     => _weapon != null ? _weapon.GetCurrentHP()     : 0f;
    public float AttackRange   => _weapon != null ? _weapon.GetAttackRange()   : 0f;

    /// <summary>Setup trực tiếp từ WeaponEntry — nguồn dữ liệu duy nhất cho Gear (WeaponData).</summary>
    public void Setup(WeaponEntry weapon, BattleGridManager gridManager, RectTransform trash = null, Image trashImg = null)
    {
        _weapon = weapon;
        InitCommon(gridManager, trash, trashImg);

        if (_weapon == null)
            Debug.LogWarning("[GearItemUI] Setup() nhan WeaponEntry NULL!");

        ApplyShapeSize(GetShapeCells());
        RefreshVisual();
    }

    /// <summary>
    /// Quy đổi WeaponEntry.GridCells (WeaponGridCell[], có gridPosition dạng [row,col])
    /// sang Vector2Int[] dùng chung cho ShopItemSizing. Fallback shape 1 ô nếu weapon
    /// chưa khai báo GridCells.
    /// </summary>
    private Vector2Int[] GetShapeCells()
    {
        var cells = _weapon != null ? _weapon.GridCells : null;
        if (cells == null || cells.Length == 0)
            return new Vector2Int[] { Vector2Int.zero };

        var result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            result[i] = cells[i].gridPosition;
        return result;
    }

    // ─── Place lên grid (đè lên các ô đã Unlock) ─────────────

    /// <summary>Gear có thể đặt tại anchor nếu toàn bộ shape (GridCells) của nó đang là UnlockedEmpty.</summary>
    protected override bool CanPlaceShapeAt(BattleGridCell anchorCell)
    {
        if (anchorCell == null || _gridManager == null || _weapon == null) return false;
        return _gridManager.CanPlaceGear(anchorCell.Row, anchorCell.Col, _weapon);
    }

    /// <summary>Đặt Gear lên grid tại anchor — các ô trong shape chuyển UnlockedFull, OccupyingWeapon = _weapon.</summary>
    protected override void PlaceShapeAt(BattleGridCell anchorCell)
    {
        _gridManager.PlaceGear(anchorCell.Row, anchorCell.Col, _weapon);
    }

    protected override void RefreshVisual()
    {
        Sprite icon = _weapon != null ? _weapon.GetSpriteByTier(CurrentTier) : null;

        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (nameText  != null) nameText.text  = DisplayName;
        if (levelText != null) levelText.text = "T" + CurrentTier;

        if (frameImage != null && tierFrames != null)
        {
            int idx = Mathf.Clamp(CurrentTier - 1, 0, tierFrames.Length - 1);
            if (idx < tierFrames.Length) frameImage.sprite = tierFrames[idx];
        }

        ApplyTierColor();
    }

    /// <summary>2 GearItemUI được coi là cùng loại nếu trỏ chung 1 WeaponEntry.ID.</summary>
    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as GearItemUI;
        return o != null && o._weapon != null && _weapon != null && o._weapon.ID == _weapon.ID;
    }
}
