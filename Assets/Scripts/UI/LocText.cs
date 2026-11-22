using TMPro;
using UnityEngine;

// Gắn vào chữ TextMeshPro: tự đổi chữ khi đổi ngôn ngữ
[RequireComponent(typeof(TMP_Text))]
public class LocText : MonoBehaviour
{
    public string key;
    TMP_Text t;

    void OnEnable()
    {
        t = GetComponent<TMP_Text>();
        Loc.Changed += Refresh;
        Refresh();
    }

    void OnDisable() => Loc.Changed -= Refresh;

    public void SetKey(string k) { key = k; Refresh(); }

    void Refresh()
    {
        if (t != null && !string.IsNullOrEmpty(key)) t.text = Loc.Get(key);
    }
}
