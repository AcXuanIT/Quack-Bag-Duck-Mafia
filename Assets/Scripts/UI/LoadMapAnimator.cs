using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// Điều khiển hiệu ứng LoadMap: slide từ trái sang phải bằng DOTween.
/// BGLeft và BGRight kéo ra hai bên để lộ màn hình game.
/// Khi hoàn thành gọi callback OnComplete.
/// </summary>
public class LoadMapAnimator : MonoBehaviour
{
    [Header("=== Panel Tham Chiếu ===")]
    [Tooltip("RectTransform của tấm che bên trái")]
    [SerializeField] private RectTransform bgLeft;

    [Tooltip("RectTransform của tấm che bên phải")]
    [SerializeField] private RectTransform bgRight;

    [Tooltip("RectTransform của BG nền chính (tuỳ chọn, fade out)")]
    [SerializeField] private CanvasGroup bgCanvasGroup;

    [Header("=== Cài Đặt Thời Gian ===")]
    [Tooltip("Thời gian chạy animation LoadMap (giây). Có thể chỉnh trong UIGameManager.")]
    [SerializeField] public float animDuration = 1.5f;

    [Tooltip("Ease cho hiệu ứng slide")]
    [SerializeField] private Ease slideEase = Ease.InOutCubic;

    // Cache canvas width để tính offset
    private float _halfWidth;

    private void Awake()
    {
        // Lấy độ rộng canvas để slide ra ngoài màn hình
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<RectTransform>() != null)
            _halfWidth = canvas.GetComponent<RectTransform>().rect.width * 0.5f + 50f;
        else
            _halfWidth = 960f + 50f; // fallback
    }

    /// <summary>
    /// Chạy animation LoadMap. BGLeft trượt sang trái, BGRight trượt sang phải.
    /// onComplete gọi khi animation kết thúc.
    /// </summary>
    public void PlayAnimation(float duration, Action onComplete = null)
    {
        gameObject.SetActive(true);
        animDuration = duration;

        // Reset vị trí ban đầu (hai tấm đang đóng kín màn hình)
        if (bgLeft != null)  bgLeft.anchoredPosition  = new Vector2(-_halfWidth * 0.5f, 0f);
        if (bgRight != null) bgRight.anchoredPosition = new Vector2(_halfWidth * 0.5f,  0f);

        // Tạo sequence DOTween
        Sequence seq = DOTween.Sequence();

        // BGLeft trượt sang trái ra khỏi màn hình
        if (bgLeft != null)
            seq.Join(bgLeft.DOAnchorPosX(-_halfWidth * 1.5f, animDuration).SetEase(slideEase));

        // BGRight trượt sang phải ra khỏi màn hình
        if (bgRight != null)
            seq.Join(bgRight.DOAnchorPosX(_halfWidth * 1.5f, animDuration).SetEase(slideEase));

        // Fade BG nền (tuỳ chọn)
        if (bgCanvasGroup != null)
        {
            bgCanvasGroup.alpha = 1f;
            seq.Join(bgCanvasGroup.DOFade(0f, animDuration * 0.8f).SetEase(Ease.InQuad));
        }

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });

        seq.Play();
    }
}
