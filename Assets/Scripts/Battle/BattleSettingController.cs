using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gan vao Batte/Setting.
/// Dung Awake de AddListener vi Setting co the dang inactive khi Start chay.
///
/// BackToMenu() dung chung cho CA 3 truong hop (tat ca deu CUNG duoc gan them
/// UIGameManager.OnBackToStartGameClicked() qua Inspector onClick - xem
/// UIWin/Button, UILose/Button, va Setting/btnBackMenu):
///   1. Nguoi choi bam btnBackMenu trong Pause Setting giua tran dau.
///   2. Nguoi choi bam nut tren PanelWin/PanelLose khi tran dau da ket thuc.
///
/// QUAN TRONG: BackToMenu() CHI lo don dep DU LIEU tran dau (BattleManager.ReturnToMenu(),
/// component con sot, Time.timeScale, an Setting panel). KHONG duoc tat/bat UI BatteMap/MenuGame
/// truc tiep o day nua - viec do da chuyen het sang UIGameManager.OnBackToStartGameClicked() ->
/// LoadMapAnimator.PlayReverse(), de dam bao BatteMap chi bien mat / MenuGame chi xuat hien DUNG
/// luc LoadMap da che kin man hinh (giua animation). Neu tat/bat UI ngay trong BackToMenu() (chay
/// dong bo, tuc thi) thi MenuGame se hien ra NGAY LAP TUC truoc khi LoadMap kip chay, pha vo hieu
/// ung chuyen canh (bug da gap: nhan btnBackMenu thay MenuGame bat truoc khi LoadMap chay).
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
    [SerializeField] private Transform componentContainer; // Batte/Button/Component - xoa items

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
    /// Don dep DU LIEU tran dau va tat Setting panel. Goi tu btnBackMenu (Pause) HOAC tu nut
    /// tren PanelWin/PanelLose (gan truc tiep qua Inspector onClick).
    /// KHONG tat/bat BatteMap/MenuGame o day - xem UIGameManager.OnBackToStartGameClicked()
    /// (cung duoc gan tren cac nut nay) lo phan do, dong bo voi animation LoadMap.
    /// </summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;

        // Don sach TOAN BO du lieu/vat the cua tran dau hien tai (UnitDuck, EnemyDuck, Grid,
        // Gear/Unit da dat tren Grid, HP MyTeam...) truoc khi rroi khoi man Battle.
        if (BattleManager.Instance != null)
            BattleManager.Instance.ReturnToMenu();

        // An Setting
        if (settingPanel != null) settingPanel.SetActive(false);

        // Xoa tat ca item con sot trong Component container (GridShopItemUI spawned) - da
        // duoc BattleManager.ReturnToMenu() xoa het roi nhung giu lai buoc nay de an toan
        // (idempotent) neu componentContainer con item nao khac khong thuoc quan ly Battle.
        if (componentContainer != null)
        {
            for (int i = componentContainer.childCount - 1; i >= 0; i--)
                Destroy(componentContainer.GetChild(i).gameObject);
        }
    }

    private void OnDestroy()
    {
        if (btnPause    != null) btnPause.onClick.RemoveListener(OpenSetting);
        if (btnContinue != null) btnContinue.onClick.RemoveListener(CloseSetting);
        if (btnBackMenu != null) btnBackMenu.onClick.RemoveListener(BackToMenu);
    }
}
