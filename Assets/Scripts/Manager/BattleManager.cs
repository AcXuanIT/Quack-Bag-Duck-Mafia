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
///   - ResetBattleState() (StartBattle()/ReturnToMenu()): gọi cameraEffect.ResetToOrigin() để
///     SNAP NGAY về trạng thái gốc (không animation) và reset cờ nội bộ _isAnimated về false.
///     Bắt buộc phải có bước này: nếu thoát trận giữa chừng (VD bấm Back Menu ngay khi đang ở
///     TurnSetup, tức cameraEffect đang ở trạng thái đã PlayEffect()) mà không reset, cờ
///     _isAnimated sẽ giữ nguyên true xuyên sang trận sau — khiến lần ToggleEffect() đầu tiên
///     của trận mới (BeginTurnSetup()) gọi NHẦM ReverseEffect() thay vì PlayEffect() (do
///     ToggleEffect() chỉ dựa vào _isAnimated để quyết định hướng chạy).
///
/// Spawn Enemy (BattleSpawnEnemy):
///   - Khi vào TurnBattle: gọi battleSpawnEnemy.SpawnWave(currentWavesIndex, mapData)
///     để spawn enemy theo đúng Wave data tương ứng, với mapData được lấy từ
///     DataManager (xem ResolveMapBattleData()).
///   - Mỗi Update() trong lúc TurnBattle: poll battleSpawnEnemy.IsWaveCleared() (xem
///     CheckWaveCleared()) — khi toàn bộ enemy của Wave hiện tại đã spawn xong và đều đã
///     chết, tự động dọn sạch UnitDuck còn lại trên sân (spawnDuck.DespawnAllDucks()) rồi
///     chuyển Game về TurnSetup cho turn kế tiếp (hoặc Win nếu đây là Wave cuối cùng).
///
/// Grid Reset (BattleGridManager):
///   - Khi StartBattle(): gọi battleGridManager.ResetGrid() để đảm bảo
///     mỗi trận đấu mới luôn bắt đầu với lưới sạch (3x3 giữa Unlocked,
///     phần còn lại Locked), không giữ trạng thái unlock của trận trước.
///
/// Battle Map UI (BattleMapUI):
///   - Mỗi khi 1 Wave mới bắt đầu (StartBattle() và mỗi lần BeginTurnSetup() cho turn kế
///     tiếp), gọi battleMapUI.UpdateTextWave(currentWavesIndex) để cập nhật text hiển thị
///     Wave hiện tại trên UI.
///   - Khi StartBattle(): gọi battleMapUI.HideResultPanels() để đảm bảo panel Win/Lose
///     của trận trước không còn hiển thị.
///   - Khi chuyển sang BattleState.Win: gọi battleMapUI.ShowWin().
///   - Khi chuyển sang BattleState.Lose: gọi battleMapUI.ShowLose().
///   - Mỗi khi playerMoney thay đổi (xem mục Player Money bên dưới): gọi
///     battleMapUI.UpdateTextPlayerMoney(playerMoney).
///   - Mỗi khi power thay đổi (xem mục Power bên dưới): gọi
///     battleMapUI.UpdateTextPower(power).
///
/// Player Money (playerMoney):
///   - Nguồn dữ liệu DUY NHẤT cho số tiền của Player trong 1 trận đấu — ShopBatteManager
///     KHÔNG còn tự giữ biến tiền riêng nữa (đã xoá _playerGold), mà đọc/trừ tiền thẳng qua
///     BattleManager (shopBatteManager tham chiếu ngược lại battleManager, còn BattleManager
///     giữ tham chiếu tới shopBatteManager để đẩy cập nhật UI Shop mỗi khi tiền thay đổi —
///     xem NotifyMoneyChanged()).
///   - Bắt đầu Battle (ResetBattleState(), dùng chung cho StartBattle()/ReturnToMenu()):
///     playerMoney được reset về startMoney (mặc định 50).
///   - Thắng xong 1 Wave (CheckWaveCleared() xác nhận battleSpawnEnemy.IsWaveCleared() == true,
///     BẤT KỂ đây là Wave cuối cùng hay còn Wave tiếp theo): playerMoney += moneyPerWaveWin
///     (mặc định 50) qua AddMoney().
///   - Mua item trong Shop (ShopBatteManager.OnBuyPressed()): gọi SpendMoney(buyPrice) — trừ
///     tiền và trả về false nếu không đủ (Shop tự chặn giao dịch khi false).
///
/// Power (power):
///   - Nguồn dữ liệu DUY NHẤT cho tổng điểm sức mạnh (Power) hiện tại của đội hình Player
///     trong 1 trận đấu. KHÁC với playerMoney (do Player tự cộng/trừ qua hành động Shop),
///     power được TÍNH LẠI TỪ ĐẦU bởi BattleGridManager mỗi khi hệ thống Cells thay đổi
///     (đặt/gỡ Gear hoặc Unit trên Battle Grid — xem BattleGridManager.RefreshGearUnitLinks()
///     → RecalculateTotalPower()), rồi đẩy thẳng qua SetPower() — BattleManager KHÔNG tự ý
///     cộng/trừ power ở bất kỳ chỗ nào khác.
///   - Công thức: mỗi GearItem (WeaponEntry) ĐANG ĐẶT trên Grid VÀ đang liên kết (liền kề 4
///     hướng) với ít nhất 1 Unit sẽ đóng góp WeaponEntry.GetCurrentPower() (Power tại Level
///     hiện tại của weapon) NHÂN với số Unit đang kết nối với nó. Gear không liền kề Unit nào
///     đóng góp 0. Tổng power = tổng đóng góp của toàn bộ Gear trên Grid.
///   - Reset về 0 khi ResetBattleState() (StartBattle()/ReturnToMenu()) — Grid cũng được
///     ResetGrid() cùng lúc nên không còn Gear/Unit nào trên bàn cờ.
///   - Mỗi khi power thay đổi (SetPower()): gọi battleMapUI.UpdateTextPower(power) để cập
///     nhật UI (đếm số chạy dần, xem BattleMapUI.UpdateTextPower()).
///
/// My Team (MyTeam):
///   - BattleManager lắng nghe myTem.OnDeath (HP <= 0) để tự động chuyển trận đấu
///     sang BattleState.Lose bất kể đang ở TurnSetup hay TurnBattle (xem HandleMyTeamDeath()).
///
/// Map Battle Data:
///   - currentMapBattleData là MapBattleData của level đang chiến đấu.
///     Có thể được gán tay từ bên ngoài qua SetMapBattleData() (ví dụ
///     GameManager.OnBattle), hoặc nếu chưa được gán, BattleManager sẽ tự lấy
///     từ DataManager.Instance.MapBattleData theo mapBattleIndex (xem
///     ResolveMapBattleData()) ngay khi cần dùng (FinishTurnSetup()).
///
/// Điều Kiện Thắng/Thua:
///   - Win: currentWavesIndex là Wave cuối cùng (>= CurrentMapBattleData.WaveCount)
///     VÀ toàn bộ enemy của Wave đó đã chết hết (battleSpawnEnemy.IsWaveCleared()).
///   - Lose: HP của MyTeam <= 0 (myTem.IsDead / sự kiện myTem.OnDeath).
///
/// DỌN DẸP / RESET TRẬN ĐẤU (ResetBattleState — dùng chung cho cả StartBattle() VÀ ReturnToMenu()):
///   Trước đây StartBattle() chỉ ResetGrid() + HideResultPanels(), KHÔNG dọn UnitDuck/EnemyDuck
///   còn sống sót từ trận trước (VD Lose giữa chừng khi vẫn còn Duck/Enemy trên sân), KHÔNG dọn
///   các GearItemUI/UnitPlayerItemUI ĐÃ ĐẶT LÊN Battle Grid (khi Place lên Grid, item bị reparent
///   ra khỏi componentContainer của Shop nên ShopBatteManager không còn track được nữa — dễ bị
///   "mồ côi" tồn tại xuyên suốt các trận nếu không dọn tay), KHÔNG reset lại HP của MyTeam
///   (MyTeam.InitHP() chỉ chạy 1 lần trong Awake()), KHÔNG reset cameraEffect (gây bug lệch
///   Play/Reverse mô tả ở mục "Camera Effect" phía trên), và KHÔNG reset playerMoney. Giờ
///   ResetBattleState() xử lý dứt điểm toàn bộ (kể cả playerMoney và power), được gọi ở ĐẦU
///   StartBattle() (an toàn dù trận trước kết thúc bất thường) và trong ReturnToMenu().
///
/// QUAY VỀ MENU (ReturnToMenu — gọi từ BattleSettingController.BackToMenu(), dùng chung cho cả nút
/// Back Menu trong Pause VÀ nút trên PanelWin/PanelLose khi kết thúc ván đấu): gọi ResetBattleState()
/// rồi đưa currentState về Intro (trạng thái "chưa bắt đầu"), không tự động SetState liên quan tới
/// UI (việc bật/tắt GameObject UI Menu/BatteMap là trách nhiệm của BattleSettingController).
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

    [Header("=== UI ===")]
    [Tooltip("BattleMapUI — được gọi UpdateTextWave(currentWavesIndex) mỗi khi 1 Wave mới bắt đầu, " +
             "UpdateTextPlayerMoney(playerMoney)/UpdateTextPower(power) mỗi khi tiền/power thay đổi, " +
             "và ShowWin()/ShowLose()/HideResultPanels() khi trận đấu kết thúc/bắt đầu lại")]
    [SerializeField] private BattleMapUI battleMapUI;

    [Header("Spawn")]
    [SerializeField] public BattleSpawnEnemy spawnEnemy;
    [SerializeField] public BattleSpawnDuck spawnDuck;

    [Header("=== My Team ===")]
    [Tooltip("MyTeam của người chơi — chứa MyTeamAnimation dùng để gọi AnimationSpawn() mỗi khi spawn Duck. " +
             "BattleManager lắng nghe OnDeath của MyTeam để chuyển trận đấu sang Lose khi HP <= 0.")]
    [SerializeField] public MyTeam myTem;

    [Header("=== Player Money ===")]
    [Tooltip("Số tiền (money) hiện tại của Player trong trận đấu — nguồn dữ liệu DUY NHẤT (ShopBatteManager " +
             "không còn giữ biến tiền riêng, đọc/trừ tiền thẳng qua BattleManager). Reset = startMoney khi " +
             "ResetBattleState() (StartBattle()/ReturnToMenu()), +moneyPerWaveWin mỗi khi thắng xong 1 Wave.")]
    [SerializeField] private int playerMoney;
    public int PlayerMoney => playerMoney;

    [Tooltip("Số tiền khởi điểm khi bắt đầu 1 trận đấu mới (ResetBattleState()). Mặc định 50.")]
    [SerializeField] private int startMoney = 50;

    [Tooltip("Số tiền được cộng thêm mỗi khi thắng xong 1 Wave (xem CheckWaveCleared()). Mặc định 50.")]
    [SerializeField] private int moneyPerWaveWin = 50;

    [Tooltip("ShopBatteManager của trận đấu — BattleManager đẩy cập nhật UI Shop (RefreshUI()) tới đây " +
             "mỗi khi playerMoney thay đổi, để giá/nút Buy trong Shop luôn khớp với tiền hiện tại.")]
    [SerializeField] private ShopBatteManager shopBatteManager;

    [Header("=== Power ===")]
    [Tooltip("Tổng điểm sức mạnh (Power) hiện tại của đội hình Player trên Battle Grid — nguồn dữ liệu " +
             "DUY NHẤT (không nơi nào khác tự cộng/trừ). Được BattleGridManager TÍNH LẠI TOÀN BỘ và đẩy " +
             "qua SetPower() mỗi khi hệ thống Cells thay đổi (đặt/gỡ Gear hoặc Unit trên Grid), theo công " +
             "thức: mỗi Gear đang liền kề Unit đóng góp WeaponEntry.GetCurrentPower() x số Unit đang liền " +
             "kề (xem BattleGridManager.RecalculateTotalPower()). Reset về 0 khi ResetBattleState().")]
    [SerializeField] private int power;
    public int Power => power;

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

    /// <summary>Phát khi ReturnToMenu() dọn dẹp xong trận đấu (dùng cho UI khác lắng nghe nếu cần).</summary>
    public event Action OnReturnToMenu;

    /// <summary>Phát mỗi khi playerMoney thay đổi (SetMoney/AddMoney/SpendMoney), truyền giá trị mới.</summary>
    public event Action<int> OnMoneyChanged;

    /// <summary>Phát mỗi khi power thay đổi (SetPower()), truyền giá trị mới.</summary>
    public event Action<int> OnPowerChanged;

    // ─── Unity Lifecycle ────────────────────────────────────

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

    // ─── Public API ─────────────────────────────────────────

    /// <summary>Gán MapBattleData cho trận đấu sắp diễn ra (gọi từ GameManager.OnBattle trước khi mở Battle).</summary>
    public void SetMapBattleData(MapBattsleData mapBattleData)
    {
        currentMapBattleData = mapBattleData;
    }

    /// <summary>
    /// Bắt đầu 1 trận đấu mới từ đầu. Luôn gọi ResetBattleState() trước tiên để đảm bảo KHÔNG còn
    /// dữ liệu/vật thể nào sót lại từ trận trước (Duck/Enemy/Gear/Unit đã spawn, HP MyTeam, Grid,
    /// CameraEffect, playerMoney, power), dù trận trước kết thúc bình thường (Win/Lose) hay bị thoát giữa chừng.
    /// </summary>
    public void StartBattle()
    {
        ResetBattleState();

        UpdateWaveText();

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
                if (battleMapUI != null)
                    battleMapUI.ShowWin();
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

    /// <summary>
    /// Kết thúc ván đấu và quay về MenuGame — gọi từ BattleSettingController.BackToMenu() (dùng
    /// chung cho cả nút "Back Menu" trong Pause VÀ nút trên PanelWin/PanelLose khi ván đấu kết thúc).
    /// Dọn sạch TOÀN BỘ vật thể/trạng thái của trận đấu hiện tại qua ResetBattleState() (UnitDuck,
    /// EnemyDuck, Grid, Gear/Unit đã spawn kể cả đã đặt trên Grid, HP MyTeam, turn/wave, playerMoney,
    /// power, CameraEffect) rồi đưa currentState về Intro (trạng thái "chưa bắt đầu trận nào"). KHÔNG
    /// tự bật/tắt bất kỳ GameObject UI nào (Menu/BatteMap) — việc đó là trách nhiệm của
    /// BattleSettingController.
    /// </summary>
    public void ReturnToMenu()
    {
        ResetBattleState();
        SetState(BattleState.Intro);
        OnReturnToMenu?.Invoke();
    }

    // ─── Player Money ───────────────────────────────────────

    /// <summary>
    /// Cộng thêm tiền cho Player (VD thưởng ngoài luồng thắng Wave, nếu cần dùng ở nơi khác).
    /// Không làm gì nếu amount &lt;= 0.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        playerMoney += amount;
        NotifyMoneyChanged();
    }

    /// <summary>
    /// Trừ tiền của Player — dùng khi mua item trong Shop (ShopBatteManager.OnBuyPressed()).
    /// Trả về false (KHÔNG trừ tiền) nếu playerMoney hiện tại không đủ amount.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (playerMoney < amount) return false;

        playerMoney -= amount;
        NotifyMoneyChanged();
        return true;
    }

    /// <summary>Set thẳng playerMoney về 1 giá trị cụ thể (dùng khi ResetBattleState() reset về startMoney).</summary>
    private void SetMoney(int amount)
    {
        playerMoney = amount;
        NotifyMoneyChanged();
    }

    /// <summary>Đẩy playerMoney mới nhất ra UI (BattleMapUI.textPlayerMoney) và Shop (ShopBatteManager.RefreshUI()).</summary>
    private void NotifyMoneyChanged()
    {
        if (battleMapUI != null)
            battleMapUI.UpdateTextPlayerMoney(playerMoney);

        if (shopBatteManager != null)
            shopBatteManager.RefreshUI();

        OnMoneyChanged?.Invoke(playerMoney);
    }

    // ─── Power ──────────────────────────────────────────────

    /// <summary>
    /// Set thẳng tổng Power hiện tại của đội hình Player — gọi từ
    /// BattleGridManager.RecalculateTotalPower() mỗi khi hệ thống Cells thay đổi (đặt/gỡ Gear
    /// hoặc Unit trên Battle Grid), theo công thức: mỗi Gear đang liền kề Unit đóng góp
    /// WeaponEntry.GetCurrentPower() x số Unit đang liền kề. BattleManager KHÔNG tự tính công
    /// thức này — chỉ giữ giá trị và đẩy ra UI. Không làm gì nếu giá trị không đổi.
    /// </summary>
    public void SetPower(int newPower)
    {
        if (power == newPower) return;
        power = newPower;

        if (battleMapUI != null)
            battleMapUI.UpdateTextPower(power);

        OnPowerChanged?.Invoke(power);
    }

    // ─── Internal ───────────────────────────────────────────

    /// <summary>
    /// Poll mỗi Update() khi đang TurnBattle: nếu toàn bộ enemy của Wave hiện tại (đã spawn
    /// xong và đều được quản lý trong BattleSpawnEnemy) đều đã dead — kiểm tra qua
    /// battleSpawnEnemy.IsWaveCleared() — thì dọn sạch UnitDuck còn lại trên sân đấu
    /// (spawnDuck.DespawnAllDucks(), dùng PoolingManager.Despawn), cộng tiền thưởng thắng Wave
    /// (AddMoney(moneyPerWaveWin) — BẤT KỂ đây là Wave cuối cùng hay còn Wave tiếp theo, vì
    /// "thắng xong 1 Wave" luôn được thưởng tiền) rồi:
    ///   - Nếu Wave hiện tại là Wave cuối cùng (currentWavesIndex >= CurrentMapBattleData.WaveCount)
    ///     → kết thúc trận đấu với BattleResult.Win.
    ///   - Ngược lại → chuyển Game về TurnSetup (BattleResult.Continue) để bắt đầu turn kế tiếp.
    /// </summary>
    private void CheckWaveCleared()
    {
        if (currentState != BattleState.TurnBattle) return;
        if (battleSpawnEnemy == null || !battleSpawnEnemy.IsWaveCleared()) return;

        if (spawnDuck != null)
            spawnDuck.DespawnAllDucks();

        // Thắng xong 1 Wave -> +moneyPerWaveWin (mặc định 50), áp dụng cho MỌI Wave (kể cả Wave cuối).
        AddMoney(moneyPerWaveWin);

        MapBattsleData mapData = currentMapBattleData ?? ResolveMapBattleData();
        bool isLastWave = mapData != null && currentWavesIndex >= mapData.WaveCount;

        FinishTurnBattle(isLastWave ? BattleResult.Win : BattleResult.Continue);
    }

    /// <summary>
    /// Được gọi khi myTem.OnDeath bắn ra (HP của MyTeam đã về 0). Kết thúc trận đấu ngay lập
    /// tức với BattleState.Lose, bất kể đang ở TurnSetup hay TurnBattle (bỏ qua nếu trận đấu
    /// đã kết thúc — Win/Lose — hoặc đang Pause).
    /// </summary>
    private void HandleMyTeamDeath()
    {
        if (currentState == BattleState.Win || currentState == BattleState.Lose) return;

        SetState(BattleState.Lose);

        if (battleMapUI != null)
            battleMapUI.ShowLose();

        OnLose?.Invoke();
    }

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

        UpdateWaveText();

        OnTurnSetupStart?.Invoke(currentTurn);
    }

    /// <summary>Cập nhật text Wave trên BattleMapUI (nếu đã được gán) theo currentWavesIndex.</summary>
    private void UpdateWaveText()
    {
        if (battleMapUI != null)
            battleMapUI.UpdateTextWave(currentWavesIndex);
    }

    /// <summary>
    /// Dọn sạch TOÀN BỘ vật thể/trạng thái runtime của 1 trận đấu, dùng chung cho cả StartBattle()
    /// (đảm bảo trận mới không dính dữ liệu cũ, kể cả khi trận trước bị thoát bất thường) VÀ
    /// ReturnToMenu() (thoát ván đấu về Menu). Gồm:
    ///   1. Despawn toàn bộ UnitDuck đang sống (BattleSpawnDuck.DespawnAllDucks() — qua PoolingManager).
    ///   2. Xoá toàn bộ EnemyDuck đang sống + dừng coroutine spawn dở dang (BattleSpawnEnemy.ClearSpawnedEnemies()).
    ///   3. Reset Battle Grid về trạng thái ban đầu (BattleGridManager.ResetGrid() — chỉ 3x3 giữa Unlocked;
    ///      không còn Gear/Unit nào trên bàn cờ nên power cũng cần reset về 0 — xem bước 6b).
    ///   4. Xoá TOÀN BỘ GearItemUI/UnitPlayerItemUI/GridShopItemUI còn sót lại trong scene — kể cả
    ///      những item ĐÃ ĐẶT lên Battle Grid (bị reparent ra khỏi componentContainer của Shop nên
    ///      ShopBatteManager không còn track được, dễ "mồ côi" nếu không quét toàn scene).
    ///   5. Reset HP của MyTeam về đầy (MyTeam.InitHP(BaseHP) — trước đây chỉ chạy 1 lần ở Awake()).
    ///   6a. Reset playerMoney về startMoney (mặc định 50) — SetMoney() tự đẩy cập nhật UI
    ///      (BattleMapUI.textPlayerMoney) và Shop (ShopBatteManager.RefreshUI()).
    ///   6b. Reset power về 0 (SetPower(0)) — tự đẩy cập nhật UI (BattleMapUI.textPower).
    ///   7. Ẩn 2 panel kết quả Win/Lose của trận trước.
    ///   8. Snap cameraEffect về trạng thái gốc (cameraEffect.ResetToOrigin()) — KHÔNG animation,
    ///      đồng thời reset cờ nội bộ _isAnimated về false, tránh bug ToggleEffect() lần đầu của
    ///      trận mới bị lệch chạy nhầm ReverseEffect() do trận trước thoát giữa chừng lúc đang
    ///      ở trạng thái đã PlayEffect() (xem class doc phần "Camera Effect").
    ///   9. Reset currentTurn/currentWavesIndex/_isPaused về trạng thái ban đầu.
    /// </summary>
    private void ResetBattleState()
    {
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

    /// <summary>
    /// Xoá mọi GearItemUI/UnitPlayerItemUI/GridShopItemUI còn tồn tại trong scene (quét toàn bộ,
    /// KHÔNG chỉ trong componentContainer của Shop) — bắt cả những item đã được kéo thả (Place)
    /// lên Battle Grid, vì khi Place chúng bị reparent sang BattleGridManager (BGGrid) nên
    /// ShopBatteManager.ClearAllItems() (chỉ quét componentContainer) sẽ bỏ sót.
    /// </summary>
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

/// <summary>Kết quả của 1 Turn Battle, dùng để quyết định bước tiếp theo.</summary>
public enum BattleResult
{
    Continue, // chưa phân thắng bại, tiếp tục turn kế
    Win,
    Lose
}
