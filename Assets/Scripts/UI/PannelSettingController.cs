using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PannelSettingController - Gắn vào PannelSetting trong MenuGame.
/// Xử lý: click btnBack -> tắt PannelSetting.
/// </summary>
public class PannelSettingController : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Button Back trong PannelSetting")]
    [SerializeField] private Button btnBack;

    private void Start()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(CloseSetting);
        else
            Debug.LogWarning("[PannelSettingController] btnBack chưa được gán!");
    }

    /// <summary>
    /// Tắt PannelSetting khi click btnBack.
    /// </summary>
    public void CloseSetting()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (btnBack != null)
            btnBack.onClick.RemoveListener(CloseSetting);
    }
}
