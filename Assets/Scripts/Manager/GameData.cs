using System;
using UnityEngine;

/// <summary>
/// GameData - Lưu trữ các giá trị chung của game hiện tại (session data): Coin, Ruby,
/// CurrentWaveIndex (tiến độ màn chơi), CurrentWave (MapBattleData đang chiến đấu).
/// ScriptableObject — tạo asset qua menu Assets/Create/Game/Game Data, đặt trong Assets/Data
/// và gán tay vào GameManager.gameData (GameManager forward các giá trị này ra ngoài qua
/// property, xem GameManager.CurrentWave).
///
/// LƯU Ý:
///   - CurrentWaveIndex ở đây là TIẾN ĐỘ MÀN CHƠI Player đã đạt được (persist xuyên suốt nhiều
///     trận đấu, dùng để mở khoá/tiếp tục màn kế tiếp) — KHÁC với BattleManager.CurrentWavesIndex,
///     vốn chỉ là bộ đếm Turn NỘI BỘ của 1 trận đang diễn ra (luôn reset về 1 mỗi StartBattle()).
///     Gọi AdvanceWaveIndex() khi Player thắng 1 màn để cập nhật tiến độ (không lùi tiến độ nếu
///     waveIndex truyền vào nhỏ hơn giá trị đã lưu).
///   - Coin dùng chung cho toàn bộ hệ thống nâng cấp vũ khí — xem WeaponManager.TryLevelUp(id,
///     ref int playerCoin): lấy Coin ra bằng biến tạm, gọi TryLevelUp, rồi gọi lại SetCoin() với
///     giá trị mới (WeaponManager tự trừ tiền vào biến ref truyền vào).
///   - Ruby là đơn vị tiền tệ phụ (premium currency), độc lập hoàn toàn với Coin.
///   - Là ScriptableObject nên giá trị được lưu trực tiếp vào asset trong Editor (giống
///     WeaponData), nhưng KHÔNG tự reset giữa các lần Play — cần gọi ResetSession() (hoặc
///     tự cấp init) khi build thật để tránh mang số dư từ lần chơi trước trong Editor.
///     CHƯA có hệ thống Save/Load (PlayerPrefs/JSON) lâu dài — có thể bổ sung Save()/Load()
///     sau này mà không ảnh hưởng API hiện tại (chỉ cần đọc/ghi thêm các field bên dưới).
///   - CurrentWave dùng cùng pattern encapsulation với Coin/Ruby/CurrentWaveIndex: field private
///     + property public + event OnWaveChanged khi thay đổi (thay vì field public trần trụi).
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
    [Tooltip("Tiến độ màn chơi (Wave) cao nhất Player đã đạt được, bắt đầu = 1. KHÁC với " +
             "BattleManager.CurrentWavesIndex (bộ đếm turn nội bộ của 1 trận, luôn reset về 1 " +
             "mỗi StartBattle()) — giá trị này chỉ tăng khi AdvanceWaveIndex() được gọi (VD: sau " +
             "khi Player thắng màn hiện tại).")]
    [SerializeField] private int currentWaveIndex = 1;

    [Header("=== Battle ===")]
    [Tooltip("MapBattleData của màn/level đang chiến đấu")]
    [SerializeField] private MapBattsleData currentWave;

    // ─── Events ─────────────────────────────────────────────
    /// <summary>Fired khi Coin thay đổi (SetCoin/AddCoin/TrySpendCoin) — giá trị Coin mới.</summary>
    public event Action<int> OnCoinChanged;
    /// <summary>Fired khi Ruby thay đổi — giá trị Ruby mới.</summary>
    public event Action<int> OnRubyChanged;
    /// <summary>Fired khi CurrentWaveIndex thay đổi — giá trị mới.</summary>
    public event Action<int> OnWaveIndexChanged;
    /// <summary>Fired khi CurrentWave thay đổi — giá trị mới.</summary>
    public event Action<MapBattsleData> OnWaveChanged;

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
    #region Progress: CurrentWaveIndex

    public int CurrentWaveIndex => currentWaveIndex;

    /// <summary>Gán thẳng tiến độ Wave (tự Clamp tối thiểu = 1).</summary>
    public void SetCurrentWaveIndex(int value)
    {
        int clamped = Mathf.Max(1, value);
        if (clamped == currentWaveIndex) return;
        currentWaveIndex = clamped;
        OnWaveIndexChanged?.Invoke(currentWaveIndex);
    }

    /// <summary>
    /// Tăng tiến độ Wave lên waveIndex NẾU waveIndex lớn hơn giá trị hiện tại (không bao giờ lùi
    /// tiến độ). Gọi hàm này khi Player hoàn thành 1 màn/wave để cập nhật tiến độ đã đạt được.
    /// </summary>
    public void AdvanceWaveIndex(int waveIndex)
    {
        if (waveIndex > currentWaveIndex)
            SetCurrentWaveIndex(waveIndex);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Battle: CurrentWave

    /// <summary>MapBattleData của màn/level đang chiến đấu. Set sẽ fire OnWaveChanged nếu thay đổi.</summary>
    public MapBattsleData CurrentWave
    {
        get => currentWave;
        set
        {
            if (currentWave == value) return;
            currentWave = value;
            OnWaveChanged?.Invoke(currentWave);
        }
    }

    #endregion
}
