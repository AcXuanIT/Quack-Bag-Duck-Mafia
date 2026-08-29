using System.Collections.Generic;
using UnityEngine;


public class DataManager : Singleton<DataManager>
{
    [Header("=== Assets/Data (gán tay hoặc dùng nút Auto-Populate bên dưới) ===")]
    [SerializeField] private List<MyDuckDataAsset> myDuckAssets = new List<MyDuckDataAsset>();
    [SerializeField] private List<EnemyDuckDataAsset> enemyDuckAssets = new List<EnemyDuckDataAsset>();

    [Header("=== Assets/Resources/Data (tự Resources.Load nếu để trống) ===")]
    [SerializeField] private WeaponData weaponDatabase;

    [SerializeField] private List<MapBattsleData> mapBattleData = new List<MapBattsleData>();


    public WeaponData WeaponDatabase => weaponDatabase;
    public IReadOnlyList<MapBattsleData> MapBattleData => mapBattleData;
    public IReadOnlyList<MyDuckDataAsset> AllMyDuckAssets => myDuckAssets;
    public IReadOnlyList<EnemyDuckDataAsset> AllEnemyDuckAssets => enemyDuckAssets;

}