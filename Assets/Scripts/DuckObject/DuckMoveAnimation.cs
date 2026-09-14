using UnityEngine;
using DG.Tweening;

public class DuckMoveAnimation : MonoBehaviour
{
    [Header("Move Loop")]
    public float stepDuration = 0.22f;
    public Ease  stepEase     = Ease.InOutSine;
    public float pauseDelay   = 0.05f;

    private Sequence _seq;
    private Vector3  _base;

    [Header("=== Damage Flash ===")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer;

    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);

    [SerializeField] private float flashDuration = 0.08f;

    private Color _originalSpriteColor;
    private Tween _flashTween;

    private void Awake()
    {
        if (flashSpriteRenderer == null)
            flashSpriteRenderer = GetComponent<SpriteRenderer>();

        if (flashSpriteRenderer != null)
            _originalSpriteColor = flashSpriteRenderer.color;
    }

    private void OnEnable()
    {
        _base = transform.localScale;
        PlayLoop();
    }

    private void OnDisable()
    {
        _seq?.Kill();
        transform.localScale = _base;

        _flashTween?.Kill();
        if (flashSpriteRenderer != null)
            flashSpriteRenderer.color = _originalSpriteColor;
    }

    private void PlayLoop()
    {
        _seq?.Kill();

        var stretch = new Vector3(_base.x * 0.95f, _base.y * 1.05f, _base.z);
        var squash  = new Vector3(_base.x * 1.05f, _base.y * 0.95f, _base.z);

        _seq = DOTween.Sequence();

        // (1,1) -> stretch
        _seq.Append(transform.DOScale(stretch, stepDuration).SetEase(stepEase));
        // stretch -> (1,1)
        _seq.Append(transform.DOScale(_base,   stepDuration).SetEase(stepEase));
        _seq.AppendInterval(pauseDelay);

        // (1,1) -> squash
        _seq.Append(transform.DOScale(squash,  stepDuration).SetEase(stepEase));
        // squash -> (1,1)
        _seq.Append(transform.DOScale(_base,   stepDuration).SetEase(stepEase));
        _seq.AppendInterval(pauseDelay);

        _seq.SetLoops(-1, LoopType.Restart);
        _seq.SetUpdate(UpdateType.Normal);
    }

    public void PlayDamageFlash()
    {
        if (flashSpriteRenderer == null) return;

        _flashTween?.Kill();
        flashSpriteRenderer.color = flashColor;
        _flashTween = flashSpriteRenderer
            .DOColor(_originalSpriteColor, flashDuration)
            .SetEase(Ease.OutQuad);
    }
}
