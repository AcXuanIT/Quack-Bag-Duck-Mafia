using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gan vao Batte/Setting.
/// Dung Awake de AddListener vi Setting co the dang inactive khi Start chay.
///
/// BackToMenu() dung chung cho CA 2 truong hop:
///   1. Nguoi choi bam btnBackMenu trong Pause Setting giua tran dau.
///   2. Nguoi choi bam nut tren PanelWin/PanelLose khi tran dau da ket thuc (duoc gan qua
///      Inspector onClick cua nut do, tro thang toi BackToMenu() - xem UIWin/Button va
///      UILose/Button).
/// Truoc khi tat UI/BatteMap, LUON goi BattleManager.Instance.ReturnToMenu() truoc tien de
/// don sach TOAN BO du lieu/vat the cua tran dau (UnitDuck, EnemyDuck, Grid, Gear/Unit da dat
/// tren Grid, HP MyTeam) - tranh du lieu cu bi de len khi choi lai/qua man tiep theo.
/// </summary>
public class BattleSettingController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingPanel;   // chinh GO nay (Batte/Setting)

    [Header("Buttons")]
    [SerializeField] private Button btnPause;           // UIBatteMap/Top/btnPause
    [SerializeField] private Button btnContinue;        // Setting/btnContinue
    [SerializeField] private Button btnContinue2;
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
        if (btnContinue2 != null) btnContinue2.onClick.AddListener(CloseSetting);
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

    /// <summary>
    /// Ket thuc van dau va quay ve MenuGame. Goi tu btnBackMenu (Pause) HOAC tu nut tren
    /// PanelWin/PanelLose (gan truc tiep qua Inspector onClick).
    /// </summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;

        // 0. Don sach TOAN BO du lieu/vat the cua tran dau hien tai (UnitDuck, EnemyDuck, Grid,
        //    Gear/Unit da dat tren Grid, HP MyTeam...) truoc khi rroi khoi man Battle.
        if (BattleManager.Instance != null)
            BattleManager.Instance.ReturnToMenu();

        // 1. An Setting
        if (settingPanel != null) settingPanel.SetActive(false);

        // 2. Tat UIBatteMap (BatteGame)
        if (uiBatteMap != null) uiBatteMap.SetActive(false);

        // 3. Tat BatteMap non-UI
        if (batteMapObject != null) batteMapObject.SetActive(false);

        // 4. Xoa tat ca item con sot trong Component container (GridShopItemUI spawned) - da
        //    duoc BattleManager.ReturnToMenu() xoa het roi nhung giu lai buoc nay de an toan
        //    (idempotent) neu componentContainer con item nao khac khong thuoc quan ly Battle.
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
