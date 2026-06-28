using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tạo và quản lý Battle Grid 5 cột x 7 hàng trong BGGrid.
/// - Mặc định unlock vùng 3x3 chính giữa.
/// - Mỗi ô dùng sprite grid_base làm nền.
/// - Khi unlock + full: hiển thị icon_gear_shape_solo.
/// </summary>
public class BattleGridManager : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 7;
    [SerializeField] private Vector2 spacing = new Vector2(4f, 4f);

    [Header("Default Unlock Zone (center 3x3)")]
    [SerializeField] private int defaultUnlockCols = 3;
    [SerializeField] private int defaultUnlockRows = 3;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteGridBase;         // grid_base
    [SerializeField] private Sprite spriteGearIcon;         // icon_gear_shape_solo

    [Header("Cell Prefab (auto-built if null)")]
    [SerializeField] private GameObject cellPrefab;

    // Runtime grid
    private BattleGridCell[,] _cells;

    void Awake()
    {
        BuildGrid();
    }

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
        float innerW = totalW - 2f * border;
        float innerH = totalH - 2f * border;

        float cellW = (innerW - spacing.x * (columns - 1)) / columns;
        float cellH = (innerH - spacing.y * (rows - 1)) / rows;

        int startRow = (rows - defaultUnlockRows) / 2;
        int startCol = (columns - defaultUnlockCols) / 2;
        int endRow = startRow + defaultUnlockRows - 1;
        int endCol = startCol + defaultUnlockCols - 1;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                GameObject cellGO = new GameObject($"Cell_{r}_{c}");
                cellGO.transform.SetParent(transform, false);

                RectTransform rt = cellGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellW, cellH);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);

                float posX = border + spacing.x * c + cellW * c;
                float posY = -(border + spacing.y * r + cellH * r);
                rt.anchoredPosition = new Vector2(posX, posY);

                Image bgImg = cellGO.AddComponent<Image>();
                bgImg.sprite = spriteGridBase;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;

                // Lock Overlay
                GameObject lockGO = new GameObject("LockOverlay");
                lockGO.transform.SetParent(cellGO.transform, false);
                RectTransform lockRT = lockGO.AddComponent<RectTransform>();
                lockRT.anchorMin = Vector2.zero;
                lockRT.anchorMax = Vector2.one;
                lockRT.offsetMin = Vector2.zero;
                lockRT.offsetMax = Vector2.zero;
                Image lockImg = lockGO.AddComponent<Image>();
                lockImg.color = new Color(0f, 0f, 0f, 0.5f);

                // Item Icon - khop voi kich thuoc grid_base (offset = 0)
                GameObject iconGO = new GameObject("ItemIcon");
                iconGO.transform.SetParent(cellGO.transform, false);
                RectTransform iconRT = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;
                Image iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = spriteGearIcon;
                iconImg.preserveAspect = false;
                iconGO.SetActive(false);

                BattleGridCell cell = cellGO.AddComponent<BattleGridCell>();
                SetPrivateField(cell, "bgImage", bgImg);
                SetPrivateField(cell, "lockOverlay", lockImg);
                SetPrivateField(cell, "itemIcon", iconImg);
                SetPrivateField(cell, "spriteUnlocked", spriteGridBase);
                SetPrivateField(cell, "spriteLocked", spriteGridBase);
                SetPrivateField(cell, "spriteItem", spriteGearIcon);

                bool inDefaultZone = (r >= startRow && r <= endRow && c >= startCol && c <= endCol);
                BattleGridCell.CellState initState = inDefaultZone
                    ? BattleGridCell.CellState.UnlockedEmpty
                    : BattleGridCell.CellState.Locked;

                cell.Init(r, c);
                cell.SetState(initState);

                _cells[r, c] = cell;
            }
        }

        Debug.Log($"[BattleGridManager] Grid {columns}x{rows} built. CellSize={cellW:F2}x{cellH:F2}.");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }

    // --- Public API ---

    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }

    public void UnlockCell(int row, int col)
    {
        GetCell(row, col)?.Unlock();
    }

    public void PlaceItem(int row, int col)
    {
        GetCell(row, col)?.PlaceItem();
    }

    public void RemoveItem(int row, int col)
    {
        GetCell(row, col)?.RemoveItem();
    }
}
