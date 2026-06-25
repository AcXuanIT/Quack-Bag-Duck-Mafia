using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý thanh loading bar và text phần trăm của LoadingStartGame.
/// Dùng RectTransform.sizeDelta để animate thanh Filter từ width=0 đến full width.
/// Image.type = Sliced được giữ nguyên.
/// Được gọi bởi UIGameManager.
/// </summary>
public class LoadingBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform filterRect;
    [SerializeField] private TextMeshProUGUI textLoad;

    [Header("Settings")]
    [SerializeField] private float loadingDuration = 3f;

    // Width tối đa của Filter khi loading = 100% (đọc từ thiết kế gốc)
    private float _fullWidth;
    private bool _isLoading = false;

    private void Awake()
    {
        if (filterRect == null) return;

        // Lưu lại full width từ Inspector (kích thước thiết kế gốc)
        _fullWidth = filterRect.sizeDelta.x;

        // Đổi pivot về (0, 0.5) để thanh mở rộng từ trái sang phải
        // Khi đổi pivot, anchoredPosition phải bù lại để vị trí thực không bị lệch
        Vector2 oldPivot = filterRect.pivot;
        Vector2 newPivot = new Vector2(0f, 0.5f);
        float pivotDeltaX = (newPivot.x - oldPivot.x) * _fullWidth;
        filterRect.pivot = newPivot;
        filterRect.anchoredPosition += new Vector2(pivotDeltaX, 0f);

        // Đặt width về 0 lúc khởi tạo (bắt đầu trống)
        filterRect.sizeDelta = new Vector2(0f, filterRect.sizeDelta.y);

        if (textLoad != null)
            textLoad.text = "0%";
    }

    /// <summary>
    /// Bắt đầu animation loading từ 0 đến 100%.
    /// Callback onComplete được gọi khi đạt 100%.
    /// </summary>
    public void StartLoading(System.Action onComplete = null)
    {
        if (_isLoading) return;
        StartCoroutine(LoadingRoutine(onComplete));
    }

    private IEnumerator LoadingRoutine(System.Action onComplete)
    {
        _isLoading = true;
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDuration);
            SetProgress(progress);
            yield return null;
        }

        // Đảm bảo đạt đúng 100%
        SetProgress(1f);

        _isLoading = false;
        onComplete?.Invoke();
    }

    private void SetProgress(float t)
    {
        if (filterRect != null)
        {
            // Chỉ thay đổi width, pivot=(0,0.5) nên thanh luôn mở rộng từ trái
            filterRect.sizeDelta = new Vector2(Mathf.Lerp(0f, _fullWidth, t), filterRect.sizeDelta.y);
        }

        if (textLoad != null)
            textLoad.text = Mathf.RoundToInt(t * 100f) + "%";
    }
}
