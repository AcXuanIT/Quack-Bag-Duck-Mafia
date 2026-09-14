using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : Singleton<WeaponManager>
{

    // ── Inspector ───
    [Header("Data Source")]
    [SerializeField] private WeaponData weaponDatabase;

    // ── Runtime cache ─
    private Dictionary<int, WeaponEntry> _cache = new Dictionary<int, WeaponEntry>();

    // ── Events ──
    public static event Action<WeaponEntry> OnWeaponChanged;
    public static event Action OnDatabaseReloaded;

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        BuildCache();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Cache

    private void BuildCache()
    {
        _cache.Clear();
        if (weaponDatabase == null || weaponDatabase.Weapons == null) return;
        foreach (var asset in weaponDatabase.Weapons)
        {
            var w = asset != null ? asset.Entry : null;
            if (w == null) continue;
            if (!_cache.ContainsKey(w.ID)) _cache[w.ID] = w;
            else Debug.LogWarning($"[WeaponManager] Duplicate weapon ID={w.ID} ({w.Name})");
        }
        Debug.Log($"[WeaponManager] Loaded {_cache.Count} weapons.");
    }

    public void ReloadDatabase()
    {
        BuildCache();
        OnDatabaseReloaded?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Read API

    public WeaponData Database => weaponDatabase;

    public List<WeaponEntry> GetAllWeapons()      => new List<WeaponEntry>(_cache.Values);

    public List<WeaponEntry> GetUnlockedWeapons()
    {
        var r = new List<WeaponEntry>();
        foreach (var w in _cache.Values) if (!w.IsLocked) r.Add(w);
        return r;
    }

    public List<WeaponEntry> GetLockedWeapons()
    {
        var r = new List<WeaponEntry>();
        foreach (var w in _cache.Values) if (w.IsLocked) r.Add(w);
        return r;
    }

    public WeaponEntry GetWeapon(int id) { _cache.TryGetValue(id, out var e); return e; }
    public bool        HasWeapon(int id) => _cache.ContainsKey(id);

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Write API

    public bool UnlockWeapon(int id)
    {
        var w = GetWeapon(id);
        if (w == null || !w.IsLocked) return false;
        w.IsLocked = false;
        Dirty(id); OnWeaponChanged?.Invoke(w);
        return true;
    }

    public bool LockWeapon(int id)
    {
        var w = GetWeapon(id);
        if (w == null) return false;
        w.IsLocked = true;
        Dirty(id); OnWeaponChanged?.Invoke(w);
        return true;
    }

    public void AddXP(int id, int amount)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked) return;
        w.AddXP(amount);
        Dirty(id); OnWeaponChanged?.Invoke(w);
    }
    public bool TryLevelUp(int id, ref int playerCoin)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked)              return false;
        if (w.Level >= 5)                          { Debug.Log($"[WeaponManager] {w.Name} đã max level!"); return false; }

        int needed = w.GetCurrentXPToNextLevel();
        if (w.XP < needed)                        { Debug.Log($"[WeaponManager] {w.Name} chưa đủ XP."); return false; }
        if (playerCoin < w.Coin)                   { Debug.Log($"[WeaponManager] Không đủ coin."); return false; }

        playerCoin      -= w.Coin;
        w.XP            -= needed;
        w.Level          = Mathf.Clamp(w.Level + 1, 1, 5);
        for (int i = 0; i < w.DamagePerLevel.Length; i++) w.DamagePerLevel[i] *= 1.15f;
        for (int i = 0; i < w.HPPerLevel.Length; i++) w.HPPerLevel[i] *= 1.10f;
        w.Coin           = Mathf.RoundToInt(w.Coin * 1.5f);

        Dirty(id); OnWeaponChanged?.Invoke(w);

        return true;
    }

    public void UpdateWeapon(WeaponEntry updated)
    {
        var w = GetWeapon(updated.ID);
        if (w == null) { Debug.LogWarning($"[WeaponManager] UpdateWeapon: ID {updated.ID} not found."); return; }

        w.Name          = updated.Name;
        w.SpriteTier1   = updated.SpriteTier1;
        w.SpriteTier2   = updated.SpriteTier2;
        w.SpriteTier3   = updated.SpriteTier3;
        w.SpriteTier4   = updated.SpriteTier4;
        w.ShapeSprite   = updated.ShapeSprite;
        w.Level         = Mathf.Clamp(updated.Level, 1, 5);
        w.XP            = updated.XP;
        w.XPToNextLevel = updated.XPToNextLevel;
        w.DamagePerLevel = updated.DamagePerLevel;
        w.HPPerLevel    = updated.HPPerLevel;
        w.Coin          = updated.Coin;
        w.IsLocked      = updated.IsLocked;

        Dirty(updated.ID); OnWeaponChanged?.Invoke(w);
    }

    public bool AddWeapon(WeaponDataAsset newWeaponAsset)
    {
        if (weaponDatabase == null || newWeaponAsset == null || newWeaponAsset.Entry == null) return false;
        if (_cache.ContainsKey(newWeaponAsset.Entry.ID))
        {
            Debug.LogWarning($"[WeaponManager] AddWeapon: ID {newWeaponAsset.Entry.ID} đã tồn tại.");
            return false;
        }
        var list = weaponDatabase.Weapons != null
            ? new List<WeaponDataAsset>(weaponDatabase.Weapons)
            : new List<WeaponDataAsset>();
        list.Add(newWeaponAsset);
        weaponDatabase.Weapons = list.ToArray();
        _cache[newWeaponAsset.Entry.ID] = newWeaponAsset.Entry;
        DirtyDatabase(); OnDatabaseReloaded?.Invoke();
        return true;
    }
    public bool RemoveWeapon(int id)
    {
        if (!_cache.ContainsKey(id) || weaponDatabase == null || weaponDatabase.Weapons == null) return false;
        _cache.Remove(id);
        var list = new List<WeaponDataAsset>(weaponDatabase.Weapons);
        list.RemoveAll(w => w == null || w.Entry == null || w.Entry.ID == id);
        weaponDatabase.Weapons = list.ToArray();
        DirtyDatabase(); OnDatabaseReloaded?.Invoke();
        return true;
    }

    public int GenerateNewID()
    {
        int next = 1;
        while (_cache.ContainsKey(next)) next++;
        return next;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Persistence helpers

    private void Dirty(int id)
    {
#if UNITY_EDITOR
        if (weaponDatabase == null || weaponDatabase.Weapons == null) return;
        foreach (var asset in weaponDatabase.Weapons)
        {
            if (asset != null && asset.Entry != null && asset.Entry.ID == id)
            {
                UnityEditor.EditorUtility.SetDirty(asset);
                return;
            }
        }
#endif
    }

    private void DirtyDatabase()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(weaponDatabase);
#endif
    }

    #endregion
}
