using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI của một UnitDuck (nhân vật vịt) trong Shop.
/// KHÔNG còn dùng ShopItemData — MyDuckData là nguồn dữ liệu DUY NHẤT cho UnitDuck.
/// Setup() nhận thẳng MyDuckData (do ShopBatteManager lấy qua DataManager.GetMyDuckData()).
///
/// SHAPE / SIZING:
///   - TẤT CẢ Unit dùng CHUNG 1 shape cố định: 1 cột (width) x 2 hàng (height) —
///     không phụ thuộc data riêng của từng Unit (khác với Gear, mỗi Weapon 1 shape riêng).
///   - Kích thước thật áp qua ShopItemSizing (kế thừa từ TierShopItemUI.ApplyShapeSize),
///     dùng chung CellSize/CellGap với GridItem/GearItem để 1 ô luôn cùng kích thước vật lý.
///
/// ĐẶT (PLACE) LÊN GRID:
///   - Ngoài hành vi unlock 1 ô Locked (kế thừa từ TierShopItemUI, shape 1 ô), Unit còn
///     có thể được kéo đè lên các ô ĐÃ Unlock (UnlockedEmpty) để thực sự đặt Unit vào
///     bàn cờ theo ĐÚNG shape 1x2 cố định của nó — xem CanPlaceShapeAt()/PlaceShapeAt(),
///     dùng BattleGridManager.CanPlaceUnit()/PlaceUnit() (đánh dấu BattleGridCell.OccupyingUnit).
/// </summary>
public class UnitPlayerItemUI : TierShopItemUI
{
    /// <summary>Shape cố định 1w x 2h (offset dạng [row,col]) dùng chung cho MỌI Unit.</summary>
    private static readonly Vector2Int[] UnitShapeCells =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
    };

    private MyDuckData _unit;
    public  MyDuckData Unit => _unit;

    // ─── Display ────────────────────────────────────────────
    public override string DisplayName => _unit != null ? _unit.Name : string.Empty;
    public override Sprite DisplayIcon => _unit != null ? _unit.GetDefaultIcon() : null;

    /// <summary>HP hiện tại của Duck này (chỉ số gốc từ MyDuckData).</summary>
    public float CurrentHP => _unit != null ? _unit.BaseHP : 0f;

    /// <summary>Setup trực tiếp từ MyDuckData — nguồn dữ liệu duy nhất cho UnitDuck.</summary>
    public void Setup(MyDuckData unit, BattleGridManager gridManager, RectTransform trash = null, Image trashImg = null)
    {
        _unit = unit;
        InitCommon(gridManager, trash, trashImg);

        if (_unit == null)
            Debug.LogWarning("[UnitPlayerItemUI] Setup() nhan MyDuckData NULL!");

        ApplyShapeSize(UnitShapeCells);
        RefreshVisual();
    }

    // ─── Place lên grid (đè lên các ô đã Unlock) ─────────────

    /// <summary>Unit có thể đặt tại anchor nếu toàn bộ shape 1x2 cố định của nó đang là UnlockedEmpty.</summary>
    protected override bool CanPlaceShapeAt(BattleGridCell anchorCell)
    {
        if (anchorCell == null || _gridManager == null) return false;
        return _gridManager.CanPlaceUnit(anchorCell.Row, anchorCell.Col, UnitShapeCells);
    }

    /// <summary>Đặt Unit lên grid tại anchor — các ô trong shape 1x2 chuyển UnlockedFull, OccupyingUnit = _unit.</summary>
    protected override void PlaceShapeAt(BattleGridCell anchorCell)
    {
        _gridManager.PlaceUnit(anchorCell.Row, anchorCell.Col, _unit, UnitShapeCells);
    }

    protected override void RefreshVisual()
    {
        Sprite icon = _unit != null ? _unit.GetSprite(CurrentTier) : null;
        if (icon == null && _unit != null) icon = _unit.GetDefaultIcon();

        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (nameText  != null) nameText.text  = DisplayName;
        if (levelText != null) levelText.text = "T" + CurrentTier;

        ApplyTierColor();
    }

    /// <summary>2 UnitPlayerItemUI được coi là cùng loại nếu cùng MyDuckData.ID.</summary>
    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as UnitPlayerItemUI;
        return o != null && o._unit != null && _unit != null && o._unit.ID == _unit.ID;
    }
}
