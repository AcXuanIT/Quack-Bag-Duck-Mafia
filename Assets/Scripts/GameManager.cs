using UnityEngine;

/// <summary>
/// GameManager - Quản lý các GameObject không phải UI trong scene.
/// Bật/tắt BatteMap GameObject (non-UI) khi vào màn hình chiến đấu.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Non-UI GameObjects ===")]
    [Tooltip("GameObject BatteMap chứa logic game, được bật khi vào Battle")]
    [SerializeField] private GameObject batteMapObject;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Đảm bảo BatteMap tắt khi bắt đầu
        if (batteMapObject != null)
            batteMapObject.SetActive(false);
    }

    /// <summary>
    /// Bật GameObject BatteMap (non-UI) - gọi từ UIGameManager sau LoadMap.
    /// </summary>
    public void EnableBatteMap()
    {
        if (batteMapObject != null)
        {
            batteMapObject.SetActive(true);
            Debug.Log("[GameManager] BatteMap đã được bật.");
        }
        else
        {
            Debug.LogWarning("[GameManager] BatteMap GameObject chưa được gán!");
        }
    }

    /// <summary>
    /// Tắt GameObject BatteMap.
    /// </summary>
    public void DisableBatteMap()
    {
        if (batteMapObject != null)
            batteMapObject.SetActive(false);
    }
}
