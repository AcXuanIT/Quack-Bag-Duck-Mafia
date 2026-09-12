using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Component gắn trên GameObject "VFXDead" (child của Unit.prefab / Enemy.prefab, dưới nó có
/// "TopVFX" — Animator chạy UnitDead.controller/EnemyDead.controller — và "ButtomVFX"). Mặc định
/// GameObject này SetActive(false); CHỈ được bật lên bởi Duck (UnitDuck/EnemyDuck) khi Duck chết
/// (xem Duck.PlayDeadVFX()).
///
/// FLOW (tự động chạy trong OnEnable() ngay khi GameObject này được SetActive(true)):
///   1. Reset Animator về đúng frame đầu (Rebind + Update(0f)) để LUÔN chạy lại animation Dead
///      TỪ ĐẦU — cần thiết vì GameObject này có thể được bật lại NHIỀU LẦN trong vòng đời game
///      (Duck được lấy lại từ Pool rồi chết tiếp ở trận sau), trong khi animation Dead không loop
///      nên lần chạy trước sẽ dừng lại ở frame cuối. Đồng thời trả alpha của mọi SpriteRenderer
///      con (feather/head, ButtomVFX, ...) về 1 (đã bị mờ về 0 ở lần chạy trước).
///   2. Chờ đúng thời lượng animation Dead (lấy từ AnimationClip đầu tiên trên Animator — xem
///      GetClipLength()) để animation "nổ" chạy XONG.
///   3. Mờ dần (DOTween, fadeDuration giây) alpha của TOÀN BỘ SpriteRenderer con từ 1 về 0.
///   4. SetActive(false) chính GameObject này khi mờ xong, rồi phát OnFinished — Duck lắng nghe
///      sự kiện này để biết lúc nào thực sự Despawn() (trả Duck về Pool), tránh trường hợp
///      Despawn() chạy ngay lập tức làm GameObject cha bị PoolingManager SetActive(false) — khiến
///      VFXDead (là child) cũng bị tắt theo, cắt ngang animation/hiệu ứng mờ dần đang chạy dở.
/// </summary>
[DisallowMultipleComponent]
public class VFXDead : MonoBehaviour
{
    [Header("=== Animation ===")]
    [Tooltip("Animator phát animation Dead (UnitDead/EnemyDead.controller) — thường ở child \"TopVFX\". " +
             "Tự tìm (GetComponentInChildren) trong Awake() nếu để trống.")]
    [SerializeField] private Animator animator;

    [Header("=== Fade Out ===")]
    [Tooltip("Thời gian (giây) mờ dần TOÀN BỘ SpriteRenderer con, chạy NGAY SAU KHI animation Dead " +
             "phát xong (hết đúng thời lượng AnimationClip).")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Tooltip("SpriteRenderer con cần mờ dần (feather/head, ButtomVFX, ...). Tự tìm TOÀN BỘ " +
             "SpriteRenderer con (GetComponentsInChildren) trong Awake() nếu để trống.")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private Tween      _fadeTween;
    private Coroutine  _playRoutine;

    /// <summary>
    /// Phát khi đã mờ xong VÀ GameObject này đã tự SetActive(false) — Duck lắng nghe để biết
    /// đúng thời điểm có thể Despawn() (trả về Pool) mà không cắt ngang hiệu ứng.
    /// </summary>
    public event System.Action OnFinished;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    /// <summary>Tự động Play() ngay khi GameObject này được SetActive(true) — không cần gọi tay từ bên ngoài.</summary>
    private void OnEnable() => Play();

    /// <summary>
    /// Dọn Tween/Coroutine đang chạy dở nếu GameObject này bị tắt giữa chừng (VD: Duck bị Despawn
    /// cưỡng bức bởi hệ thống khác trước khi VFX kịp chạy xong) — tránh lỗi gọi callback trên
    /// GameObject đã inactive/huỷ.
    /// </summary>
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

    /// <summary>
    /// Chạy lại toàn bộ hiệu ứng từ đầu: reset Animator về frame 0, trả alpha mọi SpriteRenderer
    /// con về 1, rồi chờ animation Dead chạy xong -> mờ dần -> SetActive(false) -> OnFinished.
    /// Tự động được gọi trong OnEnable(); public để có thể replay thủ công nếu cần trong lúc
    /// GameObject đang active (hiếm khi dùng tới).
    /// </summary>
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

    /// <summary>Gán alpha cho TOÀN BỘ spriteRenderers cùng lúc, giữ nguyên RGB.</summary>
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

    /// <summary>
    /// Thời lượng (giây) của AnimationClip đầu tiên trên Animator (state mặc định Dead, không
    /// loop — xem UnitDead.controller/EnemyDead.controller) — 0 nếu thiếu Animator/Controller.
    /// </summary>
    private float GetClipLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        var clips = animator.runtimeAnimatorController.animationClips;
        return clips != null && clips.Length > 0 ? clips[0].length : 0f;
    }
}
