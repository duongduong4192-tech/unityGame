using TMPro;
using UnityEngine;

// Gắn vào GameManager. Kiểm tra xem tất cả mảnh đã nằm trọn trong khung chưa.
public class WinChecker : MonoBehaviour
{
    [Header("Vùng bên trong khung")]
    public SpriteRenderer frameInside;

    [Header("Hiện ra khi thắng")]
    public GameObject winText;

    [Header("Mã màn (để lưu tiến trình)")]
    public string levelId = "numbers";

    public float tolerance = 0.02f;     // cho phép lệch mép một chút

    PuzzlePiece[] pieces;
    bool won;

    public bool IsWon => won;

    // Báo khi thắng (GameHUD dùng để hiện bảng "Hoàn thành!"). Có người nghe thì không hiện WinText cũ.
    public event System.Action Won;

    void Start()
    {
        pieces = FindObjectsByType<PuzzlePiece>();
        if (!string.IsNullOrEmpty(GameFlow.LevelId)) levelId = GameFlow.LevelId;            // màn tạo sẵn đang chơi
        else if (!string.IsNullOrEmpty(GameFlow.Category)) levelId = GameFlow.Category;
        if (winText != null)
        {
            // chữ "Hoàn thành!" dùng font có dấu + đổi theo ngôn ngữ
            TMP_Text t = winText.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.font = UIFont.Bold;
                LocText lt = winText.GetComponent<LocText>();
                if (lt == null) lt = winText.AddComponent<LocText>();
                lt.SetKey("complete");
            }
            winText.SetActive(false);
        }
    }

    public void ResetWin()
    {
        won = false;
        if (winText != null) winText.SetActive(false);
    }

    public int CountInside()
    {
        if (pieces == null) return 0;
        if (FrameShape.Current != null)
        {
            int k = 0;
            foreach (PuzzlePiece p in pieces) if (p != null && InsideShape(p, FrameShape.Current)) k++;
            return k;
        }
        if (frameInside == null) return 0;
        Bounds inner = frameInside.bounds;
        inner.Expand(tolerance * 2);
        int n = 0;
        foreach (PuzzlePiece p in pieces) if (p != null && IsInside(p, inner)) n++;
        return n;
    }

    public int Total => pieces == null ? 0 : pieces.Length;

    public void CheckWin()
    {
        if (won || (frameInside == null && FrameShape.Current == null)) return;
        if (CountInside() < pieces.Length) return;   // còn mảnh ở ngoài -> chưa thắng

        won = true;
        Debug.Log("Hoàn thành!");
        Progress.MarkCompleted(levelId);
        GameFlow.JustSolved = levelId;
        if (Won != null) Won();
        else if (winText != null) winText.SetActive(true);

        PieceController controller = GetComponent<PieceController>();
        // thành tựu
        Achievements.Unlock(levelId);
        if (controller != null)
        {
            if (!controller.UndoUsed) Achievements.Unlock("no_undo");
            if (controller.ElapsedTime < 180f) Achievements.Unlock("fast");
            controller.enabled = false;  // khóa điều khiển
        }
    }

    // Khung đa giác (màn tạo sẵn): mọi đỉnh viền của mảnh phải nằm trong khung
    bool InsideShape(PuzzlePiece p, FrameShape f)
    {
        PolygonCollider2D poly = p.Col as PolygonCollider2D;
        if (poly == null) return false;
        for (int i = 0; i < poly.pathCount; i++)
            foreach (Vector2 pt in poly.GetPath(i))
                if (!f.Contains(p.transform.TransformPoint(pt + poly.offset), tolerance)) return false;
        return true;
    }

    // Kiểm tra từng đỉnh viền của mảnh có nằm trong khung không
    bool IsInside(PuzzlePiece p, Bounds inner)
    {
        PolygonCollider2D poly = p.Col as PolygonCollider2D;
        if (poly == null) return inner.Contains(p.Col.bounds.min) && inner.Contains(p.Col.bounds.max);

        for (int i = 0; i < poly.pathCount; i++)
        {
            foreach (Vector2 pt in poly.GetPath(i))
            {
                Vector3 w = p.transform.TransformPoint(pt + poly.offset);
                if (w.x < inner.min.x || w.x > inner.max.x ||
                    w.y < inner.min.y || w.y > inner.max.y)
                    return false;
            }
        }
        return true;
    }
}
