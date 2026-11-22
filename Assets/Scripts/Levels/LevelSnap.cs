using System.Collections.Generic;
using UnityEngine;

// "Nam châm" cho màn tạo sẵn — KHÔNG biết đáp án, nên không lộ gợi ý:
//  - Ghép khối: góc xoay hút về bội số 90°, đỉnh mảnh bám vào lưới ô của khung.
//  - Hình học / Đường cong: hút theo MÉP — mép mảnh gần song song & gần sát mép khung hoặc mép mảnh khác
//    thì xoay cho song song rồi áp sát; góc gần trùng góc thì khớp góc. Đặt sai chỗ vẫn hút như thường.
//  - Hình tròn: không hút (khung rộng, đặt đâu cũng được).
// LevelLoader gắn component này vào từng mảnh khi dựng màn.
public class LevelSnap : MonoBehaviour
{
    enum Mode { None, Grid, Edge }

    public static bool Active;            // đang chơi màn tạo sẵn
    static Mode mode;
    static float unit;                    // 1 đơn vị màn = bao nhiêu đơn vị thế giới
    static Vector2 gridOrigin;
    static readonly List<LevelSnap> all = new List<LevelSnap>();

    const float RANGE = 0.3f;             // khoảng hút (tính theo đơn vị màn, ~1/3 ô)
    const float ROT_RANGE = 10f;          // lệch góc tối đa để tự xoay cho song song
    const float GRID_ROT_RANGE = 18f;
    const float MIN_EDGE = 0.1f;           // bỏ qua mép quá ngắn (đoạn cong nhỏ)

    Vector2[] local;
    Transform tr;

    public static void Setup(string cat, float scale, Vector2 frameVertex0)
    {
        all.Clear();
        Active = true;
        unit = scale;
        mode = cat == "blocks" ? Mode.Grid : cat == "circle" ? Mode.None : Mode.Edge;
        gridOrigin = frameVertex0;
    }

    void OnDestroy() { all.Remove(this); if (all.Count == 0) Active = false; }

    public void Init(Vector2[] path, Vector2 sol, float scale, bool enabledSnap)
    {
        local = path;
        tr = transform;
        all.Add(this);
    }

    Vector2[] World(Vector2 pos, float rot)
    {
        Quaternion q = Quaternion.Euler(0, 0, rot);
        Vector2[] w = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++) w[i] = pos + (Vector2)(q * (local[i] * unit));
        return w;
    }

    // Chỗ hút cho mảnh vừa thả (rỗng nếu không có gì đủ gần)
    public static List<(Vector3 pos, float rot)> Candidates(PuzzlePiece p)
    {
        List<(Vector3, float)> res = new List<(Vector3, float)>();
        LevelSnap me = p.GetComponent<LevelSnap>();
        if (me == null || !all.Contains(me) || mode == Mode.None) return res;
        Vector2 cur = p.transform.position;
        float rot = p.transform.eulerAngles.z;
        bool ok = mode == Mode.Grid ? me.GridSnap(ref cur, ref rot) : me.EdgeSnap(ref cur, ref rot);
        if (ok) res.Add((new Vector3(cur.x, cur.y, p.transform.position.z), rot));
        return res;
    }

    // ---------- Ghép khối: lưới ô ----------
    bool GridSnap(ref Vector2 pos, ref float rot)
    {
        float r = Mathf.Round(rot / 90f) * 90f;
        if (Mathf.Abs(Mathf.DeltaAngle(rot, r)) > GRID_ROT_RANGE) return false;
        Vector2 v0 = World(pos, r)[0];
        Vector2 g = (v0 - gridOrigin) / unit;
        Vector2 delta = new Vector2(Mathf.Round(g.x), Mathf.Round(g.y)) * unit + gridOrigin - v0;
        if (delta.magnitude > 0.4f * unit) return false;
        pos += delta; rot = r;
        return true;
    }

    // ---------- Hình học / Đường cong: hút theo mép ----------
    struct Seg { public Vector2 a, b; public Vector2 Dir => (b - a).normalized; public float Len => (b - a).magnitude; }

    static void AddEdges(List<Seg> list, Vector2[] poly)
    {
        for (int i = 0; i < poly.Length; i++)
        {
            Seg s = new Seg { a = poly[i], b = poly[(i + 1) % poly.Length] };
            if (s.Len >= MIN_EDGE * unit) list.Add(s);
        }
    }

    List<Seg> Targets(out List<Vector2> corners)
    {
        List<Seg> t = new List<Seg>();
        corners = new List<Vector2>();
        if (FrameShape.Current != null) { AddEdges(t, FrameShape.Current.world); corners.AddRange(FrameShape.Current.world); }
        foreach (LevelSnap o in all)
        {
            if (o == this || o == null) continue;
            Vector2[] w = o.World(o.tr.position, o.tr.eulerAngles.z);
            AddEdges(t, w);
            corners.AddRange(w);
        }
        return t;
    }

    // khoảng cách 2 đoạn gần song song: (khoảng cách theo pháp tuyến, có chồng lên nhau theo chiều dọc không)
    static bool Facing(Seg m, Seg t, float range, out float normalDist)
    {
        Vector2 d = t.Dir, n = new Vector2(-d.y, d.x);
        normalDist = Vector2.Dot((m.a + m.b) * 0.5f - t.a, n);
        if (Mathf.Abs(normalDist) > range) return false;
        float ta = Vector2.Dot(m.a - t.a, d), tb = Vector2.Dot(m.b - t.a, d);
        float lo = Mathf.Min(ta, tb), hi = Mathf.Max(ta, tb);
        return hi > 0.05f * unit && lo < t.Len - 0.05f * unit;      // hai mép thực sự đối diện nhau
    }

    static float LineAngleDiff(Vector2 a, Vector2 b)      // lệch góc giữa 2 đường thẳng (-90..90)
    {
        float d = Mathf.DeltaAngle(Mathf.Atan2(b.y, b.x) * Mathf.Rad2Deg, Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg);
        if (d > 90) d -= 180; if (d < -90) d += 180;
        return d;
    }

    bool EdgeSnap(ref Vector2 pos, ref float rot)
    {
        float range = RANGE * unit;
        List<Seg> targets = Targets(out List<Vector2> corners);
        if (targets.Count == 0) return false;

        // 1. xoay: cặp mép gần nhau nhất và gần song song -> xoay cho song song hẳn
        List<Seg> mine = new List<Seg>();
        AddEdges(mine, World(pos, rot));
        float bestScore = float.MaxValue, rotDelta = 0;
        bool any = false;
        foreach (Seg m in mine)
            foreach (Seg t in targets)
            {
                float ad = LineAngleDiff(m.Dir, t.Dir);
                if (Mathf.Abs(ad) > ROT_RANGE) continue;
                if (!Facing(m, t, range, out float nd)) continue;
                // ưu tiên cặp mép dài (vd. cạnh thẳng của khung) hơn các đoạn cong ngắn
                float score = Mathf.Abs(nd) + Mathf.Abs(ad) * 0.01f * unit - 0.2f * Mathf.Min(m.Len, t.Len);
                if (score < bestScore) { bestScore = score; rotDelta = ad; any = true; }
            }
        if (!any) return false;
        float newRot = rot - rotDelta;

        // 2. áp sát mép: dịch theo pháp tuyến của cặp mép song song gần nhất
        mine.Clear();
        AddEdges(mine, World(pos, newRot));
        Seg? first = null; float firstN = 0, bestA = float.MaxValue;
        foreach (Seg m in mine)
            foreach (Seg t in targets)
            {
                if (Mathf.Abs(LineAngleDiff(m.Dir, t.Dir)) > 1f) continue;
                if (!Facing(m, t, range, out float nd)) continue;
                float sc = Mathf.Abs(nd) - 0.2f * Mathf.Min(m.Len, t.Len);
                if (sc < bestA) { bestA = sc; first = t; firstN = nd; }
            }
        if (first == null) return false;
        Vector2 dA = first.Value.Dir, nA = new Vector2(-dA.y, dA.x);
        Vector2 newPos = pos - nA * firstN;

        // 3. trượt dọc mép đó: áp vào một mép khác không song song, hoặc khớp góc với góc gần nhất
        Vector2[] w = World(newPos, newRot);
        mine.Clear();
        AddEdges(mine, w);
        float bestSlide = float.MaxValue, slide = 0;
        foreach (Seg m in mine)
            foreach (Seg t in targets)
            {
                Vector2 dt = t.Dir;
                if (Mathf.Abs(LineAngleDiff(dt, dA)) < 20f) continue;               // phải cắt chéo mép thứ nhất
                if (Mathf.Abs(LineAngleDiff(m.Dir, dt)) > 1f) continue;
                if (!Facing(m, t, range, out float nd)) continue;
                Vector2 nt = new Vector2(-dt.y, dt.x);
                float k = Vector2.Dot(nt, dA);
                if (Mathf.Abs(k) < 0.2f) continue;
                float s = -nd / k;
                if (Mathf.Abs(s) < range && Mathf.Abs(s) < bestSlide) { bestSlide = Mathf.Abs(s); slide = s; }
            }
        if (bestSlide == float.MaxValue)
        {
            // không có mép thứ hai: thử khớp góc (đỉnh mảnh gần trùng một góc) theo chiều dọc mép
            foreach (Vector2 v in w)
                foreach (Vector2 c in corners)
                {
                    Vector2 diff = c - v;
                    float along = Vector2.Dot(diff, dA);
                    if (Mathf.Abs(Vector2.Dot(diff, nA)) > 0.02f * unit) continue;
                    if (Mathf.Abs(along) < range * 0.6f && Mathf.Abs(along) < bestSlide) { bestSlide = Mathf.Abs(along); slide = along; }
                }
        }
        if (bestSlide != float.MaxValue) newPos += dA * slide;

        // 4. tinh chỉnh: dịch nhẹ sao cho nhiều đỉnh của mảnh trùng đỉnh xung quanh nhất (khớp các mép cong)
        w = World(newPos, newRot);
        float eps = 0.03f * unit;
        int bestCount = 1; Vector2 bestT = Vector2.zero; bool found = false;
        foreach (Vector2 v in w)
            foreach (Vector2 c in corners)
            {
                Vector2 t = c - v;
                if (t.magnitude > range * 0.5f) continue;
                int cnt = 0;
                foreach (Vector2 v2 in w)
                {
                    Vector2 q = v2 + t;
                    foreach (Vector2 c2 in corners) if ((q - c2).sqrMagnitude < eps * eps) { cnt++; break; }
                }
                if (cnt > bestCount || (cnt == bestCount && found && t.sqrMagnitude < bestT.sqrMagnitude))
                { bestCount = cnt; bestT = t; found = true; }
            }
        if (found) newPos += bestT;

        pos = newPos; rot = newRot;
        return true;
    }
}
