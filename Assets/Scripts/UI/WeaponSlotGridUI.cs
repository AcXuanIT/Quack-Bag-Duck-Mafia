using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào mỗi Pistol_Slot trong Grid của SectionCurrent.
/// Hiển thị: Icon (SpriteTier1), LevelText, XP Bar (Bar + Slice).
/// Khi click sẽ mở WeaponInfoUI và truyền dữ liệu vào.
/// </summary>
[RequireComponent(typeof(Button))]
public class WeaponSlotGridUI : MonoBehaviour
{
    [Header("Icon")]
    public Image iconWaepon;            // child: IconWaepon

    [Header("Level")]
    public TextMeshProUGUI levelText;   // child: LevelText

    [Header("XP Bar (RectTransform.sizeDelta như LoadingBar)")]
    public RectTransform   xpBar;       // child: XP/Bar   — thanh nền đầy
    public RectTransform   xpSlice;     // child: XP/Slice — thanh fill
    public TextMeshProUGUI xpText;      // child: XP/XPText

    // ─── Private ─────────────────────────────────────────────
    private WeaponEntry  _data;
    private WeaponInfoUI _weaponInfoUI;
    private float        _sliceFullWidth;

    private void Awake()
    {
        if (xpSlice == null) return;

        _sliceFullWidth = xpSlice.sizeDelta.x;

        // Pivot → (0, 0.5) để thanh fill mở từ trái sang phải (giống LoadingBar)
        Vector2 oldPivot    = xpSlice.pivot;
        Vector2 newPivot    = new Vector2(0f, 0.5f);
        float   pivotDeltaX = (newPivot.x - oldPivot.x) * _sliceFullWidth;
        xpSlice.pivot              = newPivot;
        xpSlice.anchoredPosition  += new Vector2(pivotDeltaX, 0f);
        xpSlice.sizeDelta          = new Vector2(0f, xpSlice.sizeDelta.y);
    }

    /// <summary>
    /// Được gọi bởi GearPanelUI để khởi tạo slot với data và ref đến WeaponInfoUI.
    /// Icon luôn dùng SpriteTier1 (icon mặc định UIGear).
    /// </summary>
    public void Bind(WeaponEntry data, WeaponInfoUI weaponInfoUI)
    {
        _data          = data;
        _weaponInfoUI  = weaponInfoUI;

        // Icon: SpriteTier1 là icon mặc định UIGear
        if (iconWaepon != null)
            iconWaepon.sprite = data.GetUIIcon();

        // Level (1-5)
        if (levelText != null)
            levelText.text = "Cấp " + data.Level;

        // XP Bar
        float xpPct = data.XPToNextLevel > 0 ? (float)data.XP / data.XPToNextLevel : 0f;
        SetXPBar(xpPct);

        if (xpText != null)
            xpText.text = data.XP + "/" + data.XPToNextLevel;

        // Click listener
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void SetXPBar(float t)
    {
        if (xpSlice == null) return;
        xpSlice.sizeDelta = new Vector2(
            Mathf.Lerp(0f, _sliceFullWidth, Mathf.Clamp01(t)),
            xpSlice.sizeDelta.y);
    }

    private void OnClick()
    {
        if (_weaponInfoUI != null && _data != null)
            _weaponInfoUI.Show(_data);
    }
}
