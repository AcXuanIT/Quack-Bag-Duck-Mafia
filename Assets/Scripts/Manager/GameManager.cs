using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Sub Managers ===")]
    [SerializeField] public WeaponManager weaponManager;
    [SerializeField] public BattleManager battleManager;

    [Header("=== Game Data ===")]
    [SerializeField] private GameData gameData;

    public int CurrentMapIndex => gameData != null ? gameData.CurrentMapIndex : 0;

    public int Coin => gameData != null ? gameData.Coin : 0;

    public int Ruby => gameData != null ? gameData.Ruby : 0;

    public int Power => gameData != null ? gameData.Power : 0;

    [Header("=== UI ===")]
    [SerializeField] private UIGameManager uiGameManager;

    [Header("=== Non-UI GameObjects ===")]
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
    }

    public void DisableBatteMap()
    {
        if (batteMapObject != null) batteMapObject.SetActive(false);
    }

    public void OnBattle()
    {
        if (battleManager != null)
            battleManager.SetMapIndex(CurrentMapIndex);
        else
            Debug.LogWarning("[GameManager] BattleManager chưa được gán!");

        if (uiGameManager != null)
            uiGameManager.OnPlayButtonClicked();
        else
            Debug.LogWarning("[GameManager] UIGameManager chưa được gán!");
    }

    public void OnBattleMapWin()
    {
        if (gameData == null)
        {
            return;
        }

        gameData.AdvanceMapIndex(gameData.CurrentMapIndex + 1);
    }
    public void AddBattleRewards(int coinAmount, int rubyAmount)
    {
        if (gameData == null)
        {
            return;
        }

        if (coinAmount != 0) gameData.AddCoin(coinAmount);
        if (rubyAmount != 0) gameData.AddRuby(rubyAmount);
    }

    public void SetCoin(int value)
    {
        if (gameData == null)
        {
            Debug.LogWarning("[GameManager] GameData chưa được gán, không thể cập nhật Coin!");
            return;
        }

        gameData.SetCoin(value);
    }

    public void RecalculatePower()
    {
        if (gameData == null) return;
        if (WeaponManager.Instance == null) return;

        int total = 0;
        foreach (var w in WeaponManager.Instance.GetUnlockedWeapons())
            total += w.GetCurrentPower();

        gameData.SetPower(total);
    }
}
