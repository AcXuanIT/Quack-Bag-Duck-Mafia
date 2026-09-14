using UnityEngine;

public class UnitDuck : Duck
{
    [Header("=== UnitDuck - Movement ===")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("=== UnitDuck - Target Switching ===")]
    [SerializeField] private float targetSwitchThreshold = 2f;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);
    }

    protected virtual void Update()
    {
        if (IsDead || IsBattleStopped()) return;

        if (currentTarget != null)
        {
            var enemy = currentTarget.GetComponent<EnemyDuck>();
            if (enemy == null || enemy.IsDead)
            {
                currentTarget = null;
                isAttacking = false;
            }
        }

        Transform nearestEnemy = ScanForEnemy();

        if (nearestEnemy != null)
        {
            if (currentTarget == null)
            {
                currentTarget = nearestEnemy;
            }
            else if (currentTarget != nearestEnemy
                     && DistanceTo(currentTarget) - DistanceTo(nearestEnemy) > targetSwitchThreshold)
            {
                currentTarget = nearestEnemy;
            }
        }

        if (currentTarget == null)
        {
            isAttacking = false;
            return;
        }
        if (IsTargetInAttackRange(currentTarget))
        {
            UpdateAttack();
            return;
        }

        isAttacking = false;
        AutoMoveTowards(currentTarget.position, moveSpeed);
    }

    private Transform ScanForEnemy()
    {
        var hits = ScanForward(Vector2.right);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue;
            if (!hit.collider.CompareTag("Enemy")) continue;

            var enemy = hit.collider.GetComponentInParent<EnemyDuck>();
            if (enemy != null && !enemy.IsDead)
                return enemy.transform;
        }

        return null;
    }
}
