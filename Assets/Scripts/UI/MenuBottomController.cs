using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý MenuBottom: điều phối 5 BottomButtonController.
/// Kết nối với MenuPanelController để chuyển panel theo button được chọn.
/// Được gọi và cấu hình bởi UIGameManager.
/// </summary>
public class MenuBottomController : MonoBehaviour
{
    [Header("Buttons (theo thứ tự: Shop=0, Car=1, Map=2, Gear=3, Talent=4)")]
    public BottomButtonController[] buttons;

    [Header("Panel Controller")]
    public MenuPanelController panelController;

    // Index đang active
    private int _activeIndex = -1;

    // Duration của animation (được set từ UIGameManager)
    private float _animDuration = 1f;

    public void Initialize(float animDuration, int defaultIndex = 0)
    {
        _animDuration = animDuration;

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
}
