using UnityEngine;
using TMPro;


public class ItemInfoPanel : Singleton<ItemInfoPanel>
{
    [Header("Info Root")]
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
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    public void ShowInfoForGear(string name, int level, float damage, float timeDelay, float hp)
    {
        if (infoRoot != null) infoRoot.SetActive(true);

        SetText(textName, name);
        SetText(textLevel, "Cấp " + level);

        SetDisplay(displayDamage, textDamage, FormatNumber(damage), true);
        SetDisplay(displayTimeDelay, textTimeDelay, FormatNumber(timeDelay), true);
        SetDisplay(displayHp, textHp, FormatNumber(hp), true);
    }
    public void ShowInfoForUnit(string name, int level, float hp)
    {
        if (infoRoot != null) infoRoot.SetActive(true);

        SetText(textName, name);
        SetText(textLevel, "Cấp " + level);

        SetDisplay(displayDamage, textDamage, null, false);
        SetDisplay(displayTimeDelay, textTimeDelay, null, false);
        SetDisplay(displayHp, textHp, FormatNumber(hp), true);
    }

    public void HideInfo()
    {
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    // ─── Helpers ────
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
