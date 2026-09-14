using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public interface IGridPlaceable
{
    bool IsPlacedOnGrid { get; }

    void ForceReturnToComponentContainer();
}

[RequireComponent(typeof(CanvasGroup))]
public abstract class TierShopItemUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    protected const int MaxTier = 4;

    private static readonly Vector2Int[] SingleCellOffset = { Vector2Int.zero };

    [Header("Base UI")]
    [SerializeField] protected Image           fillImage;
    [SerializeField] protected Image           iconImage;

    [Header("Tier")]
    [SerializeField]
    protected Color[] tierColors = new Color[4]
    {
        new Color(0.55f, 0.55f, 0.55f, 0.4f), 
        new Color(0.25f, 0.55f, 1.00f, 0.4f), 
        new Color(0.65f, 0.25f, 1.00f, 0.4f), 
        new Color(1.00f, 0.78f, 0.10f, 0.4f), 
    };

    [Header("Trash Zone")]
    [SerializeField] protected RectTransform trashZone;
    [SerializeField] protected Image         trashImage;
    [SerializeField] protected Color         colorTrash = new Color(1f, 0.3f, 0.3f, 0.9f);
    protected Color _trashOriginalColor;
    protected bool  _overTrash;

    protected BattleGridManager _gridManager;

    protected CanvasGroup   _canvasGroup;
    protected Canvas        _rootCanvas;
    protected RectTransform _rt;
    protected LayoutElement _layoutElement;
    protected Transform     _originalParent;
    protected int           _originalSiblingIndex;
    protected Vector2       _originalAnchoredPos;
    protected bool          _isDragging;

    private static int _nextInstanceId = 1;

    public int InstanceId { get; private set; }

    private int _currentTier = 1;
    public  int CurrentTier => _currentTier;

    public abstract string DisplayName { get; }
    public abstract Sprite DisplayIcon { get; }

    protected abstract void RefreshVisual();

    protected abstract bool IsSameKind(TierShopItemUI other);

    protected virtual bool CanPlaceShapeAt(BattleGridCell anchorCell) => false;

    protected virtual void PlaceShapeAt(BattleGridCell anchorCell) { }

    // ─── Init ─
    protected virtual void Awake()
    {
        InstanceId     = _nextInstanceId++;
        _canvasGroup   = GetComponent<CanvasGroup>();
        _rt            = GetComponent<RectTransform>();
        _layoutElement = GetComponent<LayoutElement>();
        _rootCanvas    = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.rootCanvas;
    }

    protected void InitCommon(BattleGridManager gridManager, RectTransform trash, Image trashImg)
    {
        EnsureCached();

        _gridManager = gridManager;
        if (trash    != null) trashZone  = trash;
        if (trashImg != null) trashImage = trashImg;
        _currentTier = 1;
    }

    protected void EnsureCached()
    {
        if (_rt == null)            _rt            = GetComponent<RectTransform>();
        if (_canvasGroup == null)   _canvasGroup   = GetComponent<CanvasGroup>();
        if (_layoutElement == null) _layoutElement = GetComponent<LayoutElement>();
        if (_rootCanvas == null)
        {
            _rootCanvas = GetComponentInParent<Canvas>(true);
            if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
                _rootCanvas = _rootCanvas.rootCanvas;
        }
    }

    protected void ApplyShapeSize(Vector2Int[] cells)
    {
        ShopItemSizing.ApplySize(_rt, _layoutElement, cells);
    }

    protected virtual void ApplyTierColor()
    {
        if (fillImage == null || tierColors == null || tierColors.Length == 0) return;
        int idx = Mathf.Clamp(_currentTier - 1, 0, tierColors.Length - 1);
        fillImage.color = tierColors[idx];
    }

    public bool TryUpgradeTier()
    {
        if (_currentTier >= MaxTier) return false;
        _currentTier++;
        RefreshVisual();
        return true;
    }

    public void Discard() => Destroy(gameObject);

    protected bool IsBattleTurnLocked()
    {
        return BattleManager.Instance != null
            && BattleManager.Instance.CurrentState == BattleManager.BattleState.TurnBattle;
    }

    // ─── Click ─
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
    }

    public virtual void OnPointerDown(PointerEventData eventData) { }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        ItemInfoPanel.Instance.HideInfo();
    }

    protected void SnapTopLeftToPointer(PointerEventData eventData)
    {
        if (_rt == null || _rootCanvas == null) return;

        var canvasRT = _rootCanvas.transform as RectTransform;
        if (canvasRT == null) return;

        Vector3[] corners = new Vector3[4];
        _rt.GetWorldCorners(corners);
        Vector3 topLeftWorld = corners[1]; 

        Vector2 topLeftScreen = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, topLeftWorld);

        Vector2 topLeftLocal, pointerLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, topLeftScreen, eventData.pressEventCamera, out topLeftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, eventData.position, eventData.pressEventCamera, out pointerLocal);

        _rt.anchoredPosition += (pointerLocal - topLeftLocal);
    }

    // ─── Drag ───────────────────────────────────────────────
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleTurnLocked()) return; 

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha          = 0.8f;
        _canvasGroup.blocksRaycasts = false;

        if (trashImage != null) _trashOriginalColor = trashImage.color;
        _overTrash = false;
    }

    public virtual void OnDrag(PointerEventData eventData)
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
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (trashImage != null) trashImage.color = _trashOriginalColor;

        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log($"[{GetType().Name}] Discarded '{DisplayName}' vao trash.");
            Discard();
            return;
        }

        var mergeTarget = GetPointerTarget<TierShopItemUI>(eventData);
        if (mergeTarget != null && mergeTarget != this
            && mergeTarget.CurrentTier == _currentTier
            && _currentTier < MaxTier
            && IsSameKind(mergeTarget))
        {
            mergeTarget.TryUpgradeTier();
            Debug.Log($"[{GetType().Name}] Merge '{DisplayName}' (Tier {_currentTier}) vao '{mergeTarget.DisplayName}' -> Tier {mergeTarget.CurrentTier}.");
            Discard();
            return;
        }

        var cell = GetPointerTarget<BattleGridCell>(eventData);

        if (cell != null && _gridManager != null && _gridManager.CanUnlock(cell.Row, cell.Col, SingleCellOffset))
        {
            _gridManager.UnlockShape(cell.Row, cell.Col, SingleCellOffset);
            Debug.Log($"[{GetType().Name}] Unlock o ({cell.Row},{cell.Col}) bang '{DisplayName}'.");
            Discard();
            return;
        }

        if (cell != null && cell.State == BattleGridCell.CellState.UnlockedEmpty && CanPlaceShapeAt(cell))
        {
            PlaceShapeAt(cell);
            Debug.Log($"[{GetType().Name}] Da dat '{DisplayName}' len grid tai o ({cell.Row},{cell.Col}).");
            Discard();
            return;
        }

        transform.SetParent(_originalParent, true);
        transform.SetSiblingIndex(_originalSiblingIndex);
        _rt.anchoredPosition = _originalAnchoredPos;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    protected bool IsPointerOverTrash(PointerEventData eventData)
    {
        if (trashZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            trashZone, eventData.position, eventData.pressEventCamera);
    }

    protected T GetPointerTarget<T>(PointerEventData eventData) where T : Component
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            if (r.gameObject == gameObject) continue;
            var comp = r.gameObject.GetComponentInParent<T>();
            if (comp != null) return comp;
        }
        return null;
    }
}
