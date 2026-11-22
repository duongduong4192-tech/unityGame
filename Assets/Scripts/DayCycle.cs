using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Ngày trong game tự quay vòng (mặc định 24 phút = 1 ngày), dùng chung cho menu và bàn chơi.
// Thời điểm được lưu lại, lần sau mở game chạy tiếp từ đó. Lần đầu bắt đầu vào buổi chiều (giống tranh phòng).
public static class DayCycle
{
    public static float DaySeconds = 24 * 60f;

    static bool inited;
    static float startT, startTime;

    public struct Sample
    {
        public Color ambient;     // màu ánh sáng chung trên bàn (Global Light 2D)
        public float intensity;   // độ sáng chung
        public Color room;        // màu nhuộm bức tranh căn phòng ở menu
        public Color beam;        // màu nắng rọi qua cửa sổ
        public float beamStrength;
        public float sun;         // 0..1: vị trí mặt trời (vệt nắng di chuyển theo)
    }

    // các mốc trong ngày (t = 0 lúc 6 giờ sáng)
    static readonly float[] T = { 0f, 0.12f, 0.28f, 0.45f, 0.58f, 0.68f, 0.8f, 0.92f, 1f };
    static readonly Sample[] K =
    {
        S(0.86f, 0.88f, 1.00f, 0.86f, 0.90f, 0.92f, 1.00f, 1.00f, 0.82f, 0.74f, 0.35f, 0.05f),   // bình minh
        S(0.97f, 0.97f, 1.00f, 0.98f, 0.98f, 0.98f, 1.00f, 1.00f, 0.95f, 0.88f, 0.45f, 0.20f),   // sáng
        S(1.00f, 1.00f, 0.98f, 1.06f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 0.95f, 0.55f, 0.45f),   // trưa
        S(1.00f, 0.94f, 0.84f, 1.00f, 1.00f, 0.96f, 0.90f, 1.00f, 0.82f, 0.52f, 0.75f, 0.62f),   // chiều
        S(1.00f, 0.80f, 0.66f, 0.88f, 0.96f, 0.80f, 0.70f, 1.00f, 0.62f, 0.38f, 0.65f, 0.80f),   // hoàng hôn
        S(0.72f, 0.64f, 0.86f, 0.66f, 0.72f, 0.64f, 0.82f, 0.90f, 0.50f, 0.55f, 0.15f, 0.92f),   // chạng vạng
        S(0.46f, 0.52f, 0.80f, 0.46f, 0.42f, 0.46f, 0.70f, 0.60f, 0.70f, 1.00f, 0.00f, 1.00f),   // đêm
        S(0.52f, 0.58f, 0.86f, 0.52f, 0.50f, 0.54f, 0.78f, 0.70f, 0.78f, 1.00f, 0.05f, 0.00f),   // khuya -> rạng sáng
        S(0.86f, 0.88f, 1.00f, 0.86f, 0.90f, 0.92f, 1.00f, 1.00f, 0.82f, 0.74f, 0.35f, 0.05f),   // = bình minh
    };

    static Sample S(float ar, float ag, float ab, float inten, float rr, float rg, float rb,
                    float br, float bg, float bb, float bs, float sun)
        => new Sample { ambient = new Color(ar, ag, ab), intensity = inten, room = new Color(rr, rg, rb),
                        beam = new Color(br, bg, bb), beamStrength = bs, sun = sun };

    static void Init()
    {
        if (inited) return;
        inited = true;
        startT = PlayerPrefs.GetFloat("day_t", 0.42f);
        startTime = Time.realtimeSinceStartup;
        Application.quitting += () => { PlayerPrefs.SetFloat("day_t", Now); PlayerPrefs.Save(); };
    }

    public static float Now
    {
        get { Init(); return Mathf.Repeat(startT + (Time.realtimeSinceStartup - startTime) / DaySeconds, 1f); }
    }

    public static Sample Current => Evaluate(Now);

    public static Sample Evaluate(float t)
    {
        int i = 0;
        while (i < T.Length - 2 && t > T[i + 1]) i++;
        float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(T[i], T[i + 1], t));
        Sample a = K[i], b = K[i + 1];
        return new Sample
        {
            ambient = Color.Lerp(a.ambient, b.ambient, k), intensity = Mathf.Lerp(a.intensity, b.intensity, k),
            room = Color.Lerp(a.room, b.room, k), beam = Color.Lerp(a.beam, b.beam, k),
            beamStrength = Mathf.Lerp(a.beamStrength, b.beamStrength, k),
            sun = Mathf.Lerp(a.sun, b.sun, k),
        };
    }

    // Ảnh vệt nắng mềm rọi chéo từ cửa sổ bên trái (tạo bằng code, không cần file ảnh)
    static Texture2D beamTex;
    public static Texture2D BeamTexture
    {
        get
        {
            if (beamTex != null) return beamTex;
            const int W = 256, H = 144;
            beamTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H;
                    float d1 = u - 0.55f * (1 - v) - 0.02f, d2 = u - 0.55f * (1 - v) - 0.34f;   // 2 vệt nắng chéo
                    float shafts = Mathf.Exp(-d1 * d1 * 40f) + 0.7f * Mathf.Exp(-d2 * d2 * 60f);
                    float side = Mathf.Clamp01(1.1f - u * 1.4f);                                   // sáng dần về phía cửa sổ
                    float a = Mathf.Clamp01(0.45f * shafts + 0.55f * side * side);
                    px[y * W + x] = new Color(1, 1, 1, a);
                }
            beamTex.SetPixels(px); beamTex.Apply();
            return beamTex;
        }
    }
}

// Bàn chơi: đổi màu/độ sáng ánh sáng chung theo giờ + vệt nắng chéo phủ lên cả bàn và mảnh gỗ.
public class TableLighting : MonoBehaviour
{
    Light2D global;
    SpriteRenderer beam;

    void Start()
    {
        foreach (Light2D l in FindObjectsByType<Light2D>())
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }

        Texture2D t = DayCycle.BeamTexture;
        GameObject go = new GameObject("SunBeam");
        go.transform.SetParent(transform, false);
        beam = go.AddComponent<SpriteRenderer>();
        beam.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width / 20f);
        beam.sortingOrder = 30000;                          // trên mọi mảnh gỗ (kể cả mảnh đang nhấc)
        Apply();
    }

    void Update() => Apply();

    void Apply()
    {
        DayCycle.Sample s = DayCycle.Current;
        if (global != null) { global.color = s.ambient; global.intensity = s.intensity; }
        if (beam != null)
        {
            Camera cam = Camera.main;
            float h = cam != null ? cam.orthographicSize * 2 : 10, w = h * (cam != null ? cam.aspect : 1.78f);
            beam.transform.localScale = new Vector3(w / 20f * 1.25f, h / 11.25f * 1.25f, 1);
            beam.transform.position = new Vector3(Mathf.Lerp(-0.25f, 0.25f, s.sun) * w, 0, 0);
            Color c = s.beam; c.a = s.beamStrength * 0.32f; beam.color = c;
        }
    }
}

// Menu: nhuộm màu bức tranh căn phòng theo giờ + lớp nắng ấm từ phía cửa sổ.
public class MenuDayTint : MonoBehaviour
{
    Image room;
    Image[] extra;
    Image beam;

    public void Init(Image roomImage, Image[] tintAlso)
    {
        room = roomImage; extra = tintAlso;
        Texture2D t = DayCycle.BeamTexture;
        beam = UIFactory.Img(transform, "SunBeam", Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)), Color.clear);
        beam.raycastTarget = false;
        UIFactory.Stretch(beam.rectTransform);
        beam.transform.SetSiblingIndex(1);               // ngay trên ảnh phòng, dưới các lớp khác
        Update();
    }

    void Update()
    {
        if (room == null) return;
        DayCycle.Sample s = DayCycle.Current;
        room.color = s.room;
        if (extra != null)
            foreach (Image i in extra)
                if (i != null) { Color c = i.color; c.r = s.room.r; c.g = s.room.g; c.b = s.room.b; i.color = c; }
        Color b = s.beam; b.a = s.beamStrength * 0.22f; beam.color = b;
    }
}
