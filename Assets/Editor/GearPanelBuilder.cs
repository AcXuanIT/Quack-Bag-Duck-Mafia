using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor tool – chạy 1 lần để xây toàn bộ GearPanel UI dưới Gear GameObject.
/// Menu: Tools > Build Gear Panel UI
/// </summary>
public static class GearPanelBuilder
{
    [MenuItem("Tools/Build Gear Panel UI")]
    public static void Build()
    {
        // ----- Tìm Gear -----
        GameObject gear = GameObject.Find("Gear");
        if (gear == null)
        {
            foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
                if (g.name == "Gear") { gear = g; break; }
        }
        if (gear == null) { Debug.LogError("Gear GameObject not found!"); return; }

        // Xóa các child cũ (trừ BG)
        for (int i = gear.transform.childCount - 1; i >= 0; i--)
        {
            var child = gear.transform.GetChild(i);
            if (child.name != "BG") Object.DestroyImmediate(child.gameObject);
        }

        // Load sprites
        Sprite sprBg        = Load<Sprite>("Assets/Sprite/ui_popup_shared_frame_inner_1.asset");
        Sprite sprSlotBg    = Load<Sprite>("Assets/Sprite/ui_gear_slot_bg.asset");
        Sprite sprSlotBanner= Load<Sprite>("Assets/Sprite/ui_gear_slot_banner.asset");
        Sprite sprBarFill   = Load<Sprite>("Assets/Sprite/ui_gear_bar_fill_green.asset");
        Sprite sprBar       = Load<Sprite>("Assets/Sprite/ui_gear_bar.asset");
        Sprite sprLock      = Load<Sprite>("Assets/Sprite/icon_shared_lock.asset");
        Sprite sprStar1     = Load<Sprite>("Assets/Sprite/ui_gear_star_tier_1.asset");
        Sprite sprStarGrey  = Load<Sprite>("Assets/Sprite/ui_gear_star_grey.asset");
        Sprite sprTitleCur  = Load<Sprite>("Assets/Sprite/ui_gear_title_current_gear.asset");
        Sprite sprTitleAll  = Load<Sprite>("Assets/Sprite/ui_gear_title_all_gear.asset");
        Sprite sprBtnClose  = Load<Sprite>("Assets/Sprite/ui_shared_btn_close.asset");
        Sprite sprDmg       = Load<Sprite>("Assets/Sprite/icon_damage.asset");
        Sprite sprHp        = Load<Sprite>("Assets/Sprite/icon_base_hp.asset");
        Sprite sprCoin      = Load<Sprite>("Assets/Sprite/icon_shared_attribute_coin.asset");
        Sprite sprExp       = Load<Sprite>("Assets/Sprite/icon_exp.asset");

        // Colors
        Color colorUnlockedBg   = new Color(0.12f, 0.35f, 0.65f, 1f);
        Color colorLockedBg     = new Color(0.22f, 0.22f, 0.25f, 1f);
        Color colorUnlockedFr   = new Color(0.30f, 0.75f, 1.00f, 1f);
        Color colorLockedFr     = new Color(0.40f, 0.40f, 0.42f, 1f);
        Color colorSectionBg    = new Color(0.05f, 0.12f, 0.25f, 0.90f);
        Color colorHeaderBg     = new Color(0.08f, 0.20f, 0.45f, 1f);
        Color colorGreenFill    = new Color(0.20f, 0.85f, 0.45f, 1f);
        Color colorGreyFill     = new Color(0.40f, 0.40f, 0.42f, 1f);
        Color colorStarOn       = new Color(1.00f, 0.85f, 0.20f, 1f);
        Color colorStarOff      = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        Color colorTextWhite    = new Color(0.95f, 0.95f, 0.95f, 1f);
        Color colorTextCyan     = new Color(0.40f, 0.90f, 1.00f, 1f);
        Color colorTextGold     = new Color(1.00f, 0.80f, 0.20f, 1f);
        Color colorGreenText    = new Color(0.20f, 0.90f, 0.45f, 1f);
        Color transparent       = new Color(0f, 0f, 0f, 0f);

        // ============================================================
        //  1. ScrollView chứa toàn bộ nội dung (có thể scroll)
        // ============================================================
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(gear.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        SetStretch(scrollRT, new Vector2(0,0), new Vector2(1,1), Vector2.zero, Vector2.zero);

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 40f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Mask
        var maskImg = scrollGO.AddComponent<Image>();
        maskImg.color = transparent;
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        SetStretch(contentRT, new Vector2(0,1), new Vector2(1,1), Vector2.zero, Vector2.zero);
        contentRT.pivot = new Vector2(0.5f, 1f);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 20f;
        vlg.padding = new RectOffset(20, 20, 20, 40);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content   = contentRT;
        scroll.viewport  = scrollRT;

        // ============================================================
        //  2. Section: Trang Bị Hiện Tại
        // ============================================================
        BuildSection(contentGO.transform, "SectionCurrent",
            "TRANG BỊ HIỆN TẠI", sprTitleCur,
            colorSectionBg, colorHeaderBg, colorTextCyan,
            new string[]{"Pistol","Shotgun","Assault Rifle","Sniper Rifle","RPG","Minigun"},
            new string[]{"Assets/Sprite/icon_gear_001_1.asset","Assets/Sprite/icon_gear_002_1.asset",
                         "Assets/Sprite/icon_gear_003_1.asset","Assets/Sprite/icon_gear_004_1.asset",
                         "Assets/Sprite/icon_gear_005_1.asset","Assets/Sprite/icon_gear_006_1.asset"},
            new int[]   {3,2,4,1,5,2},
            new int[]   {120,60,180,30,400,80},
            new int[]   {200,150,300,100,500,200},
            new float[] {85,140,110,250,350,95},
            new float[] {300,250,320,200,280,350},
            new int[]   {500,400,800,300,1500,600},
            isLocked: false,
            sprSlotBg, sprSlotBanner, sprBar, sprBarFill, sprLock,
            sprStar1, sprStarGrey, sprDmg, sprHp, sprCoin, sprExp,
            colorUnlockedBg, colorLockedBg, colorUnlockedFr, colorLockedFr,
            colorStarOn, colorStarOff, colorGreenFill, colorGreyFill,
            colorTextWhite, colorTextCyan, colorTextGold, colorGreenText);

        // ============================================================
        //  3. Section: Tất Cả Trang Bị
        // ============================================================
        BuildSection(contentGO.transform, "SectionAll",
            "TẤT CẢ TRANG BỊ", sprTitleAll,
            colorSectionBg, colorHeaderBg, colorTextGold,
            new string[]{"Flamethrower","Laser Cannon","Plasma Rifle","Rail Gun","Grenade Launcher","Missile Pod"},
            new string[]{"Assets/Sprite/icon_gear_007_1.asset","Assets/Sprite/icon_gear_008_1.asset",
                         "Assets/Sprite/icon_gear_009_1.asset","Assets/Sprite/icon_gear_010_1.asset",
                         "Assets/Sprite/icon_gear_011_1.asset","Assets/Sprite/icon_gear_101_1.asset"},
            new int[]   {1,1,1,1,1,1},
            new int[]   {0,0,0,0,0,0},
            new int[]   {100,100,100,100,100,100},
            new float[] {120,200,175,300,160,280},
            new float[] {260,220,240,180,300,200},
            new int[]   {350,600,500,900,700,800},
            isLocked: true,
            sprSlotBg, sprSlotBanner, sprBar, sprBarFill, sprLock,
            sprStar1, sprStarGrey, sprDmg, sprHp, sprCoin, sprExp,
            colorUnlockedBg, colorLockedBg, colorUnlockedFr, colorLockedFr,
            colorStarOn, colorStarOff, colorGreenFill, colorGreyFill,
            colorTextWhite, colorTextCyan, colorTextGold, colorGreenText);

        // ============================================================
        //  4. Thêm GearPanelUI + assign database
        // ============================================================
        var panelUI = gear.GetComponent<GearPanelUI>();
        if (panelUI == null) panelUI = gear.AddComponent<GearPanelUI>();
        panelUI.weaponDatabase = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Resources/Data/WeaponDatabase.asset");

        EditorUtility.SetDirty(gear);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gear.scene);
        Debug.Log("[GearPanelBuilder] Build complete!");
    }

    // ----------------------------------------------------------------
    static void BuildSection(Transform parent, string sectionName, string title, Sprite titleSprite,
        Color sectionBg, Color headerBg, Color headerTextColor,
        string[] names, string[] iconPaths, int[] levels, int[] xps, int[] xpMax,
        float[] damages, float[] hps, int[] coins, bool isLocked,
        Sprite sprSlotBg, Sprite sprSlotBanner, Sprite sprBar, Sprite sprBarFill, Sprite sprLock,
        Sprite sprStar1, Sprite sprStarGrey, Sprite sprDmg, Sprite sprHp, Sprite sprCoin, Sprite sprExp,
        Color colorUnlockedBg, Color colorLockedBg, Color colorUnlockedFr, Color colorLockedFr,
        Color colorStarOn, Color colorStarOff, Color colorGreenFill, Color colorGreyFill,
        Color colorTextWhite, Color colorTextCyan, Color colorTextGold, Color colorGreenText)
    {
        // Section container
        var sec = new GameObject(sectionName);
        sec.transform.SetParent(parent, false);
        var secRT = sec.AddComponent<RectTransform>();
        secRT.sizeDelta = new Vector2(0, 0);

        var secVLG = sec.AddComponent<VerticalLayoutGroup>();
        secVLG.childAlignment = TextAnchor.UpperCenter;
        secVLG.spacing = 12f;
        secVLG.padding = new RectOffset(0, 0, 0, 0);
        secVLG.childControlWidth = true;
        secVLG.childControlHeight = false;
        secVLG.childForceExpandWidth = true;
        secVLG.childForceExpandHeight = false;
        var secCSF = sec.AddComponent<ContentSizeFitter>();
        secCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Header ---
        var header = new GameObject("Header");
        header.transform.SetParent(sec.transform, false);
        var headerRT = header.AddComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0, 80);
        var headerImg = header.AddComponent<Image>();
        headerImg.color = headerBg;
        if (sprSlotBanner) { headerImg.sprite = sprSlotBanner; headerImg.type = Image.Type.Sliced; }

        // Title text
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        SetStretch(titleRT, Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = title;
        titleTMP.fontSize = 36;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = headerTextColor;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // --- Grid (2 columns) ---
        var grid = new GameObject("Grid");
        grid.transform.SetParent(sec.transform, false);
        var gridRT = grid.AddComponent<RectTransform>();
        gridRT.sizeDelta = new Vector2(0, 0);
        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(490, 240);
        glg.spacing = new Vector2(16, 16);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.padding = new RectOffset(10, 10, 10, 10);
        var gridCSF = grid.AddComponent<ContentSizeFitter>();
        gridCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Weapon Slots ---
        for (int i = 0; i < names.Length; i++)
        {
            Sprite icon = Load<Sprite>(iconPaths[i]);
            BuildWeaponSlot(grid.transform, names[i], icon, levels[i], xps[i], xpMax[i],
                damages[i], hps[i], coins[i], isLocked,
                sprSlotBg, sprBar, sprBarFill, sprLock, sprStar1, sprStarGrey,
                sprDmg, sprHp, sprCoin, sprExp,
                colorUnlockedBg, colorLockedBg, colorUnlockedFr, colorLockedFr,
                colorStarOn, colorStarOff, colorGreenFill, colorGreyFill,
                colorTextWhite, colorTextCyan, colorTextGold, colorGreenText);
        }
    }

    // ----------------------------------------------------------------
    static void BuildWeaponSlot(Transform parent, string wName, Sprite icon, int level,
        int xp, int xpMax, float damage, float hp, int coin, bool isLocked,
        Sprite sprSlotBg, Sprite sprBar, Sprite sprBarFill, Sprite sprLock,
        Sprite sprStar1, Sprite sprStarGrey, Sprite sprDmg, Sprite sprHp, Sprite sprCoin, Sprite sprExp,
        Color colorUnlockedBg, Color colorLockedBg, Color colorUnlockedFr, Color colorLockedFr,
        Color colorStarOn, Color colorStarOff, Color colorGreenFill, Color colorGreyFill,
        Color colorTextWhite, Color colorTextCyan, Color colorTextGold, Color colorGreenText)
    {
        Color bgColor = isLocked ? colorLockedBg : colorUnlockedBg;
        Color frColor = isLocked ? colorLockedFr  : colorUnlockedFr;

        // Slot root
        var slot = new GameObject(wName + "_Slot");
        slot.transform.SetParent(parent, false);
        var slotRT = slot.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(490, 240);
        var slotImg = slot.AddComponent<Image>();
        slotImg.color = bgColor;
        if (sprSlotBg) { slotImg.sprite = sprSlotBg; slotImg.type = Image.Type.Sliced; }

        // Frame overlay (border effect)
        var frame = new GameObject("Frame");
        frame.transform.SetParent(slot.transform, false);
        var frameRT = frame.AddComponent<RectTransform>();
        SetStretch(frameRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = frColor * new Color(1,1,1,0.5f);
        if (sprSlotBg) { frameImg.sprite = sprSlotBg; frameImg.type = Image.Type.Sliced; }

        // --- LEFT: Icon Area (square) ---
        var iconArea = new GameObject("IconArea");
        iconArea.transform.SetParent(slot.transform, false);
        var iconAreaRT = iconArea.AddComponent<RectTransform>();
        iconAreaRT.anchorMin = new Vector2(0, 0);
        iconAreaRT.anchorMax = new Vector2(0, 1);
        iconAreaRT.offsetMin = new Vector2(10, 10);
        iconAreaRT.offsetMax = new Vector2(200, -10);
        var iconAreaImg = iconArea.AddComponent<Image>();
        iconAreaImg.color = new Color(0f, 0f, 0f, 0.25f);

        // Icon image
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(iconArea.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        SetStretch(iconRT, new Vector2(0.05f,0.05f), new Vector2(0.95f,0.95f), Vector2.zero, Vector2.zero);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        if (icon) iconImg.sprite = icon;
        iconImg.color = isLocked ? new Color(0.4f,0.4f,0.4f,1f) : Color.white;

        // Lock overlay
        if (isLocked)
        {
            var lockOv = new GameObject("LockOverlay");
            lockOv.transform.SetParent(iconArea.transform, false);
            var lockOvRT = lockOv.AddComponent<RectTransform>();
            SetStretch(lockOvRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var lockOvImg = lockOv.AddComponent<Image>();
            lockOvImg.color = new Color(0f, 0f, 0f, 0.55f);

            var lockIco = new GameObject("LockIcon");
            lockIco.transform.SetParent(lockOv.transform, false);
            var lockIcoRT = lockIco.AddComponent<RectTransform>();
            lockIcoRT.anchorMin = new Vector2(0.5f, 0.5f);
            lockIcoRT.anchorMax = new Vector2(0.5f, 0.5f);
            lockIcoRT.sizeDelta = new Vector2(60, 60);
            lockIcoRT.anchoredPosition = Vector2.zero;
            var lockIcoImg = lockIco.AddComponent<Image>();
            if (sprLock) lockIcoImg.sprite = sprLock;
            lockIcoImg.color = new Color(1f, 1f, 1f, 0.9f);
        }

        // --- RIGHT: Info Area ---
        var info = new GameObject("InfoArea");
        info.transform.SetParent(slot.transform, false);
        var infoRT = info.AddComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.offsetMin = new Vector2(210, 8);
        infoRT.offsetMax = new Vector2(-10, -8);

        // Name
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(info.transform, false);
        var nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0,1); nameRT.anchorMax = new Vector2(1,1);
        nameRT.sizeDelta = new Vector2(0, 38);
        nameRT.anchoredPosition = new Vector2(0, -19);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = isLocked ? "???" : wName;
        nameTMP.fontSize = 28;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = isLocked ? new Color(0.6f,0.6f,0.6f,1f) : colorTextCyan;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Stars row
        var starsGO = new GameObject("Stars");
        starsGO.transform.SetParent(info.transform, false);
        var starsRT = starsGO.AddComponent<RectTransform>();
        starsRT.anchorMin = new Vector2(0,1); starsRT.anchorMax = new Vector2(1,1);
        starsRT.sizeDelta = new Vector2(0, 28);
        starsRT.anchoredPosition = new Vector2(0, -57);
        var starsHLG = starsGO.AddComponent<HorizontalLayoutGroup>();
        starsHLG.childAlignment = TextAnchor.MiddleLeft;
        starsHLG.spacing = 4;
        starsHLG.childControlWidth = false;
        starsHLG.childControlHeight = false;
        starsHLG.childForceExpandWidth = false;
        for (int s = 0; s < 5; s++)
        {
            var star = new GameObject("Star" + s);
            star.transform.SetParent(starsGO.transform, false);
            star.AddComponent<RectTransform>().sizeDelta = new Vector2(26, 26);
            var starImg = star.AddComponent<Image>();
            bool filled = (!isLocked && s < level);
            starImg.sprite = filled ? sprStar1 : sprStarGrey;
            starImg.color  = filled ? colorStarOn : colorStarOff;
        }

        // Stats row (Damage, HP, Coin) - small icons + text
        float statY = -92f;
        BuildStatRow(info.transform, "Damage", sprDmg,
            isLocked ? "-" : $"{damage:0}",
            statY, colorTextWhite, colorGreenText, new Color(1f,0.5f,0.3f,1f));

        BuildStatRow(info.transform, "HP", sprHp,
            isLocked ? "-" : $"{hp:0}",
            statY - 32, colorTextWhite, colorGreenText, new Color(0.4f,1f,0.5f,1f));

        BuildStatRow(info.transform, "Coin", sprCoin,
            isLocked ? "-" : $"{coin}",
            statY - 64, colorTextWhite, colorGreenText, colorTextGold);

        // XP Bar
        var xpBarGO = new GameObject("XPBarBg");
        xpBarGO.transform.SetParent(info.transform, false);
        var xpBarBgRT = xpBarGO.AddComponent<RectTransform>();
        xpBarBgRT.anchorMin = new Vector2(0,0); xpBarBgRT.anchorMax = new Vector2(1,0);
        xpBarBgRT.sizeDelta = new Vector2(0, 20);
        xpBarBgRT.anchoredPosition = new Vector2(0, 18);
        var xpBarBgImg = xpBarGO.AddComponent<Image>();
        xpBarBgImg.color = new Color(0.1f,0.1f,0.15f,0.8f);
        if (sprBar) { xpBarBgImg.sprite = sprBar; xpBarBgImg.type = Image.Type.Sliced; }

        if (!isLocked)
        {
            // XP fill
            var xpFill = new GameObject("XPFill");
            xpFill.transform.SetParent(xpBarGO.transform, false);
            var xpFillRT = xpFill.AddComponent<RectTransform>();
            xpFillRT.anchorMin = Vector2.zero; xpFillRT.anchorMax = new Vector2(0,1);
            float ratio = (xpMax > 0) ? Mathf.Clamp01((float)xp / xpMax) : 0f;
            xpFillRT.anchorMax = new Vector2(ratio, 1);
            xpFillRT.offsetMin = new Vector2(2,2); xpFillRT.offsetMax = new Vector2(-2,-2);
            var xpFillImg = xpFill.AddComponent<Image>();
            xpFillImg.color = colorGreenFill;
            if (sprBarFill) { xpFillImg.sprite = sprBarFill; xpFillImg.type = Image.Type.Sliced; }

            // XP text
            var xpTxtGO = new GameObject("XPText");
            xpTxtGO.transform.SetParent(xpBarGO.transform, false);
            var xpTxtRT = xpTxtGO.AddComponent<RectTransform>();
            SetStretch(xpTxtRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var xpTMP = xpTxtGO.AddComponent<TextMeshProUGUI>();
            xpTMP.text = $"XP {xp}/{xpMax}";
            xpTMP.fontSize = 14;
            xpTMP.color = Color.white;
            xpTMP.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            // Locked XP bar – grey
            var xpFillL = new GameObject("XPFillLocked");
            xpFillL.transform.SetParent(xpBarGO.transform, false);
            var xpFillLRT = xpFillL.AddComponent<RectTransform>();
            SetStretch(xpFillLRT, new Vector2(0.01f,0.1f), new Vector2(0.15f,0.9f), Vector2.zero, Vector2.zero);
            var xpFillLImg = xpFillL.AddComponent<Image>();
            xpFillLImg.color = colorGreyFill;
        }

        // Level badge
        var lvBadge = new GameObject("LvBadge");
        lvBadge.transform.SetParent(slot.transform, false);
        var lvBadgeRT = lvBadge.AddComponent<RectTransform>();
        lvBadgeRT.anchorMin = new Vector2(0,1); lvBadgeRT.anchorMax = new Vector2(0,1);
        lvBadgeRT.sizeDelta = new Vector2(70, 32);
        lvBadgeRT.anchoredPosition = new Vector2(10, -10);
        var lvBadgeImg = lvBadge.AddComponent<Image>();
        lvBadgeImg.color = isLocked ? new Color(0.25f,0.25f,0.27f,0.9f) : new Color(0.1f,0.5f,0.9f,0.9f);
        var lvTxtGO = new GameObject("LvText");
        lvTxtGO.transform.SetParent(lvBadge.transform, false);
        var lvTxtRT = lvTxtGO.AddComponent<RectTransform>();
        SetStretch(lvTxtRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var lvTMP = lvTxtGO.AddComponent<TextMeshProUGUI>();
        lvTMP.text = isLocked ? "LV?" : $"LV{level}";
        lvTMP.fontSize = 20;
        lvTMP.fontStyle = FontStyles.Bold;
        lvTMP.color = colorTextWhite;
        lvTMP.alignment = TextAlignmentOptions.Center;
    }

    static void BuildStatRow(Transform parent, string label, Sprite icon, string value,
        float anchoredY, Color textColor, Color valueColor, Color iconTint)
    {
        var row = new GameObject("Stat_" + label);
        row.transform.SetParent(parent, false);
        var rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0,1); rowRT.anchorMax = new Vector2(1,1);
        rowRT.sizeDelta = new Vector2(0, 28);
        rowRT.anchoredPosition = new Vector2(0, anchoredY);

        // Icon
        if (icon != null)
        {
            var icoGO = new GameObject("Icon");
            icoGO.transform.SetParent(row.transform, false);
            var icoRT = icoGO.AddComponent<RectTransform>();
            icoRT.anchorMin = new Vector2(0,0); icoRT.anchorMax = new Vector2(0,1);
            icoRT.sizeDelta = new Vector2(22, 0);
            icoRT.anchoredPosition = new Vector2(11,0);
            var icoImg = icoGO.AddComponent<Image>();
            icoImg.sprite = icon;
            icoImg.color = iconTint;
            icoImg.preserveAspect = true;
        }

        // Value text
        var valGO = new GameObject("Value");
        valGO.transform.SetParent(row.transform, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0,0); valRT.anchorMax = new Vector2(1,1);
        valRT.offsetMin = new Vector2(28, 0); valRT.offsetMax = Vector2.zero;
        var valTMP = valGO.AddComponent<TextMeshProUGUI>();
        valTMP.text = value;
        valTMP.fontSize = 22;
        valTMP.color = valueColor;
        valTMP.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // ----------------------------------------------------------------
    static void SetStretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);
}
