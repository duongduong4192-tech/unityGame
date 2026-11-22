using System.Collections.Generic;
using UnityEngine;

// Lưới trong khung: mảnh thả vào khung sẽ hít về cạnh lưới gần nhất và xoay về góc 90° gần nhất.
// Gắn vào GameManager (công cụ dựng màn tự gắn).
public class GridBoard : MonoBehaviour
{
    public int cols = 5;
    public int rows = 4;
    public float cellSize = 1.2f;
    public Vector2 center = Vector2.zero;

    [Tooltip("Tâm mảnh nằm trong vùng này (lưới nới rộng thêm) thì mới hít lưới")]
    public float snapMargin = 0.6f;

    Vector2 Origin => center - new Vector2(cols * cellSize, rows * cellSize) / 2f;   // đỉnh dưới-trái

    public bool IsOverBoard(Vector3 pos)
    {
        Vector2 o = Origin;
        return pos.x > o.x - snapMargin && pos.x < o.x + cols * cellSize + snapMargin &&
               pos.y > o.y - snapMargin && pos.y < o.y + rows * cellSize + snapMargin;
    }

    public float SnapRotation(float rotZ) => Mathf.Round(rotZ / 90f) * 90f;

    // Vị trí hít lưới, gần nhất trước (ô gần nhất rồi 8 ô xung quanh)
    public IEnumerable<Vector3> SnapCandidates(PuzzlePiece p, Vector3 pos, float rotZ)
    {
        Vector2 off = Quaternion.Euler(0, 0, rotZ) * p.gridOffset;   // tâm ảnh -> đỉnh lưới, đã xoay
        Vector2 vertex = (Vector2)pos + off;
        Vector2 o = Origin;
        Vector2 f = (vertex - o) / cellSize;
        int ix = Mathf.RoundToInt(f.x), iy = Mathf.RoundToInt(f.y);

        var list = new List<Vector3>();
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2 v = o + new Vector2(ix + dx, iy + dy) * cellSize;
                list.Add(new Vector3(v.x - off.x, v.y - off.y, pos.z));
            }
        list.Sort((a, b) => (a - pos).sqrMagnitude.CompareTo((b - pos).sqrMagnitude));
        return list;
    }

    // Vẽ lưới trong cửa sổ Scene để dễ nhìn khi dựng màn
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Vector2 o = Origin;
        for (int i = 0; i <= cols; i++)
            Gizmos.DrawLine(o + new Vector2(i * cellSize, 0), o + new Vector2(i * cellSize, rows * cellSize));
        for (int j = 0; j <= rows; j++)
            Gizmos.DrawLine(o + new Vector2(0, j * cellSize), o + new Vector2(cols * cellSize, j * cellSize));
    }
}
