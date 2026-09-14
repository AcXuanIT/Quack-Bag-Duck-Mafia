using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform filterRect;
    [SerializeField] private TextMeshProUGUI textLoad;

    [Header("Settings")]
    [SerializeField] private float loadingDuration = 3f;

    private float _fullWidth;
    private bool _isLoading = false;

    private void Awake()
    {
        if (filterRect == null) return;

        _fullWidth = filterRect.sizeDelta.x;

        Vector2 oldPivot = filterRect.pivot;
        Vector2 newPivot = new Vector2(0f, 0.5f);
        float pivotDeltaX = (newPivot.x - oldPivot.x) * _fullWidth;
        filterRect.pivot = newPivot;
        filterRect.anchoredPosition += new Vector2(pivotDeltaX, 0f);

        filterRect.sizeDelta = new Vector2(0f, filterRect.sizeDelta.y);

        if (textLoad != null)
            textLoad.text = "0%";
    }

    public void StartLoading(System.Action onComplete = null)
    {
        if (_isLoading) return;
        StartCoroutine(LoadingRoutine(onComplete));
    }

    private IEnumerator LoadingRoutine(System.Action onComplete)
    {
        _isLoading = true;
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDuration);
            SetProgress(progress);
            yield return null;
        }

        SetProgress(1f);

        _isLoading = false;
        onComplete?.Invoke();
    }

    private void SetProgress(float t)
    {
        if (filterRect != null)
        {
            filterRect.sizeDelta = new Vector2(Mathf.Lerp(0f, _fullWidth, t), filterRect.sizeDelta.y);
        }

        if (textLoad != null)
            textLoad.text = Mathf.RoundToInt(t * 100f) + "%";
    }
}
