using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Hướng dẫn trong màn "Số" (lần đầu chơi): mẩu giấy ở cuối màn hình, mỗi bước dạy 1 thao tác,
// làm đúng thao tác thì tự sang bước sau. GameHUD tự gắn component này khi cần.
public class Tutorial : MonoBehaviour
{
    const string CATEGORY = "numbers";

    // (chữ, thao tác cần làm để qua bước; null = bấm "Tiếp")
    static readonly string[][] Steps =
    {
        new[] { "tut_drag",    "drop" },
        new[] { "tut_rotate",  "rotate" },
        new[] { "tut_scroll",  "scroll" },
        new[] { "tut_overlap", null },
        new[] { "tut_undo",    "undo" },
        new[] { "tut_goal",    null },
    };

    public static bool ShouldShow =>
        string.IsNullOrEmpty(GameFlow.LevelId) &&
        (string.IsNullOrEmpty(GameFlow.Category) || GameFlow.Category == CATEGORY) && !Progress.IsCompleted(CATEGORY);

    RectTransform card;
    CanvasGroup group;
    TextMeshProUGUI text, counter;
    LocText textLoc;
    GameObject nextBtn;
    TextMeshProUGUI nextLabel;
    int step = -1;
    bool advancing;

    public void Build(Transform canvas)
    {
        card = UIFactory.Rect("Tutorial", canvas);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
        card.pivot = new Vector2(0.5f, 0);
        card.anchoredPosition = new Vector2(0, 26);
        card.sizeDelta = new Vector2(1080, 170);
        card.localRotation = Quaternion.Euler(0, 0, -0.8f);
        group = card.gameObject.AddComponent<CanvasGroup>();

        Image paper = UIFactory.Img(card, "Paper", HandUI.Sp("hand_label"), Color.white);
        paper.type = Image.Type.Simple;
        paper.raycastTarget = true;              // không cho bấm xuyên xuống bàn
        UIFactory.Stretch(paper.rectTransform);
        PaperShadow.Add(paper, 1.3f);

        counter = HandUI.Text(card, "Step", null, 30, HandUI.Accent);
        RectTransform cr = UIFactory.Place(counter.rectTransform, new Vector2(-470, -8), new Vector2(90, 60));
        cr.localRotation = Quaternion.Euler(0, 0, 4);

        text = HandUI.Text(card, "Text", null, 34, HandUI.Ink);
        text.enableAutoSizing = true; text.fontSizeMin = 24; text.fontSizeMax = 34;
        UIFactory.Place(text.rectTransform, new Vector2(-40, -8), new Vector2(760, 120));
        textLoc = text.gameObject.AddComponent<LocText>();

        Button next = UIFactory.Button(card, "tut_next", new Vector2(440, -8), new Vector2(150, 70), HandUI.Pastels[1], Next, 30);
        next.GetComponent<Image>().sprite = HandUI.Sp("hand_label");
        next.GetComponent<Image>().type = Image.Type.Simple;
        PaperShadow.Add(next.GetComponent<Image>(), 0.6f);
        nextLabel = next.GetComponentInChildren<TextMeshProUGUI>();
        nextLabel.font = UIFont.Hand;
        nextBtn = next.gameObject;

        // nút bỏ qua nhỏ ở góc trên
        TextMeshProUGUI skip = HandUI.Text(card, "Skip", "tut_skip", 24, HandUI.InkSoft);
        UIFactory.Place(skip.rectTransform, new Vector2(440, 70), new Vector2(200, 36));
        skip.raycastTarget = true;
        Button sb = skip.gameObject.AddComponent<Button>();
        sb.transition = Selectable.Transition.None;
        sb.onClick.AddListener(Close);
        skip.gameObject.AddComponent<ButtonBounce>();

        PieceController.PlayerAction += OnAction;
        WinChecker wc = GetComponent<WinChecker>();
        if (wc != null) wc.Won += Close;
        Show(0);
    }

    void OnDestroy() => PieceController.PlayerAction -= OnAction;

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && nextBtn != null && nextBtn.activeSelf && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
            Next();
    }

    void Show(int i)
    {
        step = i;
        textLoc.SetKey(Steps[i][0]);
        text.color = HandUI.Ink;
        counter.text = $"{i + 1}/{Steps.Length}";
        bool manual = Steps[i][1] == null;
        nextBtn.SetActive(manual || i == Steps.Length - 1);
        nextLabel.GetComponent<LocText>().SetKey(i == Steps.Length - 1 ? "tut_ok" : "tut_next");
        StartCoroutine(Pop());
    }

    void OnAction(string a)
    {
        if (advancing || step < 0 || step >= Steps.Length) return;
        if (Steps[step][1] == a) StartCoroutine(Advance());
    }

    void Next()
    {
        if (advancing) return;
        if (step >= Steps.Length - 1) { Close(); return; }
        StartCoroutine(Advance());
    }

    IEnumerator Advance()
    {
        advancing = true;
        textLoc.SetKey("tut_good");      // "Tốt lắm!"
        text.color = new Color32(96, 150, 90, 255);
        yield return new WaitForSecondsRealtime(0.9f);
        advancing = false;
        if (step + 1 < Steps.Length) Show(step + 1);
    }

    IEnumerator Pop()
    {
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / 0.25f);
            float s = 1 + 0.06f * Mathf.Sin(t * Mathf.PI);
            card.localScale = new Vector3(s, s, 1);
            group.alpha = Mathf.Max(group.alpha, t);
            yield return null;
        }
    }

    void Close()
    {
        PieceController.PlayerAction -= OnAction;
        if (card != null) StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        group.blocksRaycasts = false;
        while (group.alpha > 0)
        {
            group.alpha -= Time.unscaledDeltaTime / 0.3f;
            yield return null;
        }
        Destroy(card.gameObject);
        card = null;
    }
}
