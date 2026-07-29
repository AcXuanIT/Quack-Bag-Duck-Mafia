using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mot o trong Battle Grid. Cau truc don gian: chi co 1 Image (bgImage).
/// Trang thai:
///   Locked        — o bi khoa, an hoan toan. Hien khi GridItem hover vao (sprite grid_base).
///   UnlockedEmpty — o da mo, hien sprite grid_gear_shape_solo.
///   UnlockedFull  — o da mo va co item chiem, hien sprite grid_gear_shape_solo.
/// LockOverlay va ItemIcon da bi xoa: khong can thiet vi sprite da the hien du trang thai.
/// </summary>
public class BattleGridCell : MonoBehaviour
{
    public enum CellState { Locked, UnlockedEmpty, UnlockedFull }

    [Header("References")]
    [SerializeField] private Image bgImage;         // Image duy nhat cua cell

    [Header("Sprites")]
    [SerializeField] private Sprite spriteLocked;   // grid_base   — hien khi hover Locked
    [SerializeField] private Sprite spriteUnlocked; // grid_gear_shape_solo — khi da Unlock

    [Header("State")]
    [SerializeField] private CellState _state = CellState.Locked;

    public int       Row   { get; private set; }
    public int       Col   { get; private set; }
    public CellState State => _state;

public void Init(int row, int col, Image image, Sprite locked, Sprite unlocked)
    {
        Row            = row;
        Col            = col;
        bgImage        = image;
        spriteLocked   = locked;
        spriteUnlocked = unlocked;
        ApplyVisual(_state);
    }

    public void SetState(CellState newState)
    {
        _state = newState;
        ApplyVisual(_state);
    }

    /// <summary>Restore visual dung theo state hien tai.</summary>
    public void RestoreVisual() => ApplyVisual(_state);

    private void ApplyVisual(CellState state)
    {
        switch (state)
        {
            case CellState.Locked:
                // An hoan toan — chi hien khi GridItem hover (SetHighlightColor)
                if (bgImage) bgImage.gameObject.SetActive(false);
                break;

            case CellState.UnlockedEmpty:
                if (bgImage)
                {
                    bgImage.gameObject.SetActive(true);
                    bgImage.sprite = spriteUnlocked;
                    bgImage.color  = Color.white;
                }
                break;

            case CellState.UnlockedFull:
                if (bgImage)
                {
                    bgImage.gameObject.SetActive(true);
                    bgImage.sprite = spriteUnlocked;
                    bgImage.color  = Color.white;
                }
                break;
        }
    }

    // ── Public API ───────────────────────────────────────────

    /// <summary>Mo khoa o (Locked → UnlockedEmpty).</summary>
    public void Unlock()
    {
        if (_state == CellState.Locked)
            SetState(CellState.UnlockedEmpty);
    }

    /// <summary>Dat item vao o da mo (UnlockedEmpty → UnlockedFull).</summary>
    public void PlaceItem()
    {
        if (_state == CellState.UnlockedEmpty)
            SetState(CellState.UnlockedFull);
    }

    /// <summary>Xoa item (UnlockedFull → UnlockedEmpty).</summary>
    public void RemoveItem()
    {
        if (_state == CellState.UnlockedFull)
            SetState(CellState.UnlockedEmpty);
    }

    // ── Highlight helpers (dung boi GridShopItemUI khi drag) ─

    /// <summary>Hien cell Locked tam thoi voi mau preview khi GridItem hover vao.</summary>
    public void SetHighlightColor(Color color)
    {
        if (bgImage)
        {
            bgImage.sprite = spriteLocked;
            bgImage.gameObject.SetActive(true);
            bgImage.color = color;
        }
    }

    /// <summary>An lai cell Locked ve trang thai vo hinh (goi khi GridItem roi khoi).</summary>
    public void HideLockedPreview()
    {
        if (_state == CellState.Locked)
            ApplyVisual(CellState.Locked);
    }
}
