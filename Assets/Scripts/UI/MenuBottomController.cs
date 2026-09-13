using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý MenuBottom: điều phối 5 BottomButtonController.
/// Kết nối với MenuPanelController để chuyển panel theo button được chọn.
/// Được gọi và cấu hình bởi UIGameManager.
///
/// AUTO-SCALE THEO CHIỀU RỘNG:
///   MenuBottom (RectTransform) stretch theo chiều ngang màn hình (anchorMin.x=0,
///   anchorMax.x=1) nên chiều rộng thật thay đổi theo từng thiết bị/tỉ lệ màn hình
///   (điện thoại, PC, các aspect ratio khác nhau) — trong khi 5 Button con lại có
///   width/X cố định (set tay trong Inspector), nên chỉ đúng khít với ĐÚNG 1 tỉ lệ màn
///   hình lúc thiết kế. ScaleButtonsToFitWidth() lấy MenuBottom.rect.width chia đều cho
///   5 rồi set lại width + vị trí X của từng Button để luôn phủ kín, chia đều MenuBottom
///   trên MỌI kích thước màn hình.
/// </summary>
public class MenuBottomController : MonoBehaviour
{
    [Header("Buttons (theo thứ tự: Shop=0, Car=1, Map=2, Gear=3, Talent=4)")]
    public BottomButtonController[] buttons;

    [Header("Panel Controller")]
    public MenuPanelController panelController;

    [Header("Auto-Scale theo chiều rộng MenuBottom")]
    [Tooltip("Tự động chia đều chiều rộng MenuBottom cho 5 Button (set lại width + vị trí X) " +
             "mỗi khi Initialize() và mỗi khi MenuBottom đổi kích thước (xoay màn hình/đổi tỉ lệ). " +
             "Tắt nếu muốn giữ nguyên width/X đã set tay trong Inspector.")]
    public bool autoScaleButtonsToFitWidth = true;

    // Index đang active
    private int _activeIndex = -1;

    // Duration của animation (được set từ UIGameManager)
    private float _animDuration = 1f;

    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void Initialize(float animDuration, int defaultIndex = 0)
    {
        _animDuration = animDuration;

        // Chia đều chiều rộng MenuBottom cho 5 Button TRƯỚC khi gán listener/trạng thái,
        // để _btnDefaultPos trong BottomButtonController được refresh theo đúng vị trí mới
        // ngay từ đầu (xem RefreshDefaultPosition()).
        ScaleButtonsToFitWidth();

        // Gán index và click listener cho từng button
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].Index = i;

            int capturedIndex = i; // capture cho lambda
            var btn = buttons[i].GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButtonClicked(capturedIndex));
            }
        }

        // Set trạng thái ban đầu (không animation)
        _activeIndex = defaultIndex;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].SetStateImmediate(i == defaultIndex);
        }

        // Hiển thị panel mặc định
        if (panelController != null)
            panelController.ShowPanelImmediate(defaultIndex);
    }

    public void OnButtonClicked(int index)
    {
        if (index == _activeIndex) return;

        int prevIndex = _activeIndex;
        _activeIndex  = index;

        // Animate buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].SetState(i == index, _animDuration);
        }

        // Slide panel
        if (panelController != null)
            panelController.ShowPanel(index, _animDuration);
    }

    // ─── Auto-Scale ─────────────────────────────────────────────

    /// <summary>
    /// Lấy chiều rộng thật của MenuBottom (RectTransform.rect.width — đã tính theo anchor
    /// stretch full màn hình) chia đều cho số lượng Button, rồi:
    ///   - Set sizeDelta.x của mỗi Button = slotWidth (giữ nguyên height).
    ///   - Set anchoredPosition.x sao cho các Button nối tiếp nhau, phủ kín, chia đều từ
    ///     mép trái sang mép phải MenuBottom (giả định MenuBottom pivot.x = 0.5 — đúng với
    ///     setup hiện tại — nên tâm slot thứ i = -totalWidth/2 + slotWidth*(i + 0.5)).
    ///   - Gọi buttons[i].RefreshDefaultPosition() để BottomButtonController cache lại đúng
    ///     X mới — nếu không, animation Slide Y (SetState) lần sau sẽ tự đưa X về giá trị cũ.
    /// Bỏ qua nếu totalWidth &lt;= 0 (RectTransform chưa có kích thước hợp lệ, VD gọi quá sớm).
    /// </summary>
    public void ScaleButtonsToFitWidth()
    {
        if (!autoScaleButtonsToFitWidth) return;
        if (buttons == null || buttons.Length == 0) return;

        if (_rt == null) _rt = GetComponent<RectTransform>();
        float totalWidth = _rt.rect.width;
        if (totalWidth <= 0f) return;

        int count = buttons.Length;
        float slotWidth = totalWidth / count;

        for (int i = 0; i < count; i++)
        {
            var b = buttons[i];
            if (b == null) continue;

            var brt = b.GetComponent<RectTransform>();
            if (brt == null) continue;

            // Chỉ đổi width (x), giữ nguyên height (y) đã set trong Inspector.
            brt.sizeDelta = new Vector2(slotWidth, brt.sizeDelta.y);

            // Tâm của slot thứ i (i=0 sát mép trái nhất) trong hệ toạ độ tâm MenuBottom.
            float centerX = -totalWidth * 0.5f + slotWidth * (i + 0.5f);
            brt.anchoredPosition = new Vector2(centerX, brt.anchoredPosition.y);

            b.RefreshDefaultPosition();
        }
    }

    /// <summary>
    /// Tự re-scale khi MenuBottom đổi kích thước lúc runtime (VD xoay ngang/dọc màn hình,
    /// hoặc Canvas Scaler tính lại theo tỉ lệ mới) — để 5 Button luôn khớp chiều rộng mới.
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        // Chỉ chạy khi đã Initialize xong (tránh gọi lúc prefab chưa active/buttons null)
        if (!isActiveAndEnabled || buttons == null || buttons.Length == 0) return;
        ScaleButtonsToFitWidth();
    }
}
