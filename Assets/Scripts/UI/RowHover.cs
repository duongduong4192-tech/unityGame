using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Rê chuột vào dòng để chọn, bấm vào dòng để đổi giá trị
public class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public Action onEnter, onClick;
    public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
    public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { if (onClick != null) Sfx.Play("click"); onClick?.Invoke(); } }
}
