using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tao va quan ly Battle Grid (mac dinh 5 cot x 7 hang).
/// - Mac dinh unlock vung 3x3 chinh giua.
/// - Cau truc cell don gian: moi cell chi co 1 GameObject voi 1 Image (bgImage).
///   Locked        → sprite spriteLocked   (grid_base), an hoan toan binh thuong.
///   UnlockedEmpty → sprite spriteUnlocked (grid_gear_shape_solo).
///   UnlockedFull  → sprite spriteUnlocked (grid_gear_shape_solo).
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

    public int   Rows       => rows;
    public int   Cols       => columns;
    public float CellWidth  { get; private set; }
    public float CellHeight { get; private set; }

    void Awake() => BuildGrid();

    // ── Build ────────────────────────────────────────────────

[ContextMenu("Rebuild Grid")]
    public void BuildGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _cells = new BattleGridCell[rows, columns];

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
                var cellGO = new GameObject("Cell_" + r + "_" + c);
                cellGO.transform.SetParent(transform, false);

                var rt = cellGO.AddComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(cellW, cellH);
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(
                    border + spacing.x * c + cellW * c,
                   -(border + spacing.y * r + cellH * r));

                var bgImg = cellGO.AddComponent<Image>();
                bgImg.preserveAspect = false;

                // Gan component va truyen sprite truc tiep — khong dung reflection
                var cell = cellGO.AddComponent<BattleGridCell>();

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
        Debug.Log("[BattleGridManager] Grid " + columns + "x" + rows + " built."
            + " spriteLocked=" + (spriteLocked   != null ? spriteLocked.name   : "NULL")
            + " spriteUnlocked=" + (spriteUnlocked != null ? spriteUnlocked.name : "NULL"));
    }

    // ── Public API ───────────────────────────────────────────

    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }

    /// <summary>Unlock mot o (Locked → UnlockedEmpty).</summary>
    public void UnlockCell(int row, int col) => GetCell(row, col)?.Unlock();

    /// <summary>Dat item vao o da unlock (UnlockedEmpty → UnlockedFull).</summary>
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

    // ── Helpers ──────────────────────────────────────────────


}
