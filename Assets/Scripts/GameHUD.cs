using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Giao diện trong màn chơi, kiểu mẩu giấy viết tay dán băng dính (giống menu):
//   góc trên-phải: Về phòng / Hoàn tác (Ctrl+Z) / Chơi lại
//   khi thắng   : bảng "Hoàn thành!" + Về phòng / Màn tiếp (hoặc Chơi lại nếu là màn cuối)
// PieceController tự gắn component này, không cần làm gì trong Unity.
public class GameHUD : MonoBehaviour
{
    public const string MENU_SCENE = "MainMenu";

    bool built;
    PieceController controller;
    CanvasGroup win;
    RectTransform winCard;
    Coroutine fade;

    public void Build(PieceController ctrl)
    {
        if (built) return;
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ChillPuzzle] Không có Canvas trong scene, bỏ qua các nút.");
            return;
        }
        built = true;
        controller = ctrl;
        EnsureEventSystem();

        RectTransform hud = UIFactory.Stretch(UIFactory.Rect("HUD", canvas.transform));
        Tag(hud, "back_room", new Vector2(-30, -24), 230, 1.5f, GameFlow.BackToMenu, true);
        Tag(hud, "undo", new Vector2(-30, -116), 300, -1f, ctrl.Undo, true);
        Tag(hud, "restart", new Vector2(-30, -208), 230, 1f, Restart, true);

        if (Tutorial.ShouldShow) gameObject.AddComponent<Tutorial>().Build(canvas.transform);
        BuildWin(canvas.transform);
        WinChecker wc = GetComponent<WinChecker>();
        if (wc != null) wc.Won += ShowWin;
    }

    // ---------- mẩu giấy bấm được ----------
    static Button Tag(Transform parent, string locKey, Vector2 pos, float width, float rot,
                      UnityEngine.Events.UnityAction onClick, bool topRight)
    {
        RectTransform r = UIFactory.Rect("Tag_" + locKey, parent);
        if (topRight)
        {
            r.anchorMin = r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(1, 1);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(width, 84);
        }
        else UIFactory.Place(r, pos, new Vector2(width, 84));
        r.localRotation = Quaternion.Euler(0, 0, rot);

        Image paper = r.gameObject.AddComponent<Image>();
        paper.sprite = HandUI.Sp("hand_label");
        paper.type = Image.Type.Simple;
        PaperShadow.Add(paper, 1f);
        Button b = r.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        r.gameObject.AddComponent<ButtonBounce>();

        TextMeshProUGUI t = HandUI.Text(r, "Text", locKey, 34, HandUI.Ink);
        UIFactory.Stretch(t.rectTransform).offsetMax = new Vector2(0, -16);
        return b;
    }

    // ---------- bảng thắng ----------
    void BuildWin(Transform canvas)
    {
        RectTransform root = UIFactory.Stretch(UIFactory.Rect("WinPanel", canvas));
        win = root.gameObject.AddComponent<CanvasGroup>();
        UIFactory.Stretch(UIFactory.Solid(root, "Dim", HandUI.Dim, true).rectTransform);   // chặn bấm vào bàn

        winCard = UIFactory.Place(UIFactory.Rect("Card", root), new Vector2(0, 20), new Vector2(720, 420));
        winCard.localRotation = Quaternion.Euler(0, 0, -1.2f);
        Image paper = UIFactory.Img(winCard, "Paper", HandUI.Sp("hand_paper"), Color.white);
        paper.type = Image.Type.Simple;
        PaperShadow.Add(paper, 2f);
        UIFactory.Stretch(paper.rectTransform);
        paper.raycastTarget = false;

        HandUI.TapeTitle(winCard, "complete", new Vector2(0, 160));
        Image blob = HandUI.Img(winCard, "Medal", "hand_blob", new Vector2(0, 30), new Vector2(150, 150), 8f,
                                HandUI.Pastels[2]);
        TextMeshProUGUI star = HandUI.Text(blob.transform, "Star", null, 90, HandUI.Accent);
        UIFactory.Stretch(star.rectTransform);
        star.text = "♥";     // trái tim (font viết tay có sẵn ký tự này)

        Tag(winCard, "back_room", new Vector2(-150, -120), 260, 2f, GameFlow.BackToMenu, false);
        if (GameFlow.HasNext) Tag(winCard, "next_level", new Vector2(150, -120), 260, -1.5f, GameFlow.PlayNext, false);
        else Tag(winCard, "restart", new Vector2(150, -120), 260, -1.5f, Restart, false);

        root.gameObject.SetActive(false);
    }

    void ShowWin()
    {
        if (win == null) return;
        Sfx.Play("win");
        win.alpha = 0;
        win.gameObject.SetActive(true);
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        yield return new WaitForSecondsRealtime(0.45f);     // cho mảnh cuối đặt xong
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.unscaledDeltaTime / 0.5f);
            float e = 1 - Mathf.Pow(1 - t, 3);
            win.alpha = e;
            winCard.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
            yield return null;
        }
        fade = null;
    }

    void HideWin()
    {
        if (fade != null) { StopCoroutine(fade); fade = null; }
        if (win != null) { win.alpha = 0; win.gameObject.SetActive(false); }
    }

    void Restart()
    {
        HideWin();
        controller.RestartLevel();
        LevelSave.Restarted();
    }

    static void GoToMenu()
    {
        if (Application.CanStreamedLevelBeLoaded(MENU_SCENE)) SceneManager.LoadScene(MENU_SCENE);
        else Debug.LogWarning("[ChillPuzzle] Chưa có scene MainMenu. Chạy menu ChillPuzzle → Setup Main Menu.");
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();
    }
}
