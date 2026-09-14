using TMPro;
using UnityEngine;


public class UIGameManager : MonoBehaviour
{
    [Header("=== Loading ===")]
    [SerializeField] private LoadingStartGameUI loadingStartGameUI;

    [Header("=== Menu Navigation ===")]
    [SerializeField] private MenuBottomController menuBottomController;

    [Header("=== Animation Settings ===")]
    [SerializeField] private float menuAnimDuration = 1f;
    [SerializeField] private int defaultButtonIndex = 0;

    [Header("=== LoadMap Animation ===")]
    [SerializeField] private LoadMapAnimator loadMapAnimator;
    [SerializeField] private float loadMapDuration = 1.5f;

    [Header("=== Battle Map UI ===")]
    [SerializeField] private BattleMapUI battleMapUI;

    [Header("=== Game Manager (Non-UI) ===")]
    [SerializeField] private GameManager gameManager;

    [Header("=== HUD Info ===")]
    [SerializeField] private TextMeshProUGUI textCurrentBattleMap;
    [SerializeField] private TextMeshProUGUI textNameMapIndex;
    [SerializeField] private TextMeshProUGUI textRuby;
    [SerializeField] private TextMeshProUGUI textCoin;
    [SerializeField] private TextMeshProUGUI textPower;

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (loadingStartGameUI != null)
            loadingStartGameUI.StartLoadingSequence();

        if (menuBottomController != null)
            menuBottomController.Initialize(menuAnimDuration, defaultButtonIndex);
    }

    public void OnPlayButtonClicked()
    {
        if (loadMapAnimator == null)
        {
            OpenBattleMap();
            return;
        }

        loadMapAnimator.Play(loadMapDuration, onComplete: OpenBattleMap);
    }
    private void OpenBattleMap()
    {
        if (gameManager != null)
            gameManager.EnableBatteMap();

        BattleManager battleManager = gameManager != null ? gameManager.battleManager : null;

        if (battleManager != null)
            battleManager.StartBattle();

        if (battleMapUI != null)
        {
            battleMapUI.Show(onComplete: () =>
            {
                battleManager?.FinishIntro();
            });
        }
        else
        {
            battleManager?.FinishIntro();
        }
    }
    public void OnBackToStartGameClicked()
    {
        if (loadMapAnimator == null)
        {
            battleMapUI?.Hide();
            gameManager?.DisableBatteMap();
            return;
        }

        loadMapAnimator.PlayReverse(loadMapDuration);
    }
    public void RefreshHUD()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.RecalculatePower();

        if (textPower != null) textPower.text =gameManager.Power.ToString();
        if (textRuby != null) textRuby.text = gameManager.Ruby.ToString();
        if (textCoin != null) textCoin.text = gameManager.Coin.ToString();
    }

    public void RefreshCurrentBattleMapText()
    {
        if (gameManager == null) return;

        int currentMap = gameManager.CurrentMapIndex;

        if (textCurrentBattleMap != null)
            textCurrentBattleMap.text = "Cấp độ " + currentMap.ToString();

        if (textNameMapIndex != null)
        {
            int totalMaps = DataManager.Instance != null ? DataManager.Instance.MapBattleData.Count : 0;
            textNameMapIndex.text = "Map " + currentMap + "\n(" + currentMap + "/" + totalMaps + ")";
        }
    }
}
