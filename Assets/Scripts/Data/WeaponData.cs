using UnityEngine;

/// <summary>
/// Phân loại weapon để chọn ĐÚNG kiểu animation tấn công (xem Duck.PlayAttackAnimation()):
///   Ranged = tấn công tầm xa (súng/laser/...) -> animation giật lùi (recoil).
///   Melee  = cận chiến (kiếm/dao/...)         -> animation vung chém (swing).
///   Thrown = ném weapon thẳng vào enemy (bom/dao ném/lựu đạn/...) -> animation vung tay ném
///            (windup ra sau rồi bung tay về phía trước). VD: Weapon_4_Sniper_SR_1 (bom ném).
///   Boom   = weapon phát nổ tại chỗ (mìn/pháo/bom tự kích nổ/...) -> animation Weapon_Attack_Boom.
/// </summary>
public enum WeaponCategory
{
    Ranged,
    Melee,
    Thrown,
    Boom,
}

/// <summary>
/// Lưu trạng thái của một ô (cell) trong grid khi weapon được gắn vào.
/// </summary>
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
    [Tooltip("Ranged = animation giật lùi (recoil). Melee = animation vung chém (swing). " +
             "Thrown = animation vung tay ném weapon vào enemy (bom/dao ném/...). " +
             "Xem Duck.PlayAttackAnimation().")]
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

    [Tooltip("XP hiện tại — mỗi khi weapon này (do UnitDuck cầm) tiêu diệt 1 EnemyDuck thì +1 " +
             "(xem Duck.OnKilledTarget()). Khi XP >= XPToNextLevel[Level-1] thì tự động lên Level.")]
    public int XP;

    [Tooltip("XP cần để lên level tiếp theo, theo TỪNG Level — index 0=cần để Lv1→Lv2, " +
             "1=Lv2→Lv3, 2=Lv3→Lv4, 3=Lv4→Lv5, 4=Lv5 (đã max, không dùng/để 0). " +
             "Cùng quy ước index với DamagePerLevel/HPPerLevel bên dưới.")]
    public int[] XPToNextLevel = new int[5];

    [Header("=== Stats per Level (index 0=Lv1 … 4=Lv5) ===")]
    [Tooltip("Damage tại mỗi Level — index 0=Lv1, 1=Lv2, 2=Lv3, 3=Lv4, 4=Lv5")]
    public float[] DamagePerLevel = new float[5];

    [Tooltip("HP tại mỗi Level — index 0=Lv1, 1=Lv2, 2=Lv3, 3=Lv4, 4=Lv5")]
    public float[] HPPerLevel = new float[5];

    [Tooltip("Power (điểm sức mạnh) tại mỗi Level — index 0=Lv1, 1=Lv2, 2=Lv3, 3=Lv4, 4=Lv5. " +
             "Cùng quy ước index với DamagePerLevel/HPPerLevel ở trên. Dùng để đánh giá/so sánh " +
             "sức mạnh tổng thể của weapon (VD hiển thị UI, xếp hạng, cân bằng Shop).")]
    public int[] Power = new int[5];

    [Header("=== Combat ===")]
    [Tooltip("Phạm vi tấn công của weapon (khoảng cách/bán kính có thể đánh trúng mục tiêu)")]
    public float AttackRange = 1f;

    [Header("=== Unlock ===")]
    [Tooltip("Level Player tối thiểu để mở khóa weapon này (0 = không yêu cầu)")]
    [Min(0)]
    public int LevelLock;

    [Header("=== Upgrade ===")]
    [Tooltip("Coin cần để nâng level khi XP đầy")]
    public int Coin;

    [Tooltip("Chưa mở khoá = true")]
    public bool IsLocked;

    [Header("=== Spawn ===")]
    [Tooltip("Chu kỳ (giây) giữa mỗi lần weapon này spawn 1 UnitDuck trên Grid (BattleSpawnDuck) " +
             "— KHÔNG liên quan tới tốc độ tấn công, xem TimeAttack bên dưới.")]
    public float TimeDelay;

    [Header("=== Attack ===")]
    [Tooltip("Tốc độ tấn công (giây/đòn) của UnitDuck/EnemyDuck khi cầm weapon này " +
             "— dùng trong Duck.UpdateAttack() để tính chu kỳ ra đòn.")]
    public float TimeAttack = 1f;

    [Header("=== VFX ===")]
    [Tooltip("Prefab VFX (nên có component VFXGun) được Duck.SpawnWeaponVFX() Spawn qua " +
             "PoolingManager tại vị trí posVFX (child \"Weapon/PosVFX\") mỗi khi Duck ra đòn " +
             "(xem Duck.PlayAttackAnimation()). Để trống (null) nếu weapon này không có VFX riêng.")]
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

    /// <summary>Power (điểm sức mạnh) tại 1 Level bất kỳ (1-5, tự Clamp).</summary>
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

    /// <summary>XP cần để lên level tiếp theo, tính từ 1 Level bất kỳ (1-5, tự Clamp).</summary>
    public int GetXPToNextLevel(int level)
    {
        if (XPToNextLevel == null || XPToNextLevel.Length < 5) return 0;
        return XPToNextLevel[Mathf.Clamp(level - 1, 0, 4)];
    }

    /// <summary>XP cần để lên level tiếp theo, tính từ Level hiện tại (0 nếu đã max Level 5).</summary>
    public int GetCurrentXPToNextLevel() => Level >= 5 ? 0 : GetXPToNextLevel(Level);

    /// <summary>
    /// Cộng thêm XP (VD: +1 mỗi khi weapon này tiêu diệt 1 EnemyDuck — xem Duck.OnKilledTarget())
    /// và tự động tăng Level nếu XP đã đủ ngưỡng GetXPToNextLevel(Level). Có thể tăng nhiều Level
    /// liên tiếp trong 1 lần gọi nếu amount đủ lớn. Không làm gì nếu đã Level tối đa (5) hoặc
    /// amount <= 0.
    /// </summary>
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

/// <summary>
/// Database tổng cho toàn bộ vũ khí của Player.
///
/// Weapons là WeaponDataAsset[] — MỖI weapon chỉ có DUY NHẤT 1 file .asset làm nguồn dữ
/// liệu gốc (xem WeaponDataAsset.cs), được cả Player (qua database này) và Enemy (qua
/// EnemyDuckData.weaponAsset) cùng tham chiếu tới. Sửa 1 weapon ở 1 nơi (VD WeaponEditorWindow)
/// sẽ áp dụng cho cả 2 phía.
/// </summary>
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponData : ScriptableObject
{
    public WeaponDataAsset[] Weapons;

    /// <summary>
    /// Trả về toàn bộ WeaponEntry (dereference qua từng WeaponDataAsset), bỏ qua phần tử null
    /// hoặc Entry null. Dùng cho code chỉ cần đọc dữ liệu (không cần biết tới WeaponDataAsset).
    /// </summary>
    public WeaponEntry[] GetEntries()
    {
        if (Weapons == null) return new WeaponEntry[0];
        var list = new System.Collections.Generic.List<WeaponEntry>(Weapons.Length);
        foreach (var w in Weapons)
            if (w != null && w.Entry != null) list.Add(w.Entry);
        return list.ToArray();
    }
}
