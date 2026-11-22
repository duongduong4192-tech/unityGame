using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dữ liệu một bức tranh lớn (vd. Công viên): ảnh xám, ảnh màu, các nét cọ lộ màu cho từng loại sự vật.
// Nằm trong Assets/Resources/UI/pictures/<id>/ : picture.json, gray.png, color.png, strokes/*.png
[Serializable] public class StrokeData { public string file; public int x0, y0, x1, y1; }
[Serializable] public class CategoryData { public string id; public int[] thumb; public StrokeData[] strokes; }

[Serializable]
public class PictureData
{
    public string id;
    public int w, h;
    public CategoryData[] categories;

    public const string ROOT = "UI/pictures/";

    public static PictureData Load(string id)
    {
        TextAsset ta = Resources.Load<TextAsset>(ROOT + id + "/picture");
        return ta == null ? null : JsonUtility.FromJson<PictureData>(ta.text);
    }

    public float Aspect => (float)w / h;
    public Sprite Gray => Resources.Load<Sprite>(ROOT + id + "/gray");
    public Sprite Color => Resources.Load<Sprite>(ROOT + id + "/color");
    public Sprite Stroke(string file) => Resources.Load<Sprite>(ROOT + id + "/strokes/" + file);
    public string NameKey => "pic_" + id;

    // ---- tiến trình riêng của tranh này
    public bool IsRevealed(string cat) => PlayerPrefs.GetInt($"rev_{id}_{cat}", 0) == 1;
    public void MarkRevealed(string cat) { PlayerPrefs.SetInt($"rev_{id}_{cat}", 1); PlayerPrefs.Save(); }
    public bool IsComplete => PlayerPrefs.GetInt("pic_" + id, 0) == 1;
    public void MarkComplete() { PlayerPrefs.SetInt("pic_" + id, 1); PlayerPrefs.Save(); }

    public int RevealedCount
    {
        get { int n = 0; foreach (CategoryData c in categories) if (IsRevealed(c.id)) n++; return n; }
    }
    public bool AllRevealed => RevealedCount >= categories.Length;
}

// Danh sách tranh trên bảng ghim (tranh chưa có dữ liệu sẽ hiện "Sắp ra mắt")
public static class Pictures
{
    public static readonly string[] All = { "park", "space", "tokyo" };
}

// Chuyển cảnh giữa Menu (bảng chọn màn) <-> Màn xếp hình
public static class GameFlow
{
    public const string MENU_SCENE = "MainMenu", BOARD_SCENE = "Board", PUZZLE_SCENE = "PuzzleScene";

    public static string LevelCat, LevelId;  // màn tạo sẵn đang chơi (null = màn Số hướng dẫn trong scene)
    public static string ReopenCat;          // về menu thì mở lại bảng chọn màn ở thể loại này

    // (giữ lại cho bảng tranh cũ)
    public static string Picture, Category, JustSolved;

    public static void PlayLevel(string cat, string id)
    {
        LevelCat = cat; LevelId = id; Category = null; Picture = null; JustSolved = null;
        SceneManager.LoadScene(PUZZLE_SCENE);
    }

    public static void PlayTutorial()
    {
        LevelCat = "blocks"; LevelId = null; Category = "numbers"; Picture = null;
        SceneManager.LoadScene(PUZZLE_SCENE);
    }

    public static bool HasNext => !string.IsNullOrEmpty(LevelId) && LevelCatalog.Get().Next(LevelCat, LevelId) != null;

    public static void PlayNext()
    {
        string next = string.IsNullOrEmpty(LevelId) ? null : LevelCatalog.Get().Next(LevelCat, LevelId);
        if (next != null) PlayLevel(LevelCat, next); else BackToMenu();
    }

    // về phòng và mở lại bảng chọn màn
    public static void BackToMenu()
    {
        ReopenCat = string.IsNullOrEmpty(LevelCat) ? "blocks" : LevelCat;
        LevelId = null; Category = null;
        if (Application.CanStreamedLevelBeLoaded(MENU_SCENE)) SceneManager.LoadScene(MENU_SCENE);
        else Debug.LogWarning("[ChillPuzzle] Chưa có scene MainMenu trong danh sách build.");
    }

    public static void BackToBoard() => BackToMenu();

    public static void PlayCategory(string picture, string category)
    {
        Picture = picture; Category = category; JustSolved = null; LevelId = null;
        SceneManager.LoadScene(PUZZLE_SCENE);
    }

    public static void ToMenu()
    {
        Picture = null; Category = null; JustSolved = null; LevelId = null; ReopenCat = null;
        if (Application.CanStreamedLevelBeLoaded(MENU_SCENE)) SceneManager.LoadScene(MENU_SCENE);
        else Debug.LogWarning("[ChillPuzzle] Chưa có scene MainMenu trong danh sách build.");
    }
}
