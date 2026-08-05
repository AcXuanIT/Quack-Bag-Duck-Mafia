using UnityEngine;
using UnityEngine.UI;

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

    private BattleGridCell[,] _cells;

    // Template (component) dùng làm "prefab" nguồn cho PoolingManager.Spawn<BattleGridCell>()
    // (GameObject của nó không parent vào transform của grid, để không bị BuildGrid() dọn nhầm).
    private BattleGridCell _cellTemplate;

    public int   Rows       => rows;
    public int   Cols       => columns;
    public float CellWidth  { get; private set; }
    public float CellHeight { get; private set; }

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

        _cells = new BattleGridCell[rows, columns];
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

    // ── Public API ───────────────────────────────────────────

    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }

    /// <summary>Unlock mot o (Locked → UnlockedEmpty).</summary>
    public void UnlockCell(int row, int col) => GetCell(row, col)?.Unlock();

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
    /// de WeaponEntry tu biet minh dang chiem nhung o nao.
    /// Goi CanPlaceGear() truoc de dam bao hop le.
    /// </summary>
    public void PlaceGear(int anchorRow, int anchorCol, WeaponEntry weapon)
    {
        if (weapon == null || weapon.GridCells == null) return;

        foreach (var wc in weapon.GridCells)
        {
            int r = anchorRow + wc.gridPosition.x;
            int c = anchorCol + wc.gridPosition.y;

            GetCell(r, c)?.PlaceItem(weapon);
            weapon.OccupyCell(wc.gridPosition);
        }
    }

    /// <summary>
    /// Go 1 WeaponEntry khoi ban co tai vi tri anchor: cac BattleGridCell tuong ung
    /// tro ve UnlockedEmpty (OccupyingWeapon = null) VA goi weapon.ReleaseAllCells()
    /// de WeaponEntry giai phong toan bo trang thai chiem o cua no.
    /// </summary>
    public void RemoveGear(int anchorRow, int anchorCol, WeaponEntry weapon)
    {
        if (weapon == null || weapon.GridCells == null) return;

        foreach (var wc in weapon.GridCells)
        {
            int r = anchorRow + wc.gridPosition.x;
            int c = anchorCol + wc.gridPosition.y;
            GetCell(r, c)?.RemoveItem();
        }

        weapon.ReleaseAllCells();
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
    /// cac BattleGridCell tuong ung la UnlockedFull (OccupyingUnit = unit).
    /// Goi CanPlaceUnit() truoc de dam bao hop le.
    /// </summary>
    public void PlaceUnit(int anchorRow, int anchorCol, MyDuckData unit, Vector2Int[] shape)
    {
        if (shape == null) return;

        foreach (var o in shape)
        {
            int r = anchorRow + o.x;
            int c = anchorCol + o.y;
            GetCell(r, c)?.PlaceItem(unit);
        }
    }

    /// <summary>
    /// Go 1 Unit khoi ban co tai vi tri anchor theo shape rieng: cac BattleGridCell
    /// tuong ung tro ve UnlockedEmpty (OccupyingUnit = null).
    /// </summary>
    public void RemoveUnit(int anchorRow, int anchorCol, Vector2Int[] shape)
    {
        if (shape == null) return;

        foreach (var o in shape)
        {
            int r = anchorRow + o.x;
            int c = anchorCol + o.y;
            GetCell(r, c)?.RemoveItem();
        }
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
