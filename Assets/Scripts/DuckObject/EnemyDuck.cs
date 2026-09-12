using UnityEngine;

/// <summary>
/// Vịt địch. Mỗi frame QUÉT LIÊN TỤC về phía trước (hướng -X, phía MyTeam) bằng Duck.ScanForward()
/// (BoxCast) để tìm mục tiêu — không chỉ quét khi chưa có currentTarget, mà quét lại MỖI FRAME kể
/// cả khi đang có target hợp lệ, để có thể đổi target ngay khi có lựa chọn tốt hơn:
///   - ƯU TIÊN UnitDuck (tag "Player") tuyệt đối so với MyTeam: nếu đang target MyTeam mà quét
///     thấy bất kỳ UnitDuck nào xuất hiện trong tầm quét -> chuyển currentTarget sang UnitDuck đó
///     ngay lập tức (dù MyTeam đang target có thể gần hơn).
///   - Nếu đang target 1 UnitDuck mà quét thấy 1 UnitDuck KHÁC gần hơn ÍT NHẤT
///     targetSwitchThreshold (mặc định 2f) so với UnitDuck đang target -> đổi sang UnitDuck gần
///     hơn đó. Ngưỡng này (hysteresis) để tránh target bị "giật" qua lại liên tục mỗi frame khi
///     2 UnitDuck có khoảng cách xấp xỉ nhau.
///   - Chỉ target MyTeam khi hoàn toàn không quét thấy UnitDuck nào phía trước.
///
/// Quét thấy mục tiêu -> khoá làm currentTarget, di chuyển (AutoMoveTowards) lại gần cho tới khi
/// ĐÚNG TẦM ĐÁNH (va chạm vật lý weaponRangeCollider của mình <-> Collider2D của target, xem
/// Duck.IsTargetInAttackRange) thì dừng lại và tự động tấn công (UpdateAttack, chu kỳ
/// weaponData.TimeAttack). Không quét thấy gì phía trước (currentTarget == null) -> đứng yên chờ,
/// KHÔNG còn mặc định hành quân thẳng về MyTeam như cơ chế cũ.
///
/// GameObject cần được gắn tag "Enemy" để UnitDuck nhận diện.
/// </summary>
public class EnemyDuck : Duck
{
    [Header("=== EnemyDuck - Movement ===")]
    [Tooltip("Tốc độ di chuyển (đơn vị/giây) khi mục tiêu còn ngoài tầm đánh")]
    [SerializeField] private float moveSpeed = 1f;

    [Header("=== EnemyDuck - Target Switching ===")]
    [Tooltip("Ngưỡng chênh lệch khoảng cách (đơn vị) tối thiểu để đổi sang UnitDuck khác gần hơn " +
             "UnitDuck đang target. Chỉ áp dụng khi đang target 1 UnitDuck và có UnitDuck khác gần " +
             "hơn xuất hiện — tránh target bị giật qua lại khi 2 mục tiêu ở khoảng cách xấp xỉ nhau.")]
    [SerializeField] private float targetSwitchThreshold = 2f;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);
    }

    private void Update()
    {
        if (IsDead || IsBattleStopped()) return;

        // Mục tiêu hiện tại (nếu có) đã chết/bị huỷ -> nhả ra ngay trong frame này.
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

        // QUÉT LIÊN TỤC mỗi frame (không chỉ khi currentTarget == null) để có thể bắt UnitDuck
        // mới xuất hiện hoặc UnitDuck gần hơn UnitDuck đang target.
        FindNearestTargets(out Transform nearestUnit, out Transform nearestTeam);

        bool currentIsUnit = currentTarget != null && currentTarget.GetComponent<UnitDuck>() != null;

        if (nearestUnit != null)
        {
            if (!currentIsUnit)
            {
                // Đang không target UnitDuck nào (null hoặc đang target MyTeam) mà có UnitDuck
                // xuất hiện trong tầm quét -> ưu tiên tuyệt đối, chuyển ngay sang UnitDuck.
                // (Không áp dụng ngưỡng threshold ở đây vì đây là đổi LOẠI mục tiêu ưu tiên hơn,
                // không phải so khoảng cách giữa 2 UnitDuck.)
                currentTarget = nearestUnit;
            }
            else if (currentTarget != nearestUnit
                     && DistanceTo(currentTarget) - DistanceTo(nearestUnit) > targetSwitchThreshold)
            {
                // Đang target 1 UnitDuck nhưng có UnitDuck khác gần hơn ÍT NHẤT threshold
                // -> đổi sang con gần hơn đó.
                currentTarget = nearestUnit;
            }
        }
        else if (currentTarget == null)
        {
            // Không quét thấy UnitDuck nào -> fallback MyTeam (chỉ khi chưa có target nào khác).
            currentTarget = nearestTeam;
        }
        // Lưu ý: nếu đang target MyTeam và vẫn không thấy UnitDuck nào (nearestUnit == null),
        // giữ nguyên MyTeam đang target (không so khoảng cách giữa các MyTeam với nhau).

        // Không quét thấy gì phía trước -> đứng yên chờ (không tự hành quân về MyTeam nữa).
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
    /// Quét về phía trước (Vector2.left, hướng MyTeam) bằng Duck.ScanForward() và trả về ĐỒNG THỜI
    /// UnitDuck gần nhất còn sống (nếu có) VÀ MyTeam gần nhất còn sống (nếu có) trong 1 lần quét
    /// (hits đã được ScanForward sắp xếp gần -> xa, nên hit đầu tiên khớp mỗi tag chính là gần nhất).
    /// Dùng để so sánh khoảng cách khi quyết định đổi target liên tục mỗi frame trong Update().
    /// </summary>
    private void FindNearestTargets(out Transform nearestUnit, out Transform nearestTeam)
    {
        nearestUnit = null;
        nearestTeam = null;

        var hits = ScanForward(Vector2.left);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue; // bỏ qua chính mình

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

            if (nearestUnit != null && nearestTeam != null) break; // đã có đủ cả 2, dừng sớm
        }
    }
}
