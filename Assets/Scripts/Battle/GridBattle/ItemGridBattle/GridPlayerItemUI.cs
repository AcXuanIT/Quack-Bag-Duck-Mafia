using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grid item đại diện cho 1 unit player trong Shop/Battle.
/// BG đổi màu theo tier (0=Default, 1=Blue, 2=Purple, 3=Gold).
/// Icon hiển thị sprite unit.
/// </summary>
public class GridPlayerItemUI : MonoBehaviour
{
    public enum PlayerTier { Default = 0, Blue = 1, Purple = 2, Gold = 3 }

    [Header("References")]
    [SerializeField] public Image bgImage;
    [SerializeField] public Image iconImage;

    [Header("Tier Colors")]
    [SerializeField] private Color colorDefault = new Color(0.55f, 0.55f, 0.55f, 1f);   // xam mac dinh
    [SerializeField] private Color colorBlue    = new Color(0.25f, 0.55f, 1.00f, 1f);   // xanh duong
    [SerializeField] private Color colorPurple  = new Color(0.65f, 0.25f, 1.00f, 1f);   // tim
    [SerializeField] private Color colorGold    = new Color(1.00f, 0.78f, 0.10f, 1f);   // vang

    // ──────────────────────────────────────────────
    public void Setup(Sprite unitIcon, PlayerTier tier)
    {
        SetTier(tier);
        SetIcon(unitIcon);
    }

    public void SetTier(PlayerTier tier)
    {
        if (bgImage == null) return;
        bgImage.color = tier switch
        {
            PlayerTier.Blue   => colorBlue,
            PlayerTier.Purple => colorPurple,
            PlayerTier.Gold   => colorGold,
            _                 => colorDefault,
        };
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;
        iconImage.sprite  = icon;
        iconImage.enabled = icon != null;
    }
}
