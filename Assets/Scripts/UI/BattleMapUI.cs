using System;
using UnityEngine;
using DG.Tweening;
using TMPro;

/// <summary>
/// UI Controller cho màn hình BattleMap.
/// Được kích hoạt sau khi animation LoadMap hoàn thành.
/// Gắn vào: UIGame/StartGame/BatteMap
/// </summary>
public class BattleMapUI : MonoBehaviour
{
    [Header("=== Tham Chiếu UI ===")]
    [Tooltip("Canvas Group để fade in BattleMap UI (tuỳ chọn)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== Hiệu Ứng Vào ===")]
    [Tooltip("Dùng DOTween fade in khi mở BattleMapUI")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 0.4f;

    [SerializeField] private TextMeshProUGUI waveText;

    [Header("=== Player Money ===")]
    [Tooltip("Text hiển thị số tiền (money) hiện tại của Player — cập nhật qua UpdateTextPlayerMoney(), " +
             "gọi từ BattleManager mỗi khi playerMoney thay đổi (bắt đầu Battle = 50, +50 mỗi khi " +
             "thắng xong 1 Wave, hoặc khi mua item trong Shop).")]
    [SerializeField] private TextMeshProUGUI textPlayerMoney;

    [Header("=== Power ===")]
    [Tooltip("Text hiển thị điểm sức mạnh (Power) hiện tại — cập nhật qua UpdateTextPower(), " +
             "gọi mỗi khi tổng Power của đội hình/weapon Player thay đổi (VD: mua/nâng cấp Gear trong Shop, " +
             "đặt/gỡ item khỏi Battle Grid).")]
    [SerializeField] private TextMeshProUGUI textPower;

    [Tooltip("Thời gian (giây) để số hiển thị trên textPower chạy dần từ giá trị cũ tới giá trị mới " +
             "(dùng DOTween.To đếm số nguyên) — thay vì nhảy số đột ngột. Áp dụng cho cả tăng lẫn giảm.")]
    [SerializeField] private float powerCountDuration = 0.5f;

    [Header("=== Kết Quả Trận Đấu ===")]
    [Tooltip("Panel hiển thị khi thắng trận (BattleManager.BattleState.Win)")]
    [SerializeField] private GameObject panelUIWin;

    [Tooltip("Panel hiển thị khi thua trận (BattleManager.BattleState.Lose)")]
    [SerializeField] private GameObject panelUILose;

    [Tooltip("Text hiển thị số Coin đã kiếm được trong BattleMap hiện tại, nằm trong panelUIWin " +
             "(VD: UIWin/BG/CoinBG/Coin/text) — cập nhật từ BattleManager.EarnedCoin mỗi khi ShowWin() được gọi.")]
    [SerializeField] private TextMeshProUGUI textCoinWin;

    [Tooltip("Text hiển thị số Ruby đã kiếm được trong BattleMap hiện tại, nằm trong panelUIWin " +
             "(VD: UIWin/BG/CoinBG/Ruby/text) — cập nhật từ BattleManager.EarnedRuby mỗi khi ShowWin() được gọi.")]
    [SerializeField] private TextMeshProUGUI textRubyWin;

    [Tooltip("Text hiển thị số Coin đã kiếm được trong BattleMap hiện tại, nằm trong panelUILose " +
             "(VD: UILose/BG/CoinBG/Coin/text) — cập nhật từ BattleManager.EarnedCoin mỗi khi ShowLose() được gọi.")]
    [SerializeField] private TextMeshProUGUI textCoinLose;

    [Tooltip("Text hiển thị số Ruby đã kiếm được trong BattleMap hiện tại, nằm trong panelUILose " +
             "(VD: UILose/BG/CoinBG/Ruby/text) — cập nhật từ BattleManager.EarnedRuby mỗi khi ShowLose() được gọi.")]
    [SerializeField] private TextMeshProUGUI textRubyLose;

    // Giá trị Power đang hiển thị trên textPower (đích đến của tween gần nhất) — dùng làm điểm bắt
    // đầu (from) cho lần UpdateTextPower() kế tiếp, và điểm neo khi Kill() 1 tween đang chạy dở.
    private int _displayedPower;
    private Tween _powerCountTween;

    private void Awake()
    {
        // Đảm bảo UI ẩn ban đầu
        gameObject.SetActive(false);

        // Đảm bảo 2 panel kết quả luôn ẩn cho tới khi ShowWin()/ShowLose() được gọi
        if (panelUIWin != null) panelUIWin.SetActive(false);
        if (panelUILose != null) panelUILose.SetActive(false);
    }

    private void OnDestroy()
    {
        _powerCountTween?.Kill();
    }

    /// <summary>
    /// Mở BattleMap UI. Gọi từ UIGameManager sau LoadMap animation.
    /// onComplete được gọi khi hiệu ứng fade-in (Intro) kết thúc.
    /// </summary>
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

    /// <summary>
    /// Ẩn BattleMap UI.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Cập nhật text hiển thị Wave hiện tại. Gọi từ BattleManager mỗi khi bắt đầu 1 Wave mới
    /// (StartBattle() và mỗi lần vào TurnSetup của turn kế tiếp).
    ///
    /// Có null-guard cho CurrentMapBattleData/Waves: nếu BattleManager.StartBattle() được
    /// gọi mà quên SetMapBattleData() trước (và DataManager cũng chưa resolve được), text sẽ
    /// chỉ hiển thị "Vòng: {waveIndex}" thay vì crash NullReferenceException.
    /// </summary>
    public void UpdateTextWave(int waveIndex)
    {
        if (waveText == null) return;

        var mapData = BattleManager.Instance != null ? BattleManager.Instance.CurrentMapBattleData : null;
        if (mapData == null || mapData.Waves == null)
        {
            Debug.LogWarning("[BattleMapUI] CurrentMapBattleData/Waves chưa sẵn sàng khi UpdateTextWave() được gọi!");
            waveText.text = $"Vòng: {waveIndex}";
            return;
        }

        waveText.text = $"Vòng: {waveIndex}/{mapData.Waves.Length}";
    }

    /// <summary>
    /// Cập nhật text hiển thị số tiền (money) hiện tại của Player. Gọi từ BattleManager mỗi khi
    /// playerMoney thay đổi (StartBattle() = 50, +50 mỗi khi thắng xong 1 Wave, hoặc khi trừ tiền
    /// mua item trong Shop — xem BattleManager.SetMoney()/AddMoney()/SpendMoney()).
    /// </summary>
    public void UpdateTextPlayerMoney(int money)
    {
        if (textPlayerMoney == null) return;

        textPlayerMoney.text = money.ToString();
    }

    /// <summary>
    /// Cập nhật text hiển thị điểm sức mạnh (Power) hiện tại. Gọi mỗi khi tổng Power của
    /// đội hình/weapon Player thay đổi (VD: mua Gear mới trong Shop, đặt/gỡ item khỏi Battle Grid,
    /// weapon lên Level — xem WeaponEntry.GetCurrentPower()/GetPower()).
    ///
    /// KHÔNG set text ngay lập tức — dùng DOTween.To() để đếm số chạy dần từ giá trị đang hiển thị
    /// tới giá trị mới trong powerCountDuration giây (cả khi tăng lẫn khi giảm), tránh nhảy số
    /// đột ngột gây giật mắt. Nếu đang có 1 tween đếm số dở dang (VD Power đổi liên tục), tween cũ
    /// bị Kill() và tween mới bắt đầu từ đúng giá trị đang hiển thị tại thời điểm đó (không giật lùi).
    /// </summary>
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

    /// <summary>
    /// Hiện Panel UIWin — gọi từ BattleManager khi trận đấu chuyển sang BattleState.Win
    /// (đã đánh bại hết enemy của Wave cuối cùng). Ẩn Panel UILose để tránh hiện đè 2 panel.
    /// Cập nhật textCoinWin/textRubyWin theo BattleManager.EarnedCoin/EarnedRuby — LƯU Ý:
    /// BattleManager phải cộng thưởng coinPerMapWin/rubyPerMapWin vào earnedCoin/earnedRuby
    /// TRƯỚC khi gọi ShowWin(), nếu không số hiển thị sẽ thiếu phần thưởng thắng Map
    /// (xem BattleManager.FinishTurnBattle() case Win).
    /// </summary>
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

    /// <summary>
    /// Hiện Panel UILose — gọi từ BattleManager khi trận đấu chuyển sang BattleState.Lose
    /// (HP của MyTeam &lt;= 0). Ẩn Panel UIWin để tránh hiện đè 2 panel.
    /// Cập nhật textCoinLose/textRubyLose theo BattleManager.EarnedCoin/EarnedRuby (Coin/Ruby
    /// đã tích luỹ được từ các Wave đã vượt qua trước khi thua — không có thưởng thắng Map).
    /// </summary>
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

    /// <summary>
    /// Ẩn cả 2 panel kết quả — gọi khi bắt đầu lại 1 trận đấu mới (StartBattle()).
    /// </summary>
    public void HideResultPanels()
    {
        if (panelUIWin != null) panelUIWin.SetActive(false);
        if (panelUILose != null) panelUILose.SetActive(false);
    }
}
