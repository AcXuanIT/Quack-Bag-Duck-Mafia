using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý việc spawn UnitDuck thật vào trận đấu.
///
/// CƠ CHẾ HIỆN TẠI: mỗi GearItemUI đã đặt trên Battle Grid và đang liền kề (4 hướng) với
/// 1 hoặc nhiều UnitItem tự chạy 1 thanh charge riêng (fillImage) theo weapon.TimeDelay
/// trong lúc BattleManager ở TurnBattle (xem GearItemUI.Update()/SpawnLinkedDucks()).
/// Khi thanh charge chạy đầy, GearItemUI gọi thẳng BattleManager.Instance.spawnDuck.SpawnDuck()
/// (hàm bên dưới) cho MỖI Unit đang liên kết rồi tự chạy lại từ đầu.
///
/// BattleSpawnDuck (class này) chỉ còn giữ vai trò: thực thi spawn 1 UnitDuck cụ thể
/// (SpawnDuck) và dọn sạch toàn bộ Duck trên sân khi 1 Wave kết thúc (DespawnAllDucks,
/// gọi từ BattleManager.CheckWaveCleared()).
///
/// Mỗi khi 1 UnitDuck được spawn, gọi BattleManager.Instance.myTem.mytemAnimation.AnimationSpawn()
/// để MyTeam "nhún" 1 lần báo hiệu vừa có Duck mới xuất hiện.
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

    [Header("Random Spawn Offset")]
    [Tooltip("Khoảng random cộng vào trục Y của spawnPos (mặc định -1 -> 0).")]
    [SerializeField] private Vector2 randomOffsetY = new Vector2(-1f, 0f);
    [Tooltip("Khoảng random cộng vào trục X của spawnPos (mặc định 0 -> 1).")]
    [SerializeField] private Vector2 randomOffsetX = new Vector2(0f, 1f);

    /// <summary>
    /// Toàn bộ UnitDuck đã được spawn ra sân (qua SpawnDuck), dùng để dọn sạch sân bằng
    /// DespawnAllDucks() mỗi khi Wave hiện tại đã sạch enemy và Game chuyển về TurnSetup.
    /// </summary>
    private readonly List<UnitDuck> _spawnedDucks = new List<UnitDuck>();

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<BattleGridManager>();
    }

    /// <summary>
    /// Dùng PoolingManager.Despawn để trả toàn bộ UnitDuck còn lại trên sân đấu (đang được
    /// quản lý trong _spawnedDucks) về pool. Gọi khi Wave hiện tại đã sạch enemy (BattleManager
    /// phát hiện qua BattleSpawnEnemy.IsWaveCleared()) và Game chuyển về TurnSetup, để đảm bảo
    /// sân đấu sạch trước khi bắt đầu turn kế tiếp.
    /// </summary>
    public void DespawnAllDucks()
    {
        foreach (var duck in _spawnedDucks)
        {
            if (duck != null)
                PoolingManager.Despawn(duck.gameObject);
        }
        _spawnedDucks.Clear();
    }

    /// <summary>
    /// Tính vị trí spawn ngẫu nhiên quanh spawnbase: X = spawnPos.x + random(offsetX.x, offsetX.y),
    /// Y = spawnPos.y + random(offsetY.x, offsetY.y). Mặc định offsetY (-1 -> 0), offsetX (0 -> 1).
    /// </summary>
    private Vector3 GetRandomSpawnPos()
    {
        Vector3 basePos = spawnbase != null ? spawnbase.position : transform.position;
        float randX = Random.Range(randomOffsetX.x, randomOffsetX.y);
        float randY = Random.Range(randomOffsetY.x, randomOffsetY.y);
        return new Vector3(basePos.x + randX, basePos.y + randY, basePos.z);
    }

    /// <summary>
    /// Gọi hiệu ứng "nhún" của MyTeam mỗi khi 1 Duck được spawn thành công.
    /// An toàn nếu BattleManager.Instance, myTem hoặc mytemAnimation chưa được gán.
    /// </summary>
    private void PlayMyTeamSpawnAnimation()
    {
        BattleManager.Instance?.myTem?.mytemAnimation?.AnimationSpawn();
    }

    /// <summary>
    /// Spawn 1 UnitDuck thật vào trận, dùng Weapon truyền vào cho cả chỉ số lẫn hình ảnh.
    /// Gọi trực tiếp từ GearItemUI.SpawnLinkedDucks() mỗi khi thanh charge của 1 Gear đã đặt
    /// trên Grid chạy đầy 1 vòng. Mặc định spawn tại vị trí ngẫu nhiên quanh spawnbase
    /// (Y: -1 -> 0, X: spawnPos.x + 0 -> 1).
    /// </summary>
    public void SpawnDuck(WeaponEntry weapon, MyDuckData duck, int tierWeapon, int tierUnit)
        => SpawnDuck(weapon, duck, tierUnit, tierWeapon, GetRandomSpawnPos(), parent);

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
        {
            unitDuck.Init(duck, weapon, duckTier, weaponTier);
            _spawnedDucks.Add(unitDuck);
        }
        else
            Debug.LogWarning("[BattleSpawnDuck] unitDuckPrefab thieu component UnitDuck!");

        PlayMyTeamSpawnAnimation();
    }
}
