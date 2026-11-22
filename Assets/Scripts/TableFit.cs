using UnityEngine;

// Mặt bàn luôn phủ kín màn hình, mọi tỉ lệ màn hình (16:9, 16:10, 21:9...).
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class TableFit : MonoBehaviour
{
    public float overscan = 1.02f;   // dư một chút để không lộ mép

    SpriteRenderer sr;

    void LateUpdate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic || sr.sprite == null) return;

        float h = cam.orthographicSize * 2f, w = h * cam.aspect;
        Vector2 sp = sr.sprite.bounds.size;
        float k = Mathf.Max(w / sp.x, h / sp.y) * overscan;
        Vector3 scale = new Vector3(k, k, 1);
        if (transform.localScale != scale) transform.localScale = scale;
        Vector3 pos = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
        if (transform.position != pos) transform.position = pos;
    }
}
