using System.Collections;
using UnityEngine;

// Hiệu ứng lúc vào màn: từng mảnh gỗ "rơi" từ trên cao xuống bàn (giả 3D bằng 2D):
// mảnh to + mờ + bóng xa lệch nhiều  ->  nhỏ dần về đúng cỡ, bóng thu lại sát mảnh, nảy nhẹ khi chạm bàn.
// LevelLoader tự gắn vào mọi mảnh khi PuzzleScene vừa nạp. Trong lúc rơi không nhấc được mảnh.
public class PieceDrop : MonoBehaviour
{
    public static float StartDelay = 0.25f, Stagger = 0.07f, FallTime = 0.42f;
    public static event System.Action<PuzzlePiece> Landed;   // chỗ gắn tiếng "cạch" sau này

    public int index;
    PuzzlePiece piece;
    Collider2D col;
    Vector3 endPos, endScale;

    void Awake()
    {
        piece = GetComponent<PuzzlePiece>();
        col = GetComponent<Collider2D>();
        endPos = transform.position;
        endScale = transform.localScale;
        if (piece != null && piece.Sr != null) piece.Sr.color = new Color(1, 1, 1, 0);
        piece?.SetShadow(0f, PuzzlePiece.SHADOW_LIFT * 3f, true);
    }

    IEnumerator Start()
    {
        yield return null;                      // để PieceController lưu cỡ gốc của mảnh trước
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(StartDelay + index * Stagger);

        SpriteRenderer sr = piece != null ? piece.Sr : GetComponent<SpriteRenderer>();
        Vector3 lift = new Vector3(0.25f, 0.9f, 0);        // lệch lên trên một chút như đang ở gần camera
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.deltaTime / FallTime);
            float fall = t * t;                              // rơi nhanh dần
            float h = 1 - fall;                              // độ cao còn lại (1 -> 0)
            transform.localScale = endScale * (1 + 0.65f * h);
            transform.position = endPos + lift * h;
            if (sr != null) sr.color = new Color(1, 1, 1, Mathf.Clamp01(t * 3f));
            piece?.SetShadow(Mathf.Lerp(0.32f, 0.08f, h), Vector2.Lerp(PuzzlePiece.SHADOW_REST, PuzzlePiece.SHADOW_LIFT * 3f, h), true);
            yield return null;
        }

        transform.position = endPos;
        if (sr != null) sr.color = Color.white;
        Sfx.Play("land");
        Landed?.Invoke(piece);

        // nảy nhẹ: dẹt xuống rồi trở lại
        t = 0;
        while (t < 1)
        {
            t = Mathf.Min(1, t + Time.deltaTime / 0.18f);
            float k = 1 - 0.05f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = endScale * k;
            yield return null;
        }
        transform.localScale = endScale;
        piece?.SetShadow(0.18f, PuzzlePiece.SHADOW_REST);
        if (col != null) col.enabled = true;
        Destroy(this);
    }
}
