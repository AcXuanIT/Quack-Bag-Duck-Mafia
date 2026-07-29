using UnityEngine;

/// <summary>
/// Lớp cơ sở cho dữ liệu 1 con vịt (Duck) trong game.
/// EnemyDuckData và MyDuckData sẽ kế thừa từ lớp này.
/// </summary>
[System.Serializable]
public class BaseDuckData
{
    [Header("=== Identity ===")]
    public int ID;
    public string Name;

    [Header("=== Tier ===")]
    [Range(1, 4)]
    public int Tier = 1;

    [Header("=== Stats ===")]
    public float BaseHP;
    public float BaseDamage;

    [Header("=== Sprite theo Tier ===")]
    public Sprite SpriteTier1;
    public Sprite SpriteTier2;
    public Sprite SpriteTier3;
    public Sprite SpriteTier4;

    [Header("=== Grid ===")]
    [Tooltip("Sprite dùng để hiển thị grid/khung nền của Duck")]
    public Sprite GridSprite;

    [Header("=== Color theo Tier ===")]
    [Tooltip("Màu gốc - Tier 1")]
    public Color ColorBase = Color.white;
    [Tooltip("Màu xanh Blue - Tier 2")]
    public Color ColorBlue = Color.blue;
    [Tooltip("Màu tím - Tier 3")]
    public Color ColorPurple = new Color(0.6f, 0.2f, 0.8f);
    [Tooltip("Màu vàng - Tier 4")]
    public Color ColorYellow = Color.yellow;

    // ─── Public API ───────────────────────────────────────────

    public virtual Sprite GetSprite(int tier)
    {
        switch (tier)
        {
            case 4:  return SpriteTier4 != null ? SpriteTier4 : GetSprite(3);
            case 3:  return SpriteTier3 != null ? SpriteTier3 : GetSprite(2);
            case 2:  return SpriteTier2 != null ? SpriteTier2 : SpriteTier1;
            default: return SpriteTier1;
        }
    }

    public virtual Sprite GetDefaultIcon()
    {
        if (SpriteTier1 != null) return SpriteTier1;
        if (SpriteTier2 != null) return SpriteTier2;
        if (SpriteTier3 != null) return SpriteTier3;
        return SpriteTier4;
    }

    /// <summary>Lấy màu tương ứng theo Tier (1=gốc, 2=blue, 3=purple, 4=yellow).</summary>
    public virtual Color GetColor(int tier)
    {
        switch (tier)
        {
            case 4:  return ColorYellow;
            case 3:  return ColorPurple;
            case 2:  return ColorBlue;
            default: return ColorBase;
        }
    }
}
