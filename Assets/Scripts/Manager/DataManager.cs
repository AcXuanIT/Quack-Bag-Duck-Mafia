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
public class DataManager : Singleton<DataManager>
{
    [Header("=== Assets/Data (gán tay hoặc dùng nút Auto-Populate bên dưới) ===")]
    [SerializeField] private List<MyDuckDataAsset> myDuckAssets = new List<MyDuckDataAsset>();
    [SerializeField] private List<EnemyDuckDataAsset> enemyDuckAssets = new List<EnemyDuckDataAsset>();

    [Header("=== Assets/Resources/Data (tự Resources.Load nếu để trống) ===")]
    [SerializeField] private WeaponData weaponDatabase;

    [SerializeField] private MapBattsleData mapBattleData;


    public WeaponData WeaponDatabase => weaponDatabase;
    public MapBattsleData MapBattleData => mapBattleData;
    public IReadOnlyList<MyDuckDataAsset> AllMyDuckAssets => myDuckAssets;
    public IReadOnlyList<EnemyDuckDataAsset> AllEnemyDuckAssets => enemyDuckAssets;


    /*private void Awake()
    {
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (weaponDatabase == null)
            weaponDatabase = Resources.Load<WeaponData>("Data/WeaponDatabase");

    }*/
}