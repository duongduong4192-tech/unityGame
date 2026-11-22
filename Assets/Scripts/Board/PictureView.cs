using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Vẽ một bức tranh: nền xám mờ + các nét cọ lộ màu (mỗi nét là 1 ảnh, quét từ trái sang phải) + lớp màu đầy đủ.
// Dùng cho cả ảnh nhỏ trên bảng ghim và ảnh lớn khi bấm vào tranh.
public class PictureView : MonoBehaviour
{
    public PictureData data;
    Image color;
    readonly Dictionary<string, List<Image>> strokes = new Dictionary<string, List<Image>>();

    public static PictureView Create(Transform parent, string name, PictureData d)
    {
        RectTransform rt = UIFactory.Rect(name, parent);
        PictureView pv = rt.gameObject.AddComponent<PictureView>();
        pv.Build(d);
        return pv;
    }

    void Build(PictureData d)
    {
        data = d;
        RectTransform rt = (RectTransform)transform;
        Image gray = UIFactory.Img(rt, "Gray", d.Gray, Color.white);
        UIFactory.Stretch(gray.rectTransform);
        gray.raycastTarget = false;

        RectTransform layer = UIFactory.Stretch(UIFactory.Rect("Strokes", rt));
        foreach (CategoryData c in d.categories)
        {
            List<Image> list = new List<Image>();
            List<StrokeData> ordered = new List<StrokeData>(c.strokes);
            ordered.Sort((a, b) => a.x0.CompareTo(b.x0));          // quẹt dần từ trái sang phải
            foreach (StrokeData s in ordered)
            {
                Image img = UIFactory.Img(layer, s.file, d.Stroke(s.file), Color.white);
                img.raycastTarget = false;
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                RectTransform r = img.rectTransform;
                r.anchorMin = new Vector2((float)s.x0 / d.w, 1f - (float)s.y1 / d.h);
                r.anchorMax = new Vector2((float)s.x1 / d.w, 1f - (float)s.y0 / d.h);
                r.offsetMin = r.offsetMax = Vector2.zero;
                list.Add(img);
            }
            strokes[c.id] = list;
        }

        color = UIFactory.Img(rt, "Color", d.Color, new Color(1, 1, 1, 0));
        UIFactory.Stretch(color.rectTransform);
        color.raycastTarget = false;
        Refresh();
    }

    // Hiện đúng trạng thái đã lưu (không hiệu ứng)
    public void Refresh()
    {
        foreach (var kv in strokes)
        {
            float f = data.IsRevealed(kv.Key) || data.IsComplete ? 1f : 0f;
            foreach (Image img in kv.Value) img.fillAmount = f;
        }
        Color c = color.color; c.a = data.IsComplete ? 1f : 0f; color.color = c;
    }

    // Quẹt cọ lộ màu cho 1 loại sự vật
    public IEnumerator Reveal(string cat)
    {
        if (!strokes.TryGetValue(cat, out List<Image> list)) yield break;
        float dur = 0.5f, gap = list.Count > 8 ? 0.08f : 0.16f;
        for (int i = 0; i < list.Count; i++) StartCoroutine(Sweep(list[i], dur, i * gap));
        yield return new WaitForSecondsRealtime(dur + gap * list.Count + 0.1f);
    }

    IEnumerator Sweep(Image img, float dur, float delay)
    {
        if (img.fillAmount >= 1f) yield break;
        img.fillAmount = 0;
        yield return new WaitForSecondsRealtime(delay);
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / dur);
            img.fillAmount = 1 - Mathf.Pow(1 - t, 2.2f);     // nhanh lúc đầu, chậm dần như nhấc cọ
            yield return null;
        }
    }

    // Hoàn thành: cả bức tranh hiện màu đầy đủ
    public IEnumerator RevealAll(float dur = 1.8f)
    {
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / dur);
            Color c = color.color; c.a = Mathf.SmoothStep(0, 1, t); color.color = c;
            yield return null;
        }
    }

    // Ảnh nhỏ của 1 loại sự vật (cắt từ tranh) để đặt trong khung gỗ, đúng tỉ lệ khung
    public static Sprite Thumb(PictureData d, CategoryData c, bool colored, float aspect)
    {
        Sprite src = colored ? d.Color : d.Gray;
        if (src == null || c.thumb == null || c.thumb.Length < 4) return src;
        Texture2D tex = src.texture;
        float sx = (float)tex.width / d.w, sy = (float)tex.height / d.h;
        float x0 = c.thumb[0], y0 = c.thumb[1], x1 = c.thumb[2], y1 = c.thumb[3];
        float cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, bw = x1 - x0, bh = y1 - y0;
        if (bw / bh > aspect) bh = bw / aspect; else bw = bh * aspect;    // nới khung cho đúng tỉ lệ
        bw = Mathf.Min(bw, d.w); bh = Mathf.Min(bh, d.h);
        x0 = Mathf.Clamp(cx - bw / 2, 0, d.w - bw); y0 = Mathf.Clamp(cy - bh / 2, 0, d.h - bh);
        Rect r = new Rect(x0 * sx, (d.h - y0 - bh) * sy, bw * sx, bh * sy);
        return Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f);
    }
}
