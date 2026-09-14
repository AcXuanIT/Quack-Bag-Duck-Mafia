using UnityEngine;
using DG.Tweening;


[RequireComponent(typeof(BoxCollider2D))]
public abstract class Duck : MonoBehaviour
{
    [Header("Visual ")]
    [SerializeField] protected SpriteRenderer duckRenderer;
    [SerializeField] protected SpriteRenderer weaponRenderer;

    [SerializeField] protected Animator weaponAnimator;

    [SerializeField] protected GameObject vfxDead;

    [Header("Combat")]
    [SerializeField] protected CircleCollider2D weaponRangeCollider;
    [SerializeField] protected Transform posVFX;

    [Header(" Targeting Raycast")]
    [SerializeField] protected float scanDistance = 30f;

    [SerializeField] protected float scanHeight = 2.2f;

    [Header("Refs")]
    [SerializeField] protected DuckMoveAnimation duckAnimation;
    [SerializeField] protected DuckHPBar hpBar;

    [Header("Weapon Rest Pose")]

    [SerializeField] protected Vector3 weaponRestLocalPosition;

    // ─── Data ──
    protected BaseDuckData duckData;
    protected WeaponEntry  weaponData;
    protected int duckTier;
    protected int weaponTier;

    // ─── Runtime Stats ───
    public float MaxHp   { get; protected set; }
    public float Hp      { get; protected set; }
    public float Damage  { get; protected set; }
    public bool  IsDead  { get; protected set; }

    public float AttackRange => weaponData != null ? weaponData.GetAttackRange() : 0f;

    // ─── Combat Runtime ───
    protected Transform currentTarget;
    protected bool      isAttacking;
    private   float     _attackTimer;

    private VFXDead _vfxDeadComp;
    private bool    _vfxDeadCompResolved;

    private const string AttackRangedTrigger = "AttackRanged";
    private const string AttackMeleeTrigger  = "AttackMelee";
    private const string AttackThrowTrigger  = "AttackThrow";
    private const string AttackBoomTrigger   = "AttackBoom";

    protected const float MinLaneY = 0f;
    protected const float MaxLaneY = 2f;

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

            weaponRestLocalPosition = weaponTf.localPosition;
        }
    }

    // ─── Init ─
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

        if (duckRenderer != null) duckRenderer.enabled = true;
        if (weaponRenderer != null) weaponRenderer.enabled = true;
        if (hpBar != null) hpBar.gameObject.SetActive(true);
        if (vfxDead != null) vfxDead.SetActive(false);

        if (duckRenderer != null && duck != null)
            duckRenderer.sprite = duck.GetSprite(duckTierIn);

        if (weaponRenderer != null && weapon != null)
        {
            weaponRenderer.sprite = weapon.GetSpriteByTier(weaponTierIn);
            RecalculatePosVFX();
        }

        if (weaponRangeCollider != null && weapon != null)
        {
            weaponRangeCollider.isTrigger = true;
            weaponRangeCollider.radius    = weapon.GetAttackRange();
        }

        if (hpBar != null)
            hpBar.Heal(1f); 

        if (duckAnimation != null)
            duckAnimation.enabled = true; 

        if (weaponAnimator != null)
        {
            var weaponGO = weaponAnimator.gameObject;
            weaponGO.SetActive(false);
            weaponGO.transform.localPosition = weaponRestLocalPosition;
            weaponGO.transform.localRotation = Quaternion.identity;
            weaponGO.SetActive(true);
            weaponAnimator.Rebind(); 
        }
    }

    protected virtual void RecalculatePosVFX()
    {
        if (posVFX == null || weaponRenderer == null || weaponRenderer.sprite == null) return;

        Bounds bounds = weaponRenderer.sprite.bounds;

        float topRightX = weaponRenderer.flipX ? bounds.min.x : bounds.max.x;
        float topRightY = weaponRenderer.flipY ? bounds.min.y : bounds.max.y;

        posVFX.localPosition = new Vector3(topRightX, topRightY - 0.13f, posVFX.localPosition.z);
    }

    // ─── Combat ──
    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Hp -= amount;

        if (hpBar != null)
            hpBar.SetHP(MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f);

        PlayDamageFlash();

        CheckDead();
    }

    protected virtual void PlayDamageFlash()
    {
        if (duckAnimation != null)
            duckAnimation.PlayDamageFlash();
    }

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

    protected virtual void SpawnWeaponVFX()
    {
        if (weaponData == null || weaponData.vfxWeapon == null || posVFX == null) return;

        GameObject vfxObj = PoolingManager.Spawn(weaponData.vfxWeapon, posVFX.position, posVFX.rotation);
        if (vfxObj == null) return;

        if (weaponData.Category == WeaponCategory.Boom)
        {
            SpawnBoomVFX(vfxObj);
            return;
        }

        var vfxGun = vfxObj.GetComponent<VFXGun>();
        if (vfxGun != null)
            vfxGun.RunVFX();
    }
    private void SpawnBoomVFX(GameObject vfxObj)
    {
        var boomVFX = vfxObj.GetComponent<BoomVFX>();
        if (boomVFX == null) return;

        Vector3 startPos  = transform.position;
        Vector3 targetPos = currentTarget != null ? currentTarget.position : posVFX.position;
        Sprite  sprite    = weaponRenderer != null ? weaponRenderer.sprite : null;

        boomVFX.Launch(startPos, targetPos, sprite, Damage, gameObject.tag);
    }
    public virtual void DealDamage(Duck target)
    {
        if (target == null || IsDead) return;

        bool wasAliveBeforeHit = !target.IsDead;

        target.TakeDamage(Damage);

        if (wasAliveBeforeHit && target.IsDead)
            OnKilledTarget(target);
    }

    public virtual void DealDamage(MyTeam target)
    {
        if (target == null || IsDead) return;
        target.TakeDamage(Damage);
    }

    protected virtual void OnKilledTarget(Duck target)
    {
        if (weaponData != null && target is EnemyDuck)
            weaponData.AddXP(1);
    }

    protected virtual void CheckDead()
    {
        if (Hp > 0f || IsDead) return;

        IsDead = true;
        Hp = 0f;
        currentTarget = null;
        isAttacking = false;

        if (duckAnimation != null)
            duckAnimation.enabled = false; 

        PlayDeadVFX();
    }

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


        Despawn();
    }

    private void HandleDeadVFXFinished()
    {
        var vfx = GetVFXDeadComponent();
        if (vfx != null)
            vfx.OnFinished -= HandleDeadVFXFinished;

        Despawn();
    }
    private VFXDead GetVFXDeadComponent()
    {
        if (_vfxDeadCompResolved) return _vfxDeadComp;

        _vfxDeadComp = vfxDead != null ? vfxDead.GetComponent<VFXDead>() : null;
        _vfxDeadCompResolved = true;
        return _vfxDeadComp;
    }

    protected virtual void Despawn()
    {
        PoolingManager.Despawn(gameObject);
    }

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

    // ─── Movement ─
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

    // ─── Targeting (Raycast) ─
    protected RaycastHit2D[] ScanForward(Vector2 direction)
    {
        Vector2 origin = transform.position;
        Vector2 boxSize = new Vector2(0.15f, scanHeight);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxSize, 0f, direction, scanDistance);

        if (hits.Length > 1)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        return hits;
    }
    protected float DistanceTo(Transform t)
    {
        Vector3 a = transform.position;
        Vector3 b = t.position;
        a.z = 0f;
        b.z = 0f;
        return Vector3.Distance(a, b);
    }
    protected bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;

        Collider2D targetCollider = target.GetComponent<Collider2D>();

        if (weaponRangeCollider != null && targetCollider != null)
            return Physics2D.IsTouching(weaponRangeCollider, targetCollider);

        return DistanceTo(target) <= AttackRange;
    }
}
