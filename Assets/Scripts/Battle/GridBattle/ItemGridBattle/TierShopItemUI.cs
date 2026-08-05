using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lớp cha dùng chung cho các ShopItem có khái niệm "Tier" và có thể MERGE với nhau
/// (GearItemUI, UnitPlayerItemUI). KHÔNG còn dùng ShopItemData nữa — mỗi subclass
/// Setup() trực tiếp từ nguồn data thật của nó (WeaponEntry cho Gear, MyDuckData
/// cho UnitDuck), tất cả đều resolve qua DataManager ở tầng ShopBatteManager.
///
/// Gộp toàn bộ logic kéo-thả dùng chung:
///   1) Kéo vào Trash Zone            → Discard() (huỷ item)
///   2) Kéo vào 1 item khác CÙNG LOẠI + CÙNG TIER (chưa Max) → merge:
///        item đích được nâng lên Tier+1 (RefreshVisual), item đang kéo bị Discard().
///   3) Kéo vào 1 ô Locked của Battle Grid → Unlock đúng 1 ô đó (dùng chung
///      BattleGridManager.CanUnlock()/UnlockShape() với shape 1 ô [0,0]).
///   4) Kéo vào 1 ô ĐÃ Unlock (UnlockedEmpty) của Battle Grid → ĐẶT (place) hẳn
///      Gear/Unit lên grid theo ĐÚNG shape riêng của item (không phải shape 1 ô):
///        - GearItemUI dùng WeaponEntry.GridCells (qua BattleGridManager.CanPlaceGear/PlaceGear).
///        - UnitPlayerItemUI dùng shape cố định 1x2 (qua BattleGridManager.CanPlaceUnit/PlaceUnit).
///      Subclass override CanPlaceShapeAt()/PlaceShapeAt() để cung cấp hành vi riêng.
///   5) Không hợp lệ → trả về vị trí cũ.
///
/// Dùng chung 1 list màu theo Tier (index 0=Tier1 … 3=Tier4, Max Tier = 4).
/// Mọi item mới spawn ra từ Shop luôn bắt đầu ở Tier 1 — Tier chỉ tăng qua merge.
///
/// SIZING:
///   - Dùng chung ShopItemSizing (CellSize/CellGap) với GridShopItemUI để đảm bảo
///     1 ô luôn cùng kích thước vật lý giữa cả 3 loại item trong Shop (Grid/Gear/UnitDuck).
///     Subclass gọi ApplyShapeSize(cells) trong Setup() SAU khi gán data riêng.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class TierShopItemUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    protected const int MaxTier = 4;

    // Shape 1 ô duy nhất — dùng khi kéo Gear/UnitDuck (không có hình dạng nhiều ô như Grid item)
    // vào Battle Grid để unlock đúng 1 ô Locked.
    private static readonly Vector2Int[] SingleCellOffset = { Vector2Int.zero };

    // ─── Inspector: Base UI ───────────────────────────────────
    [Header("Base UI")]
    [SerializeField] protected Image           bgImage;
    [SerializeField] protected Image           iconImage;
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI levelText;

    // ─── Inspector: Tier Colors (DÙNG CHUNG cho Gear & UnitDuck) ─
    [Header("Tier Colors (index 0=Tier1 .. 3=Tier4)")]
    [SerializeField]
    protected Color[] tierColors = new Color[4]
    {
        new Color(0.55f, 0.55f, 0.55f, 1f), // Tier 1 - xám (mặc định)
        new Color(0.25f, 0.55f, 1.00f, 1f), // Tier 2 - xanh dương
        new Color(0.65f, 0.25f, 1.00f, 1f), // Tier 3 - tím
        new Color(1.00f, 0.78f, 0.10f, 1f), // Tier 4 - vàng
    };

    // ─── Inspector: Trash Zone ─────────────────────────────────
    [Header("Trash Zone")]
    [SerializeField] protected RectTransform trashZone;
    [SerializeField] protected Image         trashImage;
    [SerializeField] protected Color         colorTrash = new Color(1f, 0.3f, 0.3f, 0.9f);
    private Color _trashOriginalColor;
    private bool  _overTrash;

    // ─── Runtime ─────────────────────────────────────────────
    protected BattleGridManager _gridManager;

    protected CanvasGroup   _canvasGroup;
    protected Canvas        _rootCanvas;
    protected RectTransform _rt;
    protected LayoutElement _layoutElement;
    private   Transform     _originalParent;
    private   int           _originalSiblingIndex;
    private   Vector2       _originalAnchoredPos;
    protected bool          _isDragging;

    private int _currentTier = 1;
    public  int CurrentTier => _currentTier;

    public abstract string DisplayName { get; }
    public abstract Sprite DisplayIcon { get; }

    /// <summary>Cập nhật icon/tên/level text theo CurrentTier + data riêng. Gọi mỗi khi Setup hoặc merge nâng Tier.</summary>
    protected abstract void RefreshVisual();

    /// <summary>So sánh 2 item có cùng "loại" (VD cùng weaponID, cùng unitID) để cho phép merge hay không.</summary>
    protected abstract bool IsSameKind(TierShopItemUI other);

    /// <summary>
    /// Kiểm tra item này (theo shape RIÊNG của nó — không phải shape 1 ô) có thể ĐẶT
    /// (place, không phải unlock) lên grid tại anchor cell hay không. Anchor cell phải
    /// đang ở trạng thái UnlockedEmpty (điều này base class đã kiểm tra trước khi gọi).
    /// Mặc định false (không hỗ trợ) — subclass override để cung cấp hành vi riêng
    /// (GearItemUI dùng WeaponEntry.GridCells, UnitPlayerItemUI dùng shape 1x2 cố định).
    /// </summary>
    protected virtual bool CanPlaceShapeAt(BattleGridCell anchorCell) => false;

    /// <summary>
    /// Thực hiện đặt item lên grid tại anchor cell theo shape riêng của nó — chỉ được
    /// gọi SAU KHI CanPlaceShapeAt() đã trả về true. Subclass override để gọi đúng
    /// API tương ứng trên BattleGridManager (PlaceGear/PlaceUnit).
    /// </summary>
    protected virtual void PlaceShapeAt(BattleGridCell anchorCell) { }

    // ─── Init ────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _canvasGroup   = GetComponent<CanvasGroup>();
        _rt            = GetComponent<RectTransform>();
        _layoutElement = GetComponent<LayoutElement>();
        _rootCanvas    = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.rootCanvas;
    }

    /// <summary>
    /// Gán các tham chiếu dùng chung (gridManager/trash) và reset về Tier 1
    /// (mọi item mới mua từ Shop luôn bắt đầu Tier 1, chỉ tăng qua merge).
    /// Gọi từ Setup() riêng của từng subclass — SAU khi gán xong data riêng,
    /// rồi subclass tự gọi RefreshVisual().
    /// </summary>
    protected void InitCommon(BattleGridManager gridManager, RectTransform trash, Image trashImg)
    {
        _gridManager = gridManager;
        if (trash    != null) trashZone  = trash;
        if (trashImg != null) trashImage = trashImg;
        _currentTier = 1;
    }

    /// <summary>
    /// Áp kích thước (RectTransform + LayoutElement) theo shape gồm nhiều ô (offset [row,col]),
    /// dùng CHUNG công thức CellSize/CellGap với GridShopItemUI (qua ShopItemSizing) —
    /// đảm bảo 1 ô luôn cùng kích thước vật lý giữa Grid/Gear/UnitDuck trong Shop.
    /// Gọi trong Setup() của subclass, SAU khi đã có shape cells của data riêng.
    /// </summary>
    protected void ApplyShapeSize(Vector2Int[] cells)
    {
        ShopItemSizing.ApplySize(_rt, _layoutElement, cells);
    }

    /// <summary>Tô màu bgImage theo tierColors[CurrentTier-1] — dùng chung cho mọi loại item con.</summary>
    protected void ApplyTierColor()
    {
        if (bgImage == null || tierColors == null || tierColors.Length == 0) return;
        int idx = Mathf.Clamp(_currentTier - 1, 0, tierColors.Length - 1);
        bgImage.color = tierColors[idx];
    }

    /// <summary>Nâng Tier lên 1 bậc (tối đa MaxTier=4) rồi refresh hiển thị. Trả về false nếu đã Max.</summary>
    public bool TryUpgradeTier()
    {
        if (_currentTier >= MaxTier) return false;
        _currentTier++;
        RefreshVisual();
        return true;
    }

    public void Discard() => Destroy(gameObject);

    // ─── Click ───────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        Debug.Log($"[{GetType().Name}] Clicked: {DisplayName} (Tier {_currentTier})");
    }

    // ─── Drag ───────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (trashImage != null) trashImage.color = _trashOriginalColor;

        // 1) Kéo vào Trash → Discard
        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log($"[{GetType().Name}] Discarded '{DisplayName}' vao trash.");
            Discard();
            return;
        }

        // 2) Kéo vào item khác CÙNG LOẠI + CÙNG TIER (chưa Max) → Merge
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

        // 3) Kéo vào 1 ô Locked của Battle Grid → Unlock đúng ô đó
        if (cell != null && _gridManager != null && _gridManager.CanUnlock(cell.Row, cell.Col, SingleCellOffset))
        {
            _gridManager.UnlockShape(cell.Row, cell.Col, SingleCellOffset);
            Debug.Log($"[{GetType().Name}] Unlock o ({cell.Row},{cell.Col}) bang '{DisplayName}'.");
            Discard();
            return;
        }

        // 4) Kéo vào 1 ô ĐÃ Unlock (UnlockedEmpty) → ĐẶT (place) item lên grid theo shape riêng
        if (cell != null && cell.State == BattleGridCell.CellState.UnlockedEmpty && CanPlaceShapeAt(cell))
        {
            PlaceShapeAt(cell);
            Debug.Log($"[{GetType().Name}] Da dat '{DisplayName}' len grid tai o ({cell.Row},{cell.Col}).");
            Discard();
            return;
        }

        // 5) Không hợp lệ → trả về vị trí cũ
        transform.SetParent(_originalParent, true);
        transform.SetSiblingIndex(_originalSiblingIndex);
        _rt.anchoredPosition = _originalAnchoredPos;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    private bool IsPointerOverTrash(PointerEventData eventData)
    {
        if (trashZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            trashZone, eventData.position, eventData.pressEventCamera);
    }

    /// <summary>Raycast toàn bộ UI dưới con trỏ, trả về component kiểu T đầu tiên tìm thấy (bỏ qua chính mình).</summary>
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
