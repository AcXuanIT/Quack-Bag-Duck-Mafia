using System;
using UnityEngine;

public class MyTeam : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private MyTeamHPBar     hpBar;
    [SerializeField] private MyTeamAnimation spawnAnimation;

    public MyTeamAnimation mytemAnimation => spawnAnimation;

    [Header("=== HP ===")]
    [SerializeField] private float baseHP = 100f;
    private float _currentHP;

    public float BaseHP    => baseHP;
    public float CurrentHP => _currentHP;
    public bool  IsDead    => _currentHP <= 0f;

    // ─── Events ─
    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;

    private void Awake()
    {
        InitHP(baseHP);
    }
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

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;

        _currentHP = Mathf.Min(baseHP, _currentHP + amount);
        SyncHPBar();
    }

    // ─── Internal ──

    private void SyncHPBar()
    {
        if (hpBar != null)
            hpBar.UpdateHP(_currentHP);

        OnHPChanged?.Invoke(_currentHP, baseHP);
    }
}
