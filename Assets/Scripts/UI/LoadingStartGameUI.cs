using UnityEngine;

/// <summary>
/// Script gắn vào GameObject LoadingStartGame.
/// Nhận lệnh từ UIGameManager để bắt đầu loading
/// và tự tắt khi hoàn thành, sau đó bật MenuGame.
/// </summary>
[RequireComponent(typeof(LoadingBarController))]
public class LoadingStartGameUI : MonoBehaviour
{
    [Header("=== Menu Game ===")]
    [Tooltip("MenuGame GameObject sẽ được bật sau khi loading xong")]
    [SerializeField] private GameObject menuGame;

    private LoadingBarController _loadingBar;

    private void Awake()
    {
        _loadingBar = GetComponent<LoadingBarController>();
    }

    /// <summary>
    /// Được gọi bởi UIGameManager khi game khởi động.
    /// Bắt đầu animation loading bar, khi xong sẽ tắt LoadStartGame và bật MenuGame.
    /// </summary>
    public void StartLoadingSequence()
    {
        gameObject.SetActive(true);
        _loadingBar.StartLoading(OnLoadingComplete);
    }

    private void OnLoadingComplete()
    {
        // Tắt LoadStartGame (GameObject này)
        gameObject.SetActive(false);

        // Bật MenuGame
        if (menuGame != null)
            menuGame.SetActive(true);
        else
            Debug.LogWarning("[LoadingStartGameUI] MenuGame chưa được gán! Hãy kéo MenuGame vào Inspector.");
    }
}
