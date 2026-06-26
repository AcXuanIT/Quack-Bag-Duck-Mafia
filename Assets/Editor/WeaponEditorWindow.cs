using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ================================================================
//  WeaponEditorWindow  –  Tools ▸ ⚔ Weapon Database Editor
//  Full CRUD editor: thêm / sửa / nhân bản / xoá / sắp xếp
//  Không cần chạm vào .asset file thủ công.
// ================================================================
public class WeaponEditorWindow : EditorWindow
{
    // ── Constants ─────────────────────────────────────────────
    private const string PREF_DB_PATH    = "WeaponEditor_DBPath";
    private const string DEFAULT_DB_PATH = "Assets/Resources/Data/WeaponDatabase.asset";
    private const string ICON_FOLDER     = "Assets/Sprite";
    private const float  LEFT_W          = 270f;
    private const float  TOOLBAR_H       = 32f;
    private const float  ROW_H           = 60f;

    // ── State ─────────────────────────────────────────────────
    private WeaponData         _db;
    private string             _dbPath;
    private List<WeaponEntry>  _list     = new List<WeaponEntry>();
    private List<WeaponEntry>  _filtered = new List<WeaponEntry>();

    private int         _selectedIndex     = -1;   // index in _list
    private WeaponEntry _editing;                   // working copy
    private Vector2     _listScroll;
    private Vector2     _detailScroll;
    private string      _searchTerm        = "";
    private bool        _showConfirmDelete  = false;
    private bool        _isDirty           = false;

    // Sort
    private enum SortMode { ID, Name, Level, Damage, HP, Locked }
    private SortMode _sortMode      = SortMode.ID;
    private bool     _sortAscending = true;

    // Icon picker
    private bool          _showIconPicker   = false;
    private List<Sprite>  _allSprites       = new List<Sprite>();
    private Vector2       _iconPickerScroll;
    private string        _iconSearch       = "";

    // Stats compare (hover)
    private WeaponEntry   _compareEntry;

    // Styles
    private GUIStyle _sHeader, _sSection, _sBadge, _sRowName, _sRowSub, _sRowID;
    private bool     _stylesReady;

    // ── Color Palette ─────────────────────────────────────────
    private static readonly Color CBg        = new Color(0.13f, 0.14f, 0.16f);
    private static readonly Color CPanel     = new Color(0.17f, 0.19f, 0.22f);
    private static readonly Color CSel       = new Color(0.20f, 0.46f, 0.80f);
    private static readonly Color CLocked    = new Color(0.25f, 0.25f, 0.27f);
    private static readonly Color CUnlocked  = new Color(0.16f, 0.35f, 0.58f);
    private static readonly Color CSep       = new Color(0.09f, 0.10f, 0.12f);
    private static readonly Color CGreen     = new Color(0.18f, 0.72f, 0.38f);
    private static readonly Color COrange    = new Color(0.92f, 0.58f, 0.12f);
    private static readonly Color CRed       = new Color(0.72f, 0.18f, 0.18f);
    private static readonly Color CCyan      = new Color(0.25f, 0.78f, 1.00f);
    private static readonly Color CGold      = new Color(1.00f, 0.80f, 0.18f);
    private static readonly Color CPurple    = new Color(0.65f, 0.35f, 0.90f);
    private static readonly Color CDirtyTag  = new Color(0.90f, 0.50f, 0.10f);

    // ── Menu ──────────────────────────────────────────────────
    [MenuItem("Tools/⚔ Weapon Database Editor")]
    public static void Open()
    {
        var w = GetWindow<WeaponEditorWindow>("⚔ Weapon DB");
        w.minSize = new Vector2(920, 560);
        w.Show();
    }

    // ─────────────────────────────────────────────────────────
    #region Unity Callbacks

    private void OnEnable()
    {
        _dbPath = EditorPrefs.GetString(PREF_DB_PATH, DEFAULT_DB_PATH);
        LoadDatabase();
        LoadAllSprites();
    }

    private void OnGUI()
    {
        EnsureStyles();
        EditorGUI.DrawRect(new Rect(0,0,position.width,position.height), CBg);

        DrawTopBar();
        DrawDividerH();

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        {
            DrawLeftPanel();
            DrawDividerV();
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        // Overlays (drawn on top)
        if (_showIconPicker)    DrawIconPickerOverlay();
        if (_showConfirmDelete) DrawConfirmDeleteOverlay();

        HandleKeys();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Top Bar

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(TOOLBAR_H));
        {
            GUILayout.Space(8);

            // DB path picker
            GUI.color = _db != null ? CCyan : CRed;
            GUILayout.Label(_db != null ? "📂 " + System.IO.Path.GetFileName(_dbPath) : "⚠ No Database", _sHeader, GUILayout.Width(260), GUILayout.Height(TOOLBAR_H));
            GUI.color = Color.white;

            if (GUILayout.Button("Chọn DB…", GUILayout.Width(80), GUILayout.Height(24)))
            {
                string picked = EditorUtility.OpenFilePanel("Chọn WeaponData asset", "Assets", "asset");
                if (!string.IsNullOrEmpty(picked))
                {
                    // Convert absolute path to relative
                    if (picked.StartsWith(Application.dataPath))
                        picked = "Assets" + picked.Substring(Application.dataPath.Length);
                    _dbPath = picked;
                    EditorPrefs.SetString(PREF_DB_PATH, _dbPath);
                    LoadDatabase();
                }
            }

            // Create new DB
            GUI.color = new Color(0.5f,0.5f,0.5f);
            if (GUILayout.Button("+ Tạo DB mới", GUILayout.Width(100), GUILayout.Height(24)))
                CreateNewDatabase();
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // Dirty indicator
            if (_isDirty)
            {
                GUI.color = CDirtyTag;
                GUILayout.Label("● Chưa lưu", _sHeader, GUILayout.Width(80));
                GUI.color = Color.white;
            }

            // Count badge
            if (_db != null)
            {
                GUI.color = new Color(0.4f, 0.4f, 0.5f);
                GUILayout.Label($"{_list.Count} vũ khí", EditorStyles.miniLabel, GUILayout.Width(70));
                GUI.color = Color.white;
            }

            // Reload
            if (GUILayout.Button("↺ Reload", GUILayout.Width(72), GUILayout.Height(24)))
            { LoadDatabase(); LoadAllSprites(); }

            // Save
            bool canSave = _isDirty && _db != null;
            GUI.enabled = canSave;
            GUI.color = canSave ? CGold : Color.gray;
            if (GUILayout.Button("💾 Lưu  (Ctrl+S)", GUILayout.Width(128), GUILayout.Height(24)))
                SaveDatabase();
            GUI.color = Color.white;
            GUI.enabled = true;

            GUILayout.Space(8);
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Left Panel

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_W), GUILayout.ExpandHeight(true));
        {
            DrawListToolbar();
            DrawSearchAndSort();
            DrawDividerH();
            DrawWeaponList();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawListToolbar()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(TOOLBAR_H));
        {
            GUILayout.Space(6);
            GUI.color = CGreen;
            if (GUILayout.Button("＋ Thêm", GUILayout.Width(80), GUILayout.Height(26)))
                AddNewWeapon();
            GUI.color = Color.white;

            GUI.enabled = _editing != null;
            GUI.color = CPurple;
            if (GUILayout.Button("⎘ Nhân bản", GUILayout.Width(90), GUILayout.Height(26)))
                DuplicateSelected();
            GUI.color = Color.white;
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchAndSort()
    {
        GUILayout.Space(4);
        // Search bar
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(6);
            GUILayout.Label("🔍", GUILayout.Width(18));
            string newSearch = EditorGUILayout.TextField(_searchTerm, GUILayout.ExpandWidth(true));
            if (newSearch != _searchTerm) { _searchTerm = newSearch; RebuildFiltered(); }
            if (!string.IsNullOrEmpty(_searchTerm))
            {
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                { _searchTerm = ""; RebuildFiltered(); }
            }
            GUILayout.Space(6);
        }
        EditorGUILayout.EndHorizontal();

        // Sort bar
        GUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(6);
            GUILayout.Label("↕", GUILayout.Width(14));
            SortMode[] modes = { SortMode.ID, SortMode.Name, SortMode.Level, SortMode.Damage };
            string[]   labels = { "ID", "Tên", "Lv", "DMG" };
            for (int i = 0; i < modes.Length; i++)
            {
                bool active = _sortMode == modes[i];
                GUI.color = active ? CCyan : Color.gray;
                string lbl = labels[i] + (active ? (_sortAscending ? " ▲" : " ▼") : "");
                if (GUILayout.Button(lbl, EditorStyles.miniButton, GUILayout.Width(46)))
                {
                    if (_sortMode == modes[i]) _sortAscending = !_sortAscending;
                    else { _sortMode = modes[i]; _sortAscending = true; }
                    RebuildFiltered();
                }
            }
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    private void RebuildFiltered()
    {
        // Filter
        _filtered = string.IsNullOrEmpty(_searchTerm)
            ? new List<WeaponEntry>(_list)
            : _list.Where(w =>
                (w.Name ?? "").IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                w.ID.ToString() == _searchTerm
              ).ToList();

        // Sort
        switch (_sortMode)
        {
            case SortMode.ID:     _filtered = _sortAscending ? _filtered.OrderBy(w=>w.ID).ToList()     : _filtered.OrderByDescending(w=>w.ID).ToList(); break;
            case SortMode.Name:   _filtered = _sortAscending ? _filtered.OrderBy(w=>w.Name).ToList()   : _filtered.OrderByDescending(w=>w.Name).ToList(); break;
            case SortMode.Level:  _filtered = _sortAscending ? _filtered.OrderBy(w=>w.Level).ToList()  : _filtered.OrderByDescending(w=>w.Level).ToList(); break;
            case SortMode.Damage: _filtered = _sortAscending ? _filtered.OrderBy(w=>w.Damage).ToList() : _filtered.OrderByDescending(w=>w.Damage).ToList(); break;
            case SortMode.Locked: _filtered = _sortAscending ? _filtered.OrderBy(w=>w.IsLocked).ToList(): _filtered.OrderByDescending(w=>w.IsLocked).ToList(); break;
        }
    }

    private void DrawWeaponList()
    {
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
        {
            if (_db == null)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Chưa có database. Chọn hoặc tạo mới.", EditorStyles.centeredGreyMiniLabel);
            }
            else if (_filtered.Count == 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Không tìm thấy vũ khí nào.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var entry in _filtered)
                {
                    int realIdx = _list.IndexOf(entry);
                    DrawWeaponRow(entry, realIdx, realIdx == _selectedIndex);
                }
            }
            GUILayout.Space(8);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawWeaponRow(WeaponEntry w, int idx, bool sel)
    {
        Color bg = sel ? CSel : (w.IsLocked ? CLocked : CUnlocked);
        var rect = EditorGUILayout.GetControlRect(false, ROW_H);
        EditorGUI.DrawRect(rect, bg);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax-1, rect.width, 1), CSep);

        // Left accent bar (level color)
        Color accentColor = w.IsLocked ? Color.gray : Color.Lerp(CCyan, CGold, (w.Level-1)/4f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), accentColor);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        { SelectWeapon(idx); Event.current.Use(); Repaint(); }

        // Icon (48×48)
        var iconR = new Rect(rect.x+10, rect.y+6, 48, 48);
        if (w.IconLevel1 != null)
        {
            var tex = AssetPreview.GetAssetPreview(w.IconLevel1);
            GUI.DrawTexture(iconR, tex != null ? tex : w.IconLevel1.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.DrawRect(iconR, new Color(0.25f,0.25f,0.28f));
            GUI.Label(iconR, "?", new GUIStyle(GUI.skin.label){ alignment=TextAnchor.MiddleCenter, fontSize=20, normal={textColor=Color.gray} });
        }

        // Lock badge
        if (w.IsLocked)
            GUI.Label(new Rect(iconR.x, iconR.y, 16, 16), "🔒", new GUIStyle(GUI.skin.label){fontSize=10});

        // Name
        GUI.Label(new Rect(rect.x+64, rect.y+5, rect.width-108, 22), w.Name ?? "(no name)", _sRowName);

        // Sub info
        string sub = w.IsLocked
            ? $"Locked  |  Lv{w.Level}"
            : $"Lv{w.Level}  ⚔ {w.Damage:0}  ♥ {w.HP:0}  💰 {w.Coin}";
        GUI.Label(new Rect(rect.x+64, rect.y+28, rect.width-108, 18), sub, _sRowSub);

        // ID badge (right)
        GUI.Label(new Rect(rect.xMax-42, rect.y+4, 38, 18), $"#{w.ID}", _sRowID);

        // XP mini bar (bottom)
        if (!w.IsLocked && w.XPToNextLevel > 0)
        {
            float ratio = Mathf.Clamp01((float)w.XP / w.XPToNextLevel);
            var barBg = new Rect(rect.x+64, rect.yMax-8, rect.width-70, 4);
            EditorGUI.DrawRect(barBg, new Color(0.1f,0.1f,0.15f));
            EditorGUI.DrawRect(new Rect(barBg.x, barBg.y, barBg.width*ratio, barBg.height), CCyan);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Right Panel

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        {
            if (_db == null)
            {
                DrawNoDB();
            }
            else if (_editing == null)
            {
                DrawNoSelection();
            }
            else
            {
                DrawDetailToolbar();
                DrawDividerH();
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                { DrawWeaponForm(); }
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawNoDB()
    {
        GUILayout.FlexibleSpace();
        GUI.color = CRed;
        EditorGUILayout.LabelField("⚠ Chưa chọn WeaponDatabase!", EditorStyles.centeredGreyMiniLabel);
        GUI.color = Color.white;
        EditorGUILayout.LabelField("Dùng nút \"Chọn DB\" hoặc \"+ Tạo DB mới\" ở trên.", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
    }

    private void DrawNoSelection()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("← Chọn vũ khí để chỉnh sửa", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(6);
        EditorGUILayout.LabelField("hoặc  ＋ Thêm  để tạo vũ khí mới", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
    }

    private void DrawDetailToolbar()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(TOOLBAR_H));
        {
            GUILayout.Space(10);
            string title = _editing != null
                ? $"✏  {_editing.Name}  (ID #{_editing.ID})"
                : "Chi tiết";
            GUI.color = CCyan;
            GUILayout.Label(title, _sHeader, GUILayout.ExpandWidth(true), GUILayout.Height(TOOLBAR_H));
            GUI.color = Color.white;

            GUI.color = CGreen;
            if (GUILayout.Button("✔ Áp dụng  (Ctrl+↵)", GUILayout.Width(152), GUILayout.Height(26))) ApplyEditing();
            GUI.color = Color.gray;
            if (GUILayout.Button("↩ Huỷ", GUILayout.Width(66), GUILayout.Height(26))) DiscardEditing();
            GUI.color = CRed;
            if (GUILayout.Button("🗑 Xoá", GUILayout.Width(66), GUILayout.Height(26))) _showConfirmDelete = true;
            GUI.color = Color.white;
            GUILayout.Space(10);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWeaponForm()
    {
        GUILayout.Space(14);

        // ══ IDENTITY ═══════════════════════════════════════════
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);

        // Icon picker block
        EditorGUILayout.BeginVertical(GUILayout.Width(110));
        {
            var iconR = GUILayoutUtility.GetRect(90, 90);
            iconR = new Rect(iconR.x+10, iconR.y, 90, 90);
            EditorGUI.DrawRect(iconR, new Color(0.18f,0.20f,0.24f));
            if (_editing.IconLevel1 != null)
            {
                var prev = AssetPreview.GetAssetPreview(_editing.IconLevel1);
                GUI.DrawTexture(iconR, prev != null ? prev : _editing.IconLevel1.texture, ScaleMode.ScaleToFit);
            }
            else
                GUI.Label(iconR, "No Icon", new GUIStyle(GUI.skin.label){ alignment=TextAnchor.MiddleCenter, normal={textColor=Color.gray} });

            GUILayout.Space(4);
            if (GUILayout.Button("🖼 Chọn Icon…", GUILayout.Width(100))) { _showIconPicker = true; _iconSearch = ""; }
            if (_editing.IconLevel1 != null)
            {
                GUI.color = new Color(1f,0.4f,0.4f);
                if (GUILayout.Button("✕ Xóa", GUILayout.Width(100))) _editing.IconLevel1 = null;
                GUI.color = Color.white;
            }
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(20);

        // Identity fields
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        {
            GUILayout.Space(6);
            FieldRow("ID", () =>
            {
                _editing.ID = EditorGUILayout.IntField(_editing.ID, GUILayout.Width(80));
                GUILayout.Space(6);
                GUI.color = new Color(0.5f,0.5f,0.5f);
                if (GUILayout.Button("Auto ID", GUILayout.Width(70))) _editing.ID = GenerateFreeID(_editing.ID);
                GUI.color = Color.white;
            });
            FieldRow("Tên vũ khí", () => _editing.Name = EditorGUILayout.TextField(_editing.Name));
            FieldRow("Trạng thái", () =>
            {
                GUI.color = _editing.IsLocked ? CRed : CGreen;
                _editing.IsLocked = EditorGUILayout.Toggle(_editing.IsLocked, GUILayout.Width(20));
                GUILayout.Label(_editing.IsLocked ? "🔒 Bị khoá" : "🔓 Đã mở", GUILayout.Width(90));
                GUI.color = Color.white;
            });
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(14);
        DrawDividerH();
        GUILayout.Space(10);

        // ══ COMBAT STATS ═══════════════════════════════════════
        SectionLabel("⚔  Chỉ Số Chiến Đấu");
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);

        EditorGUILayout.BeginVertical(GUILayout.Width(310));
        {
            FieldRow("Sát thương (Damage)", () =>
            {
                _editing.Damage = EditorGUILayout.FloatField(_editing.Damage, GUILayout.Width(100));
                SliderHint(ref _editing.Damage, 1f, 1000f);
            });
            FieldRow("Máu (HP)", () =>
            {
                _editing.HP = EditorGUILayout.FloatField(_editing.HP, GUILayout.Width(100));
                SliderHint(ref _editing.HP, 1f, 2000f);
            });
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(30);

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        {
            StatBar("DMG", _editing.Damage, 1000f, COrange);
            GUILayout.Space(6);
            StatBar("HP ", _editing.HP, 2000f, CGreen);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(12);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);
        DrawDividerH();
        GUILayout.Space(10);

        // ══ LEVEL & XP ═════════════════════════════════════════
        SectionLabel("⭐  Level & XP");
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        {
            // Stars row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Level (1–5)", GUILayout.Width(130));
            _editing.Level = EditorGUILayout.IntSlider(_editing.Level, 1, 5);
            GUILayout.Space(8);
            for (int s = 0; s < 5; s++)
            {
                GUI.color = s < _editing.Level ? CGold : Color.gray;
                GUILayout.Label("★", GUILayout.Width(18));
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            FieldRow("XP hiện tại", () => _editing.XP = Mathf.Max(0, EditorGUILayout.IntField(_editing.XP, GUILayout.Width(120))));
            FieldRow("XP lên Lv tiếp", () => _editing.XPToNextLevel = Mathf.Max(1, EditorGUILayout.IntField(_editing.XPToNextLevel, GUILayout.Width(120))));

            // XP progress bar
            GUILayout.Space(6);
            float xpR = _editing.XPToNextLevel > 0 ? Mathf.Clamp01((float)_editing.XP / _editing.XPToNextLevel) : 0f;
            var xpBarR = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            xpBarR = new Rect(xpBarR.x+4, xpBarR.y, xpBarR.width-8, xpBarR.height);
            EditorGUI.DrawRect(xpBarR, new Color(0.08f,0.08f,0.12f));
            EditorGUI.DrawRect(new Rect(xpBarR.x, xpBarR.y, xpBarR.width*xpR, xpBarR.height), CCyan);
            GUI.Label(xpBarR, $"  {_editing.XP} / {_editing.XPToNextLevel}  ({xpR*100:0}%)",
                new GUIStyle(GUI.skin.label){ fontSize=10, normal={textColor=Color.white} });

            GUILayout.Space(4);
            FieldRow("Coin nâng cấp 💰", () =>
            {
                GUI.color = CGold;
                _editing.Coin = Mathf.Max(0, EditorGUILayout.IntField(_editing.Coin, GUILayout.Width(120)));
                GUI.color = Color.white;
            });
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(16);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);
        DrawDividerH();
        GUILayout.Space(10);

        // ══ QUICK ACTIONS ══════════════════════════════════════
        SectionLabel("🛠  Thao Tác Nhanh");
        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(16);

            GUI.color = _editing.IsLocked ? CGreen : CRed;
            if (GUILayout.Button(_editing.IsLocked ? "🔓 Mở khoá" : "🔒 Khoá lại", GUILayout.Width(120), GUILayout.Height(28)))
                _editing.IsLocked = !_editing.IsLocked;
            GUI.color = Color.white;

            GUILayout.Space(8);
            GUI.color = CCyan;
            if (GUILayout.Button("↑ Max Level", GUILayout.Width(110), GUILayout.Height(28)))
            { _editing.Level = 5; _editing.XP = _editing.XPToNextLevel; }
            GUI.color = Color.white;

            GUILayout.Space(8);
            GUI.color = Color.gray;
            if (GUILayout.Button("Reset XP", GUILayout.Width(90), GUILayout.Height(28)))
                _editing.XP = 0;
            GUI.color = Color.white;

            GUILayout.Space(8);
            GUI.color = CPurple;
            if (GUILayout.Button("⎘ Nhân bản", GUILayout.Width(100), GUILayout.Height(28)))
                DuplicateSelected();
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(28);

        // ══ NEXT LEVEL PREVIEW ═════════════════════════════════
        if (!_editing.IsLocked && _editing.Level < 5)
        {
            DrawDividerH();
            GUILayout.Space(8);
            SectionLabel("🔮  Dự Đoán Chỉ Số Lv " + (_editing.Level + 1));
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            float nDmg = _editing.Damage * 1.15f;
            float nHP  = _editing.HP * 1.10f;
            int   nCoin = Mathf.RoundToInt(_editing.Coin * 1.5f);
            GUI.color = new Color(0.7f,0.9f,1f);
            GUILayout.Label($"DMG: {_editing.Damage:0} → {nDmg:0}  (+{(nDmg-_editing.Damage):0})", GUILayout.Width(200));
            GUILayout.Label($"HP: {_editing.HP:0} → {nHP:0}  (+{(nHP-_editing.HP):0})", GUILayout.Width(200));
            GUILayout.Label($"Coin cost: {nCoin} 💰", GUILayout.Width(130));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(16);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Icon Picker Overlay

    private void DrawIconPickerOverlay()
    {
        var full = new Rect(0,0,position.width,position.height);
        EditorGUI.DrawRect(full, new Color(0,0,0,0.60f));

        float pw = Mathf.Min(position.width*0.82f, 720f);
        float ph = Mathf.Min(position.height*0.78f, 540f);
        var panel = new Rect((position.width-pw)*.5f, (position.height-ph)*.5f, pw, ph);
        EditorGUI.DrawRect(panel, new Color(0.15f,0.17f,0.20f));
        EditorGUI.DrawRect(new Rect(panel.x, panel.y, panel.width, 2), CCyan);

        GUI.Label(new Rect(panel.x+14, panel.y+8, pw-54, 28),
            "🖼  Chọn Icon Vũ Khí",
            new GUIStyle(GUI.skin.label){ fontSize=15, fontStyle=FontStyle.Bold, normal={textColor=Color.white} });

        if (GUI.Button(new Rect(panel.xMax-38, panel.y+7, 28, 28), "✕"))
        { _showIconPicker = false; Repaint(); return; }

        // Search
        GUI.Label(new Rect(panel.x+14, panel.y+42, 30, 22), "🔍");
        _iconSearch = GUI.TextField(new Rect(panel.x+40, panel.y+42, pw-58, 22), _iconSearch);

        // Reload sprites
        if (GUI.Button(new Rect(panel.xMax-90, panel.y+42, 78, 22), "↺ Reload"))
            LoadAllSprites();

        var scrollArea = new Rect(panel.x+8, panel.y+74, pw-16, ph-82);
        var filtered = string.IsNullOrEmpty(_iconSearch)
            ? _allSprites
            : _allSprites.Where(s => s.name.IndexOf(_iconSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        int cols = Mathf.Max(1, Mathf.FloorToInt((scrollArea.width-12)/88));
        int rows = Mathf.CeilToInt((float)filtered.Count/cols);
        var viewR = new Rect(0,0, scrollArea.width-16, rows*94+8);
        _iconPickerScroll = GUI.BeginScrollView(scrollArea, _iconPickerScroll, viewR);

        for (int i = 0; i < filtered.Count; i++)
        {
            int col = i % cols, row = i / cols;
            var cell  = new Rect(col*88+4, row*94+4, 84, 90);
            var imgR  = new Rect(cell.x+6, cell.y+4, 72, 68);
            var lblR  = new Rect(cell.x, cell.yMax-16, 84, 14);

            bool isSel = filtered[i] == _editing.IconLevel1;
            EditorGUI.DrawRect(cell, isSel ? CSel : new Color(0.20f,0.22f,0.26f));
            if (isSel) EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 2), CCyan);

            var prev = AssetPreview.GetAssetPreview(filtered[i]);
            GUI.DrawTexture(imgR, prev != null ? prev : filtered[i].texture, ScaleMode.ScaleToFit);

            GUI.Label(lblR, filtered[i].name,
                new GUIStyle(GUI.skin.label){ fontSize=8, alignment=TextAnchor.MiddleCenter,
                    normal={textColor=isSel?Color.white:Color.gray} });

            if (Event.current.type == EventType.MouseDown && cell.Contains(Event.current.mousePosition))
            { _editing.IconLevel1 = filtered[i]; _showIconPicker = false; Event.current.Use(); Repaint(); }
        }
        GUI.EndScrollView();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Confirm Delete Overlay

    private void DrawConfirmDeleteOverlay()
    {
        EditorGUI.DrawRect(new Rect(0,0,position.width,position.height), new Color(0,0,0,0.55f));
        float pw=380, ph=150;
        var panel = new Rect((position.width-pw)*.5f,(position.height-ph)*.5f,pw,ph);
        EditorGUI.DrawRect(panel, new Color(0.16f,0.06f,0.06f));
        EditorGUI.DrawRect(new Rect(panel.x, panel.y, panel.width, 2), CRed);

        GUI.Label(new Rect(panel.x+14, panel.y+12, pw-28, 26),
            "⚠  Xác nhận xoá vũ khí?",
            new GUIStyle(GUI.skin.label){ fontSize=14, fontStyle=FontStyle.Bold, normal={textColor=CRed} });
        GUI.Label(new Rect(panel.x+14, panel.y+42, pw-28, 52),
            $"Bạn có chắc muốn xoá\n\"{_editing?.Name ?? "???"}\"\n(Hành động này không thể hoàn tác.)",
            new GUIStyle(GUI.skin.label){ fontSize=11, normal={textColor=new Color(1f,0.65f,0.65f)} });

        GUI.color = CRed;
        if (GUI.Button(new Rect(panel.x+14, panel.yMax-38, 140, 28), "🗑 Xoá"))
        { DeleteSelected(); _showConfirmDelete = false; }
        GUI.color = Color.gray;
        if (GUI.Button(new Rect(panel.xMax-154, panel.yMax-38, 140, 28), "↩ Huỷ"))
            _showConfirmDelete = false;
        GUI.color = Color.white;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Database IO

    private void LoadDatabase()
    {
        _db = AssetDatabase.LoadAssetAtPath<WeaponData>(_dbPath);
        if (_db == null) { Debug.LogWarning($"[WeaponEditor] Database not found at: {_dbPath}"); }
        SyncListFromDB();
        RebuildFiltered();
        _isDirty = false;
    }

    private void SyncListFromDB()
    {
        _list.Clear();
        if (_db?.Weapons != null)
            foreach (var w in _db.Weapons) _list.Add(w);
    }

    private void SaveDatabase()
    {
        if (_db == null) return;
        Undo.RecordObject(_db, "Save Weapon Database");
        _db.Weapons = _list.ToArray();
        EditorUtility.SetDirty(_db);
        AssetDatabase.SaveAssets();
        _isDirty = false;
        Debug.Log($"[WeaponEditor] ✅ Đã lưu {_list.Count} vũ khí vào {_dbPath}");
    }

    private void CreateNewDatabase()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Tạo WeaponDatabase mới", "WeaponDatabase", "asset", "Chọn vị trí lưu database");
        if (string.IsNullOrEmpty(path)) return;

        var newDb = CreateInstance<WeaponData>();
        newDb.Weapons = Array.Empty<WeaponEntry>();
        AssetDatabase.CreateAsset(newDb, path);
        AssetDatabase.SaveAssets();
        _dbPath = path;
        EditorPrefs.SetString(PREF_DB_PATH, _dbPath);
        LoadDatabase();
        Debug.Log($"[WeaponEditor] ✅ Tạo database mới: {path}");
    }

    private void LoadAllSprites()
    {
        _allSprites.Clear();
        var guids = AssetDatabase.FindAssets("t:Sprite", new[]{ ICON_FOLDER });
        foreach (var g in guids)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
            if (sp != null) _allSprites.Add(sp);
        }
        _allSprites = _allSprites.OrderBy(s => s.name).ToList();
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region CRUD

    private void SelectWeapon(int idx)
    {
        _selectedIndex = idx;
        _editing = (idx >= 0 && idx < _list.Count) ? CloneEntry(_list[idx]) : null;
        _showIconPicker = _showConfirmDelete = false;
    }

    private void AddNewWeapon()
    {
        if (_db == null) return;
        var entry = new WeaponEntry
        {
            ID            = GenerateFreeID(-1),
            Name          = "New Weapon",
            Level         = 1,
            XP            = 0,
            XPToNextLevel = 100,
            Damage        = 50f,
            HP            = 200f,
            Coin          = 300,
            IsLocked      = true,
        };
        _list.Add(entry);
        _isDirty = true;
        RebuildFiltered();
        SelectWeapon(_list.Count - 1);
        Repaint();
    }

    private void DuplicateSelected()
    {
        if (_editing == null || _db == null) return;
        var copy = CloneEntry(_editing);
        copy.ID   = GenerateFreeID(-1);
        copy.Name = _editing.Name + " (copy)";
        _list.Add(copy);
        _isDirty = true;
        RebuildFiltered();
        SelectWeapon(_list.Count - 1);
        Repaint();
        Debug.Log($"[WeaponEditor] Nhân bản: {copy.Name}");
    }

    private void ApplyEditing()
    {
        if (_editing == null || _selectedIndex < 0 || _selectedIndex >= _list.Count) return;

        // ID uniqueness check
        for (int i = 0; i < _list.Count; i++)
            if (i != _selectedIndex && _list[i].ID == _editing.ID)
            {
                EditorUtility.DisplayDialog("ID trùng lặp", $"ID #{_editing.ID} đã tồn tại!\nVui lòng chọn ID khác.", "OK");
                return;
            }

        CopyEntry(_editing, _list[_selectedIndex]);
        _isDirty = true;
        RebuildFiltered();
        Repaint();
        Debug.Log($"[WeaponEditor] ✔ Đã áp dụng: {_editing.Name}");
    }

    private void DiscardEditing()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _list.Count)
            _editing = CloneEntry(_list[_selectedIndex]);
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _list.Count) return;
        string n = _list[_selectedIndex].Name;
        _list.RemoveAt(_selectedIndex);
        _selectedIndex = Mathf.Clamp(_selectedIndex-1, -1, _list.Count-1);
        _editing = _selectedIndex >= 0 ? CloneEntry(_list[_selectedIndex]) : null;
        _isDirty = true;
        RebuildFiltered();
        Debug.Log($"[WeaponEditor] 🗑 Đã xoá: {n}");
        Repaint();
    }

    private int GenerateFreeID(int excludeExisting)
    {
        int id = 1;
        while (_list.Any(w => w.ID == id && w.ID != excludeExisting)) id++;
        return id;
    }

    private WeaponEntry CloneEntry(WeaponEntry s) => new WeaponEntry
    { ID=s.ID, Name=s.Name, IconLevel1=s.IconLevel1, Level=s.Level, XP=s.XP,
      XPToNextLevel=s.XPToNextLevel, Damage=s.Damage, HP=s.HP, Coin=s.Coin, IsLocked=s.IsLocked };

    private void CopyEntry(WeaponEntry s, WeaponEntry d)
    { d.ID=s.ID; d.Name=s.Name; d.IconLevel1=s.IconLevel1; d.Level=s.Level; d.XP=s.XP;
      d.XPToNextLevel=s.XPToNextLevel; d.Damage=s.Damage; d.HP=s.HP; d.Coin=s.Coin; d.IsLocked=s.IsLocked; }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region UI Helpers

    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _sHeader  = new GUIStyle(EditorStyles.boldLabel)
            { fontSize=13, normal={textColor=CCyan} };
        _sSection = new GUIStyle(EditorStyles.boldLabel)
            { fontSize=11, normal={textColor=CCyan} };
        _sRowName = new GUIStyle(GUI.skin.label)
            { fontSize=13, fontStyle=FontStyle.Bold, normal={textColor=Color.white} };
        _sRowSub  = new GUIStyle(GUI.skin.label)
            { fontSize=10, normal={textColor=new Color(0.72f,0.84f,1f)} };
        _sRowID   = new GUIStyle(GUI.skin.label)
            { fontSize=10, alignment=TextAnchor.MiddleRight, normal={textColor=new Color(0.50f,0.72f,1f)} };
    }

    private void DrawDividerH()
    {
        var r = EditorGUILayout.GetControlRect(false, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, CSep);
    }

    private void DrawDividerV()
    {
        var r = EditorGUILayout.GetControlRect(false, GUILayout.Width(2), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(r, CSep);
    }

    private void SectionLabel(string label)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.Label(label, _sSection ?? EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void FieldRow(string label, Action draw)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        draw?.Invoke();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);
    }

    private void SliderHint(ref float v, float min, float max)
    {
        float nv = GUILayout.HorizontalSlider(v, min, max, GUILayout.Width(100));
        if (Mathf.Abs(nv-v) > 0.5f) v = Mathf.Round(nv);
    }

    private void StatBar(string lbl, float val, float max, Color color)
    {
        float ratio = max > 0 ? Mathf.Clamp01(val/max) : 0f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(4);
        GUILayout.Label(lbl, GUILayout.Width(32));
        var r = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0.08f,0.08f,0.12f));
        if (ratio > 0) EditorGUI.DrawRect(new Rect(r.x, r.y, r.width*ratio, r.height), color);
        GUILayout.Label($"{val:0}", GUILayout.Width(50));
        GUILayout.Space(4);
        EditorGUILayout.EndHorizontal();
    }

    private void HandleKeys()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        // Ctrl+S → Save
        if (e.control && e.keyCode == KeyCode.S) { SaveDatabase(); e.Use(); }
        // Ctrl+Enter → Apply
        if (e.control && e.keyCode == KeyCode.Return) { ApplyEditing(); e.Use(); }
        // Escape → close overlays
        if (e.keyCode == KeyCode.Escape)
        {
            if (_showIconPicker) { _showIconPicker = false; e.Use(); }
            else if (_showConfirmDelete) { _showConfirmDelete = false; e.Use(); }
        }
        // Arrow keys → navigate list
        if (!_showIconPicker && !_showConfirmDelete)
        {
            if (e.keyCode == KeyCode.UpArrow && _selectedIndex > 0)
            { SelectWeapon(_selectedIndex-1); e.Use(); Repaint(); }
            if (e.keyCode == KeyCode.DownArrow && _selectedIndex < _list.Count-1)
            { SelectWeapon(_selectedIndex+1); e.Use(); Repaint(); }
        }
    }

    #endregion
}
