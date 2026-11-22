using UnityEngine;
using UnityEngine.UI;

// Bóng đổ mềm cho giấy / nút giấy / khung gỗ, giúp nhìn nổi khối hơn.
// Bóng là 1 ảnh mờ (Resources/UI/shadow/<tên ảnh>.png) đặt NGAY SAU vật thể, tự bám theo vị trí, cỡ, góc nghiêng.
// Khi nút phồng lên (rê chuột) thì bóng lệch xa hơn và nhạt đi -> cảm giác nút được nhấc lên.
public class PaperShadow : MonoBehaviour
{
    const float DOWNSCALE = 4f;     // ảnh bóng được lưu nhỏ 1/4 (vì đã làm mờ)

    public RectTransform target;
    public Vector2 offset = new Vector2(8, -12);
    [Range(0, 1)] public float alpha = 0.72f;

    Image img;
    Graphic targetGraphic;
    Vector2 padFrac;    // phần bóng thò ra mỗi bên, tính theo tỉ lệ cỡ vật thể

    // Thêm bóng cho 1 ảnh giấy. Không có ảnh bóng tương ứng thì bỏ qua.
    // follow: bám theo 1 khung khác (vd. cả ô chứa ảnh + khung), mặc định bám theo chính ảnh giấy
    public static PaperShadow Add(Image paper, float depth = 1f, RectTransform follow = null)
    {
        if (paper == null || paper.sprite == null) return null;
        Sprite sh = Resources.Load<Sprite>("UI/shadow/" + paper.sprite.name);
        if (sh == null) return null;

        RectTransform t = follow != null ? follow : paper.rectTransform;
        GameObject go = new GameObject(paper.name + "_Shadow", typeof(RectTransform));
        go.transform.SetParent(t.parent, false);
        go.transform.SetSiblingIndex(t.GetSiblingIndex());          // nằm ngay dưới vật thể
        PaperShadow ps = go.AddComponent<PaperShadow>();
        ps.target = t;
        ps.targetGraphic = follow != null ? null : paper;
        ps.offset *= depth;
        ps.img = go.AddComponent<Image>();
        ps.img.sprite = sh;
        ps.img.raycastTarget = false;
        Rect s = paper.sprite.rect, r = sh.rect;
        ps.padFrac = new Vector2((r.width * DOWNSCALE - s.width) / 2f / s.width,
                                 (r.height * DOWNSCALE - s.height) / 2f / s.height);
        ps.LateUpdate();
        return ps;
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        RectTransform me = (RectTransform)transform;
        bool on = target.gameObject.activeInHierarchy && (targetGraphic == null || targetGraphic.enabled);
        if (img.enabled != on) img.enabled = on;
        if (!on) return;

        // nút đang phồng (ButtonBounce) -> "nhấc" lên
        float lift = Mathf.Clamp01((target.localScale.x - 1f) / 0.06f);
        Vector2 size = target.rect.size;
        Vector2 pad = new Vector2(size.x * padFrac.x, size.y * padFrac.y) * (1f + 0.25f * lift);

        me.anchorMin = target.anchorMin;
        me.anchorMax = target.anchorMax;
        me.pivot = target.pivot;
        me.offsetMin = target.offsetMin - pad;
        me.offsetMax = target.offsetMax + pad;
        me.anchoredPosition += offset * (1f + 1.2f * lift);
        me.localRotation = target.localRotation;
        me.localScale = target.localScale;
        Color c = img.color; c.a = alpha * (1f - 0.3f * lift); img.color = c;
    }
}
