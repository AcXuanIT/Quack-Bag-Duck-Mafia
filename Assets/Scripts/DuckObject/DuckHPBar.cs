using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class DuckHPBar : MonoBehaviour
{
    [Header("Fill Images (dung fillAmount de fill)")]
    [SerializeField] private Image fillGreen;   
    [SerializeField] private Image fillWhite;   

    [Header("White Chase Settings")]
    [SerializeField] private float chaseDelay    = 0.4f;
    [SerializeField] private float chaseDuration = 0.5f;
    [SerializeField] private Ease  chaseEase     = Ease.InOutQuad;

    private float _currentHP = 1f;   
    private Tween _whiteTween;

    private void Awake()
    {
        SetFill(fillGreen, 1f);
        SetFill(fillWhite, 1f);
        gameObject.SetActive(false); 
    }

    public void SetHP(float hpRatio)
    {
        hpRatio = Mathf.Clamp01(hpRatio);
        _currentHP = hpRatio;

        bool full = Mathf.Approximately(hpRatio, 1f);
        gameObject.SetActive(!full);
        if (full) return;

        SetFill(fillGreen, hpRatio);

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
