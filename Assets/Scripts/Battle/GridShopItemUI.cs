using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI của một Grid ShopItem trong Shop.
///
/// RULE ĐẶT ITEM:
///   - Chỉ được drag vào ô Locked (để unlock chúng).
///   - Toàn bộ shape phải nằm trên ô Locked.
///   - Ít nhất 1 ô trong shape phải kề (4 hướng) với ô đã Unlocked
///     (UnlockedEmpty hoặc UnlockedFull).
///
/// KHI ĐẶT THÀNH CÔNG:
///   - Tất cả ô trong shape chuyển Locked → UnlockedEmpty (unlock).
///
/// SIZING:
///   - width  = cols * cellSize + (cols-1) * cellGap
///   - height = rows * cellSize + (rows-1) * cellGap
///   - Apply vào RectTransform + LayoutElement để đồng bộ với HorizontalLayoutGroup.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GridShopItemUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // ─── Inspector ───────────────────────────────────────────
    [Header("Base UI")]
    [SerializeField] public Image           bgImage;
    [SerializeField] public Image           iconImage;
    [SerializeField] public Image           frameImage;
    [SerializeField] public TextMeshProUGUI nameText;

    [Header("Shape Preview")]
    [SerializeField] private GridShapePreview shapePreview;

    [Header("Grid Item Sizing")]
    [SerializeField] private float cellSize = 56f;
    [SerializeField] private float cellGap  = 4f;

    [Header("Rarity Frames")]
    [SerializeField] private Sprite[] rarityFrames;

    [Header("Trash Zone")]
    [SerializeField] private RectTransform trashZone;   // Close GO trong UIBatteMap
    [SerializeField] private Image         trashImage;  // Image cua Close de highlight
    [SerializeField] private Color         colorTrash   = new Color(1f, 0.3f, 0.3f, 0.9f);
    private Color _trashOriginalColor;
    private bool  _overTrash;

    
[Header("Highlight Colors")]
    [SerializeField] private Color colorValid   = new Color(0.2f, 1f,   0.3f, 0.9f);
    [SerializeField] private Color colorInvalid = new Color(1f,   0.2f, 0.2f, 0.9f);

    // ─── Runtime ─────────────────────────────────────────────
    [HideInInspector] public ShopItemData data;

    private CanvasGroup   _canvasGroup;
    private Canvas        _rootCanvas;
    private RectTransform _rt;
    private Transform     _originalParent;
    private int           _originalSiblingIndex;
    private Vector2       _originalAnchoredPos;

    private BattleGridManager _gridManager;
    private BattleGridCell    _hoveredAnchor;   // anchor cell đang hover
    private bool              _isDragging;

    // ─── Init ────────────────────────────────────────────────
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rt          = GetComponent<RectTransform>();
        _rootCanvas  = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.rootCanvas;
    }

public void Setup(ShopItemData itemData, BattleGridManager gridManager,
                         RectTransform trash = null, Image trashImg = null)
    {
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
        if (shapePreview != null && data.itemType == ShopItemData.ItemType.Grid)
            shapePreview.Draw(data.gridCells);

        ApplySize(data.gridCells);
    }

    // ─── Sizing ──────────────────────────────────────────────
    private void ApplySize(Vector2Int[] cells)
    {
        int cols = 1, rows = 1;
        if (cells != null && cells.Length > 0)
        {
            int minC = cells[0].y, maxC = cells[0].y;
            int minR = cells[0].x, maxR = cells[0].x;
            foreach (var c in cells)
            {
                if (c.y < minC) minC = c.y; if (c.y > maxC) maxC = c.y;
                if (c.x < minR) minR = c.x; if (c.x > maxR) maxR = c.x;
            }
            cols = maxC - minC + 1;
            rows = maxR - minR + 1;
        }
        float w = cols * cellSize + (cols - 1) * cellGap;
        float h = rows * cellSize + (rows - 1) * cellGap;

        if (_rt == null) _rt = GetComponent<RectTransform>();
        _rt.sizeDelta = new Vector2(w, h);

        var le = GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minWidth = w; le.minHeight = h;
            le.preferredWidth = w; le.preferredHeight = h;
        }
    }

    // ─── Click ───────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        Debug.Log("[GridShopItemUI] Clicked: " + (data != null ? data.itemName : "null"));
    }

    // ─── Drag ────────────────────────────────────────────────
public void OnBeginDrag(PointerEventData eventData)
    {
        if (data == null || data.itemType != ShopItemData.ItemType.Grid) return;

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

        ShowAllLockedCells();
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

        var anchor = GetCellUnderPointer(eventData);

        ClearHighlight(hideLocked: true);
        HideAllLockedCells();
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log("[GridShopItemUI] Discarded '" + data.itemName + "' vao trash.");
            Destroy(gameObject);
            return;
        }

        bool placed = TryUnlockOnGrid(anchor);
        if (!placed)
        {
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rt.anchoredPosition = _originalAnchoredPos;
            Debug.Log("[GridShopItemUI] Drag cancelled.");
        }
        else
        {
            Debug.Log("[GridShopItemUI] Unlocked cells with '" + data.itemName + "'!");
            Destroy(gameObject);
        }
    }

    // ─── Placement logic ─────────────────────────────────────

    private BattleGridCell GetCellUnderPointer(PointerEventData eventData)
    {
        if (_gridManager == null) return null;
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var cell = r.gameObject.GetComponentInParent<BattleGridCell>();
            if (cell != null) return cell;
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra có thể đặt shape tại anchorCell không.
    /// Delegate hoàn toàn cho BattleGridManager.CanUnlock() để giữ logic ở 1 chỗ.
    /// </summary>
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

        foreach (var offset in data.gridCells)
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            cell?.SetHighlightColor(c);
        }
    }

private void ClearHighlight(bool hideLocked = false)
    {
        if (_hoveredAnchor == null || data?.gridCells == null) return;
        foreach (var offset in data.gridCells)
        {
            var cell = _gridManager.GetCell(_hoveredAnchor.Row + offset.x, _hoveredAnchor.Col + offset.y);
            if (cell != null)
            {
                if (cell.State == BattleGridCell.CellState.Locked)
                {
                    if (hideLocked)
                        cell.HideLockedPreview(); // chi an khi EndDrag
                    else
                        cell.SetHighlightColor(new Color(1f, 1f, 1f, 0.25f)); // tra ve mau hint mac dinh
                }
                else
                    cell.RestoreVisual();
            }
        }
        _hoveredAnchor = null;
    }

    /// <summary>Unlock các ô nếu hợp lệ. Trả về true nếu thành công.</summary>
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
            var cell = _gridManager.GetCell(r, c);
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
            var cell = _gridManager.GetCell(r, c);
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
