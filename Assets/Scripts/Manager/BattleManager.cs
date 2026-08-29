using System;
using UnityEngine;

/// <summary>
/// Quản lý toàn bộ vòng đời 1 trận đấu (Battle) bằng state machine.
///
/// Flow:
///   Intro → TurnSetup → TurnBattle → TurnSetup → TurnBattle → ... (lặp lại)
///   → Win (nếu thắng) hoặc Lose (nếu thua)
///
///   Pause có thể được gọi xen vào bất kỳ lúc nào (trừ Win/Lose),
///   và Resume() sẽ quay lại đúng state trước khi pause (giao diện Setup/Battle
///   giữ nguyên, không đổi khi Pause/Resume).
///
/// Camera Effect (BatteCameraEffect):
///   - Intro → TurnSetup : gọi ToggleEffect() để vào giao diện Setup
///   - TurnSetup → TurnBattle : gọi ReverseEffect() để vào giao diện Battle
///
/// Spawn Enemy (BattleSpawnEnemy):
///   - Khi vào TurnBattle: gọi battleSpawnEnemy.SpawnWave(currentWavesIndex, mapData)
///     để spawn enemy theo đúng Wave data tương ứng, với mapData được lấy từ
///     DataManager (xem ResolveMapBattleData()).
///
/// Grid Reset (BattleGridManager):
///   - Khi StartBattle(): gọi battleGridManager.ResetGrid() để đảm bảo
///     mỗi trận đấu mới luôn bắt đầu với lưới sạch (3x3 giữa Unlocked,
///     phần còn lại Locked), không giữ trạng thái unlock của trận trước.
///
/// Map Battle Data:
///   - currentMapBattleData là MapBattleData của level đang chiến đấu.
///     Có thể được gán tay từ bên ngoài qua SetMapBattleData() (ví dụ
///     GameManager.OnBattle), hoặc nếu chưa được gán, BattleManager sẽ tự lấy
///     từ DataManager.Instance.MapBattleData theo mapBattleIndex (xem
///     ResolveMapBattleData()) ngay khi cần dùng (FinishTurnSetup()).
/// </summary>
public class BattleManager : Singleton<BattleManager>
{
    public enum BattleState
    {
        Intro,
        TurnSetup,
        TurnBattle,
        Win,
        Lose,
        Pause
    }

    [Header("=== State ===")]
    [SerializeField] private BattleState currentState = BattleState.Intro;
    public BattleState CurrentState => currentState;

    [Header("=== Turn ===")]
    [SerializeField] private int currentTurn = 0;
    public int CurrentTurn => currentTurn;

    [Header("=== Wave ===")]
    [Tooltip("Wave hiện tại, bắt đầu = 1 khi StartBattle()")]
    [SerializeField] private int currentWavesIndex = 1;
    public int CurrentWavesIndex => currentWavesIndex;

    [Header("=== Map Battle Data ===")]
    [Tooltip("MapBattleData của level hiện tại. Có thể gán tay qua SetMapBattleData() " +
             "(GameManager.OnBattle) — nếu để trống, sẽ tự lấy từ DataManager theo mapBattleIndex.")]
    [SerializeField] private MapBattsleData currentMapBattleData;
    public MapBattsleData CurrentMapBattleData => currentMapBattleData;

    [Tooltip("Vị trí (index) của MapBattleData cần lấy trong DataManager.MapBattleData, " +
             "dùng khi currentMapBattleData chưa được gán tay.")]
    [SerializeField] private int mapBattleIndex = 0;

    [Header("=== Camera Effect ===")]
    [Tooltip("Hiệu ứng camera chuyển đổi giữa giao diện Setup và Battle")]
    [SerializeField] private BatteCameraEffect cameraEffect;

    [Header("=== Enemy Spawn ===")]
    [Tooltip("Spawner enemy theo Wave, gọi khi vào Turn Battle")]
    [SerializeField] private BattleSpawnEnemy battleSpawnEnemy;

    [Header("=== Grid ===")]
    [Tooltip("BattleGridManager — được Reset mỗi khi StartBattle() để đảm bảo lưới sạch cho trận mới")]
    [SerializeField] private BattleGridManager battleGridManager;

    [Header("Spawn")]
    [SerializeField] public BattleSpawnEnemy spawnEnemy;
    [SerializeField] public BattleSpawnDuck spawnDuck;

    [Header("=== My Team ===")]
    [Tooltip("MyTeam của người chơi — chứa MyTeamAnimation dùng để gọi AnimationSpawn() mỗi khi spawn Duck")]
    [SerializeField] public MyTeam myTem;

    // State được lưu lại trước khi Pause, để Resume() quay lại đúng chỗ
    private BattleState _stateBeforePause;
    private bool _isPaused;

    // ─── Events ─────────────────────────────────────────────
    public event Action<BattleState, BattleState> OnStateChanged; // (oldState, newState)
    public event Action<int> OnTurnSetupStart;   // turn index
    public event Action<int> OnTurnBattleStart;  // turn index
    public event Action OnIntroStart;
    public event Action OnWin;
    public event Action OnLose;
    public event Action OnPaused;
    public event Action OnResumed;

    // ─── Public API ─────────────────────────────────────────

    /// <summary>Gán MapBattleData cho trận đấu sắp diễn ra (gọi từ GameManager.OnBattle trước khi mở Battle).</summary>
    public void SetMapBattleData(MapBattsleData mapBattleData)
    {
        currentMapBattleData = mapBattleData;
    }

    /// <summary>Bắt đầu 1 trận đấu mới từ đầu.</summary>
    public void StartBattle()
    {
        currentTurn = 0;
        currentWavesIndex = 1;
        _isPaused = false;

        // Đảm bảo lưới sạch cho trận mới (không giữ trạng thái unlock của trận trước)
        if (battleGridManager != null)
            battleGridManager.ResetGrid();

        SetState(BattleState.Intro);
    }

    /// <summary>Gọi khi Intro chạy xong (animation/cutscene kết thúc) để bắt đầu turn đầu tiên.</summary>
    public void FinishIntro()
    {
        if (currentState != BattleState.Intro) return;
        BeginTurnSetup();
    }

    /// <summary>Gọi khi Turn Setup hoàn tất (đã bố trí xong đội hình/gear) để chuyển sang Battle.</summary>
    public void FinishTurnSetup()
    {
        if (currentState != BattleState.TurnSetup) return;
        SetState(BattleState.TurnBattle);

        // Vào giao diện Battle
        if (cameraEffect != null)
            cameraEffect.ReverseEffect();

        // Lấy MapBattleData (ưu tiên giá trị đã gán tay, nếu chưa có thì lấy từ DataManager)
        MapBattsleData mapData = ResolveMapBattleData();

        // Spawn enemy theo Wave hiện tại
        if (battleSpawnEnemy != null)
            battleSpawnEnemy.SpawnWave(currentWavesIndex, mapData);

        OnTurnBattleStart?.Invoke(currentTurn);
    }

    /// <summary>
    /// Gọi khi Turn Battle hoàn tất (hết thời gian giao tranh của turn).
    /// Truyền vào kết quả trận đấu hiện tại để quyết định đi tiếp hay kết thúc.
    /// </summary>
    public void FinishTurnBattle(BattleResult result)
    {
        if (currentState != BattleState.TurnBattle) return;

        switch (result)
        {
            case BattleResult.Win:
                SetState(BattleState.Win);
                OnWin?.Invoke();
                break;

            case BattleResult.Lose:
                SetState(BattleState.Lose);
                OnLose?.Invoke();
                break;

            case BattleResult.Continue:
            default:
                BeginTurnSetup();
                break;
        }
    }

    /// <summary>Tạm dừng trận đấu (không cho phép khi đang Win/Lose). Giao diện Setup/Battle giữ nguyên.</summary>
    public void Pause()
    {
        if (_isPaused) return;
        if (currentState == BattleState.Win || currentState == BattleState.Lose) return;

        _stateBeforePause = currentState;
        _isPaused = true;
        SetState(BattleState.Pause);
        OnPaused?.Invoke();
    }

    /// <summary>Tiếp tục trận đấu sau khi Pause, quay lại đúng state trước đó. Giao diện Setup/Battle giữ nguyên.</summary>
    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        SetState(_stateBeforePause);
        OnResumed?.Invoke();
    }

    // ─── Internal ───────────────────────────────────────────

    /// <summary>
    /// Trả về MapBattleData sẽ dùng cho trận đấu hiện tại:
    ///   - Nếu currentMapBattleData đã được gán tay (SetMapBattleData) -> dùng luôn giá trị đó.
    ///   - Nếu chưa -> lấy từ DataManager.Instance.MapBattleData theo mapBattleIndex,
    ///     lưu lại vào currentMapBattleData để các lần gọi sau không cần tra cứu lại.
    /// </summary>
    private MapBattsleData ResolveMapBattleData()
    {
        if (currentMapBattleData != null)
            return currentMapBattleData;

        var dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogWarning("[BattleManager] DataManager chưa sẵn sàng, không thể lấy MapBattleData!");
            return null;
        }

        var allMaps = dataManager.MapBattleData;
        if (allMaps == null || allMaps.Count == 0)
        {
            Debug.LogWarning("[BattleManager] DataManager chưa có MapBattleData nào!");
            return null;
        }

        if (mapBattleIndex < 0 || mapBattleIndex >= allMaps.Count)
        {
            Debug.LogWarning($"[BattleManager] mapBattleIndex ({mapBattleIndex}) ngoài phạm vi DataManager.MapBattleData!");
            return null;
        }

        currentMapBattleData = allMaps[mapBattleIndex];
        return currentMapBattleData;
    }

    private void BeginTurnSetup()
    {
        currentTurn++;
        currentWavesIndex = currentTurn;
        SetState(BattleState.TurnSetup);

        // Vào giao diện Setup
        if (cameraEffect != null)
            cameraEffect.ToggleEffect();

        OnTurnSetupStart?.Invoke(currentTurn);
    }

    private void SetState(BattleState newState)
    {
        if (currentState == newState) return;

        BattleState oldState = currentState;
        currentState = newState;

        if (newState == BattleState.Intro)
            OnIntroStart?.Invoke();

        OnStateChanged?.Invoke(oldState, newState);
    }
}

/// <summary>Kết quả của 1 Turn Battle, dùng để quyết định bước tiếp theo.</summary>
public enum BattleResult
{
    Continue, // chưa phân thắng bại, tiếp tục turn kế
    Win,
    Lose
}
