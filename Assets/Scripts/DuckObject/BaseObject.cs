using UnityEngine;

/// <summary>
/// Lớp cơ sở cho GameObject vịt trong scene (đại diện runtime,
/// khác với BaseDuckData chỉ là dữ liệu thuần).
/// EnemyObject và DuckObject sẽ kế thừa từ lớp này.
/// </summary>
public class BaseObject : MonoBehaviour
{
    [Header("=== Data Reference ===")]
    [Tooltip("Dữ liệu gốc (ScriptableObject/Serializable data) của con vịt này")]
    public BaseDuckData Data;

    [Header("=== Runtime Stats ===")]
    public float CurrentHP;
    public float CurrentDamage;

    [Header("=== Visual ===")]
    public SpriteRenderer SpriteRenderer;

    // ─── Public API ───────────────────────────────────────────

    /// <summary>Khởi tạo object từ data gốc.</summary>
    public virtual void Init(BaseDuckData data)
    {
        Data = data;
        if (data == null) return;

        CurrentHP = data.BaseHP;
        CurrentDamage = data.BaseDamage;

        if (SpriteRenderer != null)
            SpriteRenderer.sprite = data.GetSprite(data.Tier);
    }

    /// <summary>Nhận sát thương, trả về true nếu chết.</summary>
    public virtual bool TakeDamage(float amount)
    {
        CurrentHP -= amount;
        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            Die();
            return true;
        }
        return false;
    }

    /// <summary>Xử lý khi object chết.</summary>
    protected virtual void Die()
    {
        // Override ở lớp con để xử lý riêng (drop reward, animation, ...)
    }
}
