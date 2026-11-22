using System.Collections.Generic;
using UnityEngine;

// Lưu "lời giải mẫu" của màn: vị trí + góc xoay của từng mảnh khi nằm gọn trong khung.
// Dùng cho kỹ năng gợi ý sau này (mảnh tự nhảy về đúng chỗ).
[System.Serializable]
public class PieceSlot
{
    public PuzzlePiece piece;
    public Vector2 position;   // vị trí trong thế giới (khung đặt ở gốc 0,0)
    public float rotation;     // góc Z (độ)
}

public class LevelSolution : MonoBehaviour
{
    public float frameSize;                       // cạnh trong của khung (đơn vị Unity)
    public List<PieceSlot> slots = new List<PieceSlot>();

    public PieceSlot SlotOf(PuzzlePiece p)
    {
        foreach (PieceSlot s in slots) if (s.piece == p) return s;
        return null;
    }
}
