using UnityEngine;
using DG.Tweening;

public class MyTeamAnimation : MonoBehaviour
{
    [Header("=== Spawn Animation ===")]
    public float stepDuration = 0.22f;
    public Ease  stepEase     = Ease.InOutSine;
    public float pauseDelay   = 0.005f;

    private Sequence _seq;
    private Vector3  _base;
    private bool     _isPlaying;

    /// <summary>True khi animation spawn đang chạy dở — dùng để chặn gọi chồng.</summary>
    public bool IsPlaying => _isPlaying;

    [Header("=== Damage Flash ===")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer;

    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);

    [SerializeField] private float flashDuration = 0.08f;

    private Color _originalSpriteColor;
    private Tween _flashSpriteTween;

    private void Awake()
    {
        _base = transform.localScale;

        if (flashSpriteRenderer == null)
            flashSpriteRenderer = GetComponent<SpriteRenderer>();

        if (flashSpriteRenderer != null)
            _originalSpriteColor = flashSpriteRenderer.color;
    }

    // ─── Spawn Animation ──

    public void PlaySpawnAnimation()
    {
        if (_isPlaying) return; 

        _isPlaying = true;
        _seq?.Kill();

        var stretch = new Vector3(_base.x * 0.95f, _base.y * 1.05f, _base.z);
        var squash  = new Vector3(_base.x * 1.05f, _base.y * 0.95f, _base.z);

        _seq = DOTween.Sequence();

    
        _seq.Append(transform.DOScale(stretch, stepDuration).SetEase(stepEase));
      
        _seq.Append(transform.DOScale(_base,   stepDuration).SetEase(stepEase));
        _seq.AppendInterval(pauseDelay);

        // (1,1) -> squash
        _seq.Append(transform.DOScale(squash,  stepDuration).SetEase(stepEase));
        // squash -> (1,1)
        _seq.Append(transform.DOScale(_base,   stepDuration).SetEase(stepEase));
        _seq.AppendInterval(pauseDelay);

        // KHÔNG SetLoops
        _seq.SetUpdate(UpdateType.Normal);
        _seq.OnComplete(() =>
        {
            _isPlaying = false;
        });
    }

    private Sequence _bounceSeq;
    private bool     _isBouncing;
    private bool     _bouncePending;

    public bool IsBouncing => _isBouncing;

    public void AnimationSpawn()
    {
        if (_isBouncing)
        {
            _bouncePending = true;
            return;
        }

        PlayBounceOnce();
    }

    private void PlayBounceOnce()
    {
        _isBouncing = true;
        _bouncePending = false;

        _bounceSeq?.Kill();

        const float totalDuration = 0.5f;
        float stepTime = totalDuration / 4f;

        var up   = new Vector3(_base.x - 0.005f, _base.y + 0.005f, _base.z);
        var down = new Vector3(_base.x + 0.005f, _base.y - 0.005f, _base.z);

        _bounceSeq = DOTween.Sequence();
        _bounceSeq.Append(transform.DOScale(up,   stepTime).SetEase(Ease.InOutSine));
        _bounceSeq.Append(transform.DOScale(_base, stepTime).SetEase(Ease.InOutSine));
        _bounceSeq.Append(transform.DOScale(down, stepTime).SetEase(Ease.InOutSine));
        _bounceSeq.Append(transform.DOScale(_base, stepTime).SetEase(Ease.InOutSine));
        _bounceSeq.SetUpdate(UpdateType.Normal);
        _bounceSeq.OnComplete(() =>
        {
            _isBouncing = false;

            if (_bouncePending)
                PlayBounceOnce();
        });
    }

    // ─── Damage Flash ─
    public void PlayDamageFlash()
    {
        if (flashSpriteRenderer == null) return;

        _flashSpriteTween?.Kill();
        flashSpriteRenderer.color = flashColor;
        _flashSpriteTween = flashSpriteRenderer.DOColor(_originalSpriteColor, flashDuration).SetEase(Ease.OutQuad);
    }

    private void OnDisable()
    {
        _seq?.Kill();
        _bounceSeq?.Kill();
        transform.localScale = _base;
        _isPlaying = false;
        _isBouncing = false;
        _bouncePending = false;

        _flashSpriteTween?.Kill();
        if (flashSpriteRenderer != null) flashSpriteRenderer.color = _originalSpriteColor;
    }
}
