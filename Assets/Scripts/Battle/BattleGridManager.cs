using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tạo và quản lý Battle Grid (mặc định 5 cột x 7 hàng).
/// - Mặc định unlock vùng 3x3 chính giữa.
/// - GridItem chỉ được đặt vào ô Locked kề với ô đã Unlocked.
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
    [SerializeField] private Sprite spriteGridBase;
    [SerializeField] private Sprite spriteGearIcon;

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
                bgImg.sprite = spriteGridBase;

                // Lock Overlay
                var lockGO  = new GameObject("LockOverlay");
                lockGO.transform.SetParent(cellGO.transform, false);
                var lockRT  = lockGO.AddComponent<RectTransform>();
                lockRT.anchorMin = Vector2.zero; lockRT.anchorMax = Vector2.one;
                lockRT.offsetMin = Vector2.zero; lockRT.offsetMax = Vector2.zero;
                var lockImg = lockGO.AddComponent<Image>();
                lockImg.color = new Color(0f, 0f, 0f, 0.45f);

                // Item Icon
                var iconGO  = new GameObject("ItemIcon");
                iconGO.transform.SetParent(cellGO.transform, false);
                var iconRT  = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = spriteGearIcon;
                iconImg.preserveAspect = false;
                iconGO.SetActive(false);

                var cell = cellGO.AddComponent<BattleGridCell>();
                SetField(cell, "bgImage",        bgImg);
                SetField(cell, "lockOverlay",     lockImg);
                SetField(cell, "itemIcon",        iconImg);
                SetField(cell, "spriteUnlocked",  spriteGridBase);
                SetField(cell, "spriteLocked",    spriteGridBase);

                bool inZone = (r >= startRow && r <= endRow && c >= startCol && c <= endCol);
                cell.Init(r, c);
                cell.SetState(inZone
                    ? BattleGridCell.CellState.UnlockedEmpty
                    : BattleGridCell.CellState.Locked);

                _cells[r, c] = cell;
            }
        }

        CellWidth  = cellW;
        CellHeight = cellH;
        Debug.Log("[BattleGridManager] Grid " + columns + "x" + rows + " built. CellSize=" + cellW.ToString("F1") + "x" + cellH.ToString("F1") + ".");
    }

    // ── Public API ───────────────────────────────────────────

    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }

    /// <summary>Unlock một ô (Locked → UnlockedEmpty).</summary>
    public void UnlockCell(int row, int col) => GetCell(row, col)?.Unlock();

    /// <summary>Đặt item vào ô đã unlock (UnlockedEmpty → UnlockedFull).</summary>
    public void PlaceItem(int row, int col) => GetCell(row, col)?.PlaceItem();

    public void RemoveItem(int row, int col) => GetCell(row, col)?.RemoveItem();

    /// <summary>
    /// Kiểm tra shape (anchorRow/Col + offsets) có hợp lệ để unlock không:
    /// 1. Tất cả ô trong shape phải là Locked.
    /// 2. Ít nhất 1 ô trong shape phải kề (4 hướng) với ô UnlockedEmpty hoặc UnlockedFull.
    /// </summary>
    public bool CanUnlock(int anchorRow, int anchorCol, Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0) return false;

        // Rule 1: tất cả ô đích phải là Locked
        foreach (var o in offsets)
        {
            var cell = GetCell(anchorRow + o.x, anchorCol + o.y);
            if (cell == null || cell.State != BattleGridCell.CellState.Locked) return false;
        }

        // Rule 2: ít nhất 1 ô đích kề với ô đã Unlocked
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

    /// <summary>Unlock tất cả ô trong shape (không kiểm tra — gọi CanUnlock trước).</summary>
    public void UnlockShape(int anchorRow, int anchorCol, Vector2Int[] offsets)
    {
        foreach (var o in offsets)
            UnlockCell(anchorRow + o.x, anchorCol + o.y);
    }

    // ── Helpers ──────────────────────────────────────────────

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }
}
