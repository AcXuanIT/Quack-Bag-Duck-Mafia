using UnityEngine;

public enum WeaponCategory
{
    Ranged,
    Melee,
    Thrown,
    Boom,
}

[System.Serializable]
public class WeaponGridCell
{
    [Tooltip("Vị trí ô trong grid (cột, hàng)")]
    public Vector2Int gridPosition;

    [Tooltip("True = ô này đang bị chiếm bởi weapon")]
    public bool isOccupied;
}

[System.Serializable]
public class WeaponEntry
{
    [Header("=== Identity ===")]
    public int    ID;
    public string Name;

    [Header("=== Category (Attack Animation) ===")]
    public WeaponCategory Category = WeaponCategory.Ranged;

    [Header("=== Tier Sprites (tier 1-4) ===")]
    [Tooltip("Sprite tier 1 — icon mặc định trên UIGear")]
    public Sprite SpriteTier1;

    [Tooltip("Sprite tier 2")]
    public Sprite SpriteTier2;

    [Tooltip("Sprite tier 3")]
    public Sprite SpriteTier3;

    [Tooltip("Sprite tier 4")]
    public Sprite SpriteTier4;

    [Header("=== Shape & Grid ===")]
    [Tooltip("Sprite khung hình dạng của weapon")]
    public Sprite ShapeSprite;

    public Sprite ShapeFill;

    [Tooltip("Các ô grid mà weapon chiếm khi gắn vào lưới")]
    public WeaponGridCell[] GridCells;

    [Header("=== Level (1-5) ===")]
    [Range(1, 5)]
    public int Level = 1;

    public int XP;

    public int[] XPToNextLevel = new int[5];

    public float[] DamagePerLevel = new float[5];

    public float[] HPPerLevel = new float[5];

    public int[] Power = new int[5];

    [Header("=== Combat ===")]
    public float AttackRange = 1f;

    [Header("=== Unlock ===")]
    [Min(0)]
    public int LevelLock;

    [Header("=== Upgrade ===")]
    public int Coin;

    [Tooltip("Chưa mở khoá = true")]
    public bool IsLocked;

    [Header("=== Spawn ===")]
    public float TimeDelay;

    [Header("=== Attack ===")]
    public float TimeAttack = 1f;

    [Header("=== VFX ===")]
    public GameObject vfxWeapon;

    public float GetDamage(int level)
    {
        if (DamagePerLevel == null || DamagePerLevel.Length < 5) return 0f;
        return DamagePerLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    public float GetCurrentDamage() => GetDamage(Level);

    public float GetNextLevelDamage()
    {
        if (Level >= 5) return GetCurrentDamage();
        return GetDamage(Level + 1);
    }

    public float GetHP(int level)
    {
        if (HPPerLevel == null || HPPerLevel.Length < 5) return 0f;
        return HPPerLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    public float GetCurrentHP() => GetHP(Level);

    public float GetNextLevelHP()
    {
        if (Level >= 5) return GetCurrentHP();
        return GetHP(Level + 1);
    }

  
    public int GetPower(int level)
    {
        if (Power == null || Power.Length < 5) return 0;
        return Power[Mathf.Clamp(level - 1, 0, 4)];
    }

    public int GetCurrentPower() => GetPower(Level);

    public int GetNextLevelPower()
    {
        if (Level >= 5) return GetCurrentPower();
        return GetPower(Level + 1);
    }
    public int GetXPToNextLevel(int level)
    {
        if (XPToNextLevel == null || XPToNextLevel.Length < 5) return 0;
        return XPToNextLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    public int GetCurrentXPToNextLevel() => Level >= 5 ? 0 : GetXPToNextLevel(Level);

    public void AddXP(int amount)
    {
        if (amount <= 0 || Level >= 5) return;

        XP += amount;

        while (Level < 5)
        {
            int needed = GetXPToNextLevel(Level);
            if (needed <= 0 || XP < needed) break;

            XP -= needed;
            Level++;
        }
    }

    public float GetAttackRange() => AttackRange;

    public Sprite GetSpriteByTier(int tier)
    {
        switch (tier)
        {
            case 2:  return SpriteTier2 != null ? SpriteTier2 : SpriteTier1;
            case 3:  return SpriteTier3 != null ? SpriteTier3 : SpriteTier1;
            case 4:  return SpriteTier4 != null ? SpriteTier4 : SpriteTier1;
            default: return SpriteTier1;
        }
    }

    public Sprite GetUIIcon() => SpriteTier1;

    public bool IsGridPositionOccupied(Vector2Int position)
    {
        if (GridCells == null) return false;
        foreach (var cell in GridCells)
            if (cell.isOccupied && cell.gridPosition == position)
                return true;
        return false;
    }

    public void OccupyCell(Vector2Int position)
    {
        if (GridCells == null) return;
        foreach (var cell in GridCells)
            if (cell.gridPosition == position) { cell.isOccupied = true; return; }
    }

    public void ReleaseAllCells()
    {
        if (GridCells == null) return;
        foreach (var cell in GridCells)
            cell.isOccupied = false;
    }

    public WeaponEntry Clone()
    {
        return new WeaponEntry
        {
            ID = ID,
            Name = Name,
            Category = Category,
            SpriteTier1 = SpriteTier1,
            SpriteTier2 = SpriteTier2,
            SpriteTier3 = SpriteTier3,
            SpriteTier4 = SpriteTier4,
            ShapeSprite = ShapeSprite,
            ShapeFill = ShapeFill,
            GridCells = GridCells,
            Level = Level,
            XP = XP,
            XPToNextLevel = (int[])XPToNextLevel?.Clone(),
            DamagePerLevel = (float[])DamagePerLevel?.Clone(),
            HPPerLevel = (float[])HPPerLevel?.Clone(),
            Power = (int[])Power?.Clone(),
            AttackRange = AttackRange,
            LevelLock = LevelLock,
            Coin = Coin,
            IsLocked = IsLocked,
            TimeDelay = TimeDelay,
            TimeAttack = TimeAttack,
            vfxWeapon = vfxWeapon,
        };
    }
}

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponData : ScriptableObject
{
    public WeaponDataAsset[] Weapons;

    public WeaponEntry[] GetEntries()
    {
        if (Weapons == null) return new WeaponEntry[0];
        var list = new System.Collections.Generic.List<WeaponEntry>(Weapons.Length);
        foreach (var w in Weapons)
            if (w != null && w.Entry != null) list.Add(w.Entry);
        return list.ToArray();
    }
}
