using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Các hàm dựng giao diện bằng code (nút gỗ, khung, chữ, thanh trượt...)
public static class UIFactory
{
    public static readonly Color Brown = new Color32(90, 58, 32, 255);
    public static readonly Color Cream = new Color32(250, 242, 226, 255);
    public static readonly Color BtnColor = new Color32(240, 190, 120, 255);
    public static readonly Color BtnMuted = new Color32(205, 190, 170, 255);
    public static readonly Color Green = new Color32(150, 196, 120, 255);

    public static Sprite Sp(string name) => Resources.Load<Sprite>("UI/" + name);

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Đặt theo tâm màn hình (1920x1080), pos tính bằng pixel
    public static RectTransform Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Img(Transform parent, string name, Sprite sp, Color color)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sp;
        img.color = color;
        if (sp != null && sp.border != Vector4.zero) img.type = Image.Type.Sliced;
        return img;
    }

    // Khối màu trơn (không ảnh)
    public static Image Solid(Transform parent, string name, Color color, bool raycast = false)
    {
        Image img = Img(parent, name, null, color);
        img.raycastTarget = raycast;
        return img;
    }

    // Đặt theo tỉ lệ khung cha: (0,0) = góc dưới-trái, (1,1) = góc trên-phải
    public static RectTransform Anchor(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string locKey, float size, Color color,
                                       bool bold = true, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = Rect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = bold ? UIFont.Bold : UIFont.Medium;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (!string.IsNullOrEmpty(locKey)) rt.gameObject.AddComponent<LocText>().SetKey(locKey);
        return t;
    }

    public static Button Button(Transform parent, string locKey, Vector2 pos, Vector2 size, Color color,
                                UnityAction onClick, float fontSize = 40)
    {
        Image img = Img(parent, "Btn_" + locKey, Sp("btn"), color);
        Place(img.rectTransform, pos, size);
        Button b = img.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        if (onClick != null) b.onClick.AddListener(onClick);
        img.gameObject.AddComponent<ButtonBounce>();

        TextMeshProUGUI t = Text(img.transform, "Label", locKey, fontSize, Brown);
        Stretch(t.rectTransform).offsetMin = new Vector2(10, 6);
        t.rectTransform.offsetMax = new Vector2(-10, 0);
        return b;
    }

    public static Slider Slider(Transform parent, Vector2 pos, Vector2 size, float value, UnityAction<float> onChange)
    {
        RectTransform root = Place(Rect("Slider", parent), pos, size);
        Slider s = root.gameObject.AddComponent<Slider>();

        Image bg = Img(root, "Background", Sp("bar"), new Color32(200, 175, 145, 255));
        Stretch(bg.rectTransform);

        RectTransform fillArea = Stretch(Rect("Fill Area", root));
        fillArea.offsetMin = new Vector2(6, 6); fillArea.offsetMax = new Vector2(-6, -6);
        Image fill = Img(fillArea, "Fill", Sp("bar"), Green);
        fill.rectTransform.sizeDelta = Vector2.zero;

        RectTransform handleArea = Stretch(Rect("Handle Slide Area", root));
        handleArea.offsetMin = new Vector2(22, 0); handleArea.offsetMax = new Vector2(-22, 0);
        Image knob = Img(handleArea, "Handle", Sp("knob"), Color.white);
        knob.rectTransform.sizeDelta = new Vector2(size.y + 16, 16);

        s.fillRect = fill.rectTransform;
        s.handleRect = knob.rectTransform;
        s.targetGraphic = knob;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = 0; s.maxValue = 1;
        s.value = value;
        if (onChange != null) s.onValueChanged.AddListener(onChange);
        return s;
    }

    // Ô chọn kiểu  <  giá trị  >
    public static TextMeshProUGUI Stepper(Transform parent, Vector2 pos, float width, UnityAction onPrev, UnityAction onNext)
    {
        RectTransform root = Place(Rect("Stepper", parent), pos, new Vector2(width, 70));
        Button prev = Button(root, "", new Vector2(-width / 2 + 35, 0), new Vector2(70, 70), BtnColor, onPrev, 40);
        prev.GetComponentInChildren<TextMeshProUGUI>().text = "<";
        Button next = Button(root, "", new Vector2(width / 2 - 35, 0), new Vector2(70, 70), BtnColor, onNext, 40);
        next.GetComponentInChildren<TextMeshProUGUI>().text = ">";
        TextMeshProUGUI val = Text(root, "Value", null, 36, Brown);
        Place(val.rectTransform, Vector2.zero, new Vector2(width - 160, 70));
        return val;
    }
}

