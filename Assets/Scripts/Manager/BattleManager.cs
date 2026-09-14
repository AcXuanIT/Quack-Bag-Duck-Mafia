using System;
using UnityEngine;


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
    [SerializeField] private MapBattsleData currentMapBattleData;
    public MapBattsleData CurrentMapBattleData => currentMapBattleData;

    [SerializeField] private int mapBattleIndex = 0;

    [Header("=== Camera Effect ===")]
    [SerializeField] private BatteCameraEffect cameraEffect;

    [Header("=== Enemy Spawn ===")]
    [SerializeField] private BattleSpawnEnemy battleSpawnEnemy;

    [Header("=== Grid ===")]
    [SerializeField] private BattleGridManager battleGridManager;

    [Header("=== UI ===")]
    [SerializeField] private BattleMapUI battleMapUI;

    [Header("Spawn")]
    [SerializeField] public BattleSpawnEnemy spawnEnemy;
    [SerializeField] public BattleSpawnDuck spawnDuck;

    [Header("=== My Team ===")]
    [SerializeField] public MyTeam myTem;

    [Header("=== Player Money ===")]
    [SerializeField] private int playerMoney;
    public int PlayerMoney => playerMoney;

    [SerializeField] private int startMoney = 50;
    [SerializeField] private int moneyPerWaveWin = 50;
    [SerializeField] private ShopBatteManager shopBatteManager;

    [Header("=== Power ===")]
    [SerializeField] private int power;
    public int Power => power;

    [Header("=== Battle Rewards (Coin/Ruby thật, cộng vào GameData) ===")]
    [SerializeField] private int coinPerWaveWin = 10;
    [SerializeField] private int coinPerMapWin = 50;
    [SerializeField] private int rubyPerMapWin = 10;
    [SerializeField] private int earnedCoin;
    public int EarnedCoin => earnedCoin;

    [SerializeField] private int earnedRuby;
    public int EarnedRuby => earnedRuby;

    private BattleState _stateBeforePause;
    private bool _isPaused;

    // ─── Events ─
    public event Action<BattleState, BattleState> OnStateChanged; 
    public event Action<int> OnTurnSetupStart;   // turn index
    public event Action<int> OnTurnBattleStart;  // turn index
    public event Action OnIntroStart;
    public event Action OnWin;
    public event Action OnLose;
    public event Action OnPaused;
    public event Action OnResumed;

    public event Action OnReturnToMenu;

    public event Action<int> OnMoneyChanged;

    public event Action<int> OnPowerChanged;

    // ─── Unity Lifecycle ─

    protected override void Awake()
    {
        base.Awake();

        if (myTem != null)
            myTem.OnDeath += HandleMyTeamDeath;
    }

    private void OnDestroy()
    {
        if (myTem != null)
            myTem.OnDeath -= HandleMyTeamDeath;
    }

    private void Update()
    {
        CheckWaveCleared();
    }

    // ─── Public API ─
    public void SetMapBattleData(MapBattsleData mapBattleData)
    {
        currentMapBattleData = mapBattleData;
    }

    public void SetMapIndex(int mapIndex)
    {
        mapBattleIndex = mapIndex;
        currentMapBattleData = null;
    }
    public void StartBattle()
    {
        ResetBattleState();

        UpdateWaveText();

        SetState(BattleState.Intro);
    }

    public void FinishIntro()
    {
        if (currentState != BattleState.Intro) return;
        BeginTurnSetup();
    }
    public void FinishTurnSetup()
    {
        if (currentState != BattleState.TurnSetup) return;
        SetState(BattleState.TurnBattle);

        if (cameraEffect != null)
            cameraEffect.ReverseEffect();

        MapBattsleData mapData = ResolveMapBattleData();

        if (battleSpawnEnemy != null)
            battleSpawnEnemy.SpawnWave(currentWavesIndex, mapData);

        OnTurnBattleStart?.Invoke(currentTurn);
    }

    public void FinishTurnBattle(BattleResult result)
    {
        if (currentState != BattleState.TurnBattle) return;

        switch (result)
        {
            case BattleResult.Win:
                SetState(BattleState.Win);

                earnedCoin += coinPerMapWin;
                earnedRuby += rubyPerMapWin;

                if (battleMapUI != null)
                    battleMapUI.ShowWin();

                GameManager.Instance?.OnBattleMapWin();

                OnWin?.Invoke();
                break;

            case BattleResult.Lose:
                SetState(BattleState.Lose);
                if (battleMapUI != null)
                    battleMapUI.ShowLose();
                OnLose?.Invoke();
                break;

            case BattleResult.Continue:
            default:
                BeginTurnSetup();
                break;
        }
    }

    public void Pause()
    {
        if (_isPaused) return;
        if (currentState == BattleState.Win || currentState == BattleState.Lose) return;

        _stateBeforePause = currentState;
        _isPaused = true;
        SetState(BattleState.Pause);
        OnPaused?.Invoke();
    }
    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        SetState(_stateBeforePause);
        OnResumed?.Invoke();
    }

    public void ReturnToMenu()
    {
        ResetBattleState();
        SetState(BattleState.Intro);
        OnReturnToMenu?.Invoke();
    }

    // ─── Player Money ─
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        playerMoney += amount;
        NotifyMoneyChanged();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (playerMoney < amount) return false;

        playerMoney -= amount;
        NotifyMoneyChanged();
        return true;
    }
    private void SetMoney(int amount)
    {
        playerMoney = amount;
        NotifyMoneyChanged();
    }
    private void NotifyMoneyChanged()
    {
        if (battleMapUI != null)
            battleMapUI.UpdateTextPlayerMoney(playerMoney);

        if (shopBatteManager != null)
            shopBatteManager.RefreshUI();

        OnMoneyChanged?.Invoke(playerMoney);
    }

    // ─── Power ──
    public void SetPower(int newPower)
    {
        if (power == newPower) return;
        power = newPower;

        if (battleMapUI != null)
            battleMapUI.UpdateTextPower(power);

        OnPowerChanged?.Invoke(power);
    }

    // ─── Battle Rewards
    private void CommitEarnedRewards()
    {
        if (earnedCoin == 0 && earnedRuby == 0) return;

        GameManager.Instance?.AddBattleRewards(earnedCoin, earnedRuby);

        earnedCoin = 0;
        earnedRuby = 0;
    }

    // ─── Internal 
    private void CheckWaveCleared()
    {
        if (currentState != BattleState.TurnBattle) return;
        if (battleSpawnEnemy == null || !battleSpawnEnemy.IsWaveCleared()) return;

        if (spawnDuck != null)
            spawnDuck.DespawnAllDucks();

        AddMoney(moneyPerWaveWin);
        earnedCoin += coinPerWaveWin;

        MapBattsleData mapData = currentMapBattleData ?? ResolveMapBattleData();
        bool isLastWave = mapData != null && currentWavesIndex >= mapData.WaveCount;

        FinishTurnBattle(isLastWave ? BattleResult.Win : BattleResult.Continue);
    }

    private void HandleMyTeamDeath()
    {
        if (currentState == BattleState.Win || currentState == BattleState.Lose) return;

        SetState(BattleState.Lose);

        if (battleMapUI != null)
            battleMapUI.ShowLose();

        OnLose?.Invoke();
    }
    private MapBattsleData ResolveMapBattleData()
    {
        if (currentMapBattleData != null)
            return currentMapBattleData;

        var dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            return null;
        }

        var allMaps = dataManager.MapBattleData;
        if (allMaps == null || allMaps.Count == 0)
        {
            return null;
        }

        if (mapBattleIndex < 0 || mapBattleIndex >= allMaps.Count)
        {
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

        if (cameraEffect != null)
            cameraEffect.ToggleEffect();

        UpdateWaveText();

        OnTurnSetupStart?.Invoke(currentTurn);
    }
    private void UpdateWaveText()
    {
        if (battleMapUI != null)
            battleMapUI.UpdateTextWave(currentWavesIndex);
    }

    private void ResetBattleState()
    {

        CommitEarnedRewards();

        var enemySpawner = battleSpawnEnemy != null ? battleSpawnEnemy : spawnEnemy;

        if (spawnDuck != null)
            spawnDuck.DespawnAllDucks();

        if (enemySpawner != null)
            enemySpawner.ClearSpawnedEnemies();

        if (battleGridManager != null)
            battleGridManager.ResetGrid();

        ClearAllBattleShopItems();

        if (myTem != null)
            myTem.InitHP(myTem.BaseHP);

        SetMoney(startMoney);
        SetPower(0);

        if (battleMapUI != null)
            battleMapUI.HideResultPanels();

        if (cameraEffect != null)
            cameraEffect.ResetToOrigin();

        currentTurn = 0;
        currentWavesIndex = 1;
        _isPaused = false;
    }

    private void ClearAllBattleShopItems()
    {
        foreach (var gear in GameObject.FindObjectsOfType<GearItemUI>(true))
            if (gear != null) Destroy(gear.gameObject);

        foreach (var unit in GameObject.FindObjectsOfType<UnitPlayerItemUI>(true))
            if (unit != null) Destroy(unit.gameObject);

        foreach (var gridItem in GameObject.FindObjectsOfType<GridShopItemUI>(true))
            if (gridItem != null) Destroy(gridItem.gameObject);
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

public enum BattleResult
{
    Continue, 
    Win,
    Lose
}
