using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI của một UnitDuck ShopItem trong Shop.
/// Không tự giữ data riêng — toàn bộ icon/tên/rarity/stats được lấy từ
/// MyDuckData (qua ShopItemData.GetUnitDuckData()).
/// BG đổi màu theo tier (0=Default, 1=Blue, 2=Purple, 3=Gold) dựa trên rarity.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UnitPlayerItemUI : MonoBehaviour, IShopItem,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // ─── Inspector ───────────────────────────────────────────
    [Header("Base UI")]
    [SerializeField] public Image           bgImage;
    [SerializeField] public Image           iconImage;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI levelText;

    [Header("Tier Colors")]
    [SerializeField] private Color colorDefault = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color colorBlue    = new Color(0.25f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color colorPurple  = new Color(0.65f, 0.25f, 1.00f, 1f);
    [SerializeField] private Color colorGold    = new Color(1.00f, 0.78f, 0.10f, 1f);

    [Header("Trash Zone")]
    [SerializeField] private RectTransform trashZone;
    [SerializeField] private Image         trashImage;
    [SerializeField] private Color         colorTrash = new Color(1f, 0.3f, 0.3f, 0.9f);
    private Color _trashOriginalColor;
    private bool  _overTrash;

    // ─── Runtime ─────────────────────────────────────────────
    [HideInInspector] public ShopItemData data;
    private MyDuckData _unit;

    private CanvasGroup   _canvasGroup;
    private Canvas        _rootCanvas;
    private RectTransform _rt;
    private Transform     _originalParent;
    private int           _originalSiblingIndex;
    private Vector2       _originalAnchoredPos;
    private bool          _isDragging;

    // ─── IShopItem ───────────────────────────────────────────
    public ShopItemData ShopData    => data;
    public string       DisplayName => _unit != null ? _unit.Name : (data != null ? data.itemName : string.Empty);
    public Sprite       DisplayIcon => _unit != null ? _unit.GetDefaultIcon() : null;
    public int          Rarity      => data != null ? data.rarity : 0;
    public int          SellPrice   => data != null ? data.sellPrice : 0;

    // ─── Init ────────────────────────────────────────────────
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rt          = GetComponent<RectTransform>();
        _rootCanvas  = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.rootCanvas;
    }

    public void Setup(ShopItemData itemData, BattleGridManager gridManager,
                       RectTransform trash = null, Image trashImg = null)
    {
        data = itemData;
        if (trash    != null) trashZone  = trash;
        if (trashImg != null) trashImage = trashImg;
        if (data == null) return;

        _unit = data.GetUnitDuckData();

        Sprite icon = _unit != null ? _unit.GetDefaultIcon() : null;
        string name = _unit != null ? _unit.Name : data.itemName;
        int tier = data.rarity + 1; // rarity 0-3 → tier 1-4
        if (iconImage != null) { iconImage.sprite = _unit != null ? _unit.GetSprite(tier) : icon; iconImage.enabled = icon != null || _unit != null; }
        if (nameText  != null) nameText.text  = name;
        if (levelText != null) levelText.text = "T" + tier;

        SetTierColor(data.rarity);
    }

    private void SetTierColor(int rarity)
    {
        if (bgImage == null) return;
        bgImage.color = rarity switch
        {
            1 => colorBlue,
            2 => colorPurple,
            3 => colorGold,
            _ => colorDefault,
        };
    }

    public void Discard() => Destroy(gameObject);

    // ─── Click ───────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        Debug.Log("[UnitPlayerItemUI] Clicked: " + DisplayName);
    }

    // ─── Drag (kéo vào trash để bỏ) ────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (trashZone == null) return;

        _isDragging           = true;
        _originalParent       = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPos  = _rt.anchoredPosition;

        transform.SetParent(_rootCanvas.transform, true);
        transform.SetAsLastSibling();

        _canvasGroup.alpha          = 0.8f;
        _canvasGroup.blocksRaycasts = false;

        if (trashImage != null) _trashOriginalColor = trashImage.color;
        _overTrash = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _rt.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;

        bool nowOverTrash = IsPointerOverTrash(eventData);
        if (nowOverTrash != _overTrash)
        {
            _overTrash = nowOverTrash;
            if (trashImage != null)
                trashImage.color = _overTrash ? colorTrash : _trashOriginalColor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (trashImage != null) trashImage.color = _trashOriginalColor;

        if (_overTrash || IsPointerOverTrash(eventData))
        {
            Debug.Log("[UnitPlayerItemUI] Discarded '" + DisplayName + "' vao trash.");
            Discard();
            return;
        }

        transform.SetParent(_originalParent, true);
        transform.SetSiblingIndex(_originalSiblingIndex);
        _rt.anchoredPosition = _originalAnchoredPos;

        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    private bool IsPointerOverTrash(PointerEventData eventData)
    {
        if (trashZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            trashZone, eventData.position, eventData.pressEventCamera);
    }
}
