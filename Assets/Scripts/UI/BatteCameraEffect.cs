using UnityEngine;
using DG.Tweening;

/// <summary>
/// BatteCameraEffect - Hiệu ứng camera "đi xuống & zoom ra" cho màn hình battle.
/// 
/// Cách hoạt động:
/// - Top và Bottom trong UIBatteMap: KHÔNG thay đổi (HUD cố định)
/// - Batte (panel trung tâm): di chuyển về giữa màn hình + phóng to → cảm giác camera zoom ra
/// - Camera.orthographicSize: tăng nhẹ để world objects cũng zoom ra theo
/// - Camera.position.y: giảm nhẹ để tạo cảm giác "đi xuống"
///
/// Chỉ ảnh hưởng đến: Batte (RectTransform), Main Camera (orthoSize + posY)
/// Không ảnh hưởng đến: Top, Bottom, và mọi UI khác
/// </summary>
public class BatteCameraEffect : MonoBehaviour
{
    [Header("--- References (tự động tìm nếu để trống) ---")]
    [SerializeField] private RectTransform batteRect;
    [SerializeField] private Camera mainCamera;

    [Header("--- Batte UI Animation ---")]
    [Tooltip("Vị trí Y đích của Batte (0 = giữa màn hình)")]
    [SerializeField] private float batteTargetY = 80f;
    [Tooltip("Scale đích của Batte khi zoom ra")]
    [SerializeField] private Vector3 batteTargetScale = new Vector3(1.25f, 1.25f, 1f);
    [SerializeField] private float batteDuration = 0.75f;
    [SerializeField] private Ease batteEase = Ease.OutCubic;

    [Header("--- Camera Animation ---")]
    [Tooltip("Camera đi xuống bao nhiêu world units")]
    [SerializeField] private float cameraPanDownY = -1.2f;
    [Tooltip("orthographicSize đích khi zoom ra (default = 5)")]
    [SerializeField] private float cameraZoomOutSize = 6.5f;
    [SerializeField] private float cameraDuration = 0.8f;
    [SerializeField] private Ease cameraEase = Ease.OutQuart;

    [Header("--- Timing ---")]
    [Tooltip("Delay trước khi bắt đầu hiệu ứng")]
    [SerializeField] private float startDelay = 0f;

    // --- Giá trị gốc để Reverse ---
    private Vector2 _batteOriginPos;
    private Vector3 _batteOriginScale;
    private Vector3 _cameraOriginPos;
    private float   _cameraOriginOrthoSize;

    private Sequence _seq;
    private bool _isAnimated = false;

    // ─────────────────────────────────────────────
    void Awake()
    {
        // Auto-find nếu chưa gán trong Inspector
        if (batteRect == null)
        {
            var batteGO = transform.Find("Batte");
            if (batteGO != null) batteRect = batteGO.GetComponent<RectTransform>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

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

    // ─────────────────────────────────────────────
    /// <summary>Phát hiệu ứng: Batte về giữa + zoom ra, Camera đi xuống & zoom ra</summary>
    [ContextMenu("Play Effect")]
    public void PlayEffect()
    {
        if (batteRect == null || mainCamera == null)
        {
            Debug.LogWarning("[BatteCameraEffect] Missing references!");
            return;
        }

        _seq?.Kill(true);
        _isAnimated = true;

        _seq = DOTween.Sequence();

        // === Batte UI: di chuyển về giữa + scale lên ===
        Tween moveUI = batteRect
            .DOAnchorPosY(batteTargetY, batteDuration)
            .SetEase(batteEase);

        Tween scaleUI = batteRect
            .DOScale(batteTargetScale, batteDuration)
            .SetEase(batteEase);

        // === Camera: đi xuống + zoom ra ===
        Vector3 camTarget = new Vector3(
            _cameraOriginPos.x,
            _cameraOriginPos.y + cameraPanDownY,  // đi xuống
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

        // Tất cả chạy đồng thời sau delay
        _seq.SetDelay(startDelay);
        _seq.Join(moveUI);
        _seq.Join(scaleUI);
        _seq.Join(moveCam);
        _seq.Join(zoomCam);
    }

    // ─────────────────────────────────────────────
    /// <summary>Khôi phục về trạng thái ban đầu (dùng khi close panel)</summary>
    [ContextMenu("Reverse Effect")]
    public void ReverseEffect()
    {
        if (batteRect == null || mainCamera == null) return;

        _seq?.Kill(true);
        _isAnimated = false;

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

    // ─────────────────────────────────────────────
    /// <summary>Toggle Play / Reverse (tiện test)</summary>
    [ContextMenu("Toggle Effect")]
    public void ToggleEffect()
    {
        if (_isAnimated) ReverseEffect();
        else PlayEffect();
    }

    void OnDestroy()
    {
        _seq?.Kill();
    }
}
