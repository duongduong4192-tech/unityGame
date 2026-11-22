using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Một đồ vật bấm được trong căn phòng menu (quyển sách, bộ xếp hình, giá tranh).
// - Lúc vào game: sáng lên nhẹ và "thở" chậm
// - Rê chuột: sáng rõ + viền phát sáng + hiện nhãn tên
// - Bấm: gọi onClick
public class Hotspot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image lit;            // lớp đồ vật sáng hơn
    public Image rim;            // viền phát sáng quanh đồ vật
    public CanvasGroup label;    // nhãn tên
    public System.Action onClick;

    public float introDelay;     // bao lâu sau khi vào game thì bắt đầu sáng
    public bool interactable = true;

    bool hovered;
    float intro, litA, rimA, labA;
    float phase;
    Vector2 labelBase;
    RectTransform labelRect;

    void Start()
    {
        phase = Random.value * 6f;
        if (label != null)
        {
            labelRect = (RectTransform)label.transform;
            labelBase = labelRect.anchoredPosition;
            label.alpha = 0;
        }
        SetA(lit, 0); SetA(rim, 0);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (introDelay > 0) introDelay -= dt;
        else intro = Mathf.MoveTowards(intro, 1f, dt / 0.9f);

        bool on = hovered && interactable;
        float pulse = 0.42f + 0.14f * Mathf.Sin(Time.unscaledTime * 1.8f + phase);   // "thở" chậm
        float k = 1f - Mathf.Exp(-10f * dt);

        litA = Mathf.Lerp(litA, on ? 1f : pulse * intro, k);
        rimA = Mathf.Lerp(rimA, on ? 0.95f : 0.12f * intro, k);
        labA = Mathf.Lerp(labA, on ? 1f : 0f, k);

        SetA(lit, litA);
        SetA(rim, rimA);
        if (label != null)
        {
            label.alpha = labA;
            labelRect.anchoredPosition = labelBase + new Vector2(0, (labA - 1f) * 14f);   // trượt nhẹ lên khi hiện
        }
    }

    static void SetA(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }

    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;

    public void OnPointerClick(PointerEventData e)
    {
        if (!interactable || e.button != PointerEventData.InputButton.Left) return;
        hovered = false;
        Sfx.Play("click");
        onClick?.Invoke();
    }
}
