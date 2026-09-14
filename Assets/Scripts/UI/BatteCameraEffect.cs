using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class BatteCameraEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform batteRect;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Button btnStartWar;
    [SerializeField] private GameObject compoentItem;

    [Header("--- Batte UI Animation ---")]
    [SerializeField] private float batteTargetY = 80f;
    [SerializeField] private Vector3 batteTargetScale = new Vector3(1.25f, 1.25f, 1f);
    [SerializeField] private float batteDuration = 0.75f;
    [SerializeField] private Ease batteEase = Ease.OutCubic;

    [Header("--- Camera Animation ---")]
    [SerializeField] private float cameraPanDownY = -1.2f;
    [SerializeField] private float cameraZoomOutSize = 6.5f;
    [SerializeField] private float cameraDuration = 0.8f;
    [SerializeField] private Ease cameraEase = Ease.OutQuart;

    [Header("--- Timing ---")]
    [SerializeField] private float startDelay = 0f;

    private Vector2 _batteOriginPos;
    private Vector3 _batteOriginScale;
    private Vector3 _cameraOriginPos;
    private float   _cameraOriginOrthoSize;

    private Sequence _seq;
    private bool _isAnimated = false;

    void Awake()
    {
        if (batteRect == null)
        {
            var batteGO = transform.Find("Batte");
            if (batteGO != null) batteRect = batteGO.GetComponent<RectTransform>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        btnStartWar.onClick.AddListener(() => {
            BattleManager.Instance.FinishTurnSetup();
        });

        CacheOriginalValues();
    }

    void CacheOriginalValues()
    {
        if (batteRect != null)
        {
            _batteOriginPos   = batteRect.anchoredPosition;
            _batteOriginScale = batteRect.localScale;
        }

        if (mainCamera != null)
        {
            _cameraOriginPos       = mainCamera.transform.position;
            _cameraOriginOrthoSize = mainCamera.orthographicSize;
        }
    }

    [ContextMenu("Play Effect")]
    public void PlayEffect()
    {
        if (batteRect == null || mainCamera == null)
        {
            return;
        }
        
        if (compoentItem != null)
            compoentItem.SetActive(true);

        _seq?.Kill(true);
        _isAnimated = true;

        _seq = DOTween.Sequence();

        Tween moveUI = batteRect
            .DOAnchorPosY(batteTargetY, batteDuration)
            .SetEase(batteEase);

        Tween scaleUI = batteRect
            .DOScale(batteTargetScale, batteDuration)
            .SetEase(batteEase);

        Vector3 camTarget = new Vector3(
            _cameraOriginPos.x,
            _cameraOriginPos.y + cameraPanDownY,  
            _cameraOriginPos.z
        );

        Tween moveCam = mainCamera.transform
            .DOMove(camTarget, cameraDuration)
            .SetEase(cameraEase);

        Tween zoomCam = DOTween
            .To(() => mainCamera.orthographicSize,
                x  => mainCamera.orthographicSize = x,
                cameraZoomOutSize,
                cameraDuration)
            .SetEase(cameraEase);

        _seq.SetDelay(startDelay);
        _seq.Join(moveUI);
        _seq.Join(scaleUI);
        _seq.Join(moveCam);
        _seq.Join(zoomCam);
    }

    [ContextMenu("Reverse Effect")]
    public void ReverseEffect()
    {
        if (batteRect == null || mainCamera == null) return;

        _seq?.Kill(true);
        _isAnimated = false;

        if(compoentItem != null) 
            compoentItem.SetActive(false);

        _seq = DOTween.Sequence();

        _seq.Join(batteRect
            .DOAnchorPos(_batteOriginPos, batteDuration * 0.8f)
            .SetEase(Ease.InCubic));

        _seq.Join(batteRect
            .DOScale(_batteOriginScale, batteDuration * 0.8f)
            .SetEase(Ease.InCubic));

        _seq.Join(mainCamera.transform
            .DOMove(_cameraOriginPos, cameraDuration * 0.8f)
            .SetEase(Ease.InCubic));

        _seq.Join(DOTween
            .To(() => mainCamera.orthographicSize,
                x  => mainCamera.orthographicSize = x,
                _cameraOriginOrthoSize,
                cameraDuration * 0.8f)
            .SetEase(Ease.InCubic));
    }

    [ContextMenu("Toggle Effect")]
    public void ToggleEffect()
    {
        if (_isAnimated) ReverseEffect();
        else PlayEffect();
    }

    [ContextMenu("Reset To Origin")]
    public void ResetToOrigin()
    {
        _seq?.Kill();
        _isAnimated = false;

        if (compoentItem != null)
            compoentItem.SetActive(false);

        if (batteRect != null)
        {
            batteRect.anchoredPosition = _batteOriginPos;
            batteRect.localScale       = _batteOriginScale;
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position = _cameraOriginPos;
            mainCamera.orthographicSize   = _cameraOriginOrthoSize;
        }
    }

    void OnDestroy()
    {
        _seq?.Kill();
    }
}
