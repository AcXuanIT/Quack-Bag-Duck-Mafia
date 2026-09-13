using UnityEngine;
using UnityEngine.EventSystems;
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
///     dùng chung CellSize/CellGap (thực chất là CellWidth/CellHeight thật của
///     BattleGridManager) với GridItem/GearItem để 1 ô luôn cùng kích thước vật lý.
///
/// ĐẶT (PLACE) LÊN GRID — override TOÀN BỘ luồng kéo-thả của TierShopItemUI (không
/// dùng chung step "Unlock 1 ô Locked" hay auto-Discard() sau khi đặt của lớp cha),
/// vì Unit có luật RIÊNG khác Gear/GridItem:
///   - Chỉ đặt được vào ô ĐANG Unlocked (UnlockedEmpty) — KHÔNG unlock ô Locked như
///     Gear/GridItem vẫn làm.
///   - Khi hover trong lúc kéo: tô màu xanh/đỏ lên ĐÚNG 2 ô sẽ chiếm để báo hợp lệ hay
///     không (SetUnlockedHighlight — giữ nguyên sprite Unlocked, KHÔNG hiện lại các ô
///     Locked ẩn như GridItem vẫn làm khi kéo).
///   - Khi thả hợp lệ: gọi BattleGridManager.PlaceUnit() để đánh dấu đúng 2 ô đã chiếm,
///     ĐỒNG THỜI di chuyển (reparent) chính UnitItem này vào đúng vị trí 2 ô đó —
///     KHÔNG Destroy() nó như GridItem/Gear vẫn làm. Item vẫn giữ nguyên các component
///     kéo-thả nên có thể được nhấc lên lại bất cứ lúc nào.
///   - Khi nhấc 1 Unit ĐANG nằm trên Grid lên để kéo tiếp: BattleGridManager.RemoveUnit()
///     được gọi ngay lập tức để giải phóng 2 ô cũ; nếu cú kéo này bị huỷ (thả vào chỗ
///     không hợp lệ), Unit sẽ tự đặt lại đúng 2 ô cũ đó.
///   - Có thể kéo thả ngược lại vào khu Component (danh sách item trong Shop) để rút
///     Unit ra khỏi bàn cờ, quay về Shop — không bị mất đi.
/// </summary>
public class UnitPlayerItemUI : TierShopItemUI, IGridPlaceable
{
    /// <summary>Shape cố định 1w x 2h (offset dạng [row,col]) dùng chung cho MỌI Unit.</summary>
    private static readonly Vector2Int[] UnitShapeCells =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
    };

    [Header("Grid Placement Highlight (chỉ riêng Unit)")]
    [SerializeField] private Color colorValid   = new Color(0.2f, 1f,   0.3f, 0.9f);
    [SerializeField] private Color colorInvalid = new Color(1f,   0.2f, 0.2f, 0.9f);

    private MyDuckData _unit;
    public  MyDuckData Unit => _unit;

    // ─── Runtime: vị trí trên Grid (null nếu đang ở trong Shop, chưa đặt) ─────
    private BattleGridCell _placedAnchorCell;   // anchor hiện tại NẾU đang nằm trên Grid
    private BattleGridCell _dragStartAnchorCell; // anchor TRƯỚC khi bắt đầu kéo lần này (để trả lại nếu huỷ)
    private BattleGridCell _hoveredAnchor;      // anchor đang hover trong lúc kéo (để highlight)

    private RectTransform _componentContainer;  // khu danh sách item trong Shop (để có thể kéo Unit về lại)

    // Anchor/pivot gốc của prefab (trước khi bị đổi để khớp toạ độ Grid) — dùng để khôi phục khi trả về Shop.
    private Vector2 _prefabAnchorMin;
    private Vector2 _prefabAnchorMax;
    private Vector2 _prefabPivot;

    // ─── Display ────────────────────────────────────────────
    public override string DisplayName => _unit != null ? _unit.Name : string.Empty;
    public override Sprite DisplayIcon => _unit != null ? _unit.GetDefaultIcon() : null;

    /// <summary>HP hiện tại của Duck này (chỉ số gốc từ MyDuckData).</summary>
    public float CurrentHP => _unit != null ? _unit.BaseHP : 0f;

    /// <summary>Đang nằm trên Battle Grid (đã Place) hay chưa.</summary>
    public bool IsPlacedOnGrid => _placedAnchorCell != null;

    /// <summary>Ô anchor hiện tại trên Grid (null nếu đang ở Shop) — dùng khi 1 Unit khác kéo đè lên để swap vị trí.</summary>
    public BattleGridCell PlacedAnchorCell => _placedAnchorCell;

    protected override void Awake()
    {
        base.Awake();
        _prefabAnchorMin = _rt.anchorMin;
        _prefabAnchorMax = _rt.anchorMax;
        _prefabPivot     = _rt.pivot;
    }

    /// <summary>
    /// Setup trực tiếp từ MyDuckData — nguồn dữ liệu duy nhất cho UnitDuck.
    /// componentContainer: khu danh sách item trong Shop — cần để nhận biết khi người chơi
    /// kéo Unit (đang ở trên Grid) thả ngược lại khu này, coi như "rút Unit về Shop".
    /// </summary>
    public void Setup(MyDuckData unit, BattleGridManager gridManager, RectTransform trash = null,
                       Image trashImg = null, RectTransform componentContainer = null)
    {
        _unit = unit;

        InitCommon(gridManager, trash, trashImg);
        if (componentContainer != null) _componentContainer = componentContainer;

        _placedAnchorCell    = null;
        _dragStartAnchorCell = null;
        _hoveredAnchor        = null;

        if (_unit == null)
            Debug.LogWarning("[UnitPlayerItemUI] Setup() nhan MyDuckData NULL!");

        ApplyShapeSize(UnitShapeCells);
        RefreshVisual();
    }

    protected override void RefreshVisual()
    {
        Sprite icon = _unit != null ? _unit.GetSprite(CurrentTier) : null;
        if (icon == null && _unit != null) icon = _unit.GetDefaultIcon();

        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }

        ApplyTierColor();
    }

    /// <summary>2 UnitPlayerItemUI được coi là cùng loại nếu cùng MyDuckData.ID.</summary>
    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as UnitPlayerItemUI;
        return o != null && o._unit != null && _unit != null && o._unit.ID == _unit.ID;
    }

    // ─── Info Panel (giữ để xem thông tin) ────────────────
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (_unit == null) return;
        ItemInfoPanel.Instance.ShowInfoForUnit(DisplayName, _unit.Level, CurrentHP);
    }

    // ─── Drag (override TOÀN BỘ — Unit có luật riêng, không dùng luồng chung) ─

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleTurnLocked()) return; // Khong cho phep keo-tha Unit khi BattleManager dang o TurnBattle

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        // Nếu đang nằm trên Grid: nhấc lên = gỡ tạm khỏi 2 ô đang chiếm (trả về UnlockedEmpty).
        // Nếu cú kéo bị huỷ, sẽ tự đặt lại đúng 2 ô này ở OnEndDrag.
        _dragStartAnchorCell = _placedAnchorCell;
        if (_placedAnchorCell != null && _gridManager != null)
        {
            _gridManager.RemoveUnit(_placedAnchorCell.Row, _placedAnchorCell.Col, UnitShapeCells, this);
            _placedAnchorCell = null;
        }

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();
        SnapTopLeftToPointer(eventData);

        _canvasGroup.alpha          = 0.8f;
        _canvasGroup.blocksRaycasts = false;

        if (trashImage != null) _trashOriginalColor = trashImage.color;
        _overTrash = false;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _rt.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;

        bool nowOverTrash = IsPointerOverTrash(eventData);
        if (nowOverTrash != _overTrash)
        {
            _overTrash = nowOverTrash;
            if (trashImage != null)
                trashImage.color = _overTrash ? colorTrash : _trashOriginalColor;
        }

        if (!_overTrash)
            UpdateHoverHighlight(GetPointerTarget<BattleGridCell>(eventData));
        else
            ClearHighlight();
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (trashImage != null) trashImage.color = _trashOriginalColor;
        ClearHighlight();

        // 1) Kéo vào Trash → huỷ hẳn (2 ô cũ nếu có đã được gỡ ở OnBeginDrag rồi)
        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log($"[UnitPlayerItemUI] Discarded '{DisplayName}' vao trash.");
            Discard();
            return;
        }

        // 2) Kéo vào 1 UnitItem khác:
        //    - CÙNG unitID + CÙNG Tier (chưa Max)              → Merge (item kéo biến mất, item dưới +1 Tier).
        //    - KHÁC unitID, HOẶC KHÁC Tier, HOẶC Tier đã Max    → nếu item dưới đang nằm trên Grid: SWAP
        //      (item đang kéo thế chỗ trên Grid, item cũ bị đẩy về Component).
        var mergeTarget = GetPointerTarget<TierShopItemUI>(eventData);
        if (mergeTarget != null && mergeTarget != this)
        {
            bool canMerge = mergeTarget.CurrentTier == CurrentTier
                && CurrentTier < MaxTier
                && IsSameKind(mergeTarget);

            if (canMerge)
            {
                mergeTarget.TryUpgradeTier();
                Debug.Log($"[UnitPlayerItemUI] Merge '{DisplayName}' (Tier {CurrentTier}) vao '{mergeTarget.DisplayName}'.");
                Discard();
                return;
            }

            var unitTarget = mergeTarget as UnitPlayerItemUI;
            if (unitTarget != null && unitTarget.IsPlacedOnGrid)
            {
                var swapAnchor = unitTarget.PlacedAnchorCell;
                unitTarget.ForceReturnToComponentContainer();
                PlaceOnGrid(swapAnchor);
                Debug.Log($"[UnitPlayerItemUI] '{DisplayName}' da thay the '{unitTarget.DisplayName}' tren Grid; '{unitTarget.DisplayName}' bi day ve Component.");
                return;
            }
        }

        // 3) Thả về khu Component (danh sách item trong Shop) → rút Unit khỏi Grid, quay lại Shop
        if (IsPointerOverComponentContainer(eventData))
        {
            ReturnToComponentContainer();
            return;
        }

        // 4) Thả vào vị trí HỢP LỆ trên Grid (không Locked, có thể đang bị item khác chiếm):
        //    - Nếu đúng 1 item khác đang chiếm + CÙNG unitID + CÙNG Tier (chưa Max) → Merge vào đó.
        //    - Nếu KHÔNG phải merge (khác loại/tier/đã Max, hoặc bị nhiều item chiếm) → đẩy
        //      TẤT CẢ item đang chiếm về Component rồi mới đặt Unit này vào, KHÔNG Destroy.
        var anchor = GetPointerTarget<BattleGridCell>(eventData);
        if (anchor != null && _gridManager != null && IsShapeAreaValid(anchor))
        {
            var occupants = GetDistinctOccupants(anchor);

            if (occupants.Count == 1)
            {
                var occupantUnit = occupants[0] as UnitPlayerItemUI;
                if (occupantUnit != null
                    && occupantUnit.CurrentTier == CurrentTier
                    && CurrentTier < MaxTier
                    && IsSameKind(occupantUnit))
                {
                    occupantUnit.TryUpgradeTier();
                    Debug.Log($"[UnitPlayerItemUI] Merge '{DisplayName}' (Tier {CurrentTier}) vao '{occupantUnit.DisplayName}' dang tren Grid.");
                    Discard();
                    return;
                }
            }

            foreach (var occ in occupants)
                (occ as IGridPlaceable)?.ForceReturnToComponentContainer();

            PlaceOnGrid(anchor);
            Debug.Log($"[UnitPlayerItemUI] Da dat '{DisplayName}' len grid, chiem 2 o tu ({anchor.Row},{anchor.Col})" +
                (occupants.Count > 0 ? $", day {occupants.Count} item cu ve Component." : ".")); 
            return;
        }

        // 5) Không hợp lệ → trả lại đúng vị trí trước khi kéo (Grid cũ hoặc Shop cũ)
        if (_dragStartAnchorCell != null && _gridManager != null)
        {
            PlaceOnGrid(_dragStartAnchorCell);
        }
        else
        {
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rt.anchoredPosition = _originalAnchoredPos;

            _canvasGroup.alpha          = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    // ─── Placement helpers ────────────────────────────────────

    /// <summary>Đặt Unit lên Grid tại anchor: cập nhật state BattleGridManager + di chuyển item tới đúng vị trí 2 ô.</summary>
    private void PlaceOnGrid(BattleGridCell anchor)
    {
        _gridManager.PlaceUnit(anchor.Row, anchor.Col, _unit, UnitShapeCells, this);
        _placedAnchorCell = anchor;

        foreach (var offset in UnitShapeCells)
            _gridManager.GetCell(anchor.Row + offset.x, anchor.Col + offset.y)?.SetOccupyingItemUI(this);

        // Parent vào cùng transform chứa các Cell để dùng chung hệ toạ độ anchoredPosition.
        Transform gridParent = anchor.transform.parent;
        transform.SetParent(gridParent, true);
        transform.SetAsLastSibling(); // vẽ đè lên các cell bên dưới

        var anchorRT = anchor.GetComponent<RectTransform>();
        _rt.anchorMin        = anchorRT.anchorMin;
        _rt.anchorMax        = anchorRT.anchorMax;
        _rt.pivot            = anchorRT.pivot;
        _rt.anchoredPosition = anchorRT.anchoredPosition;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Bị 1 UnitItem khác "đá" ra khỏi vị trí đang chiếm trên Grid do bị SWAP (kéo item khác đè lên,
    /// khác kind/tier hoặc đã Max Tier) — giải phóng đúng 2 ô đang chiếm rồi trả về Component.
    /// Khác ReturnToComponentContainer() (chỉ gọi được nội bộ sau khi OnBeginDrag đã tự gỡ khỏi Grid),
    /// hàm này TỰ gỡ khỏi Grid trước vì item bị động (không phải đang được kéo).
    /// </summary>
    public void ForceReturnToComponentContainer()
    {
        if (_placedAnchorCell != null && _gridManager != null)
        {
            _gridManager.RemoveUnit(_placedAnchorCell.Row, _placedAnchorCell.Col, UnitShapeCells, this);
            _placedAnchorCell = null;
        }
        ReturnToComponentContainer();
    }

    /// <summary>Kiểm tra toàn bộ ô trong shape 1x2 (tại anchor) có tồn tại và KHÔNG Locked (cho phép UnlockedEmpty HOẶC UnlockedFull — có thể đẩy item khác ra).</summary>
    private bool IsShapeAreaValid(BattleGridCell anchorCell)
    {
        foreach (var offset in UnitShapeCells)
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            if (cell == null || cell.State == BattleGridCell.CellState.Locked) return false;
        }
        return true;
    }

    /// <summary>Danh sách các TierShopItemUI KHÁC NHAU (không trùng) đang chiếm các ô trong shape (tại anchor) — cần đẩy trước khi đặt item mới vào.</summary>
    private System.Collections.Generic.List<TierShopItemUI> GetDistinctOccupants(BattleGridCell anchorCell)
    {
        var result = new System.Collections.Generic.List<TierShopItemUI>();
        foreach (var offset in UnitShapeCells)
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            var occ = cell?.OccupyingItemUI as TierShopItemUI;
            if (occ != null && occ != (TierShopItemUI)this && !result.Contains(occ))
                result.Add(occ);
        }
        return result;
    }

    /// <summary>Rút Unit khỏi Grid (nếu có) và trả về khu Component (danh sách item trong Shop).</summary>
    private void ReturnToComponentContainer()
    {
        transform.SetParent(_componentContainer, true);
        transform.SetAsLastSibling();

        _rt.anchorMin = _prefabAnchorMin;
        _rt.anchorMax = _prefabAnchorMax;
        _rt.pivot     = _prefabPivot;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (_componentContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_componentContainer);

        Debug.Log($"[UnitPlayerItemUI] '{DisplayName}' da duoc rut khoi Grid, quay ve Shop.");
    }

    private bool IsPointerOverComponentContainer(PointerEventData eventData)
    {
        if (_componentContainer == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            _componentContainer, eventData.position, eventData.pressEventCamera);
    }

    // ─── Highlight khi kéo (chỉ tô 2 ô Unit sẽ chiếm, không hiện lại ô Locked) ─

    private void UpdateHoverHighlight(BattleGridCell anchorCell)
    {
        if (anchorCell == _hoveredAnchor) return;
        ClearHighlight();
        _hoveredAnchor = anchorCell;
        if (anchorCell == null || _gridManager == null) return;

        bool valid = _gridManager.CanPlaceUnit(anchorCell.Row, anchorCell.Col, UnitShapeCells);
        Color c = valid ? colorValid : colorInvalid;

        foreach (var offset in UnitShapeCells)
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            cell?.SetUnlockedHighlight(c);
        }
    }

    private void ClearHighlight()
    {
        if (_hoveredAnchor == null || _gridManager == null) return;
        foreach (var offset in UnitShapeCells)
        {
            var cell = _gridManager.GetCell(_hoveredAnchor.Row + offset.x, _hoveredAnchor.Col + offset.y);
            cell?.RestoreVisual();
        }
        _hoveredAnchor = null;
    }
}
