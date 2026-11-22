using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Cài đặt người chơi, lưu bằng PlayerPrefs (tự nhớ giữa các lần mở game, không cần nút Lưu)
public static class GameSettings
{
    public static event Action Changed;

    // ===== Âm thanh (0..1)
    public static float Master { get => PlayerPrefs.GetFloat("vol_master", 0.8f); set { SetF("vol_master", value); AudioListener.volume = Master; } }
    public static float Music  { get => PlayerPrefs.GetFloat("vol_music", 0.6f);  set => SetF("vol_music", value); }
    public static float Sfx    { get => PlayerPrefs.GetFloat("vol_sfx", 0.8f);    set => SetF("vol_sfx", value); }

    // ===== Hiển thị
    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt("fullscreen", 1) == 1;
        set { SetI("fullscreen", value ? 1 : 0); ApplyDisplay(); }
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt("vsync", 1) == 1;
        set { SetI("vsync", value ? 1 : 0); ApplyFrameRate(); }
    }

    public static readonly int[] FrameLimits = { 30, 60, 120, 144, -1 };   // -1 = không giới hạn
    public static int FrameLimitIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("fps_index", 1), 0, FrameLimits.Length - 1);
        set { SetI("fps_index", ((value % FrameLimits.Length) + FrameLimits.Length) % FrameLimits.Length); ApplyFrameRate(); }
    }

    public static List<Vector2Int> Resolutions
    {
        get
        {
            List<Vector2Int> list = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Distinct()
                .Where(r => r.x >= 1024)
                .OrderBy(r => r.x * 10000 + r.y)
                .ToList();
            if (list.Count == 0) list.Add(new Vector2Int(1920, 1080));
            return list;
        }
    }

    public static int ResolutionIndex
    {
        get
        {
            List<Vector2Int> list = Resolutions;
            int saved = PlayerPrefs.GetInt("res_index", -1);
            if (saved >= 0 && saved < list.Count) return saved;
            int cur = list.FindIndex(r => r.x == Screen.width && r.y == Screen.height);
            return cur >= 0 ? cur : list.Count - 1;
        }
        set
        {
            int n = Resolutions.Count;
            SetI("res_index", ((value % n) + n) % n);
            ApplyDisplay();
        }
    }

    public static string ResolutionLabel
    {
        get { Vector2Int r = Resolutions[ResolutionIndex]; return $"{r.x} x {r.y}"; }
    }

    // ===== Lối chơi
    public static bool SnapRotate  { get => PlayerPrefs.GetInt("snap_rotate", 1) == 1;  set => SetI("snap_rotate", value ? 1 : 0); }
    public static bool ShowRing    { get => PlayerPrefs.GetInt("show_ring", 1) == 1;    set => SetI("show_ring", value ? 1 : 0); }
    public static bool OverlapWarn { get => PlayerPrefs.GetInt("overlap_warn", 1) == 1; set => SetI("overlap_warn", value ? 1 : 0); }

    // ===== Khôi phục mặc định theo nhóm
    public static void ResetAudio()    { Del("vol_master", "vol_music", "vol_sfx"); AudioListener.volume = Master; }
    public static void ResetDisplay()  { Del("fullscreen", "vsync", "fps_index", "res_index"); ApplyDisplay(); ApplyFrameRate(); }
    public static void ResetGameplay() { Del("snap_rotate", "show_ring", "overlap_warn"); }

    // ===== Áp dụng
    public static void ApplyDisplay()
    {
        Vector2Int r = Resolutions[ResolutionIndex];
        Screen.SetResolution(r.x, r.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : FrameLimits[FrameLimitIndex];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        AudioListener.volume = Master;
        ApplyFrameRate();
    }

    // ===== Hỗ trợ
    static void SetF(string k, float v) { PlayerPrefs.SetFloat(k, Mathf.Clamp01(v)); Save(); }
    static void SetI(string k, int v) { PlayerPrefs.SetInt(k, v); Save(); }
    static void Del(params string[] keys) { foreach (string k in keys) PlayerPrefs.DeleteKey(k); Save(); }
    static void Save() { PlayerPrefs.Save(); Changed?.Invoke(); }
}
