using System.Collections.Generic;
using UnityEngine;

// Âm thanh của game. Gọi Sfx.Play("put") ở bất kỳ đâu, không cần đặt gì trong scene.
// File nằm ở Resources/Audio/sfx/{tên}_{số}.ogg — mỗi tên có vài bản, phát ngẫu nhiên + lệch cao độ nhẹ cho đỡ lặp.
// Nhạc nền: Resources/Audio/music/bgm.ogg, chạy liên tục qua mọi scene.
// Âm lượng theo Cài đặt (Tổng / Nhạc / Hiệu ứng).
public class Sfx : MonoBehaviour
{
    // âm lượng gốc từng loại (0..1) — chỉnh ở đây nếu nghe to/nhỏ quá
    static readonly Dictionary<string, float> Volume = new Dictionary<string, float>
    {
        { "pick", 0.35f }, { "put", 0.6f }, { "land", 0.3f }, { "snap", 0.35f }, { "rotate", 0.45f },
        { "win", 0.55f }, { "click", 0.45f }, { "open", 0.4f }, { "close", 0.4f }, { "undo", 0.45f },
    };
    const float MUSIC_VOLUME = 0.45f;

    static Sfx inst;
    readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    AudioSource[] pool;
    int next;
    AudioSource music;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject("[Sfx]");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<Sfx>();
    }

    void Awake()
    {
        pool = new AudioSource[12];
        for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }

        music = gameObject.AddComponent<AudioSource>();
        music.clip = Resources.Load<AudioClip>("Audio/music/bgm");
        music.loop = true; music.playOnAwake = false;
        AudioListener.volume = GameSettings.Master;
        ApplyMusic();
        if (music.clip != null) music.Play();
        GameSettings.Changed += ApplyMusic;
    }

    void OnDestroy() => GameSettings.Changed -= ApplyMusic;

    void ApplyMusic() { if (music != null) music.volume = MUSIC_VOLUME * GameSettings.Music; }

    AudioClip[] Clips(string name)
    {
        if (clips.TryGetValue(name, out AudioClip[] c)) return c;
        string file = name == "land" ? "put" : name;            // tiếng rơi xuống bàn dùng lại tiếng đặt gỗ (cao hơn, nhỏ hơn)
        List<AudioClip> list = new List<AudioClip>();
        for (int i = 0; i < 10; i++)
        {
            AudioClip a = Resources.Load<AudioClip>($"Audio/sfx/{file}_{i}");
            if (a == null) break;
            list.Add(a);
        }
        return clips[name] = list.ToArray();
    }

    public static void Play(string name, float volume = 1f)
    {
        if (inst == null) return;
        AudioClip[] c = inst.Clips(name);
        if (c.Length == 0) return;

        // chống dồn tiếng khi nhiều mảnh cùng kêu một lúc
        float now = Time.unscaledTime;
        if (inst.lastPlayed.TryGetValue(name, out float t) && now - t < 0.03f) return;
        inst.lastPlayed[name] = now;

        AudioSource s = inst.pool[inst.next];
        inst.next = (inst.next + 1) % inst.pool.Length;
        s.clip = c[Random.Range(0, c.Length)];
        float pitch = Random.Range(0.94f, 1.06f);
        if (name == "land") pitch *= 1.15f;
        s.pitch = pitch;
        s.volume = (Volume.TryGetValue(name, out float v) ? v : 0.5f) * volume * GameSettings.Sfx;
        s.Play();
    }
}
