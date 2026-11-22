using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Công cụ dựng màn (chỉ chạy trong Editor, không đóng gói vào game).
// Menu: ChillPuzzle → Level → ...
//   1. Build Level From Folder : tạo mảnh từ thư mục ảnh (mặc định Assets/Sprites/Digits),
//                                đọc digits_meta.json (nếu có): viền va chạm chính xác + co khung theo lưới
//   2. Scatter Pieces          : rải mảnh quanh bàn, xoay ngẫu nhiên
public static class LevelBuilderTool
{
    const string DEFAULT_FOLDER = "Assets/Sprites/Digits";
    const string META_NAME = "digits_meta.json";
    const float DEFAULT_PPU = 250f;
    const float FRAME_MARGIN = 0.08f;  // khoảng dư mỗi bên giữa mép thanh và viền khung
    const float WALL = 0.5f;           // độ dày viền khung
    const float TOLERANCE = 0.002f;
    const float TABLE_HALF_W = 8.6f, TABLE_HALF_H = 4.4f;
    const float HUD_MIN_X = 5.4f, HUD_MIN_Y = 1.8f;   // vùng các mẩu giấy Menu / Hoàn tác / Chơi lại

    [System.Serializable] class PieceMeta { public string name; public float offsetX; public float offsetY; public float[] path; }
    [System.Serializable] class LevelMeta
    {
        public float cellPx; public float cellWorld; public int cols; public int rows; public float barHalfPx; public bool snap;
        public PieceMeta[] pieces;
    }

    // ===================== 1. DỰNG MÀN =====================
    [MenuItem("ChillPuzzle/Level/1. Build Level From Folder")]
    public static void BuildLevel()
    {
        if (!CheckEditMode()) return;

        string folder = DEFAULT_FOLDER;
        string sel = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
        if (!string.IsNullOrEmpty(sel) && AssetDatabase.IsValidFolder(sel)) folder = sel;
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("ChillPuzzle", $"Không tìm thấy thư mục {folder}.", "OK");
            return;
        }

        // Đọc thông số lưới (nếu có)
        LevelMeta meta = null;
        TextAsset metaAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{META_NAME}");
        if (metaAsset != null) meta = JsonUtility.FromJson<LevelMeta>(metaAsset.text);
        float ppu = meta != null && meta.cellPx > 0 && meta.cellWorld > 0 ? meta.cellPx / meta.cellWorld : DEFAULT_PPU;

        // Chuẩn hóa thiết lập ảnh: Sprite, PPU chung, tâm ở giữa
        string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string g in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool changed = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                           || !Mathf.Approximately(ti.spritePixelsPerUnit, ppu) || ti.mipmapEnabled || !ti.alphaIsTransparency;
            if (!changed) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }

        List<Sprite> sprites = texGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .OrderBy(s => s.name, new NaturalComparer())
            .ToList();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", $"Thư mục {folder} không có ảnh nào.", "OK");
            return;
        }

        // Xóa mảnh cũ
        foreach (PuzzlePiece old in Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include))
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject parent = GameObject.Find("Pieces");
        if (parent == null)
        {
            parent = new GameObject("Pieces");
            Undo.RegisterCreatedObjectUndo(parent, "Create Pieces");
        }

        foreach (Sprite s in sprites)
        {
            GameObject go = new GameObject(s.name);
            Undo.RegisterCreatedObjectUndo(go, "Create Piece");
            go.transform.SetParent(parent.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = 0;
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>();   // tự thêm PolygonCollider2D theo hình mảnh

            PieceMeta pm = meta?.pieces?.FirstOrDefault(x => x.name == s.name);

            // Viền va chạm chính xác theo hình vẽ (thay cho viền Unity tự ước lượng)
            if (pm != null && pm.path != null && pm.path.Length >= 6)
            {
                PolygonCollider2D poly = go.GetComponent<PolygonCollider2D>();
                Vector2[] pts = new Vector2[pm.path.Length / 2];
                for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(pm.path[2 * i], pm.path[2 * i + 1]);
                poly.pathCount = 1;
                poly.SetPath(0, pts);
            }

            if (pm != null && meta.snap)
            {
                pp.hasGridOffset = true;
                // meta: tâm ảnh so với đỉnh lưới (ô, y hướng xuống) -> offset: tâm ảnh tới đỉnh (Unity, y hướng lên)
                pp.gridOffset = new Vector2(-pm.offsetX * meta.cellWorld, pm.offsetY * meta.cellWorld);
            }
        }

        GameObject gm = GameObject.Find("GameManager");
        if (gm != null)
        {
            // gỡ phần lưu đáp án cũ (nếu còn)
            Component oldSol = gm.GetComponent("LevelSolution");
            if (oldSol != null) Undo.DestroyObjectImmediate(oldSol);

            GridBoard board = gm.GetComponent<GridBoard>();
            if (meta != null && meta.snap)
            {
                if (board == null) board = Undo.AddComponent<GridBoard>(gm);
                Undo.RecordObject(board, "Setup Grid");
                board.cols = meta.cols;
                board.rows = meta.rows;
                board.cellSize = meta.cellWorld;
                board.center = Vector2.zero;
            }
            else if (board != null)
            {
                Undo.DestroyObjectImmediate(board);   // không hít lưới -> đặt tự do
            }

            // Khung vừa với lưới thiết kế (cộng độ dày thanh + khoảng dư nhỏ)
            if (meta != null && meta.cols > 0)
            {
                float bar = meta.barHalfPx / ppu;
                FitFrame(meta.cols * meta.cellWorld + 2 * (bar + FRAME_MARGIN),
                         meta.rows * meta.cellWorld + 2 * (bar + FRAME_MARGIN));
            }
        }

        Scatter();
        Debug.Log($"[ChillPuzzle] Đã dựng màn: {sprites.Count} mảnh từ {folder}" +
                  (meta != null ? $", khung theo lưới {meta.cols}x{meta.rows}, hít lưới: {(meta.snap ? "bật" : "tắt")}." : "."));
    }

    // ===================== 2. RẢI MẢNH =====================
    [MenuItem("ChillPuzzle/Level/2. Scatter Pieces")]
    public static void Scatter()
    {
        if (!CheckEditMode()) return;
        PuzzlePiece[] pieces = Object.FindObjectsByType<PuzzlePiece>();

        GameObject inside = GameObject.Find("FrameInside");
        float frameHalf = inside != null ? inside.transform.localScale.x / 2 : 3f;
        float keepOut = frameHalf + WALL + 0.3f;   // vùng cấm quanh khung (theo chiều ngang)

        for (int i = 0; i < pieces.Length; i++)
        {
            Undo.RecordObject(pieces[i].transform, "Scatter");
            pieces[i].transform.position = new Vector3(1000 + i * 10, 1000, 0);
        }
        Physics2D.SyncTransforms();

        int failed = 0;
        foreach (PuzzlePiece p in pieces.OrderBy(_ => Random.value))
        {
            bool placed = false;
            for (int t = 0; t < 400 && !placed; t++)
            {
                float side = Random.value < 0.5f ? -1 : 1;
                float x = side * Random.Range(keepOut, TABLE_HALF_W);
                float y = Random.Range(-TABLE_HALF_H, TABLE_HALF_H);
                p.transform.SetPositionAndRotation(new Vector3(x, y, 0), Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
                Physics2D.SyncTransforms();
                Bounds pb = Col(p).bounds;
                bool onTable = pb.min.x > -TABLE_HALF_W - 0.4f && pb.max.x < TABLE_HALF_W + 0.4f &&
                               pb.min.y > -TABLE_HALF_H - 0.4f && pb.max.y < TABLE_HALF_H + 0.4f &&
                               Mathf.Min(Mathf.Abs(pb.min.x), Mathf.Abs(pb.max.x)) > keepOut - 0.05f &&
                               Mathf.Sign(pb.min.x) == Mathf.Sign(pb.max.x);
                bool underHud = pb.max.x > HUD_MIN_X && pb.max.y > HUD_MIN_Y;   // tránh 3 nút góc trên-phải
                placed = onTable && !underHud && IsFree(p);
            }
            if (!placed) failed++;
        }
        SaveScene();
        if (failed > 0)
            Debug.LogWarning($"[ChillPuzzle] {failed} mảnh không tìm được chỗ trống trên bàn. Thử chạy Scatter lại.");
    }

    // ===================== HỖ TRỢ =====================
    static void FitFrame(float w, float h)
    {
        GameObject inside = GameObject.Find("FrameInside");
        GameObject top = GameObject.Find("Wall_Top"), bottom = GameObject.Find("Wall_Bottom");
        GameObject left = GameObject.Find("Wall_Left"), right = GameObject.Find("Wall_Right");
        if (inside == null || top == null || bottom == null || left == null || right == null)
        {
            Debug.LogWarning("[ChillPuzzle] Không tìm thấy FrameInside hoặc 4 viền Wall_..., bỏ qua bước chỉnh khung.");
            return;
        }
        float ox = w / 2 + WALL / 2, oy = h / 2 + WALL / 2;
        Set(inside, Vector2.zero, new Vector2(w, h));
        Set(top, new Vector2(0, oy), new Vector2(w + 2 * WALL, WALL));
        Set(bottom, new Vector2(0, -oy), new Vector2(w + 2 * WALL, WALL));
        Set(left, new Vector2(-ox, 0), new Vector2(WALL, h));
        Set(right, new Vector2(ox, 0), new Vector2(WALL, h));
    }

    static void Set(GameObject go, Vector2 pos, Vector2 scale)
    {
        Undo.RecordObject(go.transform, "Fit Frame");
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
    }

    static bool IsFree(PuzzlePiece p)
    {
        List<Collider2D> hits = new List<Collider2D>();
        Col(p).Overlap(ContactFilter2D.noFilter, hits);
        foreach (Collider2D h in hits)
        {
            ColliderDistance2D d = Col(p).Distance(h);
            if (d.isValid && d.isOverlapped && d.distance < -TOLERANCE) return false;
        }
        return true;
    }

    static Collider2D Col(PuzzlePiece p) => p.GetComponent<Collider2D>();

    static bool CheckEditMode()
    {
        if (!EditorApplication.isPlaying) return true;
        EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi dùng công cụ.", "OK");
        return false;
    }

    static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // So sánh tên kiểu tự nhiên: Digit_2 < Digit_10
    class NaturalComparer : IComparer<string>
    {
        public int Compare(string a, string b) => EditorUtility.NaturalCompare(a, b);
    }
}
