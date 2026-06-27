using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller cho UI Gear Panel.
/// Gắn vào GameObject "Gear" tại UIGame/StartGame/MenuGame/MenuMid/Gear.
///
/// Khi Gear được bật (OnEnable): populate cả 2 Grid (SectionCurrent / SectionAll)
/// từ WeaponData. Mỗi slot trong SectionCurrent là Pistol_Slot với WeaponSlotGridUI.
/// Khi click slot → mở WeaponInfoUI.
/// </summary>
public class GearPanelUI : MonoBehaviour
{
    [Header("Data")]
    public WeaponData weaponDatabase;

    [Header("Grid - SectionCurrent (vũ khí đã mở khóa)")]
    public Transform  currentGrid;          // SectionCurrent/Grid
    public GameObject currentSlotPrefab;    // Pistol_Slot prefab có WeaponSlotGridUI

    [Header("Grid - SectionAll (tất cả vũ khí còn lại)")]
    public Transform  allGrid;              // SectionAll/Grid
    public GameObject allSlotPrefab;        // Pistol_Slot prefab cho SectionAll

    [Header("WeaponInfo Panel")]
    public WeaponInfoUI weaponInfoUI;        // UIGame/StartGame/MenuGame/WeaponInfo

    void OnEnable()  => RefreshUI();

    public void RefreshUI()
    {
        if (weaponDatabase == null) return;

        ClearGrid(currentGrid);
        ClearGrid(allGrid);

        var unlockedList = new List<WeaponEntry>();
        var lockedList   = new List<WeaponEntry>();

        foreach (var w in weaponDatabase.Weapons)
        {
            if (!w.IsLocked) unlockedList.Add(w);
            else             lockedList.Add(w);
        }

        foreach (var w in unlockedList) SpawnCurrentSlot(w);
        foreach (var w in lockedList)   SpawnAllSlot(w);

        // Rebuild layout sau khi spawn xong để ContentSizeFitter + VerticalLayoutGroup tính đúng chiều cao
        StartCoroutine(RebuildLayoutNextFrame());
    }

    IEnumerator RebuildLayoutNextFrame()
    {
        // Chờ 1 frame để Unity hoàn tất việc khởi tạo các slot mới
        yield return null;

        Canvas.ForceUpdateCanvases();

        // Rebuild từ trong ra ngoài
        if (currentGrid != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentGrid.GetComponent<RectTransform>());
        if (allGrid != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(allGrid.GetComponent<RectTransform>());

        var sectionCurrentRT = currentGrid != null ? currentGrid.parent?.GetComponent<RectTransform>() : null;
        var sectionAllRT     = allGrid     != null ? allGrid.parent?.GetComponent<RectTransform>()     : null;
        if (sectionCurrentRT) LayoutRebuilder.ForceRebuildLayoutImmediate(sectionCurrentRT);
        if (sectionAllRT)     LayoutRebuilder.ForceRebuildLayoutImmediate(sectionAllRT);

        var contentRT = sectionCurrentRT != null ? sectionCurrentRT.parent?.GetComponent<RectTransform>() : null;
        if (contentRT) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
    }

    void SpawnCurrentSlot(WeaponEntry data)
    {
        if (currentGrid == null || currentSlotPrefab == null) return;
        GameObject go = Instantiate(currentSlotPrefab, currentGrid);
        WeaponSlotGridUI slot = go.GetComponent<WeaponSlotGridUI>();
        if (slot != null) slot.Bind(data, weaponInfoUI);
    }

    void SpawnAllSlot(WeaponEntry data)
    {
        if (allGrid == null || allSlotPrefab == null) return;
        GameObject go = Instantiate(allSlotPrefab, allGrid);

        // Icon: dùng SpriteTier1 (icon mặc định UIGear)
        var iconImg = go.transform.Find("IconWaepon");
        if (iconImg != null)
        {
            var img = iconImg.GetComponent<Image>();
            if (img != null) img.sprite = data.GetUIIcon();
        }

        // Level text: ẩn nếu chưa mở khoá
        var lvTxtT = go.transform.Find("LevelText");
        if (lvTxtT != null)
        {
            var lvTxt = lvTxtT.GetComponent<TextMeshProUGUI>();
            if (lvTxt != null) lvTxt.text = data.IsLocked ? "Mở khóa ở cấp độ " : ("Cấp " + data.Level);
        }

        var btn = go.GetComponent<Button>();
        if (btn != null && weaponInfoUI != null)
        {
            WeaponEntry captured = data;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => weaponInfoUI.Show(captured));
        }
    }

    void ClearGrid(Transform grid)
    {
        if (grid == null) return;
        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);
    }
}
