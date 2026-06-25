using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// LoadMapAnimator - Trượt LoadMap qua màn hình với điểm dừng giữa.
///
/// Timeline mặc định (totalDuration=3s, pauseAtCenter=1s):
///   0s → 1s  : -width → 0     (slide vào)
///   1s        : MenuGame OFF  |  BatteMapUI ON  |  BatteMap GO ON
///   1s → 2s  : dừng tại 0    (pause 1s)
///   2s → 3s  : 0 → +width    (slide ra)
/// </summary>
public class LoadMapAnimator : MonoBehaviour
{
    [Header("=== Targets ===")]
    [Tooltip("UI MenuGame - tắt khi LoadMap đến PosX = 0")]
    [SerializeField] private GameObject menuGame;

    [Tooltip("UI BatteMap - bật khi LoadMap đến PosX = 0")]
    [SerializeField] private GameObject batteMapUI;

    [Tooltip("GameObject BatteMap (non-UI) - bật qua GameManager khi đến PosX = 0")]
    [SerializeField] private GameManager gameManager;

    [Header("=== Tween Settings ===")]
    [Tooltip("Tổng thời gian toàn bộ animation (giây)")]
    public float totalDuration = 3f;

    [Tooltip("Thời gian dừng tại PosX = 0 (giây)")]
    public float pauseAtCenter = 1f;

    [Tooltip("Ease cho đoạn slide vào màn hình")]
    [SerializeField] private Ease easeIn  = Ease.OutCubic;

    [Tooltip("Ease cho đoạn slide ra khỏi màn hình")]
    [SerializeField] private Ease easeOut = Ease.InCubic;

    /// <summary>
    /// Gọi từ UIGameManager khi nhấn Play.
    /// </summary>
    public void Play(float dur, Action onComplete = null)
    {
        totalDuration = dur;

        var rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("[LoadMapAnimator] Không tìm thấy RectTransform!");
            onComplete?.Invoke();
            return;
        }

        float width    = rt.sizeDelta.x;
        float moveTime = totalDuration - pauseAtCenter; // 2s
        float halfMove = moveTime * 0.5f;              // 1s mỗi đoạn

        // Setup vị trí bắt đầu ngoài màn hình bên trái
        rt.anchoredPosition = new Vector2(-width, rt.anchoredPosition.y);
        gameObject.SetActive(true);
        rt.DOKill();

        Sequence seq = DOTween.Sequence();

        // [1] Slide vào: -width → 0
        seq.Append(rt.DOAnchorPosX(0f, halfMove).SetEase(easeIn));

        // [2] Callback tại PosX = 0: tắt MenuGame, bật BatteMap
        seq.AppendCallback(() =>
        {
            if (menuGame != null)
                menuGame.SetActive(false);
            else
                Debug.LogWarning("[LoadMapAnimator] menuGame chưa được gán!");

            if (batteMapUI != null)
                batteMapUI.SetActive(true);
            else
                Debug.LogWarning("[LoadMapAnimator] batteMapUI chưa được gán!");

            if (gameManager != null)
                gameManager.EnableBatteMap();
            else
                Debug.LogWarning("[LoadMapAnimator] gameManager chưa được gán!");
        });

        // [3] Dừng tại 0 trong pauseAtCenter giây
        seq.AppendInterval(pauseAtCenter);

        // [4] Slide ra: 0 → +width
        seq.Append(rt.DOAnchorPosX(width, halfMove).SetEase(easeOut));

        // [5] Tắt LoadMap, gọi callback
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });

        seq.Play();
    }
}
