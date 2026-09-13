using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Shop trong màn Battle.
/// - Grid  : vẫn dùng ShopItemData (List gridItems) — vì shape của Grid item không tồn tại ở đâu khác.
/// - Gear  : KHÔNG còn ItemPool ShopItemData nữa — random trực tiếp 1 WeaponEntry từ
///           DataManager.Instance.WeaponDatabase.GetEntries() (nguồn WeaponData duy nhất,
///           dereference qua từng WeaponDataAsset).
/// - UnitDuck : KHÔNG còn ItemPool ShopItemData nữa — random trực tiếp 1 MyDuckData từ
///           DataManager.Instance.AllMyDuckAssets (nguồn Assets/Data/MyDuck duy nhất).
///
/// - Component GO chứa tối đa 4 item
/// - btnBuy spawn 3 item ngẫu nhiên theo trọng số (cần ít nhất 3 slot trống)
/// - Tự động refresh Shop mỗi khi 1 Turn mới bắt đầu (BattleManager.OnTurnSetupStart)
/// - GridItem spawn ra LUÔN được ràng buộc theo GridSystem: chỉ random trong số
///   những item có ÍT NHẤT 1 vị trí đặt hợp lệ thật sự trên bàn cờ hiện tại
///   (BattleGridManager.HasValidPlacement).
/// - Tỷ lệ spawn (cả RefreshShop() lẫn OnBuyPressed()): GridItem 20% / GearItem 40% / UnitItem 40%
///   (xem GetRandomItemKindWeighted()).
///
/// Player Money: KHÔNG còn giữ biến tiền riêng (_playerGold đã bị xoá) — nguồn dữ liệu tiền
/// DUY NHẤT là BattleManager.PlayerMoney. Mua item (OnBuyPressed()) gọi thẳng
/// battleManager.SpendMoney(buyPrice); BattleManager tự đẩy cập nhật lại RefreshUI() ở đây
/// mỗi khi tiền thay đổi (xem BattleManager.NotifyMoneyChanged()).
/// </summary>
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
    [Tooltip("Lắng nghe OnTurnSetupStart để tự động refresh Shop mỗi khi turn mới bắt đầu. " +
             "Đồng thời là nguồn dữ liệu DUY NHẤT cho tiền của Player (PlayerMoney/SpendMoney/AddMoney) " +
             "— ShopBatteManager không còn giữ biến tiền riêng.")]
    [SerializeField] private BattleManager battleManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject gridItemPrefab;   // GridItem.prefab — dùng cho ShopItemData (GridShopItemUI)
    [SerializeField] private GameObject gearItemPrefab;   // GearItem.prefab — dùng cho WeaponEntry   (GearItemUI)
    [SerializeField] private GameObject unitItemPrefab;   // UnitItem.prefab — dùng cho MyDuckData    (UnitPlayerItemUI)

    [Header("Spawn Config")]
    [SerializeField] private int defaultSpawnCount = 3;
    [SerializeField] private int maxSlots          = 4;

    [Header("Spawn Weights (RefreshShop + OnBuyPressed) — tổng không bắt buộc = 100, sẽ tự chuẩn hoá")]
    [SerializeField] private float gridSpawnWeight = 20f; // GridItem  20%
    [SerializeField] private float gearSpawnWeight = 40f; // GearItem  40%
    [SerializeField] private float unitSpawnWeight = 40f; // UnitItem  40%

    [Header("Item Pool — Grid (vẫn dùng ShopItemData)")]
    [SerializeField] private List<ShopItemData> gridItems = new List<ShopItemData>();

    private List<GameObject>  _spawnedItems = new List<GameObject>();
    private BattleGridManager _gridManager;

    void Start()
    {
        // Dùng Start() thay vì Awake() để đảm bảo BattleGridManager.Awake()
        // (nơi gọi BuildGrid()) đã chạy xong — Unity luôn chạy hết toàn bộ
        // Awake() của mọi component trước khi bắt đầu gọi Start() bất kỳ,
        // nên không cần phụ thuộc vào Script Execution Order thủ công.
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

    // ── Turn Sync ────────────────────────────────────────────

    /// <summary>Gọi tự động khi BattleManager bắt đầu 1 Turn Setup mới.</summary>
    private void HandleTurnSetupStart(int turnIndex)
    {
        RefreshShop();
    }

    /// <summary>
    /// Xoá toàn bộ item đang hiển thị trong Shop và spawn lại defaultSpawnCount
    /// item mới (random có trọng số: Grid 20% / Gear 40% / UnitDuck 40%). Gọi mỗi khi turn mới bắt đầu,
    /// hoặc có thể gọi thủ công (VD nút Reroll) nếu cần sau này.
    /// </summary>
    public void RefreshShop()
    {
        ClearAllItems();
        for (int i = 0; i < defaultSpawnCount; i++)
        {
            var type = GetRandomItemKindWeighted();
            SpawnItemOfType(type);
        }
        Debug.Log($"[Shop] RefreshShop: spawned {defaultSpawnCount} item moi cho turn.");
    }

    /// <summary>
    /// Random 1 ItemKind theo trọng số: GridItem 20% / GearItem 40% / UnitItem 40%
    /// (lấy từ gridSpawnWeight / gearSpawnWeight / unitSpawnWeight, tự chuẩn hoá theo tổng).
    /// </summary>
    private ItemKind GetRandomItemKindWeighted()
    {
        float total = gridSpawnWeight + gearSpawnWeight + unitSpawnWeight;
        if (total <= 0f) return (ItemKind)Random.Range(0, 3); // fallback nếu cấu hình sai

        float roll = Random.Range(0f, total);

        if (roll < gridSpawnWeight) return ItemKind.Grid;
        roll -= gridSpawnWeight;

        if (roll < gearSpawnWeight) return ItemKind.Gear;

        return ItemKind.UnitDuck;
    }

    /// <summary>Xoá toàn bộ item đang có trong componentContainer.</summary>
    private void ClearAllItems()
    {
        SyncSpawnedList();
        foreach (var g in new List<GameObject>(_spawnedItems))
            Destroy(g);
        _spawnedItems.Clear();
    }

    /// <summary>Loại item trong Shop (thay cho ShopItemData.ItemType cũ — Gear/UnitDuck không còn ShopItemData nữa).</summary>
    public enum ItemKind { Grid, Gear, UnitDuck }

    // ── Buy ──────────────────────────────────────────────────

    /// <summary>
    /// Nhấn Buy: spawn 3 item random theo trọng số Grid 20% / Gear 40% / UnitDuck 40%
    /// (cần ít nhất 3 slot trống). Trừ tiền qua BattleManager.SpendMoney() — nguồn dữ liệu
    /// tiền DUY NHẤT của trận đấu.
    /// </summary>
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

        // Buy: spawn 3 item random theo trọng số (Grid 20% / Gear 40% / Unit 40%)
        for (int i = 0; i < 3; i++)
            SpawnItemOfType(GetRandomItemKindWeighted());

        Debug.Log($"[Shop] Buy: spawned 3 item (weighted). Money={battleManager.PlayerMoney}. Slots={_spawnedItems.Count}/{maxSlots}");
    }

    // ── Spawn ────────────────────────────────────────────────

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

    /// <summary>Spawn 1 GridItem — vẫn dùng ShopItemData, lọc theo GridSystem (HasValidPlacement).</summary>
    private void SpawnGridItem()
    {
        if (gridItemPrefab == null) { Debug.LogWarning("[Shop] Thieu gridItemPrefab!"); return; }
        if (componentContainer == null) { Debug.LogWarning("[Shop] Thieu componentContainer!"); return; }

        var eligible = GetEligibleGridItems();
        if (eligible.Count == 0)
        {
            Debug.LogWarning("[Shop] Khong co GridItem nao dat duoc tren ban co hien tai — bo qua lan spawn nay.");
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

    /// <summary>Spawn 1 GearItem — random trực tiếp 1 WeaponEntry từ DataManager (WeaponData),
    /// dereference qua WeaponData.GetEntries() (mỗi entry lấy từ 1 WeaponDataAsset riêng).</summary>
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

    /// <summary>Spawn 1 UnitItem — random trực tiếp 1 MyDuckData từ DataManager (Assets/Data/MyDuck).</summary>
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

    /// <summary>Tiền hiện tại của Player — đọc thẳng từ BattleManager (nguồn dữ liệu DUY NHẤT).</summary>
    public int PlayerGold => battleManager != null ? battleManager.PlayerMoney : 0;

    /// <summary>Cộng thêm tiền cho Player — forward thẳng qua BattleManager.AddMoney() (tự đẩy RefreshUI()).</summary>
    public void AddGold(int amount)
    {
        if (battleManager != null) battleManager.AddMoney(amount);
    }

    /// <summary>Cập nhật giá tiền hiển thị và trạng thái nút Buy — gọi từ BattleManager mỗi khi
    /// PlayerMoney thay đổi (NotifyMoneyChanged()), và tự gọi khi Start().</summary>
    public void RefreshUI()
    {
        int currentMoney = battleManager != null ? battleManager.PlayerMoney : 0;
        if (priceText != null) priceText.text = buyPrice.ToString();
        if (btnBuy    != null) btnBuy.interactable = (currentMoney >= buyPrice);
    }

    /// <summary>
    /// Lọc gridItems chỉ giữ lại những item THỰC SỰ đặt được trên bàn cờ hiện tại:
    /// dùng BattleGridManager.HasValidPlacement() để quét toàn bộ vị trí anchor
    /// khả dĩ, kiểm tra đúng rule CanUnlock() (toàn bộ ô Locked + kề ô đã Unlocked) —
    /// KHÔNG chỉ đếm số ô trống, vì đếm số lượng không đảm bảo các ô Locked còn lại
    /// có nằm liền khối đúng hình dạng shape hay không.
    /// </summary>
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
