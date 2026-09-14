using UnityEngine;
using UnityEngine.UI;

public class PannelSettingController : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Button btnBack;

    private void Start()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(CloseSetting);
    }
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
