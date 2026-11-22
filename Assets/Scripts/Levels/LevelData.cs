using System;
using System.Collections.Generic;
using UnityEngine;

// Dữ liệu màn chơi tạo sẵn (Assets/Resources/Levels/<thể loại>/<id>.png + .json).
// Mọi tọa độ trong json tính theo "ô" (1 ô = ppu điểm ảnh), gốc ở tâm khung.
[Serializable] public class LevelPiece { public int[] rect; public float[] path; public float[] sol; }

[Serializable]
public class LevelData
{
    public string id, cat, title;
    public int tier, n;
    public float ppu;
    public float[] frame, outer;
    public int[] frameRect, thumbRect;
    public LevelPiece[] pieces;

    public static LevelData Load(string cat, string id)
    {
        TextAsset ta = Resources.Load<TextAsset>($"Levels/{cat}/{id}");
        return ta == null ? null : JsonUtility.FromJson<LevelData>(ta.text);
    }

    public Texture2D Atlas => Resources.Load<Texture2D>($"Levels/{cat}/{id}");

    // rect trong json tính từ góc TRÊN-trái của ảnh; Unity tính từ góc DƯỚI-trái
    public Sprite MakeSprite(Texture2D tex, int[] r, string name)
    {
        Sprite s = Sprite.Create(tex, new Rect(r[0], tex.height - r[1] - r[3], r[2], r[3]),
                                 new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }

    public static Vector2[] Points(float[] flat, float scale = 1f)
    {
        Vector2[] pts = new Vector2[flat.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(flat[2 * i], flat[2 * i + 1]) * scale;
        return pts;
    }

    public Vector2 FrameSize
    {
        get
        {
            Vector2 mn = Vector2.positiveInfinity, mx = Vector2.negativeInfinity;
            foreach (Vector2 p in Points(frame)) { mn = Vector2.Min(mn, p); mx = Vector2.Max(mx, p); }
            return mx - mn;
        }
    }
}

// Danh sách thể loại + màn (Resources/Levels/catalog.json) và ảnh nhỏ của từng màn
[Serializable] public class CatalogLevel { public string id; public int tier, n; public int[] thumb; }
[Serializable] public class CatalogCategory { public string id; public CatalogLevel[] levels; }

[Serializable]
public class LevelCatalog
{
    public CatalogCategory[] categories;

    static LevelCatalog cached;
    public static LevelCatalog Get()
    {
        if (cached != null) return cached;
        TextAsset ta = Resources.Load<TextAsset>("Levels/catalog");
        cached = ta != null ? JsonUtility.FromJson<LevelCatalog>(ta.text) : new LevelCatalog { categories = new CatalogCategory[0] };
        return cached;
    }

    public CatalogCategory Find(string cat) => Array.Find(categories, c => c.id == cat);

    public static Texture2D Thumbs(string cat) => Resources.Load<Texture2D>($"Levels/{cat}/_thumbs");

    // màn kế tiếp trong cùng thể loại (null nếu đã hết)
    public string Next(string cat, string id)
    {
        CatalogCategory c = Find(cat);
        if (c == null) return null;
        int i = Array.FindIndex(c.levels, l => l.id == id);
        return i >= 0 && i + 1 < c.levels.Length ? c.levels[i + 1].id : null;
    }
}
