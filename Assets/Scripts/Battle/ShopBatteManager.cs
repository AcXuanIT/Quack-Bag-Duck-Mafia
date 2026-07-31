using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Shop trong màn Battle.
/// - 3 loại item spawn: Grid, Gear, UnitDuck
/// - Component GO chứa tối đa 4 item
/// - btnBuy spawn 2 hoặc 3 item; nếu Component đang có đúng 2 item thì spawn 2
/// - Tự động refresh Shop mỗi khi 1 Turn mới bắt đầu (BattleManager.OnTurnSetupStart)
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

    [Header("Battle Manager")]
    [Tooltip("Lắng nghe OnTurnSetupStart để tự động refresh Shop mỗi khi turn mới bắt đầu")]
    [SerializeField] private BattleManager battleManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject gridItemPrefab;   // GridItem.prefab — dùng cho ItemType.Grid
        [SerializeField] private GameObject gearItemPrefab;   // GearItem.prefab — dùng cho ItemType.Gear (GearItemUI)
        [SerializeField] private GameObject unitItemPrefab;   // UnitItem.prefab — dùng cho ItemType.UnitDuck (UnitPlayerItemUI)

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

    void Start()
    {
        // Dùng Start() thay vì Awake() để đảm bảo BattleGridManager.Awake()
        // (nơi gọi BuildGrid()) đã chạy xong — Unity luôn chạy hết toàn bộ
        // Awake() của mọi component trước khi bắt đầu gọi Start() bất kỳ,
        // nên không cần phụ thuộc vào Script Execution Order thủ công.
        _gridManager = FindObjectOfType<BattleGridManager>();
        RebuildPool();
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

    // ── Turn Sync ────────────────────────────────────────────

    /// <summary>Gọi tự động khi BattleManager bắt đầu 1 Turn Setup mới.</summary>
    private void HandleTurnSetupStart(int turnIndex)
    {
        RefreshShop();
    }

    /// <summary>
    /// Xoá toàn bộ item đang hiển thị trong Shop và spawn lại defaultSpawnCount
    /// item mới (random đều 3 loại Grid/Gear/UnitDuck). Gọi mỗi khi turn mới bắt đầu,
    /// hoặc có thể gọi thủ công (VD nút Reroll) nếu cần sau này.
    /// </summary>
    public void RefreshShop()
    {
        ClearAllItems();
        for (int i = 0; i < defaultSpawnCount; i++)
        {
            var type = (ShopItemData.ItemType)Random.Range(0, System.Enum.GetValues(typeof(ShopItemData.ItemType)).Length);
            SpawnItemOfType(type);
        }
        Debug.Log($"[Shop] RefreshShop: spawned {defaultSpawnCount} item moi cho turn.");
    }

    /// <summary>Xoá toàn bộ item đang có trong componentContainer.</summary>
    private void ClearAllItems()
    {
        SyncSpawnedList();
        foreach (var g in new List<GameObject>(_spawnedItems))
            Destroy(g);
        _spawnedItems.Clear();
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
        int freeSlots = maxSlots - _spawnedItems.Count;

        if (freeSlots < 2) { Debug.Log("[Shop] Không đủ slot (cần ít nhất 2)."); return; }
        if (_playerGold < buyPrice) { Debug.Log($"[Shop] Thiếu vàng ({_playerGold}/{buyPrice})."); return; }

        _playerGold -= buyPrice;
        RefreshUI();

        // TEST: luôn spawn 1 UnitItem + 1 GridItem
        SpawnItemOfType(ShopItemData.ItemType.UnitDuck);
        SpawnItemOfType(ShopItemData.ItemType.Grid);

        Debug.Log($"[Shop] Spawn 1 Unit + 1 Grid. Gold={_playerGold}. Slots={_spawnedItems.Count}/{maxSlots}");
    }

    // ── Spawn ────────────────────────────────────────────────
private void SpawnFromPool(List<ShopItemData> pool)
        {
            if (pool == null || pool.Count == 0) { Debug.LogWarning("[Shop] Pool rong!"); return; }
            if (componentContainer == null)      { Debug.LogWarning("[Shop] Thieu componentContainer!"); return; }

            System.Collections.Generic.List<ShopItemData> activePool = (pool == _allPool || pool == gridItems) ? GetEligibleGridItems() : pool;
            if (activePool == null || activePool.Count == 0) { Debug.LogWarning("[Shop] Pool rong sau filter!"); return; }
            ShopItemData data = activePool[Random.Range(0, activePool.Count)];

            GameObject prefabToUse = data.itemType switch
            {
                ShopItemData.ItemType.Grid     => gridItemPrefab,
                ShopItemData.ItemType.Gear     => gearItemPrefab,
                ShopItemData.ItemType.UnitDuck => unitItemPrefab,
                _                               => null,
            };
            if (prefabToUse == null) { Debug.LogWarning("[Shop] Thieu prefab: " + data.itemType); return; }

            var go = Instantiate(prefabToUse, componentContainer);
            go.SetActive(true);

            var shopItem = go.GetComponent<IShopItem>();
            if (shopItem != null)
                shopItem.Setup(data, _gridManager, trashZone, trashImage);
            else
                Debug.LogWarning("[Shop] Prefab cho " + data.itemType + " thieu component IShopItem!");

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

    private System.Collections.Generic.List<ShopItemData> GetEligibleGridItems()
    {
        int free = _gridManager != null ? _gridManager.CountUnlockedEmpty() : int.MaxValue;
        var ok = new System.Collections.Generic.List<ShopItemData>();
        int minSz = int.MaxValue; ShopItemData smallest = null;
        foreach (var it in gridItems)
        {
            if (it == null) continue;
            int sz = (it.gridCells != null && it.gridCells.Length > 0) ? it.gridCells.Length : 1;
            if (sz <= free) ok.Add(it);
            if (sz < minSz) { minSz = sz; smallest = it; }
        }
        if (ok.Count == 0 && smallest != null)
        {
            Debug.LogWarning("[Shop] Khong co GridItem nao vua (" + free + " o trong). Fallback: " + smallest.itemName);
            ok.Add(smallest);
        }
        return ok;
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
    [ContextMenu("Test: Refresh Shop (Turn Setup)")]
    void EditorRefreshShop() => RefreshShop();
#endif
}
