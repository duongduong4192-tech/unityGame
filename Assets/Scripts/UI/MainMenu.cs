using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu chính = căn phòng. 3 đồ vật bấm được:
//   Bộ xếp hình trên bàn  -> Chơi (game luôn tự lưu, không có Tiếp tục / New game)
//   Quyển sách trên bàn   -> Cài đặt
//   Giá tranh             -> Thành tựu
// Toàn bộ giao diện dựng bằng code khi game chạy.
public class MainMenu : MonoBehaviour
{
    public string playScene = "PuzzleScene";
    public string publisherName = "Tên studio của bạn";

    [System.Serializable] class HS { public string name; public int x0, y0, x1, y1; public float labelX, labelY; }
    [System.Serializable] class HSMeta { public int imageWidth, imageHeight; public HS[] hotspots; }

    Canvas canvas;
    RectTransform root, room;
    Image dim, fade;
    CanvasGroup titleGroup;
    readonly List<Hotspot> hotspots = new List<Hotspot>();
    SettingsPanel settings;
    AchievementsPanel achievements;
    LevelSelectPanel levels;
    bool loading;

    void Start()
    {
        EnsureCanvas();
        BuildRoom();
        BuildTitle();
        settings = SettingsPanel.Create(root, publisherName, OnPanelClosed);
        achievements = AchievementsPanel.Create(root, OnPanelClosed);
        levels = LevelSelectPanel.Create(root, OnPanelClosed, StartLevel);

        fade = UIFactory.Solid(root, "Fade", Color.black, true);
        UIFactory.Stretch(fade.rectTransform);
        StartCoroutine(Intro());

        // vừa chơi xong một màn -> mở lại bảng chọn màn đúng thể loại
        if (!string.IsNullOrEmpty(GameFlow.ReopenCat))
        {
            SetHotspots(false);
            levels.OpenAt(GameFlow.ReopenCat);
            GameFlow.ReopenCat = null;
        }
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || loading || !kb.escapeKey.wasPressedThisFrame) return;
        if (settings.IsOpen) settings.Close();
        else if (achievements.IsOpen) achievements.Close();
        else if (levels.IsOpen) levels.Close();
        else OpenPanel(settings);
    }

    // ===================== DỰNG CĂN PHÒNG =====================
    void EnsureCanvas()
    {
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
        }
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        root = UIFactory.Stretch(UIFactory.Rect("Menu", canvas.transform));
    }

    void BuildRoom()
    {
        TextAsset json = Resources.Load<TextAsset>("UI/menu/hotspots");
        HSMeta meta = json != null ? JsonUtility.FromJson<HSMeta>(json.text) : null;
        float W = meta != null ? meta.imageWidth : 2752, H = meta != null ? meta.imageHeight : 1536;

        // ảnh phòng phủ kín màn hình, giữ đúng tỉ lệ (cắt bớt mép nếu màn hình khác tỉ lệ)
        room = UIFactory.Rect("Room", root);
        room.anchorMin = room.anchorMax = room.pivot = new Vector2(0.5f, 0.5f);
        Image bg = room.gameObject.AddComponent<Image>();
        bg.sprite = Resources.Load<Sprite>("UI/menu/room_bg");
        bg.raycastTarget = false;
        AspectRatioFitter arf = room.gameObject.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        arf.aspectRatio = W / H;

        dim = UIFactory.Solid(room, "Dim", new Color(0.04f, 0.02f, 0.01f, 0f));
        UIFactory.Stretch(dim.rectTransform);

        if (meta == null)
        {
            Debug.LogWarning("[ChillPuzzle] Thiếu Resources/UI/menu/hotspots.json");
            return;
        }

        var rims = new List<Image>(); var lits = new List<Image>(); var labels = new List<CanvasGroup>();
        for (int i = 0; i < meta.hotspots.Length; i++)
        {
            HS h = meta.hotspots[i];
            Vector2 min = new Vector2(h.x0 / W, 1f - h.y1 / H), max = new Vector2(h.x1 / W, 1f - h.y0 / H);
            Image rim = UIFactory.Img(room, "Rim_" + h.name, Resources.Load<Sprite>($"UI/menu/hs_{h.name}_rim"), new Color(1, 1, 1, 0));
            UIFactory.Anchor(rim.rectTransform, min, max);
            rim.raycastTarget = false;
            rims.Add(rim);

            Image lit = UIFactory.Img(room, "Lit_" + h.name, Resources.Load<Sprite>($"UI/menu/hs_{h.name}_lit"), new Color(1, 1, 1, 0));
            UIFactory.Anchor(lit.rectTransform, min, max);
            try { lit.alphaHitTestMinimumThreshold = 0.08f; }   // chỉ bấm được đúng chỗ đồ vật
            catch (System.Exception) { Debug.LogWarning("[ChillPuzzle] Ảnh vùng bấm chưa bật Read/Write. Chạy lại ChillPuzzle → Setup Main Menu."); }
            lits.Add(lit);

            // nhãn tên nổi phía trên đồ vật
            RectTransform lr = UIFactory.Rect("Label_" + h.name, room);
            Vector2 p = new Vector2(h.labelX / W, 1f - h.labelY / H);
            lr.anchorMin = lr.anchorMax = p;
            lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = Vector2.zero;
            lr.sizeDelta = new Vector2(230, 84);
            lr.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 2.5f : -2f));
            CanvasGroup lg = lr.gameObject.AddComponent<CanvasGroup>();
            lg.blocksRaycasts = false;
            Image paper = UIFactory.Img(lr, "Paper", HandUI.Sp("hand_label"), Color.white);
            paper.type = Image.Type.Simple;
            UIFactory.Stretch(paper.rectTransform);
            paper.raycastTarget = false;
            PaperShadow.Add(paper, 1f);
            TextMeshProUGUI t = HandUI.Text(lr, "Text", "hs_" + h.name, 34, HandUI.Ink);
            UIFactory.Stretch(t.rectTransform).offsetMax = new Vector2(0, -16);
            labels.Add(lg);
        }
        // thứ tự vẽ: viền -> đồ vật sáng -> nhãn
        foreach (Image r in rims) r.transform.SetAsLastSibling();
        foreach (Image l in lits) l.transform.SetAsLastSibling();
        foreach (CanvasGroup g in labels) g.transform.SetAsLastSibling();

        for (int i = 0; i < meta.hotspots.Length; i++)
        {
            HS h = meta.hotspots[i];
            Hotspot hs = lits[i].gameObject.AddComponent<Hotspot>();
            hs.lit = lits[i]; hs.rim = rims[i]; hs.label = labels[i];
            hs.introDelay = 0.6f + 0.3f * i;
            switch (h.name)
            {
                case "puzzle":  hs.onClick = Play; break;
                case "book":    hs.onClick = () => OpenPanel(settings); break;
                case "gallery": hs.onClick = () => OpenPanel(achievements); break;
            }
            hotspots.Add(hs);
        }    }

    [System.Serializable] class TitleRect { public int x0, y0, x1, y1; }
    [System.Serializable] class SubRects { public TitleRect vi, en; }
    Image subImage;
    SubRects subRects;

    void ApplySubtitle()
    {
        if (subImage == null || subRects == null) return;
        bool vi = Loc.Current == Loc.Lang.VI;
        TitleRect r = vi ? subRects.vi : subRects.en;
        subImage.sprite = Resources.Load<Sprite>(vi ? "UI/menu/subtitle_vi" : "UI/menu/subtitle_en");
        subImage.enabled = subImage.sprite != null;
        float W = 2752, H = 1536;
        UIFactory.Anchor(subImage.rectTransform, new Vector2(r.x0 / W, 1f - r.y1 / H), new Vector2(r.x1 / W, 1f - r.y0 / H));
    }

    void OnDestroy() => Loc.Changed -= ApplySubtitle;

    void BuildTitle()
    {
        // Ưu tiên: ảnh tên game vẽ sơn trên tường (Resources/UI/menu/title.png + title_rect.json)
        Sprite painted = Resources.Load<Sprite>("UI/menu/title");
        TextAsset rectJson = Resources.Load<TextAsset>("UI/menu/title_rect");
        if (painted != null && rectJson != null && room != null)
        {
            TitleRect r = JsonUtility.FromJson<TitleRect>(rectJson.text);
            float W = 2752, H = 1536;
            RectTransform g = UIFactory.Rect("PaintedTitle", room);
            UIFactory.Anchor(g, new Vector2(r.x0 / W, 1f - r.y1 / H), new Vector2(r.x1 / W, 1f - r.y0 / H));
            titleGroup = g.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0;
            titleGroup.blocksRaycasts = false;
            Image img = g.gameObject.AddComponent<Image>();
            img.sprite = painted;
            img.raycastTarget = false;
            g.SetSiblingIndex(1);   // ngay trên lớp phủ tối, dưới các đồ vật sáng

            // dòng phụ: ảnh vẽ sẵn theo cùng phối cảnh tường, có bản tiếng Việt + tiếng Anh
            RectTransform sr = UIFactory.Rect("Sub", room);
            sr.SetSiblingIndex(2);
            subGroup = sr.gameObject.AddComponent<CanvasGroup>();
            subGroup.blocksRaycasts = false;
            subGroup.alpha = 0;
            subImage = sr.gameObject.AddComponent<Image>();
            subImage.raycastTarget = false;
            TextAsset subJson = Resources.Load<TextAsset>("UI/menu/subtitle_rect");
            if (subJson != null) subRects = JsonUtility.FromJson<SubRects>(subJson.text);
            ApplySubtitle();
            Loc.Changed += ApplySubtitle;
            BuildVersion();
            return;
        }

        RectTransform tr = UIFactory.Rect("Title", root);
        UIFactory.Place(tr, new Vector2(0, 360), new Vector2(1400, 260));
        titleGroup = tr.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0;
        titleGroup.blocksRaycasts = false;

        TextMeshProUGUI title = UIFactory.Text(tr, "Name", "title", 150, new Color32(255, 240, 214, 255));
        UIFactory.Place(title.rectTransform, new Vector2(0, 30), new Vector2(1400, 190));
        title.outlineWidth = 0.15f;
        title.outlineColor = new Color32(70, 40, 18, 255);

        TextMeshProUGUI sub = UIFactory.Text(tr, "Sub", "subtitle", 44, new Color32(255, 214, 160, 255));
        UIFactory.Place(sub.rectTransform, new Vector2(0, -80), new Vector2(1000, 64));

        BuildVersion();
    }

    CanvasGroup subGroup;

    void BuildVersion()
    {
        TextMeshProUGUI ver = UIFactory.Text(root, "Version", null, 26, new Color(1f, 0.9f, 0.75f, 0.55f), false,
                                             TextAlignmentOptions.BottomRight);
        RectTransform vr = ver.rectTransform;
        vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(1, 0);
        vr.anchoredPosition = new Vector2(-24, 14);
        vr.sizeDelta = new Vector2(400, 40);
        ver.text = "v" + Application.version;
    }

    IEnumerator Intro()
    {
        // từ đen hiện dần căn phòng, rồi phòng tối lại một chút để 3 đồ vật nổi bật
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime / 0.8f;
            fade.color = new Color(0, 0, 0, 1f - t);
            yield return null;
        }
        fade.raycastTarget = false;
        t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime / 1.2f;
            dim.color = new Color(0.04f, 0.02f, 0.01f, Mathf.SmoothStep(0, 0.32f, t));
            titleGroup.alpha = Mathf.SmoothStep(0, 1, t);
            if (subGroup != null) subGroup.alpha = Mathf.SmoothStep(0, 1, t);
            yield return null;
        }
    }

    // ===================== HÀNH ĐỘNG =====================
    // Bấm bộ xếp hình trên bàn: lần đầu vào thẳng màn Số (hướng dẫn), sau đó mở bảng chọn màn
    void Play()
    {
        if (loading) return;
        if (!Progress.IsCompleted("numbers")) StartLevel(GameFlow.PlayTutorial);
        else OpenPanel(levels);
    }

    // tối dần màn hình rồi vào màn chơi
    public void StartLevel(System.Action load)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(GameFlow.PUZZLE_SCENE))
        {
            Debug.LogWarning("[ChillPuzzle] Scene PuzzleScene chưa có trong danh sách build. Chạy ChillPuzzle → Setup Main Menu.");
            return;
        }
        StartCoroutine(FadeAndLoad(load));
    }

    IEnumerator FadeAndLoad(System.Action load)
    {
        loading = true;
        SetHotspots(false);
        fade.transform.SetAsLastSibling();
        fade.raycastTarget = true;
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime / 0.5f;
            fade.color = new Color(0, 0, 0, t);
            yield return null;
        }
        load();
    }

    void OpenPanel(IMenuPanel p)
    {
        if (loading) return;
        SetHotspots(false);
        Sfx.Play("open");
        p.Open();
    }

    void OnPanelClosed() { Sfx.Play("close"); SetHotspots(true); }

    void SetHotspots(bool on)
    {
        foreach (Hotspot h in hotspots) h.interactable = on;
    }
}

public interface IMenuPanel
{
    bool IsOpen { get; }
    void Open();
    void Close();
}
