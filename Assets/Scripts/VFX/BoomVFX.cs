using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CircleCollider2D))]
public class BoomVFX : MonoBehaviour
{
    [Header("=== Visual ===")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private CircleCollider2D damageCollider;

    [Header("=== Arc Flight (DOTween DOJump) ===")]
    [SerializeField] private float arcHeight = 2.5f;

    [Tooltip("Thời gian bay từ vị trí attacker tới target (giây).")]
    [SerializeField] private float flyDuration = 1f;
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

        _originalLocalScale = transform.localScale;
    }

    public void Launch(Vector3 startPos, Vector3 targetPos, Sprite sprite, float damage, string attackerTag)
    {
        _flyTween?.Kill();
        if (_despawnRoutine != null) { StopCoroutine(_despawnRoutine); _despawnRoutine = null; }

        _damage      = damage;
        _attackerTag = attackerTag;
        _targetPos   = targetPos;
        _exploded    = false;

        transform.position = startPos;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }
        transform.localScale = _originalLocalScale;
        if (spriteRenderer != null)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true; 
        }

        _flyTween = transform
            .DOJump(targetPos, arcHeight, 1, flyDuration)
            .SetEase(Ease.Linear)
            .OnUpdate(CheckReachedTarget)
            .OnComplete(Explode);
    }
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

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        _flyTween = null;

        if (animator != null)
        {
            animator.enabled = true; 
            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);
        }

        DealAreaDamage();

        _despawnRoutine = StartCoroutine(WaitAnimationThenDespawn());
    }
    private void DealAreaDamage()
    {
        if (damageCollider == null) return;

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
        yield return null; 

        float duration = animator != null ? animator.GetCurrentAnimatorStateInfo(0).length : 0.25f;
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        _despawnRoutine = null;
        PoolingManager.Despawn(gameObject);
    }

    private void OnDisable()
    {
        _flyTween?.Kill();
        _flyTween = null;

        if (_despawnRoutine != null)
        {
            StopCoroutine(_despawnRoutine);
            _despawnRoutine = null;
        }
    }
}
