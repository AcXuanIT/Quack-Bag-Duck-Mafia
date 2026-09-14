using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GridShopItemUI : MonoBehaviour,
    IShopItem,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Base UI")]
    [SerializeField] public Image           bgImage;
    [SerializeField] public Image           iconImage;
    [SerializeField] public Image           frameImage;
    [SerializeField] public TextMeshProUGUI nameText;

    [Header("Shape Preview")]
    [SerializeField] private GridShapePreview shapePreview;

    [Header("Rarity Frames")]
    [SerializeField] private Sprite[] rarityFrames;

    [Header("Trash Zone")]
    [SerializeField] private RectTransform trashZone;   
    [SerializeField] private Image         trashImage;  
    [SerializeField] private Color         colorTrash   = new Color(1f, 0.3f, 0.3f, 0.9f);
    private Color _trashOriginalColor;
    private bool  _overTrash;

    
    [Header("Highlight Colors")]
    [SerializeField] private Color colorValid   = new Color(0.2f, 1f,   0.3f, 0.9f);
    [SerializeField] private Color colorInvalid = new Color(1f,   0.2f, 0.2f, 0.9f);

    [Header("Drag Grab Offset")]
    [SerializeField] private Vector2 dragGrabOffset = new Vector2(10f, -10f);

    [HideInInspector] public ShopItemData data;

        public ShopItemData ShopData    => data;
        public string       DisplayName => data != null ? data.itemName : string.Empty;
        public Sprite       DisplayIcon => data != null ? data.icon : null;
        public int          Rarity      => data != null ? data.rarity : 0;
        public int          SellPrice   => data != null ? data.sellPrice : 0;

        public void Discard() => Destroy(gameObject);


    private CanvasGroup    _canvasGroup;
    private Canvas         _rootCanvas;
    private RectTransform  _rt;
    private LayoutElement  _layoutElement;
    private Transform      _originalParent;
    private int            _originalSiblingIndex;
    private Vector2        _originalAnchoredPos;

    private BattleGridManager _gridManager;
    private BattleGridCell    _hoveredAnchor;   
    private bool              _isDragging;

    // Init 
    private void Awake() => EnsureCached();

    private void EnsureCached()
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

    public void Setup(ShopItemData itemData, BattleGridManager gridManager,
                         RectTransform trash = null, Image trashImg = null)
    {
        EnsureCached();

        data         = itemData;
        _gridManager = gridManager;
        if (trash    != null) trashZone  = trash;
        if (trashImg != null) trashImage = trashImg;
        if (data == null) return;

        if (iconImage  != null && data.icon != null)             iconImage.sprite  = data.icon;
        if (bgImage    != null && data.backgroundSprite != null) bgImage.sprite    = data.backgroundSprite;
        if (nameText   != null)                                  nameText.text     = data.itemName;
        if (frameImage != null && rarityFrames != null && data.rarity < rarityFrames.Length)
            frameImage.sprite = rarityFrames[data.rarity];
        if (shapePreview != null)
            shapePreview.Draw(data.gridCells);

        ShopItemSizing.ApplySize(_rt, _layoutElement, data.gridCells);
    }

    // ─── Click ─
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
    }

    // ─── Drag ──
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (data == null) return;

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();
        SnapTopLeftOffsetToPointer(eventData);

        _canvasGroup.alpha          = 0.8f;
        _canvasGroup.blocksRaycasts = false;

        if (trashImage != null) _trashOriginalColor = trashImage.color;
        _overTrash = false;

        ShowAllLockedCells();
    }

    private void SnapTopLeftOffsetToPointer(PointerEventData eventData)
    {
        if (_rt == null || _rootCanvas == null) return;

        RectTransform canvasRT = _rootCanvas.transform as RectTransform;
        if (canvasRT == null) return;

        Vector3[] corners = new Vector3[4];
        _rt.GetWorldCorners(corners);
        Vector3 topLeftWorld = corners[1]; 

        Vector2 topLeftScreen = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, topLeftWorld);

        Vector2 topLeftLocal, pointerLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, topLeftScreen, eventData.pressEventCamera, out topLeftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, eventData.position, eventData.pressEventCamera, out pointerLocal);

        _rt.anchoredPosition += (pointerLocal - (topLeftLocal + dragGrabOffset));
    }

    public void OnDrag(PointerEventData eventData)
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
            UpdateHoverHighlight(GetCellUnderPointer(eventData));
        else
            ClearHighlight();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (trashImage != null) trashImage.color = _trashOriginalColor;

        BattleGridCell anchor = GetCellUnderPointer(eventData);

        ClearHighlight(hideLocked: true);
        HideAllLockedCells();
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Destroy(gameObject);
            return;
        }

        bool placed = TryUnlockOnGrid(anchor);
        if (!placed)
        {
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rt.anchoredPosition = _originalAnchoredPos;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private BattleGridCell GetCellUnderPointer(PointerEventData eventData)
    {
        if (_gridManager == null) return null;
        List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            BattleGridCell cell = r.gameObject.GetComponentInParent<BattleGridCell>();
            if (cell != null) return cell;
        }
        return null;
    }

    private bool CanUnlock(BattleGridCell anchorCell)
    {
        if (anchorCell == null || data?.gridCells == null) return false;
        return _gridManager.CanUnlock(anchorCell.Row, anchorCell.Col, data.gridCells);
    }

    private void UpdateHoverHighlight(BattleGridCell anchorCell)
    {
        if (anchorCell == _hoveredAnchor) return;
        ClearHighlight();
        _hoveredAnchor = anchorCell;
        if (anchorCell == null || data?.gridCells == null) return;

        bool valid = CanUnlock(anchorCell);
        Color c = valid ? colorValid : colorInvalid;

        foreach (Vector2Int offset in data.gridCells)
        {
            BattleGridCell cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            cell?.SetHighlightColor(c);
        }
    }

    private void ClearHighlight(bool hideLocked = false)
    {
        if (_hoveredAnchor == null || data?.gridCells == null) return;
        foreach (Vector2Int offset in data.gridCells)
        {
            BattleGridCell cell = _gridManager.GetCell(_hoveredAnchor.Row + offset.x, _hoveredAnchor.Col + offset.y);
            if (cell != null)
            {
                if (cell.State == BattleGridCell.CellState.Locked)
                {
                    if (hideLocked)
                        cell.HideLockedPreview(); 
                    else
                        cell.SetHighlightColor(new Color(1f, 1f, 1f, 0.25f)); 
                }
                else
                    cell.RestoreVisual();
            }
        }
        _hoveredAnchor = null;
    }

    private bool TryUnlockOnGrid(BattleGridCell anchorCell)
    {
        if (!CanUnlock(anchorCell)) return false;
        _gridManager.UnlockShape(anchorCell.Row, anchorCell.Col, data.gridCells);
        return true;
    }


    private void ShowAllLockedCells()
    {
        if (_gridManager == null) return;
        Color hint = new Color(1f, 1f, 1f, 0.25f);
        for (int r = 0; r < _gridManager.Rows; r++)
        for (int c = 0; c < _gridManager.Cols; c++)
        {
            BattleGridCell cell = _gridManager.GetCell(r, c);
            if (cell != null && cell.State == BattleGridCell.CellState.Locked)
                cell.SetHighlightColor(hint);
        }
    }

    private void HideAllLockedCells()
    {
        if (_gridManager == null) return;
        for (int r = 0; r < _gridManager.Rows; r++)
        for (int c = 0; c < _gridManager.Cols; c++)
        {
            BattleGridCell cell = _gridManager.GetCell(r, c);
            if (cell != null && cell.State == BattleGridCell.CellState.Locked)
                cell.HideLockedPreview();
        }
    }

    private bool IsPointerOverTrash(PointerEventData eventData)
    {
        if (trashZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            trashZone, eventData.position, eventData.pressEventCamera);
    }

}
