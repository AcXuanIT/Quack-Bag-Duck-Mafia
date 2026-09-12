using UnityEngine;

/// <summary>
/// [DEPRECATED — chỉ còn mục đích hiển thị] Gắn vào child "Weapon/Direction", nơi đặt
/// CircleCollider2D (Is Trigger) đại diện tầm đánh của weapon.
///
/// Trước đây component này forward sự kiện OnTriggerEnter2D/Exit2D lên Duck cha để làm cơ chế
/// target. Từ khi Duck đổi sang cơ chế target bằng Raycast/BoxCast quét về phía trước
/// (xem Duck.ScanForward(), UnitDuck/EnemyDuck.Update()), component này KHÔNG còn ảnh hưởng
/// gameplay — chỉ giữ lại để Collider2D vẫn hiển thị đúng bán kính tầm đánh (radius được
/// Duck.Init() cập nhật theo weaponData.GetAttackRange()) làm tham khảo trực quan trong Editor.
/// Có thể xoá GameObject "Weapon/Direction" khỏi prefab nếu muốn dọn dẹp hoàn toàn.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class DuckWeaponRange : MonoBehaviour
{
    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;
    }
}
