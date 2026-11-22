using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Công cụ chỉ chạy trong Unity Editor, KHÔNG được đóng gói vào game.
// Dùng: menu ChillPuzzle → Setup Win Check
public static class PuzzleSetupTool
{
    [MenuItem("ChillPuzzle/Setup Win Check")]
    public static void SetupWinCheck()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ChillPuzzle", "Hãy dừng game (bấm nút Play) trước khi chạy công cụ.", "OK");
            return;
        }

        // 1. Tìm GameManager và FrameInside
        GameObject gm = GameObject.Find("GameManager");
        GameObject frameInside = GameObject.Find("FrameInside");
        if (gm == null || frameInside == null)
        {
            EditorUtility.DisplayDialog("ChillPuzzle",
                "Không tìm thấy 'GameManager' hoặc 'FrameInside' trong scene. Kiểm tra lại tên trong Hierarchy.", "OK");
            return;
        }

        // 2. Canvas (dùng lại nếu đã có)
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(cgo, "Create Canvas");
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        Undo.RecordObject(scaler, "Setup Canvas Scaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 3. EventSystem dùng Input System mới
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject ego = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(ego, "Create EventSystem");
            es = ego.GetComponent<EventSystem>();
        }
        StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
        if (oldModule != null) Undo.DestroyObjectImmediate(oldModule);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);

        // 4. WinText (dùng lại nếu đã có, kể cả đang bị ẩn)
        GameObject winText = FindInScene("WinText");
        if (winText == null)
        {
            winText = new GameObject("WinText", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(winText, "Create WinText");
        }
        Undo.SetTransformParent(winText.transform, canvas.transform, "Parent WinText");

        RectTransform rt = winText.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Setup WinText");
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1000, 250);
        rt.localScale = Vector3.one;

        TextMeshProUGUI tmp = winText.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(winText);
        Undo.RecordObject(tmp, "Setup WinText");
        tmp.text = "Complete!";
        tmp.fontSize = 120;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(0x5A, 0x34, 0x14, 0xFF);   // nâu đậm
        tmp.raycastTarget = false;                        // không chặn chuột

        // 5. WinChecker trên GameManager + nối các ô
        WinChecker wc = gm.GetComponent<WinChecker>();
        if (wc == null) wc = Undo.AddComponent<WinChecker>(gm);
        Undo.RecordObject(wc, "Setup WinChecker");
        wc.frameInside = frameInside.GetComponent<SpriteRenderer>();
        wc.winText = winText;

        // 6. Lưu scene
        EditorSceneManager.MarkSceneDirty(gm.scene);
        EditorSceneManager.SaveScene(gm.scene);

        Selection.activeGameObject = gm;
        Debug.Log("[ChillPuzzle] Đã thiết lập kiểm tra thắng: Canvas, WinText, WinChecker. Scene đã được lưu.");
    }

    // GameObject.Find không tìm được object đang ẩn, nên tự duyệt
    static GameObject FindInScene(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }
}
