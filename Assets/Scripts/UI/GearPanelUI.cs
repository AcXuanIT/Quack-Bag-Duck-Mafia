using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller cho UI Gear Panel.
/// Gắn vào GameObject "Gear" tại UIGame/StartGame/MenuGame/MenuMid/Gear.
/// </summary>
public class GearPanelUI : MonoBehaviour
{
    [Header("Data")]
    public WeaponData weaponDatabase;

    [Header("Section - Current Gear (đã mở khóa)")]
    public Transform currentGearContent;    // ScrollRect content cho phần hiện tại
    public GameObject weaponSlotPrefab;

    [Header("Section - All Gear (tất cả còn lại)")]
    public Transform allGearContent;        // ScrollRect content cho phần tất cả

    [Header("Section Labels")]
    public TextMeshProUGUI currentGearLabel;
    public TextMeshProUGUI allGearLabel;

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (weaponDatabase == null || weaponSlotPrefab == null) return;

        // Clear old slots
        ClearContent(currentGearContent);
        ClearContent(allGearContent);

        var unlockedList = new List<WeaponEntry>();
        var lockedList   = new List<WeaponEntry>();

        foreach (var w in weaponDatabase.Weapons)
        {
            if (!w.IsLocked) unlockedList.Add(w);
            else             lockedList.Add(w);
        }

        // Section tiêu đề
        if (currentGearLabel) currentGearLabel.text = $"Trang Bị Hiện Tại ({unlockedList.Count})";
        if (allGearLabel)     allGearLabel.text     = $"Tất Cả Trang Bị ({lockedList.Count})";

        // Spawn unlocked weapons
        foreach (var w in unlockedList)
            SpawnSlot(currentGearContent, w);

        // Spawn locked weapons
        foreach (var w in lockedList)
            SpawnSlot(allGearContent, w);
    }

    void SpawnSlot(Transform parent, WeaponEntry data)
    {
        var go = Instantiate(weaponSlotPrefab, parent);
        var slot = go.GetComponent<WeaponSlotUI>();
        if (slot) slot.Bind(data);
    }

    void ClearContent(Transform content)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }
}
