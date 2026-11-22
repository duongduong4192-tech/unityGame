using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: ChillPuzzle → Setup Board Scene
// Tạo scene "Board" (bảng ghim chọn tranh), chỉnh ảnh, xếp danh sách build: MainMenu -> Board -> PuzzleScene.
public static class BoardSetupTool
{
    const string MENU_PATH = "Assets/Scenes/MainMenu.unity";
    const string BOARD_PATH = "Assets/Scenes/Board.unity";
    const string PUZZLE_PATH = "Assets/Scenes/PuzzleScene.unity";
    const string BOARD_UI = "Assets/Resources/UI/board";
    const string PICTURES = "Assets/Resources/UI/pictures";

    [MenuItem("ChillPuzzle/Setup Board Scene")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi chạy công cụ.", "OK");
            return;
        }

        // 1. Ảnh: viền 9 mảnh cho giấy / khung / quầng sáng (trái, dưới, phải, trên)
        Dictionary<string, Vector4> borders = new Dictionary<string, Vector4>
        {
            { "polaroid", new Vector4(58, 145, 58, 58) },
            { "frame_small", new Vector4(26, 23, 26, 26) },
            { "glow", new Vector4(96, 96, 96, 96) },
        };
        foreach (var kv in borders)
        {
            TextureImporter ti = AssetImporter.GetAtPath($"{BOARD_UI}/{kv.Key}.png") as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[ChillPuzzle] Thiếu ảnh {BOARD_UI}/{kv.Key}.png"); continue; }
            ti.spriteBorder = kv.Value;
            ti.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(PICTURES, ImportAssetOptions.ImportRecursive);

        // 2. Scene Board
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(BOARD_PATH) ||
            EditorUtility.DisplayDialog("ChillPuzzle", "Scene Board đã có. Tạo lại từ đầu?", "Tạo lại", "Giữ nguyên"))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            Camera cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(150, 104, 66, 255);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            GameObject cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;

            new GameObject("Board").AddComponent<BoardScreen>();
            EditorSceneManager.SaveScene(scene, BOARD_PATH);
        }

        // 3. Danh sách build
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        foreach (string p in new[] { MENU_PATH, BOARD_PATH, PUZZLE_PATH })
            if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path != MENU_PATH && s.path != BOARD_PATH && s.path != PUZZLE_PATH) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        // mở lại menu để bấm Play từ đầu
        if (File.Exists(MENU_PATH)) EditorSceneManager.OpenScene(MENU_PATH);
        Debug.Log("[ChillPuzzle] Đã tạo scene Board. Mở MainMenu và bấm Play, rồi bấm vào khung tranh trên bàn.");
    }
}
