using UnityEngine;
using DG.Tweening;

/// <summary>
/// Animation cho Duck trong MyTeam:
///   1) PlaySpawnAnimation() — hiệu ứng "nảy" khi spawn (stretch → squash),
///      cùng kiến trúc chuyển động với DuckMoveAnimation nhưng KHÔNG lặp vô hạn,
///      chỉ chạy đúng 1 chu kỳ rồi dừng. Có cờ _isPlaying chặn gọi chồng lấp.
///   2) PlayDamageFlash() — nháy trắng SpriteRenderer khi nhận damage.
/// </summary>
public class MyTeamAnimation : MonoBehaviour
{
    [Header("=== Spawn Animation ===")]
    public float stepDuration = 0.22f;
    public Ease  stepEase     = Ease.InOutSine;
    public float pauseDelay   = 0.05f;

    private Sequence _seq;
    private Vector3  _base;
    private bool     _isPlaying;

    /// <summary>True khi animation spawn đang chạy dở — dùng để chặn gọi chồng.</summary>
    public bool IsPlaying => _isPlaying;

    [Header("=== Damage Flash ===")]
    [Tooltip("SpriteRenderer hiển thị hình Duck/Team — object hiện dùng SpriteRenderer để hiển thị")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer;

    [Tooltip("Màu nháy khi nhận damage")]
    [SerializeField] private Color flashColor = Color.white;

    [Tooltip("Thời gian tween từ flashColor về màu gốc (giây)")]
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

    // ─── Spawn Animation ────────────────────────────────────

    /// <summary>
    /// Chạy animation spawn (stretch → squash) đúng 1 lần, không lặp lại.
    /// Nếu animation trước chưa chạy xong, lời gọi này sẽ bị bỏ qua
    /// (chỉ chạy tiếp được sau khi animation hiện tại hoàn tất).
    /// </summary>
    public void PlaySpawnAnimation()
    {
        if (_isPlaying) return; // đang chạy dở -> chặn gọi chồng

        _isPlaying = true;
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

        // KHÔNG SetLoops — chỉ chạy 1 chu kỳ duy nhất
        _seq.SetUpdate(UpdateType.Normal);
        _seq.OnComplete(() =>
        {
            _isPlaying = false;
        });
    }

    // ─── Damage Flash ───────────────────────────────────────

    /// <summary>
    /// Nháy trắng SpriteRenderer khi nhận damage:
    /// set color = flashColor ngay lập tức rồi tween mượt về màu gốc trong flashDuration.
    /// </summary>
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
        transform.localScale = _base;
        _isPlaying = false;

        _flashSpriteTween?.Kill();
        if (flashSpriteRenderer != null) flashSpriteRenderer.color = _originalSpriteColor;
    }
}
