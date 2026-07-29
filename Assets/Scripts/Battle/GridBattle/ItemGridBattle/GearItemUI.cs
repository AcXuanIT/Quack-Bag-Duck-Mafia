using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI của một Gear ShopItem trong Shop.
/// Không tự giữ data riêng — toàn bộ icon/tên/rarity/stats được lấy từ
/// WeaponEntry (qua ShopItemData.GetWeaponEntry()), shop chỉ cần biết weaponID.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GearItemUI : MonoBehaviour, IShopItem,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // ─── Inspector ───────────────────────────────────────────
    [Header("Base UI")]
    [SerializeField] public Image           bgImage;
    [SerializeField] public Image           iconImage;
    [SerializeField] public Image           frameImage;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI levelText;

    [Header("Rarity Frames")]
    [SerializeField] private Sprite[] rarityFrames;   // common, rare, epic, legendary, mythic

    [Header("Trash Zone")]
    [SerializeField] private RectTransform trashZone;
    [SerializeField] private Image         trashImage;
    [SerializeField] private Color         colorTrash = new Color(1f, 0.3f, 0.3f, 0.9f);
    private Color _trashOriginalColor;
    private bool  _overTrash;

    // ─── Runtime ─────────────────────────────────────────────
    [HideInInspector] public ShopItemData data;
    private WeaponEntry _weapon;

    private CanvasGroup   _canvasGroup;
    private Canvas        _rootCanvas;
    private RectTransform _rt;
    private Transform     _originalParent;
    private int           _originalSiblingIndex;
    private Vector2       _originalAnchoredPos;
    private bool          _isDragging;

    // ─── IShopItem ───────────────────────────────────────────
    public ShopItemData ShopData    => data;
    public string       DisplayName => _weapon != null ? _weapon.Name : (data != null ? data.itemName : string.Empty);
    public Sprite       DisplayIcon => _weapon != null ? _weapon.GetUIIcon() : null;
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

        _weapon = data.GetWeaponEntry();

        Sprite icon = _weapon != null ? _weapon.GetUIIcon() : null;
        string name = _weapon != null ? _weapon.Name : data.itemName;
        int    lvl  = _weapon != null ? _weapon.Level : data.level;

        if (iconImage  != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (nameText   != null) nameText.text  = name;
        if (levelText  != null) levelText.text = "Lv " + lvl;
        if (frameImage != null && rarityFrames != null && data.rarity < rarityFrames.Length)
            frameImage.sprite = rarityFrames[data.rarity];
    }

    public void Discard() => Destroy(gameObject);

    // ─── Click ───────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        Debug.Log("[GearItemUI] Clicked: " + DisplayName);
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
            Debug.Log("[GearItemUI] Discarded '" + DisplayName + "' vao trash.");
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
