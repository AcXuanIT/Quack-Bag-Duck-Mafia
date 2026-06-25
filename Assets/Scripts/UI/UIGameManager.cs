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
    [Tooltip("LoadMapAnimator gắn trên GameObject LoadMap")]
    [SerializeField] private LoadMapAnimator loadMapAnimator;

    [Tooltip("Thời gian slide LoadMap từ trái sang phải (giây)")]
    [SerializeField] private float loadMapDuration = 1.5f;

    [Header("=== Battle Map UI ===")]
    [Tooltip("UI BattleMap (UIGame/StartGame/BatteMap) - bật sau khi LoadMap xong")]
    [SerializeField] private BattleMapUI battleMapUI;

    [Header("=== Game Manager (Non-UI) ===")]
    [Tooltip("GameManager quản lý các GameObject không phải UI")]
    [SerializeField] private GameManager gameManager;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (loadingStartGameUI != null)
            loadingStartGameUI.StartLoadingSequence();
        else
            Debug.LogWarning("[UIGameManager] LoadingStartGameUI chưa được gán!");

        if (menuBottomController != null)
            menuBottomController.Initialize(menuAnimDuration, defaultButtonIndex);
        else
            Debug.LogWarning("[UIGameManager] MenuBottomController chưa được gán!");
    }

    /// <summary>
    /// Gọi từ Button Play (MenuMid → Map).
    /// Chạy LoadMap slide rồi mở BattleMap.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (loadMapAnimator == null)
        {
            Debug.LogWarning("[UIGameManager] LoadMapAnimator chưa được gán!");
            OpenBattleMap();
            return;
        }

        loadMapAnimator.Play(loadMapDuration, onComplete: OpenBattleMap);
    }

    /// <summary>
    /// Bật UI BattleMap và GameObject BatteMap qua GameManager.
    /// </summary>
    private void OpenBattleMap()
    {
        if (battleMapUI != null)
            battleMapUI.Show();
        else
            Debug.LogWarning("[UIGameManager] BattleMapUI chưa được gán!");

        if (gameManager != null)
            gameManager.EnableBatteMap();
        else
            Debug.LogWarning("[UIGameManager] GameManager chưa được gán!");
    }
}
