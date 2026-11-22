using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Gắn vào GameManager. Điều khiển chuột:
//   Chuột trái giữ + kéo : nhấc mảnh (bay qua các mảnh khác), thả xuống
//   Chuột phải giữ + kéo : xoay quanh mảnh (có hít góc 90°, hiện vòng chỉ góc)
//   Lăn chuột trên mảnh  : xoay nhanh 90°
//   U / Backspace        : hoàn tác (bản game thật: thêm Ctrl + Z)
public class PieceController : MonoBehaviour
{
    // Báo thao tác của người chơi (dùng cho hướng dẫn): drop, rotate, scroll, undo, overlap, inside
    public static event System.Action<string> PlayerAction;
    static void Notify(string a) => PlayerAction?.Invoke(a);

    [Header("Cảm giác nhấc mảnh")]
    public float liftScale = 1.08f;
    public float followSpeed = 25f;

    [Header("Xoay")]
    [Tooltip("Lệch bao nhiêu độ so với góc 90° thì hít vào")]
    public float snapAngle = 6f;
    public float scrollRotateTime = 0.12f;

    [Header("Trượt ra khi bị đè")]
    public float slideTime = 0.15f;
    public float gap = 0.01f;

    [Header("Dò chỗ trống khi bị kẹt")]
    public float searchStep = 0.1f;
    public float maxSearchRadius = 8f;

    const int LIFTED_ORDER = 500;
    // màn tạo sẵn: các mảnh khít nhau tuyệt đối nên cho phép chạm/lấn rất nhẹ (0.03 ≈ 3 điểm ảnh) để đặt được sát nhau
    static float OVERLAP_TOLERANCE => LevelSnap.Active ? 0.03f : 0.002f;
    const float SNAP_TOLERANCE = 0.02f;
    const int MAX_UNDO = 50;
    int topOrder = 0;

    Camera cam;
    WinChecker winChecker;
    GridBoard grid;

    PuzzlePiece[] allPieces;
    readonly Dictionary<PuzzlePiece, Vector3> baseScale = new Dictionary<PuzzlePiece, Vector3>();

    // kéo
    PuzzlePiece held;
    Vector3 grabOffset;
    Vector3 pickPos;
    float pickRot;

    // xoay
    PuzzlePiece rotating;

    // cho thành tựu
    public bool UndoUsed { get; private set; }
    public float ElapsedTime => Time.time - levelStartTime;
    float levelStartTime;
    float lastAngle;
    float rawRot;        // góc "thật" theo tay, trước khi hít
    bool rotLocked;

    // rê chuột
    PuzzlePiece hovered;

    ContactFilter2D filter;
    readonly List<Collider2D> overlaps = new List<Collider2D>();

    // hoàn tác / chơi lại
    struct Pose { public PuzzlePiece p; public Vector3 pos; public float rot; }
    readonly List<Pose[]> undoStack = new List<Pose[]>();
    Pose[] initialPoses;

    // vòng chỉ góc
    GameObject ring;
    LineRenderer ringCircle, ringPointer;
    readonly List<LineRenderer> ringTicks = new List<LineRenderer>();
    float ringRadius;

    void Start()
    {
        cam = Camera.main;
        filter = ContactFilter2D.noFilter;
        winChecker = GetComponent<WinChecker>();
        grid = GetComponent<GridBoard>();

        allPieces = FindObjectsByType<PuzzlePiece>();
        foreach (PuzzlePiece p in allPieces) baseScale[p] = p.transform.localScale;
        initialPoses = Snapshot();
        levelStartTime = Time.time;

        GameHUD hud = GetComponent<GameHUD>();
        if (hud == null) hud = gameObject.AddComponent<GameHUD>();
        hud.Build(this);
    }

    void OnDisable()
    {
        // khi thắng, controller bị tắt: dọn trạng thái đang dở
        if (hovered != null) { SetHover(hovered, false); hovered = null; }
        if (ring != null) ring.SetActive(false);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            bool undo = kb.uKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame;
#if !UNITY_EDITOR
            // Trong Unity Editor, Ctrl+Z là phím hoàn tác của chính Editor (sẽ xóa đồ vật khi đang Play),
            // nên chỉ bật Ctrl+Z ở bản game thật.
            undo |= kb.zKey.wasPressedThisFrame && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
#endif
            if (undo) Undo();
        }

        Vector3 mp = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        mp.z = 0;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ===== RÊ CHUỘT: sáng mảnh dưới con trỏ =====
        if (held == null && rotating == null)
        {
            PuzzlePiece h = overUI ? null : PickTopPiece(mp);
            if (h != hovered)
            {
                if (hovered != null) SetHover(hovered, false);
                hovered = h;
                if (hovered != null) SetHover(hovered, true);
            }
        }

        // ===== CHUỘT TRÁI: nhấc - kéo - thả =====
        if (mouse.leftButton.wasPressedThisFrame && rotating == null && !overUI)
        {
            PuzzlePiece p = PickTopPiece(mp);
            if (p != null) { PushUndo(); Lift(p, mp); }
        }

        if (held != null)
        {
            Vector3 target = mp + grabOffset;
            held.transform.position = Vector3.Lerp(held.transform.position, target, followSpeed * Time.deltaTime);
            bool over = OverlapsNow(held);
            held.SetWarning(GameSettings.OverlapWarn && over);
            if (over) Notify("overlap");

            if (mouse.leftButton.wasReleasedThisFrame) Drop();
        }

        // ===== CHUỘT PHẢI: xoay (hít góc 90°) =====
        if (mouse.rightButton.wasPressedThisFrame && held == null && !overUI)
        {
            PuzzlePiece p = PickTopPiece(mp);
            if (p != null)
            {
                PushUndo();
                StopSlide(p);
                pickPos = p.transform.position;
                pickRot = p.transform.eulerAngles.z;
                rotating = p;
                rawRot = pickRot;
                rotLocked = false;
                lastAngle = AngleFromCenter(p.transform, mp);
                p.SetOrder(++topOrder);
                p.SetShadow(0.28f, PuzzlePiece.SHADOW_HOVER);
                if (GameSettings.ShowRing) ShowRing(p);
            }
        }

        if (rotating != null)
        {
            float angle = AngleFromCenter(rotating.transform, mp);
            rawRot += Mathf.DeltaAngle(lastAngle, angle);
            lastAngle = angle;

            bool locked = false;
            float shown = GameSettings.SnapRotate ? Magnet(rawRot, out locked) : rawRot;
            if (locked && !rotLocked) OnAngleSnap();
            rotLocked = locked;
            rotating.transform.rotation = Quaternion.Euler(0, 0, shown);
            rotating.SetWarning(GameSettings.OverlapWarn && OverlapsNow(rotating));
            UpdateRing(rotating, shown, locked);

            if (mouse.rightButton.wasReleasedThisFrame)
            {
                HideRing();
                PuzzlePiece p = rotating;
                rotating = null;
                p.SetWarning(false);
                p.SetShadow(0.18f, PuzzlePiece.SHADOW_REST);
                CountRotation();
                Place(p);
                Notify("rotate");
            }
        }

        // ===== LĂN CHUỘT: xoay nhanh 90° =====
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0 && held == null && rotating == null && !overUI)
        {
            PuzzlePiece p = PickTopPiece(mp);
            if (p != null && p.slide == null)
            {
                PushUndo();
                p.SetOrder(++topOrder);
                p.slide = StartCoroutine(RotateStep(p, scroll > 0 ? 90f : -90f));
                CountRotation();
                Sfx.Play("rotate");
                Notify("scroll");
            }
        }
    }

    // ===================== CHỌN / NHẤC / THẢ =====================
    PuzzlePiece PickTopPiece(Vector3 point)
    {
        PuzzlePiece best = null;
        foreach (Collider2D col in Physics2D.OverlapPointAll(point))
        {
            PuzzlePiece p = col.GetComponent<PuzzlePiece>();
            if (p == null) continue;
            if (best == null || p.Sr.sortingOrder > best.Sr.sortingOrder) best = p;
        }
        return best;
    }

    void SetHover(PuzzlePiece p, bool on)
    {
        if (p == null || p == held) return;
        // chỉ đổi bóng đổ (không phóng to) để viền va chạm và kiểm tra thắng không bị ảnh hưởng
        p.SetShadow(on ? 0.24f : 0.18f, on ? PuzzlePiece.SHADOW_HOVER : PuzzlePiece.SHADOW_REST);
    }

    void Lift(PuzzlePiece p, Vector3 mp)
    {
        StopSlide(p);
        hovered = null;
        pickPos = p.transform.position;
        pickRot = p.transform.eulerAngles.z;
        held = p;
        grabOffset = p.transform.position - mp;
        p.transform.localScale = Base(p) * liftScale;
        p.SetOrder(LIFTED_ORDER);
        p.SetShadow(0.32f, PuzzlePiece.SHADOW_LIFT);
        OnPickUp();
    }

    void Drop()
    {
        PuzzlePiece p = held;
        held = null;
        p.transform.localScale = Base(p);
        p.SetOrder(++topOrder);
        p.SetShadow(0.18f, PuzzlePiece.SHADOW_REST);
        p.SetWarning(false);
        OnPutDown();
        Place(p);
        Notify("drop");
    }

    Vector3 Base(PuzzlePiece p) => baseScale.TryGetValue(p, out Vector3 s) ? s : Vector3.one;

    // Đang kéo/xoay mà chỗ hiện tại sẽ đè lên thứ khác? (kiểm tra với kích thước thật, không tính phóng to khi nhấc)
    bool OverlapsNow(PuzzlePiece p)
    {
        Vector3 s = p.transform.localScale;
        p.transform.localScale = Base(p);
        Physics2D.SyncTransforms();
        overlaps.Clear();
        p.Col.Overlap(filter, overlaps);
        bool hit = false;
        foreach (Collider2D other in overlaps)
        {
            ColliderDistance2D d = p.Col.Distance(other);
            if (d.isOverlapped && d.distance < -OVERLAP_TOLERANCE) { hit = true; break; }
        }
        p.transform.localScale = s;
        Physics2D.SyncTransforms();
        return hit;
    }

    // ===================== XOAY =====================
    float Magnet(float raw, out bool locked)
    {
        float nearest = Mathf.Round(raw / 90f) * 90f;
        locked = Mathf.Abs(Mathf.DeltaAngle(raw, nearest)) <= snapAngle;
        return locked ? nearest : raw;
    }

    IEnumerator RotateStep(PuzzlePiece p, float step)
    {
        float from = p.transform.eulerAngles.z;
        float to = Mathf.Round(from / 90f) * 90f + step;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / scrollRotateTime;
            p.transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(from, to, Mathf.SmoothStep(0, 1, t)));
            yield return null;
        }
        p.transform.rotation = Quaternion.Euler(0, 0, to);
        p.slide = null;
        OnAngleSnap();
        pickPos = p.transform.position;
        pickRot = from;
        Place(p);
    }

    // ===================== ĐẶT XUỐNG =====================
    void Place(PuzzlePiece p)
    {
        if (grid == null || !p.hasGridOffset || !grid.IsOverBoard(p.transform.position))
        {
            // màn tạo sẵn: thả gần đúng chỗ thì hút vào khít
            Vector3 p0 = p.transform.position;
            float r0 = p.transform.eulerAngles.z;
            foreach (var c in LevelSnap.Candidates(p))
            {
                if (IsFreeAtPose(p, c.pos, c.rot, OVERLAP_TOLERANCE))
                {
                    SetPose(p, p0, r0);
                    p.slide = StartCoroutine(SlideTo(p, p0, c.pos, r0, c.rot));
                    return;
                }
            }
            SetPose(p, p0, r0);
            Settle(p);
            return;
        }

        Vector3 startPos = p.transform.position;
        float startRot = p.transform.eulerAngles.z;
        float snapRot = grid.SnapRotation(startRot);
        foreach (Vector3 candidate in grid.SnapCandidates(p, startPos, snapRot))
        {
            if (IsFreeAtPose(p, candidate, snapRot, SNAP_TOLERANCE))
            {
                SetPose(p, startPos, startRot);
                p.slide = StartCoroutine(SlideTo(p, startPos, candidate, startRot, snapRot));
                return;
            }
        }
        SetPose(p, startPos, startRot);
        p.slide = StartCoroutine(SlideTo(p, startPos, pickPos, startRot, pickRot));
    }

    bool IsFreeAtPose(PuzzlePiece p, Vector3 pos, float rotZ, float tolerance)
    {
        SetPose(p, pos, rotZ);
        overlaps.Clear();
        p.Col.Overlap(filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            ColliderDistance2D d = p.Col.Distance(other);
            if (d.isOverlapped && d.distance < -tolerance) return false;
        }
        return true;
    }

    void SetPose(PuzzlePiece p, Vector3 pos, float rotZ)
    {
        p.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, rotZ));
        Physics2D.SyncTransforms();
    }

    // Trượt ra chỗ trống nếu đang đè lên thứ khác (chỉ mảnh này di chuyển)
    void Settle(PuzzlePiece p)
    {
        Vector3 start = p.transform.position;
        Vector3 end = FindFreePosition(p);
        float rz = p.transform.eulerAngles.z;
        if ((end - start).sqrMagnitude > 0.000001f)
            p.slide = StartCoroutine(SlideTo(p, start, end, rz, rz));
        else
            NotifySettled();
    }

    Vector3 FindFreePosition(PuzzlePiece p)
    {
        Vector3 start = p.transform.position;
        Vector3 result = start;
        if (!IsFreeAt(p, start))
        {
            Vector3 pushed = PushOut(p, start);
            result = IsFreeAt(p, pushed) ? pushed : SearchNearestFree(p, start);
        }
        p.transform.position = start;
        Physics2D.SyncTransforms();
        return result;
    }

    Vector3 PushOut(PuzzlePiece p, Vector3 from)
    {
        p.transform.position = from;
        for (int i = 0; i < 30; i++)
        {
            Physics2D.SyncTransforms();
            overlaps.Clear();
            p.Col.Overlap(filter, overlaps);
            bool moved = false;
            foreach (Collider2D other in overlaps)
            {
                ColliderDistance2D d = p.Col.Distance(other);
                if (!d.isOverlapped || d.distance > -OVERLAP_TOLERANCE) continue;
                Vector2 push = d.pointB - d.pointA;
                p.transform.position += (Vector3)(push + push.normalized * gap);
                Physics2D.SyncTransforms();
                moved = true;
            }
            if (!moved) break;
        }
        return p.transform.position;
    }

    Vector3 SearchNearestFree(PuzzlePiece p, Vector3 center)
    {
        for (float r = searchStep; r <= maxSearchRadius; r += searchStep)
        {
            int count = Mathf.Max(8, Mathf.CeilToInt(2 * Mathf.PI * r / searchStep));
            for (int k = 0; k < count; k++)
            {
                float a = k * 2 * Mathf.PI / count;
                Vector3 candidate = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r;
                if (IsFreeAt(p, candidate)) return candidate;
            }
        }
        return center;
    }

    bool IsFreeAt(PuzzlePiece p, Vector3 pos)
    {
        p.transform.position = pos;
        Physics2D.SyncTransforms();
        overlaps.Clear();
        p.Col.Overlap(filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            ColliderDistance2D d = p.Col.Distance(other);
            if (d.isOverlapped && d.distance < -OVERLAP_TOLERANCE) return false;
        }
        return true;
    }

    IEnumerator SlideTo(PuzzlePiece p, Vector3 from, Vector3 to, float fromRot, float toRot)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / slideTime;
            float k = Mathf.SmoothStep(0, 1, t);
            p.transform.SetPositionAndRotation(Vector3.Lerp(from, to, k),
                Quaternion.Euler(0, 0, Mathf.LerpAngle(fromRot, toRot, k)));
            yield return null;
        }
        p.transform.SetPositionAndRotation(to, Quaternion.Euler(0, 0, toRot));
        p.slide = null;
        NotifySettled();
    }

    void CountRotation()
    {
        if (Achievements.AddCounter("rotations") >= 100) Achievements.Unlock("rotate_100");
    }

    void NotifySettled()
    {
        if (winChecker != null && winChecker.CountInside() > 0) { Achievements.Unlock("first_piece"); Notify("inside"); }
        if (winChecker != null) winChecker.CheckWin();
    }

    void StopSlide(PuzzlePiece p)
    {
        if (p.slide != null) { StopCoroutine(p.slide); p.slide = null; }
    }

    float AngleFromCenter(Transform t, Vector3 mp)
    {
        Vector2 dir = mp - t.position;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    // ===================== HOÀN TÁC / CHƠI LẠI =====================
    Pose[] Snapshot()
    {
        Pose[] s = new Pose[allPieces.Length];
        for (int i = 0; i < allPieces.Length; i++)
            s[i] = new Pose { p = allPieces[i], pos = allPieces[i].transform.position, rot = allPieces[i].transform.eulerAngles.z };
        return s;
    }

    void PushUndo()
    {
        undoStack.Add(Snapshot());
        if (undoStack.Count > MAX_UNDO) undoStack.RemoveAt(0);
    }

    void Restore(Pose[] poses)
    {
        foreach (Pose ps in poses)
        {
            if (ps.p == null) continue;
            StopSlide(ps.p);
            ps.p.transform.SetPositionAndRotation(ps.pos, Quaternion.Euler(0, 0, ps.rot));
            ps.p.transform.localScale = Base(ps.p);
            ps.p.SetWarning(false);
            ps.p.SetShadow(0.18f, PuzzlePiece.SHADOW_REST);
        }
        Physics2D.SyncTransforms();
    }

    public void Undo()
    {
        if (!enabled || held != null || rotating != null || undoStack.Count == 0) return;
        UndoUsed = true;
        Pose[] last = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        Restore(last);
        Sfx.Play("undo");
        Notify("undo");
    }

    public void RestartLevel()
    {
        StopAllCoroutines();
        foreach (PuzzlePiece p in allPieces) if (p != null) p.slide = null;
        held = null; rotating = null; hovered = null;
        HideRing();
        Restore(initialPoses);
        undoStack.Clear();
        UndoUsed = false;
        levelStartTime = Time.time;
        if (winChecker != null) winChecker.ResetWin();
        enabled = true;
    }

    // ===================== VÒNG CHỈ GÓC =====================
    void ShowRing(PuzzlePiece p)
    {
        if (ring == null) CreateRing(p.Sr.sharedMaterial);
        Vector3 ext = p.Sr.sprite.bounds.extents;
        ringRadius = new Vector2(ext.x * Base(p).x, ext.y * Base(p).y).magnitude + 0.15f;
        ring.SetActive(true);

        for (int i = 0; i < ringCircle.positionCount; i++)
        {
            float a = i * Mathf.PI * 2 / ringCircle.positionCount;
            ringCircle.SetPosition(i, p.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * ringRadius);
        }
        for (int i = 0; i < 4; i++)
        {
            Vector3 d = Quaternion.Euler(0, 0, 90 * i) * Vector3.up;
            ringTicks[i].SetPosition(0, p.transform.position + d * (ringRadius - 0.14f));
            ringTicks[i].SetPosition(1, p.transform.position + d * (ringRadius + 0.14f));
        }
    }

    void UpdateRing(PuzzlePiece p, float angle, bool locked)
    {
        if (ring == null) return;
        Vector3 d = Quaternion.Euler(0, 0, angle) * Vector3.up;
        ringPointer.SetPosition(0, p.transform.position + d * (ringRadius - 0.22f));
        ringPointer.SetPosition(1, p.transform.position + d * (ringRadius + 0.22f));
        Color c = locked ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(1f, 1f, 1f, 0.9f);
        ringPointer.startColor = c;
        ringPointer.endColor = c;
    }

    void HideRing()
    {
        if (ring != null) ring.SetActive(false);
    }

    void CreateRing(Material mat)
    {
        ring = new GameObject("RotateRing");
        ringCircle = NewLine(ring.transform, mat, 64, 0.035f, new Color(1f, 1f, 1f, 0.45f));
        ringCircle.loop = true;
        for (int i = 0; i < 4; i++)
            ringTicks.Add(NewLine(ring.transform, mat, 2, 0.05f, new Color(1f, 1f, 1f, 0.7f)));
        ringPointer = NewLine(ring.transform, mat, 2, 0.07f, Color.white);
        ring.SetActive(false);
    }

    LineRenderer NewLine(Transform parent, Material mat, int points, float width, Color color)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = mat;
        lr.useWorldSpace = true;
        lr.positionCount = points;
        lr.widthMultiplier = width;
        lr.numCapVertices = 4;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 2 * LIFTED_ORDER + 10;
        return lr;
    }

    // ===================== ÂM THANH (gắn sau) =====================
    void OnPickUp() => Sfx.Play("pick");
    void OnPutDown() => Sfx.Play("put");
    void OnAngleSnap() => Sfx.Play("snap");
}
