using UnityEngine;

/// <summary>
/// Vịt của Player (đã đặt lên Battle Grid). Đứng yên tại vị trí — không tự đi tìm địch;
/// khi 1 EnemyDuck (tag "Enemy") lọt vào tầm đánh (weaponRangeCollider) thì tự động dừng
/// và tấn công theo chu kỳ weaponData.TimeDelay cho tới khi mục tiêu rời tầm đánh hoặc chết.
/// GameObject cần được gắn tag "Player" để EnemyDuck nhận diện và ưu tiên tấn công.
/// </summary>
public class UnitDuck : Duck
{
    protected virtual void Update()
    {
        if (IsDead) return;
        UpdateAttack();
    }

    protected override void HandleTriggerEnter(Collider2D other)
    {
        if (IsDead) return;
        if (!other.CompareTag("Enemy")) return;

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
    }
}
