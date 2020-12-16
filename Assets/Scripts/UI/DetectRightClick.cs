using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DetectRightClick : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent rightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            rightClick.Invoke();
    }
}
