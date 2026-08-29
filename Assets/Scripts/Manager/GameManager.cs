using UnityEngine;

/// <summary>
/// GameManager - Root manager, quản lý các sub-manager và GameObject non-UI.
/// Singleton. Tất cả sub-manager là con của GameObject này.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Sub Managers ===")]
    [Tooltip("WeaponManager child — quản lý toàn bộ dữ liệu vũ khí")]
    [SerializeField] public WeaponManager weaponManager;

    [Tooltip("BattleManager child — quản lý state machine trận đấu (Intro/TurnSetup/TurnBattle/Win/Lose)")]
    [SerializeField] public BattleManager battleManager;

    [Header("=== Game Data ===")]
    [Tooltip("GameData chứa các giá trị chung của game hiện tại (CurrentWave,...). Gán tay trong Inspector.")]
    [SerializeField] private GameData gameData;

    /// <summary>MapBattleData đang được chiến đấu, giá trị thực lưu trong GameData.</summary>
    public MapBattsleData CurrentWave
    {
        get => gameData != null ? gameData.CurrentWave : null;
        set { if (gameData != null) gameData.CurrentWave = value; }
    }

    [Header("=== UI ===")]
    [Tooltip("UIGameManager — dùng để chuyển đổi UI khi bắt đầu Battle")]
    [SerializeField] private UIGameManager uiGameManager;

    [Header("=== Non-UI GameObjects ===")]
    [Tooltip("GameObject BatteMap chứa logic game,m được bật khi vào Battle")]
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
        else Debug.LogWarning("[GameManager] BatteMap chưa được gán!");
    }

    public void DisableBatteMap()
    {
        if (batteMapObject != null) batteMapObject.SetActive(false);
    }

    /// <summary>
    /// Gọi khi người chơi chọn 1 màn (MapBattleData) và bấm chiến đấu.
    /// Lưu lại CurrentWave, truyền cho BattleManager, rồi chuyển UI qua UIGameManager.OnPlayButtonClicked().
    /// </summary>
    public void OnBattle(MapBattsleData mapBattleData)
    {
        CurrentWave = mapBattleData;

        if (battleManager != null)
            battleManager.SetMapBattleData(CurrentWave);
        else
            Debug.LogWarning("[GameManager] BattleManager chưa được gán!");

        if (uiGameManager != null)
            uiGameManager.OnPlayButtonClicked();
        else
            Debug.LogWarning("[GameManager] UIGameManager chưa được gán!");
    }
}
