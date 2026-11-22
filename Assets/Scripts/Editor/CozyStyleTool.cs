using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Áp phong cách "tranh vẽ ấm" cho màn chơi: mặt bàn gỗ, khung gỗ 3 lớp, ẩn hình khối trắng cũ.
// Menu: ChillPuzzle → Level → 3. Apply Cozy Style   (chạy lại bao nhiêu lần cũng được)
// Ảnh nằm ở Assets/Sprites/Play — thay file cùng tên rồi chạy lại là đổi bàn / khung.
public static class CozyStyleTool
{
    const string FOLDER = "Assets/Sprites/Play";
    const string PUZZLE_SCENE = "Assets/Scenes/PuzzleScene.unity";
    const int ORDER_TABLE = -1000, ORDER_SHADOW = -30, ORDER_BOARD = -20, ORDER_WOOD = -10;

    // PPU 208: viền khung dày 104px trong ảnh = 0.5 đơn vị = đúng độ dày tường va chạm
    struct Imp { public string file; public float ppu; public Vector4 border; public Imp(string f, float p, Vector4 b) { file = f; ppu = p; border = b; } }
    static readonly Imp[] IMPORTS =
    {
        new Imp("table.png",        150f, Vector4.zero),
        new Imp("frame_wood.png",   208f, new Vector4(104, 90, 102, 104)),   // trái, dưới, phải, trên
        new Imp("frame_board.png",  208f, new Vector4(95, 24, 24, 80)),
        new Imp("frame_shadow.png", 170f, new Vector4(60, 60, 60, 60)),
    };

    [MenuItem("ChillPuzzle/Level/3. Apply Cozy Style")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi dùng công cụ.", "OK");
            return;
        }

        // 1. Thiết lập ảnh
        foreach (Imp im in IMPORTS)
        {
            string path = $"{FOLDER}/{im.file}";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                EditorUtility.DisplayDialog("ChillPuzzle", $"Không thấy ảnh {path}.", "OK");
                return;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = im.ppu;
            ti.spriteBorder = im.border;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;      // bắt buộc cho kiểu kéo giãn 9 mảnh
            st.spriteAlignment = (int)SpriteAlignment.Center;
            st.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        Sprite table = Load("table"), wood = Load("frame_wood"), board = Load("frame_board"), shadow = Load("frame_shadow");

        // 2. Ẩn hình khối của khung cũ (giữ nguyên va chạm + vùng thắng)
        GameObject inside = GameObject.Find("FrameInside");
        if (inside == null && System.IO.File.Exists(PUZZLE_SCENE))
        {
            // đang mở nhầm scene (vd. MainMenu) -> tự mở scene màn chơi
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PUZZLE_SCENE);
            inside = GameObject.Find("FrameInside");
        }
        if (inside == null)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Không tìm thấy 'FrameInside' trong scene.", "OK");
            return;
        }
        HashSet<GameObject> keep = new HashSet<GameObject> { inside };
        foreach (string n in new[] { "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right" })
        {
            GameObject w = GameObject.Find(n);
            if (w != null) keep.Add(w);
        }
        foreach (GameObject go in keep)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Undo.RecordObject(sr, "Hide frame block");
            Color c = sr.color; c.a = 0f; sr.color = c;     // trong suốt nhưng vẫn bật (WinChecker cần kích thước)
        }

        // 3. Tạo / cập nhật mặt bàn
        GameObject tableGo = FindOrCreate("CozyTable", null);
        SpriteRenderer tsr = Get<SpriteRenderer>(tableGo);
        tsr.sprite = table; tsr.sortingOrder = ORDER_TABLE; tsr.drawMode = SpriteDrawMode.Simple;
        Get<TableFit>(tableGo);

        // 4. Tạo / cập nhật khung
        GameObject root = FindOrCreate("FrameVisual", null);
        FrameVisual fv = Get<FrameVisual>(root);
        Undo.RecordObject(fv, "Setup FrameVisual");
        fv.frameInside = inside.GetComponent<SpriteRenderer>();
        fv.shadow = Layer(root, "Shadow", shadow, ORDER_SHADOW, new Color(1, 1, 1, fv.shadowAlpha));
        fv.board = Layer(root, "Board", board, ORDER_BOARD, Color.white);
        fv.wood = Layer(root, "Wood", wood, ORDER_WOOD, Color.white);
        fv.Fit();

        // 5. Tắt các hình nền cũ (không phải mảnh, khung, bàn mới) — chỉ tắt hình, không xóa
        List<string> off = new List<string>();
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            GameObject go = sr.gameObject;
            if (!sr.enabled || keep.Contains(go) || go == tableGo || go.transform.IsChildOf(root.transform)) continue;
            if (go.GetComponentInParent<PuzzlePiece>(true) != null) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            Undo.RecordObject(sr, "Disable old background");
            sr.enabled = false;
            off.Add(go.name);
        }

        // chữ "New Text" thừa (tạo tay lúc đầu) -> ẩn đi
        foreach (TMPro.TMP_Text t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude))
        {
            if (t.text != "New Text" || t.gameObject.name == "WinText") continue;
            Undo.RecordObject(t.gameObject, "Hide leftover text");
            t.gameObject.SetActive(false);
            off.Add(t.gameObject.name);
        }

        // 6. Rải lại mảnh (tránh khung mới + 3 nút góc phải), hàm này tự lưu scene
        LevelBuilderTool.Scatter();
        Debug.Log("[ChillPuzzle] Đã áp phong cách gỗ ấm." +
                  (off.Count > 0 ? $" Đã tắt hình cũ: {string.Join(", ", off)} (bật lại ô SpriteRenderer nếu cần)." : ""));
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{FOLDER}/{name}.png");

    static GameObject FindOrCreate(string name, Transform parent)
    {
        Transform t = parent != null ? parent.Find(name) : null;
        GameObject go = t != null ? t.gameObject : (parent == null ? GameObject.Find(name) : null);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            if (parent != null) go.transform.SetParent(parent, false);
        }
        return go;
    }

    static T Get<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static SpriteRenderer Layer(GameObject root, string name, Sprite sp, int order, Color color)
    {
        GameObject go = FindOrCreate(name, root.transform);
        SpriteRenderer sr = Get<SpriteRenderer>(go);
        Undo.RecordObject(sr, "Setup " + name);
        sr.sprite = sp;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.sortingOrder = order;
        sr.color = color;
        return sr;
    }
}
