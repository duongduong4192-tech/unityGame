using TMPro;
using UnityEngine;

// Font có dấu tiếng Việt (Baloo 2), nạp từ Assets/Resources/Fonts khi game chạy
public static class UIFont
{
    static TMP_FontAsset bold, medium, hand;

    public static TMP_FontAsset Bold => bold != null ? bold : (bold = Make("Fonts/Baloo2-Bold"));
    public static TMP_FontAsset Medium => medium != null ? medium : (medium = Make("Fonts/Baloo2-Medium"));
    // chữ viết tay (Patrick Hand) cho giao diện kiểu sổ tay
    public static TMP_FontAsset Hand => hand != null ? hand : (hand = Make("Fonts/PatrickHand-Regular"));

    static TMP_FontAsset Make(string path)
    {
        Font f = Resources.Load<Font>(path);
        if (f == null)
        {
            Debug.LogWarning($"[ChillPuzzle] Không tìm thấy font Resources/{path}. Dùng font mặc định (không có dấu).");
            return TMP_Settings.defaultFontAsset;
        }
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(f);   // font động: tự thêm chữ khi cần
        fa.name = f.name + " (runtime)";
        return fa;
    }
}
