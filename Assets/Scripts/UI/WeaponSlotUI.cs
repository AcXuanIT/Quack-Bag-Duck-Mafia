using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("Background / Frame")]
    public Image bgImage;
    public Image frameImage;

    [Header("Icon")]
    public Image iconImage;
    public Image lockOverlay;      
    public Image lockIcon;     
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
    public Image[] starImages;     

    [Header("Colors")]
    public Color unlockedBgColor  = new Color(0.18f, 0.50f, 0.85f, 1f);  
    public Color lockedBgColor    = new Color(0.30f, 0.30f, 0.30f, 1f);  
    public Color unlockedFrameColor = new Color(0.25f, 0.75f, 1f, 1f);
    public Color lockedFrameColor   = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color starOnColor  = new Color(1f, 0.85f, 0.20f, 1f);
    public Color starOffColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    public void Bind(WeaponEntry data)
    {
        bool unlocked = !data.IsLocked;

        if (bgImage)    bgImage.color    = unlocked ? unlockedBgColor  : lockedBgColor;
        if (frameImage) frameImage.color = unlocked ? unlockedFrameColor : lockedFrameColor;

        if (iconImage)
        {
            iconImage.sprite = data.GetUIIcon();
        }
        if (lockOverlay) lockOverlay.gameObject.SetActive(!unlocked);
        if (lockIcon)    lockIcon.gameObject.SetActive(!unlocked);

        if (nameText)   nameText.text   = data.Name;
        if (levelText)  levelText.text  = unlocked ? $"Lv.{data.Level}" : "???";
        if (damageText) damageText.text = unlocked ? $"{data.GetCurrentDamage():0}" : "-";
        if (hpText)     hpText.text     = unlocked ? $"{data.GetCurrentHP():0}" : "-";
        if (coinText)   coinText.text   = unlocked ? $"{data.Coin}" : "-";

        if (xpBar)
        {
            xpBar.gameObject.SetActive(unlocked);
            int xpNeeded = data.GetCurrentXPToNextLevel();
            if (unlocked && xpNeeded > 0)
            {
                xpBar.value = (float)data.XP / xpNeeded;
                if (xpText) xpText.text = $"{data.XP}/{xpNeeded}";
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
