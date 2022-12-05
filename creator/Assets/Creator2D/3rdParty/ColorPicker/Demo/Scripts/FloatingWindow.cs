using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ColorPickerUtil.Demo
{
    public class FloatingWindow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Transform target;

        bool dragging = false;
        Vector3 dif;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            dif = target.position - Input.mousePosition;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
        }

        private void Update()
        {
            if (!dragging) return;
            target.position = Input.mousePosition + dif;
        }
    }
}