using System;
using UnityEngine;

/// <summary>
/// Script CHÍNH gắn trên GameObject "MyTeam" — quản lý HP của đội hình người chơi,
/// nhận Damage / hồi máu, và điều phối 2 component con:
///   - MyTeamHPBar      : hiển thị thanh HP + text HP
///   - MyTeamAnimation  : hiệu ứng "nảy" khi spawn (chạy 1 lần, không lặp)
///                        + hiệu ứng nháy trắng khi nhận damage (PlayDamageFlash)
/// </summary>
public class MyTeam : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private MyTeamHPBar     hpBar;
    [SerializeField] private MyTeamAnimation spawnAnimation;

    [Header("=== HP ===")]
    [SerializeField] private float baseHP = 100f;
    private float _currentHP;

    public float BaseHP    => baseHP;
    public float CurrentHP => _currentHP;
    public bool  IsDead    => _currentHP <= 0f;

    // ─── Events ─────────────────────────────────────────────
    /// <summary>Bắn ra mỗi khi HP thay đổi (currentHP, baseHP) — UI khác có thể lắng nghe.</summary>
    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;

    private void Awake()
    {
        InitHP(baseHP);
    }

    // ─── Public API ─────────────────────────────────────────

    /// <summary>
    /// Khởi tạo HP gốc cho team (gọi khi spawn hoặc reset trận đấu mới).
    /// Đồng bộ MyTeamHPBar về đầy máu và phát hiệu ứng spawn (MyTeamAnimation).
    /// </summary>
    public void InitHP(float newBaseHP)
    {
        baseHP     = Mathf.Max(0f, newBaseHP);
        _currentHP = baseHP;

        if (hpBar != null)
            hpBar.Init(baseHP);

        OnHPChanged?.Invoke(_currentHP, baseHP);

        if (spawnAnimation != null)
            spawnAnimation.PlaySpawnAnimation();
    }

    /// <summary>Nhận damage — trừ HP (không âm), nháy trắng (qua MyTeamAnimation), cập nhật HP Bar, bắn OnDeath nếu về 0.</summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        _currentHP = Mathf.Max(0f, _currentHP - amount);

        if (spawnAnimation != null)
            spawnAnimation.PlayDamageFlash();

        SyncHPBar();

        if (_currentHP <= 0f)
            OnDeath?.Invoke();
    }

    /// <summary>Hồi máu — cộng HP (không vượt quá baseHP), cập nhật HP Bar.</summary>
    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;

        _currentHP = Mathf.Min(baseHP, _currentHP + amount);
        SyncHPBar();
    }

    // ─── Internal ───────────────────────────────────────────

    private void SyncHPBar()
    {
        if (hpBar != null)
            hpBar.UpdateHP(_currentHP);

        OnHPChanged?.Invoke(_currentHP, baseHP);
    }
}
