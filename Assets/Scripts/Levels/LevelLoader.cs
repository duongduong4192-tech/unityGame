using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Khung đa giác của màn đang chơi (dùng để kiểm tra thắng): điểm nằm trong đa giác hoặc sát mép (sai số nhỏ).
public class FrameShape : MonoBehaviour
{
    public static FrameShape Current;
    public Vector2[] world;      // các đỉnh khung (tọa độ thế giới)

    void OnEnable() => Current = this;
    void OnDisable() { if (Current == this) Current = null; }

    public bool Contains(Vector2 p, float tol)
    {
        bool inside = false;
        for (int i = 0, j = world.Length - 1; i < world.Length; j = i++)
        {
            Vector2 a = world[i], b = world[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        if (inside) return true;
        for (int i = 0, j = world.Length - 1; i < world.Length; j = i++)
        {
            Vector2 a = world[j], ab = world[i] - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-9f));
            if ((a + ab * t - p).sqrMagnitude <= tol * tol) return true;
        }
        return false;
    }
}

// Dựng màn tạo sẵn ngay khi PuzzleScene vừa nạp (sau Awake, trước Start của các script khác):
// ẩn bộ số mặc định của scene, tạo khung + tường + các mảnh gỗ, rồi rải mảnh ra bàn.
public static class LevelLoader
{
    const float AREA_W = 6.4f, AREA_H = 5.2f;      // vùng khung trên bàn (đơn vị thế giới)
    const float TABLE_HALF_W = 8.5f, TABLE_HALF_H = 4.3f;
    const float HUD_MIN_X = 5.2f, HUD_MIN_Y = 1.7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameFlow.PUZZLE_SCENE) return;
        if (!string.IsNullOrEmpty(GameFlow.LevelId))
        {
            LevelData d = LevelData.Load(GameFlow.LevelCat, GameFlow.LevelId);
            if (d == null) Debug.LogWarning($"[ChillPuzzle] Không tìm thấy màn {GameFlow.LevelCat}/{GameFlow.LevelId}");
            else Build(d);
        }

        // lưu màn đang chơi dở; có bản lưu thì đặt mảnh về chỗ cũ, không chạy hiệu ứng rơi
        bool resumed = LevelSave.HasSave;
        new GameObject("LevelSave").AddComponent<LevelSave>();
        if (resumed) return;

        // mảnh gỗ rơi xuống bàn (cả màn hướng dẫn Số)
        List<PuzzlePiece> all = new List<PuzzlePiece>(Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Exclude));
        for (int i = all.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (all[i], all[j]) = (all[j], all[i]); }
        for (int i = 0; i < all.Count; i++) all[i].gameObject.AddComponent<PieceDrop>().index = i;
    }

    static void Build(LevelData d)
    {
        // 1. tắt bộ số và khung chữ nhật có sẵn trong scene
        foreach (PuzzlePiece p in Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Exclude))
        { p.gameObject.SetActive(false); Object.Destroy(p.gameObject); }
        foreach (string n in new[] { "FrameVisual", "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right" })
        { GameObject g = GameObject.Find(n); if (g != null) g.SetActive(false); }

        Texture2D tex = d.Atlas;
        Vector2 fs = d.FrameSize;
        float s = Mathf.Min(AREA_W / fs.x, AREA_H / fs.y, 1.25f);

        // 2. khung: ảnh + bóng + tường vòng (lòng khung là lỗ) + hình dạng để kiểm tra thắng
        GameObject frame = new GameObject("LevelFrame");
        frame.transform.localScale = new Vector3(s, s, 1);
        SpriteRenderer fr = frame.AddComponent<SpriteRenderer>();
        fr.sprite = d.MakeSprite(tex, d.frameRect, "frame");
        fr.sortingOrder = -20;
        GameObject sh = new GameObject("Shadow");
        sh.transform.SetParent(frame.transform, false);
        sh.transform.localPosition = new Vector3(0.08f, -0.12f, 0) / s;
        SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
        ssr.sprite = fr.sprite; ssr.color = new Color(0.12f, 0.06f, 0.02f, 0.38f); ssr.sortingOrder = -21;

        PolygonCollider2D walls = frame.AddComponent<PolygonCollider2D>();
        walls.pathCount = 2;
        walls.SetPath(0, LevelData.Points(d.outer));
        walls.SetPath(1, LevelData.Points(d.frame));

        FrameShape shape = frame.AddComponent<FrameShape>();
        shape.world = LevelData.Points(d.frame, s);
        LevelSnap.Setup(d.cat, s, shape.world[0]);

        // 3. các mảnh
        GameObject parent = GameObject.Find("Pieces") ?? new GameObject("Pieces");
        List<PuzzlePiece> made = new List<PuzzlePiece>();
        for (int i = 0; i < d.pieces.Length; i++)
        {
            LevelPiece lp = d.pieces[i];
            GameObject go = new GameObject($"{d.id}_p{i}");
            go.transform.SetParent(parent.transform, false);
            go.transform.localScale = new Vector3(s, s, 1);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = d.MakeSprite(tex, lp.rect, go.name);       // phải có ảnh TRƯỚC khi thêm PuzzlePiece (tạo bóng)
            PolygonCollider2D col = go.AddComponent<PolygonCollider2D>();
            col.pathCount = 1;
            col.SetPath(0, LevelData.Points(lp.path));
            made.Add(go.AddComponent<PuzzlePiece>());
            if (lp.sol != null && lp.sol.Length >= 2)
                go.AddComponent<LevelSnap>().Init(LevelData.Points(lp.path), new Vector2(lp.sol[0], lp.sol[1]), s, d.cat != "circle");
        }

        Scatter(made, fs.x * s / 2f + 0.55f);
    }

    // rải mảnh hai bên khung, không chồng nhau, tránh góc các nút
    static void Scatter(List<PuzzlePiece> pieces, float keepOut)
    {
        foreach (PuzzlePiece p in pieces) p.transform.position = new Vector3(1000 + Random.value * 500, 1000, 0);
        Physics2D.SyncTransforms();
        List<Collider2D> hits = new List<Collider2D>();
        pieces.Sort((a, b) => Area(b).CompareTo(Area(a)));        // mảnh to đặt trước
        foreach (PuzzlePiece p in pieces)
        {
            Collider2D col = p.GetComponent<Collider2D>();
            for (int t = 0; t < 400; t++)
            {
                float side = Random.value < 0.5f ? -1 : 1;
                Vector3 pos = new Vector3(side * Random.Range(keepOut, TABLE_HALF_W), Random.Range(-TABLE_HALF_H, TABLE_HALF_H), 0);
                p.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
                Physics2D.SyncTransforms();
                Bounds b = col.bounds;
                bool onTable = b.min.x > -TABLE_HALF_W - 0.3f && b.max.x < TABLE_HALF_W + 0.3f &&
                               b.min.y > -TABLE_HALF_H - 0.4f && b.max.y < TABLE_HALF_H + 0.4f &&
                               Mathf.Sign(b.min.x) == Mathf.Sign(b.max.x) &&
                               Mathf.Min(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x)) > keepOut - 0.1f;
                bool underHud = b.max.x > HUD_MIN_X && b.max.y > HUD_MIN_Y;
                if (!onTable || underHud) continue;
                hits.Clear();
                col.Overlap(ContactFilter2D.noFilter, hits);
                if (hits.Count == 0) break;
            }
        }
    }

    static float Area(PuzzlePiece p) { Bounds b = p.GetComponent<Collider2D>().bounds; return b.size.x * b.size.y; }
}
