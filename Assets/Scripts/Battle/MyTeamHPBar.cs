using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HP Bar cho GameObject "MyTeam" (Canvas chứa 2 Image BG/Fill + 1 TextHP).
///
/// KHÔNG dùng Image.Type.Filled (dễ làm méo/kéo giãn fill với 1 số loại sprite).
/// Thay vào đó dùng kỹ thuật: co giãn RectTransform.sizeDelta.x của fillImage
/// (pivot/anchor cố định bên TRÁI) kết hợp Image.Type.Sliced (9-slice) —
/// phần viền/bo góc 2 đầu thanh KHÔNG bị kéo giãn biến dạng, chỉ phần giữa co giãn.
/// (Yêu cầu sprite của fillImage đã set Border trong Sprite Editor để 9-slice hoạt động đúng).
///
/// - Pivot/Anchor = Left khiến fill luôn cố định mép TRÁI; khi HP giảm, sizeDelta.x
///   giảm theo tỉ lệ → mép PHẢI lùi dần vào trong → đúng hiệu ứng "giảm từ phải sang trái".
/// - TextHP rút gọn số lớn: > 999 → dạng "1k" (nghìn), >= 1,000,000 → dạng "1m" (triệu).
///
/// LƯU Ý QUAN TRỌNG (fix lỗi fill bị lệch khi Start):
///   Đổi anchorMin/anchorMax/pivot bằng CODE (khác với kéo trong Editor) KHÔNG tự
///   bù trừ anchoredPosition/sizeDelta để giữ nguyên vị trí hiển thị — rect sẽ
///   "nhảy" lệch ngay khi đổi, nếu fillImage ban đầu không set sẵn anchor Left
///   trong Editor (VD: fillImage đang Stretch để khớp BgImage). Vì vậy TRƯỚC khi
///   đổi anchor/pivot, ta phải lưu lại mép trái + chiều rộng thật (theo local
///   space của parent), rồi bù lại anchoredPosition.x/sizeDelta.x SAU khi đổi để
///   rect giữ nguyên đúng vị trí/kích thước ban đầu (chỉ khác là giờ "neo" bên
///   trái để co giãn đúng hướng).
/// </summary>
public class MyTeamHPBar : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI textHP;

    private RectTransform _fillRT;
    private float _maxWidth; // Chiều rộng fillImage ứng với 100% HP — lấy từ kích thước đặt sẵn trong Editor

    private float _baseHP;
    private float _currentHP;

    public float BaseHP    => _baseHP;
    public float CurrentHP => _currentHP;

    private void Awake()
    {
        if (fillImage != null)
        {
            _fillRT = fillImage.rectTransform;

            // ── 1) Ghi lại vị trí & kích thước GỐC (theo local space của parent)
            //       TRƯỚC KHI đổi anchor/pivot, để có thể bù lại chính xác ngay sau đó.
            var parentRT     = _fillRT.parent as RectTransform;
            float parentWidth = parentRT != null ? parentRT.rect.width : 0f;

            float oldAnchorMinX = _fillRT.anchorMin.x;
            float oldOffsetMinX = _fillRT.offsetMin.x;
            float oldWidth      = _fillRT.rect.width; // chiều rộng THẬT đang hiển thị (đã tính cả anchor span + sizeDelta)

            // Mép trái thật của fillImage, quy về 1 hệ toạ độ cố định của parent
            // (anchorMin.x * parentWidth + offsetMin.x luôn cho ra cùng 1 điểm vật lý,
            // bất kể anchor đang là gì) — dùng làm mốc để bù lại sau khi đổi anchor.
            float trueLeftEdgeX = oldAnchorMinX * parentWidth + oldOffsetMinX;

            // ── 2) Ép anchor/pivot về bên TRÁI để fill luôn co giãn từ mép trái cố định
            // (mép phải là phần di chuyển khi HP thay đổi).
            _fillRT.anchorMin = new Vector2(0f, _fillRT.anchorMin.y);
            _fillRT.anchorMax = new Vector2(0f, _fillRT.anchorMax.y);
            _fillRT.pivot     = new Vector2(0f, _fillRT.pivot.y);

            // Dùng Sliced (9-slice) thay vì Simple/Filled: khi đổi sizeDelta.x,
            // phần viền/bo tròn 2 đầu KHÔNG bị kéo giãn biến dạng — chỉ phần giữa co giãn.
            fillImage.type = Image.Type.Sliced;

            // ── 3) Bù lại vị trí & kích thước để fillImage giữ ĐÚNG vị trí/kích thước
            //       ban đầu (không còn bị lệch so với BgImage), CHỈ khác là giờ nó
            //       neo (anchor+pivot) ở mép trái để co giãn đúng hướng.
            //       (anchorMin.x mới = 0 => anchoredPosition.x = trueLeftEdgeX, vì
            //       offsetMin.x = anchoredPosition.x khi pivot.x = 0)
            _fillRT.sizeDelta        = new Vector2(oldWidth, _fillRT.sizeDelta.y);
            _fillRT.anchoredPosition = new Vector2(trueLeftEdgeX, _fillRT.anchoredPosition.y);

            // Lưu lại chiều rộng gốc (đặt sẵn trong Editor) làm mốc 100% HP.
            _maxWidth = oldWidth;
        }
    }

    /// <summary>Khởi tạo HP Bar với HP gốc (Max HP). Gọi 1 lần khi spawn/setup team.</summary>
    public void Init(float baseHP)
    {
        _baseHP    = Mathf.Max(0f, baseHP);
        _currentHP = _baseHP;
        Refresh();
    }

    /// <summary>
    /// Cập nhật lại thanh HP Bar theo HP hiện có — dùng chung cho cả nhận damage
    /// (currentHP giảm) lẫn hồi HP (currentHP tăng). Tự clamp trong khoảng [0, baseHP].
    /// </summary>
    public void UpdateHP(float currentHP)
    {
        _currentHP = Mathf.Clamp(currentHP, 0f, _baseHP);
        Refresh();
    }

    private void Refresh()
    {
        float ratio = _baseHP > 0f ? _currentHP / _baseHP : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (_fillRT != null)
        {
            var size = _fillRT.sizeDelta;
            size.x = _maxWidth * ratio;
            _fillRT.sizeDelta = size;
        }

        if (textHP != null)
            textHP.text = FormatHP(_currentHP);
    }

    /// <summary>
    /// Rút gọn số HP hiển thị:
    /// - >= 1,000,000 → "Xm" (triệu)
    /// - > 999        → "Xk" (nghìn)
    /// - còn lại      → số nguyên bình thường
    /// </summary>
    private string FormatHP(float hp)
    {
        if (hp >= 1000000f)
            return (hp / 1000000f).ToString("0.#") + "m";

        if (hp > 999f)
            return (hp / 1000f).ToString("0.#") + "k";

        return Mathf.RoundToInt(hp).ToString();
    }
}
