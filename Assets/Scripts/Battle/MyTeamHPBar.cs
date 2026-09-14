using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyTeamHPBar : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI textHP;

    private RectTransform _fillRT;
    private float _maxWidth; 

    private float _baseHP;
    private float _currentHP;

    public float BaseHP    => _baseHP;
    public float CurrentHP => _currentHP;

    private void Awake()
    {
        if (fillImage != null)
        {
            _fillRT = fillImage.rectTransform;

            var parentRT     = _fillRT.parent as RectTransform;
            float parentWidth = parentRT != null ? parentRT.rect.width : 0f;

            float oldAnchorMinX = _fillRT.anchorMin.x;
            float oldOffsetMinX = _fillRT.offsetMin.x;
            float oldWidth      = _fillRT.rect.width; 

            float trueLeftEdgeX = oldAnchorMinX * parentWidth + oldOffsetMinX;

            _fillRT.anchorMin = new Vector2(0f, _fillRT.anchorMin.y);
            _fillRT.anchorMax = new Vector2(0f, _fillRT.anchorMax.y);
            _fillRT.pivot     = new Vector2(0f, _fillRT.pivot.y);

            fillImage.type = Image.Type.Sliced;

            _fillRT.sizeDelta        = new Vector2(oldWidth, _fillRT.sizeDelta.y);
            _fillRT.anchoredPosition = new Vector2(trueLeftEdgeX, _fillRT.anchoredPosition.y);

            _maxWidth = oldWidth;
        }
    }

    public void Init(float baseHP)
    {
        _baseHP    = Mathf.Max(0f, baseHP);
        _currentHP = _baseHP;
        Refresh();
    }

    public void UpdateHP(float currentHP)
    {
        _currentHP = Mathf.Clamp(currentHP, 0f, _baseHP);
        Refresh();
    }

    private void Refresh()
    {
        float ratio = _baseHP > 0f ? _currentHP / _baseHP : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (_fillRT != null)
        {
            var size = _fillRT.sizeDelta;
            size.x = _maxWidth * ratio;
            _fillRT.sizeDelta = size;
        }

        if (textHP != null)
            textHP.text = FormatHP(_currentHP);
    }
    private string FormatHP(float hp)
    {
        if (hp >= 1000000f)
            return (hp / 1000000f).ToString("0.#") + "m";

        if (hp > 999f)
            return (hp / 1000f).ToString("0.#") + "k";

        return Mathf.RoundToInt(hp).ToString();
    }
}
