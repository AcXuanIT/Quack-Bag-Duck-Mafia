using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loại item đang chiếm 1 ô trong <see cref="BattleGridManager.GridItemCell"/>.
/// </summary>
public enum GridItemType
{
    None,
    Gear,
    Unit
}

/// <summary>
/// 1 Ô DỮ LIỆU LOGIC trong hệ thống Cells của BattleGridManager — TÁCH BIỆT hoàn toàn với
/// BattleGridCell (MonoBehaviour hiển thị UI/sprite). Đây là lớp "quản lý" thuần dữ liệu:
/// biết ô này Lock hay không, và item nào (Gear/Unit) đang thực sự chiếm nó.
///
/// ItemID (KHÔNG PHẢI Type+Tier) là khoá định danh DUY NHẤT cho từng item INSTANCE — dùng
/// GameObject.GetInstanceID() của chính item đó. Điều này đảm bảo 2 GearItem CÙNG WeaponID
/// + CÙNG Tier (2 instance khác nhau, VD 2 khẩu súng giống hệt nhau đặt cạnh nhau) KHÔNG bị
/// hệ thống nhầm là "cùng 1 item" khi ghi/xoá dữ liệu ô.
/// </summary>
[System.Serializable]
public struct GridItemCell
{
    public bool          IsLocked;
    public GridItemType  ItemType;
    public int           ItemID;
    public MonoBehaviour ItemRef;

    public bool IsEmpty => ItemType == GridItemType.None;

    public void Clear()
    {
        ItemType = GridItemType.None;
        ItemID   = 0;
        ItemRef  = null;
    }
}

/// <summary>
/// Tao va quan ly Battle Grid (mac dinh 5 cot x 7 hang).
/// - Mac dinh unlock vung 3x3 chinh giua.
/// - Cau truc cell don gian: moi cell chi co 1 GameObject voi 1 Image (bgImage).
///   Locked        → sprite spriteLocked   (grid_base), an hoan toan binh thuong.
///   UnlockedEmpty → sprite spriteUnlocked (grid_gear_shape_solo).
///   UnlockedFull  → sprite spriteUnlocked (grid_gear_shape_solo).
///
/// Hai khai niem "Grid" trong project va cach chung lien ket:
///   1. Battle Grid (lop nay + BattleGridCell) — la "ban co": quan ly Locked/Unlocked
///      cua tung o. Grid ShopItem (loai item hinh khoi trong Shop) chi dung de UNLOCK
///      o (Locked → UnlockedEmpty), khong chiem giu vinh vien.
///   2. Gear Shape (WeaponEntry.GridCells trong WeaponData) — la hinh dang cua 1 vu khi
///      khi no THUC SU duoc dat len ban co, chiem nhieu o cung luc.
///   PlaceGear()/RemoveGear() la cau noi giua 2 khai niem: dat 1 WeaponEntry len ban co
///   se danh dau cac BattleGridCell tuong ung la UnlockedFull (OccupyingWeapon = weapon)
///   DONG THOI goi weapon.OccupyCell() de WeaponEntry cung tu biet cac o no dang chiem.
///   Tuong tu, PlaceUnit()/RemoveUnit() la cau noi cho UnitPlayerItemUI (MyDuckData):
///   dat 1 Unit len ban co se danh dau cac BattleGridCell tuong ung la UnlockedFull
///   (OccupyingUnit = unit). MyDuckData khong tu track o chiem (khac WeaponEntry),
///   nen toan bo trang thai chiem o nam o BattleGridCell.OccupyingUnit.
///
/// Pooling: cac o (BattleGridCell) duoc lay/tra ve qua PoolingManager (Scripts/Tool)
/// thay vi Instantiate/Destroy moi lan BuildGrid()/ResetGrid() — tranh GC spike khi
/// lien tuc build lai luoi (VD moi tran dau moi qua ResetGrid()).
///
/// SIZING: CellWidth/CellHeight/SpacingX/SpacingY (public, cap nhat moi lan BuildGrid())
/// la NGUON DUY NHAT cho kich thuoc 1 o thuc te tren ban co — ShopItemSizing doc truc
/// tiep tu day de dam bao Item khi spawn trong Shop co kich thuoc TRUNG KHOP voi 1 o
/// thuc su tren Battle Grid (khong dung hang so cung).
///
/// HỆ THỐNG CELLS (GridItemCell[,]): lớp dữ liệu logic độc lập, song song với BattleGridCell,
/// quản lý CHÍNH XÁC item nào (Gear hay Unit, phân biệt theo ItemID duy nhất) đang chiếm từng
/// ô. Mỗi khi PlaceGear/RemoveGear/PlaceUnit/RemoveUnit chạy (thêm hoặc xoá item khỏi Grid),
/// hệ thống tự động ghi/xoá dữ liệu tương ứng trong Cells, rồi gọi RefreshGearUnitLinks() để
/// quét lại toàn bộ Cells và cập nhật trạng thái "liên kết" giữa GearItem và UnitItem liền kề
/// (hiện hiệu ứng OnConnect() trên các Gear đang liền kề ít nhất 1 Unit).
/// </summary>
public class BattleGridManager : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private int     columns = 5;
    [SerializeField] private int     rows    = 7;
    [SerializeField] private Vector2 spacing = new Vector2(4f, 4f);

    [Header("Default Unlock Zone (center 3x3)")]
    [SerializeField] private int defaultUnlockCols = 3;
    [SerializeField] private int defaultUnlockRows = 3;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteLocked;   // grid_base
    [SerializeField] private Sprite spriteUnlocked; // grid_gear_shape_solo

    [Header("Cell Prefab (auto-built if null)")]
    [SerializeField] private GameObject cellPrefab;

    [Header("Editor Preview (khong anh huong runtime/build)")]
    [Tooltip("Bat/tat ve luoi preview trong Scene View luc Edit Mode (khong can bam Play).")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private bool showGizmoLabels = true;

    private BattleGridCell[,] _cells;

    /// <summary>
    /// Mảng 2 chiều dữ liệu logic (kích thước rows x columns, đồng bộ với _cells) — quản lý
    /// item nào (Gear/Unit) đang thực sự chiếm từng ô, độc lập với việc hiển thị UI.
    /// </summary>
    private GridItemCell[,] _itemCells;

    // Template (component) dùng làm "prefab" nguồn cho PoolingManager.Spawn<BattleGridCell>()
    // (GameObject của nó không parent vào transform của grid, để không bị BuildGrid() dọn nhầm).
    private BattleGridCell _cellTemplate;

    public int   Rows       => rows;
    public int   Cols       => columns;
    public float CellWidth  { get; private set; }
    public float CellHeight { get; private set; }

    /// <summary>Khoảng cách ngang (X) giữa 2 ô liền kề trên Battle Grid — dùng để ShopItemSizing tính đúng kích thước item nhiều ô.</summary>
    public float SpacingX => spacing.x;
    /// <summary>Khoảng cách dọc (Y) giữa 2 ô liền kề trên Battle Grid — dùng để ShopItemSizing tính đúng kích thước item nhiều ô.</summary>
    public float SpacingY => spacing.y;

    void Awake() => BuildGrid();

    // ── Build ────────────────────────────────────────────────

    /// <summary>Lấy (hoặc tạo lần đầu) component BattleGridCell trên template GameObject dùng làm nguồn Pool.</summary>
    private BattleGridCell GetCellTemplate()
    {
        if (_cellTemplate != null) return _cellTemplate;

        var go = new GameObject("~BattleGridCell_Template (Pool Source)");
        go.SetActive(false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.preserveAspect = false;
        _cellTemplate = go.AddComponent<BattleGridCell>();
        return _cellTemplate;
    }

[ContextMenu("Rebuild Grid")]
    public void BuildGrid()
    {
        // Dọn TOÀN BỘ child hiện có trong hierarchy (không dựa vào _cells, vì field
        // runtime này bị reset về null sau domain reload / khi gọi BuildGrid() ở Edit
        // Mode ngoài Awake() — nếu chỉ dựa vào _cells, cell cũ còn sót lại trong scene
        // sẽ không được Despawn và bị cell mới build chồng lên, tạo ra 2 lớp Cell
        // trùng nhau trong hierarchy). Quét trực tiếp children thay vì dùng _cells
        // đảm bảo BuildGrid() luôn idempotent dù gọi bao nhiêu lần / lúc nào.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            var oldCell = child.GetComponent<BattleGridCell>();
            if (oldCell != null)
            {
                // PoolingManager.Despawn() bỏ qua (return sớm) nếu object đang inactive —
                // ép về active trước để đảm bảo nó LUÔN được xử lý (push vào pool hoặc
                // Destroy nếu không thuộc pool nào), tránh sót lại cell "mồ côi" inactive
                // vĩnh viễn trong hierarchy qua nhiều lần BuildGrid().
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                PoolingManager.Despawn(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject); // child lạ không phải cell pool (thay đổi thủ công trong Editor)
            }
        }

        _cells     = new BattleGridCell[rows, columns];
        _itemCells = new GridItemCell[rows, columns];
        var template = GetCellTemplate();

        RectTransform parentRT = GetComponent<RectTransform>();
        float totalW = parentRT.rect.width;
        float totalH = parentRT.rect.height;
        float border = 34f;
        float cellW  = ((totalW - 2f * border) - spacing.x * (columns - 1)) / columns;
        float cellH  = ((totalH - 2f * border) - spacing.y * (rows    - 1)) / rows;

        int startRow = (rows    - defaultUnlockRows) / 2;
        int startCol = (columns - defaultUnlockCols) / 2;
        int endRow   = startRow + defaultUnlockRows - 1;
        int endCol   = startCol + defaultUnlockCols - 1;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                // Lấy (hoặc tạo mới nếu pool rỗng) 1 cell từ PoolingManager
                var cell = PoolingManager.Spawn<BattleGridCell>(template, Vector3.zero, Quaternion.identity, transform);
                var cellGO = cell.gameObject;
                cellGO.name = "Cell_" + r + "_" + c;

                // Template nguồn đang SetActive(false); Instantiate lần đầu (chưa từng
                // Despawn để có sẵn trong pool) sẽ giữ nguyên trạng thái inactive đó — ép về true.
                if (!cellGO.activeSelf) cellGO.SetActive(true);

                var rt = cellGO.GetComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(cellW, cellH);
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(
                    border + spacing.x * c + cellW * c,
                   -(border + spacing.y * r + cellH * r));

                var bgImg = cellGO.GetComponent<Image>();
                bgImg.preserveAspect = false;

                bool inZone = (r >= startRow && r <= endRow && c >= startCol && c <= endCol);
                var initState = inZone
                    ? BattleGridCell.CellState.UnlockedEmpty
                    : BattleGridCell.CellState.Locked;

                cell.Init(r, c, bgImg, spriteLocked, spriteUnlocked);
                cell.SetState(initState);

                _cells[r, c] = cell;

                _itemCells[r, c] = new GridItemCell { IsLocked = !inZone, ItemType = GridItemType.None, ItemID = 0, ItemRef = null };
            }
        }

        CellWidth  = cellW;
        CellHeight = cellH;
        Debug.Log("[BattleGridManager] Grid " + columns + "x" + rows + " built (pooled)."
            + " spriteLocked=" + (spriteLocked   != null ? spriteLocked.name   : "NULL")
            + " spriteUnlocked=" + (spriteUnlocked != null ? spriteUnlocked.name : "NULL"));
    }

    /// <summary>
    /// Reset lưới về trạng thái ban đầu (chỉ 3x3 giữa Unlocked, phần còn lại Locked,
    /// không còn ô nào UnlockedFull). Gọi khi bắt đầu 1 trận đấu mới
    /// (VD: BattleManager.StartBattle()) để tránh giữ lại trạng thái lưới của trận trước.
    /// Thực chất chỉ là alias của BuildGrid() — build lại từ đầu (qua Pool, không GC-spike).
    /// </summary>
    public void ResetGrid() => BuildGrid();

    // ── Editor Preview (Gizmos) ──────────────────────────────
    // Ve truoc luoi Grid ngay trong Scene View luc Edit Mode, dua tren CUNG cong thuc
    // toa do/kich thuoc voi BuildGrid() (border=34, spacing, cellW/cellH tinh tu RectTransform
    // hien tai) — KHONG spawn GameObject that, chi la Gizmos nen KHONG anh huong runtime/build
    // (boc trong #if UNITY_EDITOR). O trong vung defaultUnlockCols x defaultUnlockRows (giua bang)
    // to mau xanh (se Unlock san luc BuildGrid()), cac o con lai to mau nhat (Locked).
    // Bat/tat qua showGridGizmos trong Inspector. Tu dong cap nhat khi doi columns/rows/spacing
    // trong Inspector, khong can bam Play hay goi BuildGrid().

    // ── Public API ───────────────────────────────────────────

    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }

    /// <summary>Unlock mot o (Locked → UnlockedEmpty).</summary>
    public void UnlockCell(int row, int col)
    {
        GetCell(row, col)?.Unlock();
        if (IsInBounds(row, col)) _itemCells[row, col].IsLocked = false;
    }

    /// <summary>Dat item vao o da unlock (UnlockedEmpty → UnlockedFull). Khong gan weapon nao (Grid item thuan).</summary>
    public void PlaceItem(int row, int col) => GetCell(row, col)?.PlaceItem();

    public void RemoveItem(int row, int col) => GetCell(row, col)?.RemoveItem();

    /// <summary>
    /// Kiem tra shape co hop le de unlock khong:
    /// 1. Tat ca o trong shape phai la Locked.
    /// 2. It nhat 1 o phai ke (4 huong) voi o da Unlocked.
    /// </summary>
    public bool CanUnlock(int anchorRow, int anchorCol, Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0) return false;

        foreach (var o in offsets)
        {
            var cell = GetCell(anchorRow + o.x, anchorCol + o.y);
            if (cell == null || cell.State != BattleGridCell.CellState.Locked) return false;
        }

        int[] dr = { -1, 1, 0, 0 };
        int[] dc = {  0, 0,-1, 1 };

        foreach (var o in offsets)
        {
            int r = anchorRow + o.x;
            int c = anchorCol + o.y;
            for (int d = 0; d < 4; d++)
            {
                var neighbor = GetCell(r + dr[d], c + dc[d]);
                if (neighbor != null && neighbor.State != BattleGridCell.CellState.Locked)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Unlock tat ca o trong shape.</summary>
    public void UnlockShape(int anchorRow, int anchorCol, Vector2Int[] offsets)
    {
        foreach (var o in offsets)
            UnlockCell(anchorRow + o.x, anchorCol + o.y);
    }

    /// <summary>
    /// Quét TOÀN BỘ lưới để kiểm tra xem shape (offsets) có ÍT NHẤT 1 vị trí anchor
    /// hợp lệ để unlock hay không (dùng CanUnlock() tại từng ô làm anchor).
    /// Dùng để lọc GridItem nào thực sự "đặt được" trên bàn cờ hiện tại — khác với
    /// chỉ đếm số ô trống (CountUnlockedEmpty), vì số ô đủ KHÔNG đảm bảo có vị trí
    /// khớp hình dạng thật sự (ô Locked có thể rải rác không liền khối).
    /// </summary>
    public bool HasValidPlacement(Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0 || _cells == null) return false;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (CanUnlock(r, c, offsets))
                    return true;
            }
        }
        return false;
    }

    // ── Gear Placement (lien ket voi WeaponEntry) ───────────────

    /// <summary>
    /// Kiem tra 1 WeaponEntry co the dat len ban co tai vi tri anchor khong:
    /// tat ca o trong shape cua weapon phai dang UnlockedEmpty (da mo khoa san,
    /// khac voi CanUnlock() von yeu cau Locked).
    /// </summary>
    public bool CanPlaceGear(int anchorRow, int anchorCol, WeaponEntry weapon)
    {
        if (weapon == null || weapon.GridCells == null || weapon.GridCells.Length == 0) return false;

        foreach (var wc in weapon.GridCells)
        {
            var cell = GetCell(anchorRow + wc.gridPosition.x, anchorCol + wc.gridPosition.y);
            if (cell == null || cell.State != BattleGridCell.CellState.UnlockedEmpty) return false;
        }
        return true;
    }

    /// <summary>
    /// Dat 1 WeaponEntry len ban co tai vi tri anchor: danh dau cac BattleGridCell
    /// tuong ung la UnlockedFull (OccupyingWeapon = weapon) VA goi weapon.OccupyCell()
    /// de WeaponEntry tu biet minh dang chiem nhung o nao. Dong thoi ghi du lieu vao
    /// he thong Cells (GridItemCell) va tu dong RefreshGearUnitLinks().
    /// Goi CanPlaceGear() truoc de dam bao hop le.
    /// itemRef: GearItemUI dang goi ham nay (dung lam ItemRef/ItemID trong Cells).
    /// </summary>
    public void PlaceGear(int anchorRow, int anchorCol, WeaponEntry weapon, MonoBehaviour itemRef = null)
    {
        if (weapon == null || weapon.GridCells == null) return;

        int id = itemRef != null ? itemRef.gameObject.GetInstanceID() : 0;

        foreach (var wc in weapon.GridCells)
        {
            int r = anchorRow + wc.gridPosition.x;
            int c = anchorCol + wc.gridPosition.y;

            GetCell(r, c)?.PlaceItem(weapon);
            weapon.OccupyCell(wc.gridPosition);

            if (IsInBounds(r, c))
            {
                _itemCells[r, c].ItemType = GridItemType.Gear;
                _itemCells[r, c].ItemID   = id;
                _itemCells[r, c].ItemRef  = itemRef;
            }
        }

        RefreshGearUnitLinks();
    }

    /// <summary>
    /// Go 1 WeaponEntry khoi ban co tai vi tri anchor: cac BattleGridCell tuong ung
    /// tro ve UnlockedEmpty (OccupyingWeapon = null) VA goi weapon.ReleaseAllCells()
    /// de WeaponEntry giai phong toan bo trang thai chiem o cua no. Dong thoi xoa du
    /// lieu tuong ung trong he thong Cells (chi xoa neu dung ItemID, tranh xoa nham
    /// item khac neu da bi ghi de) va tu dong RefreshGearUnitLinks().
    /// </summary>
    public void RemoveGear(int anchorRow, int anchorCol, WeaponEntry weapon, MonoBehaviour itemRef = null)
    {
        if (weapon == null || weapon.GridCells == null) return;

        int id = itemRef != null ? itemRef.gameObject.GetInstanceID() : 0;

        foreach (var wc in weapon.GridCells)
        {
            int r = anchorRow + wc.gridPosition.x;
            int c = anchorCol + wc.gridPosition.y;
            GetCell(r, c)?.RemoveItem();

            if (IsInBounds(r, c) && (itemRef == null || _itemCells[r, c].ItemID == id))
                _itemCells[r, c].Clear();
        }

        weapon.ReleaseAllCells();
        RefreshGearUnitLinks();
    }

    // ── Unit Placement (lien ket voi MyDuckData / UnitPlayerItemUI) ─

    /// <summary>
    /// Kiem tra 1 Unit (shape rieng, VD 1x2 co dinh cua UnitPlayerItemUI) co the dat
    /// len ban co tai vi tri anchor khong: tat ca o trong shape phai dang UnlockedEmpty.
    /// </summary>
    public bool CanPlaceUnit(int anchorRow, int anchorCol, Vector2Int[] shape)
    {
        if (shape == null || shape.Length == 0) return false;

        foreach (var o in shape)
        {
            var cell = GetCell(anchorRow + o.x, anchorCol + o.y);
            if (cell == null || cell.State != BattleGridCell.CellState.UnlockedEmpty) return false;
        }
        return true;
    }

    /// <summary>
    /// Dat 1 Unit (MyDuckData) len ban co tai vi tri anchor theo shape rieng: danh dau
    /// cac BattleGridCell tuong ung la UnlockedFull (OccupyingUnit = unit). Dong thoi
    /// ghi du lieu vao he thong Cells va tu dong RefreshGearUnitLinks().
    /// Goi CanPlaceUnit() truoc de dam bao hop le.
    /// itemRef: UnitPlayerItemUI dang goi ham nay (dung lam ItemRef/ItemID trong Cells).
    /// </summary>
    public void PlaceUnit(int anchorRow, int anchorCol, MyDuckData unit, Vector2Int[] shape, MonoBehaviour itemRef = null)
    {
        if (shape == null) return;

        int id = itemRef != null ? itemRef.gameObject.GetInstanceID() : 0;

        foreach (var o in shape)
        {
            int r = anchorRow + o.x;
            int c = anchorCol + o.y;
            GetCell(r, c)?.PlaceItem(unit);

            if (IsInBounds(r, c))
            {
                _itemCells[r, c].ItemType = GridItemType.Unit;
                _itemCells[r, c].ItemID   = id;
                _itemCells[r, c].ItemRef  = itemRef;
            }
        }

        RefreshGearUnitLinks();
    }

    /// <summary>
    /// Go 1 Unit khoi ban co tai vi tri anchor theo shape rieng: cac BattleGridCell
    /// tuong ung tro ve UnlockedEmpty (OccupyingUnit = null). Dong thoi xoa du lieu
    /// tuong ung trong he thong Cells (chi xoa neu dung ItemID) va tu dong
    /// RefreshGearUnitLinks().
    /// </summary>
    public void RemoveUnit(int anchorRow, int anchorCol, Vector2Int[] shape, MonoBehaviour itemRef = null)
    {
        if (shape == null) return;

        int id = itemRef != null ? itemRef.gameObject.GetInstanceID() : 0;

        foreach (var o in shape)
        {
            int r = anchorRow + o.x;
            int c = anchorCol + o.y;
            GetCell(r, c)?.RemoveItem();

            if (IsInBounds(r, c) && (itemRef == null || _itemCells[r, c].ItemID == id))
                _itemCells[r, c].Clear();
        }

        RefreshGearUnitLinks();
    }

    // ── Cells System — Links Gear-Unit ──────────────────────────

    private bool IsInBounds(int r, int c) => _itemCells != null && r >= 0 && r < rows && c >= 0 && c < columns;

    /// <summary>
    /// Quét TOÀN BỘ hệ thống Cells (GridItemCell[,]), tìm mọi GearItem đang có ÍT NHẤT 1 ô
    /// liền kề (4 hướng) với 1 UnitItem — gọi GearItemUI.OnConnect() (hiệu ứng "Đã kết nối!")
    /// cho các Gear đó. Được gọi TỰ ĐỘNG mỗi khi Cells thay đổi (PlaceGear/RemoveGear/
    /// PlaceUnit/RemoveUnit), không cần gọi tay từ bên ngoài.
    /// Dùng ItemID (không phải Type+Tier) để nhóm đúng các ô thuộc CÙNG 1 Gear instance,
    /// tránh nhầm 2 Gear khác nhau nhưng cùng loại+tier thành 1.
    /// </summary>
    public void RefreshGearUnitLinks()
    {
        if (_itemCells == null) return;

        int[] dr = { -1, 1, 0, 0 };
        int[] dc = {  0, 0,-1, 1 };

        var linkedGearIDs = new HashSet<int>();
        var gearRefByID   = new Dictionary<int, MonoBehaviour>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var cell = _itemCells[r, c];
                if (cell.ItemType != GridItemType.Gear || cell.ItemRef == null) continue;

                gearRefByID[cell.ItemID] = cell.ItemRef;
                if (linkedGearIDs.Contains(cell.ItemID)) continue;

                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d], nc = c + dc[d];
                    if (!IsInBounds(nr, nc)) continue;
                    if (_itemCells[nr, nc].ItemType == GridItemType.Unit)
                    {
                        linkedGearIDs.Add(cell.ItemID);
                        break;
                    }
                }
            }
        }

        foreach (var id in linkedGearIDs)
            (gearRefByID[id] as GearItemUI)?.OnConnect();
    }

    // ── Helpers ──────────────────────────────────────────────

    public int CountUnlockedEmpty()
    {
        if (_cells == null) return 0;
        int count = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (_cells[r, c] != null && _cells[r, c].State == BattleGridCell.CellState.UnlockedEmpty)
                    count++;
        return count;
    }

}
