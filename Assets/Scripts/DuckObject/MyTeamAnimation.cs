using UnityEngine;
using DG.Tweening;

/// <summary>
/// Animation cho Duck trong MyTeam:
///   1) PlaySpawnAnimation() — hiệu ứng "nảy" khi spawn (stretch → squash),
///      cùng kiến trúc chuyển động với DuckMoveAnimation nhưng KHÔNG lặp vô hạn,
///      chỉ chạy đúng 1 chu kỳ rồi dừng. Có cờ _isPlaying chặn gọi chồng lấp.
///   2) AnimationSpawn() — object "nhún" 1 lần trong 1 giây:
///      (y,x) -> (y+0.05,x-0.05) -> (y,x) -> (y-0.05,x+0.05) -> (y,x).
///      Nếu có nhiều tín hiệu spawn dồn dập trong lúc đang chạy thì KHÔNG huỷ
///      animation hiện tại — chỉ ghi nhận có tín hiệu mới, đợi animation hiện
///      tại chạy XONG rồi mới chạy lại thêm 1 lần.
///   3) PlayDamageFlash() — nháy màu SpriteRenderer khi nhận damage.
///      LƯU Ý MÀU: SpriteRenderer.color là phép NHÂN (tint), không phải overlay. Sprite gốc
///      mặc định color = (1,1,1,1) trắng tinh, nên nếu flashColor = trắng thì set color =
///      flashColor KHÔNG đổi gì cả (trắng x trắng = trắng, không thể "sáng hơn trắng").
///      Vì vậy flashColor mặc định dùng ĐỎ — tint nhân với đỏ luôn tạo khác biệt rõ rệt
///      bất kể màu gốc sprite là gì.
/// </summary>
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
    [Tooltip("SpriteRenderer hiển thị hình Duck/Team — object hiện dùng SpriteRenderer để hiển thị")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer;

    [Tooltip("Màu nháy khi nhận damage. Dùng ĐỎ (không dùng trắng) vì SpriteRenderer.color " +
             "là tint NHÂN — sprite gốc đã trắng (1,1,1,1) nên nháy trắng sẽ không thấy được.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);

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

    // ─── Bounce Animation (AnimationSpawn) ───────────────────

    private Sequence _bounceSeq;
    private bool     _isBouncing;
    private bool     _bouncePending;

    /// <summary>True khi hiệu ứng nhún (AnimationSpawn) đang chạy dở.</summary>
    public bool IsBouncing => _isBouncing;

    /// <summary>
    /// Cho object "nhún" 1 lần trong tổng thời gian 1 giây:
    ///   (y,x) -> (y+0.05, x-0.05) -> (y,x) -> (y-0.05, x+0.05) -> (y,x)
    ///
    /// Nếu có nhiều tín hiệu spawn dồn dập trong lúc animation đang chạy,
    /// KHÔNG huỷ animation hiện tại giữa chừng — chỉ đánh dấu "có tín hiệu mới
    /// đang chờ" (_bouncePending). Khi animation hiện tại chạy XONG mới nhận
    /// tín hiệu đó và tự chạy lại thêm 1 lần. Nhiều tín hiệu dồn dập trong lúc
    /// đó chỉ gộp lại thành ĐÚNG 1 lần chạy tiếp theo (không xếp hàng dài vô hạn).
    /// </summary>
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

    // ─── Damage Flash ───────────────────────────────────────

    /// <summary>
    /// Nháy màu SpriteRenderer khi nhận damage:
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
        _bounceSeq?.Kill();
        transform.localScale = _base;
        _isPlaying = false;
        _isBouncing = false;
        _bouncePending = false;

        _flashSpriteTween?.Kill();
        if (flashSpriteRenderer != null) flashSpriteRenderer.color = _originalSpriteColor;
    }
}
