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

    [Header("=== LoadMap Animation ===")]
    [Tooltip("Script điều khiển hiệu ứng LoadMap DOTween - gắn vào GameObject LoadMap")]
    [SerializeField] private LoadMapAnimator loadMapAnimator;

    [Tooltip("Thời gian chạy animation LoadMap từ trái sang phải (giây). Mặc định 1.5s.")]
    [SerializeField] private float loadMapDuration = 1.5f;

    [Header("=== Battle Map UI ===")]
    [Tooltip("UI BattleMap (UIGame/StartGame/BatteMap) - được bật sau khi LoadMap animation xong")]
    [SerializeField] private BattleMapUI battleMapUI;

    [Header("=== Game Manager (Non-UI) ===")]
    [Tooltip("GameManager quản lý các GameObject không phải UI (BatteMap root object)")]
    [SerializeField] private GameManager gameManager;

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

    /// <summary>
    /// Gọi khi nhấn button Play trong MenuMid → Map.
    /// Chạy animation LoadMap rồi mở BattleMapUI và bật BatteMap GameObject.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (loadMapAnimator == null)
        {
            Debug.LogWarning("[UIGameManager] LoadMapAnimator chưa được gán! Mở BattleMap ngay.");
            OpenBattleMap();
            return;
        }

        // Chạy animation LoadMap với thời gian cấu hình được
        loadMapAnimator.PlayAnimation(loadMapDuration, onComplete: OpenBattleMap);
    }

    /// <summary>
    /// Mở UI BattleMap và bật GameObject BatteMap qua GameManager.
    /// </summary>
    private void OpenBattleMap()
    {
        // Bật UI BattleMap
        if (battleMapUI != null)
            battleMapUI.Show();
        else
            Debug.LogWarning("[UIGameManager] BattleMapUI chưa được gán!");

        // Bật non-UI BatteMap GameObject thông qua GameManager
        if (gameManager != null)
            gameManager.EnableBatteMap();
        else
            Debug.LogWarning("[UIGameManager] GameManager chưa được gán!");
    }
}
