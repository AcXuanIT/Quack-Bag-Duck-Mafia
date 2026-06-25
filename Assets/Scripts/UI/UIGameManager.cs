using UnityEngine;

/// <summary>
/// UIGameManager - Script trung tâm quản lý toàn bộ UI trong game.
/// Gắn vào GameObject UIGame trong scene.
/// Điều phối các UI sub-controller.
/// </summary>
public class UIGameManager : MonoBehaviour
{
    [Header("=== Loading ===")]
    [SerializeField] private LoadingStartGameUI loadingStartGameUI;

    [Header("=== Menu Navigation ===")]
    [SerializeField] private MenuBottomController menuBottomController;

    [Header("=== Animation Settings ===")]
    [Tooltip("Thời gian chuyển động button và panel (giây)")]
    [SerializeField] private float menuAnimDuration = 1f;

    [Tooltip("Index button mặc định khi mở game (0=Shop, 1=Car, 2=Map, 3=Gear, 4=Talent)")]
    [SerializeField] private int defaultButtonIndex = 0;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 1. Loading screen
        if (loadingStartGameUI != null)
            loadingStartGameUI.StartLoadingSequence();
        else
            Debug.LogWarning("[UIGameManager] LoadingStartGameUI chưa được gán!");

        // 2. Menu bottom navigation
        if (menuBottomController != null)
            menuBottomController.Initialize(menuAnimDuration, defaultButtonIndex);
        else
            Debug.LogWarning("[UIGameManager] MenuBottomController chưa được gán!");
    }
}
