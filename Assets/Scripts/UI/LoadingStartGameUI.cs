using UnityEngine;

[RequireComponent(typeof(LoadingBarController))]
public class LoadingStartGameUI : MonoBehaviour
{
    [Header("=== Menu Game ===")]
    [SerializeField] private GameObject menuGame;

    private LoadingBarController _loadingBar;

    private void Awake()
    {
        _loadingBar = GetComponent<LoadingBarController>();
    }

    public void StartLoadingSequence()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
                parent.gameObject.SetActive(true);
            parent = parent.parent;
        }

        gameObject.SetActive(true);
        _loadingBar.StartLoading(OnLoadingComplete);
    }

    private void OnLoadingComplete()
    {
        gameObject.SetActive(false);

        if (menuGame != null)
            menuGame.SetActive(true);
    }
}
