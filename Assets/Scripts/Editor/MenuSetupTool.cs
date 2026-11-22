using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: ChillPuzzle → Setup Main Menu
// Tạo scene MainMenu, chỉnh hình nút/nền trong Resources/UI, thêm 2 scene vào danh sách build.
public static class MenuSetupTool
{
    const string MENU_PATH = "Assets/Scenes/MainMenu.unity";
    const string PUZZLE_PATH = "Assets/Scenes/PuzzleScene.unity";
    const string UI_FOLDER = "Assets/Resources/UI";
    const string DIGITS_FOLDER = "Assets/Sprites/Digits";

    [MenuItem("ChillPuzzle/Setup Main Menu")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi chạy công cụ.", "OK");
            return;
        }

        ConfigureUiSprites();

        if (File.Exists(MENU_PATH) &&
            !EditorUtility.DisplayDialog("ChillPuzzle", "Scene MainMenu đã có. Tạo lại từ đầu?", "Tạo lại", "Hủy"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        Camera cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(200, 160, 118, 255);

        // EventSystem (Input System mới)
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Canvas
        GameObject cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        // Menu
        new GameObject("MainMenu").AddComponent<MainMenu>();

        EditorSceneManager.SaveScene(scene, MENU_PATH);

        // Danh sách scene khi đóng gói: MainMenu chạy đầu tiên
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MENU_PATH, true),
        };
        if (File.Exists(PUZZLE_PATH)) list.Add(new EditorBuildSettingsScene(PUZZLE_PATH, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path != MENU_PATH && s.path != PUZZLE_PATH) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("[ChillPuzzle] Đã tạo scene MainMenu và thêm vào danh sách build. Bấm Play để xem menu.");
    }

    // Hình nút/khung dạng "9 mảnh" để co giãn không méo góc bo tròn
    static void ConfigureUiSprites()
    {
        Dictionary<string, Vector4> borders = new Dictionary<string, Vector4>
        {
            { "btn", new Vector4(42, 42, 42, 42) },
            { "panel", new Vector4(46, 46, 46, 46) },
            { "bar", new Vector4(16, 14, 16, 14) },
            { "knob", Vector4.zero },
            { "wood_bg", Vector4.zero },
            { "arrow", Vector4.zero },
        };
        foreach (var kv in borders)
        {
            string path = $"{UI_FOLDER}/{kv.Key}.png";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[ChillPuzzle] Thiếu ảnh {path}"); continue; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spriteBorder = kv.Value;
            ti.textureCompression = kv.Key == "wood_bg" ? TextureImporterCompression.CompressedHQ
                                                        : TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        // Hình kiểu sổ tay vẽ tay (Cài đặt, Thành tựu)
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { UI_FOLDER + "/hand" }))
        {
            TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        // Ảnh căn phòng menu + các lớp sáng của đồ vật bấm được
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { UI_FOLDER + "/menu" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool isRoom = path.EndsWith("room_bg.png");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.textureCompression = isRoom ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            ti.isReadable = !isRoom;   // để chỉ bấm được đúng hình đồ vật (alpha hit test)
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
    }

    class NaturalComparer : IComparer<string>
    {
        public int Compare(string a, string b) => EditorUtility.NaturalCompare(a, b);
    }
}
