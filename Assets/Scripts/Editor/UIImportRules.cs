using UnityEditor;
using UnityEngine;

// Luật nhập ảnh tự động cho mọi ảnh giao diện trong Assets/Resources/UI.
// Unity hay tự nhận ảnh có nhiều mảng rời (vd. chữ có chấm sơn) là "nhiều sprite" và cắt nhỏ ra,
// khiến game lấy nhầm 1 mảnh. Luật này buộc mỗi file là MỘT sprite.
public class UIImportRules : AssetPostprocessor
{
    // Tăng số này mỗi khi đổi luật -> Unity tự nhập lại các ảnh/âm thanh theo luật mới
    public override uint GetVersion() => 4;

    // Âm thanh: tiếng ngắn nạp sẵn vào bộ nhớ (phát tức thì), nhạc nền đọc dần từ ổ đĩa (nhẹ bộ nhớ)
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        AudioImporter ai = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/music/");
        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = music ? 0.6f : 0.8f;
        ai.defaultSampleSettings = s;
        ai.forceToMono = !music;
        ai.loadInBackground = music;
    }

    void OnPreprocessTexture()
    {
        TextureImporter ti = (TextureImporter)assetImporter;

        // Ảnh màn chơi tạo sẵn (atlas): giữ nguyên kích thước (không làm tròn lũy thừa 2) vì code cắt theo tọa độ điểm ảnh
        if (assetPath.StartsWith("Assets/Resources/Levels/"))
        {
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;      // Unity chỉ nén được ảnh không phải lũy thừa 2 khi TẮT mipmap
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            // nén Crunch: bản build nhẹ hơn nhiều (vân gỗ vẫn đẹp ở chất lượng 80)
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 80;
            ti.isReadable = false;
            return;
        }

        if (!assetPath.StartsWith("Assets/Resources/UI/")) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;

        bool hotspot = assetPath.Contains("/menu/hs_");
        bool unused = assetPath.Contains("/pictures/") || assetPath.Contains("/board/") && !assetPath.EndsWith("check.png");
        ti.isReadable = hotspot;   // vùng bấm theo đúng hình đồ vật cần đọc được điểm ảnh
        ti.crunchedCompression = false;
        if (hotspot) ti.textureCompression = TextureImporterCompression.Uncompressed;     // vùng bấm cần đọc điểm ảnh
        else if (unused) { ti.textureCompression = TextureImporterCompression.Compressed; ti.crunchedCompression = true; ti.compressionQuality = 60; }
        else ti.textureCompression = TextureImporterCompression.CompressedHQ;             // BC7: gần như không mất chất lượng, nhẹ 4 lần

        TextureImporterSettings st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteGenerateFallbackPhysicsShape = false;
        ti.SetTextureSettings(st);
    }
}
