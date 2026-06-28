using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đại diện cho một ô trong Battle Grid.
/// Trạng thái: Locked / Unlocked-Empty / Unlocked-Full
/// </summary>
public class BattleGridCell : MonoBehaviour
{
    public enum CellState
    {
        Locked,
        UnlockedEmpty,
        UnlockedFull
    }

    [Header("References")]
    [SerializeField] private Image bgImage;          // nền ô (grid_base)
    [SerializeField] private Image lockOverlay;      // icon khoá
    [SerializeField] private Image itemIcon;         // icon item (icon_gear_shape_solo khi Full)

    [Header("Sprites")]
    [SerializeField] private Sprite spriteUnlocked;  // grid_base
    [SerializeField] private Sprite spriteLocked;    // grid_base (tối màu / overlay)
    [SerializeField] private Sprite spriteItem;      // icon_gear_shape_solo

    [Header("State")]
    [SerializeField] private CellState _state = CellState.Locked;

    public int Row { get; private set; }
    public int Col { get; private set; }
    public CellState State => _state;

    public void Init(int row, int col)
    {
        Row = row;
        Col = col;
        ApplyState(_state);
    }

    public void SetState(CellState newState)
    {
        _state = newState;
        ApplyState(_state);
    }

private void ApplyState(CellState state)
    {
        switch (state)
        {
            case CellState.Locked:
                if (bgImage)     { bgImage.sprite = spriteLocked; bgImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); }
                if (lockOverlay)   lockOverlay.gameObject.SetActive(true);
                if (itemIcon)      itemIcon.gameObject.SetActive(false);
                break;

            case CellState.UnlockedEmpty:
                if (bgImage)     { bgImage.sprite = spriteUnlocked; bgImage.color = Color.white; }
                if (lockOverlay)   lockOverlay.gameObject.SetActive(false);
                // Unlock: luon hien gear icon de len grid_base
                if (itemIcon)      itemIcon.gameObject.SetActive(true);
                break;

            case CellState.UnlockedFull:
                if (bgImage)     { bgImage.sprite = spriteUnlocked; bgImage.color = Color.white; }
                if (lockOverlay)   lockOverlay.gameObject.SetActive(false);
                if (itemIcon)      itemIcon.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>Mở khoá ô này (chuyển về UnlockedEmpty nếu đang Locked)</summary>
    public void Unlock()
    {
        if (_state == CellState.Locked)
            SetState(CellState.UnlockedEmpty);
    }

    /// <summary>Đặt item vào ô (chỉ khi UnlockedEmpty)</summary>
    public void PlaceItem()
    {
        if (_state == CellState.UnlockedEmpty)
            SetState(CellState.UnlockedFull);
    }

    /// <summary>Xoá item khỏi ô (chỉ khi UnlockedFull)</summary>
    public void RemoveItem()
    {
        if (_state == CellState.UnlockedFull)
            SetState(CellState.UnlockedEmpty);
    }
}
