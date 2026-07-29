using UnityEngine;

/// <summary>
/// Data của 1 Unit (vịt chiến đấu).
/// - ID, Name
/// - Máu cơ bản theo Tier 1-4
/// - Sprite theo Tier 1-4
/// </summary>
[System.Serializable]
public class UnitEntry
{
    // ─── Identity ────────────────────────────────────────────
    [Header("=== Identity ===" )]
    public int    ID;
    public string Name;

    // ─── Máu cơ bản theo Tier ────────────────────────────────
    [Header("=== Máu cơ bản (Tier 1 → 4) ===" )]
    public float BaseHP_Tier1;
    public float BaseHP_Tier2;
    public float BaseHP_Tier3;
    public float BaseHP_Tier4;

    // ─── Tier Sprites ────────────────────────────────────────
    [Header("=== Sprite theo Tier ===" )]
    public Sprite SpriteTier1;
    public Sprite SpriteTier2;
    public Sprite SpriteTier3;
    public Sprite SpriteTier4;

    // ─── Public API ───────────────────────────────────────────

    public float GetBaseHP(int tier)
    {
        switch (tier)
        {
            case 1:  return BaseHP_Tier1;
            case 2:  return BaseHP_Tier2;
            case 3:  return BaseHP_Tier3;
            case 4:  return BaseHP_Tier4;
            default: return BaseHP_Tier1;
        }
    }

    public Sprite GetSprite(int tier)
    {
        switch (tier)
        {
            case 4:  return SpriteTier4 != null ? SpriteTier4 : GetSprite(3);
            case 3:  return SpriteTier3 != null ? SpriteTier3 : GetSprite(2);
            case 2:  return SpriteTier2 != null ? SpriteTier2 : SpriteTier1;
            default: return SpriteTier1;
        }
    }

    public Sprite GetDefaultIcon()
    {
        if (SpriteTier1 != null) return SpriteTier1;
        if (SpriteTier2 != null) return SpriteTier2;
        if (SpriteTier3 != null) return SpriteTier3;
        return SpriteTier4;
    }
}

[CreateAssetMenu(fileName = "UnitDatabase", menuName = "Game/Unit Database")]
public class UnitData : ScriptableObject
{
    public UnitEntry[] Units;

    public UnitEntry GetByID(int id)
    {
        if (Units == null) return null;
        foreach (var u in Units)
            if (u != null && u.ID == id) return u;
        return null;
    }

    public UnitEntry GetByName(string unitName)
    {
        if (Units == null) return null;
        foreach (var u in Units)
            if (u != null && string.Equals(u.Name, unitName, System.StringComparison.OrdinalIgnoreCase))
                return u;
        return null;
    }
}
