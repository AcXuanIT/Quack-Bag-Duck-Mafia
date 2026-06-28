using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Render hình dạng shape của GridItem bằng cách tạo các ô nhỏ theo data.gridCells.
/// Gắn vào ShapePreviewRoot (child của ShopItem prefab).
/// </summary>
public class GridShapePreview : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteCellNormal;   // grid_white (9-slice)
    [SerializeField] private Sprite spriteCellHighlight; // grid_base (highlight xanh/vàng khi hover)

    [Header("Cell Config")]
    [SerializeField] private float cellSize   = 28f;
    [SerializeField] private float cellSpacing = 2f;

    // Pool cells đã tạo
    private readonly List<Image> _cells = new List<Image>();

    // ─── Public API ───────────────────────────────────────────

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

        foreach (var cell in gridCells)
        {
            var go = new GameObject($"Cell_{cell.x}_{cell.y}");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(cellSize, cellSize);
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                offX + (cell.y - minC) * step,
                offY - (cell.x - minR) * step
            );

            var img = go.AddComponent<Image>();
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

    public void ClearCells()
    {
        foreach (var c in _cells)
            if (c != null) Destroy(c.gameObject);
        _cells.Clear();
    }
}
