using System;
using UnityEngine;

// Danh sách thành tựu. Mặc định tất cả đều ẩn, mở khóa khi đạt điều kiện.
// Thêm thành tựu mới: thêm 1 dòng vào All + 2 dòng chữ (tên, mô tả) trong Loc.
public static class Achievements
{
    public class Def
    {
        public string id;
        public string NameKey => "ach_" + id;
        public string DescKey => "ach_" + id + "_d";
    }

    public static readonly Def[] All =
    {
        new Def { id = "first_piece" },   // đặt mảnh đầu tiên vào khung
        new Def { id = "numbers" },       // hoàn thành màn Bộ số
        new Def { id = "no_undo" },       // hoàn thành 1 màn không dùng Hoàn tác
        new Def { id = "fast" },          // hoàn thành 1 màn dưới 3 phút
        new Def { id = "rotate_100" },    // xoay mảnh 100 lần
        new Def { id = "park" },          // hoàn thành Công viên
        new Def { id = "space" },         // hoàn thành Vũ trụ
        new Def { id = "tokyo" },         // hoàn thành Tokyo
    };

    public static event Action<Def> Unlocked;

    public static bool IsUnlocked(string id) => PlayerPrefs.GetInt("ach_" + id, 0) == 1;

    public static int UnlockedCount
    {
        get { int n = 0; foreach (Def d in All) if (IsUnlocked(d.id)) n++; return n; }
    }

    public static void Unlock(string id)
    {
        if (IsUnlocked(id)) return;
        Def def = Array.Find(All, d => d.id == id);
        if (def == null) return;
        PlayerPrefs.SetInt("ach_" + id, 1);
        PlayerPrefs.Save();
        Debug.Log($"[ChillPuzzle] Mở khóa thành tựu: {id}");
        Unlocked?.Invoke(def);
    }

    // Bộ đếm (vd. số lần xoay). Trả về giá trị mới.
    public static int AddCounter(string key, int amount = 1)
    {
        int v = PlayerPrefs.GetInt("cnt_" + key, 0) + amount;
        PlayerPrefs.SetInt("cnt_" + key, v);
        return v;
    }

    public static void ResetAll()
    {
        foreach (Def d in All) PlayerPrefs.DeleteKey("ach_" + d.id);
        PlayerPrefs.DeleteKey("cnt_rotations");
        PlayerPrefs.Save();
    }
}
