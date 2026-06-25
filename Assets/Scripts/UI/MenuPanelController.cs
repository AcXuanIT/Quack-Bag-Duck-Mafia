using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Quản lý hiệu ứng chuyển panel trong MenuMid.
/// Panel mới slide vào từ trái hoặc phải tùy vào index của button.
/// Panel cũ slide ra theo chiều ngược lại.
/// </summary>
public class MenuPanelController : MonoBehaviour
{
    [Header("Panels (theo thứ tự index button: Shop=0, Car=1, Map=2, Gear=3, Talent=4)")]
    public RectTransform[] panels;

    // Index panel đang hiển thị
    private int _currentIndex = -1;

    // Width của mỗi panel (dùng để tính offset slide)
    private float _panelWidth;

    private Coroutine _slideCoroutine;

    private void Awake()
    {
        if (panels != null && panels.Length > 0)
            _panelWidth = panels[0].sizeDelta.x;
    }

    /// <summary>
    /// Hiển thị panel tại index mới với hiệu ứng slide.
    /// Nếu newIndex > currentIndex → slide từ phải vào.
    /// Nếu newIndex < currentIndex → slide từ trái vào.
    /// </summary>
    public void ShowPanel(int newIndex, float duration)
    {
        if (newIndex == _currentIndex) return;

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideRoutine(_currentIndex, newIndex, duration));

        _currentIndex = newIndex;
    }

    /// <summary>Hiển thị panel mặc định không có animation</summary>
    public void ShowPanelImmediate(int index)
    {
        _currentIndex = index;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            panels[i].gameObject.SetActive(i == index);
            panels[i].anchoredPosition = Vector2.zero;
        }
    }

    private IEnumerator SlideRoutine(int oldIndex, int newIndex, float duration)
    {
        // Xác định hướng: newIndex lớn hơn → panel mới đến từ phải (+x)
        float dir = (newIndex > oldIndex) ? 1f : -1f;

        RectTransform outPanel = (oldIndex >= 0 && oldIndex < panels.Length) ? panels[oldIndex] : null;
        RectTransform inPanel  = (newIndex >= 0 && newIndex < panels.Length) ? panels[newIndex]  : null;

        // Setup vị trí bắt đầu
        if (inPanel != null)
        {
            inPanel.gameObject.SetActive(true);
            inPanel.anchoredPosition = new Vector2(_panelWidth * dir, 0f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));

            if (inPanel != null)
                inPanel.anchoredPosition = new Vector2(Mathf.Lerp(_panelWidth * dir, 0f, t), 0f);

            if (outPanel != null)
                outPanel.anchoredPosition = new Vector2(Mathf.Lerp(0f, -_panelWidth * dir, t), 0f);

            yield return null;
        }

        // Snap về vị trí cuối
        if (inPanel != null)  inPanel.anchoredPosition  = Vector2.zero;
        if (outPanel != null)
        {
            outPanel.anchoredPosition = Vector2.zero;
            outPanel.gameObject.SetActive(false);
        }
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
