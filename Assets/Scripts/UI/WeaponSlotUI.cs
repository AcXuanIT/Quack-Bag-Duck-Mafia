using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị 1 ô vũ khí trong danh sách Gear UI.
/// Màu xanh = đã mở khóa, màu xám = chưa mở khóa.
/// </summary>
public class WeaponSlotUI : MonoBehaviour
{
    [Header("Background / Frame")]
    public Image bgImage;
    public Image frameImage;

    [Header("Icon")]
    public Image iconImage;
    public Image lockOverlay;       // overlay tối khi bị khóa
    public Image lockIcon;          // icon ổ khóa

    [Header("Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI coinText;

    [Header("XP Bar")]
    public Slider xpBar;
    public TextMeshProUGUI xpText;

    [Header("Stars (Level visual)")]
    public Image[] starImages;      // 5 sao, fill tùy level

    [Header("Colors")]
    public Color unlockedBgColor  = new Color(0.18f, 0.50f, 0.85f, 1f);  // xanh
    public Color lockedBgColor    = new Color(0.30f, 0.30f, 0.30f, 1f);  // xám
    public Color unlockedFrameColor = new Color(0.25f, 0.75f, 1f, 1f);
    public Color lockedFrameColor   = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color starOnColor  = new Color(1f, 0.85f, 0.20f, 1f);
    public Color starOffColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    public void Bind(WeaponEntry data)
    {
        bool unlocked = !data.IsLocked;

        // --- Background & frame color ---
        if (bgImage)    bgImage.color    = unlocked ? unlockedBgColor  : lockedBgColor;
        if (frameImage) frameImage.color = unlocked ? unlockedFrameColor : lockedFrameColor;

        // --- Icon ---
        if (iconImage)
        {
            iconImage.sprite = data.GetUIIcon();
            //iconImage.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        if (lockOverlay) lockOverlay.gameObject.SetActive(!unlocked);
        if (lockIcon)    lockIcon.gameObject.SetActive(!unlocked);

        // --- Text info ---
        if (nameText)   nameText.text   = data.Name;
        if (levelText)  levelText.text  = unlocked ? $"Lv.{data.Level}" : "???";
        if (damageText) damageText.text = unlocked ? $"{data.GetCurrentDamage():0}" : "-";
        if (hpText)     hpText.text     = unlocked ? $"{data.GetCurrentHP():0}" : "-";
        if (coinText)   coinText.text   = unlocked ? $"{data.Coin}" : "-";

        // --- XP Bar ---
        if (xpBar)
        {
            xpBar.gameObject.SetActive(unlocked);
            if (unlocked && data.XPToNextLevel > 0)
            {
                xpBar.value = (float)data.XP / data.XPToNextLevel;
                if (xpText) xpText.text = $"{data.XP}/{data.XPToNextLevel}";
            }
        }

        // --- Stars ---
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i])
                    starImages[i].color = (unlocked && i < data.Level) ? starOnColor : starOffColor;
            }
        }
    }
}
