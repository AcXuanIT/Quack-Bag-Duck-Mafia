using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý panel WeaponInfo.
/// Path: UIGame/StartGame/MenuGame/WeaponInfo
/// Icon hiển thị là SpriteTier1 (icon mặc định UIGear).
/// LevelBar và XPBar dùng cùng công thức RectTransform.sizeDelta như LoadingBarController.
/// </summary>
public class WeaponInfoUI : MonoBehaviour
{
    [Header("=== Title ===")]
    public TextMeshProUGUI textWeaponName;  // BGWaeponInfo/Title/textWeaponName
    public Image           weaponIconTitle; // BGWaeponInfo/Title/Icon (icon nhỏ trên title)

    [Header("=== Weapon Icon lớn ===")]
    public Image weaponIcon;               // BGWaeponInfo/WeaponInfo/WeaponIconBG/WeaponIcon

    [Header("=== Level Bar ===")]
    public RectTransform   levelBar;        // BGWaeponInfo/Level/LevelBar   (thanh nền)
    public RectTransform   levelSlice;      // BGWaeponInfo/Level/LevelSlice (thanh fill)
    public TextMeshProUGUI levelText;       // BGWaeponInfo/Level/LevelText

    [Header("=== Damage ===")]
    public TextMeshProUGUI damageCurrent;   // DamageBG/Damagecurrent
    public TextMeshProUGUI damageUpgrade;   // DamageBG/DamageUpgrade

    [Header("=== HP ===")]
    public TextMeshProUGUI hpCurrent;       // HPBG/HPcurrent
    public TextMeshProUGUI hpUpgrade;       // HPBG/HPUpgrade

    [Header("=== XP Bar ===")]
    public RectTransform   xpBar;           // XP/XPBar   (thanh nền)
    public RectTransform   xpSlice;         // XP/XPSlice (thanh fill)
    public TextMeshProUGUI xpText;          // XP/XPText

    [Header("=== Upgrade Button ===")]
    public TextMeshProUGUI priceUpgrade;    // btnUpgrade/PriceUpgeade

    [Header("=== Back Button ===")]
    public Button btnBack;                  // btnBack

    // ── Bar state ──────────────────────────────────────────────
    private float _levelSliceFullWidth;
    private float _xpSliceFullWidth;
    private bool  _initialized;

    private const int MAX_LEVEL = 5;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        InitBars();
        if (btnBack != null) btnBack.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    private void InitBars()
    {
        if (_initialized) return;
        _initialized = true;

        if (levelSlice != null)
        {
            _levelSliceFullWidth          = levelSlice.sizeDelta.x;
            SetPivotLeft(levelSlice, _levelSliceFullWidth);
            levelSlice.sizeDelta          = new Vector2(0f, levelSlice.sizeDelta.y);
        }

        if (xpSlice != null)
        {
            _xpSliceFullWidth             = xpSlice.sizeDelta.x;
            SetPivotLeft(xpSlice, _xpSliceFullWidth);
            xpSlice.sizeDelta             = new Vector2(0f, xpSlice.sizeDelta.y);
        }
    }

    /// <summary>Pivot → (0, 0.5) để bar fill từ trái sang phải (giống LoadingBar).</summary>
    private void SetPivotLeft(RectTransform rt, float fullWidth)
    {
        Vector2 oldPivot = rt.pivot;
        Vector2 newPivot = new Vector2(0f, 0.5f);
        rt.pivot              = newPivot;
        rt.anchoredPosition  += new Vector2((newPivot.x - oldPivot.x) * fullWidth, 0f);
    }

    // ─────────────────────────────────────────────────────────
    /// <summary>
    /// Mở WeaponInfo và điền dữ liệu từ WeaponEntry vào.
    /// Icon luôn là SpriteTier1 (icon mặc định UIGear).
    /// </summary>
    public void Show(WeaponEntry data)
    {
        if (!_initialized) InitBars();
        gameObject.SetActive(true);

        // Title
        if (textWeaponName != null)  textWeaponName.text     = data.Name;
        // Icon title + icon lớn đều dùng SpriteTier1 (UIGear default)
        if (weaponIconTitle != null) weaponIconTitle.sprite   = data.GetUIIcon();
        if (weaponIcon      != null) weaponIcon.sprite        = data.GetUIIcon();

        // Level (1-5)
        if (levelText != null) levelText.text = "Cấp " + data.Level;
        SetBar(levelSlice, _levelSliceFullWidth, (float)data.Level / MAX_LEVEL);

        // Damage
        if (damageCurrent != null) damageCurrent.text  = data.Damage.ToString("0");
        if (damageUpgrade != null) damageUpgrade.text  = (data.Damage * 1.15f).ToString("0");

        // HP
        if (hpCurrent != null) hpCurrent.text = data.HP.ToString("0");
        if (hpUpgrade != null) hpUpgrade.text = (data.HP * 1.10f).ToString("0");

        // XP Bar
        float xpPct = data.XPToNextLevel > 0 ? (float)data.XP / data.XPToNextLevel : 0f;
        SetBar(xpSlice, _xpSliceFullWidth, xpPct);
        if (xpText != null) xpText.text = data.XP + "/" + data.XPToNextLevel;

        // Price
        if (priceUpgrade != null) priceUpgrade.text = data.Coin.ToString();
    }

    /// <summary>Đóng panel WeaponInfo.</summary>
    public void Hide() => gameObject.SetActive(false);

    private void SetBar(RectTransform slice, float fullWidth, float t)
    {
        if (slice == null) return;
        slice.sizeDelta = new Vector2(
            Mathf.Lerp(0f, fullWidth, Mathf.Clamp01(t)),
            slice.sizeDelta.y);
    }
}
