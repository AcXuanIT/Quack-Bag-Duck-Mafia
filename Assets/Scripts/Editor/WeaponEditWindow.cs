using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window để xem và chỉnh sửa WeaponData ScriptableObject.
/// Menu: Tools > Weapon Edit Window
/// </summary>
public class WeaponEditWindow : EditorWindow
{
    // ── Data ─────────────────────────────────────────────────
    private WeaponData _database;
    private Vector2    _listScroll;
    private Vector2    _detailScroll;
    private int        _selectedIndex = -1;

    // ── Fold states ──────────────────────────────────────────
    private bool _foldIdentity  = true;
    private bool _foldSprites   = true;
    private bool _foldGrid      = false;
    private bool _foldLevel     = true;
    private bool _foldStats     = true;
    private bool _foldUpgrade   = true;
    private bool _foldSpawn     = true;

    // ── Styles (lazy) ────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _selectedStyle;
    private bool     _stylesInit;

    private const float LIST_WIDTH   = 200f;
    private const float PREVIEW_SIZE = 64f;

    // ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Weapon Edit Window")]
    public static void Open()
    {
        var win = GetWindow<WeaponEditWindow>("Weapon Editor");
        win.minSize = new Vector2(700, 500);
        win.Show();
    }

    // ─────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Auto-find database
        var guids = AssetDatabase.FindAssets("t:WeaponData");
        if (guids.Length > 0)
            _database = AssetDatabase.LoadAssetAtPath<WeaponData>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft
        };

        _selectedStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0.25f, 0.5f, 0.9f, 0.4f)) }
        };
    }

    // ─────────────────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        DrawToolbar();

        if (_database == null)
        {
            EditorGUILayout.HelpBox("Chưa gán WeaponData. Kéo ScriptableObject vào ô trên.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawList();
        DrawDetail();
        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUILayout.LabelField("WeaponData:", GUILayout.Width(90));
        _database = (WeaponData)EditorGUILayout.ObjectField(
            _database, typeof(WeaponData), false, GUILayout.Width(200));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ Add Weapon", EditorStyles.toolbarButton, GUILayout.Width(100)))
            AddWeapon();

        if (_selectedIndex >= 0 && _database != null && _database.Weapons != null
            && _selectedIndex < _database.Weapons.Length)
        {
            if (GUILayout.Button("Remove Selected", EditorStyles.toolbarButton, GUILayout.Width(120)))
                RemoveWeapon(_selectedIndex);
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
            SaveDatabase();

        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────
    private void DrawList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LIST_WIDTH));
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

        if (_database.Weapons != null)
        {
            for (int i = 0; i < _database.Weapons.Length; i++)
            {
                var w = _database.Weapons[i];
                if (w == null) continue;

                bool selected = i == _selectedIndex;
                if (selected)
                    GUI.Box(GUILayoutUtility.GetRect(LIST_WIDTH, 48), GUIContent.none, _selectedStyle);

                EditorGUILayout.BeginHorizontal(GUILayout.Height(48));

                // Icon preview
                var icon = w.SpriteTier1 != null ? w.SpriteTier1.texture : null;
                if (icon != null)
                    GUILayout.Label(icon, GUILayout.Width(44), GUILayout.Height(44));
                else
                    GUILayout.Label("[?]", GUILayout.Width(44), GUILayout.Height(44));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"#{w.ID} {w.Name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Lv{w.Level}  {(w.IsLocked ? "🔒" : "✅")}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _selectedIndex = i;
                    GUI.changed    = true;
                    Repaint();
                }

                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────
    private void DrawDetail()
    {
        if (_selectedIndex < 0 || _database.Weapons == null
            || _selectedIndex >= _database.Weapons.Length)
        {
            GUILayout.Label("Chọn một vũ khí từ danh sách bên trái.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        var w = _database.Weapons[_selectedIndex];
        if (w == null) return;

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        Undo.RecordObject(_database, "Edit Weapon");

        // ─── Identity ─────────────────────────────────────
        _foldIdentity = EditorGUILayout.Foldout(_foldIdentity, "Identity", true, _headerStyle);
        if (_foldIdentity)
        {
            EditorGUI.indentLevel++;
            w.ID       = EditorGUILayout.IntField("ID",   w.ID);
            w.Name     = EditorGUILayout.TextField("Name", w.Name);
            w.IsLocked = EditorGUILayout.Toggle("Locked",  w.IsLocked);

            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            int newLevelLock = EditorGUILayout.IntField(
                new GUIContent("Level Lock", "Level Player tối thiểu để mở khóa weapon (0 = không yêu cầu)"),
                w.LevelLock);
            if (EditorGUI.EndChangeCheck())
                w.LevelLock = Mathf.Max(0, newLevelLock);
            if (w.LevelLock > 0)
                EditorGUILayout.HelpBox($"Yêu cầu Player đạt Level {w.LevelLock} để mở khóa.", MessageType.None);
            else
                EditorGUILayout.HelpBox("Không yêu cầu level Player (mở sẵn theo IsLocked).", MessageType.None);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Tier Sprites ──────────────────────────────────
        _foldSprites = EditorGUILayout.Foldout(_foldSprites, "Tier Sprites (1-4)", true, _headerStyle);
        if (_foldSprites)
        {
            EditorGUI.indentLevel++;
            w.SpriteTier1 = (Sprite)EditorGUILayout.ObjectField("Tier 1 (Icon)", w.SpriteTier1, typeof(Sprite), false);
            w.SpriteTier2 = (Sprite)EditorGUILayout.ObjectField("Tier 2",        w.SpriteTier2, typeof(Sprite), false);
            w.SpriteTier3 = (Sprite)EditorGUILayout.ObjectField("Tier 3",        w.SpriteTier3, typeof(Sprite), false);
            w.SpriteTier4 = (Sprite)EditorGUILayout.ObjectField("Tier 4",        w.SpriteTier4, typeof(Sprite), false);
            w.ShapeSprite = (Sprite)EditorGUILayout.ObjectField("Shape Sprite",  w.ShapeSprite, typeof(Sprite), false);

            // Big preview
            if (w.SpriteTier1 != null)
            {
                EditorGUILayout.Space(4);
                var rect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE, GUILayout.ExpandWidth(false));
                rect.x += 16f;
                EditorGUI.DrawPreviewTexture(rect, w.SpriteTier1.texture);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Level ─────────────────────────────────────────
        _foldLevel = EditorGUILayout.Foldout(_foldLevel, "Level & XP", true, _headerStyle);
        if (_foldLevel)
        {
            EditorGUI.indentLevel++;
            w.Level         = EditorGUILayout.IntSlider("Level (1-5)", w.Level, 1, 5);
            w.XP            = EditorGUILayout.IntField("XP",              w.XP);
            w.XPToNextLevel = EditorGUILayout.IntField("XP to Next Level", w.XPToNextLevel);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Stats per Level ───────────────────────────────
        _foldStats = EditorGUILayout.Foldout(_foldStats, "Stats per Level (index 0=Lv1 … 4=Lv5)", true, _headerStyle);
        if (_foldStats)
        {
            EditorGUI.indentLevel++;

            EnsureArray(ref w.DamagePerLevel, 5);
            EnsureArray(ref w.HPPerLevel,     5);

            EditorGUILayout.LabelField("Damage per Level", EditorStyles.boldLabel);
            for (int i = 0; i < 5; i++)
                w.DamagePerLevel[i] = EditorGUILayout.FloatField($"  Lv{i + 1}", w.DamagePerLevel[i]);

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("HP per Level", EditorStyles.boldLabel);
            for (int i = 0; i < 5; i++)
                w.HPPerLevel[i] = EditorGUILayout.FloatField($"  Lv{i + 1}", w.HPPerLevel[i]);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Current Damage (Lv{w.Level}): {w.GetCurrentDamage():0}\n" +
                $"Current HP     (Lv{w.Level}): {w.GetCurrentHP():0}",
                MessageType.Info);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Upgrade ──────────────────────────────────────
        _foldUpgrade = EditorGUILayout.Foldout(_foldUpgrade, "Upgrade", true, _headerStyle);
        if (_foldUpgrade)
        {
            EditorGUI.indentLevel++;
            w.Coin = EditorGUILayout.IntField("Coin Cost", w.Coin);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Spawn ────────────────────────────────────────
        _foldSpawn = EditorGUILayout.Foldout(_foldSpawn, "Spawn", true, _headerStyle);
        if (_foldSpawn)
        {
            EditorGUI.indentLevel++;
            w.TimeDelay = EditorGUILayout.FloatField("Time Delay (s)", w.TimeDelay);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ─── Grid Cells ───────────────────────────────────
        _foldGrid = EditorGUILayout.Foldout(_foldGrid, $"Grid Cells ({(w.GridCells != null ? w.GridCells.Length : 0)})", true, _headerStyle);
        if (_foldGrid)
        {
            EditorGUI.indentLevel++;

            int newCount = EditorGUILayout.IntField("Count", w.GridCells != null ? w.GridCells.Length : 0);
            if (newCount != (w.GridCells != null ? w.GridCells.Length : 0))
            {
                var newArr = new WeaponGridCell[Mathf.Max(0, newCount)];
                if (w.GridCells != null)
                    for (int i = 0; i < Mathf.Min(newArr.Length, w.GridCells.Length); i++)
                        newArr[i] = w.GridCells[i];
                for (int i = w.GridCells != null ? w.GridCells.Length : 0; i < newArr.Length; i++)
                    newArr[i] = new WeaponGridCell();
                w.GridCells = newArr;
            }

            if (w.GridCells != null)
            {
                for (int i = 0; i < w.GridCells.Length; i++)
                {
                    var cell = w.GridCells[i];
                    if (cell == null) { w.GridCells[i] = new WeaponGridCell(); cell = w.GridCells[i]; }
                    EditorGUILayout.BeginHorizontal();
                    cell.gridPosition = EditorGUILayout.Vector2IntField($"Cell {i}", cell.gridPosition);
                    cell.isOccupied   = EditorGUILayout.Toggle("Occ", cell.isOccupied, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("💾  Save Asset", GUILayout.Height(32)))
            SaveDatabase();

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
            EditorUtility.SetDirty(_database);
    }

    // ─────────────────────────────────────────────────────────
    private void AddWeapon()
    {
        if (_database == null) return;
        Undo.RecordObject(_database, "Add Weapon");

        int newID = 1;
        if (_database.Weapons != null)
        {
            foreach (var w in _database.Weapons)
                if (w != null && w.ID >= newID) newID = w.ID + 1;
        }

        var newEntry = new WeaponEntry
        {
            ID             = newID,
            Name           = "New Weapon " + newID,
            Level          = 1,
            DamagePerLevel = new float[5],
            HPPerLevel     = new float[5],
            IsLocked       = true,
            LevelLock      = 0
        };

        var list = _database.Weapons != null
            ? new System.Collections.Generic.List<WeaponEntry>(_database.Weapons)
            : new System.Collections.Generic.List<WeaponEntry>();
        list.Add(newEntry);
        _database.Weapons = list.ToArray();

        _selectedIndex = _database.Weapons.Length - 1;
        EditorUtility.SetDirty(_database);
        Repaint();
    }

    private void RemoveWeapon(int index)
    {
        if (_database == null || _database.Weapons == null) return;
        if (!EditorUtility.DisplayDialog("Xác nhận",
            $"Xóa vũ khí \"{_database.Weapons[index].Name}\"?", "Xóa", "Huỷ")) return;

        Undo.RecordObject(_database, "Remove Weapon");
        var list = new System.Collections.Generic.List<WeaponEntry>(_database.Weapons);
        list.RemoveAt(index);
        _database.Weapons = list.ToArray();
        _selectedIndex    = Mathf.Clamp(_selectedIndex - 1, -1, _database.Weapons.Length - 1);
        EditorUtility.SetDirty(_database);
        Repaint();
    }

    private void SaveDatabase()
    {
        if (_database == null) return;
        EditorUtility.SetDirty(_database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WeaponEditWindow] Saved.");
    }

    // ─────────────────────────────────────────────────────────
    private static void EnsureArray(ref float[] arr, int size)
    {
        if (arr == null || arr.Length != size)
        {
            var n = new float[size];
            if (arr != null)
                for (int i = 0; i < Mathf.Min(arr.Length, size); i++) n[i] = arr[i];
            arr = n;
        }
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }
}
