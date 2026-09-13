using System;
using UnityEngine;

/// <summary>
/// GameData - Lưu trữ các giá trị chung của game hiện tại (session data): Coin, Ruby,
/// CurrentMapIndex (tiến độ màn chơi Player đã đạt được), Power (tổng điểm sức mạnh hiển thị
/// ở Menu, ngoài Battle).
/// ScriptableObject — tạo asset qua menu Assets/Create/Game/Game Data, đặt trong Assets/Data
/// và gán tay vào GameManager.gameData (GameManager forward các giá trị này ra ngoài qua
/// property, xem GameManager.Coin/Ruby/Power/CurrentMapIndex).
///
/// LƯU Ý:
///   - CurrentMapIndex ở đây là TIẾN ĐỘ MÀN CHƠI (BattleMap) Player đã đạt được (persist xuyên
///     suốt nhiều trận đấu, dùng để mở khoá/tiếp tục màn kế tiếp) — KHÁC với
///     BattleManager.CurrentWavesIndex, vốn chỉ là bộ đếm Turn NỘI BỘ của 1 trận đang diễn ra
///     (luôn reset về 1 mỗi StartBattle()). Gọi AdvanceMapIndex() khi Player thắng xong 1
///     BattleMap để cập nhật tiến độ (không lùi tiến độ nếu mapIndex truyền vào nhỏ hơn giá trị
///     đã lưu) — xem GameManager.OnBattleMapWin(), được BattleManager gọi mỗi khi thắng 1 BattleMap.
///   - Coin dùng chung cho toàn bộ hệ thống nâng cấp vũ khí — xem WeaponManager.TryLevelUp(id,
///     ref int playerCoin): lấy Coin ra bằng biến tạm, gọi TryLevelUp, rồi gọi lại SetCoin() với
///     giá trị mới (WeaponManager tự trừ tiền vào biến ref truyền vào).
///   - Ruby là đơn vị tiền tệ phụ (premium currency), độc lập hoàn toàn với Coin.
///   - Power là tổng điểm sức mạnh của Player hiển thị ở Menu (UIGameManager.textPower, được
///     cập nhật qua MenuPanelController mỗi khi 1 Panel được load) — độc lập với
///     BattleManager.Power (chỉ tồn tại trong lúc đang ở 1 trận đấu, tính lại từ Battle Grid).
///   - Là ScriptableObject nên giá trị được lưu trực tiếp vào asset trong Editor (giống
///     WeaponData), nhưng KHÔNG tự reset giữa các lần Play — cần gọi ResetSession() (hoặc
///     tự cấp init) khi build thật để tránh mang số dư từ lần chơi trước trong Editor.
///     CHƯA có hệ thống Save/Load (PlayerPrefs/JSON) lâu dài — có thể bổ sung Save()/Load()
///     sau này mà không ảnh hưởng API hiện tại (chỉ cần đọc/ghi thêm các field bên dưới).
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "Game/Game Data")]
public class GameData : ScriptableObject
{
    [Header("=== Currency ===")]
    [Tooltip("Số Coin hiện có của Player — dùng để nâng cấp vũ khí (xem WeaponManager.TryLevelUp)")]
    [SerializeField] private int coin;

    [Tooltip("Số Ruby hiện có của Player — đơn vị tiền tệ phụ (premium currency), độc lập với Coin")]
    [SerializeField] private int ruby;

    [Header("=== Progress ===")]
    [Tooltip("Tiến độ màn chơi (BattleMap) cao nhất Player đã đạt được, bắt đầu = 1. KHÁC với " +
             "BattleManager.CurrentWavesIndex (bộ đếm turn nội bộ của 1 trận, luôn reset về 1 " +
             "mỗi StartBattle()) — giá trị này chỉ tăng khi AdvanceMapIndex() được gọi (VD: sau " +
             "khi Player thắng xong 1 BattleMap, xem GameManager.OnBattleMapWin()).")]
    [SerializeField] private int currentMapIndex = 1;

    [Header("=== Power ===")]
    [Tooltip("Tổng điểm sức mạnh (Power) hiện tại của Player, hiển thị ở Menu (ngoài Battle) — " +
             "xem UIGameManager.textPower.")]
    [SerializeField] private int power;

    // ─── Events ─────────────────────────────────────────────
    /// <summary>Fired khi Coin thay đổi (SetCoin/AddCoin/TrySpendCoin) — giá trị Coin mới.</summary>
    public event Action<int> OnCoinChanged;
    /// <summary>Fired khi Ruby thay đổi — giá trị Ruby mới.</summary>
    public event Action<int> OnRubyChanged;
    /// <summary>Fired khi CurrentMapIndex thay đổi — giá trị mới.</summary>
    public event Action<int> OnMapIndexChanged;
    /// <summary>Fired khi Power thay đổi — giá trị mới.</summary>
    public event Action<int> OnPowerChanged;

    // ─────────────────────────────────────────────────────────
    #region Currency: Coin

    public int Coin => coin;

    /// <summary>Gán thẳng số Coin (không cho phép âm — tự Clamp về 0).</summary>
    public void SetCoin(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == coin) return;
        coin = clamped;
        OnCoinChanged?.Invoke(coin);
    }

    /// <summary>Cộng thêm Coin (amount có thể âm để trừ trực tiếp, nhưng nên dùng TrySpendCoin khi trừ).</summary>
    public void AddCoin(int amount)
    {
        if (amount == 0) return;
        SetCoin(coin + amount);
    }

    /// <summary>Trừ Coin nếu đủ. Trả về false (không trừ gì) nếu không đủ Coin.</summary>
    public bool TrySpendCoin(int amount)
    {
        if (amount <= 0) return true;
        if (coin < amount) return false;
        SetCoin(coin - amount);
        return true;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Currency: Ruby

    public int Ruby => ruby;

    /// <summary>Gán thẳng số Ruby (không cho phép âm — tự Clamp về 0).</summary>
    public void SetRuby(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == ruby) return;
        ruby = clamped;
        OnRubyChanged?.Invoke(ruby);
    }

    /// <summary>Cộng thêm Ruby (amount có thể âm để trừ trực tiếp, nhưng nên dùng TrySpendRuby khi trừ).</summary>
    public void AddRuby(int amount)
    {
        if (amount == 0) return;
        SetRuby(ruby + amount);
    }

    /// <summary>Trừ Ruby nếu đủ. Trả về false (không trừ gì) nếu không đủ Ruby.</summary>
    public bool TrySpendRuby(int amount)
    {
        if (amount <= 0) return true;
        if (ruby < amount) return false;
        SetRuby(ruby - amount);
        return true;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Progress: CurrentMapIndex

    public int CurrentMapIndex => currentMapIndex;

    /// <summary>Gán thẳng tiến độ BattleMap (tự Clamp tối thiểu = 1).</summary>
    public void SetCurrentMapIndex(int value)
    {
        int clamped = Mathf.Max(1, value);
        if (clamped == currentMapIndex) return;
        currentMapIndex = clamped;
        OnMapIndexChanged?.Invoke(currentMapIndex);
    }

    /// <summary>
    /// Tăng tiến độ BattleMap lên mapIndex NẾU mapIndex lớn hơn giá trị hiện tại (không bao giờ
    /// lùi tiến độ). Gọi hàm này khi Player thắng xong 1 BattleMap để cập nhật tiến độ đã đạt
    /// được (xem GameManager.OnBattleMapWin(), được BattleManager gọi mỗi khi thắng 1 BattleMap).
    /// </summary>
    public void AdvanceMapIndex(int mapIndex)
    {
        if (mapIndex > currentMapIndex)
            SetCurrentMapIndex(mapIndex);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Power

    public int Power => power;

    /// <summary>Gán thẳng Power (không cho phép âm — tự Clamp về 0).</summary>
    public void SetPower(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == power) return;
        power = clamped;
        OnPowerChanged?.Invoke(power);
    }

    #endregion
}
