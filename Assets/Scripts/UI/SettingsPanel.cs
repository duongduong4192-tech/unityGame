using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cài đặt kiểu "trang sổ tay vẽ tay" (mở từ quyển sách trên bàn):
// giấy màu nước, tiêu đề trên băng dính, các mục là tab giấy pastel thò ra mép phải (xếp dọc),
// dòng đang chọn tô bút dạ quang, mũi tên vẽ tay, gợi ý phím là con dấu mực.
// Điều khiển: chuột, hoặc phím  ↑↓ chọn dòng  ←→ đổi giá trị  Q/E đổi mục  R mặc định.
public class SettingsPanel : MonoBehaviour, IMenuPanel
{
    // khung giấy (tọa độ theo màn 1920x1080, gốc ở giữa)
    const float PX = -110, PY = 20, PW = 1200, PH = 700;
    const float ROW_Y0 = 133, ROW_STEP = 74, ROW_W = 1040, ROW_H = 66;

    class RowDef { public string key; public Func<string> value; public Action<int> change; }
    class RowUI { public RowDef def; public Image highlight; public TextMeshProUGUI value; }

    static readonly string[] TabKeys = { "tab_audio", "tab_display", "tab_game", "tab_about" };
    static readonly string[] SectionKeys = { "sec_volume", "sec_screen", "sec_play", null };

    CanvasGroup group;
    RectTransform paper, rowsRoot, aboutRoot;
    TextMeshProUGUI section, desc;
    readonly List<RectTransform> tabs = new List<RectTransform>();
    readonly List<TextMeshProUGUI> tabTexts = new List<TextMeshProUGUI>();
    readonly List<RowUI> rows = new List<RowUI>();
    int tab, selected;
    Action onClose;
    string publisher;

    public bool IsOpen { get; private set; }

    public static SettingsPanel Create(Transform parent, string publisherName, Action onClose)
    {
        RectTransform rt = UIFactory.Stretch(UIFactory.Rect("SettingsPanel", parent));
        SettingsPanel s = rt.gameObject.AddComponent<SettingsPanel>();
        s.onClose = onClose;
        s.publisher = publisherName;
        s.Build();
        return s;
    }

    // ===================== DỰNG GIAO DIỆN =====================
    void Build()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        Image back = UIFactory.Solid(transform, "Dim", HandUI.Dim, true);
        UIFactory.Stretch(back.rectTransform);

        // tab giấy (vẽ trước để nằm "dưới" tờ giấy)
        for (int i = 0; i < TabKeys.Length; i++)
        {
            int idx = i;
            Image tb = HandUI.Img(transform, "Tab_" + TabKeys[i], "hand_tab_" + HandUI.TabColors[i],
                                  new Vector2(605, 234 - i * 125), new Vector2(300, 112));
            tb.raycastTarget = true;
            Button b = tb.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play("click"); SetTab(idx); });
            TextMeshProUGUI tt = HandUI.Text(tb.transform, "Text", TabKeys[i], 44, HandUI.Ink);
            UIFactory.Stretch(tt.rectTransform).offsetMin = new Vector2(30, 0);
            tabs.Add(tb.rectTransform); tabTexts.Add(tt);
        }

        // tờ giấy
        Image p = HandUI.Img(transform, "Paper", "hand_paper", new Vector2(PX, PY), new Vector2(PW, PH), -0.6f);
        paper = p.rectTransform;
        HandUI.TapeTitle(transform, "settings", new Vector2(-540, 367));

        section = HandUI.Text(transform, "Section", null, 60, HandUI.Accent, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(section.rectTransform, new Vector2(-110, 250), new Vector2(920, 80));

        rowsRoot = UIFactory.Stretch(UIFactory.Rect("Rows", transform));
        aboutRoot = UIFactory.Stretch(UIFactory.Rect("About", transform));
        BuildAbout();

        desc = HandUI.Text(transform, "Desc", null, 36, HandUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(desc.rectTransform, new Vector2(-110, -180), new Vector2(920, 56));

        // con dấu gợi ý phím (bấm được)
        HandUI.StampHint(transform, "Esc", "back", new Vector2(-540, -426), Close);
        HandUI.StampHint(transform, "R", "reset_default", new Vector2(-80, -426), ResetTab);
        HandUI.StampHint(transform, "X", "quit_game", new Vector2(420, -426), Quit);

        GameSettings.Changed += RefreshValues;
        Loc.Changed += OnLang;

        SetTab(0);
        group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        GameSettings.Changed -= RefreshValues;
        Loc.Changed -= OnLang;
    }

    void BuildAbout()
    {
        float y = 210;
        AboutLine("title", 72, HandUI.Accent, ref y, 86);
        TextMeshProUGUI ver = AboutLine(null, 36, HandUI.Ink, ref y, 56);
        ver.text = $"v{Application.version}  -  © {DateTime.Now.Year}";
        AboutLine("developer", 32, HandUI.InkSoft, ref y, 46);
        TextMeshProUGUI dev = AboutLine(null, 50, HandUI.Ink, ref y, 74);
        dev.text = publisher;
        AboutLine("font_credit", 30, HandUI.InkSoft, ref y, 44);
        TextMeshProUGUI hand = AboutLine(null, 30, HandUI.InkSoft, ref y, 44);
        hand.text = "Patrick Hand – Patrick Wagesreiter (SIL OFL 1.1)";
        AboutLine("engine", 30, HandUI.InkSoft, ref y, 60);
        AboutLine("thanks", 44, HandUI.Accent, ref y, 60);
    }

    TextMeshProUGUI AboutLine(string key, float size, Color color, ref float y, float step)
    {
        TextMeshProUGUI t = HandUI.Text(aboutRoot, "Line", key, size, color);
        UIFactory.Place(t.rectTransform, new Vector2(PX, y), new Vector2(PW - 160, step));
        y -= step;
        return t;
    }

    // ===================== CÁC DÒNG CÀI ĐẶT =====================
    List<RowDef> DefsFor(int t)
    {
        var list = new List<RowDef>();
        switch (t)
        {
            case 0:
                list.Add(Volume("master", () => GameSettings.Master, v => GameSettings.Master = v));
                list.Add(Volume("music", () => GameSettings.Music, v => GameSettings.Music = v));
                list.Add(Volume("sfx", () => GameSettings.Sfx, v => GameSettings.Sfx = v));
                break;
            case 1:
                list.Add(new RowDef { key = "display_mode",
                    value = () => Loc.Get(GameSettings.Fullscreen ? "fullscreen" : "windowed"),
                    change = d => GameSettings.Fullscreen = !GameSettings.Fullscreen });
                list.Add(new RowDef { key = "resolution", value = () => GameSettings.ResolutionLabel,
                    change = d => GameSettings.ResolutionIndex = GameSettings.ResolutionIndex + d });
                list.Add(Toggle("vsync", () => GameSettings.VSync, v => GameSettings.VSync = v));
                list.Add(new RowDef { key = "fps",
                    value = () => GameSettings.FrameLimits[GameSettings.FrameLimitIndex] < 0
                        ? Loc.Get("unlimited") : GameSettings.FrameLimits[GameSettings.FrameLimitIndex] + " FPS",
                    change = d => GameSettings.FrameLimitIndex = GameSettings.FrameLimitIndex + d });
                break;
            case 2:
                list.Add(new RowDef { key = "language", value = () => Loc.Get("lang_name"),
                    change = d => Loc.Current = Loc.Current == Loc.Lang.VI ? Loc.Lang.EN : Loc.Lang.VI });
                list.Add(Toggle("snap_rotate", () => GameSettings.SnapRotate, v => GameSettings.SnapRotate = v));
                list.Add(Toggle("show_ring", () => GameSettings.ShowRing, v => GameSettings.ShowRing = v));
                list.Add(Toggle("overlap_warn", () => GameSettings.OverlapWarn, v => GameSettings.OverlapWarn = v));
                break;
        }
        return list;
    }

    static RowDef Volume(string key, Func<float> get, Action<float> set) => new RowDef
    {
        key = key,
        value = () => Mathf.RoundToInt(get() * 100) + "%",
        change = d => set(Mathf.Clamp01(Mathf.Round(get() * 10 + d) / 10f)),
    };

    static RowDef Toggle(string key, Func<bool> get, Action<bool> set) => new RowDef
    {
        key = key,
        value = () => Loc.Get(get() ? "on" : "off"),
        change = d => set(!get()),
    };

    void BuildRows()
    {
        foreach (Transform c in rowsRoot) Destroy(c.gameObject);
        rows.Clear();

        List<RowDef> defs = DefsFor(tab);
        for (int i = 0; i < defs.Count; i++)
        {
            int idx = i;
            RowDef def = defs[i];
            RectTransform r = UIFactory.Place(UIFactory.Rect("Row_" + def.key, rowsRoot),
                                              new Vector2(PX, ROW_Y0 - i * ROW_STEP), new Vector2(ROW_W, ROW_H));
            Image hit = r.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            RowHover hov = r.gameObject.AddComponent<RowHover>();
            hov.onEnter = () => Select(idx);
            hov.onClick = () => { Select(idx); Change(1); };

            Image hl = HandUI.Img(r, "Highlight", "hand_highlight", Vector2.zero, new Vector2(1000, 70));
            hl.color = new Color(1, 1, 1, 0);

            TextMeshProUGUI label = HandUI.Text(r, "Label", def.key, 44, HandUI.Ink, TextAlignmentOptions.MidlineLeft);
            UIFactory.Stretch(label.rectTransform).offsetMin = new Vector2(60, 0);

            Arrow(r, new Vector2(240, 0), true, () => { Select(idx); Change(-1); });
            Arrow(r, new Vector2(500, 0), false, () => { Select(idx); Change(1); });
            TextMeshProUGUI val = HandUI.Text(r, "Value", null, 42, HandUI.Ink);
            UIFactory.Place(val.rectTransform, new Vector2(370, 0), new Vector2(220, ROW_H));

            rows.Add(new RowUI { def = def, highlight = hl, value = val });
        }
        rowsRoot.gameObject.SetActive(defs.Count > 0);
        aboutRoot.gameObject.SetActive(defs.Count == 0);
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, rows.Count - 1));
        RefreshValues();
        Select(selected);
    }

    void Arrow(Transform parent, Vector2 pos, bool flip, UnityEngine.Events.UnityAction onClick)
    {
        Image a = HandUI.Img(parent, flip ? "Left" : "Right", "hand_arrow", pos, new Vector2(44, 44));
        a.raycastTarget = true;
        if (flip) a.rectTransform.localScale = new Vector3(-1, 1, 1);
        Button b = a.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
    }

    // ===================== HÀNH VI =====================
    void SetTab(int t)
    {
        tab = ((t % TabKeys.Length) + TabKeys.Length) % TabKeys.Length;
        selected = 0;
        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = i == tab;
            tabs[i].anchoredPosition = new Vector2(on ? 630 : 605, 234 - i * 125);   // tab đang chọn thò ra xa hơn
            tabTexts[i].fontSize = on ? 48 : 44;
        }
        string sk = SectionKeys[tab];
        section.gameObject.SetActive(sk != null);
        if (sk != null) section.text = "~ " + Loc.Get(sk) + " ~";
        BuildRows();
    }

    void Select(int i)
    {
        if (rows.Count == 0) { desc.text = ""; return; }
        selected = Mathf.Clamp(i, 0, rows.Count - 1);
        for (int k = 0; k < rows.Count; k++)
            rows[k].highlight.color = new Color(1, 1, 1, k == selected ? 0.95f : 0f);
        desc.text = Loc.Get(rows[selected].def.key + "_d");
    }

    void Change(int d)
    {
        if (rows.Count == 0) return;
        rows[selected].def.change(d);
        RefreshValues();
        Sfx.Play("click");     // phát sau khi đổi, nên chỉnh âm lượng là nghe được ngay mức mới
    }

    void RefreshValues()
    {
        foreach (RowUI r in rows) r.value.text = r.def.value();
    }

    void OnLang()
    {
        if (SectionKeys[tab] != null) section.text = "~ " + Loc.Get(SectionKeys[tab]) + " ~";
        RefreshValues();
        Select(selected);
    }

    void ResetTab()
    {
        switch (tab)
        {
            case 0: GameSettings.ResetAudio(); break;
            case 1: GameSettings.ResetDisplay(); break;
            case 2: GameSettings.ResetGameplay(); break;
        }
        RefreshValues();
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        if (!IsOpen) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Select(selected - 1);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Select(selected + 1);
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Change(-1);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) Change(1);
        if (kb.qKey.wasPressedThisFrame) SetTab(tab - 1);
        if (kb.eKey.wasPressedThisFrame) SetTab(tab + 1);
        if (kb.rKey.wasPressedThisFrame) ResetTab();
    }

    // ===================== MỞ / ĐÓNG =====================
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

    // hiện ra: mờ dần + tờ giấy trượt nhẹ lên như vừa đặt xuống bàn
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
            float off = (1f - Mathf.Lerp(start, target, k)) * -30f;
            ((RectTransform)transform).anchoredPosition = new Vector2(0, off);
            yield return null;
        }
        group.alpha = target;
        ((RectTransform)transform).anchoredPosition = Vector2.zero;
        group.interactable = target > 0.5f;
        if (target < 0.5f) gameObject.SetActive(false);
    }
}
