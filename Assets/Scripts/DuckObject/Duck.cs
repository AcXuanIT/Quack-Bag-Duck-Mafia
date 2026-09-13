using UnityEngine;
using DG.Tweening;

/// <summary>
/// Lớp cơ sở (base) cho mọi "con vịt" chiến đấu trong trận (khác với DuckObject/EnemyObject
/// cũ chỉ là bản nháp) — UnitDuck (vịt của Player) và EnemyDuck (vịt địch) đều kế thừa từ đây.
///
/// CẤU TRÚC PREFAB MÀ SCRIPT NÀY GIẢ ĐỊNH (đã khớp unit_001_tier_1 / enemy_001_tier_1):
///   Root (Duck script + SpriteRenderer "duck" + DuckMoveAnimation + BoxCollider2D + Tag
///         "Player"/"Enemy" — chính BoxCollider2D này là mục tiêu bị ScanForward() bắt trúng,
///         ĐỒNG THỜI cũng là collider dùng để kiểm tra "đã đúng tầm đánh" khi Duck khác tấn công
///         mình — xem phần CHIẾN ĐẤU bên dưới)
///   ├── HPBar        → DuckHPBar
///   ├── Weapon        → SpriteRenderer "weapon" + Animator (WeaponAttack.controller — xem
///   │              PlayAttackAnimation() bên dưới). Rest pose (local position khi KHÔNG
///   │              tấn công) hiện = (0.2, 0.5, 0) — xem weaponRestLocalPosition bên dưới.
///   │        ├── Direction → CircleCollider2D (Is Trigger = true, bán kính = tầm đánh weapon —
///   │        │               ĐÂY LÀ COLLIDER DÙNG ĐỂ XÁC ĐỊNH ĐÚNG TẦM ĐÁNH, xem IsTargetInAttackRange())
///   │        └── PosVFX    → Transform trống, đánh dấu vị trí Spawn VFX của weapon (nếu
///   │                         weaponData.vfxWeapon != null) mỗi khi ra đòn — xem posVFX/SpawnWeaponVFX().
///   │                         Vị trí LUÔN được tự động tính lại (góc trên-phải sprite weapon) mỗi khi
///   │                         Image (Sprite) của weapon được gán vào Duck — xem RecalculatePosVFX().
///   └── VFXDead      → GameObject hiệu ứng khi Duck chết (component VFXDead — xem VFXDead.cs),
///                       MẶC ĐỊNH SetActive(false). Con: "TopVFX" (Animator chạy
///                       UnitDead.controller/EnemyDead.controller — feather/head bay ra) và
///                       "ButtomVFX" (SpriteRenderer tĩnh). Chỉ được BẬT LÊN bởi PlayDeadVFX()
///                       khi Duck chết — xem mục "CHẾT (Dead VFX)" bên dưới.
///
/// KHỞI TẠO: gọi Init(duckData, weaponData, duckTier, weaponTier) mỗi khi spawn (kể cả khi
/// lấy lại từ Pool) để reset toàn bộ chỉ số runtime (HP/Damage/sprite/tầm đánh) — không dựa
/// vào Awake()/giá trị cũ còn sót lại từ lần dùng trước của prefab trong Pool.
///
/// CHỈ SỐ:
///   Hp     = duckData.BaseHP + weaponData.GetCurrentHP()      (theo Level hiện tại của weapon)
///   Damage = weaponData.GetCurrentDamage()                     (theo Level hiện tại của weapon)
///   AttackRange (tầm đánh) = weaponData.GetAttackRange()
///
/// duckTier/weaponTier CHỈ dùng để chọn đúng sprite hiển thị (GetSprite(tier)/GetSpriteByTier(tier))
/// — KHÔNG ảnh hưởng Damage/HP (2 chỉ số này tính theo Level của WeaponEntry, không phải Tier).
///
/// DI CHUYỂN: AutoMoveTowards() di chuyển về 1 điểm đích, trục Y luôn bị giới hạn trong [0, 2]
/// (khớp phạm vi làn đánh của game) dù đích ở Y bao nhiêu.
///
/// CHIẾN ĐẤU (Raycast targeting + Physical attack-range check): mỗi Duck tự quét về phía trước
/// bằng ScanForward() (BoxCast dọc theo làn đánh) — subclass (UnitDuck/EnemyDuck) tự gọi hàm này
/// mỗi frame với hướng quét cố định của phe mình (UnitDuck: +X, EnemyDuck: -X) và tự lọc theo tag
/// để chọn currentTarget hợp lệ (có thể đổi target liên tục nếu có mục tiêu ưu tiên/gần hơn xuất
/// hiện, xem EnemyDuck/UnitDuck). Khi đã có currentTarget:
///   - Gọi IsTargetInAttackRange(currentTarget) để kiểm tra ĐÃ ĐÚNG TẦM ĐÁNH CHƯA — dựa trên va
///     chạm VẬT LÝ thực tế (Physics2D.IsTouching) giữa weaponRangeCollider (CircleCollider2D con
///     "Weapon/Direction", bán kính = AttackRange) của Duck này và Collider2D của target, KHÔNG
///     còn so khoảng cách tâm-tâm giữa 2 root Transform như trước (thiếu chính xác khi weapon có
///     offset hoặc target có BoxCollider2D lớn). Nếu đúng tầm -> dừng di chuyển, tự động tấn công
///     theo chu kỳ weaponData.TimeAttack (giây/đòn) qua UpdateAttack() — mỗi lần ra đòn cũng phát
///     animation vung/giật weapon qua PlayAttackAnimation() (xem bên dưới).
///   - Nếu chưa đúng tầm -> tiếp tục AutoMoveTowards() lại gần currentTarget.
///   - FALLBACK: nếu thiếu weaponRangeCollider (chưa gán Inspector) hoặc target không có
///     Collider2D (VD: target là MyTeam), IsTargetInAttackRange() tự động fallback về so khoảng
///     cách tâm-tâm (DistanceTo <= AttackRange) như cơ chế cũ.
///
/// ANIMATION TẤN CÔNG: weaponAnimator (Animator gắn trên child "Weapon", controller
/// WeaponAttack.controller — xem Assets/Animations/Weapon/) có 4 Trigger param:
///   - "AttackRanged" -> animation giật lùi (recoil), dùng cho weaponData.Category == Ranged.
///   - "AttackMelee"  -> animation vung chém (swing), dùng cho weaponData.Category == Melee.
///   - "AttackThrow"  -> animation vung tay ném weapon vào enemy (windup ra sau rồi bung tay ra
///                        trước), dùng cho weaponData.Category == Thrown (VD: bom, dao ném).
///   - "AttackBoom"   -> animation Weapon_Attack_Boom (weapon phát nổ tại chỗ), dùng cho
///                        weaponData.Category == Boom (VD: mìn, pháo, bom tự kích nổ).
/// PlayAttackAnimation() tự chọn đúng trigger dựa theo weaponData.Category mỗi khi UpdateAttack()
/// ra đòn (KHÔNG phát mỗi frame — chỉ phát đúng lúc _attackTimer reset, tức đúng chu kỳ TimeAttack).
/// Ngay sau khi SetTrigger(), PlayAttackAnimation() cũng gọi SpawnWeaponVFX() để bắn VFX của
/// weapon (nếu có, xem WeaponEntry.vfxWeapon) tại vị trí posVFX.
///
/// VFX TẤN CÔNG: nếu weaponData.vfxWeapon (GameObject, xem WeaponData.cs) khác null VÀ posVFX
/// (child "Weapon/PosVFX") đã được gán, SpawnWeaponVFX() sẽ Spawn prefab đó qua PoolingManager
/// tại vị trí/góc quay của posVFX mỗi khi ra đòn. Có 2 nhánh xử lý tuỳ Category:
///   - Ranged/Melee/Thrown: nếu prefab VFX có component VFXGun, RunVFX() được gọi ngay để tự phát
///     animation TẠI CHỖ (posVFX) rồi tự Despawn khi animation chạy xong (xem VFXGun.cs).
///   - Boom: prefab VFX (thường là Assets/Prefabs/VFX/Boom.prefab, component BoomVFX — xem
///     BoomVFX.cs) được SpawnBoomVFX() truyền dữ liệu (sprite weapon, Damage, tag phe) rồi gọi
///     Launch() để bay VÒNG CUNG từ vị trí Duck này (attacker) tới vị trí currentTarget, tự nổ +
///     gây AoE damage cho phe địch khi tới nơi, rồi tự Despawn.
///
/// VỊ TRÍ posVFX (GÓC TRÊN-PHẢI SPRITE WEAPON): mỗi khi Init() gán Image mới cho weaponRenderer
/// (weaponRenderer.sprite = weapon.GetSpriteByTier(...)), RecalculatePosVFX() được gọi ngay sau
/// đó để tính lại vị trí posVFX = góc trên-phải (top-right) của bounds sprite weapon vừa gán,
/// trong local space của "Weapon" (posVFX và weaponRenderer cùng chung transform cha). Nhờ vậy
/// mỗi weapon (kích thước/hình dạng sprite khác nhau) đều tự có posVFX đúng vị trí góc trên-phải
/// của chính nó mà không cần chỉnh tay trong Inspector. Có tính đến flipX/flipY của weaponRenderer
/// (nếu bị lật, góc trên-phải VISUAL đổi sang cạnh đối diện trong local space).
///
/// FIX BUG LỆCH VỊ TRÍ WEAPON (Y): 3 clip tấn công trên đều animate m_LocalPosition của Weapon
/// xoay quanh weaponRestLocalPosition (rest pose thật, VD (0.2, 0.5, 0) — KHÔNG phải (0,0,0)).
/// Trước đây các clip animate quanh (0,0,0) trong khi rest pose thật là (0.2, 0.5, 0), nên Duck
/// nào đã tấn công ít nhất 1 lần sẽ bị "kẹt" Weapon lệch về gần (0,0,0) vĩnh viễn (do Animator
/// Write Defaults chụp lại giá trị SAI làm baseline mới và KHÔNG tự tái chụp khi Rebind() được
/// gọi lại — xem chi tiết trong Init() bên dưới) — gây hiện tượng Weapon lệch Y khác nhau giữa
/// các UnitDuck (con đã tấn công vs con chưa tấn công lần nào).
///
/// NHẬN DAMAGE: mỗi khi TakeDamage() bị gọi (Hp giảm), HPBar cập nhật ngay (DuckHPBar.SetHP)
/// VÀ đồng thời phát hiệu ứng nháy màu (duckAnimation.PlayDamageFlash) để báo hiệu trực quan.
/// Hiệu ứng nháy màu được quản lý tập trung trong DuckMoveAnimation (cùng chỗ với hiệu ứng
/// squash/stretch khi di chuyển), Duck chỉ gọi qua chứ không tự giữ Tween/Color riêng —
/// cùng kiến trúc với MyTeamAnimation (gộp Spawn/Bounce/Flash vào 1 component).
///
/// XP WEAPON KHI DIỆT ĐỊCH: mỗi khi đòn đánh của Duck này (qua DealDamage(Duck)) khiến target
/// CHUYỂN từ còn sống -> chết (IsDead), OnKilledTarget(target) được gọi đúng 1 lần. Mặc định
/// (xem OnKilledTarget bên dưới): nếu target là EnemyDuck, weaponData của Duck này (Duck vừa ra
/// đòn) được cộng +1 XP qua weaponData.AddXP(1) — đúng nghĩa "XP = số EnemyDuck đã bị tiêu diệt
/// bởi weapon này" (xem WeaponEntry.XP/XPToNextLevel/AddXP trong WeaponData.cs). Vì trong luật
/// chơi hiện tại chỉ UnitDuck mới có thể hạ EnemyDuck (EnemyDuck không đánh EnemyDuck khác) nên
/// XP thực tế chỉ được cộng cho weapon của UnitDuck, nhưng logic được đặt ở lớp cha Duck để dùng
/// chung, tổng quát, không cần sửa BattleManager/UnitDuck/EnemyDuck.
///
/// CHẾT (Dead VFX): khi Hp về 0, CheckDead() đặt IsDead = true rồi gọi PlayDeadVFX() thay vì
/// Despawn() ngay lập tức:
///   1. Ẩn hình ảnh gốc của Duck (duckRenderer, weaponRenderer) và HPBar (hpBar.gameObject) —
///      nhường chỗ hiển thị cho hiệu ứng VFXDead.
///   2. Nếu có vfxDead (child "VFXDead", component VFXDead — xem VFXDead.cs): SetActive(true)
///      để nó tự chạy animation Dead rồi mờ dần (OnEnable() của VFXDead tự lo toàn bộ), lắng
///      nghe VFXDead.OnFinished để biết đúng lúc gọi Despawn() thật sự — KHÔNG gọi Despawn()
///      ngay, vì PoolingManager.Despawn() sẽ SetActive(false) GameObject cha (Root Duck), làm
///      tắt luôn VFXDead (là child) đang chạy dở animation/mờ dần.
///   3. Nếu KHÔNG có vfxDead (chưa gán trong Inspector/prefab) -> fallback Despawn() ngay lập
///      tức như hành vi cũ, để không phá vỡ Duck nào chưa có VFXDead.
/// Init() (mỗi lần spawn/lấy lại từ Pool) tự bật lại duckRenderer/weaponRenderer/hpBar và ép
/// vfxDead về SetActive(false), đảm bảo Duck lấy từ Pool sau khi từng chết luôn sạch trạng thái.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public abstract class Duck : MonoBehaviour
{
    [Header("=== Visual ===")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh con vịt (thường ở Root)")]
    [SerializeField] protected SpriteRenderer duckRenderer;

    [Tooltip("SpriteRenderer hiển thị hình ảnh vũ khí (thường ở child \"Weapon\")")]
    [SerializeField] protected SpriteRenderer weaponRenderer;

    [Tooltip("Animator gắn trên child \"Weapon\" — chạy WeaponAttack.controller để phát animation " +
             "tấn công (giật lùi/vung chém/vung ném) mỗi khi ra đòn, xem PlayAttackAnimation().")]
    [SerializeField] protected Animator weaponAnimator;

    [Tooltip("GameObject \"VFXDead\" (component VFXDead — xem VFXDead.cs), mặc định SetActive(false). " +
             "Được BẬT LÊN bởi PlayDeadVFX() khi Duck chết để chạy animation + hiệu ứng mờ dần trước " +
             "khi Duck thực sự Despawn() về Pool. Tự động tìm trong Reset() nếu để trống.")]
    [SerializeField] protected GameObject vfxDead;

    [Header("=== Combat ===")]
    [Tooltip("CircleCollider2D (Is Trigger) con \"Weapon/Direction\", bán kính = AttackRange — " +
             "DÙNG ĐỂ XÁC ĐỊNH ĐÚNG TẦM ĐÁNH bằng va chạm vật lý thực tế qua Physics2D.IsTouching() " +
             "với Collider2D của target (xem IsTargetInAttackRange()). Nếu để trống, tự động " +
             "fallback về so khoảng cách tâm-tâm (DistanceTo <= AttackRange).")]
    [SerializeField] protected CircleCollider2D weaponRangeCollider;

    [Tooltip("Transform con \"Weapon/PosVFX\" — vị trí (và góc quay) dùng để Spawn VFX của weapon " +
             "(weaponData.vfxWeapon, xem WeaponData.cs) mỗi khi ra đòn, xem SpawnWeaponVFX(). " +
             "Tự động tìm trong Reset() nếu để trống. Vị trí (localPosition, tính theo local space " +
             "của \"Weapon\") LUÔN được tự động tính lại = góc trên-phải sprite weapon mỗi khi " +
             "Image weapon được gán, xem RecalculatePosVFX().")]
    [SerializeField] protected Transform posVFX;

    [Header("=== Targeting (Raycast) ===")]
    [Tooltip("Khoảng cách quét về phía trước (đơn vị) để tìm mục tiêu — subclass gọi ScanForward() " +
             "với giá trị này mỗi khi chưa có currentTarget.")]
    [SerializeField] protected float scanDistance = 30f;

    [Tooltip("Bề rộng (chiều cao) vùng quét BoxCast — nên >= biên độ làn đánh [0,2] để không bỏ " +
             "sót mục tiêu lệch trục Y so với Duck đang quét.")]
    [SerializeField] protected float scanHeight = 2.2f;

    [Header("=== Refs ===")]
    [Tooltip("Script hiệu ứng di chuyển (squash/stretch) + nháy damage — thường ở Root")]
    [SerializeField] protected DuckMoveAnimation duckAnimation;

    [Tooltip("Script thanh máu — thường ở child \"HPBar\"")]
    [SerializeField] protected DuckHPBar hpBar;

    [Header("=== Weapon Rest Pose (Fix lệch vị trí) ===")]
    [Tooltip("Local position GỐC (rest pose) của child \"Weapon\" khi KHÔNG tấn công — tự động " +
             "capture trong Reset() từ vị trí hiện có trên prefab (VD: (0.2, 0.5, 0)). Init() dùng " +
             "giá trị này để ÉP RESET lại Weapon về đúng vị trí mỗi lần spawn/lấy từ Pool, tránh " +
             "bug: animation tấn công (Ranged/Melee/Thrown) animate localPosition quanh rest pose " +
             "SAI (0,0,0) rồi khi quay về Idle (không có curve vị trí) bị \"kẹt\" lại giá trị dở " +
             "dang thay vì về đúng rest pose thật — gây hiện tượng Weapon lệch Y khác nhau giữa " +
             "các Duck đã tấn công và Duck chưa tấn công lần nào.")]
    [SerializeField] protected Vector3 weaponRestLocalPosition;

    // ─── Data ───────────────────────────────────────────────
    protected BaseDuckData duckData;
    protected WeaponEntry  weaponData;
    protected int duckTier;
    protected int weaponTier;

    // ─── Runtime Stats ──────────────────────────────────────
    public float MaxHp   { get; protected set; }
    public float Hp      { get; protected set; }
    public float Damage  { get; protected set; }
    public bool  IsDead  { get; protected set; }

    /// <summary>Tầm đánh hiện tại, lấy từ weaponData.GetAttackRange() (0 nếu chưa có weaponData).</summary>
    public float AttackRange => weaponData != null ? weaponData.GetAttackRange() : 0f;

    // ─── Combat Runtime ─────────────────────────────────────
    protected Transform currentTarget;
    protected bool      isAttacking;
    private   float     _attackTimer;

    // Cache component VFXDead trên vfxDead (nếu có) — tránh GetComponent() lặp lại mỗi lần chết.
    private VFXDead _vfxDeadComp;
    private bool    _vfxDeadCompResolved;

    // Tên các Trigger param trên WeaponAttack.controller — xem Assets/Animations/Weapon/.
    private const string AttackRangedTrigger = "AttackRanged";
    private const string AttackMeleeTrigger  = "AttackMelee";
    private const string AttackThrowTrigger  = "AttackThrow";
    private const string AttackBoomTrigger   = "AttackBoom";

    protected const float MinLaneY = 0f;
    protected const float MaxLaneY = 2f;

    /// <summary>
    /// Kiểm tra Battle hiện có đang ở trạng thái cần DỪNG mọi hoạt động của Duck hay không — TRUE
    /// khi BattleManager.CurrentState là Pause, Win, hoặc Lose (trận đấu đang tạm dừng hoặc đã kết
    /// thúc). UnitDuck/EnemyDuck gọi hàm này ở đầu Update() để "đóng băng" Duck tại chỗ ngay lập
    /// tức (không quét mục tiêu, không di chuyển, không đổi target, không tấn công) trong các
    /// trạng thái này, không tự Destroy/tắt gì — Duck tiếp tục hoạt động bình thường ngay khi Battle
    /// quay lại TurnSetup/TurnBattle (Resume()). Trả về false (không dừng) nếu chưa có
    /// BattleManager.Instance (VD scene test riêng lẻ không dùng BattleManager) để không phá vỡ
    /// các scene/test không phụ thuộc BattleManager.
    /// </summary>
    protected bool IsBattleStopped()
    {
        var battleManager = BattleManager.Instance;
        if (battleManager == null) return false;

        var state = battleManager.CurrentState;
        return state == BattleManager.BattleState.Pause
            || state == BattleManager.BattleState.Win
            || state == BattleManager.BattleState.Lose;
    }

    protected virtual void Reset()
    {
        if (duckRenderer == null) duckRenderer = GetComponent<SpriteRenderer>();
        if (duckAnimation == null) duckAnimation = GetComponent<DuckMoveAnimation>();
        if (hpBar == null) hpBar = GetComponentInChildren<DuckHPBar>(true);

        var vfxDeadTf = transform.Find("VFXDead");
        if (vfxDeadTf != null && vfxDead == null)
            vfxDead = vfxDeadTf.gameObject;

        var weaponTf = transform.Find("Weapon");
        if (weaponTf != null)
        {
            if (weaponRenderer == null) weaponRenderer = weaponTf.GetComponent<SpriteRenderer>();
            if (weaponAnimator == null) weaponAnimator = weaponTf.GetComponent<Animator>();
            var directionTf = weaponTf.Find("Direction");
            if (directionTf != null && weaponRangeCollider == null)
                weaponRangeCollider = directionTf.GetComponent<CircleCollider2D>();

            var posVFXTf = weaponTf.Find("PosVFX");
            if (posVFXTf != null && posVFX == null)
                posVFX = posVFXTf;

            // Capture rest pose thật của Weapon (VD (0.2, 0.5, 0)) để Init() dùng làm baseline
            // ép reset, tránh bug lệch Y mô tả ở class doc phía trên.
            weaponRestLocalPosition = weaponTf.localPosition;
        }
    }

    // ─── Init ───────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo lại TOÀN BỘ chỉ số runtime của Duck khi spawn (kể cả lấy lại từ Pool).
    /// duckTier/weaponTier chỉ dùng để chọn sprite hiển thị đúng Tier, không ảnh hưởng Damage/HP.
    /// </summary>
    public virtual void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        duckData   = duck;
        weaponData = weapon;
        duckTier   = duckTierIn;
        weaponTier = weaponTierIn;

        IsDead        = false;
        isAttacking   = false;
        currentTarget = null;
        _attackTimer  = 0f;

        float duckHp   = duck   != null ? duck.BaseHP            : 0f;
        float weaponHp = weapon != null ? weapon.GetCurrentHP()  : 0f;
        MaxHp = duckHp + weaponHp;
        Hp    = MaxHp;

        Damage = weapon != null ? weapon.GetCurrentDamage() : 0f;

        // Duck vừa spawn/lấy lại từ Pool -> đảm bảo hiển thị sạch, không còn dính trạng thái
        // "đã chết" (ẩn duckRenderer/weaponRenderer/hpBar, VFXDead đang bật) của lần dùng trước.
        if (duckRenderer != null) duckRenderer.enabled = true;
        if (weaponRenderer != null) weaponRenderer.enabled = true;
        if (hpBar != null) hpBar.gameObject.SetActive(true);
        if (vfxDead != null) vfxDead.SetActive(false);

        if (duckRenderer != null && duck != null)
            duckRenderer.sprite = duck.GetSprite(duckTierIn);

        if (weaponRenderer != null && weapon != null)
        {
            weaponRenderer.sprite = weapon.GetSpriteByTier(weaponTierIn);

            // Mỗi khi Image (Sprite) của weapon được gán vào Duck -> tính lại vị trí posVFX
            // (góc trên-phải sprite weapon vừa gán), xem RecalculatePosVFX().
            RecalculatePosVFX();
        }

        if (weaponRangeCollider != null && weapon != null)
        {
            weaponRangeCollider.isTrigger = true;
            weaponRangeCollider.radius    = weapon.GetAttackRange();
        }

        if (hpBar != null)
            hpBar.Heal(1f); // reset thanh máu về đầy lúc spawn

        if (duckAnimation != null)
            duckAnimation.enabled = true; // bật lại hiệu ứng di chuyển + reset nháy damage cũ
                                           // (có thể đã bị tắt lúc chết/tấn công trước đó — xem
                                           // DuckMoveAnimation.OnDisable, chạy khi CheckDead() tắt component)

        if (weaponAnimator != null)
        {
            // ÉP RESET vị trí Weapon về đúng rest pose mỗi lần Init() (kể cả lấy lại từ Pool).
            //
            // LƯU Ý QUAN TRỌNG (đã kiểm chứng thực tế trong PlayMode): CHỈ set localPosition rồi
            // gọi Rebind() là KHÔNG ĐỦ — Write Defaults của Animator KHÔNG tái chụp (re-capture)
            // baseline từ giá trị Transform hiện tại tại thời điểm Rebind(), mà áp lại baseline CŨ
            // đã "đóng băng" từ lần bind đầu tiên của riêng Animator component đó. Nếu baseline cũ
            // đã bị hỏng (do 1 lần tấn công trước đó khiến Init() chạy đúng lúc animation dở dang),
            // Rebind() sẽ tiếp tục áp SAI baseline đó mãi mãi.
            //
            // Cách khắc phục triệt để: tắt rồi bật lại GameObject "Weapon" — việc này buộc Animator
            // hủy toàn bộ trạng thái/pose cache nội bộ và bind lại từ đầu (OnEnable), chụp CHÍNH
            // XÁC giá trị Transform mà ta vừa gán làm baseline mới.
            var weaponGO = weaponAnimator.gameObject;
            weaponGO.SetActive(false);
            weaponGO.transform.localPosition = weaponRestLocalPosition;
            weaponGO.transform.localRotation = Quaternion.identity;
            weaponGO.SetActive(true);
            weaponAnimator.Rebind(); // reset về Idle, xoá trigger/queue cũ còn sót từ lần dùng trước (Pool)
        }
    }

    /// <summary>
    /// Tính lại vị trí posVFX = GÓC TRÊN-PHẢI (top-right) của bounds sprite hiện tại trên
    /// weaponRenderer, đặt bằng localPosition trong local space của "Weapon" (posVFX và
    /// weaponRenderer luôn cùng chung transform cha "Weapon" theo cấu trúc prefab, xem class doc).
    ///
    /// Được gọi ngay sau mỗi lần Init() gán Image mới cho weaponRenderer.sprite — nhờ vậy mỗi
    /// weapon (kích thước/hình dạng sprite khác nhau khi đổi Tier hoặc đổi weapon) đều tự động có
    /// posVFX đúng vị trí góc trên-phải của chính sprite đó mà không cần chỉnh tay trong Inspector.
    ///
    /// Sprite.bounds trả về bounds THEO ĐƠN VỊ LOCAL của chính SpriteRenderer (không phụ thuộc
    /// scale/rotation của Transform), nên bounds.max/min ánh xạ thẳng sang localPosition của child
    /// "PosVFX" (cùng cha "Weapon" với weaponRenderer).
    ///
    /// Có xét flipX/flipY của weaponRenderer: nếu sprite bị lật ngang/dọc khi hiển thị, góc
    /// trên-phải THỰC TẾ NHÌN THẤY nằm ở cạnh local đối diện (min thay vì max) — nên đảo trục
    /// tương ứng để posVFX luôn bám đúng góc trên-phải VISUAL, không chỉ đúng theo bounds gốc.
    ///
    /// Giữ nguyên trục Z hiện tại của posVFX (không đổi độ sâu render).
    /// </summary>
    protected virtual void RecalculatePosVFX()
    {
        if (posVFX == null || weaponRenderer == null || weaponRenderer.sprite == null) return;

        Bounds bounds = weaponRenderer.sprite.bounds;

        float topRightX = weaponRenderer.flipX ? bounds.min.x : bounds.max.x;
        float topRightY = weaponRenderer.flipY ? bounds.min.y : bounds.max.y;

        posVFX.localPosition = new Vector3(topRightX, topRightY - 0.13f, posVFX.localPosition.z);
    }

    // ─── Combat ─────────────────────────────────────────────

    /// <summary>
    /// Nhận sát thương. Cập nhật HP Bar, phát hiệu ứng nháy damage (duckAnimation.PlayDamageFlash),
    /// rồi kiểm tra chết (CheckDead → Despawn nếu Hp <= 0).
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Hp -= amount;

        if (hpBar != null)
            hpBar.SetHP(MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f);

        PlayDamageFlash();

        CheckDead();
    }

    /// <summary>
    /// Phát hiệu ứng nháy damage. Uỷ quyền hoàn toàn cho DuckMoveAnimation (component quản lý
    /// mọi hiệu ứng hình ảnh của Duck — squash/stretch khi di chuyển VÀ nháy màu khi bị đánh),
    /// Duck không tự giữ Tween/Color riêng nữa.
    /// </summary>
    protected virtual void PlayDamageFlash()
    {
        if (duckAnimation != null)
            duckAnimation.PlayDamageFlash();
    }

    /// <summary>
    /// Phát animation tấn công của weapon (giật lùi nếu Ranged, vung chém nếu Melee, vung ném nếu
    /// Thrown) qua weaponAnimator.SetTrigger(). Gọi đúng 1 lần mỗi khi UpdateAttack() thực sự ra
    /// đòn (không phát mỗi frame). Ngay sau khi trigger animation, gọi SpawnWeaponVFX() để bắn VFX
    /// của weapon (nếu có, xem WeaponEntry.vfxWeapon) tại vị trí posVFX. Không làm gì nếu thiếu
    /// weaponAnimator hoặc weaponData.
    /// </summary>
    protected virtual void PlayAttackAnimation()
    {
        if (weaponAnimator == null || weaponData == null) return;

        string trigger;
        switch (weaponData.Category)
        {
            case WeaponCategory.Melee:
                trigger = AttackMeleeTrigger;
                break;
            case WeaponCategory.Thrown:
                trigger = AttackThrowTrigger;
                break;
            case WeaponCategory.Boom:
                trigger = AttackBoomTrigger;
                break;
            default:
                trigger = AttackRangedTrigger;
                break;
        }

        weaponAnimator.SetTrigger(trigger);

        SpawnWeaponVFX();
    }

    /// <summary>
    /// Spawn VFX của weapon (weaponData.vfxWeapon) tại vị trí/góc quay của posVFX (child
    /// "Weapon/PosVFX") mỗi khi ra đòn, qua PoolingManager (để tái sử dụng thay vì Instantiate mới
    /// mỗi lần). Không làm gì nếu thiếu weaponData, weaponData.vfxWeapon == null (weapon không có
    /// VFX riêng), hoặc thiếu posVFX (chưa gán trong Inspector/prefab). Nếu prefab VFX có component
    /// VFXGun, gọi RunVFX() ngay để tự phát animation rồi tự Despawn khi animation chạy xong.
    /// </summary>
    protected virtual void SpawnWeaponVFX()
    {
        if (weaponData == null || weaponData.vfxWeapon == null || posVFX == null) return;

        GameObject vfxObj = PoolingManager.Spawn(weaponData.vfxWeapon, posVFX.position, posVFX.rotation);
        if (vfxObj == null) return;

        // weaponData.Category == Boom -> vfxWeapon là prefab Boom (component BoomVFX, xem
        // BoomVFX.cs) cần bay vòng cung tới currentTarget rồi tự nổ + gây AoE damage, KHÁC hoàn
        // toàn luồng VFXGun (phát tại chỗ ngay tại posVFX) dùng cho Ranged/Melee/Thrown.
        if (weaponData.Category == WeaponCategory.Boom)
        {
            SpawnBoomVFX(vfxObj);
            return;
        }

        var vfxGun = vfxObj.GetComponent<VFXGun>();
        if (vfxGun != null)
            vfxGun.RunVFX();
    }

    /// <summary>
    /// Truyền dữ liệu (sprite weapon hiện tại, Damage của Duck này, tag phe của Duck này) vào
    /// BoomVFX vừa Spawn rồi gọi Launch() — VFX tự bay vòng cung từ vị trí Duck này (attacker) tới
    /// vị trí currentTarget (target enemy tại thời điểm ném) rồi tự nổ + gây damage khi tới nơi
    /// (xem BoomVFX.cs). Fallback vị trí posVFX nếu thiếu currentTarget (không nên xảy ra vì
    /// SpawnWeaponVFX() chỉ được gọi từ PlayAttackAnimation(), luôn có currentTarget hợp lệ tại
    /// thời điểm UpdateAttack() ra đòn).
    /// </summary>
    private void SpawnBoomVFX(GameObject vfxObj)
    {
        var boomVFX = vfxObj.GetComponent<BoomVFX>();
        if (boomVFX == null) return;

        Vector3 startPos  = transform.position;
        Vector3 targetPos = currentTarget != null ? currentTarget.position : posVFX.position;
        Sprite  sprite    = weaponRenderer != null ? weaponRenderer.sprite : null;

        boomVFX.Launch(startPos, targetPos, sprite, Damage, gameObject.tag);
    }

    /// <summary>
    /// Gây sát thương (bằng Damage hiện tại) lên 1 Duck khác. Nếu đòn này khiến target CHUYỂN từ
    /// còn sống -> chết, gọi OnKilledTarget(target) đúng 1 lần (xem class doc — dùng để cộng XP
    /// cho weaponData của Duck này khi hạ được EnemyDuck).
    /// </summary>
    public virtual void DealDamage(Duck target)
    {
        if (target == null || IsDead) return;

        bool wasAliveBeforeHit = !target.IsDead;

        target.TakeDamage(Damage);

        if (wasAliveBeforeHit && target.IsDead)
            OnKilledTarget(target);
    }

    /// <summary>Gây sát thương lên MyTeam (dùng khi EnemyDuck đánh tới căn cứ Player).</summary>
    public virtual void DealDamage(MyTeam target)
    {
        if (target == null || IsDead) return;
        target.TakeDamage(Damage);
    }

    /// <summary>
    /// Được gọi đúng 1 lần ngay khi đòn đánh của Duck này (DealDamage(Duck)) khiến target chết.
    /// Mặc định: nếu target là EnemyDuck, cộng +1 XP vào weaponData của Duck này qua
    /// weaponData.AddXP(1) — tracking "số EnemyDuck đã bị tiêu diệt bởi weapon này" (XP tại
    /// WeaponEntry, xem WeaponData.cs). Không làm gì nếu thiếu weaponData hoặc target không phải
    /// EnemyDuck (VD: EnemyDuck hạ UnitDuck — không cộng XP).
    /// </summary>
    protected virtual void OnKilledTarget(Duck target)
    {
        if (weaponData != null && target is EnemyDuck)
            weaponData.AddXP(1);
    }

    /// <summary>Kiểm tra chết — Hp <= 0 thì phát VFX chết rồi Despawn (chỉ chạy đúng 1 lần nhờ cờ IsDead).</summary>
    protected virtual void CheckDead()
    {
        if (Hp > 0f || IsDead) return;

        IsDead = true;
        Hp = 0f;
        currentTarget = null;
        isAttacking = false;

        if (duckAnimation != null)
            duckAnimation.enabled = false; // tắt Move Loop, đồng thời tự dọn Tween nháy damage (OnDisable)

        PlayDeadVFX();
    }

    /// <summary>
    /// Gọi đúng 1 lần từ CheckDead() khi Duck vừa chết. Ẩn hình ảnh gốc của Duck (duckRenderer,
    /// weaponRenderer) và HPBar để nhường chỗ hiển thị cho hiệu ứng chết, rồi BẬT vfxDead lên (nếu
    /// có) để nó tự chạy animation Dead + mờ dần (xem VFXDead.cs) — lắng nghe VFXDead.OnFinished
    /// để biết đúng lúc gọi Despawn() thật sự, KHÔNG Despawn() ngay lập tức (PoolingManager sẽ
    /// SetActive(false) GameObject cha, làm tắt luôn VFXDead đang chạy dở vì nó là child).
    /// Nếu KHÔNG có vfxDead (chưa gán) -> fallback Despawn() ngay như hành vi cũ.
    /// </summary>
    protected virtual void PlayDeadVFX()
    {
        if (duckRenderer != null) duckRenderer.enabled = false;
        if (weaponRenderer != null) weaponRenderer.enabled = false;
        if (hpBar != null) hpBar.gameObject.SetActive(false);

        var vfx = GetVFXDeadComponent();
        if (vfx != null)
        {
            vfx.OnFinished += HandleDeadVFXFinished;
            vfxDead.SetActive(true);
            return;
        }

        // Không có VFXDead/component hợp lệ -> giữ hành vi cũ, Despawn ngay lập tức.
        Despawn();
    }

    /// <summary>Callback từ VFXDead.OnFinished (đã mờ xong + tự SetActive(false)) — gỡ đăng ký rồi Despawn Duck thật sự.</summary>
    private void HandleDeadVFXFinished()
    {
        var vfx = GetVFXDeadComponent();
        if (vfx != null)
            vfx.OnFinished -= HandleDeadVFXFinished;

        Despawn();
    }

    /// <summary>Lấy (cache) component VFXDead trên vfxDead — null nếu thiếu vfxDead hoặc chưa gắn component.</summary>
    private VFXDead GetVFXDeadComponent()
    {
        if (_vfxDeadCompResolved) return _vfxDeadComp;

        _vfxDeadComp = vfxDead != null ? vfxDead.GetComponent<VFXDead>() : null;
        _vfxDeadCompResolved = true;
        return _vfxDeadComp;
    }

    /// <summary>Despawn Duck (trả về Pool nếu prefab được quản lý bởi PoolingManager, fallback Destroy).</summary>
    protected virtual void Despawn()
    {
        PoolingManager.Despawn(gameObject);
    }

    /// <summary>
    /// Vòng lặp tấn công mục tiêu hiện tại (currentTarget) theo chu kỳ weaponData.TimeAttack (giây/đòn).
    /// Subclass gọi hàm này trong Update() khi IsTargetInAttackRange(currentTarget) == true (xem class doc).
    /// LƯU Ý: dùng TimeAttack (tốc độ tấn công), KHÔNG dùng TimeDelay (chu kỳ spawn trên Grid).
    /// </summary>
    protected virtual void UpdateAttack()
    {
        if (currentTarget == null) { isAttacking = false; return; }

        isAttacking = true;

        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f) return;

        _attackTimer = weaponData != null && weaponData.TimeAttack > 0f ? weaponData.TimeAttack : 1f;

        PlayAttackAnimation();

        if (weaponData.Category == WeaponCategory.Boom) return;

        var targetDuck = currentTarget.GetComponent<Duck>();
        if (targetDuck != null) { DealDamage(targetDuck); return; }

        var targetTeam = currentTarget.GetComponent<MyTeam>();
        if (targetTeam != null) DealDamage(targetTeam);
    }

    // ─── Movement ───────────────────────────────────────────

    /// <summary>
    /// Di chuyển tự động về hướng targetPos với tốc độ speed (đơn vị/giây).
    /// Trục Y LUÔN bị giới hạn trong khoảng [0, 2] bất kể targetPos.y là bao nhiêu.
    /// Không di chuyển nếu đang tấn công (isAttacking) hoặc đã chết.
    /// </summary>
    protected virtual void AutoMoveTowards(Vector3 targetPos, float speed)
    {
        if (IsDead || isAttacking || speed <= 0f) return;

        Vector3 pos = transform.position;
        Vector3 dir = targetPos - pos;
        dir.z = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            pos += dir * speed * Time.deltaTime;
        }

        pos.y = Mathf.Clamp(pos.y, MinLaneY, MaxLaneY);
        transform.position = pos;
    }

    // ─── Targeting (Raycast) ────────────────────────────────

    /// <summary>
    /// Quét về phía trước theo direction (đơn vị thế giới, VD Vector2.right/Vector2.left) bằng
    /// Physics2D.BoxCastAll — dùng Box (không phải Line) để không bỏ sót mục tiêu lệch trục Y
    /// trong cùng làn đánh. Trả về toàn bộ hit đã sắp xếp gần -> xa; subclass tự lọc theo tag
    /// (CompareTag) và tự loại trừ hit trùng chính mình (hit.collider.transform == transform).
    /// </summary>
    protected RaycastHit2D[] ScanForward(Vector2 direction)
    {
        Vector2 origin = transform.position;
        Vector2 boxSize = new Vector2(0.15f, scanHeight);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxSize, 0f, direction, scanDistance);

        if (hits.Length > 1)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        return hits;
    }

    /// <summary>Khoảng cách (bỏ qua trục Z) từ Duck này tới 1 Transform khác.</summary>
    protected float DistanceTo(Transform t)
    {
        Vector3 a = transform.position;
        Vector3 b = t.position;
        a.z = 0f;
        b.z = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>
    /// Kiểm tra target đã ĐÚNG TẦM ĐÁNH CHƯA — dựa trên va chạm VẬT LÝ thực tế giữa
    /// weaponRangeCollider (CircleCollider2D con \"Weapon/Direction\", bán kính = AttackRange) của
    /// Duck này và Collider2D gắn trên root của target, qua Physics2D.IsTouching(). Đây là cách
    /// chính xác hơn so-khoảng-cách-tâm-tâm cũ vì tính luôn hình dạng/kích thước thật của cả 2
    /// collider (VD: BoxCollider2D của target rộng ra sẽ khiến 2 bên "chạm" nhau sớm hơn so với
    /// so khoảng cách 2 điểm gốc).
    ///
    /// FALLBACK: nếu thiếu weaponRangeCollider (chưa gán trong Inspector) hoặc target không có
    /// Collider2D nào (VD: target là MyTeam — hiện MyTeam không gắn Collider2D), tự động fallback
    /// về so khoảng cách tâm-tâm cũ (DistanceTo(target) <= AttackRange) để không phá vỡ hành vi
    /// tấn công trong các trường hợp thiếu dữ liệu.
    /// </summary>
    protected bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;

        Collider2D targetCollider = target.GetComponent<Collider2D>();

        if (weaponRangeCollider != null && targetCollider != null)
            return Physics2D.IsTouching(weaponRangeCollider, targetCollider);

        // Thiếu 1 trong 2 collider cần thiết -> fallback so khoảng cách tâm-tâm như cơ chế cũ.
        return DistanceTo(target) <= AttackRange;
    }
}
