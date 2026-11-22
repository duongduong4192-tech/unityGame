using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Bảng chọn màn kiểu "trang sổ tay" (mở khi bấm bộ xếp hình trên bàn), giống Cài đặt / Thành tựu:
//   bên phải: các tab thể loại (Hình học, Ghép khối, Đường cong, Hình tròn)
//   giữa    : lưới thẻ màn — ảnh khung, số màn, số mảnh, chấm độ khó, dấu tích nếu đã xong
// Chỉ khi bấm vào một màn mới chuyển sang bàn chơi.
public class LevelSelectPanel : MonoBehaviour, IMenuPanel
{
    const float PX = -110, PY = 20, PW = 1200, PH = 700;
    static readonly string[] Cats = { "geo", "blocks", "curve", "circle" };

    CanvasGroup group;
    RectTransform grid;
    readonly List<RectTransform> tabs = new List<RectTransform>();
    readonly List<TextMeshProUGUI> tabTexts = new List<TextMeshProUGUI>();
    TextMeshProUGUI title;
    Action onClose;
    Action<Action> onPlay;
    int tab;
    readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();

    public bool IsOpen { get; private set; }

    public static LevelSelectPanel Create(Transform parent, Action onClose, Action<Action> onPlay)
    {
        RectTransform rt = UIFactory.Stretch(UIFactory.Rect("LevelSelectPanel", parent));
        LevelSelectPanel p = rt.gameObject.AddComponent<LevelSelectPanel>();
        p.onClose = onClose; p.onPlay = onPlay;
        p.Build();
        return p;
    }

    void Build()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        UIFactory.Stretch(UIFactory.Solid(transform, "Dim", HandUI.Dim, true).rectTransform);

        // tab thể loại (nằm dưới tờ giấy, thò ra mép phải)
        for (int i = 0; i < Cats.Length; i++)
        {
            int idx = i;
            Image tb = HandUI.Img(transform, "Tab_" + Cats[i], "hand_tab_" + HandUI.TabColors[i % HandUI.TabColors.Length],
                                  new Vector2(605, 234 - i * 125), new Vector2(300, 112));
            tb.raycastTarget = true;
            Button b = tb.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play("click"); SetTab(idx); });
            TextMeshProUGUI tt = HandUI.Text(tb.transform, "Text", "cat_" + Cats[i], 40, HandUI.Ink);
            tt.enableAutoSizing = true; tt.fontSizeMin = 26; tt.fontSizeMax = 40;
            UIFactory.Stretch(tt.rectTransform).offsetMin = new Vector2(30, 0);
            tabs.Add(tb.rectTransform); tabTexts.Add(tt);
        }

        HandUI.Img(transform, "Paper", "hand_paper", new Vector2(PX, PY), new Vector2(PW, PH), -0.6f);
        HandUI.TapeTitle(transform, "choose_level", new Vector2(-540, 367));
        title = HandUI.Text(transform, "Cat", null, 56, HandUI.Accent, TextAlignmentOptions.MidlineRight);
        UIFactory.Place(title.rectTransform, new Vector2(300, 300), new Vector2(420, 70));

        grid = UIFactory.Stretch(UIFactory.Rect("Cards", transform));

        HandUI.StampHint(transform, "Esc", "back", new Vector2(-110, -415), Close);

        Loc.Changed += Rebuild;
        group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    void OnDestroy() => Loc.Changed -= Rebuild;

    void Update()
    {
        if (!IsOpen) return;
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.qKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)
            SetTab((tab + Cats.Length - 1) % Cats.Length);
        if (k.eKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)
            SetTab((tab + 1) % Cats.Length);
    }

    void SetTab(int i)
    {
        tab = i;
        for (int k = 0; k < tabs.Count; k++)
        {
            bool on = k == i;
            tabs[k].anchoredPosition = new Vector2(on ? 630 : 605, 234 - k * 125);
            tabTexts[k].fontSizeMax = on ? 44 : 40;
        }
        Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform c in grid) Destroy(c.gameObject);
        string cat = Cats[tab];
        title.text = Loc.Get("cat_" + cat);
        CatalogCategory cc = LevelCatalog.Get().Find(cat);
        int slot = 0;
        if (cat == "blocks") Card(slot++, null, cat);            // màn Số (hướng dẫn) đứng đầu Ghép khối
        if (cc != null) foreach (CatalogLevel l in cc.levels) Card(slot++, l, cat);
    }

    void Card(int i, CatalogLevel l, string cat)
    {
        const int cols = 5;
        int cx = i % cols, cy = i / cols;
        Vector2 pos = new Vector2(PX + (cx - 2) * 212, 160 - cy * 186);
        float tilt = ((i * 37) % 7 - 3) * 0.6f;
        bool done = l == null ? Progress.IsCompleted("numbers") : Progress.IsCompleted(l.id);

        Image card = HandUI.Img(grid, "Level_" + (l != null ? l.id : "numbers"), "hand_card", pos, new Vector2(196, 172), tilt);
        card.raycastTarget = true;
        Button b = card.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        if (l == null) b.onClick.AddListener(() => Play(GameFlow.PlayTutorial));
        else { string id = l.id; b.onClick.AddListener(() => Play(() => GameFlow.PlayLevel(cat, id))); }
        card.gameObject.AddComponent<ButtonBounce>().hover = 1.06f;
        Transform c = card.transform;

        // ảnh khung (hoặc chữ "0-9" cho màn Số)
        if (l != null)
        {
            Texture2D tex = Thumbs(cat);
            if (tex != null)
            {
                int[] r = l.thumb;
                Sprite sp = Sprite.Create(tex, new Rect(r[0], tex.height - r[1] - r[3], r[2], r[3]), new Vector2(0.5f, 0.5f), 100f);
                Image th = UIFactory.Img(c, "Thumb", sp, Color.white);
                th.preserveAspect = true; th.raycastTarget = false;
                UIFactory.Place(th.rectTransform, new Vector2(0, 14), new Vector2(150, 104));
            }
        }
        else
        {
            TextMeshProUGUI z = HandUI.Text(c, "Digits", null, 64, HandUI.Accent);
            UIFactory.Place(z.rectTransform, new Vector2(0, 16), new Vector2(170, 100));
            z.text = "0–9";
        }

        TextMeshProUGUI num = HandUI.Text(c, "Num", null, 28, HandUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(num.rectTransform, new Vector2(-30, 70), new Vector2(120, 34));
        num.text = l == null ? Loc.Get("tutorial_card") : l.id.Substring(l.id.Length - 2);

        TextMeshProUGUI info = HandUI.Text(c, "Info", null, 26, HandUI.Ink);
        UIFactory.Place(info.rectTransform, new Vector2(0, -56), new Vector2(180, 34));
        int tier = l == null ? 1 : l.tier;
        string dots = new string('•', tier) + "<color=#C8B8A8>" + new string('•', 5 - tier) + "</color>";
        info.text = (l == null ? "10" : l.n.ToString()) + " " + Loc.Get("pieces_word") + "  " + dots;

        if (done)
        {
            Image chk = UIFactory.Img(c, "Done", Resources.Load<Sprite>("UI/board/check"), Color.white);
            chk.raycastTarget = false;
            UIFactory.Place(chk.rectTransform, new Vector2(78, 66), new Vector2(46, 46));
        }
    }

    Texture2D Thumbs(string cat)
    {
        if (!thumbs.TryGetValue(cat, out Texture2D t)) thumbs[cat] = t = LevelCatalog.Thumbs(cat);
        return t;
    }

    void Play(Action load)
    {
        if (!IsOpen) return;
        onPlay?.Invoke(load);
    }

    public void OpenAt(string cat)
    {
        int i = Array.IndexOf(Cats, cat);
        tab = i < 0 ? 0 : i;
        Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetTab(tab);
        StopAllCoroutines();
        StartCoroutine(Anim(1f));
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        StopAllCoroutines();
        StartCoroutine(Anim(0f));
        onClose?.Invoke();
    }

    IEnumerator Anim(float target)
    {
        group.blocksRaycasts = target > 0.5f;
        group.interactable = false;
        float start = group.alpha, t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime / 0.22f;
            float k = Mathf.SmoothStep(0, 1, t);
            group.alpha = Mathf.Lerp(start, target, k);
            ((RectTransform)transform).anchoredPosition = new Vector2(0, (1f - group.alpha) * -30f);
            yield return null;
        }
        group.alpha = target;
        ((RectTransform)transform).anchoredPosition = Vector2.zero;
        group.interactable = target > 0.5f;
        if (target < 0.5f) gameObject.SetActive(false);
    }
}
