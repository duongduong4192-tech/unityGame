using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: ChillPuzzle → Fix All Scenes
// Sửa 1 lần cho cả 3 scene:
//   MainMenu    : xóa các mảnh số lỡ bị tạo nhầm vào menu
//   PuzzleScene : nếu mất mảnh -> dựng lại 10 mảnh số + rải ra bàn
//   Board       : tạo scene bảng tranh + xếp danh sách build
public static class RepairTool
{
    const string MENU = "Assets/Scenes/MainMenu.unity";
    const string PUZZLE = "Assets/Scenes/PuzzleScene.unity";

    [MenuItem("ChillPuzzle/Fix All Scenes")]
    public static void FixAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi chạy công cụ.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. MainMenu: bỏ mảnh thừa
        int removed = 0;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MENU) != null)
        {
            var menu = EditorSceneManager.OpenScene(MENU);
            foreach (PuzzlePiece p in Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include))
            { Object.DestroyImmediate(p.gameObject); removed++; }
            GameObject pieces = GameObject.Find("Pieces");
            if (pieces != null) Object.DestroyImmediate(pieces);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
        }

        // 2. PuzzleScene: dựng lại mảnh nếu mất
        bool rebuilt = false;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PUZZLE) != null)
        {
            EditorSceneManager.OpenScene(PUZZLE);
            if (Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include).Length == 0)
            {
                Selection.activeObject = null;          // dùng thư mục mặc định Assets/Sprites/Digits
                LevelBuilderTool.BuildLevel();           // tự rải mảnh + lưu scene
                rebuilt = true;
            }
        }

        // 3. Scene bảng tranh (tự mở lại MainMenu khi xong)
        BoardSetupTool.Setup();

        EditorUtility.DisplayDialog("ChillPuzzle",
            $"Đã sửa xong.\n- MainMenu: xóa {removed} mảnh thừa\n- PuzzleScene: {(rebuilt ? "đã dựng lại 10 mảnh số" : "mảnh vẫn còn, giữ nguyên")}\n- Đã tạo scene Board\n\nBấm Play ở MainMenu để chơi.", "OK");
    }
}
