using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  WeaponManager – Runtime singleton quản lý toàn bộ vũ khí.
//  Gắn vào child "WeaponManager" của GameManager.
// ============================================================
public class WeaponManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static WeaponManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────
    [Header("Data Source")]
    [Tooltip("ScriptableObject chứa toàn bộ dữ liệu vũ khí")]
    [SerializeField] private WeaponData weaponDatabase;

    // ── Runtime cache ────────────────────────────────────────
    private Dictionary<int, WeaponEntry> _cache = new Dictionary<int, WeaponEntry>();

    // ── Events ───────────────────────────────────────────────
    /// <summary>Fired khi bất kỳ vũ khí nào thay đổi (unlock, levelup, xp…).</summary>
    public static event Action<WeaponEntry> OnWeaponChanged;
    /// <summary>Fired khi toàn bộ database được reload.</summary>
    public static event Action OnDatabaseReloaded;

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCache();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Cache

    private void BuildCache()
    {
        _cache.Clear();
        if (weaponDatabase == null || weaponDatabase.Weapons == null) return;
        foreach (var w in weaponDatabase.Weapons)
        {
            if (!_cache.ContainsKey(w.ID)) _cache[w.ID] = w;
            else Debug.LogWarning($"[WeaponManager] Duplicate weapon ID={w.ID} ({w.Name})");
        }
        Debug.Log($"[WeaponManager] Loaded {_cache.Count} weapons.");
    }

    /// <summary>Reload database từ SO (dùng khi SO bị sửa ngoài runtime).</summary>
    public void ReloadDatabase()
    {
        BuildCache();
        OnDatabaseReloaded?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Read API

    public WeaponData Database => weaponDatabase;

    public List<WeaponEntry> GetAllWeapons()      => new List<WeaponEntry>(weaponDatabase.Weapons);

    public List<WeaponEntry> GetUnlockedWeapons()
    {
        var r = new List<WeaponEntry>();
        foreach (var w in weaponDatabase.Weapons) if (!w.IsLocked) r.Add(w);
        return r;
    }

    public List<WeaponEntry> GetLockedWeapons()
    {
        var r = new List<WeaponEntry>();
        foreach (var w in weaponDatabase.Weapons) if (w.IsLocked) r.Add(w);
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
        Dirty(); OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] Unlocked: {w.Name}");
        return true;
    }

    public bool LockWeapon(int id)
    {
        var w = GetWeapon(id);
        if (w == null) return false;
        w.IsLocked = true;
        Dirty(); OnWeaponChanged?.Invoke(w);
        return true;
    }

    public void AddXP(int id, int amount)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked) return;
        w.XP = Mathf.Max(0, w.XP + amount);
        Dirty(); OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] {w.Name} XP → {w.XP}/{w.XPToNextLevel}");
    }

    public bool TryLevelUp(int id, ref int playerCoin)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked)              return false;
        if (w.Level >= 5)                          { Debug.Log($"[WeaponManager] {w.Name} đã max level!"); return false; }
        if (w.XP < w.XPToNextLevel)               { Debug.Log($"[WeaponManager] {w.Name} chưa đủ XP."); return false; }
        if (playerCoin < w.Coin)                   { Debug.Log($"[WeaponManager] Không đủ coin."); return false; }

        playerCoin      -= w.Coin;
        w.XP            -= w.XPToNextLevel;
        w.Level          = Mathf.Clamp(w.Level + 1, 1, 5);
        for (int i = 0; i < w.DamagePerLevel.Length; i++) w.DamagePerLevel[i] *= 1.15f;
        for (int i = 0; i < w.HPPerLevel.Length; i++) w.HPPerLevel[i] *= 1.10f;
        w.Coin           = Mathf.RoundToInt(w.Coin * 1.5f);
        w.XPToNextLevel  = Mathf.RoundToInt(w.XPToNextLevel * 1.3f);

        Dirty(); OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] {w.Name} leveled up → Lv{w.Level}!");
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

        Dirty(); OnWeaponChanged?.Invoke(w);
    }

    public bool AddWeapon(WeaponEntry newWeapon)
    {
        if (weaponDatabase == null) return false;
        if (_cache.ContainsKey(newWeapon.ID))
        {
            Debug.LogWarning($"[WeaponManager] AddWeapon: ID {newWeapon.ID} đã tồn tại.");
            return false;
        }
        var list = new List<WeaponEntry>(weaponDatabase.Weapons) { newWeapon };
        weaponDatabase.Weapons = list.ToArray();
        _cache[newWeapon.ID] = newWeapon;
        Dirty(); OnDatabaseReloaded?.Invoke();
        return true;
    }

    public bool RemoveWeapon(int id)
    {
        if (!_cache.ContainsKey(id)) return false;
        _cache.Remove(id);
        var list = new List<WeaponEntry>(weaponDatabase.Weapons);
        list.RemoveAll(w => w.ID == id);
        weaponDatabase.Weapons = list.ToArray();
        Dirty(); OnDatabaseReloaded?.Invoke();
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

    private void Dirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(weaponDatabase);
#endif
    }

    #endregion
}
