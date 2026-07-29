using UnityEngine;

/// <summary>
/// GameObject runtime đại diện cho 1 con vịt địch (Enemy) trong scene.
/// Sử dụng dữ liệu từ EnemyDuckData.
/// </summary>
public class EnemyObject : BaseObject
{
    [Header("=== Enemy Data ===")]
    public EnemyDuckData EnemyData;

    [Header("=== Enemy Runtime ===")]
    public float MoveSpeed;
    public float AttackSpeed;

    private float _attackTimer;

    // ─── Public API ───────────────────────────────────────────

    public void Init(EnemyDuckData data)
    {
        EnemyData = data;
        base.Init(data);

        if (data == null) return;

        CurrentDamage = data.AttackDamage;
        MoveSpeed = data.MoveSpeed;
        AttackSpeed = data.AttackSpeed;
        _attackTimer = 0f;
    }

    protected override void Die()
    {
        base.Die();
        // TODO: cộng RewardCoin / RewardExp cho người chơi khi enemy chết
    }
}
