using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Shop trong màn Battle.
/// - 3 loại item spawn: Grid, Gear, UnitDuck
/// - Component GO chứa tối đa 4 item
/// - btnBuy spawn 2 hoặc 3 item; nếu Component đang có đúng 2 item thì spawn 2
/// </summary>
public class ShopBatteManager : MonoBehaviour
{
    [Header("Price")]
    [SerializeField] private int buyPrice = 100;

    [Header("References")]
    [SerializeField] private RectTransform trashZone;
    [SerializeField] private Image         trashImage;

    [SerializeField] private Button          btnBuy;
    [SerializeField] private Transform       componentContainer;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Prefabs")]
    [SerializeField] private GameObject gridItemPrefab;   // GridItem.prefab — dùng cho ItemType.Grid
    [SerializeField] private GameObject shopItemPrefab;   // ShopItem.prefab — dùng cho Gear / UnitDuck

    [Header("Spawn Config")]
    [SerializeField] private int defaultSpawnCount = 3;
    [SerializeField] private int maxSlots          = 4;

    [Header("Item Pool — Grid")]
    [SerializeField] private List<ShopItemData> gridItems     = new List<ShopItemData>();

    [Header("Item Pool — Gear")]
    [SerializeField] private List<ShopItemData> gearItems     = new List<ShopItemData>();

    [Header("Item Pool — Unit Duck")]
    [SerializeField] private List<ShopItemData> unitDuckItems = new List<ShopItemData>();

    private List<GameObject>  _spawnedItems = new List<GameObject>();
    private List<ShopItemData> _allPool     = new List<ShopItemData>();
    private int _playerGold = 9999;
    private BattleGridManager _gridManager;

    void Awake()
    {
        _gridManager = FindObjectOfType<BattleGridManager>();
        RebuildPool();
        if (btnBuy != null) btnBuy.onClick.AddListener(OnBuyPressed);
        RefreshUI();
        SyncSpawnedList();
    }

    void OnDestroy()
    {
        if (btnBuy != null) btnBuy.onClick.RemoveListener(OnBuyPressed);
    }

private void RebuildPool()
    {
        // gridItems pool chỉ chứa Grid assets (Gear/UnitDuck đã loại ra)
        _allPool.Clear();
        _allPool.AddRange(gridItems);
    }

    // ── Buy ──────────────────────────────────────────────────
    public void OnBuyPressed()
    {
        SyncSpawnedList();
        int currentCount = _spawnedItems.Count;
        int freeSlots    = maxSlots - currentCount;

        if (freeSlots <= 0) { Debug.Log("[Shop] Component đầy."); return; }
        if (_playerGold < buyPrice) { Debug.Log($"[Shop] Thiếu vàng ({_playerGold}/{buyPrice})."); return; }

        int toSpawn = (currentCount == 2) ? 2 : defaultSpawnCount;
        toSpawn = Mathf.Clamp(toSpawn, 0, freeSlots);
        if (toSpawn <= 0) return;

        _playerGold -= buyPrice;
        RefreshUI();

        for (int i = 0; i < toSpawn; i++) SpawnFromPool(_allPool);

        Debug.Log($"[Shop] Spawn {toSpawn}. Gold={_playerGold}. Slots={_spawnedItems.Count}/{maxSlots}");
    }

    // ── Spawn ────────────────────────────────────────────────
private void SpawnFromPool(List<ShopItemData> pool)
    {
        if (pool == null || pool.Count == 0) { Debug.LogWarning("[Shop] Pool rong!"); return; }
        if (componentContainer == null)      { Debug.LogWarning("[Shop] Thieu componentContainer!"); return; }

        var gridPool = pool.FindAll(d => d != null && d.itemType == ShopItemData.ItemType.Grid);
        if (gridPool.Count == 0) { Debug.LogWarning("[Shop] Khong co Grid item!"); return; }

        ShopItemData data = gridPool[Random.Range(0, gridPool.Count)];
        GameObject prefabToUse = (data.itemType == ShopItemData.ItemType.Grid) ? gridItemPrefab : shopItemPrefab;
        if (prefabToUse == null) { Debug.LogWarning("[Shop] Thieu prefab: " + data.itemType); return; }

        var go = Instantiate(prefabToUse, componentContainer);
        go.SetActive(true);

        var gridUI = go.GetComponent<GridShopItemUI>();
        if (gridUI != null)
            gridUI.Setup(data, _gridManager, trashZone, trashImage);
        else
            go.GetComponent<ShopItemUI>()?.Setup(data);

        _spawnedItems.Add(go);

        var rt = componentContainer.GetComponent<RectTransform>();
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    public void SpawnItemOfType(ShopItemData.ItemType type)
    {
        SyncSpawnedList();
        if (_spawnedItems.Count >= maxSlots) return;
        SpawnFromPool(GetPool(type));
    }

    private List<ShopItemData> GetPool(ShopItemData.ItemType type)
    {
        if (type == ShopItemData.ItemType.Grid)     return gridItems;
        if (type == ShopItemData.ItemType.Gear)     return gearItems;
        if (type == ShopItemData.ItemType.UnitDuck) return unitDuckItems;
        return _allPool;
    }

    // ── Helpers ──────────────────────────────────────────────
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
    public int  PlayerGold                   => _playerGold;
    public void AddGold(int amount)          { _playerGold += amount; RefreshUI(); }

    private void RefreshUI()
    {
        if (priceText != null) priceText.text = buyPrice.ToString();
        if (btnBuy    != null) btnBuy.interactable = (_playerGold >= buyPrice);
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Buy")]         void EditorBuy()        => OnBuyPressed();
    [ContextMenu("Test: Spawn Grid")]  void EditorGrid()       => SpawnItemOfType(ShopItemData.ItemType.Grid);
    [ContextMenu("Test: Spawn Gear")]  void EditorGear()       => SpawnItemOfType(ShopItemData.ItemType.Gear);
    [ContextMenu("Test: Spawn Duck")]  void EditorDuck()       => SpawnItemOfType(ShopItemData.ItemType.UnitDuck);
    [ContextMenu("Test: Clear")]
    void EditorClear()
    {
        SyncSpawnedList();
        foreach (var g in new List<GameObject>(_spawnedItems)) { _spawnedItems.Remove(g); DestroyImmediate(g); }
    }
#endif
}
