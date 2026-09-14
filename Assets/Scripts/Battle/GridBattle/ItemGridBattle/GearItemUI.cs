using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class GearItemUI : TierShopItemUI, IGridPlaceable
{
    [SerializeField] private Image bgImage;

    [Header("Connect Effect")]
    [SerializeField] private TextMeshProUGUI textConnect;
    [SerializeField] private float connectFadeDuration = 0.2f;
    private Tween _connectTween;

    [Header("Rarity Frames")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Sprite[] tierFrames; 

    [Header("Grid Placement Highlight")]
    [SerializeField] private Color colorValid   = new Color(0.2f, 1f,   0.3f, 0.9f);
    [SerializeField] private Color colorInvalid = new Color(1f,   0.2f, 0.2f, 0.9f);

    private WeaponEntry _weapon;
    public  WeaponEntry Weapon => _weapon;


    [SerializeField] private bool chargeFillEnabled = true;
    private float _chargeTimer;

    [Header("Spawn Duck")]
    [SerializeField] private GameObject unitDuckPrefab;
    [SerializeField] private Transform spawnPoint;

    private readonly List<UnitPlayerItemUI> _linkedUnits = new List<UnitPlayerItemUI>();
    public IReadOnlyList<UnitPlayerItemUI> LinkedUnits => _linkedUnits;

    public bool IsLinkedToUnit => _linkedUnits.Count > 0;


    public static event System.Action<GearItemUI> OnGearFullCharge;

    private BattleGridCell _placedAnchorCell;    
    private BattleGridCell _dragStartAnchorCell; 
    private BattleGridCell _hoveredAnchor;       

    private RectTransform _componentContainer;   

    private Vector2 _prefabAnchorMin;
    private Vector2 _prefabAnchorMax;
    private Vector2 _prefabPivot;

    public override string DisplayName => _weapon != null ? _weapon.Name : string.Empty;
    public override Sprite DisplayIcon => _weapon != null ? _weapon.GetUIIcon() : null;

    public float CurrentDamage => _weapon != null ? _weapon.GetCurrentDamage() : 0f;
    public float CurrentHP     => _weapon != null ? _weapon.GetCurrentHP()     : 0f;
    public float AttackRange   => _weapon != null ? _weapon.GetAttackRange()   : 0f;

    public bool IsPlacedOnGrid => _placedAnchorCell != null;

    public BattleGridCell PlacedAnchorCell => _placedAnchorCell;

    protected override void Awake()
    {
        base.Awake();
        _prefabAnchorMin = _rt.anchorMin;
        _prefabAnchorMax = _rt.anchorMax;
        _prefabPivot     = _rt.pivot;
    }

    public void Setup(WeaponEntry weapon, BattleGridManager gridManager, RectTransform trash = null, Image trashImg = null, RectTransform componentContainer = null)
    {
        _weapon = weapon;
        InitCommon(gridManager, trash, trashImg);
        if (componentContainer != null) _componentContainer = componentContainer;

        _placedAnchorCell    = null;
        _dragStartAnchorCell = null;
        _hoveredAnchor        = null;
        _linkedUnits.Clear();

        ApplyShapeSize(GetShapeCells());
        ApplyIconLayout();
        SetupChargeFillImage();
        RefreshVisual();

        _chargeTimer = 0f;
    }

    private void SetupChargeFillImage()
    {
        if (fillImage == null) return;
        fillImage.type       = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 0f;
    }

    public void SetLinkedUnits(List<UnitPlayerItemUI> units)
    {
        _linkedUnits.Clear();
        if (units != null) _linkedUnits.AddRange(units);
    }
    private void Update()
    {
        if (!chargeFillEnabled || fillImage == null || _weapon == null) return;


        bool isWar = IsPlacedOnGrid
            && IsLinkedToUnit
            && BattleManager.Instance != null
            && BattleManager.Instance.CurrentState == BattleManager.BattleState.TurnBattle;

        if (!isWar)
        {
            if (_chargeTimer != 0f || fillImage.fillAmount != 0f)
            {
                _chargeTimer = 0f;
                fillImage.fillAmount = 0f;
            }
            return;
        }

        float duration = _weapon.TimeDelay > 0f ? _weapon.TimeDelay : 1f;
        _chargeTimer += Time.deltaTime;
        fillImage.fillAmount = Mathf.Clamp01(_chargeTimer / duration);

        if (_chargeTimer >= duration)
        {
            _chargeTimer = 0f;
            fillImage.fillAmount = 0f;

            SpawnLinkedDucks();
            OnGearFullCharge?.Invoke(this);
        }
    }
    private void SpawnLinkedDucks()
    {
        if (_linkedUnits.Count == 0) return;

        foreach (UnitPlayerItemUI unit in _linkedUnits)
        {
            if (unit == null || unit.Unit == null) continue;
            BattleManager.Instance.spawnDuck.SpawnDuck(_weapon, unit.Unit , CurrentTier , unit.CurrentTier);
        }
    }

    private Vector2Int[] GetShapeCells()
    {
        WeaponGridCell[] cells = _weapon != null ? _weapon.GridCells : null;

        if (cells == null || cells.Length == 0)
            return new Vector2Int[] { Vector2Int.zero };

        Vector2Int[] result = new Vector2Int[cells.Length];

        for (int i = 0; i < cells.Length; i++)
            result[i] = cells[i].gridPosition;

        return result;
    }

    private Vector2Int GetShapeTopLeftOffset(Vector2Int[] cells)
    {
        int minR = cells[0].x, minC = cells[0].y;
        foreach (Vector2Int c in cells)
        {
            if (c.x < minR) minR = c.x;
            if (c.y < minC) minC = c.y;
        }
        return new Vector2Int(minR, minC);
    }

    protected override void RefreshVisual()
    {
        Sprite icon = _weapon != null ? _weapon.GetSpriteByTier(CurrentTier) : null;

        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }

        if (frameImage != null && tierFrames != null)
        {
            int idx = Mathf.Clamp(CurrentTier - 1, 0, tierFrames.Length - 1);
            if (idx < tierFrames.Length) 
            { 
                frameImage.sprite = tierFrames[idx]; 
                fillImage.color = tierColors[idx];
            }
        }

        ApplyTierColor();
    }
    protected override void ApplyTierColor()
    {
        base.ApplyTierColor(); 

        if (fillImage != null && _weapon != null)
        {
            fillImage.sprite  = _weapon.ShapeFill;
            fillImage.enabled = _weapon.ShapeFill != null;
        }

        if (bgImage != null && _weapon != null)
        {
            bgImage.sprite  = _weapon.ShapeSprite;
            bgImage.enabled = _weapon.ShapeSprite != null;
        }
    }

    private void ApplyIconLayout()
    {
        if (iconImage == null) return;
        RectTransform iconRT = iconImage.rectTransform;

        if (IsTopLeftTripleShape(GetShapeCells()))
        {
            iconRT.anchorMin = new Vector2(0f, 1f);
            iconRT.anchorMax = new Vector2(1f, 1f);
            iconRT.pivot     = new Vector2(0.5f, 1f);

            float cellH = (_gridManager != null && _gridManager.CellHeight > 0f)
                ? _gridManager.CellHeight
                : ShopItemSizing.CellSize;

            iconRT.sizeDelta        = new Vector2(0f, cellH); 
            iconRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            const float padding = 15f; 
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.pivot     = new Vector2(0.5f, 0.5f);

            iconRT.offsetMin = new Vector2(padding, padding);
            iconRT.offsetMax = new Vector2(-padding, -padding);
        }
    }

    private bool IsTopLeftTripleShape(Vector2Int[] cells)
    {
        if (cells == null || cells.Length != 3) return false;

        int minR = cells[0].x, maxR = cells[0].x, minC = cells[0].y, maxC = cells[0].y;
        foreach (var c in cells)
        {
            if (c.x < minR) minR = c.x; if (c.x > maxR) maxR = c.x;
            if (c.y < minC) minC = c.y; if (c.y > maxC) maxC = c.y;
        }
        return (maxR - minR + 1) == 2 && (maxC - minC + 1) == 2;
    }

    public void OnConnect()
    {
        if (textConnect == null) return;

        _connectTween?.Kill();

        textConnect.gameObject.SetActive(true);
        SetTextConnectAlpha(1f);

        _connectTween = DOTween.To(() => textConnect.color.a, SetTextConnectAlpha, 0f, connectFadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => textConnect.gameObject.SetActive(false));
    }

    private void SetTextConnectAlpha(float a)
    {
        Color c = textConnect.color;
        c.a = a;
        textConnect.color = c;
    }

    private void OnDestroy() => _connectTween?.Kill();

    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as GearItemUI;
        return o != null && o._weapon != null && _weapon != null && o._weapon.ID == _weapon.ID;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (_weapon == null) return;
        ItemInfoPanel.Instance.ShowInfoForGear(DisplayName, _weapon.Level, CurrentDamage, _weapon.TimeDelay, CurrentHP);
    }


    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleTurnLocked()) return; 

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        _dragStartAnchorCell = _placedAnchorCell;
        if (_placedAnchorCell != null && _gridManager != null && _weapon != null)
        {
            _gridManager.RemoveGear(_placedAnchorCell.Row, _placedAnchorCell.Col, _weapon, this);
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

        TierShopItemUI mergeTarget = GetPointerTarget<TierShopItemUI>(eventData);
        if (mergeTarget != null && mergeTarget != this
            && mergeTarget.CurrentTier == CurrentTier
            && CurrentTier < MaxTier
            && IsSameKind(mergeTarget))
        {
            mergeTarget.TryUpgradeTier();
            Discard();
            return;
        }

        if (IsPointerOverComponentContainer(eventData))
        {
            ReturnToComponentContainer();
            return;
        }

        BattleGridCell anchor = GetPointerTarget<BattleGridCell>(eventData);
        if (anchor != null && _gridManager != null && _weapon != null && IsShapeAreaValid(anchor))
        {
            List<TierShopItemUI> occupants = GetDistinctOccupants(anchor);

            if (occupants.Count == 1)
            {
                GearItemUI occupantGear = occupants[0] as GearItemUI;
                if (occupantGear != null
                    && occupantGear.CurrentTier == CurrentTier
                    && CurrentTier < MaxTier
                    && IsSameKind(occupantGear))
                {
                    occupantGear.TryUpgradeTier();

                    Discard();
                    return;
                }
            }

            foreach (TierShopItemUI occ in occupants)
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

    private void PlaceOnGrid(BattleGridCell anchor)
    {
        _gridManager.PlaceGear(anchor.Row, anchor.Col, _weapon, this);
        _placedAnchorCell = anchor;

        Vector2Int[] cells = GetShapeCells();
        foreach (var offset in cells)
            _gridManager.GetCell(anchor.Row + offset.x, anchor.Col + offset.y)?.SetOccupyingItemUI(this);

        Vector2Int topLeft  = GetShapeTopLeftOffset(cells);
        BattleGridCell topLeftCell = _gridManager.GetCell(anchor.Row + topLeft.x, anchor.Col + topLeft.y) ?? anchor;

        Transform gridParent = topLeftCell.transform.parent;
        transform.SetParent(gridParent, true);
        transform.SetAsLastSibling(); 

        RectTransform anchorRT = topLeftCell.GetComponent<RectTransform>();
        _rt.anchorMin        = anchorRT.anchorMin;
        _rt.anchorMax        = anchorRT.anchorMax;
        _rt.pivot            = anchorRT.pivot;
        _rt.anchoredPosition = anchorRT.anchoredPosition;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }
    public void ForceReturnToComponentContainer()
    {
        if (_placedAnchorCell != null && _gridManager != null && _weapon != null)
        {
            _gridManager.RemoveGear(_placedAnchorCell.Row, _placedAnchorCell.Col, _weapon, this);
            _placedAnchorCell = null;
        }
        ReturnToComponentContainer();
    }

    private bool IsShapeAreaValid(BattleGridCell anchorCell)
    {
        foreach (Vector2Int offset in GetShapeCells())
        {
            BattleGridCell cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            if (cell == null || cell.State == BattleGridCell.CellState.Locked) return false;
        }
        return true;
    }

    private List<TierShopItemUI> GetDistinctOccupants(BattleGridCell anchorCell)
    {
        List<TierShopItemUI> result = new List<TierShopItemUI>();
        foreach (Vector2Int offset in GetShapeCells())
        {
            BattleGridCell cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            TierShopItemUI occ = cell?.OccupyingItemUI as TierShopItemUI;
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
        if (anchorCell == null || _gridManager == null || _weapon == null) return;

        bool valid = _gridManager.CanPlaceGear(anchorCell.Row, anchorCell.Col, _weapon);
        Color c = valid ? colorValid : colorInvalid;

        foreach (Vector2Int offset in GetShapeCells())
        {
            BattleGridCell cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            cell?.SetUnlockedHighlight(c);
        }
    }

    private void ClearHighlight()
    {
        if (_hoveredAnchor == null || _gridManager == null) return;
        foreach (Vector2Int offset in GetShapeCells())
        {
            BattleGridCell cell = _gridManager.GetCell(_hoveredAnchor.Row + offset.x, _hoveredAnchor.Col + offset.y);
            cell?.RestoreVisual();
        }
        _hoveredAnchor = null;
    }
}
