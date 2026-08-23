using UnityEngine;

/// <summary>
/// Lớp cơ sở (base) cho mọi "con vịt" chiến đấu trong trận (khác với DuckObject/EnemyObject
/// cũ chỉ là bản nháp) — UnitDuck (vịt của Player) và EnemyDuck (vịt địch) đều kế thừa từ đây.
///
/// CẤU TRÚC PREFAB MÀ SCRIPT NÀY GIẢ ĐỊNH (đã khớp unit_001_tier_1 / enemy_001_tier_1):
///   Root (Duck script + SpriteRenderer "duck" + DuckMoveAnimation + BoxCollider2D)
///   ├── HPBar        → DuckHPBar
///   └── Weapon        → SpriteRenderer "weapon"
///        └── Direction → CircleCollider2D (Is Trigger = true, bán kính = tầm đánh weapon)
///                         + DuckWeaponRange (forward sự kiện Trigger lên Duck, vì
///                           OnTriggerEnter2D chỉ được Unity gọi trên đúng GameObject sở hữu
///                           Collider2D, không tự "bubble" lên script ở GameObject cha).
///
/// KHỞI TẠO: gọi Init(duckData, weaponData, duckTier, weaponTier) mỗi khi spawn (kể cả khi
/// lấy lại từ Pool) để reset toàn bộ chỉ số runtime (HP/Damage/sprite/tầm đánh) — không dựa
/// vào Awake()/giá trị cũ còn sót lại từ lần dùng trước của prefab trong Pool.
///
/// CHỈ SỐ:
///   Hp     = duckData.BaseHP + weaponData.GetCurrentHP()      (theo Level hiện tại của weapon)
///   Damage = weaponData.GetCurrentDamage()                     (theo Level hiện tại của weapon)
///   Bán kính CircleCollider2D (tầm đánh) = weaponData.GetAttackRange()
///
/// duckTier/weaponTier CHỈ dùng để chọn đúng sprite hiển thị (GetSprite(tier)/GetSpriteByTier(tier))
/// — KHÔNG ảnh hưởng Damage/HP (2 chỉ số này tính theo Level của WeaponEntry, không phải Tier).
///
/// DI CHUYỂN: AutoMoveTowards() di chuyển về 1 điểm đích, trục Y luôn bị giới hạn trong [0, 2]
/// (khớp phạm vi làn đánh của game) dù đích ở Y bao nhiêu.
///
/// CHIẾN ĐẤU: khi Weapon Range Collider (trigger) chạm 1 tag hợp lệ (subclass tự định nghĩa qua
/// HandleTriggerEnter/Exit — VD UnitDuck tìm tag ""Enemy"", EnemyDuck tìm tag ""Player""/""MyTeam""),
/// Duck dừng di chuyển và tự động tấn công theo chu kỳ weaponData.TimeDelay (giây/đòn).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public abstract class Duck : MonoBehaviour
{
    [Header("=== Visual ===")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh con vịt (thường ở Root)")]
    [SerializeField] protected SpriteRenderer duckRenderer;

    [Tooltip("SpriteRenderer hiển thị hình ảnh vũ khí (thường ở child \"Weapon\")")]
    [SerializeField] protected SpriteRenderer weaponRenderer;

    [Header("=== Combat ===")]
    [Tooltip("CircleCollider2D (Is Trigger) đại diện tầm đánh của weapon — thường ở child \"Weapon/Direction\"")]
    [SerializeField] protected CircleCollider2D weaponRangeCollider;

    [Header("=== Refs ===")]
    [Tooltip("Script hiệu ứng di chuyển (squash/stretch) — thường ở Root")]
    [SerializeField] protected DuckMoveAnimation duckAnimation;

    [Tooltip("Script thanh máu — thường ở child \"HPBar\"")]
    [SerializeField] protected DuckHPBar hpBar;

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

    // ─── Combat Runtime ─────────────────────────────────────
    protected Transform currentTarget;
    protected bool      isAttacking;
    private   float     _attackTimer;

    protected const float MinLaneY = 0f;
    protected const float MaxLaneY = 2f;

    protected virtual void Reset()
    {
        if (duckRenderer == null) duckRenderer = GetComponent<SpriteRenderer>();
        if (duckAnimation == null) duckAnimation = GetComponent<DuckMoveAnimation>();
        if (hpBar == null) hpBar = GetComponentInChildren<DuckHPBar>(true);

        var weaponTf = transform.Find("Weapon");
        if (weaponTf != null)
        {
            if (weaponRenderer == null) weaponRenderer = weaponTf.GetComponent<SpriteRenderer>();
            var directionTf = weaponTf.Find("Direction");
            if (directionTf != null && weaponRangeCollider == null)
                weaponRangeCollider = directionTf.GetComponent<CircleCollider2D>();
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

        if (duckRenderer != null && duck != null)
            duckRenderer.sprite = duck.GetSprite(duckTierIn);

        if (weaponRenderer != null && weapon != null)
            weaponRenderer.sprite = weapon.GetSpriteByTier(weaponTierIn);

        if (weaponRangeCollider != null && weapon != null)
        {
            weaponRangeCollider.isTrigger = true;
            weaponRangeCollider.radius    = weapon.GetAttackRange();
        }

        if (hpBar != null)
            hpBar.Heal(1f); // reset thanh máu về đầy lúc spawn

        if (duckAnimation != null)
            duckAnimation.enabled = true; // bật lại hiệu ứng di chuyển (có thể đã bị tắt lúc chết/tấn công trước đó)
    }

    // ─── Combat ─────────────────────────────────────────────

    /// <summary>Nhận sát thương. Cập nhật HP Bar và kiểm tra chết (CheckDead → Despawn nếu Hp &lt;= 0).</summary>
    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Hp -= amount;

        if (hpBar != null)
            hpBar.SetHP(MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f);

        CheckDead();
    }

    /// <summary>Gây sát thương (bằng Damage hiện tại) lên 1 Duck khác.</summary>
    public virtual void DealDamage(Duck target)
    {
        if (target == null || IsDead) return;
        target.TakeDamage(Damage);
    }

    /// <summary>Gây sát thương lên MyTeam (dùng khi EnemyDuck đánh tới căn cứ Player).</summary>
    public virtual void DealDamage(MyTeam target)
    {
        if (target == null || IsDead) return;
        target.TakeDamage(Damage);
    }

    /// <summary>Kiểm tra chết — Hp &lt;= 0 thì Despawn (chỉ chạy đúng 1 lần nhờ cờ IsDead).</summary>
    protected virtual void CheckDead()
    {
        if (Hp > 0f || IsDead) return;

        IsDead = true;
        Hp = 0f;
        currentTarget = null;
        isAttacking = false;

        if (duckAnimation != null)
            duckAnimation.enabled = false;

        Despawn();
    }

    /// <summary>Despawn Duck (trả về Pool nếu prefab được quản lý bởi PoolingManager, fallback Destroy).</summary>
    protected virtual void Despawn()
    {
        PoolingManager.Despawn(gameObject);
    }

    /// <summary>
    /// Vòng lặp tấn công mục tiêu hiện tại (currentTarget) theo chu kỳ weaponData.TimeDelay (giây/đòn).
    /// Subclass gọi hàm này trong Update() khi đã có currentTarget (do HandleTriggerEnter gán).
    /// </summary>
    protected virtual void UpdateAttack()
    {
        if (currentTarget == null) { isAttacking = false; return; }

        isAttacking = true;

        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f) return;

        _attackTimer = weaponData != null && weaponData.TimeDelay > 0f ? weaponData.TimeDelay : 1f;

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

    // ─── Trigger Forwarding ─────────────────────────────────
    // Được DuckWeaponRange (gắn trên child "Weapon/Direction") gọi tới, vì OnTriggerEnter2D chỉ
    // được Unity gọi trên chính GameObject sở hữu Collider2D — không tự bubble lên script ở cha.

    /// <summary>Gọi bởi DuckWeaponRange khi có Collider2D đi vào tầm đánh (weaponRangeCollider).</summary>
    public void OnWeaponRangeEnter(Collider2D other) => HandleTriggerEnter(other);

    /// <summary>Gọi bởi DuckWeaponRange khi có Collider2D rời khỏi tầm đánh.</summary>
    public void OnWeaponRangeExit(Collider2D other) => HandleTriggerExit(other);

    /// <summary>Subclass override để định nghĩa tag nào được coi là mục tiêu hợp lệ khi vào tầm đánh.</summary>
    protected abstract void HandleTriggerEnter(Collider2D other);

    /// <summary>Subclass override để xử lý khi mục tiêu hiện tại rời khỏi tầm đánh.</summary>
    protected abstract void HandleTriggerExit(Collider2D other);
}
