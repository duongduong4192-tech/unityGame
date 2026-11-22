# 🪵 Chill Puzzle

> Game xếp hình gỗ thư giãn, phong cách ASMR, làm bằng Unity 6.
> **Trạng thái:** đang phát triển 🚧

<!-- TODO: GIF gameplay ở đây (xem mục "Cách quay GIF" ở cuối) -->
<!-- ![Gameplay](docs/gameplay.gif) -->

## Giới thiệu

Chill Puzzle là game xếp hình gỗ nhẹ nhàng. Người chơi kéo, xoay các mảnh gỗ và ghép chúng vừa khít vào khung, không được để mảnh chồng lên nhau. Game không tính giờ, không thua: chỉ có tiếng gỗ, nhạc nhẹ và cảm giác ghép đúng mảnh cuối cùng.

## Tính năng

- **45 màn chơi, 4 thể loại**, độ khó tăng dần (3–16 mảnh):
  - Hình học: tam giác, thoi, bình hành, hình thang…
  - Ghép khối: các khối ô vuông kiểu polyomino
  - Đường cong: mảnh cắt theo đường cong
  - Hình tròn: xếp các đĩa tròn vào khung
- **Màn hướng dẫn "Số" (0–9)** dạy toàn bộ thao tác, bắt buộc chơi lần đầu
- **Nam châm theo mép**: thả gần đúng chỗ là mảnh tự áp sát vào mép khung hoặc mép mảnh bên cạnh, mà không làm lộ đáp án
- **Lưu màn đang chơi dở**: thoát giữa chừng, lần sau vào lại thì các mảnh vẫn nằm đúng chỗ cũ
- **Hiệu ứng mảnh gỗ rơi xuống bàn** kiểu giả 3D khi vào màn
- **Âm thanh ASMR**: tiếng gỗ khi nhấc, đặt, xoay, chạm bàn; nhạc nền chill
- Menu dạng phòng vẽ tay: bảng chọn màn, cài đặt, thành tựu đều là trang sổ tay
- Hỗ trợ **Tiếng Việt / English**
- Hoàn tác (Ctrl+Z), chơi lại, chuyển sang màn tiếp

## Điều khiển

| Thao tác | Phím / chuột |
|---|---|
| Nhấc và kéo mảnh | Giữ chuột trái |
| Xoay mảnh | Giữ chuột phải và kéo |
| Xoay nhanh từng nấc | Lăn chuột khi đang cầm mảnh |
| Hoàn tác | Ctrl + Z |
| Mở menu / quay lại | Esc |

<!-- TODO: kiểm tra lại bảng này cho khớp với game -->

## Hệ thống tự lập trình

| Hệ thống | Mô tả |
|---|---|
| **Màn chơi theo dữ liệu** | Mỗi màn gồm 1 file `.json` (đường viền mảnh, khung, đáp án) và 1 ảnh atlas `.png`. `LevelLoader` dựng khung, tường va chạm và các mảnh lúc chạy. Thêm màn mới không phải sửa code |
| **Tạo màn tự động** | Script Python (shapely, OpenCV, Pillow) cắt khung thành mảnh nên màn nào cũng chắc chắn có lời giải, rồi phủ vân gỗ thật và đổ bóng vát cạnh <!-- TODO: chưa có trong repo --> |
| **Va chạm và đặt mảnh** | `PolygonCollider2D` theo đúng hình mảnh. Kiểm tra chồng mảnh có sai số nhỏ; khung không phải chữ nhật dùng tường vòng 2 đường viền |
| **Nam châm (LevelSnap)** | Ghép khối bám lưới ô. Hình học / Đường cong hút theo mép song song gần nhất, trượt khớp góc, khớp điểm trên mép cong |
| **Kiểm tra thắng** | Mọi đỉnh của mảnh phải nằm trong đa giác khung (`FrameShape`) |
| **Hoàn tác / Lưu dở** | Ghi lại vị trí và góc xoay của từng mảnh; lưu bằng PlayerPrefs, mỗi màn một bản |
| **Hướng dẫn** | Mỗi bước chờ người chơi làm đúng thao tác, lắng nghe qua sự kiện `PieceController.PlayerAction` |
| **Âm thanh** | `Sfx` tự khởi tạo, giữ qua các scene, phát ngẫu nhiên nhiều bản và lệch cao độ nhẹ cho đỡ lặp |
| **Giao diện** | Toàn bộ UI dựng bằng code (`UIFactory`, `HandUI`); các bảng menu dùng chung interface `IMenuPanel` |
| **Đa ngôn ngữ** | `Loc` cùng `LocText` đổi chữ ngay khi người chơi chọn ngôn ngữ khác |

## Công nghệ

- **Engine:** Unity 6 (6000.5.9f1), URP 2D
- **Ngôn ngữ:** C#, Python (công cụ tạo màn)
- **Input:** Unity Input System
- **Đồ hoạ:** Krita, vân gỗ thật từ ambientCG <!-- TODO: bổ sung công cụ khác nếu có -->
- **Nền tảng:** Windows 64-bit

## Cấu trúc thư mục

```
Assets/
├── Scripts/
│   ├── Levels/      # LevelData, LevelLoader, LevelSnap, LevelSave
│   ├── UI/          # Menu, bảng chọn màn, cài đặt, thành tựu, ngôn ngữ
│   ├── Audio/       # Sfx
│   ├── Editor/      # Công cụ trong Editor, luật nhập ảnh/âm thanh
│   └── ...          # PieceController, WinChecker, Tutorial, GameHUD
├── Resources/
│   ├── Levels/      # catalog.json + {thể loại}/*.json, *.png
│   ├── Audio/       # sfx/, music/
│   ├── UI/          # Ảnh giao diện vẽ tay
│   └── Fonts/
└── Scenes/          # MainMenu, PuzzleScene
```

## Mở project

1. Cài **Unity Hub** và **Unity 6000.5.9f1** (kèm Windows Build Support)
2. Clone repo, trong Unity Hub chọn **Add → Add project from disk**
3. Mở scene `Assets/Scenes/MainMenu` rồi bấm Play
4. Build: **File → Build Profiles → Windows → Build**

## Bản chơi thử

<!-- TODO: link itch.io hoặc bản build khi phát hành -->
Chưa có bản công khai.

## Ảnh chụp

<!-- TODO: thêm ảnh menu, bảng chọn màn, màn chơi -->

## Kế hoạch

- [ ] Thêm màn và chủ đề mới (con vật, cây cối…)
- [ ] Thư viện tranh đã hoàn thành ở mục Thành tựu
- [ ] Hiển thị % người chơi hoàn thành (Steam stats)
- [ ] Đường cong cong hơn, xếp tròn khít hơn
- [ ] Phát hành trên Steam / itch.io
<!-- TODO: bổ sung -->

## Ghi công

- Âm thanh: [Kenney](https://kenney.nl) – Interface Sounds, Impact Sounds (CC0)
- Vân gỗ: [ambientCG](https://ambientcg.com) (CC0)
- Font: Patrick Hand, Baloo 2, Caveat Brush (SIL Open Font License)
- Nhạc nền: *The Last Window*, tạo bằng Google Gemini <!-- TODO: kiểm tra điều khoản sử dụng thương mại -->

## Tác giả

<!-- TODO: tên, liên hệ -->

---

### Cách quay GIF (để điền mục đầu trang)
1. Tải **ScreenToGif** (miễn phí, Windows)
2. Mở game, khung quay chỉ phủ cửa sổ game, quay 5–10 giây cảnh ghép vài mảnh
3. Lưu thành `docs/gameplay.gif` (nên dưới 10 MB), đưa vào repo rồi bỏ dấu `<!-- -->` ở dòng ảnh đầu trang
