using System;
using System.Collections.Generic;
using UnityEngine;

// Song ngữ Việt / Anh. Thêm chữ mới: thêm 1 dòng vào bảng T bên dưới.
public static class Loc
{
    public enum Lang { VI = 0, EN = 1 }

    public static event Action Changed;

    static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
    {
        // ---- menu phòng
        { "title",        new[] { "Chill Puzzle", "Chill Puzzle" } },
        { "subtitle",     new[] { "Xếp hình thư giãn", "A cozy puzzle game" } },
        { "hs_puzzle",    new[] { "Chơi", "Play" } },
        { "hs_book",      new[] { "Cài đặt", "Settings" } },
        { "hs_gallery",   new[] { "Thành tựu", "Achievements" } },

        // ---- cài đặt
        { "settings",     new[] { "Cài đặt", "Settings" } },
        { "tab_audio",    new[] { "Âm thanh", "Audio" } },
        { "tab_display",  new[] { "Hiển thị", "Display" } },
        { "tab_game",     new[] { "Lối chơi", "Gameplay" } },
        { "tab_about",    new[] { "Thông tin", "About" } },
        { "sec_volume",   new[] { "Âm lượng", "Volume" } },
        { "sec_screen",   new[] { "Màn hình", "Screen" } },
        { "sec_play",     new[] { "Trải nghiệm", "Experience" } },

        { "master",       new[] { "Âm lượng chung", "Master volume" } },
        { "master_d",     new[] { "Âm lượng của toàn bộ trò chơi", "Overall volume of the game" } },
        { "music",        new[] { "Nhạc nền", "Music" } },
        { "music_d",      new[] { "Âm lượng nhạc nền thư giãn", "Volume of the background music" } },
        { "sfx",          new[] { "Hiệu ứng âm thanh", "Sound effects" } },
        { "sfx_d",        new[] { "Tiếng gỗ khi nhấc, đặt và xoay mảnh", "Wooden sounds when picking, placing and rotating pieces" } },

        { "display_mode",   new[] { "Chế độ hiển thị", "Display mode" } },
        { "display_mode_d", new[] { "Chơi toàn màn hình hoặc trong cửa sổ", "Play fullscreen or in a window" } },
        { "fullscreen",     new[] { "Toàn màn hình", "Fullscreen" } },
        { "windowed",       new[] { "Cửa sổ", "Windowed" } },
        { "resolution",     new[] { "Độ phân giải", "Resolution" } },
        { "resolution_d",   new[] { "Kích thước hình ảnh của trò chơi", "Size of the game image" } },
        { "vsync",          new[] { "Đồng bộ dọc (VSync)", "Vertical sync (VSync)" } },
        { "vsync_d",        new[] { "Chống xé hình, khớp tốc độ khung hình với màn hình", "Prevents screen tearing by matching your monitor" } },
        { "fps",            new[] { "Giới hạn khung hình", "Frame rate limit" } },
        { "fps_d",          new[] { "Chỉ có tác dụng khi tắt VSync", "Only used when VSync is off" } },
        { "unlimited",      new[] { "Không giới hạn", "Unlimited" } },

        { "language",       new[] { "Ngôn ngữ", "Language" } },
        { "language_d",     new[] { "Ngôn ngữ hiển thị trong trò chơi", "Language used in the game" } },
        { "lang_name",      new[] { "Tiếng Việt", "English" } },
        { "snap_rotate",    new[] { "Hít góc khi xoay", "Snap rotation" } },
        { "snap_rotate_d",  new[] { "Mảnh tự khựng lại ở các góc 90° khi xoay gần đến", "Pieces lock to 90° angles when you rotate close to them" } },
        { "show_ring",      new[] { "Vòng chỉ góc", "Rotation ring" } },
        { "show_ring_d",    new[] { "Hiện vòng tròn chỉ góc khi xoay mảnh", "Show an angle ring while rotating a piece" } },
        { "overlap_warn",   new[] { "Báo đỏ khi chồng mảnh", "Overlap warning" } },
        { "overlap_warn_d", new[] { "Mảnh ửng đỏ khi chỗ đang kéo tới sẽ đè lên mảnh khác", "A piece turns red when it would overlap another" } },
        { "on",             new[] { "Bật", "On" } },
        { "off",            new[] { "Tắt", "Off" } },

        { "back",           new[] { "Quay lại", "Back" } },
        { "reset_default",  new[] { "Khôi phục mặc định", "Reset to default" } },
        { "quit_game",      new[] { "Thoát game", "Quit game" } },
        { "switch_tab",     new[] { "Đổi mục", "Switch tab" } },

        // ---- thông tin game
        { "version",        new[] { "Phiên bản", "Version" } },
        { "developer",      new[] { "Phát triển & phát hành", "Developed & published by" } },
        { "font_credit",    new[] { "Font chữ: Baloo 2 – Ek Type (SIL Open Font License 1.1)",
                                    "Font: Baloo 2 – Ek Type (SIL Open Font License 1.1)" } },
        { "engine",         new[] { "Làm bằng Unity", "Made with Unity" } },
        { "thanks",         new[] { "Cảm ơn bạn đã chơi!", "Thank you for playing!" } },

        // ---- thành tựu
        { "achievements",   new[] { "Thành tựu", "Achievements" } },
        { "ach_hidden",     new[] { "Thành tựu ẩn", "Hidden achievement" } },
        { "ach_first_piece",   new[] { "Mảnh đầu tiên", "First piece" } },
        { "ach_first_piece_d", new[] { "Đặt mảnh ghép đầu tiên vào khung", "Place your first piece in the frame" } },
        { "ach_numbers",       new[] { "Khởi đầu", "A fresh start" } },
        { "ach_numbers_d",     new[] { "Hoàn thành màn Bộ số", "Complete the Numbers puzzle" } },
        { "ach_no_undo",       new[] { "Không hối tiếc", "No regrets" } },
        { "ach_no_undo_d",     new[] { "Hoàn thành một màn mà không dùng Hoàn tác", "Finish a puzzle without using Undo" } },
        { "ach_fast",          new[] { "Nhanh tay", "Quick hands" } },
        { "ach_fast_d",        new[] { "Hoàn thành một màn dưới 3 phút", "Finish a puzzle in under 3 minutes" } },
        { "ach_rotate_100",    new[] { "Xoay xoay", "Round and round" } },
        { "ach_rotate_100_d",  new[] { "Xoay mảnh ghép 100 lần", "Rotate pieces 100 times" } },
        { "ach_park",          new[] { "Dạo công viên", "A walk in the park" } },
        { "ach_park_d",        new[] { "Hoàn thành bức tranh Công viên", "Complete the Park picture" } },
        { "ach_space",         new[] { "Giữa các vì sao", "Among the stars" } },
        { "ach_space_d",       new[] { "Hoàn thành bức tranh Vũ trụ", "Complete the Space picture" } },
        { "ach_tokyo",         new[] { "Mùa hoa anh đào", "Cherry blossom season" } },
        { "ach_tokyo_d",       new[] { "Hoàn thành bức tranh Tokyo", "Complete the Tokyo picture" } },

        // ---- trong màn chơi
        { "complete",       new[] { "Hoàn thành!", "Complete!" } },
        { "undo",           new[] { "Hoàn tác (Ctrl+Z)", "Undo (Ctrl+Z)" } },
        { "restart",        new[] { "Chơi lại", "Restart" } },
        { "menu",           new[] { "Menu", "Menu" } },
        { "back_room",      new[] { "Về phòng", "Back to room" } },
        { "back_picture",   new[] { "Về tranh", "Back to picture" } },
        { "next_level",     new[] { "Màn tiếp", "Next level" } },
        { "choose_level",   new[] { "Chọn màn", "Levels" } },
        { "cat_geo",        new[] { "Hình học", "Geometry" } },
        { "cat_blocks",     new[] { "Ghép khối", "Blocks" } },
        { "cat_curve",      new[] { "Đường cong", "Curves" } },
        { "cat_circle",     new[] { "Hình tròn", "Circles" } },
        { "tutorial_card",  new[] { "Hướng dẫn", "Tutorial" } },
        { "pieces_word",    new[] { "mảnh", "pcs" } },

        // ---- hướng dẫn (màn Số)
        { "tut_drag",    new[] { "Nhấn giữ chuột trái vào một mảnh số rồi kéo đi chỗ khác", "Hold the left mouse button on a number piece and drag it" } },
        { "tut_rotate",  new[] { "Giữ chuột phải lên mảnh rồi kéo vòng quanh để xoay. Mảnh tự hít vào góc vuông", "Hold the right mouse button on a piece and drag around to rotate. It snaps to right angles" } },
        { "tut_scroll",  new[] { "Lăn chuột khi trỏ vào mảnh để xoay nhanh 90°", "Scroll the mouse wheel over a piece to turn it 90°" } },
        { "tut_overlap", new[] { "Các mảnh không chồng lên nhau. Mảnh hiện đỏ là đang đè mảnh khác; thả ra nó sẽ tự trượt sang chỗ trống gần nhất", "Pieces can't overlap. A red piece is on top of another; let go and it slides to the nearest free spot" } },
        { "tut_undo",    new[] { "Lỡ tay? Bấm Hoàn tác hoặc Ctrl+Z để quay lại bước trước", "Oops? Press Undo or Ctrl+Z to go back one step" } },
        { "tut_goal",    new[] { "Xếp cả 10 mảnh số vào TRONG khung gỗ là hoàn thành. Có nhiều cách xếp đúng!", "Fit all 10 number pieces INSIDE the wooden frame to finish. There are many right answers!" } },
        { "tut_good",    new[] { "Tốt lắm!", "Nice!" } },
        { "tut_next",    new[] { "Tiếp", "Next" } },
        { "tut_ok",      new[] { "Đã hiểu", "Got it" } },
        { "tut_skip",    new[] { "Bỏ qua hướng dẫn", "Skip tutorial" } },

        // ---- bảng tranh
        { "board",          new[] { "Bảng tranh", "Picture board" } },
        { "coming_soon",    new[] { "Sắp ra mắt", "Coming soon" } },
        { "pic_done",       new[] { "Bức tranh hoàn thành!", "Picture complete!" } },
        { "pic_park",       new[] { "Công viên", "Park" } },
        { "pic_space",      new[] { "Vũ trụ", "Space" } },
        { "pic_tokyo",      new[] { "Tokyo", "Tokyo" } },
        { "cat_numbers",    new[] { "Số", "Numbers" } },
        { "cat_tree",       new[] { "Cây", "Trees" } },
        { "cat_flower",     new[] { "Hoa", "Flowers" } },
        { "cat_bird",       new[] { "Chim", "Birds" } },
        { "cat_animal",     new[] { "Động vật", "Animals" } },
        { "cat_people",     new[] { "Người", "People" } },
        { "cat_fish",       new[] { "Cá", "Fish" } },
        { "cat_object",     new[] { "Đồ vật", "Objects" } },
        { "cat_building",   new[] { "Tòa nhà", "Buildings" } },
    };

    public static Lang Current
    {
        get
        {
            int def = Application.systemLanguage == SystemLanguage.Vietnamese ? 0 : 1;
            return (Lang)PlayerPrefs.GetInt("lang", def);
        }
        set
        {
            PlayerPrefs.SetInt("lang", (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        return T.TryGetValue(key, out string[] v) ? v[(int)Current] : key;
    }
}
