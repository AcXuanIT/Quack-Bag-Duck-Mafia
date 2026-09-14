using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuPanelController : MonoBehaviour
{
    [Header("Panels (theo thứ tự index button: Shop=0, Car=1, Map=2, Gear=3, Talent=4)")]
    public RectTransform[] panels;

    [Header("=== HUD ===")]

    [SerializeField] private UIGameManager uiGameManager;

    private const int MAP_PANEL_INDEX = 2;

    // Index panel đang hiển thị
    private int _currentIndex = -1;

    private Coroutine _slideCoroutine;

    private float GetPanelWidth()
    {
        if (panels == null) return 0f;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) return panels[i].rect.width;
        }
        return 0f;
    }

    public void ShowPanel(int newIndex, float duration)
    {
        if (newIndex == _currentIndex) return;

        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideRoutine(_currentIndex, newIndex, duration));

        _currentIndex = newIndex;

        NotifyPanelLoaded(newIndex);
    }
    public void ShowPanelImmediate(int index)
    {
        _currentIndex = index;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            panels[i].gameObject.SetActive(i == index);
            panels[i].anchoredPosition = Vector2.zero;
        }

        NotifyPanelLoaded(index);
    }

    private void NotifyPanelLoaded(int index)
    {
        if (uiGameManager == null) return;

        uiGameManager.RefreshHUD();

        if (index == MAP_PANEL_INDEX)
            uiGameManager.RefreshCurrentBattleMapText();
    }

    private IEnumerator SlideRoutine(int oldIndex, int newIndex, float duration)
    {
        float dir = (newIndex > oldIndex) ? 1f : -1f;

        RectTransform outPanel = (oldIndex >= 0 && oldIndex < panels.Length) ? panels[oldIndex] : null;
        RectTransform inPanel  = (newIndex >= 0 && newIndex < panels.Length) ? panels[newIndex]  : null;

        float panelWidth = GetPanelWidth();

        if (inPanel != null)
        {
            inPanel.gameObject.SetActive(true);
            inPanel.anchoredPosition = new Vector2(panelWidth * dir, 0f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));

            if (inPanel != null)
                inPanel.anchoredPosition = new Vector2(Mathf.Lerp(panelWidth * dir, 0f, t), 0f);

            if (outPanel != null)
                outPanel.anchoredPosition = new Vector2(Mathf.Lerp(0f, -panelWidth * dir, t), 0f);

            yield return null;
        }

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
