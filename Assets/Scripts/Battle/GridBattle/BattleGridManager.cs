using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GridItemType
{
    None,
    Gear,
    Unit
}

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
    [SerializeField] private Sprite spriteLocked; 
    [SerializeField] private Sprite spriteUnlocked;

    [Header("Cell Prefab")]
    [SerializeField] private GameObject cellPrefab;

    [Header("Editor Preview")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private bool showGizmoLabels = true;

    private BattleGridCell[,] _cells;

    private GridItemCell[,] _itemCells;

    private BattleGridCell _cellTemplate;

    public int   Rows       => rows;
    public int   Cols       => columns;
    public float CellWidth  { get; private set; }
    public float CellHeight { get; private set; }

    public float SpacingX => spacing.x;
    public float SpacingY => spacing.y;

    void Awake() => BuildGrid();

    // ── Build ─
    private BattleGridCell GetCellTemplate()
    {
        if (_cellTemplate != null) return _cellTemplate;

        var go = new GameObject("~BattleGridCell_Template");
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
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            var oldCell = child.GetComponent<BattleGridCell>();
            if (oldCell != null)
            {
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                PoolingManager.Despawn(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject); 
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
                var cell = PoolingManager.Spawn<BattleGridCell>(template, Vector3.zero, Quaternion.identity, transform);
                var cellGO = cell.gameObject;
                cellGO.name = "Cell_" + r + "_" + c;


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

        if (BattleManager.Instance != null)
            BattleManager.Instance.SetPower(0);
    }


    public void ResetGrid() => BuildGrid();


    public BattleGridCell GetCell(int row, int col)
    {
        if (_cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row, col];
    }


    public void UnlockCell(int row, int col)
    {
        GetCell(row, col)?.Unlock();
        if (IsInBounds(row, col)) _itemCells[row, col].IsLocked = false;
    }

    public void PlaceItem(int row, int col) => GetCell(row, col)?.PlaceItem();

    public void RemoveItem(int row, int col) => GetCell(row, col)?.RemoveItem();

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

    public void UnlockShape(int anchorRow, int anchorCol, Vector2Int[] offsets)
    {
        foreach (var o in offsets)
            UnlockCell(anchorRow + o.x, anchorCol + o.y);
    }

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
        (itemRef as GearItemUI)?.SetLinkedUnits(null);
        RefreshGearUnitLinks();
    }

    // ── Unit Placement
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

    // ── Cells System — Links Gear-Unit ───

    private bool IsInBounds(int r, int c) => _itemCells != null && r >= 0 && r < rows && c >= 0 && c < columns;

    public void RefreshGearUnitLinks()
    {
        if (_itemCells == null) return;

        int[] dr = { -1, 1, 0, 0 };
        int[] dc = {  0, 0,-1, 1 };

        var unitsByGearID = new Dictionary<int, List<UnitPlayerItemUI>>();
        var gearRefByID   = new Dictionary<int, GearItemUI>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var cell = _itemCells[r, c];
                var gear = cell.ItemType == GridItemType.Gear ? cell.ItemRef as GearItemUI : null;
                if (gear == null) continue;

                gearRefByID[cell.ItemID] = gear;
                if (!unitsByGearID.TryGetValue(cell.ItemID, out var list))
                {
                    list = new List<UnitPlayerItemUI>();
                    unitsByGearID[cell.ItemID] = list;
                }

                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d], nc = c + dc[d];
                    if (!IsInBounds(nr, nc)) continue;
                    var neighbor = _itemCells[nr, nc];
                    if (neighbor.ItemType == GridItemType.Unit
                        && neighbor.ItemRef is UnitPlayerItemUI unit
                        && !list.Contains(unit))
                    {
                        list.Add(unit);
                    }
                }
            }
        }

        foreach (var kv in gearRefByID)
        {
            var units = unitsByGearID.TryGetValue(kv.Key, out var l) ? l : new List<UnitPlayerItemUI>();
            kv.Value.SetLinkedUnits(units);
            if (units.Count > 0) kv.Value.OnConnect();
        }

        RecalculateTotalPower(gearRefByID, unitsByGearID);
    }

    private void RecalculateTotalPower(Dictionary<int, GearItemUI> gearRefByID, Dictionary<int, List<UnitPlayerItemUI>> unitsByGearID)
    {
        int totalPower = 0;

        foreach (var kv in gearRefByID)
        {
            var gear = kv.Value;
            if (gear == null || gear.Weapon == null) continue;

            int unitCount = unitsByGearID.TryGetValue(kv.Key, out var units) ? units.Count : 0;
            if (unitCount <= 0) continue;

            totalPower += gear.Weapon.GetCurrentPower() * unitCount;
        }

        if (BattleManager.Instance != null)
            BattleManager.Instance.SetPower(totalPower);
    }
    public List<UnitPlayerItemUI> GetLinkedUnits(GearItemUI gear)
    {
        var result = new List<UnitPlayerItemUI>();
        if (gear == null || _itemCells == null || !gear.IsPlacedOnGrid) return result;

        int gearId = gear.gameObject.GetInstanceID();
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = {  0, 0,-1, 1 };

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var cell = _itemCells[r, c];
                if (cell.ItemType != GridItemType.Gear || cell.ItemID != gearId) continue;

                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d], nc = c + dc[d];
                    if (!IsInBounds(nr, nc)) continue;
                    var neighbor = _itemCells[nr, nc];
                    if (neighbor.ItemType == GridItemType.Unit
                        && neighbor.ItemRef is UnitPlayerItemUI unit
                        && !result.Contains(unit))
                    {
                        result.Add(unit);
                    }
                }
            }
        }
        return result;
    }

    // ── Helpers ────
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
