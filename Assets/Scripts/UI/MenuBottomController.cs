using UnityEngine;
using UnityEngine.UI;


public class MenuBottomController : MonoBehaviour
{
    [Header("Buttons (theo thứ tự: Shop=0, Car=1, Map=2, Gear=3, Talent=4)")]
    public BottomButtonController[] buttons;

    [Header("Panel Controller")]
    public MenuPanelController panelController;

    [Header("Auto-Scale theo chiều rộng MenuBottom")]
    public bool autoScaleButtonsToFitWidth = true;

    private int _activeIndex = -1;
    private float _animDuration = 1f;
    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void Initialize(float animDuration, int defaultIndex = 0)
    {
        _animDuration = animDuration;

        ScaleButtonsToFitWidth();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].Index = i;

            int capturedIndex = i; 
            var btn = buttons[i].GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButtonClicked(capturedIndex));
            }
        }
        _activeIndex = defaultIndex;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].SetStateImmediate(i == defaultIndex);
        }

        if (panelController != null)
            panelController.ShowPanelImmediate(defaultIndex);
    }

    public void OnButtonClicked(int index)
    {
        if (index == _activeIndex) return;

        int prevIndex = _activeIndex;
        _activeIndex  = index;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].SetState(i == index, _animDuration);
        }

        // Slide panel
        if (panelController != null)
            panelController.ShowPanel(index, _animDuration);
    }

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

            brt.sizeDelta = new Vector2(slotWidth, brt.sizeDelta.y);

            float centerX = -totalWidth * 0.5f + slotWidth * (i + 0.5f);
            brt.anchoredPosition = new Vector2(centerX, brt.anchoredPosition.y);

            b.RefreshDefaultPosition();
        }
    }
    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || buttons == null || buttons.Length == 0) return;
        ScaleButtonsToFitWidth();
    }
}
