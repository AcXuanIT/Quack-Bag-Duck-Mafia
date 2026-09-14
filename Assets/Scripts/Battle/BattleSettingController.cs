using UnityEngine;
using UnityEngine.UI;

public class BattleSettingController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingPanel;   
    [Header("Buttons")]
    [SerializeField] private Button btnPause;           
    [SerializeField] private Button btnContinue;        
    [SerializeField] private Button btnContinue2;
    [SerializeField] private Button btnBackMenu;     

    [Header("Back Menu References")]
    [SerializeField] private Transform componentContainer; 

    private void Awake()
    {
        if (settingPanel != null) settingPanel.SetActive(false);

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

    public void BackToMenu()
    {
        Time.timeScale = 1f;

        if (BattleManager.Instance != null)
            BattleManager.Instance.ReturnToMenu();

        // An Setting
        if (settingPanel != null) settingPanel.SetActive(false);

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
