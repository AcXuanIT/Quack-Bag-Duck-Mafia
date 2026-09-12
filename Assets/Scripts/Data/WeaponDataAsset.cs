using UnityEngine;

/// <summary>
/// Asset gốc DUY NHẤT cho 1 weapon (Entry). Tách riêng file này (thay vì định nghĩa chung
/// trong WeaponData.cs) để Unity gán fileID ổn định vĩnh viễn cho class này khi serialize
/// (m_Script trỏ đúng guid/fileID của script) — tránh lỗi "m_Script: {fileID: 0}" (chỉ đoán
/// type qua m_EditorClassIdentifier) từng khiến toàn bộ 13 file WeaponDataAsset bị gãy sau khi
/// build/recompile.
/// </summary>
[CreateAssetMenu(fileName = "WeaponDataAsset", menuName = "Game/Weapon Data Asset")]
public class WeaponDataAsset : ScriptableObject
{
    [Tooltip("Dữ liệu weapon — điền/sửa trực tiếp tại đây, mọi nơi tham chiếu asset này sẽ dùng chung nguồn dữ liệu.")]
    public WeaponEntry Entry;
}
