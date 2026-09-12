using UnityEngine;

/// <summary>
/// Vịt của Player (đã đặt lên Battle Grid). Mỗi frame QUÉT LIÊN TỤC về phía trước (hướng +X, phía
/// EnemyDuck) bằng Duck.ScanForward() (BoxCast) để tìm EnemyDuck còn sống gần nhất — không chỉ
/// quét khi chưa có currentTarget, mà quét lại MỖI FRAME kể cả khi đang có target hợp lệ: nếu có
/// EnemyDuck khác gần hơn EnemyDuck đang target ÍT NHẤT targetSwitchThreshold (mặc định 2f) xuất
/// hiện trong tầm quét -> đổi ngay sang con gần hơn đó. Ngưỡng này (hysteresis) để tránh target bị
/// "giật" qua lại liên tục mỗi frame khi 2 EnemyDuck có khoảng cách xấp xỉ nhau.
///
/// Quét thấy EnemyDuck -> khoá làm currentTarget, di chuyển (AutoMoveTowards) lại gần cho tới khi
/// ĐÚNG TẦM ĐÁNH (va chạm vật lý weaponRangeCollider của mình <-> Collider2D của target, xem
/// Duck.IsTargetInAttackRange) thì dừng lại và tự động tấn công (UpdateAttack, chu kỳ
/// weaponData.TimeAttack). Không quét thấy Enemy nào (currentTarget == null) -> đứng yên chờ,
/// KHÔNG còn tự tìm Enemy gần nhất trong toàn scene như cơ chế cũ.
///
/// GameObject cần được gắn tag "Player" để EnemyDuck nhận diện và ưu tiên tấn công.
/// </summary>
public class UnitDuck : Duck
{
    [Header("=== UnitDuck - Movement ===")]
    [Tooltip("Tốc độ di chuyển (đơn vị/giây) khi mục tiêu còn ngoài tầm đánh")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("=== UnitDuck - Target Switching ===")]
    [Tooltip("Ngưỡng chênh lệch khoảng cách (đơn vị) tối thiểu để đổi sang EnemyDuck khác gần hơn " +
             "EnemyDuck đang target — tránh target bị giật qua lại khi 2 mục tiêu ở khoảng cách " +
             "xấp xỉ nhau.")]
    [SerializeField] private float targetSwitchThreshold = 2f;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);
    }

    protected virtual void Update()
    {
        if (IsDead || IsBattleStopped()) return;

        // Mục tiêu hiện tại (nếu có) đã chết/bị huỷ -> nhả ra ngay trong frame này.
        if (currentTarget != null)
        {
            var enemy = currentTarget.GetComponent<EnemyDuck>();
            if (enemy == null || enemy.IsDead)
            {
                currentTarget = null;
                isAttacking = false;
            }
        }

        // QUÉT LIÊN TỤC mỗi frame (không chỉ khi currentTarget == null) để bắt EnemyDuck mới xuất
        // hiện hoặc EnemyDuck gần hơn EnemyDuck đang target.
        Transform nearestEnemy = ScanForEnemy();

        if (nearestEnemy != null)
        {
            if (currentTarget == null)
            {
                // Chưa có target -> nhận ngay EnemyDuck gần nhất tìm được.
                currentTarget = nearestEnemy;
            }
            else if (currentTarget != nearestEnemy
                     && DistanceTo(currentTarget) - DistanceTo(nearestEnemy) > targetSwitchThreshold)
            {
                // Có EnemyDuck khác gần hơn EnemyDuck đang target ÍT NHẤT threshold -> đổi target.
                currentTarget = nearestEnemy;
            }
        }

        // Không quét thấy Enemy nào phía trước -> đứng yên chờ (không tự đi tìm khắp scene nữa).
        if (currentTarget == null)
        {
            isAttacking = false;
            return;
        }

        // Đã ĐÚNG tầm đánh (va chạm vật lý weaponRangeCollider <-> Collider2D của target, xem
        // Duck.IsTargetInAttackRange) -> dừng di chuyển, chuyển sang tấn công.
        if (IsTargetInAttackRange(currentTarget))
        {
            UpdateAttack();
            return;
        }

        // Còn chưa đúng tầm đánh -> tiếp tục di chuyển lại gần mục tiêu.
        isAttacking = false;
        AutoMoveTowards(currentTarget.position, moveSpeed);
    }

    /// <summary>
    /// Quét về phía trước (Vector2.right, hướng EnemyDuck) bằng Duck.ScanForward(), lọc lấy
    /// EnemyDuck còn sống gần nhất (hits đã được ScanForward sắp xếp gần -> xa nên hit đầu tiên
    /// hợp lệ chính là gần nhất). Được gọi lại MỖI FRAME trong Update() để hỗ trợ đổi target liên tục.
    /// </summary>
    private Transform ScanForEnemy()
    {
        var hits = ScanForward(Vector2.right);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue; // bỏ qua chính mình
            if (!hit.collider.CompareTag("Enemy")) continue;

            var enemy = hit.collider.GetComponentInParent<EnemyDuck>();
            if (enemy != null && !enemy.IsDead)
                return enemy.transform;
        }

        return null;
    }
}
