using UnityEngine;
using DG.Tweening;

/// <summary>
/// Animation cho Duck (UnitDuck/EnemyDuck) — gộp 2 hiệu ứng hình ảnh vào 1 chỗ
/// (cùng kiến trúc với MyTeamAnimation, vốn cũng gộp Spawn/Bounce/Flash chung 1 component):
///
///   1) Move Loop (PlayLoop, tự chạy trong OnEnable) — hiệu ứng "thở" lặp vô hạn trên scale:
///      (1,1) -> stretch -> (1,1) -> squash -> (1,1) -> lặp lại. Mỗi bước đều đi từ (1,1)
///      nên không bao giờ "nhảy" scale. Delta +-0.05 so với base.
///
///   2) Damage Flash (PlayDamageFlash) — nháy màu SpriteRenderer khi Duck nhận damage:
///      set color = flashColor ngay lập tức rồi tween mượt về màu gốc trong flashDuration.
///      Hoạt động ĐỘC LẬP với Move Loop (1 bên tween scale, 1 bên tween color) nên gọi
///      chồng lên nhau lúc nào cũng an toàn — không cần dừng Move Loop để nháy màu.
///      LƯU Ý MÀU: SpriteRenderer.color là phép NHÂN (tint), không phải overlay. Sprite gốc
///      mặc định color = (1,1,1,1) trắng tinh, nên nếu flashColor = trắng thì set color =
///      flashColor KHÔNG đổi gì cả (trắng x trắng = trắng, không thể "sáng hơn trắng").
///      Vì vậy flashColor mặc định dùng ĐỎ — tint nhân với đỏ luôn tạo khác biệt rõ rệt
///      bất kể màu gốc sprite là gì.
///
/// Duck.cs gọi PlayDamageFlash() qua field duckAnimation mỗi khi TakeDamage() thay vì
/// tự quản lý Tween/Color riêng, để mọi hiệu ứng hình ảnh của Duck tập trung ở component này.
/// </summary>
public class DuckMoveAnimation : MonoBehaviour
{
    [Header("=== Move Loop (Squash/Stretch) ===")]
    public float stepDuration = 0.22f;
    public Ease  stepEase     = Ease.InOutSine;
    public float pauseDelay   = 0.05f;

    private Sequence _seq;
    private Vector3  _base;

    [Header("=== Damage Flash ===")]
    [Tooltip("SpriteRenderer hiển thị hình Duck — để trống sẽ tự lấy GetComponent<SpriteRenderer>() trên chính GameObject này")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer;

    [Tooltip("Màu nháy khi Duck nhận damage. Dùng ĐỎ (không dùng trắng) vì SpriteRenderer.color " +
             "là tint NHÂN — sprite gốc đã trắng (1,1,1,1) nên nháy trắng sẽ không thấy được.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("Thời gian tween từ flashColor về màu gốc (giây)")]
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

        // Đảm bảo không còn tween nháy damage chạy dở khi bị tắt (chết/trả về Pool).
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

    /// <summary>
    /// Nháy màu SpriteRenderer khi Duck nhận damage: set color = flashColor ngay lập tức
    /// rồi tween mượt về màu gốc trong flashDuration. Nếu đang nháy dở thì huỷ tween cũ
    /// và nháy lại từ đầu (không cộng dồn) — giống PlayDamageFlash() của MyTeamAnimation.
    /// </summary>
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
