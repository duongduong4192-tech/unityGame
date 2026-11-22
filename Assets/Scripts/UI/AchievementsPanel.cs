using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thành tựu kiểu "trang sổ tay" (mở từ giá tranh trong phòng).
// Mỗi thành tựu là một tấm thẻ giấy được dán băng dính lên trang:
//   chưa mở khóa -> thẻ xám nhạt, dấu "?" vẽ bút chì, "???" và "Thành tựu ẩn"
//   đã mở       -> vệt màu nước pastel làm huy hiệu + tên + mô tả
public class AchievementsPanel : MonoBehaviour, IMenuPanel
{
    const float PX = 0, PY = 0, PW = 1400, PH = 780;

    CanvasGroup group;
    RectTransform grid;
    TextMeshProUGUI counter;
    Action onClose;

    public bool IsOpen { get; private set; }

    public static AchievementsPanel Create(Transform parent, Action onClose)
    {
        RectTransform rt = UIFactory.Stretch(UIFactory.Rect("AchievementsPanel", parent));
        AchievementsPanel a = rt.gameObject.AddComponent<AchievementsPanel>();
        a.onClose = onClose;
        a.Build();
        return a;
    }

    void Build()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        Image back = UIFactory.Solid(transform, "Dim", HandUI.Dim, true);
        UIFactory.Stretch(back.rectTransform);

        HandUI.Img(transform, "Paper", "hand_paper_wide", new Vector2(PX, PY), new Vector2(PW, PH), 0.5f);
        HandUI.TapeTitle(transform, "achievements", new Vector2(-560, 390));

        counter = HandUI.Text(transform, "Counter", null, 56, HandUI.Accent, TextAlignmentOptions.MidlineRight);
        UIFactory.Place(counter.rectTransform, new Vector2(520, 320), new Vector2(300, 80));

        grid = UIFactory.Stretch(UIFactory.Rect("Cards", transform));

        HandUI.StampHint(transform, "Esc", "back", new Vector2(0, -455), Close);

        Loc.Changed += Rebuild;
        group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    void OnDestroy() => Loc.Changed -= Rebuild;

    void Rebuild()
    {
        foreach (Transform c in grid) Destroy(c.gameObject);
        Achievements.Def[] all = Achievements.All;
        counter.text = $"{Achievements.UnlockedCount} / {all.Length}";

        const int cols = 4;
        for (int i = 0; i < all.Length; i++)
        {
            Achievements.Def d = all[i];
            bool open = Achievements.IsUnlocked(d.id);
            int cx = i % cols, cy = i / cols;
            Vector2 pos = new Vector2((cx - (cols - 1) / 2f) * 320, 115 - cy * 330);
            float tilt = ((i * 37) % 7 - 3) * 0.9f;   // mỗi thẻ nghiêng một chút, cố định theo vị trí

            Image card = HandUI.Img(grid, "Ach_" + d.id, "hand_card", pos, new Vector2(290, 290), tilt,
                                    open ? Color.white : new Color(0.86f, 0.84f, 0.82f, 1f));
            Transform c = card.transform;

            // băng dính nhỏ giữ thẻ
            HandUI.Img(c, "Tape", "hand_tape_small", new Vector2(0, 140), new Vector2(150, 46), -tilt * 2f,
                       new Color(1, 1, 1, 0.92f));

            if (open)
            {
                Image medal = HandUI.Img(c, "Medal", "hand_blob", new Vector2(0, 50), new Vector2(120, 120), 0,
                                         HandUI.Pastels[i % HandUI.Pastels.Length]);
                TextMeshProUGUI n = HandUI.Text(medal.transform, "N", null, 56, HandUI.Ink);
                UIFactory.Stretch(n.rectTransform);
                n.text = (i + 1).ToString();
            }
            else
            {
                TextMeshProUGUI q = HandUI.Text(c, "Q", null, 120, new Color32(170, 160, 150, 200));
                UIFactory.Place(q.rectTransform, new Vector2(0, 50), new Vector2(200, 140));
                q.text = "?";
            }

            TextMeshProUGUI name = HandUI.Text(c, "Name", open ? d.NameKey : null, 36,
                                               open ? HandUI.Ink : new Color32(150, 140, 132, 255));
            UIFactory.Place(name.rectTransform, new Vector2(0, -42), new Vector2(260, 48));
            if (!open) name.text = "???";

            TextMeshProUGUI desc = HandUI.Text(c, "Desc", open ? d.DescKey : "ach_hidden", 26,
                                               open ? HandUI.InkSoft : new Color32(160, 150, 142, 255));
            UIFactory.Place(desc.rectTransform, new Vector2(0, -98), new Vector2(250, 70));
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Rebuild();
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
