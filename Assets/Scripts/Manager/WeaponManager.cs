using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  WeaponManager – Runtime singleton quản lý toàn bộ vũ khí.
//  Gắn vào child "WeaponManager" của GameManager.
//
//  ĐÃ ĐỔI KIẾN TRÚC: weaponDatabase.Weapons giờ là WeaponDataAsset[] (mỗi weapon 1 file .asset
//  riêng, dùng chung với Enemy qua EnemyDuckData.weaponAsset) thay vì WeaponEntry[] nhúng trực
//  tiếp. API đọc (GetWeapon/GetAllWeapons/...) vẫn trả về WeaponEntry như cũ — code bên ngoài
//  (UI, Shop, Duck...) không cần đổi gì. Chỉ AddWeapon/RemoveWeapon đổi để thao tác trên
//  WeaponDataAsset (asset đã tồn tại sẵn trên disk — quản lý bởi WeaponEditorWindow).
//
//  XP/XPToNextLevel: XPToNextLevel giờ là int[5] (xem WeaponData.cs — cùng quy ước index với
//  DamagePerLevel/HPPerLevel: index 0=cần để Lv1→Lv2 ... 3=Lv4→Lv5, 4=Lv5 không dùng). Dùng
//  w.GetCurrentXPToNextLevel() để lấy ngưỡng của Level hiện tại thay vì đọc thẳng field cũ.
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
        foreach (var asset in weaponDatabase.Weapons)
        {
            var w = asset != null ? asset.Entry : null;
            if (w == null) continue;
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
        Debug.Log($"[WeaponManager] Unlocked: {w.Name}");
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

    /// <summary>
    /// Cộng thêm XP cho weapon (VD: mỗi khi weapon này diệt 1 EnemyDuck — xem Duck.OnKilledTarget()
    /// trong battle). Uỷ quyền toàn bộ logic cộng XP + tự lên Level cho WeaponEntry.AddXP() (dùng
    /// chung code với battle runtime, xem WeaponData.cs).
    /// </summary>
    public void AddXP(int id, int amount)
    {
        var w = GetWeapon(id);
        if (w == null || w.IsLocked) return;
        w.AddXP(amount);
        Dirty(id); OnWeaponChanged?.Invoke(w);
        Debug.Log($"[WeaponManager] {w.Name} XP → {w.XP}/{w.GetCurrentXPToNextLevel()} (Lv{w.Level})");
    }

    /// <summary>
    /// Nâng Level thủ công bằng Coin khi XP đã đủ ngưỡng GetCurrentXPToNextLevel() của Level hiện
    /// tại. XP dư (nếu có, trường hợp XP đã được cộng vượt ngưỡng qua AddXP nhưng chưa tự lên Level
    /// do đã max — hiếm khi xảy ra) được trừ đúng theo ngưỡng đó.
    /// </summary>
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

        Dirty(updated.ID); OnWeaponChanged?.Invoke(w);
    }

    /// <summary>
    /// Thêm 1 WeaponDataAsset ĐÃ TỒN TẠI SẴN (asset trên disk, VD tạo qua WeaponEditorWindow)
    /// vào database. KHÔNG tạo asset mới ở runtime — quản lý asset file là việc của Editor.
    /// </summary>
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

    /// <summary>Gỡ tham chiếu weapon khỏi database (KHÔNG xoá file .asset trên disk).</summary>
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

    /// <summary>Đánh dấu dirty đúng WeaponDataAsset chứa weapon ID này (KHÔNG phải database,
    /// vì dữ liệu giờ nằm ở từng asset riêng).</summary>
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
