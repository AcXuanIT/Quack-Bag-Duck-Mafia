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
    public TextMeshProUGUI weaponLevel;        // BGWaeponInfo/WeaponInfo/WeaponIconBG/WeaponName

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
    public Button          btnUpdate;       // btnUpgrade — bấm để nâng Level (cần đủ XP + Coin)

    [Header("=== Back Button ===")]
    public Button btnBack;                  // btnBack

    // ── Bar state ──────────────────────────────────────────────
    private float _levelSliceFullWidth;
    private float _xpSliceFullWidth;
    private bool  _initialized;

    // ── Data đang hiển thị (dùng lại khi bấm Update) ────────────
    private WeaponEntry _currentData;

    private const int MAX_LEVEL = 5;

    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        InitBars();
        if (btnBack   != null) btnBack.onClick.AddListener(Hide);
        if (btnUpdate != null) btnUpdate.onClick.AddListener(OnClickUpdate);
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

        _currentData = data;
        if (data == null) return;

        // Title
        if (textWeaponName != null)  textWeaponName.text     = data.Name;
        // Icon title + icon lớn đều dùng SpriteTier1 (UIGear default)
        if (weaponIconTitle != null) weaponIconTitle.sprite   = data.GetUIIcon();
        if (weaponIcon      != null) weaponIcon.sprite        = data.GetUIIcon();

        // Level (1-5)
        if (levelText != null) levelText.text = "Cấp " + data.Level;
        if (weaponLevel != null) weaponLevel.text = "Cấp " + data.Level;
        SetBar(levelSlice, _levelSliceFullWidth, (float)data.Level / MAX_LEVEL);

        // Damage
        if (damageCurrent != null) damageCurrent.text  = data.GetCurrentDamage().ToString("0");
        if (damageUpgrade != null) damageUpgrade.text  = "+" + (data.GetNextLevelDamage()-data.GetCurrentDamage()).ToString("0");

        // HP
        if (hpCurrent != null) hpCurrent.text =data.GetCurrentHP().ToString("0");
        if (hpUpgrade != null) hpUpgrade.text = "+" + (data.GetNextLevelHP()-data.GetCurrentHP()).ToString("0");

        // XP Bar — ngưỡng XP cần để lên Level tiếp theo lấy theo Level HIỆN TẠI (mảng
        // XPToNextLevel[5], xem WeaponData.cs — GetCurrentXPToNextLevel() trả về 0 nếu đã max Lv5).
        int xpNeeded = data.GetCurrentXPToNextLevel();
        float xpPct = xpNeeded > 0 ? (float)data.XP / xpNeeded : 0f;
        SetBar(xpSlice, _xpSliceFullWidth, xpPct);
        if (xpText != null) xpText.text = data.XP + "/" + xpNeeded;

        // Price
        if (priceUpgrade != null) priceUpgrade.text = data.Coin.ToString();

        // Nút Update — bật/tắt theo điều kiện Level chưa max + đủ XP + đủ Coin
        RefreshUpdateButton();
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

    // ─────────────────────────────────────────────────────────
    #region Update (Level Up) Button

    /// <summary>
    /// Bật/tắt nút Update theo điều kiện: weapon chưa max Level (5), đã đủ XP theo ngưỡng
    /// GetCurrentXPToNextLevel() của Level hiện tại, VÀ Player đủ Coin (data.Coin — giá nâng
    /// cấp hiện tại, xem WeaponData.cs) để trả cho lần nâng cấp này.
    /// </summary>
    private void RefreshUpdateButton()
    {
        if (btnUpdate == null || _currentData == null) return;

        bool notMaxLevel = _currentData.Level < MAX_LEVEL;
        int  xpNeeded    = _currentData.GetCurrentXPToNextLevel();
        bool enoughXP    = notMaxLevel && xpNeeded > 0 && _currentData.XP >= xpNeeded;

        int  playerCoin  = GameManager.Instance != null ? GameManager.Instance.Coin : 0;
        bool enoughCoin  = playerCoin >= _currentData.Coin;

        btnUpdate.interactable = notMaxLevel && enoughXP && enoughCoin;
    }

    /// <summary>
    /// Bấm nút Update: gọi WeaponManager.TryLevelUp() (đã kiểm tra đủ XP + Coin, trừ Coin,
    /// trừ XP, tăng Level, tăng chỉ số bên trong WeaponEntry — xem WeaponManager.cs), sau đó
    /// lưu lại số Coin còn dư vào GameData qua GameManager.SetCoin(), rồi show lại weaponData
    /// (cùng 1 instance WeaponEntry đã được WeaponManager sửa trực tiếp) để cập nhật UI.
    /// </summary>
    private void OnClickUpdate()
    {
        if (_currentData == null) return;

        if (WeaponManager.Instance == null)
        {
            Debug.LogWarning("[WeaponInfoUI] Thiếu WeaponManager.Instance, không thể Update!");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[WeaponInfoUI] Thiếu GameManager.Instance, không thể Update!");
            return;
        }

        int coin = GameManager.Instance.Coin;
        bool success = WeaponManager.Instance.TryLevelUp(_currentData.ID, ref coin);
        if (!success)
        {
            Debug.Log("[WeaponInfoUI] Update thất bại — chưa đủ XP/Coin hoặc weapon đã max Level.");
            RefreshUpdateButton();
            return;
        }

        // Lưu lại số Coin còn dư sau khi WeaponManager đã trừ vào biến tạm ở trên.
        GameManager.Instance.SetCoin(coin);

        // Show lại weaponData (đã được WeaponManager cập nhật Level/XP/Coin/stats trực tiếp).
        Show(_currentData);
    }

    #endregion
}
