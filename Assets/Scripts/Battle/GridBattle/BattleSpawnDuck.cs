using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý việc spawn UnitDuck thật vào trận đấu, dựa trên cách người chơi sắp xếp
/// Gear liền kề Unit trên Battle Grid trong lúc Turn Setup.
///
/// CƠ CHẾ: khi 1 GearItem (đã đặt trên Grid) LIỀN KỀ với 1 hoặc nhiều UnitItem, thì mỗi
/// khi weapon.TimeDelay (chu kỳ bắn của weapon đó) chạy hết, TỰ ĐỘNG spawn 1 bản UnitDuck
/// cho MỖI UnitItem đang liên kết. "Có thể có nhiều liên kết tuỳ cách sắp xếp":
///   - 1 Gear liền kề NHIỀU Unit -> mỗi Unit được spawn riêng theo CÙNG nhịp của Gear đó.
///   - 1 Unit liền kề NHIỀU Gear -> Unit đó được spawn ĐỘC LẬP bởi từng Gear (nhiều lần/turn).
///
/// LIÊN KẾT ĐƯỢC "CHỐT" 1 LẦN khi Turn Battle bắt đầu (gọi BuildLinks() từ
/// BattleManager.FinishTurnSetup(), snapshot đúng lúc chuyển từ Setup -> Battle) — không
/// tính real-time trong lúc Shop đang mở, để tránh liên kết thay đổi liên tục khi người
/// chơi còn đang kéo-thả sắp xếp Grid.
/// </summary>
public class BattleSpawnDuck : MonoBehaviour
{
    [Header("Battle Duck Prefab (dùng chung cho mọi MyDuckData — Init() tự đổi sprite theo data)")]
    [SerializeField] private GameObject unitDuckPrefab;

    [Header("Grid tham chiếu để quét GearItem/UnitItem đang đặt")]
    [SerializeField] private BattleGridManager gridManager;

    [Header("Tramform Base Spawn")]
    [SerializeField] private Transform spawnbase;

    [Header("Parent Object")]
    [SerializeField] private Transform parent;


    /// <summary>1 liên kết Gear -> 1 Unit cụ thể liền kề, tự đếm ngược theo weapon.TimeDelay riêng.</summary>
    private class SpawnLink
    {
        public WeaponEntry Weapon;
        public int         WeaponTier;
        public MyDuckData  Duck;
        public int         DuckTier;
        public Vector3     SpawnWorldPos;
        public float       Timer;
    }

    private readonly List<SpawnLink> _activeLinks = new List<SpawnLink>();

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<BattleGridManager>();
    }

    /// <summary>
    /// Quét toàn bộ GearItem đang đặt trên Grid (IsPlacedOnGrid), tìm UnitItem liền kề qua
    /// GearItemUI.GetAdjacentUnits(), tạo lại toàn bộ danh sách liên kết từ đầu.
    /// Gọi 1 lần mỗi khi Turn Battle bắt đầu (BattleManager.FinishTurnSetup()).
    /// </summary>
    public void BuildLinks()
    {
        _activeLinks.Clear();

        var allGears = FindObjectsOfType<GearItemUI>();
        foreach (var gear in allGears)
        {
            if (!gear.IsPlacedOnGrid || gear.Weapon == null) continue;

            var adjacentUnits = gear.GetAdjacentUnits();
            if (adjacentUnits.Count == 0) continue;

            float cycle = gear.Weapon.TimeDelay > 0f ? gear.Weapon.TimeDelay : 1f;
            Vector3 spawnPos = gear.PlacedAnchorCell != null
                ? gear.PlacedAnchorCell.transform.position
                : gear.transform.position;

            foreach (var unit in adjacentUnits)
            {
                if (unit.Unit == null) continue;

                _activeLinks.Add(new SpawnLink
                {
                    Weapon        = gear.Weapon,
                    WeaponTier    = gear.CurrentTier,
                    Duck          = unit.Unit,
                    DuckTier      = unit.CurrentTier,
                    SpawnWorldPos = spawnPos,
                    Timer         = cycle
                });
            }
        }

        Debug.Log($"[BattleSpawnDuck] Da tao {_activeLinks.Count} lien ket Gear-Unit cho Turn nay.");
    }

    /// <summary>Xoá toàn bộ liên kết đang hoạt động (gọi khi quay lại Turn Setup / kết thúc trận).</summary>
    public void ClearLinks() => _activeLinks.Clear();

    private void Update()
    {
        if (_activeLinks.Count == 0) return;

        foreach (var link in _activeLinks)
        {
            link.Timer -= Time.deltaTime;
            if (link.Timer > 0f) continue;

            link.Timer = link.Weapon != null && link.Weapon.TimeDelay > 0f ? link.Weapon.TimeDelay : 1f;
            SpawnDuck(link.Weapon, link.Duck, link.DuckTier, link.WeaponTier, link.SpawnWorldPos);
        }
    }

    /// <summary>
    /// Spawn 1 UnitDuck thật vào trận, dùng Weapon truyền vào cho cả chỉ số lẫn hình ảnh.
    /// Giữ đúng chữ ký gốc (WeaponEntry, MyDuckData) — mặc định Tier 1 và spawn tại vị trí
    /// của chính BattleSpawnDuck nếu gọi trực tiếp (không qua BuildLinks()/Update()).
    /// </summary>
    public void SpawnDuck(WeaponEntry weapon, MyDuckData duck)
        => SpawnDuck(weapon, duck, 1, 1, spawnbase.position, parent);

    private void SpawnDuck(WeaponEntry weapon, MyDuckData duck, int duckTier, int weaponTier, Vector3 pos, Transform parent = null)
    {
        if (unitDuckPrefab == null)
        {
            Debug.LogWarning("[BattleSpawnDuck] Thieu unitDuckPrefab!");
            return;
        }
        if (duck == null || weapon == null) return;

        GameObject go = PoolingManager.Spawn(unitDuckPrefab, pos, Quaternion.identity, parent);

        UnitDuck unitDuck = go.GetComponent<UnitDuck>();

        if (unitDuck != null)
            unitDuck.Init(duck, weapon, duckTier, weaponTier);
        else
            Debug.LogWarning("[BattleSpawnDuck] unitDuckPrefab thieu component UnitDuck!");
    }
}
