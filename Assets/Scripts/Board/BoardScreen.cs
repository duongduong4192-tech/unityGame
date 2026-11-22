using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scene "Board": bảng ghim gỗ bần.
//   Trang bảng : các bức tranh ghim lên bảng (tranh chưa có thì "Sắp ra mắt"), bấm để mở.
//   Trang tranh: bức tranh lớn (xám mờ, lộ màu dần) + hàng khung gỗ nhỏ, mỗi khung = 1 loại sự vật.
//     - khung chưa xếp            : bấm -> vào màn xếp hình loại đó
//     - đã xếp ở tranh khác (sáng): bấm -> quẹt cọ luôn, không phải chơi lại
//     - đã quẹt ở tranh này       : có dấu tích
//   Đủ hết các khung -> cả tranh hiện màu, lưu vào Thành tựu.
public class BoardScreen : MonoBehaviour
{
    static readonly Color Ink = new Color32(96, 70, 54, 255);
    static readonly Color[] PinColors =
    {
        new Color32(232, 112, 96, 255), new Color32(120, 176, 214, 255), new Color32(246, 196, 92, 255),
        new Color32(150, 196, 130, 255), new Color32(196, 150, 220, 255),
    };

    // vị trí / góc nghiêng / chiều rộng thẻ tranh trên bảng
    static readonly Vector3[] CardLayout = { new Vector3(-360, 10, -2.5f), new Vector3(400, 170, 2f), new Vector3(420, -275, -1.5f) };
    static readonly float[] CardWidth = { 760, 520, 520 };

    RectTransform root, boardPage, picPage;
    Image fade;
    PictureView bigView;
    PictureData current;
    readonly List<Slot> slots = new List<Slot>();
    bool busy;

    class Slot
    {
        public CategoryData cat;
        public Image thumb, glow, check;
        public RectTransform rt;
    }

    static Sprite B(string n) => Resources.Load<Sprite>("UI/board/" + n);

    void Start()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogWarning("[ChillPuzzle] Scene Board chưa có Canvas."); return; }
        root = UIFactory.Stretch(UIFactory.Rect("BoardUI", canvas.transform));

        // nền gỗ bần phủ kín màn hình
        Image cork = UIFactory.Img(root, "Cork", B("board_cork"), Color.white);
        cork.raycastTarget = false;
        UIFactory.Stretch(cork.rectTransform);
        AspectRatioFitter f = cork.gameObject.AddComponent<AspectRatioFitter>();
        f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        f.aspectRatio = 1920f / 1080f;

        BuildBoardPage();

        fade = UIFactory.Solid(root, "Fade", Color.black, true);
        UIFactory.Stretch(fade.rectTransform);
        StartCoroutine(FadeTo(0f, 0.45f));

        // vừa xếp xong ở màn chơi -> mở lại tranh và quẹt cọ
        if (!string.IsNullOrEmpty(GameFlow.Picture))
        {
            PictureData d = PictureData.Load(GameFlow.Picture);
            if (d != null)
            {
                OpenPicture(d);
                string solved = GameFlow.JustSolved;
                GameFlow.JustSolved = null;
                if (!string.IsNullOrEmpty(solved) && Progress.IsCompleted(solved) && !d.IsRevealed(solved))
                    StartCoroutine(RevealCategory(solved, 0.7f));
            }
        }
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || busy || !k.escapeKey.wasPressedThisFrame) return;
        if (picPage != null) ClosePicture();
        else StartCoroutine(Leave(GameFlow.ToMenu));
    }

    // ===================== TRANG BẢNG =====================
    void BuildBoardPage()
    {
        boardPage = UIFactory.Stretch(UIFactory.Rect("BoardPage", root));
        HandUI.TapeTitle(boardPage, "board", new Vector2(-640, 430));
        Tag(boardPage, "menu", 230, 1.5f, () => StartCoroutine(Leave(GameFlow.ToMenu)));

        List<Vector2> pins = new List<Vector2>();
        for (int i = 0; i < Pictures.All.Length && i < CardLayout.Length; i++)
        {
            PictureData d = PictureData.Load(Pictures.All[i]);
            Vector3 L = CardLayout[i];
            pins.Add(Card(d, Pictures.All[i], new Vector2(L.x, L.y), CardWidth[i], L.z, i));
        }
        // dây chỉ đỏ nối các đinh ghim (vẽ dưới thẻ)
        RectTransform strings = UIFactory.Stretch(UIFactory.Rect("Strings", boardPage));
        strings.SetSiblingIndex(1);
        for (int i = 0; i + 1 < pins.Count; i++) Thread(strings, pins[i], pins[i + 1]);
    }

    // Thẻ polaroid cho 1 bức tranh. Trả về vị trí đinh ghim.
    Vector2 Card(PictureData d, string id, Vector2 pos, float width, float rot, int index)
    {
        const float SIDE = 58, BOTTOM = 145;
        float winW = width - 2 * SIDE, winH = winW / (d != null ? d.Aspect : 1.79f);
        float height = winH + SIDE + BOTTOM;

        RectTransform card = UIFactory.Place(UIFactory.Rect("Card_" + id, boardPage), pos, new Vector2(width, height));
        card.localRotation = Quaternion.Euler(0, 0, rot);
        Image paper = UIFactory.Img(card, "Paper", B("polaroid"), Color.white);
        paper.type = Image.Type.Sliced;
        UIFactory.Stretch(paper.rectTransform);

        RectTransform win = UIFactory.Rect("Window", card);
        win.anchorMin = Vector2.zero; win.anchorMax = Vector2.one;
        win.offsetMin = new Vector2(SIDE, BOTTOM); win.offsetMax = new Vector2(-SIDE, -SIDE);

        TextMeshProUGUI name = HandUI.Text(card, "Name", "pic_" + id, 44, Ink);
        RectTransform nr = name.rectTransform;
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 0); nr.pivot = new Vector2(0.5f, 0);
        nr.offsetMin = new Vector2(SIDE, 52); nr.offsetMax = new Vector2(-SIDE, 52 + 60);

        TextMeshProUGUI sub = HandUI.Text(card, "Progress", null, 30, HandUI.InkSoft);
        RectTransform sr = sub.rectTransform;
        sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 0); sr.pivot = new Vector2(0.5f, 0);
        sr.offsetMin = new Vector2(SIDE, 22); sr.offsetMax = new Vector2(-SIDE, 22 + 40);

        if (d != null)
        {
            PictureView pv = PictureView.Create(win, "Picture", d);
            UIFactory.Stretch((RectTransform)pv.transform);
            sub.text = d.IsComplete ? Loc.Get("pic_done") : $"{d.RevealedCount} / {d.categories.Length}";
            Button b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = paper;
            b.onClick.AddListener(() => { if (!busy) OpenPicture(d); });
            card.gameObject.AddComponent<ButtonBounce>().hover = 1.03f;
        }
        else
        {
            Image blank = UIFactory.Solid(win, "Blank", new Color32(214, 204, 190, 255));
            UIFactory.Stretch(blank.rectTransform);
            TextMeshProUGUI q = HandUI.Text(win, "Q", null, 140, new Color32(170, 156, 140, 255));
            UIFactory.Stretch(q.rectTransform);
            q.text = "?";
            sub.gameObject.AddComponent<LocText>().SetKey("coming_soon");
        }

        // đinh ghim ở mép trên
        Vector2 pinLocal = new Vector2(0, height / 2 - 26);
        Image pin = UIFactory.Img(card, "Pin", B("pin"), PinColors[index % PinColors.Length]);
        pin.raycastTarget = false;
        UIFactory.Place(pin.rectTransform, pinLocal, new Vector2(72, 72));
        return pos + (Vector2)(Quaternion.Euler(0, 0, rot) * pinLocal);
    }

    static void Thread(RectTransform parent, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        Image line = UIFactory.Solid(parent, "Thread", new Color32(190, 58, 52, 230));
        RectTransform r = line.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0, 0.5f);
        r.anchoredPosition = a;
        r.sizeDelta = new Vector2(d.magnitude, 4);
        r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // ===================== TRANG TRANH =====================
    void OpenPicture(PictureData d)
    {
        if (picPage != null) Destroy(picPage.gameObject);
        current = d;
        GameFlow.Picture = d.id;
        boardPage.gameObject.SetActive(false);
        slots.Clear();

        picPage = UIFactory.Stretch(UIFactory.Rect("PicturePage", root));
        fade?.transform.SetAsLastSibling();

        // tranh lớn trên tờ giấy trắng
        const float PW = 1340, BORDER = 24, CAPTION = 64;
        float ph = PW / d.Aspect;
        Vector2 size = new Vector2(PW + 2 * BORDER, ph + BORDER + CAPTION);
        RectTransform card = UIFactory.Place(UIFactory.Rect("Card", picPage), new Vector2(0, 520 - size.y / 2), size);
        Image paper = UIFactory.Img(card, "Paper", B("polaroid"), Color.white);
        paper.type = Image.Type.Sliced;
        paper.pixelsPerUnitMultiplier = 2.3f;
        paper.raycastTarget = false;
        UIFactory.Stretch(paper.rectTransform);

        RectTransform win = UIFactory.Rect("Window", card);
        win.anchorMin = Vector2.zero; win.anchorMax = Vector2.one;
        win.offsetMin = new Vector2(BORDER, CAPTION); win.offsetMax = new Vector2(-BORDER, -BORDER);
        bigView = PictureView.Create(win, "Picture", d);
        UIFactory.Stretch((RectTransform)bigView.transform);

        TextMeshProUGUI name = HandUI.Text(card, "Name", d.NameKey, 42, Ink);
        RectTransform nr = name.rectTransform;
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 0); nr.pivot = new Vector2(0.5f, 0);
        nr.offsetMin = new Vector2(BORDER, 6); nr.offsetMax = new Vector2(-BORDER, CAPTION);

        foreach (float x in new[] { -size.x / 2 + 60, size.x / 2 - 60 })
        {
            Image pin = UIFactory.Img(card, "Pin", B("pin"), PinColors[x < 0 ? 0 : 1]);
            pin.raycastTarget = false;
            UIFactory.Place(pin.rectTransform, new Vector2(x, size.y / 2 - 18), new Vector2(64, 64));
        }

        Tag(picPage, "board", 250, -1.5f, ClosePicture, left: true);

        // hàng khung gỗ: mỗi khung là 1 loại sự vật
        int n = d.categories.Length;
        float slotW = Mathf.Min(196, 1800f / n), frameW = slotW - 34, frameH = frameW * 0.72f;
        float y = 520 - size.y - 24 - frameH / 2;
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) * slotW;
            slots.Add(BuildSlot(d, d.categories[i], new Vector2(x, y), new Vector2(frameW, frameH), i));
        }
        RefreshSlots();
    }

    Slot BuildSlot(PictureData d, CategoryData c, Vector2 pos, Vector2 size, int i)
    {
        Slot s = new Slot { cat = c };
        s.rt = UIFactory.Place(UIFactory.Rect("Slot_" + c.id, picPage), pos, size);
        s.rt.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 1.2f : -1.2f));

        s.glow = UIFactory.Img(s.rt, "Glow", B("glow"), new Color32(255, 214, 120, 255));
        s.glow.type = Image.Type.Sliced;
        s.glow.raycastTarget = false;
        UIFactory.Stretch(s.glow.rectTransform);
        s.glow.rectTransform.offsetMin = new Vector2(-40, -40);
        s.glow.rectTransform.offsetMax = new Vector2(40, 40);

        Image back = UIFactory.Solid(s.rt, "Back", new Color32(60, 40, 26, 255));
        UIFactory.Stretch(back.rectTransform);
        float inset = 11;
        s.thumb = UIFactory.Img(s.rt, "Thumb", null, Color.white);
        s.thumb.raycastTarget = false;
        UIFactory.Stretch(s.thumb.rectTransform);
        s.thumb.rectTransform.offsetMin = new Vector2(inset, inset);
        s.thumb.rectTransform.offsetMax = new Vector2(-inset, -inset);

        Image frame = UIFactory.Img(s.rt, "Frame", B("frame_small"), Color.white);
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 2.2f;
        UIFactory.Stretch(frame.rectTransform);
        frame.raycastTarget = true;
        Button b = frame.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => OnSlot(s));
        s.rt.gameObject.AddComponent<ButtonBounce>().hover = 1.07f;

        PaperShadow.Add(frame, 0.9f, s.rt);         // bóng cho cả ô (nằm dưới quầng sáng)
        s.check = UIFactory.Img(s.rt, "Check", B("check"), Color.white);
        s.check.raycastTarget = false;
        UIFactory.Place(s.check.rectTransform, new Vector2(size.x / 2 - 8, size.y / 2 - 8), new Vector2(46, 46));

        // nhãn tên loại trên mẩu giấy
        RectTransform lab = UIFactory.Place(UIFactory.Rect("Label", s.rt), new Vector2(0, -size.y / 2 - 26), new Vector2(size.x + 24, 50));
        Image lp = UIFactory.Img(lab, "Paper", HandUI.Sp("hand_label"), Color.white);
        lp.type = Image.Type.Simple;
        lp.raycastTarget = false;
        UIFactory.Stretch(lp.rectTransform);
        PaperShadow.Add(lp, 0.5f);
        TextMeshProUGUI t = HandUI.Text(lab, "Text", "cat_" + c.id, 26, Ink);
        t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = 26;
        UIFactory.Stretch(t.rectTransform).offsetMax = new Vector2(-6, -8);
        t.rectTransform.offsetMin = new Vector2(6, 0);
        return s;
    }

    void RefreshSlots()
    {
        foreach (Slot s in slots)
        {
            bool solved = Progress.IsCompleted(s.cat.id);
            bool revealed = current.IsRevealed(s.cat.id);
            Vector2 sz = s.thumb.rectTransform.rect.size;
            float aspect = sz.y > 1 ? sz.x / sz.y : 1.4f;
            s.thumb.sprite = PictureView.Thumb(current, s.cat, solved, aspect);
            s.check.gameObject.SetActive(revealed);
            s.glow.gameObject.SetActive(solved && !revealed);
        }
    }

    void LateUpdate()
    {
        // khung đã xếp ở tranh khác: nhấp nháy sáng nhẹ
        float a = 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
        foreach (Slot s in slots)
            if (s.glow != null && s.glow.gameObject.activeSelf) { Color c = s.glow.color; c.a = a; s.glow.color = c; }
    }

    void OnSlot(Slot s)
    {
        if (busy) return;
        string cat = s.cat.id;
        if (current.IsRevealed(cat)) return;
        if (Progress.IsCompleted(cat)) StartCoroutine(RevealCategory(cat, 0f));
        else StartCoroutine(Leave(() => GameFlow.PlayCategory(current.id, cat)));
    }

    IEnumerator RevealCategory(string cat, float delay)
    {
        busy = true;
        if (delay > 0) yield return new WaitForSecondsRealtime(delay);
        current.MarkRevealed(cat);
        RefreshSlots();
        yield return bigView.Reveal(cat);

        if (current.AllRevealed && !current.IsComplete)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            yield return bigView.RevealAll();
            current.MarkComplete();
            Achievements.Unlock(current.id);
            Image tape = HandUI.Img(picPage, "DoneTape", "hand_tape", new Vector2(0, 470), new Vector2(680, 120), 3f);
            TextMeshProUGUI t = HandUI.Text(tape.transform, "Text", "pic_done", 60, new Color32(110, 56, 40, 255));
            UIFactory.Stretch(t.rectTransform);
        }
        busy = false;
    }

    void ClosePicture()
    {
        if (busy || picPage == null) return;
        Destroy(picPage.gameObject);
        picPage = null; bigView = null; slots.Clear();
        GameFlow.Picture = null;
        Destroy(boardPage.gameObject);       // dựng lại để ảnh nhỏ cập nhật màu
        BuildBoardPage();
        fade.transform.SetAsLastSibling();
    }

    // ===================== CHUNG =====================
    void Tag(RectTransform parent, string locKey, float width, float rot, UnityEngine.Events.UnityAction onClick, bool left = false)
    {
        RectTransform r = UIFactory.Rect("Tag_" + locKey, parent);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(left ? 0 : 1, 1);
        r.anchoredPosition = new Vector2(left ? 30 : -30, -24);
        r.sizeDelta = new Vector2(width, 84);
        r.localRotation = Quaternion.Euler(0, 0, rot);
        Image paper = r.gameObject.AddComponent<Image>();
        paper.sprite = HandUI.Sp("hand_label");
        PaperShadow.Add(paper, 1f);
        Button b = r.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        r.gameObject.AddComponent<ButtonBounce>();
        TextMeshProUGUI t = HandUI.Text(r, "Text", locKey, 34, Ink);
        UIFactory.Stretch(t.rectTransform).offsetMax = new Vector2(0, -16);
    }

    IEnumerator Leave(System.Action go)
    {
        busy = true;
        yield return FadeTo(1f, 0.4f);
        go();
    }

    IEnumerator FadeTo(float target, float dur)
    {
        fade.raycastTarget = true;
        float from = fade.color.a, t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / dur);
            fade.color = new Color(0, 0, 0, Mathf.Lerp(from, target, t));
            yield return null;
        }
        fade.raycastTarget = target > 0.5f;
    }
}
