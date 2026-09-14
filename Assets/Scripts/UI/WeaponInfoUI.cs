using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponInfoUI : MonoBehaviour
{
    [Header("=== Title ===")]
    public TextMeshProUGUI textWeaponName;  
    public Image           weaponIconTitle; 

    [Header("=== Weapon Icon lớn ===")]
    public Image weaponIcon;             
    public TextMeshProUGUI weaponLevel;        

    [Header("=== Level Bar ===")]
    public RectTransform   levelBar;      
    public RectTransform   levelSlice;     
    public TextMeshProUGUI levelText;      

    [Header("=== Damage ===")]
    public TextMeshProUGUI damageCurrent;  
    public TextMeshProUGUI damageUpgrade;  

    [Header("=== HP ===")]
    public TextMeshProUGUI hpCurrent;      
    public TextMeshProUGUI hpUpgrade;     

    [Header("=== XP Bar ===")]
    public RectTransform   xpBar;          
    public RectTransform   xpSlice;        
    public TextMeshProUGUI xpText;         

    [Header("=== Upgrade Button ===")]
    public TextMeshProUGUI priceUpgrade;    
    public Button          btnUpdate;       

    [Header("=== Back Button ===")]
    public Button btnBack;                 

    private float _levelSliceFullWidth;
    private float _xpSliceFullWidth;
    private bool  _initialized;

    private WeaponEntry _currentData;

    private const int MAX_LEVEL = 5;

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

    private void SetPivotLeft(RectTransform rt, float fullWidth)
    {
        Vector2 oldPivot = rt.pivot;
        Vector2 newPivot = new Vector2(0f, 0.5f);
        rt.pivot              = newPivot;
        rt.anchoredPosition  += new Vector2((newPivot.x - oldPivot.x) * fullWidth, 0f);
    }


    public void Show(WeaponEntry data)
    {
        if (!_initialized) InitBars();
        gameObject.SetActive(true);

        _currentData = data;
        if (data == null) return;

        if (textWeaponName != null)  textWeaponName.text     = data.Name;
        if (weaponIconTitle != null) weaponIconTitle.sprite   = data.GetUIIcon();
        if (weaponIcon      != null) weaponIcon.sprite        = data.GetUIIcon();

        if (levelText != null) levelText.text = "Cấp " + data.Level;
        if (weaponLevel != null) weaponLevel.text = "Cấp " + data.Level;
        SetBar(levelSlice, _levelSliceFullWidth, (float)data.Level / MAX_LEVEL);

        if (damageCurrent != null) damageCurrent.text  = data.GetCurrentDamage().ToString("0");
        if (damageUpgrade != null) damageUpgrade.text  = "+" + (data.GetNextLevelDamage()-data.GetCurrentDamage()).ToString("0");

        if (hpCurrent != null) hpCurrent.text =data.GetCurrentHP().ToString("0");
        if (hpUpgrade != null) hpUpgrade.text = "+" + (data.GetNextLevelHP()-data.GetCurrentHP()).ToString("0");

        int xpNeeded = data.GetCurrentXPToNextLevel();
        float xpPct = xpNeeded > 0 ? (float)data.XP / xpNeeded : 0f;
        SetBar(xpSlice, _xpSliceFullWidth, xpPct);
        if (xpText != null) xpText.text = data.XP + "/" + xpNeeded;

        if (priceUpgrade != null) priceUpgrade.text = data.Coin.ToString();

        RefreshUpdateButton();
    }

    public void Hide() => gameObject.SetActive(false);

    private void SetBar(RectTransform slice, float fullWidth, float t)
    {
        if (slice == null) return;
        slice.sizeDelta = new Vector2(
            Mathf.Lerp(0f, fullWidth, Mathf.Clamp01(t)),
            slice.sizeDelta.y);
    }

    #region Update (Level Up) Button

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

    private void OnClickUpdate()
    {
        if (_currentData == null) return;

        if (WeaponManager.Instance == null)
        {
            return;
        }
        if (GameManager.Instance == null)
        {
            return;
        }

        int coin = GameManager.Instance.Coin;
        bool success = WeaponManager.Instance.TryLevelUp(_currentData.ID, ref coin);
        if (!success)
        {
            RefreshUpdateButton();
            return;
        }

        GameManager.Instance.SetCoin(coin);
        Show(_currentData);
    }

    #endregion
}
