using UnityEngine;
using TMPro;

/// <summary>
/// Quản lý bảng thông tin (Info) hiển thị khi người chơi nhấn giữ vào 1 GearItem hoặc UnitItem
/// trong Shop/Grid. Đường dẫn trong scene: UIGame/BatteGame/UIBatteMap/Top/Info.
///
/// Singleton — gán vào GameObject "Top" (LUÔN active), KHÔNG gán vào "Info" hay bất kỳ object
/// con nào của nó, vì Info sẽ thường xuyên bị SetActive(false) (mặc định ẩn, chỉ hiện lúc giữ
/// item) — nếu script nằm trên chính Info, Awake() (nơi Singleton đăng ký Instance) sẽ KHÔNG
/// chạy trong lúc Info đang tắt, khiến Instance null ngay khi cần dùng sớm.
///
/// Cách dùng: GearItemUI/UnitPlayerItemUI gọi ItemInfoPanel.Instance.ShowInfoForGear(...)/
/// ShowInfoForUnit(...) trong OnPointerDown(), và ItemInfoPanel.Instance.HideInfo() trong
/// OnPointerUp()/OnEndDrag(). KHÔNG cần kéo-thả gán tham chiếu ItemInfoPanel vào đâu cả nhờ
/// Singleton<T> tự FindObjectOfType (kể cả khi Info đang inactive) ở lần gọi Instance đầu tiên.
///
/// Giữ NGUYÊN cấu trúc UI đã setup sẵn trong prefab (Component/textName, textLevel,
/// displayDamage/icon+textDamage, displayTimeDelay/icon+textTimeDelay, displayHp/icon+textHp)
/// — script này CHỈ đổi giá trị text và bật/tắt displayDamage, displayTimeDelay tuỳ loại item,
/// không dựng lại hay đổi bố cục.
/// </summary>
public class ItemInfoPanel : Singleton<ItemInfoPanel>
{
    [Header("Info Root (Info) — bật/tắt khi giữ/thả item")]
    [SerializeField] private GameObject infoRoot;

    [Header("Chung cho Gear + UnitDuck")]
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textLevel;

    [Header("Chỉ Gear — Damage")]
    [SerializeField] private GameObject         displayDamage;
    [SerializeField] private TextMeshProUGUI    textDamage;

    [Header("Chỉ Gear — Time Delay")]
    [SerializeField] private GameObject         displayTimeDelay;
    [SerializeField] private TextMeshProUGUI    textTimeDelay;

    [Header("Chung cho Gear + UnitDuck — HP")]
    [SerializeField] private GameObject         displayHp;
    [SerializeField] private TextMeshProUGUI    textHp;

    protected override void Awake()
    {
        base.Awake();
        // An san luc khoi dong scene (Top luon active nen Awake nay chac chan chay).
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    /// <summary>
    /// Nhận dữ liệu từ 1 GearItem và hiển thị Info Panel — dùng đủ 5 thành phần
    /// (textName, textLevel, displayDamage, displayTimeDelay, displayHp).
    /// Gọi từ GearItemUI.OnPointerDown().
    /// </summary>
    public void ShowInfoForGear(string name, int level, float damage, float timeDelay, float hp)
    {
        if (infoRoot != null) infoRoot.SetActive(true);

        SetText(textName, name);
        SetText(textLevel, "Cấp " + level);

        SetDisplay(displayDamage, textDamage, FormatNumber(damage), true);
        SetDisplay(displayTimeDelay, textTimeDelay, FormatNumber(timeDelay), true);
        SetDisplay(displayHp, textHp, FormatNumber(hp), true);
    }

    /// <summary>
    /// Nhận dữ liệu từ 1 UnitDuck và hiển thị Info Panel — chỉ dùng 3 thành phần
    /// (textName, textLevel, displayHp); displayDamage/displayTimeDelay bị ẩn vì
    /// UnitDuck không có 2 chỉ số này.
    /// Gọi từ UnitPlayerItemUI.OnPointerDown().
    /// </summary>
    public void ShowInfoForUnit(string name, int level, float hp)
    {
        if (infoRoot != null) infoRoot.SetActive(true);

        SetText(textName, name);
        SetText(textLevel, "Cấp " + level);

        SetDisplay(displayDamage, textDamage, null, false);
        SetDisplay(displayTimeDelay, textTimeDelay, null, false);
        SetDisplay(displayHp, textHp, FormatNumber(hp), true);
    }

    /// <summary>
    /// Ẩn Info Panel — gọi khi người chơi thả tay ra khỏi item
    /// (GearItemUI/UnitPlayerItemUI.OnPointerUp() hoặc OnEndDrag()).
    /// </summary>
    public void HideInfo()
    {
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    // ─── Helpers ────────────────────────────────────────────

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text == null) return;
        text.text = value;
    }

    private void SetDisplay(GameObject displayObj, TextMeshProUGUI text, string value, bool show)
    {
        if (displayObj != null) displayObj.SetActive(show);
        if (show) SetText(text, value);
    }

    private string FormatNumber(float v)
    {
        return Mathf.Approximately(v % 1f, 0f) ? v.ToString("0") : v.ToString("0.#");
    }
}
