using UnityEngine;
using DG.Tweening;
using System;

public class LoadMapAnimator : MonoBehaviour
{
    [Header("=== Targets ===")]
    [SerializeField] private GameObject menuGame;
    [SerializeField] private GameObject batteMapUI;
    [SerializeField] private GameManager gameManager;

    [Header("=== Tween Settings ===")]
    public float totalDuration = 3f;
    public float pauseAtCenter = 1f;

    [SerializeField] private Ease easeIn  = Ease.OutCubic;
    [SerializeField] private Ease easeOut = Ease.InCubic;

    private float GetWidth(RectTransform rt) => rt.rect.width;


    public void Play(float dur, Action onComplete = null)
    {
        totalDuration = dur;

        var rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            onComplete?.Invoke();
            return;
        }

        float width    = GetWidth(rt);
        float moveTime = totalDuration - pauseAtCenter; 
        float halfMove = moveTime * 0.5f;          

        rt.anchoredPosition = new Vector2(-width, rt.anchoredPosition.y);
        gameObject.SetActive(true);
        rt.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(rt.DOAnchorPosX(0f, halfMove).SetEase(easeIn));

        seq.AppendCallback(() =>
        {
            if (menuGame != null)
                menuGame.SetActive(false);

            if (batteMapUI != null)
                batteMapUI.SetActive(true);

            if (gameManager != null)
                gameManager.EnableBatteMap();
        });

        seq.AppendInterval(pauseAtCenter);
        seq.Append(rt.DOAnchorPosX(width, halfMove).SetEase(easeOut));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });

        seq.Play();
    }
    public void PlayReverse(float dur, Action onComplete = null)
    {
        totalDuration = dur;

        var rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            onComplete?.Invoke();
            return;
        }

        float width    = GetWidth(rt);
        float moveTime = totalDuration - pauseAtCenter;
        float halfMove = moveTime * 0.5f;

        rt.anchoredPosition = new Vector2(-width, rt.anchoredPosition.y);
        gameObject.SetActive(true);
        rt.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(rt.DOAnchorPosX(0f, halfMove).SetEase(easeIn));
        seq.AppendCallback(() =>
        {
            if (batteMapUI != null)
                batteMapUI.SetActive(false);

            if (gameManager != null)
                gameManager.DisableBatteMap();

            if (menuGame != null)
                menuGame.SetActive(true);
        });

        seq.AppendInterval(pauseAtCenter);
        seq.Append(rt.DOAnchorPosX(width, halfMove).SetEase(easeOut));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });

        seq.Play();
    }
}
