using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Render hình dạng shape của GridItem bằng cách tạo các ô nhỏ theo data.gridCells.
/// Gắn vào ShapePreviewRoot (child của ShopItem prefab).
///
/// Pooling: các ô nhỏ (Image) được lấy/trả về qua PoolingManager (Scripts/Tool)
/// thay vì Instantiate/Destroy mỗi lần Draw()/ClearCells() — vì hàm này chạy
/// rất thường xuyên (mỗi lần hover item trong Shop), pooling giúp tránh GC spike.
/// </summary>
public class GridShapePreview : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteCellNormal;   // grid_white (9-slice)
    [SerializeField] private Sprite spriteCellHighlight; // grid_base (highlight xanh/vàng khi hover)

    [Header("Cell Config")]
    [SerializeField] private float cellSize   = 28f;
    [SerializeField] private float cellSpacing = 2f;

    // Cells đang active (đã lấy từ pool)
    private readonly List<Image> _cells = new List<Image>();

    // Template (component) dùng làm "prefab" nguồn cho PoolingManager.Spawn<Image>()
    // (static — dùng chung giữa mọi instance GridShapePreview, vì Shop có nhiều item cùng lúc).
    private static Image _cellTemplate;

    // ─── Public API ───────────────────────────────────────────

    /// <summary>Lấy (hoặc tạo lần đầu) component Image trên template GameObject dùng làm nguồn Pool.</summary>
    private static Image GetCellTemplate()
    {
        if (_cellTemplate != null) return _cellTemplate;

        var go = new GameObject("~GridShapePreviewCell_Template (Pool Source)");
        go.SetActive(false);
        go.AddComponent<RectTransform>();
        _cellTemplate = go.AddComponent<Image>();
        _cellTemplate.raycastTarget = false;
        return _cellTemplate;
    }

    /// <summary>Vẽ shape từ mảng gridCells trong ShopItemData.</summary>
    public void Draw(Vector2Int[] gridCells)
    {
        ClearCells();
        if (gridCells == null || gridCells.Length == 0) return;

        // Tính bounding box để căn giữa
        int minR = int.MaxValue, maxR = int.MinValue;
        int minC = int.MaxValue, maxC = int.MinValue;
        foreach (var cell in gridCells)
        {
            if (cell.x < minR) minR = cell.x;
            if (cell.x > maxR) maxR = cell.x;
            if (cell.y < minC) minC = cell.y;
            if (cell.y > maxC) maxC = cell.y;
        }

        int totalRows = maxR - minR + 1;
        int totalCols = maxC - minC + 1;

        float step  = cellSize + cellSpacing;
        float offX  = -(totalCols - 1) * step * 0.5f;
        float offY  =  (totalRows - 1) * step * 0.5f;

        var template = GetCellTemplate();

        foreach (var cell in gridCells)
        {
            var img = PoolingManager.Spawn<Image>(template, Vector3.zero, Quaternion.identity, transform);
            var go  = img.gameObject;
            go.name = $"Cell_{cell.x}_{cell.y}";

            // Template nguồn đang SetActive(false); Instantiate lần đầu (chưa từng
            // Despawn để có sẵn trong pool) sẽ giữ nguyên trạng thái inactive đó — ép về true.
            if (!go.activeSelf) go.SetActive(true);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(cellSize, cellSize);
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                offX + (cell.y - minC) * step,
                offY - (cell.x - minR) * step
            );

            img.sprite  = spriteCellNormal;
            img.type    = Image.Type.Sliced;
            img.color   = Color.white;
            img.raycastTarget = false;

            _cells.Add(img);
        }
    }

    /// <summary>Đổi màu tất cả cells (dùng khi highlight preview trên BattleGrid).</summary>
    public void SetHighlight(bool on, Color highlightColor)
    {
        foreach (var c in _cells)
        {
            if (on)
            {
                c.sprite = spriteCellHighlight != null ? spriteCellHighlight : c.sprite;
                c.color  = highlightColor;
            }
            else
            {
                c.sprite = spriteCellNormal;
                c.color  = Color.white;
            }
        }
    }

    /// <summary>Trả toàn bộ cell hiện có về Pool (thay vì Destroy).</summary>
    public void ClearCells()
    {
        foreach (var c in _cells)
            if (c != null) PoolingManager.Despawn(c.gameObject);
        _cells.Clear();
    }

    private void OnDestroy() => ClearCells();
}
