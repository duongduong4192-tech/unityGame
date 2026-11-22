using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Bộ giao diện kiểu "sổ tay vẽ tay": giấy màu nước, băng dính washi, tab giấy, con dấu mực.
// Hình nằm trong Assets/Resources/UI/hand — thay file cùng tên là đổi giao diện, không cần sửa code.
public static class HandUI
{
    public static readonly Color Ink = new Color32(96, 70, 54, 255);          // mực nâu nhạt
    public static readonly Color InkSoft = new Color32(150, 116, 92, 255);    // chữ mô tả
    public static readonly Color Accent = new Color32(214, 132, 104, 255);    // tiêu đề mục (đỏ đất nhạt)
    public static readonly Color Cream = new Color32(255, 246, 232, 255);     // chữ trên nền phòng
    public static readonly Color Dim = new Color(0.24f, 0.14f, 0.08f, 0.45f); // phủ phòng phía sau

    public static readonly string[] TabColors = { "peach", "sage", "butter", "lavender" };
    public static readonly Color[] Pastels =
    {
        new Color32(252, 200, 180, 255), new Color32(190, 222, 182, 255), new Color32(250, 228, 160, 255),
        new Color32(210, 196, 240, 255), new Color32(180, 214, 236, 255), new Color32(248, 196, 214, 255),
    };

    public static Sprite Sp(string name) => Resources.Load<Sprite>("UI/hand/" + name);

    public static Image Img(Transform parent, string name, string sprite, Vector2 pos, Vector2 size,
                            float rot = 0, Color? color = null)
    {
        Image img = UIFactory.Img(parent, name, Sp(sprite), color ?? Color.white);
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        UIFactory.Place(img.rectTransform, pos, size);
        img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        PaperShadow.Add(img);       // giấy / thẻ có bóng đổ (nếu có ảnh bóng)
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string locKey, float size, Color color,
                                       TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI t = UIFactory.Text(parent, name, locKey, size, color, true, align);
        t.font = UIFont.Hand;
        return t;
    }

    // Tiêu đề trên miếng băng dính dán chéo
    public static void TapeTitle(Transform parent, string locKey, Vector2 pos)
    {
        Image tape = Img(parent, "TitleTape", "hand_tape", pos, new Vector2(420, 110), 4f);
        TextMeshProUGUI t = Text(tape.transform, "Text", locKey, 66, new Color32(110, 56, 40, 255));
        UIFactory.Stretch(t.rectTransform);
    }

    // Gợi ý phím: con dấu mực tròn + chữ, bấm được
    public static void StampHint(Transform parent, string key, string locKey, Vector2 pos, UnityAction onClick)
    {
        RectTransform r = UIFactory.Place(UIFactory.Rect("Hint_" + locKey, parent), pos, new Vector2(440, 80));
        Image hit = r.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        Button b = r.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
        b.onClick.AddListener(onClick);
        r.gameObject.AddComponent<ButtonBounce>();

        Image st = UIFactory.Img(r, "Stamp", Sp("hand_stamp"), Color.white);
        st.raycastTarget = false;
        st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0, 0.5f);
        st.rectTransform.anchoredPosition = new Vector2(40, 0);
        st.rectTransform.sizeDelta = new Vector2(76, 76);
        TextMeshProUGUI k = Text(st.transform, "Key", null, key.Length > 1 ? 28 : 34, new Color32(255, 248, 238, 255));
        UIFactory.Stretch(k.rectTransform);
        k.text = key;

        TextMeshProUGUI t = Text(r, "Text", locKey, 44, Cream, TextAlignmentOptions.MidlineLeft);
        UIFactory.Stretch(t.rectTransform).offsetMin = new Vector2(92, 0);
    }
}
