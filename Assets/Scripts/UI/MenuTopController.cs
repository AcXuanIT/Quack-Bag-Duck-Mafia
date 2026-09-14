using UnityEngine;
using UnityEngine.UI;

public class MenuTopController : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Button btnIcon;
    [SerializeField] private GameObject pannelSetting;

    private void Awake()
    {
        if (pannelSetting != null)
            pannelSetting.SetActive(false);
    }

    private void Start()
    {
        if (btnIcon != null)
            btnIcon.onClick.AddListener(OpenSetting);
    }

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
