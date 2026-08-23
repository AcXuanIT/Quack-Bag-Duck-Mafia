using UnityEngine;

/// <summary>
/// Gắn vào child "Weapon/Direction" — nơi đặt CircleCollider2D (Is Trigger) đại diện
/// tầm đánh của weapon. Forward sự kiện OnTriggerEnter2D/OnTriggerExit2D lên component
/// Duck ở GameObject cha (Root), vì Unity CHỈ gọi các hàm Trigger trên đúng GameObject
/// sở hữu Collider2D — không tự "bubble" sự kiện lên script ở GameObject cha.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class DuckWeaponRange : MonoBehaviour
{
    private Duck _owner;

    private void Awake()
    {
        _owner = GetComponentInParent<Duck>();
        if (_owner == null)
            Debug.LogWarning("[DuckWeaponRange] Khong tim thay Duck o GameObject cha!", this);

        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => _owner?.OnWeaponRangeEnter(other);

    private void OnTriggerExit2D(Collider2D other) => _owner?.OnWeaponRangeExit(other);
}
