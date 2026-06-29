using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gan vao Batte/Setting.
/// Dung Awake de AddListener vi Setting co the dang inactive khi Start chay.
/// </summary>
public class BattleSettingController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingPanel;   // chinh GO nay (Batte/Setting)

    [Header("Buttons")]
    [SerializeField] private Button btnPause;           // UIBatteMap/Top/btnPause
    [SerializeField] private Button btnContinue;        // Setting/btnContinue
    [SerializeField] private Button btnBackMenu;        // Setting/btnBackMenu

    [Header("Back Menu References")]
    [SerializeField] private GameObject uiBatteMap;    // UIBatteGame - an di
    [SerializeField] private GameObject batteMapObject; // BatteMap (non-UI) - tat di
    [SerializeField] private Transform  componentContainer; // Batte/Button/Component - xoa items
    [SerializeField] private GameObject menuGame;       // StartGame/MenuGame - bat len

    // Dung Awake: chay du GO active hay khong
    private void Awake()
    {
        // Setting an luc dau
        if (settingPanel != null) settingPanel.SetActive(false);

        // Gan event ngay trong Awake - tranh truong hop Start khong chay vi GO inactive
        if (btnPause    != null) btnPause.onClick.AddListener(OpenSetting);
        if (btnContinue != null) btnContinue.onClick.AddListener(CloseSetting);
        if (btnBackMenu != null) btnBackMenu.onClick.AddListener(BackToMenu);
    }

    public void OpenSetting()
    {
        if (settingPanel != null) settingPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseSetting()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;

        // 1. An Setting
        if (settingPanel != null) settingPanel.SetActive(false);

        // 2. Tat UIBatteMap (BatteGame)
        if (uiBatteMap != null) uiBatteMap.SetActive(false);

        // 3. Tat BatteMap non-UI
        if (batteMapObject != null) batteMapObject.SetActive(false);

        // 4. Xoa tat ca item trong Component container (GridShopItemUI spawned)
        if (componentContainer != null)
        {
            for (int i = componentContainer.childCount - 1; i >= 0; i--)
                Destroy(componentContainer.GetChild(i).gameObject);
        }

        // 5. Bat MenuGame
        if (menuGame != null) menuGame.SetActive(true);
    }

    private void OnDestroy()
    {
        if (btnPause    != null) btnPause.onClick.RemoveListener(OpenSetting);
        if (btnContinue != null) btnContinue.onClick.RemoveListener(CloseSetting);
        if (btnBackMenu != null) btnBackMenu.onClick.RemoveListener(BackToMenu);
    }
}
