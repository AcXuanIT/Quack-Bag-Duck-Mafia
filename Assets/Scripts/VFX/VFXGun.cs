using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn trên prefab VFX của weapon (VD: Weapon_4_Sniper_SR_1_VFX...) — được Duck.SpawnWeaponVFX()
/// Spawn (qua PoolingManager) tại vị trí posVFX (child "Weapon/PosVFX") mỗi khi Duck ra đòn
/// (weaponData.vfxWeapon != null, xem WeaponEntry.vfxWeapon trong WeaponData.cs).
///
/// LUỒNG HOẠT ĐỘNG:
///   1. Duck Spawn prefab này qua PoolingManager rồi gọi RunVFX() ngay sau đó.
///   2. RunVFX() phát animation (Animator.Play() từ đầu clip) gắn trên chính GameObject này.
///   3. Đợi đúng bằng thời lượng clip đang chạy (AnimatorStateInfo.length) rồi tự Despawn về lại
///      Pool qua PoolingManager.Despawn() — KHÔNG Destroy() trực tiếp để có thể tái sử dụng.
///
/// Vì object được lấy lại từ Pool (không phải Instantiate mới mỗi lần), animator cần được
/// Rebind()/Play(..., 0f) lại từ đầu mỗi lần RunVFX() để đảm bảo animation luôn chạy lại từ frame
/// đầu tiên thay vì giữ nguyên trạng thái dở dang của lần dùng trước.
/// </summary>
[RequireComponent(typeof(Animator))]
public class VFXGun : MonoBehaviour
{
    [Tooltip("Animator phát animation VFX — mặc định tự lấy trên chính GameObject này (Reset()).")]
    [SerializeField] private Animator animator;

    private Coroutine _runningRoutine;

    private void Reset()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Chạy animation VFX từ đầu. Sau khi animation chạy xong (đúng bằng thời lượng của clip/state
    /// đang phát) thì tự Despawn GameObject này về Pool qua PoolingManager. Gọi lại được nhiều lần
    /// (VD: object được Pool tái sử dụng) — lần gọi sau sẽ huỷ coroutine cũ và chạy lại từ đầu.
    /// </summary>
    public void RunVFX()
    {
        if (_runningRoutine != null)
            StopCoroutine(_runningRoutine);

        _runningRoutine = StartCoroutine(PlayThenDespawn());
    }

    private IEnumerator PlayThenDespawn()
    {
        if (animator != null)
        {
            // Object có thể vừa được lấy lại từ Pool (đã chạy animation trước đó) -> Rebind() +
            // Play(..., 0f) để ép chạy lại animation từ frame đầu tiên, không giữ trạng thái cũ.
            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);

            // Đợi 1 frame để AnimatorStateInfo cập nhật đúng state/length vừa Play().
            yield return null;

            float duration = animator.GetCurrentAnimatorStateInfo(0).length;
            if (duration > 0f)
                yield return new WaitForSeconds(duration);
        }

        _runningRoutine = null;
        PoolingManager.Despawn(gameObject);
    }
}
