using System.Collections.Generic;
using UnityEngine;


public class BattleSpawnDuck : MonoBehaviour
{
    [Header("Battle Duck Prefab")]
    [SerializeField] private GameObject unitDuckPrefab;

    [Header("Grid")]
    [SerializeField] private BattleGridManager gridManager;

    [Header("Tramform Base Spawn")]
    [SerializeField] private Transform spawnbase;

    [Header("Parent Object")]
    [SerializeField] private Transform parent;

    [Header("Random Spawn Offset")]
    [SerializeField] private Vector2 randomOffsetY = new Vector2(-1f, 0f);
    [SerializeField] private Vector2 randomOffsetX = new Vector2(0f, 1f);

    private readonly List<UnitDuck> _spawnedDucks = new List<UnitDuck>();

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<BattleGridManager>();
    }

    public void DespawnAllDucks()
    {
        foreach (var duck in _spawnedDucks)
        {
            if (duck != null)
                PoolingManager.Despawn(duck.gameObject);
        }
        _spawnedDucks.Clear();
    }

    private Vector3 GetRandomSpawnPos()
    {
        Vector3 basePos = spawnbase != null ? spawnbase.position : transform.position;
        float randX = Random.Range(randomOffsetX.x, randomOffsetX.y);
        float randY = Random.Range(randomOffsetY.x, randomOffsetY.y);
        return new Vector3(basePos.x + randX, basePos.y + randY, basePos.z);
    }

    private void PlayMyTeamSpawnAnimation()
    {
        BattleManager.Instance?.myTem?.mytemAnimation?.AnimationSpawn();
    }

    public void SpawnDuck(WeaponEntry weapon, MyDuckData duck, int tierWeapon, int tierUnit)
        => SpawnDuck(weapon, duck, tierUnit, tierWeapon, GetRandomSpawnPos(), parent);

    private void SpawnDuck(WeaponEntry weapon, MyDuckData duck, int duckTier, int weaponTier, Vector3 pos, Transform parent = null)
    {
        if (unitDuckPrefab == null)
        {
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

        PlayMyTeamSpawnAnimation();
    }
}
