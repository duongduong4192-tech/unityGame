using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class PuzzlePiece : MonoBehaviour
{
    public SpriteRenderer Sr { get; private set; }
    public Collider2D Col { get; private set; }

    [Header("Hít lưới (chỉ dùng cho màn có lưới)")]
    public bool hasGridOffset;
    // Từ tâm ảnh tới một đỉnh lưới của mảnh, khi mảnh chưa xoay (đơn vị Unity, trục y hướng lên)
    public Vector2 gridOffset;

    [System.NonSerialized] public Coroutine slide; // hiệu ứng trượt / xoay đang chạy (nếu có)

    // ===== Bóng đổ =====
    SpriteRenderer shadow;
    Vector2 shadowOffset;
    float shadowAlpha, shadowAlphaTarget;
    Vector2 shadowOffsetTarget;

    public static readonly Vector2 SHADOW_REST = new Vector2(0.03f, -0.04f);
    public static readonly Vector2 SHADOW_HOVER = new Vector2(0.06f, -0.08f);
    public static readonly Vector2 SHADOW_LIFT = new Vector2(0.14f, -0.20f);

    Color baseColor = Color.white;

    void Awake()
    {
        Sr = GetComponent<SpriteRenderer>();
        Col = GetComponent<Collider2D>();
        baseColor = Sr.color;

        GameObject s = new GameObject("Shadow");
        s.transform.SetParent(transform, false);
        shadow = s.AddComponent<SpriteRenderer>();
        shadow.sprite = Sr.sprite;
        shadow.sharedMaterial = Sr.sharedMaterial;
        shadow.color = new Color(0.25f, 0.15f, 0.05f, 0f);

        SetOrder(Sr.sortingOrder / 2);
        SetShadow(0.18f, SHADOW_REST, true);
    }

    // Lớp vẽ: mảnh ở 2k, bóng ngay bên dưới ở 2k-1
    public void SetOrder(int k)
    {
        Sr.sortingOrder = 2 * k;
        if (shadow != null) shadow.sortingOrder = 2 * k - 1;
    }

    public int Order => Sr.sortingOrder / 2;

    public void SetShadow(float alpha, Vector2 offset, bool instant = false)
    {
        shadowAlphaTarget = alpha;
        shadowOffsetTarget = offset;
        if (instant) { shadowAlpha = alpha; shadowOffset = offset; }
    }

    // Ửng đỏ khi chỗ đang kéo tới sẽ đè lên mảnh khác
    public void SetWarning(bool on)
    {
        Sr.color = on ? new Color(baseColor.r, baseColor.g * 0.72f, baseColor.b * 0.72f, baseColor.a) : baseColor;
    }

    void LateUpdate()
    {
        if (shadow == null) return;
        float k = 1f - Mathf.Exp(-14f * Time.deltaTime);   // chuyển mượt
        shadowAlpha = Mathf.Lerp(shadowAlpha, shadowAlphaTarget, k);
        shadowOffset = Vector2.Lerp(shadowOffset, shadowOffsetTarget, k);

        Color c = shadow.color; c.a = shadowAlpha; shadow.color = c;
        // bóng luôn lệch về cùng một hướng trên màn hình, dù mảnh xoay thế nào
        shadow.transform.position = transform.position + (Vector3)shadowOffset;
    }
}
