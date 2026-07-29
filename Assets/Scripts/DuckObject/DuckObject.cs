using UnityEngine;

/// <summary>
/// GameObject runtime đại diện cho 1 con vịt của người chơi (Player-owned)
/// trong scene. Sử dụng dữ liệu từ MyDuckData.
/// </summary>
public class DuckObject : BaseObject
{
    [Header("=== My Duck Data ===")]
    public MyDuckData MyData;

    // ─── Public API ───────────────────────────────────────────

    public void Init(MyDuckData data)
    {
        MyData = data;
        base.Init(data);
    }

    /// <summary>Nâng cấp level con vịt (nếu đủ XP/Coin), trả về true nếu thành công.</summary>
    public bool TryLevelUp()
    {
        if (MyData == null) return false;
        if (MyData.Level >= 5) return false;
        if (MyData.XP < MyData.XPToNextLevel) return false;

        MyData.Level++;
        MyData.XP -= MyData.XPToNextLevel;

        return true;
    }

    protected override void Die()
    {
        base.Die();
        // TODO: xử lý khi vịt của người chơi bị hạ (respawn, mất trận, ...)
    }
}
