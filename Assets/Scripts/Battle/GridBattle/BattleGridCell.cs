using UnityEngine;
using UnityEngine.UI;

public class BattleGridCell : MonoBehaviour
{
    public enum CellState { Locked, UnlockedEmpty, UnlockedFull }

    [Header("References")]
    [SerializeField] private Image bgImage;  

    [Header("Sprites")]
    [SerializeField] private Sprite spriteLocked;  
    [SerializeField] private Sprite spriteUnlocked; 

    [Header("State")]
    [SerializeField] private CellState _state = CellState.Locked;

    public int       Row   { get; private set; }
    public int       Col   { get; private set; }
    public CellState State => _state;

    public WeaponEntry OccupyingWeapon { get; private set; }

    public MyDuckData OccupyingUnit { get; private set; }

    public MonoBehaviour OccupyingItemUI { get; private set; }

    public void SetOccupyingItemUI(MonoBehaviour ui) => OccupyingItemUI = ui;

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

    public void RestoreVisual() => ApplyVisual(_state);

    private void ApplyVisual(CellState state)
    {
        switch (state)
        {
            case CellState.Locked:
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

    // ── Public API ─
    public void Unlock()
    {
        if (_state == CellState.Locked)
            SetState(CellState.UnlockedEmpty);
    }

    public void PlaceItem(WeaponEntry weapon = null)
    {
        if (_state == CellState.UnlockedEmpty)
        {
            OccupyingWeapon = weapon;
            SetState(CellState.UnlockedFull);
        }
    }
    public void PlaceItem(MyDuckData unit)
    {
        if (_state == CellState.UnlockedEmpty)
        {
            OccupyingUnit = unit;
            SetState(CellState.UnlockedFull);
        }
    }

    public void RemoveItem()
    {
        if (_state == CellState.UnlockedFull)
        {
            OccupyingWeapon = null;
            OccupyingUnit   = null;
            OccupyingItemUI = null;
            SetState(CellState.UnlockedEmpty);
        }
    }

    public void SetHighlightColor(Color color)
    {
        if (bgImage)
        {
            bgImage.sprite = spriteLocked;
            bgImage.gameObject.SetActive(true);
            bgImage.color = color;
        }
    }

    public void HideLockedPreview()
    {
        if (_state == CellState.Locked)
            ApplyVisual(CellState.Locked);
    }


    public void SetUnlockedHighlight(Color color)
    {
        if (bgImage && _state != CellState.Locked)
        {
            bgImage.sprite = spriteUnlocked;
            bgImage.gameObject.SetActive(true);
            bgImage.color = color;
        }
    }
}
