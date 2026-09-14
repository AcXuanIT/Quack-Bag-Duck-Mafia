using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridShapePreview : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite spriteCellNormal;  
    [SerializeField] private Sprite spriteCellHighlight; 

    [Header("Cell Config")]
    [SerializeField] private float cellSize   = 28f;
    [SerializeField] private float cellSpacing = 2f;

    // Cells đang active
    private readonly List<Image> _cells = new List<Image>();


    private static Image _cellTemplate;

    private static Image GetCellTemplate()
    {
        if (_cellTemplate != null) return _cellTemplate;

        GameObject go = new GameObject("GridShapePreviewCell_Template");
        go.SetActive(false);
        go.AddComponent<RectTransform>();
        _cellTemplate = go.AddComponent<Image>();
        _cellTemplate.raycastTarget = false;
        return _cellTemplate;
    }

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
            Image img = PoolingManager.Spawn<Image>(template, Vector3.zero, Quaternion.identity, transform);
            GameObject go  = img.gameObject;
            go.name = $"Cell_{cell.x}_{cell.y}";

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

    public void SetHighlight(bool on, Color highlightColor)
    {
        foreach (Image c in _cells)
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
        foreach (Image c in _cells)
            if (c != null) PoolingManager.Despawn(c.gameObject);
        _cells.Clear();
    }

    private void OnDestroy() => ClearCells();
}
