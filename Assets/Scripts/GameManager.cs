using UnityEngine;

/// <summary>
/// GameManager - Root manager, quản lý các sub-manager và GameObject non-UI.
/// Singleton. Tất cả sub-manager là con của GameObject này.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Sub Managers ===")]
    [Tooltip("WeaponManager child — quản lý toàn bộ dữ liệu vũ khí")]
    [SerializeField] public WeaponManager weaponManager;

    [Header("=== Non-UI GameObjects ===")]
    [Tooltip("GameObject BatteMap chứa logic game, được bật khi vào Battle")]
    [SerializeField] private GameObject batteMapObject;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (batteMapObject != null)
            batteMapObject.SetActive(false);
    }

    public void EnableBatteMap()
    {
        if (batteMapObject != null) batteMapObject.SetActive(true);
        else Debug.LogWarning("[GameManager] BatteMap chưa được gán!");
    }

    public void DisableBatteMap()
    {
        if (batteMapObject != null) batteMapObject.SetActive(false);
    }
}
