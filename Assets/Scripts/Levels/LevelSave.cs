using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Lưu màn đang chơi dở: vị trí + góc xoay của từng mảnh, mỗi màn một bản lưu riêng.
// - Tự lưu sau mỗi lần thả / xoay / hoàn tác, khi rời màn và khi tắt game.
// - Vào lại màn: các mảnh nằm đúng chỗ cũ (không chạy hiệu ứng rơi).
// - Bấm "Chơi lại" hoặc hoàn thành màn thì xoá bản lưu của màn đó.
// LevelLoader tự tạo component này khi PuzzleScene vừa nạp.
public class LevelSave : MonoBehaviour
{
    static string Key => "save_" + (!string.IsNullOrEmpty(GameFlow.LevelId) ? GameFlow.LevelId : "numbers");
    public static bool HasSave => PlayerPrefs.HasKey(Key);

    PieceController controller;
    WinChecker winChecker;
    bool dirty, done;

    IEnumerator Start()
    {
        controller = FindAnyObjectByType<PieceController>();
        winChecker = FindAnyObjectByType<WinChecker>();
        if (winChecker != null) winChecker.Won += OnWon;
        PieceController.PlayerAction += OnAction;

        yield return null;                 // để PieceController ghi lại vị trí rải ban đầu (dùng cho "Chơi lại") trước
        Load();
    }

    void OnDestroy()
    {
        PieceController.PlayerAction -= OnAction;
        Save();
    }

    void OnApplicationQuit() => Save();

    void OnAction(string a)
    {
        if (done) return;
        if (a == "drop" || a == "rotate" || a == "scroll" || a == "undo")
        {
            dirty = true;
            CancelInvoke(nameof(Save));
            Invoke(nameof(Save), 0.45f);    // đợi mảnh trượt / xoay xong rồi mới ghi
        }
    }

    void OnWon()
    {
        done = true;
        Clear();
    }

    // GameHUD gọi khi bấm "Chơi lại"
    public static void Restarted()
    {
        Clear();
        LevelSave s = FindAnyObjectByType<LevelSave>();
        if (s != null) { s.dirty = false; s.done = false; s.CancelInvoke(nameof(Save)); }
    }

    static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

    static List<PuzzlePiece> Pieces()
    {
        List<PuzzlePiece> list = new List<PuzzlePiece>(FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Exclude));
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    void Save()
    {
        if (!dirty || done || (winChecker != null && winChecker.IsWon)) return;
        List<PuzzlePiece> list = Pieces();
        if (list.Count == 0) return;
        StringBuilder sb = new StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;
        foreach (PuzzlePiece p in list)
        {
            Vector3 pos = p.transform.position;
            sb.Append(p.name).Append('|')
              .Append(pos.x.ToString("0.####", ci)).Append('|')
              .Append(pos.y.ToString("0.####", ci)).Append('|')
              .Append(p.transform.eulerAngles.z.ToString("0.##", ci)).Append('\n');
        }
        PlayerPrefs.SetString(Key, sb.ToString());
        PlayerPrefs.Save();
    }

    void Load()
    {
        string data = PlayerPrefs.GetString(Key, null);
        if (string.IsNullOrEmpty(data)) return;
        List<PuzzlePiece> list = Pieces();
        string[] lines = data.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != list.Count) { Clear(); return; }      // màn đã đổi -> bản lưu cũ không dùng được

        CultureInfo ci = CultureInfo.InvariantCulture;
        for (int i = 0; i < lines.Length; i++)
        {
            string[] f = lines[i].Split('|');
            if (f.Length != 4 || f[0] != list[i].name) { Clear(); return; }
        }
        for (int i = 0; i < lines.Length; i++)
        {
            string[] f = lines[i].Split('|');
            Transform t = list[i].transform;
            t.SetPositionAndRotation(new Vector3(float.Parse(f[1], ci), float.Parse(f[2], ci), t.position.z),
                                     Quaternion.Euler(0, 0, float.Parse(f[3], ci)));
        }
        Physics2D.SyncTransforms();
        dirty = true;      // vẫn đang dở, rời màn thì lưu tiếp
    }
}
