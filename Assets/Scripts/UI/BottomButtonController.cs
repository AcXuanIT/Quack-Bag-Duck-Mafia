using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gắn vào từng button trong MenuBottom.
/// Quản lý trạng thái ON/OFF với hiệu ứng di chuyển lên/xuống.
/// 
/// Trạng thái ON  : button BG image hiện, text hiện, icon ở vị trí gốc
/// Trạng thái OFF : button BG image ẩn, text ẩn, icon hơi thấp xuống
/// Hiệu ứng       : button di chuyển lên (OFF→ON) hoặc xuống (ON→OFF)
/// </summary>
public class BottomButtonController : MonoBehaviour
{
    [Header("References (auto-filled)")]
    public Image   bgImage;          // Image component trên chính button này
    public TextMeshProUGUI label;    // Text (TMP) child
    public RectTransform   iconRect; // Image child (icon)

    [Header("Icon Offset")]
    [SerializeField] private float iconOffsetY = -20f;   // icon thấp xuống bao nhiêu khi OFF

    [Header("Button Slide")]
    [SerializeField] private float slideOffsetY = -40f;  // button dịch xuống bao nhiêu khi OFF

    // Vị trí gốc của button và icon
    private Vector2 _btnDefaultPos;
    private Vector2 _iconDefaultPos;

    // Trạng thái hiện tại
    private bool _isOn = false;
    private Coroutine _animCoroutine;

    // Index trong danh sách (dùng để so sánh vị trí với button khác khi switch)
    public int Index { get; set; }

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _btnDefaultPos = _rectTransform.anchoredPosition;
        if (iconRect != null)
            _iconDefaultPos = iconRect.anchoredPosition;
    }

    /// <summary>Khởi tạo trạng thái không có animation (dùng khi setup lần đầu)</summary>
    public void SetStateImmediate(bool isOn)
    {
        _isOn = isOn;
        ApplyState(isOn ? 1f : 0f);
    }

    /// <summary>Chuyển trạng thái với animation, duration tính bằng giây</summary>
    public void SetState(bool isOn, float duration)
    {
        if (_isOn == isOn) return;
        _isOn = isOn;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateState(isOn, duration));
    }

    private IEnumerator AnimateState(bool toOn, float duration)
    {
        float elapsed = 0f;
        float startT = toOn ? 0f : 1f;
        float endT   = toOn ? 1f : 0f;

        // Snap visibility trước khi animate
        if (toOn)
        {
            if (bgImage != null) bgImage.enabled = true;
            if (label != null)   label.enabled   = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float raw = Mathf.Clamp01(elapsed / duration);
            float t   = EaseOutCubic(raw);
            ApplyState(Mathf.Lerp(startT, endT, t));
            yield return null;
        }

        ApplyState(endT);

        // Ẩn sau khi animate xong
        if (!toOn)
        {
            if (bgImage != null) bgImage.enabled = false;
            if (label != null)   label.enabled   = false;
        }
    }

    /// <summary>
    /// t = 0 : OFF state (button thấp, icon thấp, image/text ẩn)
    /// t = 1 : ON  state (button vị trí gốc, icon gốc, image/text hiện)
    /// </summary>
    private void ApplyState(float t)
    {
        // Button slide up/down
        if (_rectTransform != null)
        {
            float y = Mathf.Lerp(_btnDefaultPos.y + slideOffsetY, _btnDefaultPos.y, t);
            _rectTransform.anchoredPosition = new Vector2(_btnDefaultPos.x, y);
        }

        // Icon offset
        if (iconRect != null)
        {
            float iy = Mathf.Lerp(_iconDefaultPos.y + iconOffsetY, _iconDefaultPos.y, t);
            iconRect.anchoredPosition = new Vector2(_iconDefaultPos.x, iy);
        }

        // Alpha fade cho image và text
        float alpha = t;
        if (bgImage != null)
        {
            var c = bgImage.color;
            bgImage.color = new Color(c.r, c.g, c.b, alpha);
        }
        if (label != null)
        {
            var c = label.color;
            label.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
