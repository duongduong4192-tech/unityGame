using UnityEngine;

// Hình khung gỗ: tự co giãn theo vùng lòng khung (FrameInside) của từng màn.
// Gồm 3 lớp tách riêng -> muốn đổi khung chỉ cần thay ảnh frame_wood.png:
//   Wood   : viền khung (lòng trong suốt), kéo giãn 9 mảnh, góc vát giữ nguyên
//   Board  : tấm ván lòng khung (có bóng khung đổ vào trong)
//   Shadow : bóng khung đổ xuống mặt bàn
[ExecuteAlways]
public class FrameVisual : MonoBehaviour
{
    public SpriteRenderer frameInside;      // vùng thắng (chỉ dùng để lấy kích thước, đã ẩn hình)
    public SpriteRenderer wood, board, shadow;

    [Header("Tấm ván lòng khung")]
    public float boardPadPx = 6;            // ván lấn dưới viền vài px để không hở mép

    [Header("Bóng đổ của khung")]
    public Vector2 shadowOffset = new Vector2(0.08f, -0.12f);
    public float shadowSpread = 0.18f;
    [Range(0, 1)] public float shadowAlpha = 0.6f;

    void LateUpdate() => Fit();

    public void Fit()
    {
        if (frameInside == null) return;
        Bounds b = frameInside.bounds;
        Vector2 c = b.center, size = b.size;
        if (size.x <= 0 || size.y <= 0) return;

        Vector2 woodCenter = c, woodSize = size;
        if (wood != null && wood.sprite != null)
        {
            Sprite s = wood.sprite;
            float ppu = s.pixelsPerUnit;
            // border = (trái, dưới, phải, trên) tính bằng px = đúng độ dày viền khung trong ảnh
            float L = s.border.x / ppu, B = s.border.y / ppu, R = s.border.z / ppu, T = s.border.w / ppu;
            woodSize = new Vector2(size.x + L + R, size.y + T + B);
            woodCenter = c - new Vector2((L - R) / 2f, (B - T) / 2f);
            Place(wood, woodCenter, woodSize);
        }
        if (board != null && board.sprite != null)
        {
            float pad = boardPadPx / board.sprite.pixelsPerUnit;
            Place(board, c, size + Vector2.one * 2 * pad);
        }
        if (shadow != null && shadow.sprite != null)
        {
            Place(shadow, woodCenter + shadowOffset, woodSize + Vector2.one * 2 * shadowSpread);
            Color col = shadow.color; col.a = shadowAlpha; shadow.color = col;
        }
    }

    static void Place(SpriteRenderer sr, Vector2 pos, Vector2 size)
    {
        if (sr.drawMode == SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Sliced;
        Transform t = sr.transform;
        Vector3 p = new Vector3(pos.x, pos.y, t.position.z);
        if (t.position != p) t.position = p;
        if (t.rotation != Quaternion.identity) t.rotation = Quaternion.identity;
        if (t.lossyScale != Vector3.one && t.parent == null) t.localScale = Vector3.one;
        if (sr.size != size) sr.size = size;
    }
}
