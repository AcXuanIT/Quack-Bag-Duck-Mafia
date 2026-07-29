using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour quản lý UnitData toàn game.
/// Gắn vào 1 GameObject trong scene (e.g. "GameManager" hoặc "DataManager").
/// Các script khác truy cập qua UnitDataManager.Instance.
/// </summary>
public class UnitDataManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────
    public static UnitDataManager Instance { get; private set; }

    // ─── Inspector ───────────────────────────────────────────
    [Header("Database")]
    [Tooltip("Kéo UnitDatabase.asset vào đây")]
    [SerializeField] private UnitData unitDatabase;

    // ─── Lifecycle ───────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // bỏ comment nếu cần persistent giữa scene

        if (unitDatabase == null)
            unitDatabase = Resources.Load<UnitData>("Data/UnitDatabase");

        if (unitDatabase == null)
            Debug.LogError("[UnitDataManager] Không tìm thấy UnitDatabase! Kéo asset vào Inspector hoặc đặt vào Resources/Data/.");
        else
            Debug.Log($"[UnitDataManager] Loaded {unitDatabase.Units?.Length ?? 0} units.");
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>Lấy UnitEntry theo ID (null nếu không có).</summary>
    public UnitEntry GetUnit(int id)
    {
        if (unitDatabase == null) return null;
        return unitDatabase.GetByID(id);
    }

    /// <summary>Lấy UnitEntry theo tên (null nếu không có).</summary>
    public UnitEntry GetUnit(string unitName)
    {
        if (unitDatabase == null) return null;
        return unitDatabase.GetByName(unitName);
    }

    /// <summary>Toàn bộ danh sách Unit (readonly).</summary>
    public UnitEntry[] GetAllUnits()
    {
        return unitDatabase != null ? unitDatabase.Units : System.Array.Empty<UnitEntry>();
    }

    /// <summary>HP cơ bản của unit tại tier chỉ định.</summary>
    public float GetBaseHP(int unitID, int tier)
    {
        var u = GetUnit(unitID);
        return u != null ? u.GetBaseHP(tier) : 0f;
    }

    /// <summary>Sprite của unit tại tier chỉ định.</summary>
    public Sprite GetSprite(int unitID, int tier)
    {
        var u = GetUnit(unitID);
        return u != null ? u.GetSprite(tier) : null;
    }

#if UNITY_EDITOR
    [ContextMenu("Log All Units")]
    void EditorLogAll()
    {
        var all = GetAllUnits();
        foreach (var u in all)
            Debug.Log($"[Unit] ID={u.ID} Name={u.Name} | HP: {u.BaseHP_Tier1}/{u.BaseHP_Tier2}/{u.BaseHP_Tier3}/{u.BaseHP_Tier4}");
    }
#endif
}
