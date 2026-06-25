using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MenuTopController - Gắn vào MenuTop trong MenuGame.
/// Xử lý: click btnIcon -> bật PannelSetting, click btnBack -> tắt PannelSetting.
/// </summary>
public class MenuTopController : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Button icon trong MenuTop")]
    [SerializeField] private Button btnIcon;

    [Tooltip("PannelSetting trong MenuGame (cùng cấp với MenuTop)")]
    [SerializeField] private GameObject pannelSetting;

    private void Awake()
    {
        // Đảm bảo PannelSetting tắt khi bắt đầu
        if (pannelSetting != null)
            pannelSetting.SetActive(false);
    }

    private void Start()
    {
        if (btnIcon != null)
            btnIcon.onClick.AddListener(OpenSetting);
        else
            Debug.LogWarning("[MenuTopController] btnIcon chưa được gán!");

        if (pannelSetting == null)
            Debug.LogWarning("[MenuTopController] pannelSetting chưa được gán!");
    }

    /// <summary>
    /// Bật PannelSetting khi click btnIcon.
    /// </summary>
    public void OpenSetting()
    {
        if (pannelSetting != null)
            pannelSetting.SetActive(true);
    }

    private void OnDestroy()
    {
        if (btnIcon != null)
            btnIcon.onClick.RemoveListener(OpenSetting);
    }
}
