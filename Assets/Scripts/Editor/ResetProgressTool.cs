using UnityEditor;
using UnityEngine;

// Menu: ChillPuzzle → Reset Progress (test)
// Xóa tiến trình chơi để thử lại từ đầu (hướng dẫn, bảng tranh, thành tựu). Giữ nguyên cài đặt âm thanh / ngôn ngữ.
public static class ResetProgressTool
{
    [MenuItem("ChillPuzzle/Reset Progress (test)")]
    public static void ResetProgress()
    {
        if (!EditorUtility.DisplayDialog("ChillPuzzle", "Xóa toàn bộ tiến trình chơi (màn đã xong, tranh đã tô, thành tựu)?", "Xóa", "Hủy"))
            return;
        foreach (string pic in Pictures.All)
        {
            PlayerPrefs.DeleteKey("pic_" + pic);
            PictureData d = PictureData.Load(pic);
            if (d == null) continue;
            foreach (CategoryData c in d.categories)
            {
                PlayerPrefs.DeleteKey("done_" + c.id);
                PlayerPrefs.DeleteKey($"rev_{pic}_{c.id}");
            }
        }
        PlayerPrefs.DeleteKey("done_numbers");
        Achievements.ResetAll();
        PlayerPrefs.Save();
        Debug.Log("[ChillPuzzle] Đã xóa tiến trình. Bấm Play ở MainMenu để chơi lại từ hướng dẫn.");
    }
}
