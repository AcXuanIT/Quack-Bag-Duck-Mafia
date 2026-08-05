using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mot o trong Battle Grid. Cau truc don gian: chi co 1 Image (bgImage).
/// Trang thai:
///   Locked        — o bi khoa, an hoan toan. Hien khi GridItem hover vao (sprite grid_base).
///   UnlockedEmpty — o da mo, hien sprite grid_gear_shape_solo.
///   UnlockedFull  — o da mo va co item chiem, hien sprite grid_gear_shape_solo.
/// LockOverlay va ItemIcon da bi xoa: khong can thiet vi sprite da the hien du trang thai.
///
/// Lien ket voi Gear (WeaponEntry):
///   Khi 1 o UnlockedFull la do 1 Gear (WeaponEntry) chiem — chu khong phai do
///   1 Grid ShopItem unlock don thuan — cell se giu tham chieu OccupyingWeapon
///   de biet "o nay dang thuoc ve weapon nao". Dung boi BattleGridManager.PlaceGear/RemoveGear.
///
/// Lien ket voi Unit (MyDuckData):
///   Tuong tu Gear, khi 1 o UnlockedFull la do 1 UnitPlayerItemUI (MyDuckData) chiem,
///   cell se giu tham chieu OccupyingUnit. Dung boi BattleGridManager.PlaceUnit/RemoveUnit.
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

    /// <summary>Weapon (Gear) dang chiem o nay, null neu o trong hoac chi la Grid item.</summary>
    public WeaponEntry OccupyingWeapon { get; private set; }

    /// <summary>Unit (UnitPlayerItemUI) dang chiem o nay, null neu o trong hoac chi la Grid item/Gear.</summary>
    public MyDuckData OccupyingUnit { get; private set; }

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

    /// <summary>
    /// Dat item vao o da mo (UnlockedEmpty → UnlockedFull).
    /// Truyen weapon (tuy chon) neu o nay dang bi 1 Gear chiem —
    /// dung khi Gear duoc dat len grid (khac voi Grid ShopItem chi unlock don thuan).
    /// </summary>
    public void PlaceItem(WeaponEntry weapon = null)
    {
        if (_state == CellState.UnlockedEmpty)
        {
            OccupyingWeapon = weapon;
            SetState(CellState.UnlockedFull);
        }
    }

    /// <summary>
    /// Dat 1 Unit (MyDuckData) vao o da mo (UnlockedEmpty → UnlockedFull).
    /// Dung khi UnitPlayerItemUI duoc dat len grid (tuong tu PlaceItem(WeaponEntry) cho Gear).
    /// </summary>
    public void PlaceItem(MyDuckData unit)
    {
        if (_state == CellState.UnlockedEmpty)
        {
            OccupyingUnit = unit;
            SetState(CellState.UnlockedFull);
        }
    }

    /// <summary>Xoa item (UnlockedFull → UnlockedEmpty), giai phong luon tham chieu weapon/unit (neu co).</summary>
    public void RemoveItem()
    {
        if (_state == CellState.UnlockedFull)
        {
            OccupyingWeapon = null;
            OccupyingUnit   = null;
            SetState(CellState.UnlockedEmpty);
        }
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
