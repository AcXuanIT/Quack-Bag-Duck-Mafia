using UnityEngine;

/// <summary>
/// UIGameManager - Script trung tâm quản lý toàn bộ UI trong game.
/// Gắn vào GameObject UIGame trong scene.
/// Điều phối các UI sub-controller như LoadingStartGameUI.
/// </summary>
public class UIGameManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private LoadingStartGameUI loadingStartGameUI;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Kích hoạt loading screen khi game bắt đầu
        if (loadingStartGameUI != null)
        {
            loadingStartGameUI.StartLoadingSequence();
        }
        else
        {
            Debug.LogWarning("[UIGameManager] LoadingStartGameUI chưa được gán trong Inspector!");
        }
    }
}
