using UnityEngine;
using UnityEngine.UI;


public static class ShopItemSizing
{
    public const float CellSize = 87.5f;
    public const float CellGap  = 4f;

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
