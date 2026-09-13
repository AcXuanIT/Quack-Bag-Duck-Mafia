using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
/// ĐẶT (PLACE) LÊN GRID — override TOÀN BỘ luồng kéo-thả của TierShopItemUI (giống UnitPlayerItemUI,
/// KHÔNG dùng chung step \"Unlock 1 ô Locked\" hay auto-Discard() sau khi đặt của lớp cha):
///   - Chỉ đặt được vào các ô ĐANG Unlocked (UnlockedEmpty) — KHÔNG unlock ô Locked.
///   - Khi hover trong lúc kéo: tô màu xanh/đỏ lên ĐÚNG các ô shape của Weapon (có thể nhiều
///     hơn 1 ô, hình dạng bất kỳ) để báo hợp lệ hay không — KHÔNG hiện lại các ô Locked ẩn.
///   - Khi thả hợp lệ: gọi BattleGridManager.PlaceGear() để đánh dấu đúng các ô đã chiếm,
///     ĐỒNG THỜI di chuyển (reparent) chính GearItem này vào đúng vị trí (góc trên-trái của
///     bounding box shape) — KHÔNG Destroy(). Item vẫn giữ nguyên các component kéo-thả nên
///     có thể được nhấc lên lại bất cứ lúc nào.
///   - Khi nhấc 1 Gear ĐANG nằm trên Grid lên để kéo tiếp: BattleGridManager.RemoveGear() được
///     gọi ngay để giải phóng các ô cũ; nếu cú kéo bị huỷ, Gear tự đặt lại đúng vị trí cũ.
///   - Có thể kéo thả ngược lại vào khu Component (danh sách item trong Shop) để rút Gear
///     ra khỏi bàn cờ, quay về Shop — không bị mất đi.
/// </summary>
public class GearItemUI : TierShopItemUI, IGridPlaceable
{
    [SerializeField] private Image bgImage;

    [Header("Connect Effect (khi Gear được xác định liền kề/kết nối hợp lệ)")]
    [SerializeField] private TextMeshProUGUI textConnect;
    [SerializeField] private float           connectFadeDuration = 0.2f;
    private Tween _connectTween;

    [Header("Rarity Frames (chỉ riêng Gear — index theo Tier)")]
    [SerializeField] private Image    frameImage;
    [SerializeField] private Sprite[] tierFrames; // index 0=Tier1 .. 3=Tier4

    [Header("Grid Placement Highlight (chỉ riêng Gear)")]
    [SerializeField] private Color colorValid   = new Color(0.2f, 1f,   0.3f, 0.9f);
    [SerializeField] private Color colorInvalid = new Color(1f,   0.2f, 0.2f, 0.9f);

    private WeaponEntry _weapon;
    public  WeaponEntry Weapon => _weapon;

    [Header("Charge Fill (chi chay khi BattleManager o trang thai TurnBattle)")]
    [Tooltip("Neu bat, fillImage se chay day tu duoi len tren theo weapon.TimeDelay, het gio thi spawn UnitDuck cho tung Unit lien ket roi chay lai tu dau. Chi chay khi Gear dang lien ket (ke can) voi it nhat 1 UnitItem tren Grid.")]
    [SerializeField] private bool chargeFillEnabled = true;
    private float _chargeTimer;

    [Header("Spawn Duck (khi fillImage chay day 1 vong)")]
    [Tooltip("Prefab UnitDuck dung de spawn khi fillImage chay day (VD unit_001_tier_1 da gan script UnitDuck).")]
    [SerializeField] private GameObject unitDuckPrefab;
    [Tooltip("Vi tri spawn UnitDuck (world space). Neu de trong, mac dinh spawn tai vi tri hien tai cua chinh Gear nay.")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// Danh sách UnitPlayerItemUI đang LIỀN KỀ (4 hướng) với Gear này trên Battle Grid — do
    /// BattleGridManager.RefreshGearUnitLinks() cập nhật mỗi khi hệ thống Cells thay đổi
    /// (đặt/gỡ Gear hoặc Unit). Rỗng nếu Gear chưa đặt lên Grid hoặc không liền kề Unit nào.
    /// </summary>
    private readonly List<UnitPlayerItemUI> _linkedUnits = new List<UnitPlayerItemUI>();
    public IReadOnlyList<UnitPlayerItemUI> LinkedUnits => _linkedUnits;

    /// <summary>Đang liền kề (4 hướng) với ít nhất 1 UnitItem trên Grid hay không (LinkedUnits.Count > 0).</summary>
    public bool IsLinkedToUnit => _linkedUnits.Count > 0;

    /// <summary>
    /// Phát khi 1 Gear ĐÃ ĐẶT trên Grid chạy đầy fillImage (đủ weapon.TimeDelay giây) trong lúc
    /// BattleManager đang ở TurnBattle — tín hiệu để hệ thống spawn (VD BattleSpawnDuck trong
    /// tương lai) biết cần spawn 1 UnitDuck tương ứng với Gear này. Static để bất kỳ listener
    /// nào cũng đăng ký được mà không cần tham chiếu tới từng instance GearItemUI cụ thể.
    /// </summary>
    public static event System.Action<GearItemUI> OnGearFullCharge;

    // ─── Runtime: vị trí trên Grid (null nếu đang ở trong Shop, chưa đặt) ─────
    private BattleGridCell _placedAnchorCell;    // anchor hiện tại (ứng với offset (0,0) của shape) NẾU đang nằm trên Grid
    private BattleGridCell _dragStartAnchorCell; // anchor TRƯỚC khi bắt đầu kéo lần này (để trả lại nếu huỷ)
    private BattleGridCell _hoveredAnchor;       // anchor đang hover trong lúc kéo (để highlight)

    private RectTransform _componentContainer;   // khu danh sách item trong Shop (để có thể kéo Gear về lại)

    // Anchor/pivot gốc của prefab (trước khi bị đổi để khớp toạ độ Grid) — dùng để khôi phục khi trả về Shop.
    private Vector2 _prefabAnchorMin;
    private Vector2 _prefabAnchorMax;
    private Vector2 _prefabPivot;

    // ─── Display ────────────────────────────────────────────
    public override string DisplayName => _weapon != null ? _weapon.Name : string.Empty;
    public override Sprite DisplayIcon => _weapon != null ? _weapon.GetUIIcon() : null;

    /// <summary>Damage/HP hiện tại của Gear này, dùng khi merge lên Tier cao hơn để tính hiệu ứng trong Battle.</summary>
    public float CurrentDamage => _weapon != null ? _weapon.GetCurrentDamage() : 0f;
    public float CurrentHP     => _weapon != null ? _weapon.GetCurrentHP()     : 0f;
    public float AttackRange   => _weapon != null ? _weapon.GetAttackRange()   : 0f;

    /// <summary>Đang nằm trên Battle Grid (đã Place) hay chưa.</summary>
    public bool IsPlacedOnGrid => _placedAnchorCell != null;

    /// <summary>Ô anchor hiện tại trên Grid (offset (0,0) của shape) — null nếu đang ở Shop.</summary>
    public BattleGridCell PlacedAnchorCell => _placedAnchorCell;

    protected override void Awake()
    {
        base.Awake();
        _prefabAnchorMin = _rt.anchorMin;
        _prefabAnchorMax = _rt.anchorMax;
        _prefabPivot     = _rt.pivot;
    }

    /// <summary>
    /// Setup trực tiếp từ WeaponEntry — nguồn dữ liệu duy nhất cho Gear (WeaponData).
    /// componentContainer: khu danh sách item trong Shop — cần để nhận biết khi người chơi
    /// kéo Gear (đang ở trên Grid) thả ngược lại khu này, coi như "rút Gear về Shop".
    /// </summary>
    public void Setup(WeaponEntry weapon, BattleGridManager gridManager, RectTransform trash = null,
                       Image trashImg = null, RectTransform componentContainer = null)
    {
        _weapon = weapon;
        InitCommon(gridManager, trash, trashImg);
        if (componentContainer != null) _componentContainer = componentContainer;

        _placedAnchorCell    = null;
        _dragStartAnchorCell = null;
        _hoveredAnchor        = null;
        _linkedUnits.Clear();

        if (_weapon == null)
            Debug.LogWarning("[GearItemUI] Setup() nhan WeaponEntry NULL!");

        ApplyShapeSize(GetShapeCells());
        ApplyIconLayout();
        SetupChargeFillImage();
        RefreshVisual();

        _chargeTimer = 0f;
    }

    /// <summary>Cấu hình fillImage 1 lần thành dạng Filled/Vertical/Bottom để chạy đầy từ dưới lên trên.</summary>
    private void SetupChargeFillImage()
    {
        if (fillImage == null) return;
        fillImage.type       = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 0f;
    }

    /// <summary>
    /// Cập nhật bởi BattleGridManager.RefreshGearUnitLinks() mỗi khi Cells thay đổi (đặt/gỡ Gear
    /// hoặc Unit trên Grid). Truyền danh sách UnitPlayerItemUI hiện đang liền kề (4 hướng) với
    /// Gear này — null hoặc rỗng nghĩa là không còn liền kề Unit nào.
    /// </summary>
    public void SetLinkedUnits(List<UnitPlayerItemUI> units)
    {
        _linkedUnits.Clear();
        if (units != null) _linkedUnits.AddRange(units);
    }

    /// <summary>
    /// Chỉ chạy khi: đã đặt trên Grid (IsPlacedOnGrid) + đang liền kề ít nhất 1 UnitItem
    /// (IsLinkedToUnit) + BattleManager đang ở TurnBattle.
    /// fillImage chạy đầy từ dưới lên trên trong đúng weapon.TimeDelay giây; khi chạy đầy
    /// (fillAmount = 1) thì phát OnGearFullCharge (tín hiệu spawn duck) rồi CHẠY LẠI TỪ ĐẦU
    /// (fillAmount về 0, timer reset). Ở mọi trường hợp khác (chưa đặt lên Grid, chưa liên kết
    /// với Unit nào, hoặc BattleManager không ở TurnBattle) thì fill về 0 và không đếm giờ.
    /// </summary>
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

    /// <summary>
    /// Gọi khi fillImage chạy đầy 1 vòng (đủ weapon.TimeDelay giây) — lấy LinkedUnits (danh
    /// sách UnitItem đang liền kề Gear này) và spawn 1 bản UnitDuck cho MỖI Unit trong danh
    /// sách đó (có thể spawn nhiều cùng lúc nếu Gear liền kề nhiều Unit).
    /// </summary>
    private void SpawnLinkedDucks()
    {
        if (_linkedUnits.Count == 0) return;

        foreach (var unit in _linkedUnits)
        {
            if (unit == null || unit.Unit == null) continue;
            BattleManager.Instance.spawnDuck.SpawnDuck(_weapon, unit.Unit , CurrentTier , unit.CurrentTier);
        }
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

    /// <summary>Góc trên-trái (row/col nhỏ nhất) của bounding box shape — có thể KHÁC offset (0,0) nếu shape có offset âm (VD (0,-1)).</summary>
    private Vector2Int GetShapeTopLeftOffset(Vector2Int[] cells)
    {
        int minR = cells[0].x, minC = cells[0].y;
        foreach (var c in cells)
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
            if (idx < tierFrames.Length) frameImage.sprite = tierFrames[idx];
        }

        ApplyTierColor();
    }

    /// <summary>
    /// Fill (base.fillImage — layer TREN) mang sprite ShapeFill (mask theo dung shape cua
    /// weapon) va duoc to mau theo Tier (qua base.ApplyTierColor()) — giong het vai tro
    /// layer "Fill" cua UnitItem. BG (layer DUOI) mang ShapeSprite — vien/khung tinh cua
    /// shape, KHONG doi theo Tier — giong vai tro layer "Shape" cua UnitItem.
    /// </summary>
    protected override void ApplyTierColor()
    {
        base.ApplyTierColor(); // fillImage.color = tierColors[Tier]

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

    /// <summary>
    /// Icon mac dinh full-stretch theo toan bo card, padding Left/Top/Right/Bottom = 15.
    /// Rieng shape "topleft" (3 o, bounding box 2x2 — hang tren 2 o lien + 1 o duoi ben phai):
    /// icon chi dai dien cho HANG TREN (rong 2 o x cao 1 o = ty le 2:1 theo x:y), neo sat mep
    /// tren cua card thay vi keo gian phu het ca hinh L.
    /// </summary>
    private void ApplyIconLayout()
    {
        if (iconImage == null) return;
        var iconRT = iconImage.rectTransform;

        if (IsTopLeftTripleShape(GetShapeCells()))
        {
            iconRT.anchorMin = new Vector2(0f, 1f);
            iconRT.anchorMax = new Vector2(1f, 1f);
            iconRT.pivot     = new Vector2(0.5f, 1f);

            float cellH = (_gridManager != null && _gridManager.CellHeight > 0f)
                ? _gridManager.CellHeight
                : ShopItemSizing.CellSize;

            iconRT.sizeDelta        = new Vector2(0f, cellH); // width theo stretch (2 o), height = 1 o -> ty le 2:1
            iconRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            const float padding = 15f; // Left/Top/Right/Bottom = 15
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.pivot     = new Vector2(0.5f, 0.5f);

            // offsetMin = (Left, Bottom), offsetMax = (-Right, -Top) — cách chuẩn để set đúng
            // 4 giá trị Left/Top/Right/Bottom hiển thị trong Inspector khi RectTransform đang stretch.
            iconRT.offsetMin = new Vector2(padding, padding);
            iconRT.offsetMax = new Vector2(-padding, -padding);
        }
    }

    /// <summary>Shape "topleft": dung 3 o, bounding box 2x2 (2 hang x 2 cot) — thieu dung 1 goc.</summary>
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

    /// <summary>
    /// Hiện textConnect ("Đã kết nối !") rồi mờ dần biến mất trong connectFadeDuration (0.2s).
    /// Gọi mỗi khi Gear này được xác định là đang liền kề/kết nối hợp lệ (VD qua
    /// BattleGridManager.GetAdjacentOccupants()). Nếu bị gọi lại trong lúc hiệu ứng đang chạy,
    /// tween cũ bị huỷ và CHẠY LẠI TỪ ĐẦU (hiện rõ 100% rồi mờ dần lại từ đầu) — không cộng dồn.
    /// </summary>
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
        var c = textConnect.color;
        c.a = a;
        textConnect.color = c;
    }

    private void OnDestroy() => _connectTween?.Kill();

    /// <summary>2 GearItemUI được coi là cùng loại nếu trỏ chung 1 WeaponEntry.ID.</summary>
    protected override bool IsSameKind(TierShopItemUI other)
    {
        var o = other as GearItemUI;
        return o != null && o._weapon != null && _weapon != null && o._weapon.ID == _weapon.ID;
    }

    // ─── Info Panel (giữ để xem thông tin) ────────────────
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (_weapon == null) return;
        ItemInfoPanel.Instance.ShowInfoForGear(DisplayName, _weapon.Level, CurrentDamage, _weapon.TimeDelay, CurrentHP);
    }

    // ─── Drag (override TOÀN BỘ — Gear có luật riêng, không dùng luồng chung) ─

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleTurnLocked()) return; // Khong cho phep keo-tha Gear khi BattleManager dang o TurnBattle

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        // Nếu đang nằm trên Grid: nhấc lên = gỡ tạm khỏi các ô đang chiếm (trả về UnlockedEmpty).
        // Nếu cú kéo bị huỷ, sẽ tự đặt lại đúng vị trí này ở OnEndDrag.
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

        // 1) Kéo vào Trash → huỷ hẳn (các ô cũ nếu có đã được gỡ ở OnBeginDrag rồi)
        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log($"[GearItemUI] Discarded '{DisplayName}' vao trash.");
            Discard();
            return;
        }

        // 2) Kéo vào 1 GearItem khác CÙNG weaponID + CÙNG Tier (chưa Max) → Merge
        var mergeTarget = GetPointerTarget<TierShopItemUI>(eventData);
        if (mergeTarget != null && mergeTarget != this
            && mergeTarget.CurrentTier == CurrentTier
            && CurrentTier < MaxTier
            && IsSameKind(mergeTarget))
        {
            mergeTarget.TryUpgradeTier();
            Debug.Log($"[GearItemUI] Merge '{DisplayName}' (Tier {CurrentTier}) vao '{mergeTarget.DisplayName}'.");
            Discard();
            return;
        }

        // 3) Thả về khu Component (danh sách item trong Shop) → rút Gear khỏi Grid, quay lại Shop
        if (IsPointerOverComponentContainer(eventData))
        {
            ReturnToComponentContainer();
            return;
        }

        // 4) Thả vào vị trí HỢP LỆ trên Grid (không Locked, có thể đang bị item khác chiếm):
        //    - Nếu đúng 1 item khác đang chiếm + CÙNG loại + CÙNG Tier (chưa Max) → Merge vào đó.
        //    - Nếu KHÔNG phải merge (khác loại/tier/đã Max, hoặc bị nhiều item chiếm) → đẩy
        //      TẤT CẢ item đang chiếm về Component rồi mới đặt Gear này vào, KHÔNG Destroy.
        var anchor = GetPointerTarget<BattleGridCell>(eventData);
        if (anchor != null && _gridManager != null && _weapon != null && IsShapeAreaValid(anchor))
        {
            var occupants = GetDistinctOccupants(anchor);

            if (occupants.Count == 1)
            {
                var occupantGear = occupants[0] as GearItemUI;
                if (occupantGear != null
                    && occupantGear.CurrentTier == CurrentTier
                    && CurrentTier < MaxTier
                    && IsSameKind(occupantGear))
                {
                    occupantGear.TryUpgradeTier();
                    Debug.Log($"[GearItemUI] Merge '{DisplayName}' (Tier {CurrentTier}) vao '{occupantGear.DisplayName}' dang tren Grid.");
                    Discard();
                    return;
                }
            }

            foreach (var occ in occupants)
                (occ as IGridPlaceable)?.ForceReturnToComponentContainer();

            PlaceOnGrid(anchor);
            Debug.Log($"[GearItemUI] Da dat '{DisplayName}' len grid tai anchor ({anchor.Row},{anchor.Col})" +
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

    /// <summary>Đặt Gear lên Grid tại anchor: cập nhật state BattleGridManager + di chuyển item tới góc trên-trái bounding box shape.</summary>
    private void PlaceOnGrid(BattleGridCell anchor)
    {
        _gridManager.PlaceGear(anchor.Row, anchor.Col, _weapon, this);
        _placedAnchorCell = anchor;

        var cells = GetShapeCells();
        foreach (var offset in cells)
            _gridManager.GetCell(anchor.Row + offset.x, anchor.Col + offset.y)?.SetOccupyingItemUI(this);

        var topLeft  = GetShapeTopLeftOffset(cells);
        var topLeftCell = _gridManager.GetCell(anchor.Row + topLeft.x, anchor.Col + topLeft.y) ?? anchor;

        // Parent vào cùng transform chứa các Cell để dùng chung hệ toạ độ anchoredPosition.
        Transform gridParent = topLeftCell.transform.parent;
        transform.SetParent(gridParent, true);
        transform.SetAsLastSibling(); // vẽ đè lên các cell bên dưới

        var anchorRT = topLeftCell.GetComponent<RectTransform>();
        _rt.anchorMin        = anchorRT.anchorMin;
        _rt.anchorMax        = anchorRT.anchorMax;
        _rt.pivot            = anchorRT.pivot;
        _rt.anchoredPosition = anchorRT.anchoredPosition;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Bị 1 item khác (Gear hoặc Unit) "đá" ra khỏi vị trí đang chiếm trên Grid do bị đặt đè lên
    /// (không phải merge) — giải phóng đúng các ô đang chiếm rồi trả về Component.
    /// Khác ReturnToComponentContainer() (chỉ gọi nội bộ sau khi OnBeginDrag đã tự gỡ khỏi Grid),
    /// hàm này TỰ gỡ khỏi Grid trước vì item bị động (không phải đang được kéo).
    /// </summary>
    public void ForceReturnToComponentContainer()
    {
        if (_placedAnchorCell != null && _gridManager != null && _weapon != null)
        {
            _gridManager.RemoveGear(_placedAnchorCell.Row, _placedAnchorCell.Col, _weapon, this);
            _placedAnchorCell = null;
        }
        ReturnToComponentContainer();
    }

    /// <summary>Kiểm tra toàn bộ ô trong shape (tại anchor) có tồn tại và KHÔNG Locked (cho phép UnlockedEmpty HOẶC UnlockedFull — có thể đẩy item khác ra).</summary>
    private bool IsShapeAreaValid(BattleGridCell anchorCell)
    {
        foreach (var offset in GetShapeCells())
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
        foreach (var offset in GetShapeCells())
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            var occ = cell?.OccupyingItemUI as TierShopItemUI;
            if (occ != null && occ != (TierShopItemUI)this && !result.Contains(occ))
                result.Add(occ);
        }
        return result;
    }

    /// <summary>Rút Gear khỏi Grid (nếu có) và trả về khu Component (danh sách item trong Shop).</summary>
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

        Debug.Log($"[GearItemUI] '{DisplayName}' da duoc rut khoi Grid, quay ve Shop.");
    }

    private bool IsPointerOverComponentContainer(PointerEventData eventData)
    {
        if (_componentContainer == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            _componentContainer, eventData.position, eventData.pressEventCamera);
    }

    // ─── Highlight khi kéo (chỉ tô các ô shape của Gear, không hiện lại ô Locked) ─

    private void UpdateHoverHighlight(BattleGridCell anchorCell)
    {
        if (anchorCell == _hoveredAnchor) return;
        ClearHighlight();
        _hoveredAnchor = anchorCell;
        if (anchorCell == null || _gridManager == null || _weapon == null) return;

        bool valid = _gridManager.CanPlaceGear(anchorCell.Row, anchorCell.Col, _weapon);
        Color c = valid ? colorValid : colorInvalid;

        foreach (var offset in GetShapeCells())
        {
            var cell = _gridManager.GetCell(anchorCell.Row + offset.x, anchorCell.Col + offset.y);
            cell?.SetUnlockedHighlight(c);
        }
    }

    private void ClearHighlight()
    {
        if (_hoveredAnchor == null || _gridManager == null) return;
        foreach (var offset in GetShapeCells())
        {
            var cell = _gridManager.GetCell(_hoveredAnchor.Row + offset.x, _hoveredAnchor.Col + offset.y);
            cell?.RestoreVisual();
        }
        _hoveredAnchor = null;
    }
}
