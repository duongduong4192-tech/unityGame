using UnityEngine;
using UnityEngine.EventSystems;

// Nút phồng nhẹ khi rê chuột, lún khi bấm (cảm giác nút gỗ)
public class ButtonBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hover = 1.05f, press = 0.95f, speed = 18f;
    float target = 1f;
    bool inside;

    public void OnPointerEnter(PointerEventData e) { inside = true; target = hover; }
    public void OnPointerExit(PointerEventData e) { inside = false; target = 1f; }
    public void OnPointerDown(PointerEventData e) { target = press; Sfx.Play("click"); }
    public void OnPointerUp(PointerEventData e) { target = inside ? hover : 1f; }

    void OnDisable() { inside = false; target = 1f; transform.localScale = Vector3.one; }

    void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(s, s, 1f);
    }
}
