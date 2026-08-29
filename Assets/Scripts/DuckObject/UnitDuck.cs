using UnityEngine;

/// <summary>
/// Vịt của Player (đã đặt lên Battle Grid). Chủ động tự động di chuyển về phía EnemyDuck
/// gần nhất khi chưa có mục tiêu trong tầm đánh (giống cơ chế AutoMoveTowards của EnemyDuck) —
/// khi 1 EnemyDuck (tag "Enemy") lọt vào tầm đánh (weaponRangeCollider) thì tự động dừng
/// và tấn công theo chu kỳ weaponData.TimeAttack cho tới khi mục tiêu rời tầm đánh hoặc chết.
/// GameObject cần được gắn tag "Player" để EnemyDuck nhận diện và ưu tiên tấn công.
/// </summary>
public class UnitDuck : Duck
{
    [Header("=== UnitDuck - Movement ===")]
    [Tooltip("Tốc độ di chuyển (đơn vị/giây) khi chưa có mục tiêu trong tầm đánh")]
    [SerializeField] private float moveSpeed = 1f;

    // Mục tiêu ưu tiên: 1 EnemyDuck (tag "Enemy") vừa phát hiện được qua trigger, LUÔN được
    // ưu tiên hơn việc tìm Enemy gần nhất, cho tới khi EnemyDuck đó chết/biến mất khỏi tầm đánh.
    private Transform _priorityEnemyTarget;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);
        _priorityEnemyTarget = null;
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        if (currentTarget != null)
        {
            UpdateAttack();
            return;
        }

        // Chưa có mục tiêu trong tầm đánh -> tiếp tục di chuyển về mục tiêu ưu tiên
        // (EnemyDuck vừa phát hiện, nếu có) hoặc EnemyDuck gần nhất còn sống trong scene.
        Transform moveTarget = _priorityEnemyTarget != null ? _priorityEnemyTarget : FindNearestEnemy();
        if (moveTarget != null)
            AutoMoveTowards(moveTarget.position, moveSpeed);
    }

    /// <summary>Tìm EnemyDuck còn sống gần vị trí hiện tại nhất trong scene.</summary>
    private Transform FindNearestEnemy()
    {
        var enemies = FindObjectsOfType<EnemyDuck>();
        Transform nearest = null;
        float minSqrDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float sqrDist = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    protected override void HandleTriggerEnter(Collider2D other)
    {
        if (IsDead) return;
        if (!other.CompareTag("Enemy")) return;

        // EnemyDuck xuất hiện trong tầm đánh -> ưu tiên di chuyển/tấn công EnemyDuck này.
        _priorityEnemyTarget = other.transform;

        // Chưa có mục tiêu -> khoá mục tiêu đầu tiên lọt vào tầm đánh.
        if (currentTarget == null)
            currentTarget = other.transform;
    }

    protected override void HandleTriggerExit(Collider2D other)
    {
        if (currentTarget != null && other.transform == currentTarget)
        {
            currentTarget = null;
            isAttacking   = false;
        }

        if (other.CompareTag("Enemy") && other.transform == _priorityEnemyTarget)
            _priorityEnemyTarget = null;
    }
}
