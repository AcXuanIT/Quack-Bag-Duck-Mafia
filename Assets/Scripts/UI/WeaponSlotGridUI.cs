using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class WeaponSlotGridUI : MonoBehaviour
{
    [Header("Icon")]
    public Image iconWaepon;           
    [Header("Level")]
    public TextMeshProUGUI levelText;   

    [Header("XP Bar")]
    public RectTransform   xpBar;      
    public RectTransform   xpSlice;     
    public TextMeshProUGUI xpText;      

    private WeaponEntry  _data;
    private WeaponInfoUI _weaponInfoUI;
    private float        _sliceFullWidth;

    private void Awake()
    {
        if (xpSlice == null) return;

        _sliceFullWidth = xpSlice.sizeDelta.x;

        Vector2 oldPivot    = xpSlice.pivot;
        Vector2 newPivot    = new Vector2(0f, 0.5f);
        float   pivotDeltaX = (newPivot.x - oldPivot.x) * _sliceFullWidth;
        xpSlice.pivot              = newPivot;
        xpSlice.anchoredPosition  += new Vector2(pivotDeltaX, 0f);
        xpSlice.sizeDelta          = new Vector2(0f, xpSlice.sizeDelta.y);
    }

    public void Bind(WeaponEntry data, WeaponInfoUI weaponInfoUI)
    {
        _data          = data;
        _weaponInfoUI  = weaponInfoUI;

        if (iconWaepon != null)
            iconWaepon.sprite = data.GetUIIcon();

        if (levelText != null)
            levelText.text = "Cấp " + data.Level;

        int xpNeeded = data.GetCurrentXPToNextLevel();
        float xpPct = xpNeeded > 0 ? (float)data.XP / xpNeeded : 0f;
     
        SetXPBar(xpPct);

        if (xpText != null)
            xpText.text = data.XP + "/" + xpNeeded;


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
