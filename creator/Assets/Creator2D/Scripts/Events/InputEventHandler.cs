using UnityEngine.EventSystems;
using UnityEngine;
using System;

public class InputEventHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<Vector3> MouseDrag;
    public event Action<Vector3> MouseDragStart;
    public event Action<Vector3> MouseDragEnd;
    public event Action MouseHovered;
    public event Action MouseExit;
    public event Action MouseClick;
    public static bool IsMoving;
    public static bool selected;

    public void OnPointerEnter(PointerEventData data)
    {
        if (!IsMoving && !CreatorUIController.isInputOverVisualElement() && !HarnessRotateManipulator.IsRotating)
        {
            Texture2D CursorTexture = Resources.Load<Texture2D>(WHConstants.FINGER_POINTER);
            UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);

            MouseHovered?.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (!IsMoving && !HarnessRotateManipulator.IsRotating)
        {
            UnityEngine.Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);
            MouseExit?.Invoke();
        }
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (!IsMoving)
        {
            MouseClick?.Invoke();
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (!HarnessRotateManipulator.IsRotating)
        {
            Texture2D CursorTexture = Resources.Load<Texture2D>(WHConstants.DRAG_POINTER);
            UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);

            MouseDrag?.Invoke(getMousePosition().GetValueOrDefault());
        }
    }

    public void OnBeginDrag(PointerEventData data)
    {
        IsMoving = true;
        Texture2D CursorTexture = Resources.Load<Texture2D>(WHConstants.DRAG_POINTER);
        UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);

        MouseDragStart?.Invoke(getMousePosition().GetValueOrDefault());
    }

    public void OnEndDrag(PointerEventData data)
    {
        IsMoving = false;
        UnityEngine.Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);
        MouseDragEnd?.Invoke(getMousePosition().GetValueOrDefault());
    }

    public Vector3? getMousePosition()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);

        float rayDistance;
        if (plane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);

        }
        return null;
    }
}
