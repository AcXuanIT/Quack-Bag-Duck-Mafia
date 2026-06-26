using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    public int ID;
    public string Name;
    public Sprite Icon;
    [Range(1, 5)] public int Level = 1;
    public int XP;          // số quái giết được bằng vũ khí này
    public int XPToNextLevel; // XP cần để lên level tiếp theo
    public float Damage;
    public float HP;
    public int Coin;        // coin cần để nâng level khi XP đầy
    public bool IsLocked;   // true = chưa mở khóa
}

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponData : ScriptableObject
{
    public WeaponEntry[] Weapons;
}
