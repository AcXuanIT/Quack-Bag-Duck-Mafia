using System;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class BattleMapUI : MonoBehaviour
{
    [Header("=== Tham Chiếu UI ===")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== Hiệu Ứng Vào ===")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 0.4f;

    [SerializeField] private TextMeshProUGUI waveText;

    [Header("=== Player Money ===")]
    [SerializeField] private TextMeshProUGUI textPlayerMoney;

    [Header("=== Power ===")]
    [SerializeField] private TextMeshProUGUI textPower;

    [SerializeField] private float powerCountDuration = 0.5f;

    [Header("=== Kết Quả Trận Đấu ===")]
    [SerializeField] private GameObject panelUIWin;
    [SerializeField] private GameObject panelUILose;
    [SerializeField] private TextMeshProUGUI textCoinWin;
    [SerializeField] private TextMeshProUGUI textRubyWin;
    [SerializeField] private TextMeshProUGUI textCoinLose;
    [SerializeField] private TextMeshProUGUI textRubyLose;

    private int _displayedPower;
    private Tween _powerCountTween;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (panelUIWin != null) panelUIWin.SetActive(false);
        if (panelUILose != null) panelUILose.SetActive(false);
    }

    private void OnDestroy()
    {
        _powerCountTween?.Kill();
    }

    public void Show(Action onComplete = null)
    {
        gameObject.SetActive(true);

        if (useFadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void UpdateTextWave(int waveIndex)
    {
        if (waveText == null) return;

        var mapData = BattleManager.Instance != null ? BattleManager.Instance.CurrentMapBattleData : null;
        if (mapData == null || mapData.Waves == null)
        {
            waveText.text = $"Vòng: {waveIndex}";
            return;
        }

        waveText.text = $"Vòng: {waveIndex}/{mapData.Waves.Length}";
    }

    public void UpdateTextPlayerMoney(int money)
    {
        if (textPlayerMoney == null) return;

        textPlayerMoney.text = money.ToString();
    }
    public void UpdateTextPower(int power)
    {
        if (textPower == null) return;

        _powerCountTween?.Kill();

        int from = _displayedPower;
        _displayedPower = power;

        _powerCountTween = DOTween.To(() => from, v =>
            {
                from = v;
                textPower.text = v.ToString();
            }, power, powerCountDuration)
            .SetEase(Ease.OutQuad);
    }
    public void ShowWin()
    {
        if (panelUILose != null) panelUILose.SetActive(false);
        if (panelUIWin != null) panelUIWin.SetActive(true);

        if (BattleManager.Instance != null)
        {
            if (textCoinWin != null) textCoinWin.text = BattleManager.Instance.EarnedCoin.ToString();
            if (textRubyWin != null) textRubyWin.text = BattleManager.Instance.EarnedRuby.ToString();
        }
    }
    public void ShowLose()
    {
        if (panelUIWin != null) panelUIWin.SetActive(false);
        if (panelUILose != null) panelUILose.SetActive(true);

        if (BattleManager.Instance != null)
        {
            if (textCoinLose != null) textCoinLose.text = BattleManager.Instance.EarnedCoin.ToString();
            if (textRubyLose != null) textRubyLose.text = BattleManager.Instance.EarnedRuby.ToString();
        }
    }

    public void HideResultPanels()
    {
        if (panelUIWin != null) panelUIWin.SetActive(false);
        if (panelUILose != null) panelUILose.SetActive(false);
    }
}
