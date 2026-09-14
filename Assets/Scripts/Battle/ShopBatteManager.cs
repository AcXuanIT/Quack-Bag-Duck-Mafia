using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBatteManager : MonoBehaviour
{
    [Header("Price")]
    [SerializeField] private int buyPrice = 20;

    [Header("References")]
    [SerializeField] private RectTransform trashZone;
    [SerializeField] private Image         trashImage;

    [SerializeField] private Button          btnBuy;
    [SerializeField] private Transform       componentContainer;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Battle Manager")]
    [SerializeField] private BattleManager battleManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject gridItemPrefab;   
    [SerializeField] private GameObject gearItemPrefab;   
    [SerializeField] private GameObject unitItemPrefab;   

    [Header("Spawn Config")]
    [SerializeField] private int defaultSpawnCount = 3;
    [SerializeField] private int maxSlots          = 4;

    [Header("Spawn Weights")]
    [SerializeField] private float gridSpawnWeight = 20f; 
    [SerializeField] private float gearSpawnWeight = 40f; 
    [SerializeField] private float unitSpawnWeight = 40f; 

    [Header("Item Pool")]
    [SerializeField] private List<ShopItemData> gridItems = new List<ShopItemData>();

    private List<GameObject>  _spawnedItems = new List<GameObject>();
    private BattleGridManager _gridManager;

    void Start()
    {
        _gridManager = FindObjectOfType<BattleGridManager>();

        if (btnBuy != null) btnBuy.onClick.AddListener(OnBuyPressed);

        RefreshUI();
        SyncSpawnedList();
    }

    void OnEnable()
    {
        if (battleManager != null)
            battleManager.OnTurnSetupStart += HandleTurnSetupStart;
    }

    void OnDisable()
    {
        if (battleManager != null)
            battleManager.OnTurnSetupStart -= HandleTurnSetupStart;
    }

    void OnDestroy()
    {
        if (btnBuy != null) btnBuy.onClick.RemoveListener(OnBuyPressed);
    }

    // ── Turn Sync ─
    private void HandleTurnSetupStart(int turnIndex)
    {
        RefreshShop();
    }

    public void RefreshShop()
    {
        ClearAllItems();
        for (int i = 0; i < defaultSpawnCount; i++)
        {
            var type = GetRandomItemKindWeighted();
            SpawnItemOfType(type);
        }
    }

    private ItemKind GetRandomItemKindWeighted()
    {
        float total = gridSpawnWeight + gearSpawnWeight + unitSpawnWeight;
        if (total <= 0f) return (ItemKind)Random.Range(0, 3); 

        float roll = Random.Range(0f, total);

        if (roll < gridSpawnWeight) return ItemKind.Grid;
        roll -= gridSpawnWeight;

        if (roll < gearSpawnWeight) return ItemKind.Gear;

        return ItemKind.UnitDuck;
    }

    private void ClearAllItems()
    {
        SyncSpawnedList();
        foreach (var g in new List<GameObject>(_spawnedItems))
            Destroy(g);
        _spawnedItems.Clear();
    }

    public enum ItemKind { Grid, Gear, UnitDuck }

    // ── Buy ─
    public void OnBuyPressed()
    {
        SyncSpawnedList();
        int freeSlots = maxSlots - _spawnedItems.Count;

        if (freeSlots < 3) { Debug.Log("[Shop] Không đủ slot (cần ít nhất 3)."); return; }

        if (battleManager == null) { Debug.LogWarning("[Shop] Thieu BattleManager!"); return; }
        if (!battleManager.SpendMoney(buyPrice))
        {
            Debug.Log($"[Shop] Thiếu tiền ({battleManager.PlayerMoney}/{buyPrice}).");
            return;
        }
        for (int i = 0; i < 3; i++)
            SpawnItemOfType(GetRandomItemKindWeighted());
    }

    public void SpawnItemOfType(ItemKind type)
    {
        SyncSpawnedList();
        if (_spawnedItems.Count >= maxSlots) return;

        switch (type)
        {
            case ItemKind.Grid:     SpawnGridItem();  break;
            case ItemKind.Gear:     SpawnGearItem();  break;
            case ItemKind.UnitDuck: SpawnUnitItem();  break;
        }
    }
    private void SpawnGridItem()
    {
        if (gridItemPrefab == null) return;
        if (componentContainer == null) return;

        var eligible = GetEligibleGridItems();
        if (eligible.Count == 0)
        {
            return;
        }

        ShopItemData data = eligible[Random.Range(0, eligible.Count)];

        var go = Instantiate(gridItemPrefab, componentContainer);
        go.SetActive(true);

        var shopItem = go.GetComponent<IShopItem>();
        if (shopItem != null)
            shopItem.Setup(data, _gridManager, trashZone, trashImage);
        else
            Debug.LogWarning("[Shop] Prefab GridItem thieu component IShopItem!");

        RegisterSpawned(go);
    }

    private void SpawnGearItem()
    {
        if (gearItemPrefab == null) { Debug.LogWarning("[Shop] Thieu gearItemPrefab!"); return; }
        if (componentContainer == null) { Debug.LogWarning("[Shop] Thieu componentContainer!"); return; }

        var weaponDb = DataManager.Instance.WeaponDatabase;
        WeaponEntry[] entries = weaponDb != null ? weaponDb.GetEntries() : null;
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning("[Shop] DataManager.WeaponDatabase rong — khong co Gear nao de spawn!");
            return;
        }

        WeaponEntry weapon = entries[Random.Range(0, entries.Length)];

        var go = Instantiate(gearItemPrefab, componentContainer);
        go.SetActive(true);

        var gearUI = go.GetComponent<GearItemUI>();
        if (gearUI != null)
        {
            var containerRT = componentContainer as RectTransform;
            if (containerRT == null && componentContainer != null)
                containerRT = componentContainer.GetComponent<RectTransform>();
            gearUI.Setup(weapon, _gridManager, trashZone, trashImage, containerRT);
        }
        else
            Debug.LogWarning("[Shop] Prefab GearItem thieu component GearItemUI!");

        RegisterSpawned(go);
    }

    private void SpawnUnitItem()
    {
        if (unitItemPrefab == null) { Debug.LogWarning("[Shop] Thieu unitItemPrefab!"); return; }
        if (componentContainer == null) { Debug.LogWarning("[Shop] Thieu componentContainer!"); return; }

        var myDuckAssets = DataManager.Instance.AllMyDuckAssets;
        if (myDuckAssets == null || myDuckAssets.Count == 0)
        {
            Debug.LogWarning("[Shop] DataManager.AllMyDuckAssets rong — khong co UnitDuck nao de spawn!");
            return;
        }

        var asset = myDuckAssets[Random.Range(0, myDuckAssets.Count)];
        MyDuckData unit = asset != null ? asset.Data : null;
        if (unit == null)
        {
            Debug.LogWarning("[Shop] MyDuckDataAsset duoc chon co Data NULL!");
            return;
        }

        var go = Instantiate(unitItemPrefab, componentContainer);
        go.SetActive(true);

        var unitUI = go.GetComponent<UnitPlayerItemUI>();
        if (unitUI != null)
        {
            var containerRT = componentContainer as RectTransform;
            if (containerRT == null && componentContainer != null)
                containerRT = componentContainer.GetComponent<RectTransform>();
            unitUI.Setup(unit, _gridManager, trashZone, trashImage, containerRT);
        }
        else
            Debug.LogWarning("[Shop] Prefab UnitItem thieu component UnitPlayerItemUI!");

        RegisterSpawned(go);
    }

    private void RegisterSpawned(GameObject go)
    {
        _spawnedItems.Add(go);

        var rt = componentContainer.GetComponent<RectTransform>();
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // ── Helpers 
    public void SyncSpawnedList()
    {
        _spawnedItems.Clear();
        if (componentContainer == null) return;
        foreach (Transform child in componentContainer)
            if (child.gameObject.activeSelf) _spawnedItems.Add(child.gameObject);
    }

    public void RemoveItem(GameObject item)   { if (item == null) return; _spawnedItems.Remove(item); Destroy(item); }
    public void RemoveItemAt(int i)           { SyncSpawnedList(); if (i < 0 || i >= _spawnedItems.Count) return; var g = _spawnedItems[i]; _spawnedItems.RemoveAt(i); Destroy(g); }
    public int  CurrentItemCount             { get { SyncSpawnedList(); return _spawnedItems.Count; } }
    public bool HasFreeSlot                  => CurrentItemCount < maxSlots;

    public int PlayerGold => battleManager != null ? battleManager.PlayerMoney : 0;

    public void AddGold(int amount)
    {
        if (battleManager != null) battleManager.AddMoney(amount);
    }

    public void RefreshUI()
    {
        int currentMoney = battleManager != null ? battleManager.PlayerMoney : 0;
        if (priceText != null) priceText.text = buyPrice.ToString();
        if (btnBuy    != null) btnBuy.interactable = (currentMoney >= buyPrice);
    }

    private List<ShopItemData> GetEligibleGridItems()
    {
        var ok = new List<ShopItemData>();

        if (_gridManager == null)
        {
            Debug.LogWarning("[Shop] Thieu BattleGridManager — khong the rang buoc GridItem theo GridSystem, tra ve nguyen pool.");
            ok.AddRange(gridItems);
            return ok;
        }

        foreach (var it in gridItems)
        {
            if (it == null || it.gridCells == null || it.gridCells.Length == 0) continue;
            if (_gridManager.HasValidPlacement(it.gridCells))
                ok.Add(it);
        }

        return ok;
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Buy")]         void EditorBuy()  => OnBuyPressed();
    [ContextMenu("Test: Spawn Grid")]  void EditorGrid() => SpawnItemOfType(ItemKind.Grid);
    [ContextMenu("Test: Spawn Gear")]  void EditorGear() => SpawnItemOfType(ItemKind.Gear);
    [ContextMenu("Test: Spawn Duck")]  void EditorDuck() => SpawnItemOfType(ItemKind.UnitDuck);
    [ContextMenu("Test: Clear")]
    void EditorClear()
    {
        SyncSpawnedList();
        foreach (var g in new List<GameObject>(_spawnedItems)) { _spawnedItems.Remove(g); DestroyImmediate(g); }
    }
    [ContextMenu("Test: Refresh Shop (Turn Setup)")]
    void EditorRefreshShop() => RefreshShop();
#endif
}
