using UnityEngine;

/// <summary>
/// Script gắn vào GameObject LoadingStartGame.
/// Nhận lệnh từ UIGameManager để bắt đầu loading
/// và tự tắt khi hoàn thành.
/// </summary>
[RequireComponent(typeof(LoadingBarController))]
public class LoadingStartGameUI : MonoBehaviour
{
    private LoadingBarController _loadingBar;

    private void Awake()
    {
        _loadingBar = GetComponent<LoadingBarController>();
    }

    /// <summary>
    /// Được gọi bởi UIGameManager khi game khởi động.
    /// Bắt đầu animation loading, khi xong sẽ tắt GameObject này.
    /// </summary>
    public void StartLoadingSequence()
    {
        gameObject.SetActive(true);
        _loadingBar.StartLoading(OnLoadingComplete);
    }

    private void OnLoadingComplete()
    {
        gameObject.SetActive(false);
    }
}
