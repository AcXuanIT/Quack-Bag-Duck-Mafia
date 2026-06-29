using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// HP Bar cho unit dung Canvas Image (fillAmount).
/// Gan vao GO HPBar (child cua unit).
/// - FillGreen : tut tuc thi khi nhan damage
/// - FillWhite : tut tu tu ve vi tri cua Green (delay roi chase)
/// - An toan bo khi HP = 100%, hien khi HP < 100%
/// </summary>
public class UnitHPBar : MonoBehaviour
{
    [Header("Fill Images (dung fillAmount de fill)")]
    [SerializeField] private Image fillGreen;   // tut tuc thi
    [SerializeField] private Image fillWhite;   // tut tu tu theo sau

    [Header("White Chase Settings")]
    [SerializeField] private float chaseDelay    = 0.4f;
    [SerializeField] private float chaseDuration = 0.5f;
    [SerializeField] private Ease  chaseEase     = Ease.InOutQuad;

    private float _currentHP = 1f;   // 0..1
    private Tween _whiteTween;

    private void Awake()
    {
        SetFill(fillGreen, 1f);
        SetFill(fillWhite, 1f);
        gameObject.SetActive(false); // an luc dau (HP day)
    }

    /// <summary>
    /// Goi khi unit nhan damage.
    /// hpRatio: gia tri HP moi, 0..1
    /// </summary>
    public void SetHP(float hpRatio)
    {
        hpRatio = Mathf.Clamp01(hpRatio);
        _currentHP = hpRatio;

        bool full = Mathf.Approximately(hpRatio, 1f);
        gameObject.SetActive(!full);
        if (full) return;

        // Green tut tuc thi
        SetFill(fillGreen, hpRatio);

        // White: cancel tween cu, doi roi chase
        _whiteTween?.Kill();
        _whiteTween = DOVirtual.DelayedCall(chaseDelay, () =>
        {
            if (fillWhite == null) return;
            float from = fillWhite.fillAmount;
            DOTween.To(() => from, v => {
                from = v;
                SetFill(fillWhite, v);
            }, hpRatio, chaseDuration).SetEase(chaseEase);
        });
    }

    /// <summary>Khi heal, White va Green deu len ngay.</summary>
    public void Heal(float hpRatio)
    {
        hpRatio = Mathf.Clamp01(hpRatio);
        _currentHP = hpRatio;
        _whiteTween?.Kill();
        SetFill(fillGreen, hpRatio);
        SetFill(fillWhite, hpRatio);
        gameObject.SetActive(!Mathf.Approximately(hpRatio, 1f));
    }

    private void SetFill(Image img, float ratio)
    {
        if (img == null) return;
        img.fillAmount = Mathf.Clamp01(ratio);
    }

    private void OnDestroy() => _whiteTween?.Kill();

#if UNITY_EDITOR
    [Header("[Editor Test]")]
    [Range(0f, 1f)] public float testHP = 1f;
    private float _lastTest = 1f;
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (!Mathf.Approximately(testHP, _lastTest))
        {
            _lastTest = testHP;
            SetHP(testHP);
        }
    }
#endif
}
