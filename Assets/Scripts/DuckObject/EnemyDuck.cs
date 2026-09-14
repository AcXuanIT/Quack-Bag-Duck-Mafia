using UnityEngine;


public class EnemyDuck : Duck
{
    [Header("EnemyDuck - Movement")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("EnemyDuck")]
    [SerializeField] private float targetSwitchThreshold = 2f;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);
    }

    private void Update()
    {
        if (IsDead || IsBattleStopped()) return;

        if (currentTarget != null)
        {
            var unit = currentTarget.GetComponent<UnitDuck>();
            var team = currentTarget.GetComponent<MyTeam>();
            bool stillValid = (unit != null && !unit.IsDead) || (team != null && !team.IsDead);
            if (!stillValid)
            {
                currentTarget = null;
                isAttacking = false;
            }
        }

        FindNearestTargets(out Transform nearestUnit, out Transform nearestTeam);

        bool currentIsUnit = currentTarget != null && currentTarget.GetComponent<UnitDuck>() != null;

        if (nearestUnit != null)
        {
            if (!currentIsUnit)
            {
                currentTarget = nearestUnit;
            }
            else if (currentTarget != nearestUnit
                     && DistanceTo(currentTarget) - DistanceTo(nearestUnit) > targetSwitchThreshold)
            {
                currentTarget = nearestUnit;
            }
        }
        else if (currentTarget == null)
        {
            currentTarget = nearestTeam;
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

    private void FindNearestTargets(out Transform nearestUnit, out Transform nearestTeam)
    {
        nearestUnit = null;
        nearestTeam = null;

        var hits = ScanForward(Vector2.left);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue; 

            if (nearestUnit == null && hit.collider.CompareTag("Player"))
            {
                var unit = hit.collider.GetComponentInParent<UnitDuck>();
                if (unit != null && !unit.IsDead)
                    nearestUnit = unit.transform;
            }
            else if (nearestTeam == null && hit.collider.CompareTag("MyTeam"))
            {
                var team = hit.collider.GetComponentInParent<MyTeam>();
                if (team != null && !team.IsDead)
                    nearestTeam = team.transform;
            }

            if (nearestUnit != null && nearestTeam != null) break; 
        }
    }
}
