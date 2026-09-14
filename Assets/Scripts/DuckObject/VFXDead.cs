using System.Collections;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class VFXDead : MonoBehaviour
{
    [Header("=== Animation ===")]
    [SerializeField] private Animator animator;

    [Header("=== Fade Out ===")]
    [SerializeField] private float fadeDuration = 0.3f;

    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private Tween      _fadeTween;
    private Coroutine  _playRoutine;

    public event System.Action OnFinished;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable() => Play();

    private void OnDisable()
    {
        _fadeTween?.Kill();
        _fadeTween = null;

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
    }

    public void Play()
    {
        _fadeTween?.Kill();
        SetAlpha(1f);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float animLength = GetClipLength();
        if (animLength > 0f)
            yield return new WaitForSeconds(animLength);

        bool faded = false;
        _fadeTween = DOVirtual.Float(1f, 0f, fadeDuration, SetAlpha)
            .SetEase(Ease.Linear)
            .OnComplete(() => faded = true);

        while (!faded)
            yield return null;

        _playRoutine = null;
        gameObject.SetActive(false);
        OnFinished?.Invoke();
    }
    private void SetAlpha(float a)
    {
        if (spriteRenderers == null) return;

        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    private float GetClipLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        var clips = animator.runtimeAnimatorController.animationClips;
        return clips != null && clips.Length > 0 ? clips[0].length : 0f;
    }
}
