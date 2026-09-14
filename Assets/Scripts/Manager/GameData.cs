using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Game/Game Data")]
public class GameData : ScriptableObject
{
    [Header("=== Currency ===")]
    [SerializeField] private int coin;
    [SerializeField] private int ruby;

    [Header("=== Progress ===")]
    [SerializeField] private int currentMapIndex = 1;

    [Header("=== Power ===")]
    [SerializeField] private int power;

    // ─── Events ───
    public event Action<int> OnCoinChanged;
    public event Action<int> OnRubyChanged;
    public event Action<int> OnMapIndexChanged;
    public event Action<int> OnPowerChanged;

    #region Currency: Coin

    public int Coin => coin;

    public void SetCoin(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == coin) return;
        coin = clamped;
        OnCoinChanged?.Invoke(coin);
    }
    public void AddCoin(int amount)
    {
        if (amount == 0) return;
        SetCoin(coin + amount);
    }
    public bool TrySpendCoin(int amount)
    {
        if (amount <= 0) return true;
        if (coin < amount) return false;
        SetCoin(coin - amount);
        return true;
    }

    #endregion

    #region Currency: Ruby

    public int Ruby => ruby;

    public void SetRuby(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == ruby) return;
        ruby = clamped;
        OnRubyChanged?.Invoke(ruby);
    }

    public void AddRuby(int amount)
    {
        if (amount == 0) return;
        SetRuby(ruby + amount);
    }

    public bool TrySpendRuby(int amount)
    {
        if (amount <= 0) return true;
        if (ruby < amount) return false;
        SetRuby(ruby - amount);
        return true;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Progress: CurrentMapIndex

    public int CurrentMapIndex => currentMapIndex;
    public void SetCurrentMapIndex(int value)
    {
        int clamped = Mathf.Max(1, value);
        if (clamped == currentMapIndex) return;
        currentMapIndex = clamped;
        OnMapIndexChanged?.Invoke(currentMapIndex);
    }

    public void AdvanceMapIndex(int mapIndex)
    {
        if (mapIndex > currentMapIndex)
            SetCurrentMapIndex(mapIndex);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Power

    public int Power => power;

    public void SetPower(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (clamped == power) return;
        power = clamped;
        OnPowerChanged?.Invoke(power);
    }

    #endregion
}
