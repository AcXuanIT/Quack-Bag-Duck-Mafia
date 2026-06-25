using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI Controller cho màn hình BattleMap.
/// Được kích hoạt sau khi animation LoadMap hoàn thành.
/// Gắn vào: UIGame/StartGame/BatteMap
/// </summary>
public class BattleMapUI : MonoBehaviour
{
    [Header("=== Tham Chiếu UI ===")]
    [Tooltip("Canvas Group để fade in BattleMap UI (tuỳ chọn)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== Hiệu Ứng Vào ===")]
    [Tooltip("Dùng DOTween fade in khi mở BattleMapUI")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 0.4f;

    private void Awake()
    {
        // Đảm bảo UI ẩn ban đầu
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Mở BattleMap UI. Gọi từ UIGameManager sau LoadMap animation.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);

        if (useFadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Ẩn BattleMap UI.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
