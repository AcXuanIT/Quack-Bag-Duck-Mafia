using UnityEngine;
using DG.Tweening;

/// <summary>
/// Sequence: (1,1) -> stretch -> (1,1) -> squash -> (1,1) -> lap lai
/// Moi buoc deu di tu (1,1) nen khong bao gio nhat scale.
/// Delta +- 0.05 so voi base.
/// </summary>
public class UnitMoveAnimation : MonoBehaviour
{
    [Header("Timing")]
    public float stepDuration = 0.22f;
    public Ease  stepEase     = Ease.InOutSine;
    public float pauseDelay   = 0.05f;

    private Sequence _seq;
    private Vector3  _base;

    private void OnEnable()
    {
        _base = transform.localScale;
        PlayLoop();
    }

    private void OnDisable()
    {
        _seq?.Kill();
        transform.localScale = _base;
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
}
