using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class DuckWeaponRange : MonoBehaviour
{
    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;
    }
}
