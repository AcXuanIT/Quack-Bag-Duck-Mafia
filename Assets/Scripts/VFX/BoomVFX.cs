using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gắn trên prefab Boom (Assets/Prefabs/VFX/Boom.prefab) — VFX dùng cho weapon
/// weaponData.Category == WeaponCategory.Boom (weapon ném ra rồi phát nổ, VD: bom, mìn).
/// Được Duck.SpawnWeaponVFX() Spawn (qua PoolingManager) rồi gọi Launch() ngay sau đó.
///
/// LUỒNG HOẠT ĐỘNG:
///   1. Launch() đặt object tại startPos (vị trí Duck vừa ném — attacker):
///        a. TẮT hẳn component Animator tạm thời khi Rebind()/Update(0f) để reset baseline sạch,
///           rồi enabled = false ngay để animation nổ TUYỆT ĐỐI không tự chạy trong lúc bay — kể
///           cả khi Boom.controller chỉ có 1 state duy nhất ("Boom") và đó cũng là default state.
///           QUAN TRỌNG: việc tắt animator này phải làm lại ở MỖI LẦN Launch() (không chỉ Awake()),
///           vì object được tái sử dụng qua PoolingManager — Explode() lần trước đã bật
///           animator.enabled = true lên để chạy animation nổ.
///        b. ÉP RESET transform.localScale VỀ _originalLocalScale (chụp 1 lần trong Awake(), là
///           scale gốc thật của prefab) NGAY SAU khi tắt Animator ở bước (a) — BẮT BUỘC, vì
///           animator.Update(0f) ở bước (a) evaluate animation clip "Boom" tại frame 0, và clip
///           nổ này có curve scale (hiệu ứng "pop" phóng to dần từ rất nhỏ, VD 0.01) — nếu không
///           reset lại, Boom sẽ bị "đóng băng" ở scale ~0.01 suốt lúc bay (bug đã gặp: không nhìn
///           thấy gì trong lúc bay do quá nhỏ).
///        c. GÁN sprite weapon (spriteRenderer.sprite = sprite) SAU KHI đã xử lý xong Animator ở
///           bước (a) — THỨ TỰ NÀY BẮT BUỘC: animator.Update(0f) evaluate animation clip "Boom"
///           tại frame 0, và nếu clip đó có curve điều khiển SpriteRenderer.sprite (thường có, vì
///           đây là clip nổ dùng sprite-swap từng frame) thì nó sẽ GHI ĐÈ sprite vừa gán, khiến
///           hiển thị nhầm sprite nổ thay vì sprite weapon trong lúc bay (bug đã gặp — sprite
///           weapon "biến mất", chỉ thấy sprite vfx explode). Gán sprite SAU animator mới đảm bảo
///           giá trị hiển thị cuối cùng đúng là sprite mình muốn.
///   2. Bay theo ĐƯỜNG VÒNG CUNG (DOTween Transform.DOJump — jumpPower = arcHeight, duration =
///      flyDuration) tới targetPos (vị trí target enemy tại thời điểm ném).
///   3. KHÔNG đợi DOJump báo hoàn tất (OnComplete) mới nổ — mỗi frame trong lúc bay (OnUpdate),
///      script tự so khoảng cách hiện tại giữa vị trí Boom và targetPos; NGAY KHI khoảng cách đó
///      <= explodeDistanceThreshold (mặc định 0.5f) -> lập tức Kill() Tween tại chỗ rồi Explode()
///      luôn (không cần bay tới đúng tâm targetPos 100% mới nổ, tránh cảm giác "trờn" qua mục tiêu
///      rồi mới nổ khi target đã nhích/animation bay không tuyệt đối chính xác điểm cuối).
///      OnComplete (DOJump bay hết quãng đường bình thường) vẫn được giữ làm SAFEGUARD dự phòng
///      (trường hợp vì lý do gì đó khoảng cách không bao giờ <= threshold, VD threshold quá nhỏ).
///        - Explode():
///        - BẬT LẠI animator.enabled = true rồi chạy animation nổ (Boom.controller, state
///          "Boom", dài 0.25s) — lúc này spriteRenderer đã bị ẩn (enabled = false) nên animation
///          nổ (bao gồm cả curve scale "pop") hiển thị độc lập, không xung đột với sprite/scale
///          của giai đoạn bay.
///        - Gây damage cho MỌI Duck thuộc PHE ĐỊCH (tag đối lập attackerTag truyền vào Launch())
///          đang NẰM TRONG / CHẠM vào damageCollider (CircleCollider2D trên chính GameObject
///          này) tại thời điểm nổ — dùng Physics2D.OverlapCollider() để lấy chính xác toàn bộ
///          Collider2D đang chồng lấn/chạm (không chỉ tâm-trong-bán-kính).
///   4. Đợi hết animation nổ rồi Despawn() về Pool qua PoolingManager (KHÔNG Destroy trực tiếp,
///      để có thể tái sử dụng như mọi VFX khác trong game — xem VFXGun.cs/VFXDead.cs).
///
/// DỮ LIỆU ĐƯỢC TRUYỀN VÀO TỪ BÊN NGOÀI (Duck.SpawnWeaponVFX()) mỗi lần Launch(), KHÔNG tự đọc
/// WeaponEntry — tách biệt hoàn toàn khỏi hệ thống weapon:
///   - sprite      : hình ảnh weapon hiển thị trong lúc bay (weaponRenderer.sprite của Duck ném).
///   - damage      : sát thương gây cho MỖI Duck địch bị trúng nổ (Duck.Damage của Duck ném, chụp
///                   lại tại thời điểm ném — không giữ tham chiếu Duck để tránh lỗi nếu Duck đó
///                   bị Despawn/tái sử dụng qua Pool trước khi bom kịp nổ).
///   - attackerTag : tag ("Player"/"Enemy") của Duck vừa ném — dùng xác định phe ĐỊCH cần gây
///                   damage (đối lập tag, giống quy ước CompareTag trong Duck.ScanForward()).
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CircleCollider2D))]
public class BoomVFX : MonoBehaviour
{
    [Header("=== Visual ===")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh weapon trong lúc bay — tự tìm trên chính GameObject này nếu để trống.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Animator chạy Boom.controller (state \"Boom\", 1 state duy nhất, dài 0.25s) khi nổ. " +
             "Bị TẮT (enabled = false) trong lúc bay — chỉ bật lại đúng lúc Explode().")]
    [SerializeField] private Animator animator;

    [Tooltip("CircleCollider2D dùng để xác định vùng gây damage khi nổ (Physics2D.OverlapCollider) — nên để isTrigger = true.")]
    [SerializeField] private CircleCollider2D damageCollider;

    [Header("=== Arc Flight (DOTween DOJump) ===")]
    [Tooltip("Độ cao vòng cung khi bay (đơn vị) — truyền thẳng vào DOJump(jumpPower:).")]
    [SerializeField] private float arcHeight = 2.5f;

    [Tooltip("Thời gian bay từ vị trí attacker tới target (giây).")]
    [SerializeField] private float flyDuration = 1f;

    [Tooltip("Khoảng cách (đơn vị) tới targetPos mà tại đó Boom coi như ĐÃ ĐẾN NƠI và bắt đầu nổ " +
             "(animation + gây damage) NGAY LẬP TỨC, không cần đợi DOJump báo hoàn tất 100%. " +
             "Được kiểm tra mỗi frame trong lúc bay (OnUpdate của Tween).")]
    [SerializeField] private float explodeDistanceThreshold = 0.5f;

    private float     _damage;
    private string    _attackerTag;
    private Vector3    _targetPos;
    private Vector3    _originalLocalScale;
    private bool       _exploded;
    private Tween      _flyTween;
    private Coroutine _despawnRoutine;

    private void Reset()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (damageCollider == null) damageCollider = GetComponent<CircleCollider2D>();
    }

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (damageCollider == null) damageCollider = GetComponent<CircleCollider2D>();

        // Chụp lại scale GỐC thật của prefab (trước khi Animator kịp evaluate bất kỳ frame nào) —
        // dùng để ép reset lại mỗi lần Launch(), tránh bug bị "đóng băng" ở scale nhỏ xíu của
        // frame 0 animation nổ (xem class doc).
        _originalLocalScale = transform.localScale;
    }

    /// <summary>
    /// Bắt đầu bay vòng cung từ startPos tới targetPos rồi tự nổ + gây damage khi tới nơi.
    /// Gọi lại được nhiều lần (object tái sử dụng từ Pool) — mỗi lần gọi tự huỷ Tween/Coroutine
    /// cũ (nếu object đang chạy dở từ lần dùng trước) rồi reset lại toàn bộ trạng thái từ đầu,
    /// BAO GỒM CẢ animator.enabled VÀ transform.localScale (xem class doc — bắt buộc reset mỗi
    /// lần, không chỉ Awake()).
    /// LƯU Ý THỨ TỰ: xử lý Animator (Rebind/Update/disable) XONG rồi mới reset scale + gán
    /// spriteRenderer.sprite — xem class doc, đảo ngược thứ tự sẽ khiến animation ghi đè mất
    /// sprite/scale đúng của weapon.
    /// </summary>
    /// <param name="startPos">Vị trí bắt đầu bay — vị trí Duck (attacker) vừa ném bom.</param>
    /// <param name="targetPos">Vị trí đích — vị trí target enemy tại thời điểm ném.</param>
    /// <param name="sprite">Sprite weapon hiển thị trong lúc bay.</param>
    /// <param name="damage">Sát thương gây cho mỗi Duck địch bị trúng nổ.</param>
    /// <param name="attackerTag">Tag ("Player"/"Enemy") của Duck vừa ném — dùng xác định phe địch.</param>
    public void Launch(Vector3 startPos, Vector3 targetPos, Sprite sprite, float damage, string attackerTag)
    {
        // Object có thể đang chạy dở (Tween bay / coroutine đợi Despawn) từ lần dùng trước trong
        // Pool -> huỷ sạch trước khi bắt đầu lượt bay mới.
        _flyTween?.Kill();
        if (_despawnRoutine != null) { StopCoroutine(_despawnRoutine); _despawnRoutine = null; }

        _damage      = damage;
        _attackerTag = attackerTag;
        _targetPos   = targetPos;
        _exploded    = false;

        transform.position = startPos;

        //if (damageCollider != null)
           // damageCollider.enabled = false; // chỉ bật đúng lúc nổ, tránh gây damage khi đang bay ngang qua

        if (animator != null)
        {
            // Bật tạm để Rebind() reset đúng baseline, rồi TẮT NGAY (enabled = false) để animation
            // nổ TUYỆT ĐỐI không tự chạy trong lúc bay — kể cả khi state "Boom" là default state
            // của controller (xem class doc). Bắt buộc làm lại mỗi lần Launch() vì object tái sử
            // dụng từ Pool có thể đã được Explode() bật animator.enabled = true từ lượt trước.
            //
            // PHẢI xử lý xong Animator TRƯỚC khi reset scale / gán spriteRenderer.sprite bên dưới
            // — Update(0f) evaluate animation clip "Boom" tại frame 0, nếu clip có curve trên
            // SpriteRenderer.sprite và/hoặc transform.localScale (rất có thể, vì đây là clip nổ
            // sprite-swap + "pop" scale) thì nó sẽ ghi đè ngay sau khi gán nếu làm ngược thứ tự —
            // gây bug "mất sprite weapon" và "Boom bị co nhỏ về ~0.01, không thấy gì".
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        // Ép reset lại scale về đúng scale gốc — animator.Update(0f) ở trên có thể đã evaluate
        // curve scale của frame 0 animation nổ (thường rất nhỏ, VD 0.01) và "đóng băng" nó lại
        // khi tắt animator ngay sau đó.
        transform.localScale = _originalLocalScale;

        // Gán sprite weapon SAU CÙNG (sau khi Animator đã bị tắt hẳn ở trên) để đảm bảo đây luôn
        // là giá trị hiển thị cuối cùng trong lúc bay, không bị animation ghi đè.
        if (spriteRenderer != null)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true; // đảm bảo hiện lại (có thể đã bị Explode() lần trước ẩn đi)
        }

        _flyTween = transform
            .DOJump(targetPos, arcHeight, 1, flyDuration)
            .SetEase(Ease.Linear)
            .OnUpdate(CheckReachedTarget)
            .OnComplete(Explode);
    }

    /// <summary>
    /// Gọi mỗi frame trong lúc DOJump đang chạy (OnUpdate) — so khoảng cách hiện tại giữa vị trí
    /// Boom và _targetPos; nếu <= explodeDistanceThreshold thì coi như đã tới nơi, Kill() Tween
    /// tại chỗ (không invoke OnComplete) rồi Explode() ngay lập tức, không đợi DOJump bay hết
    /// quãng đường 100%.
    /// </summary>
    private void CheckReachedTarget()
    {
        if (_exploded) return;

        if (Vector3.Distance(transform.position, _targetPos) <= explodeDistanceThreshold)
        {
            _flyTween?.Kill();
            _flyTween = null;
            Explode();
        }
    }

    /// <summary>Được gọi khi Boom coi như đã tới nơi (qua CheckReachedTarget hoặc DOJump OnComplete dự phòng) — BẬT LẠI animator rồi chạy animation nổ + gây AoE damage. Chỉ chạy đúng 1 lần mỗi lượt Launch() nhờ cờ _exploded.</summary>
    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        _flyTween = null;

        //if (spriteRenderer != null)
            //spriteRenderer.enabled = false; // ẩn sprite weapon, nhường chỗ hiển thị cho animation nổ

        if (animator != null)
        {
            animator.enabled = true; // chỉ bật lại ĐÚNG LÚC NÀY — xem class doc/Launch()
            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);
        }

        DealAreaDamage();

        _despawnRoutine = StartCoroutine(WaitAnimationThenDespawn());
    }

    /// <summary>
    /// Gây damage cho MỌI Duck thuộc phe địch (tag đối lập attackerTag) đang NẰM TRONG / CHẠM vào
    /// damageCollider — dùng Physics2D.OverlapCollider() để lấy chính xác các Collider2D thực sự
    /// đang chồng lấn/chạm với hình dạng CircleCollider2D này (useTriggers = true để không bỏ sót
    /// collider dạng trigger). Mỗi Duck chỉ bị trừ máu ĐÚNG 1 LẦN dù có nhiều Collider2D con cùng
    /// thuộc 1 Duck (lọc qua HashSet theo GetComponentInParent&lt;Duck&gt;()).
    /// </summary>
    private void DealAreaDamage()
    {
        if (damageCollider == null) return;

        //damageCollider.enabled = true;

        string enemyTag = string.IsNullOrEmpty(_attackerTag)
            ? null
            : (_attackerTag == "Player" ? "Enemy" : "Player");

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.NoFilter();

        var results = new List<Collider2D>();
        Physics2D.OverlapCollider(damageCollider, filter, results);

        var damaged = new HashSet<Duck>();
        foreach (var hit in results)
        {
            if (hit == null) continue;
            if (enemyTag != null && !hit.CompareTag(enemyTag)) continue;

            var duck = hit.GetComponentInParent<Duck>();
            if (duck != null && !duck.IsDead && damaged.Add(duck))
                duck.TakeDamage(_damage);
        }
    }

    private IEnumerator WaitAnimationThenDespawn()
    {
        yield return null; // đợi 1 frame để AnimatorStateInfo cập nhật đúng theo Play() vừa gọi

        float duration = animator != null ? animator.GetCurrentAnimatorStateInfo(0).length : 0.25f;
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        _despawnRoutine = null;
        PoolingManager.Despawn(gameObject);
    }

    private void OnDisable()
    {
        // Object bị Despawn/tắt giữa chừng (VD BattleManager.ResetBattleState() dọn dẹp trận đấu)
        // -> huỷ sạch Tween/Coroutine đang chạy dở, tránh callback chạy nhầm trên object đã bị
        // Pool tái sử dụng cho lượt Boom khác.
        _flyTween?.Kill();
        _flyTween = null;

        if (_despawnRoutine != null)
        {
            StopCoroutine(_despawnRoutine);
            _despawnRoutine = null;
        }
    }
}
