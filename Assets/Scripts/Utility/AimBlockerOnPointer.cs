using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Valley.Aiming;

public class AimBlockerOnPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IAimBlocker
{
    public bool blockAim = false;

    public bool CanAim => !blockAim;

    public void OnPointerDown(PointerEventData eventData)
    {
        blockAim = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        blockAim = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        blockAim = false;
    }
}