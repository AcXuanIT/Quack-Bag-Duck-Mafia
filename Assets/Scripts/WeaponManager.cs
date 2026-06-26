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
    // Key = WeaponEntry.ID
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

    /// <summary>Trả về tất cả weapons (copy list, không ref trực tiếp).</summary>
    public List<WeaponEntry> GetAllWeapons()
    {
        return new List<WeaponEntry>(weaponDatabase.Weapons);
    }

    /// <summary>Tất cả vũ khí đã mở khoá.</summary>
    public List<WeaponEntry> GetUnlockedWeapons()
    {
        var result = new List<WeaponEntry>();
        foreach (var w in weaponDatabase.Weapons)
            if (!w.IsLocked) result.Add(w);
        return result;
    }

    /// <summary>Tất cả vũ khí chưa mở khoá.</summary>
    public List<WeaponEntry> GetLockedWeapons()
    {
        var result = new List<WeaponEntry>();
        foreach (var w in weaponDatabase.Weapons)
            if (w.IsLocked) result.Add(w);
        return result;
    }

    /// <summary>Lấy weapon theo ID. Trả null nếu không tìm thấy.</summary>
    public WeaponEntry GetWeapon(int id)
    {
        _cache.TryGetValue(id, out var entry);
        return entry;
    }

    /// <summary>Có weapon ID này không?</summary>
    public bool HasWeapon(int id) => _cache.ContainsKey(id);

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Write API (Runtime mutations – fire events + dirty SO)

    /// <summary>Mở khoá vũ khí theo ID.</summary>
    public bool UnlockWeapon(int id)
    {
        var w = GetWeapon(id);
        if (w == null) { Debug.LogWarning($"[WeaponManager] UnlockWeapon: ID {id} not found."); return false; }
        if (!w.IsLocked) return false;
        w.IsLocked = false;
        Dirty();
        OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] Unlocked: {w.Name}");
        return true;
    }

    /// <summary>Khoá lại vũ khí theo ID.</summary>
    public bool LockWeapon(int id)
    {
        var w = GetWeapon(id);
        if (w == null) return false;
        w.IsLocked = true;
        Dirty();
        OnWeaponChanged?.Invoke(w);
        return true;
    }

    /// <summary>Thêm XP cho vũ khí. Tự động kiểm tra đủ XP để level up.</summary>
    public void AddXP(int id, int amount)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked) return;
        w.XP = Mathf.Max(0, w.XP + amount);
        Dirty();
        OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] {w.Name} XP → {w.XP}/{w.XPToNextLevel}");
    }

    /// <summary>
    /// Nâng level nếu XP đủ và có đủ coin.
    /// Trả về true nếu level up thành công.
    /// </summary>
    public bool TryLevelUp(int id, ref int playerCoin)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked) return false;
        if (w.Level >= 5) { Debug.Log($"[WeaponManager] {w.Name} đã max level!"); return false; }
        if (w.XP < w.XPToNextLevel) { Debug.Log($"[WeaponManager] {w.Name} chưa đủ XP."); return false; }
        if (playerCoin < w.Coin) { Debug.Log($"[WeaponManager] Không đủ coin để nâng {w.Name}."); return false; }

        playerCoin -= w.Coin;
        w.XP -= w.XPToNextLevel;
        w.Level = Mathf.Clamp(w.Level + 1, 1, 5);
        // Scale stats khi lên level
        w.Damage   *= 1.15f;
        w.HP       *= 1.10f;
        w.Coin      = Mathf.RoundToInt(w.Coin * 1.5f);
        w.XPToNextLevel = Mathf.RoundToInt(w.XPToNextLevel * 1.3f);

        Dirty();
        OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] {w.Name} leveled up → Lv{w.Level}!");
        return true;
    }

    /// <summary>Cập nhật toàn bộ field của một weapon (dùng từ Editor tool).</summary>
    public void UpdateWeapon(WeaponEntry updated)
    {
        var w = GetWeapon(updated.ID);
        if (w == null) { Debug.LogWarning($"[WeaponManager] UpdateWeapon: ID {updated.ID} not found."); return; }

        w.Name          = updated.Name;
        w.Icon          = updated.Icon;
        w.Level         = Mathf.Clamp(updated.Level, 1, 5);
        w.XP            = updated.XP;
        w.XPToNextLevel = updated.XPToNextLevel;
        w.Damage        = updated.Damage;
        w.HP            = updated.HP;
        w.Coin          = updated.Coin;
        w.IsLocked      = updated.IsLocked;

        Dirty();
        OnWeaponChanged?.Invoke(w);
    }

    /// <summary>
    /// Thêm weapon mới vào database.
    /// ID phải unique; nếu trùng sẽ trả về false.
    /// </summary>
    public bool AddWeapon(WeaponEntry newWeapon)
    {
        if (weaponDatabase == null) return false;
        if (_cache.ContainsKey(newWeapon.ID))
        {
            Debug.LogWarning($"[WeaponManager] AddWeapon: ID {newWeapon.ID} đã tồn tại.");
            return false;
        }

        // Extend array
        var list = new List<WeaponEntry>(weaponDatabase.Weapons) { newWeapon };
        weaponDatabase.Weapons = list.ToArray();
        _cache[newWeapon.ID] = newWeapon;
        Dirty();
        OnDatabaseReloaded?.Invoke();
        return true;
    }

    /// <summary>Xoá weapon theo ID.</summary>
    public bool RemoveWeapon(int id)
    {
        if (!_cache.ContainsKey(id)) return false;
        _cache.Remove(id);
        var list = new List<WeaponEntry>(weaponDatabase.Weapons);
        list.RemoveAll(w => w.ID == id);
        weaponDatabase.Weapons = list.ToArray();
        Dirty();
        OnDatabaseReloaded?.Invoke();
        return true;
    }

    /// <summary>Sinh ID mới chưa tồn tại trong database.</summary>
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
