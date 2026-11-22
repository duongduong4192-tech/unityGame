using UnityEngine;

// Lưu tiến trình chơi (màn nào đã xong)
public static class Progress
{
    public static bool IsCompleted(string levelId) => PlayerPrefs.GetInt("done_" + levelId, 0) == 1;

    public static void MarkCompleted(string levelId)
    {
        PlayerPrefs.SetInt("done_" + levelId, 1);
        PlayerPrefs.Save();
    }

    public static void ResetAll(params string[] levelIds)
    {
        foreach (string id in levelIds) PlayerPrefs.DeleteKey("done_" + id);
        PlayerPrefs.Save();
    }
}
