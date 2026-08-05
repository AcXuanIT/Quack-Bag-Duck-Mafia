using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nguồn DUY NHẤT cho tỷ lệ kích thước 1 ô (cell) dùng chung giữa 3 loại Shop Item:
///   - GridShopItemUI (shape từ ShopItemData.gridCells)
///   - GearItemUI     (shape từ WeaponEntry.GridCells — mỗi Weapon 1 shape khác nhau)
///   - UnitPlayerItemUI (shape cố định 1 ô — dùng chung cho MỌI Unit)
///
/// Đồng bộ CellSize/CellGap ở đây đảm bảo 1 "ô" trong GridItem/GearItem/UnitItem
/// luôn có cùng kích thước vật lý — item nào chiếm nhiều ô hơn sẽ to hơn theo
/// đúng cùng 1 tỷ lệ, không bị lệch giữa 3 loại prefab.
/// </summary>
public static class ShopItemSizing
{
    public const float CellSize = 56f;
    public const float CellGap  = 4f;

    /// <summary>Tính kích thước (width, height) cho 1 shape gồm nhiều ô (offset dạng [row,col]).</summary>
    public static Vector2 ComputeSize(Vector2Int[] cells)
    {
        int cols = 1, rows = 1;

        if (cells != null && cells.Length > 0)
        {
            int minC = cells[0].y, maxC = cells[0].y;
            int minR = cells[0].x, maxR = cells[0].x;
            foreach (var c in cells)
            {
                if (c.y < minC) minC = c.y; if (c.y > maxC) maxC = c.y;
                if (c.x < minR) minR = c.x; if (c.x > maxR) maxR = c.x;
            }
            cols = maxC - minC + 1;
            rows = maxR - minR + 1;
        }

        float w = cols * CellSize + (cols - 1) * CellGap;
        float h = rows * CellSize + (rows - 1) * CellGap;
        return new Vector2(w, h);
    }

    /// <summary>Áp kích thước tính từ shape vào RectTransform + LayoutElement (nếu có) của item.</summary>
    public static void ApplySize(RectTransform rt, LayoutElement layoutElement, Vector2Int[] cells)
    {
        if (rt == null) return;

        Vector2 size = ComputeSize(cells);
        rt.sizeDelta = size;

        if (layoutElement != null)
        {
            layoutElement.minWidth = size.x;
            layoutElement.minHeight = size.y;
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
        }
    }
}
