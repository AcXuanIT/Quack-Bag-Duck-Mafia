using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VFXGun : MonoBehaviour
{
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
            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);

            yield return null;

            float duration = animator.GetCurrentAnimatorStateInfo(0).length;
            if (duration > 0f)
                yield return new WaitForSeconds(duration);
        }

        _runningRoutine = null;
        PoolingManager.Despawn(gameObject);
    }
}
