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
///   - Khi vào TurnBattle: gọi battleSpawnEnemy.SpawnWave(currentWavesIndex)
///     để spawn enemy theo đúng Wave data tương ứng.
/// </summary>
public class BattleManager : MonoBehaviour
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

    [Header("=== Camera Effect ===")]
    [Tooltip("Hiệu ứng camera chuyển đổi giữa giao diện Setup và Battle")]
    [SerializeField] private BatteCameraEffect cameraEffect;

    [Header("=== Enemy Spawn ===")]
    [Tooltip("Spawner enemy theo Wave, gọi khi vào Turn Battle")]
    [SerializeField] private BattleSpawnEnemy battleSpawnEnemy;

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

    /// <summary>Bắt đầu 1 trận đấu mới từ đầu.</summary>
    public void StartBattle()
    {
        currentTurn = 0;
        currentWavesIndex = 1;
        _isPaused = false;
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

        // Spawn enemy theo Wave hiện tại
        if (battleSpawnEnemy != null)
            battleSpawnEnemy.SpawnWave(currentWavesIndex);

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
