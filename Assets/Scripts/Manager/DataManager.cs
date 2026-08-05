using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tập trung việc truy cập Data trong 2 nguồn:
///   1. Assets/Data/ (MyDuckDataAsset, EnemyDuckDataAsset) — asset THƯỜNG, không nằm
///      trong Resources nên KHÔNG THỂ Resources.Load() lúc runtime trong build thật.
///      Phải gán tay qua Inspector (list) — dùng nút Editor "Auto-Populate" bên dưới
///      để tự động kéo hết asset trong 2 folder vào list, chỉ chạy trong Editor.
///   2. Assets/Resources/Data/ (WeaponDatabase.asset) — nằm trong Resources nên
///      CÓ THỂ Resources.Load() lúc runtime, kể cả trong build thật.
///
/// GearItemUI / UnitPlayerItemUI lấy WeaponEntry / MyDuckData qua DataManager
/// (bằng ID) thay vì tự resolve trực tiếp từ ShopItemData.
///
/// Singleton tự khởi tạo (lazy) giống PoolingManager — nhưng vì list Assets/Data
/// cần asset reference thật (không auto có ở runtime build), NÊN đặt sẵn 1
/// GameObject có gắn DataManager trong scene và gán/Auto-Populate list trong Editor,
/// thay vì để nó tự tạo rỗng lúc runtime.
/// </summary>
public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    public static DataManager Instance
    {
        get { EnsureInstance(); return _instance; }
    }

    [Header("=== Assets/Data (gán tay hoặc dùng nút Auto-Populate bên dưới) ===")]
    [SerializeField] private List<MyDuckDataAsset>    myDuckAssets    = new List<MyDuckDataAsset>();
    [SerializeField] private List<EnemyDuckDataAsset> enemyDuckAssets = new List<EnemyDuckDataAsset>();

    [Header("=== Assets/Resources/Data (tự Resources.Load nếu để trống) ===")]
    [SerializeField] private WeaponData weaponDatabase;

    private Dictionary<int, MyDuckData>    _myDuckLookup;
    private Dictionary<int, EnemyDuckData> _enemyDuckLookup;
    private Dictionary<int, WeaponEntry>   _weaponLookup;

    public WeaponData WeaponDatabase => weaponDatabase;
    public IReadOnlyList<MyDuckDataAsset>    AllMyDuckAssets    => myDuckAssets;
    public IReadOnlyList<EnemyDuckDataAsset> AllEnemyDuckAssets => enemyDuckAssets;

    // ─── Singleton (lazy) ───────────────────────────────────

    private static void EnsureInstance()
    {
        if (_instance != null) return;

        _instance = FindObjectOfType<DataManager>();
        if (_instance != null) { _instance.Initialize(); return; }

        var go = new GameObject("DataManager (Auto)");
        _instance = go.AddComponent<DataManager>();
        Debug.LogWarning("[DataManager] Không tìm thấy DataManager trong scene — tự tạo mới NHƯNG list Assets/Data " +
                          "sẽ RỖNG (không có sẵn trong build). Hãy đặt 1 GameObject có DataManager trong scene và " +
                          "Auto-Populate/gán tay list trong Editor.");
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (weaponDatabase == null)
            weaponDatabase = Resources.Load<WeaponData>("Data/WeaponDatabase");

        BuildLookups();
    }

    private void BuildLookups()
    {
        _myDuckLookup = new Dictionary<int, MyDuckData>();
        foreach (var asset in myDuckAssets)
        {
            if (asset == null || asset.Data == null) continue;
            if (!_myDuckLookup.ContainsKey(asset.Data.ID))
                _myDuckLookup.Add(asset.Data.ID, asset.Data);
        }

        _enemyDuckLookup = new Dictionary<int, EnemyDuckData>();
        foreach (var asset in enemyDuckAssets)
        {
            if (asset == null || asset.Data == null) continue;
            if (!_enemyDuckLookup.ContainsKey(asset.Data.ID))
                _enemyDuckLookup.Add(asset.Data.ID, asset.Data);
        }

        _weaponLookup = new Dictionary<int, WeaponEntry>();
        if (weaponDatabase != null && weaponDatabase.Weapons != null)
        {
            foreach (var w in weaponDatabase.Weapons)
            {
                if (w == null) continue;
                if (!_weaponLookup.ContainsKey(w.ID))
                    _weaponLookup.Add(w.ID, w);
            }
        }
    }

    // ─── Public API ─────────────────────────────────────────

    public MyDuckData GetMyDuckData(int id)
    {
        if (_myDuckLookup == null) BuildLookups();
        return _myDuckLookup.TryGetValue(id, out var d) ? d : null;
    }

    public EnemyDuckData GetEnemyDuckData(int id)
    {
        if (_enemyDuckLookup == null) BuildLookups();
        return _enemyDuckLookup.TryGetValue(id, out var d) ? d : null;
    }

    public WeaponEntry GetWeaponEntry(int weaponID)
    {
        if (_weaponLookup == null) BuildLookups();
        return _weaponLookup.TryGetValue(weaponID, out var w) ? w : null;
    }

    /// <summary>Gọi lại nếu list thay đổi lúc runtime (VD gán thêm asset bằng code).</summary>
    public void RefreshLookups() => BuildLookups();

#if UNITY_EDITOR
    [ContextMenu("Editor: Auto-Populate From Assets/Data")]
    private void AutoPopulateFromAssetsData()
    {
        myDuckAssets.Clear();
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MyDuckDataAsset", new[] { "Assets/Data/MyDuck" }))
        {
            var path  = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MyDuckDataAsset>(path);
            if (asset != null) myDuckAssets.Add(asset);
        }

        enemyDuckAssets.Clear();
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDuckDataAsset", new[] { "Assets/Data/EnemyDuck" }))
        {
            var path  = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDuckDataAsset>(path);
            if (asset != null) enemyDuckAssets.Add(asset);
        }

        if (weaponDatabase == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/Resources/Data" });
            if (guids.Length > 0)
                weaponDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        BuildLookups();
        Debug.Log($"[DataManager] Auto-populated: {myDuckAssets.Count} MyDuck, {enemyDuckAssets.Count} EnemyDuck, " +
                  $"weaponDatabase={(weaponDatabase != null ? weaponDatabase.name : "NULL")}");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
