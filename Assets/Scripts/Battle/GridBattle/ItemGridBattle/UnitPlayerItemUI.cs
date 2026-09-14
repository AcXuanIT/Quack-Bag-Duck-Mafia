using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UnitPlayerItemUI : TierShopItemUI, IGridPlaceable
{
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

    private BattleGridCell _placedAnchorCell;   
    private BattleGridCell _dragStartAnchorCell; 
    private BattleGridCell _hoveredAnchor;      

    private RectTransform _componentContainer;  

    private Vector2 _prefabAnchorMin;
    private Vector2 _prefabAnchorMax;
    private Vector2 _prefabPivot;

    // ─── Display ──
    public override string DisplayName => _unit != null ? _unit.Name : string.Empty;
    public override Sprite DisplayIcon => _unit != null ? _unit.GetDefaultIcon() : null;

    public float CurrentHP => _unit != null ? _unit.BaseHP : 0f;

    public bool IsPlacedOnGrid => _placedAnchorCell != null;

    public BattleGridCell PlacedAnchorCell => _placedAnchorCell;

    protected override void Awake()
    {
        base.Awake();
        _prefabAnchorMin = _rt.anchorMin;
        _prefabAnchorMax = _rt.anchorMax;
        _prefabPivot     = _rt.pivot;
    }
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

    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as UnitPlayerItemUI;
        return o != null && o._unit != null && _unit != null && o._unit.ID == _unit.ID;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (_unit == null) return;
        ItemInfoPanel.Instance.ShowInfoForUnit(DisplayName, _unit.Level, CurrentHP);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleTurnLocked()) return;

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;


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

        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Discard();
            return;
        }

        var mergeTarget = GetPointerTarget<TierShopItemUI>(eventData);
        if (mergeTarget != null && mergeTarget != this)
        {
            bool canMerge = mergeTarget.CurrentTier == CurrentTier
                && CurrentTier < MaxTier
                && IsSameKind(mergeTarget);

            if (canMerge)
            {
                mergeTarget.TryUpgradeTier();
                Discard();
                return;
            }

            var unitTarget = mergeTarget as UnitPlayerItemUI;
            if (unitTarget != null && unitTarget.IsPlacedOnGrid)
            {
                var swapAnchor = unitTarget.PlacedAnchorCell;
                unitTarget.ForceReturnToComponentContainer();
                PlaceOnGrid(swapAnchor);
                return;
            }
        }

        if (IsPointerOverComponentContainer(eventData))
        {
            ReturnToComponentContainer();
            return;
        }

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
                    Discard();
                    return;
                }
            }

            foreach (var occ in occupants)
                (occ as IGridPlaceable)?.ForceReturnToComponentContainer();

            PlaceOnGrid(anchor);

            return;
        }

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

    // ─── Placement helpers ───
    private void PlaceOnGrid(BattleGridCell anchor)
    {
        _gridManager.PlaceUnit(anchor.Row, anchor.Col, _unit, UnitShapeCells, this);
        _placedAnchorCell = anchor;

        foreach (var offset in UnitShapeCells)
            _gridManager.GetCell(anchor.Row + offset.x, anchor.Col + offset.y)?.SetOccupyingItemUI(this);

        Transform gridParent = anchor.transform.parent;
        transform.SetParent(gridParent, true);
        transform.SetAsLastSibling(); 

        var anchorRT = anchor.GetComponent<RectTransform>();
        _rt.anchorMin        = anchorRT.anchorMin;
        _rt.anchorMax        = anchorRT.anchorMax;
        _rt.pivot            = anchorRT.pivot;
        _rt.anchoredPosition = anchorRT.anchoredPosition;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }
    public void ForceReturnToComponentContainer()
    {
        if (_placedAnchorCell != null && _gridManager != null)
        {
            _gridManager.RemoveUnit(_placedAnchorCell.Row, _placedAnchorCell.Col, UnitShapeCells, this);
            _placedAnchorCell = null;
        }
        ReturnToComponentContainer();
    }
    private bool IsShapeAreaValid(BattleGridCell anchorCell)
    {
        foreach (var offset in UnitShapeCells)
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            if (cell == null || cell.State == BattleGridCell.CellState.Locked) return false;
        }
        return true;
    }

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

    }

    private bool IsPointerOverComponentContainer(PointerEventData eventData)
    {
        if (_componentContainer == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            _componentContainer, eventData.position, eventData.pressEventCamera);
    }

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
