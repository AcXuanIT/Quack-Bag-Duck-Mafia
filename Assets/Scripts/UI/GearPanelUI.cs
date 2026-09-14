using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearPanelUI : MonoBehaviour
{
    [Header("Data")]
    public WeaponData weaponDatabase;

    [Header("Grid - SectionCurrent")]
    public Transform  currentGrid;          
    public GameObject currentSlotPrefab;    

    [Header("Grid - SectionAll")]
    public Transform  allGrid;              
    public GameObject allSlotPrefab;        

    [Header("WeaponInfo Panel")]
    public WeaponInfoUI weaponInfoUI;        
    void OnEnable()
    {
        RefreshUI();
        WeaponManager.OnWeaponChanged += HandleWeaponChanged;
    }

    void OnDisable()
    {
        WeaponManager.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void HandleWeaponChanged(WeaponEntry changed) => RefreshUI();

    public void RefreshUI()
    {
        if (weaponDatabase == null) return;

        ClearGrid(currentGrid);
        ClearGrid(allGrid);

        var unlockedList = new List<WeaponEntry>();
        var lockedList   = new List<WeaponEntry>();

        foreach (var w in weaponDatabase.GetEntries())
        {
            if (!w.IsLocked) unlockedList.Add(w);
            else             lockedList.Add(w);
        }

        foreach (var w in unlockedList) SpawnCurrentSlot(w);
        foreach (var w in lockedList)   SpawnAllSlot(w);

        StartCoroutine(RebuildLayoutNextFrame());
    }

    IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

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

        var iconImg = go.transform.Find("IconWaepon");
        if (iconImg != null)
        {
            var img = iconImg.GetComponent<Image>();
            if (img != null) img.sprite = data.GetUIIcon();
        }

        var lvTxtT = go.transform.Find("LevelText");
        if (lvTxtT != null)
        {
            var lvTxt = lvTxtT.GetComponent<TextMeshProUGUI>();
            if (lvTxt != null) lvTxt.text = data.IsLocked ? "Mở khóa ở cấp độ " + data.LevelLock : ("Cấp " + data.Level);
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
