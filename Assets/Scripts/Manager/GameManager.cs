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
    [Tooltip("GameData chứa các giá trị chung của game hiện tại (Coin, Ruby, CurrentMapIndex, Power...). Gán tay trong Inspector.")]
    [SerializeField] private GameData gameData;

    /// <summary>Tiến độ BattleMap cao nhất Player đã đạt được, giá trị thực lưu trong GameData.</summary>
    public int CurrentMapIndex => gameData != null ? gameData.CurrentMapIndex : 0;

    /// <summary>Số Coin hiện có của Player, giá trị thực lưu trong GameData.</summary>
    public int Coin => gameData != null ? gameData.Coin : 0;

    /// <summary>Số Ruby hiện có của Player, giá trị thực lưu trong GameData.</summary>
    public int Ruby => gameData != null ? gameData.Ruby : 0;

    /// <summary>Power hiển thị ở Menu (ngoài Battle), giá trị thực lưu trong GameData.</summary>
    public int Power => gameData != null ? gameData.Power : 0;

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
    /// Gọi khi người chơi bấm chiến đấu (Play). Truyền CurrentMapIndex (tiến độ BattleMap hiện
    /// tại, đọc từ GameData) cho BattleManager qua SetMapIndex(), rồi chuyển UI qua
    /// UIGameManager.OnPlayButtonClicked().
    /// </summary>
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

    /// <summary>
    /// Gọi từ BattleManager mỗi khi Player thắng xong 1 BattleMap (BattleState.Win) — tăng
    /// CurrentMapIndex lên 1 (không lùi tiến độ nếu đã cao hơn, xem GameData.AdvanceMapIndex())
    /// và lưu lại vào GameData.
    /// </summary>
    public void OnBattleMapWin()
    {
        if (gameData == null)
        {
            Debug.LogWarning("[GameManager] GameData chưa được gán, không thể cập nhật CurrentMapIndex!");
            return;
        }

        gameData.AdvanceMapIndex(gameData.CurrentMapIndex + 1);
    }

    /// <summary>
    /// Gọi từ BattleManager mỗi khi 1 BattleMap kết thúc (Win, Lose, hoặc bỏ dở/Abandon giữa
    /// chừng — xem BattleManager.ResetBattleState()/CommitEarnedRewards()). Cộng dồn số
    /// Coin/Ruby đã tích luỹ tạm thời trong trận đấu (BattleManager.earnedCoin/earnedRuby) vào
    /// GameData (Coin dùng nâng cấp vũ khí, Ruby là tiền tệ phụ). Không làm gì nếu cả hai đều = 0.
    /// </summary>
    public void AddBattleRewards(int coinAmount, int rubyAmount)
    {
        if (gameData == null)
        {
            Debug.LogWarning("[GameManager] GameData chưa được gán, không thể cộng Coin/Ruby thưởng!");
            return;
        }

        if (coinAmount != 0) gameData.AddCoin(coinAmount);
        if (rubyAmount != 0) gameData.AddRuby(rubyAmount);
    }

    /// <summary>
    /// Gán thẳng số Coin hiện có của Player vào GameData. Dùng sau khi WeaponManager.TryLevelUp()
    /// đã trừ Coin vào 1 biến tạm (ref) — gọi hàm này để lưu giá trị mới lại vào GameData
    /// (xem WeaponInfoUI.OnClickUpdate()).
    /// </summary>
    public void SetCoin(int value)
    {
        if (gameData == null)
        {
            Debug.LogWarning("[GameManager] GameData chưa được gán, không thể cập nhật Coin!");
            return;
        }

        gameData.SetCoin(value);
    }

    /// <summary>
    /// Tính lại tổng Power (điểm sức mạnh) của Player ở Menu = tổng WeaponEntry.GetCurrentPower()
    /// của TOÀN BỘ weapon ĐÃ MỞ KHOÁ trong WeaponManager (weapon đang Locked không tính), rồi ghi
    /// kết quả vào GameData qua SetPower(). Gọi từ UIGameManager.RefreshHUD() — hàm này đã được
    /// MenuPanelController.NotifyPanelLoaded() gọi mỗi khi chuyển Panel Menu (ShowPanel()/
    /// ShowPanelImmediate()), nên Power hiển thị luôn được tính lại đúng lúc, không cần lắng nghe
    /// thêm sự kiện WeaponManager.OnWeaponChanged/OnDatabaseReloaded riêng.
    /// </summary>
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
