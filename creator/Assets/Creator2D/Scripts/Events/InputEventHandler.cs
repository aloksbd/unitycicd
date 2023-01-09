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
    public static LayerMask UiMask, UiWallObjectOverlap;

    public void Start()
    {
        UiMask = 1 << LayerMask.NameToLayer("UI");
        UiWallObjectOverlap = 1 << LayerMask.NameToLayer("Default");
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (!IsMoving && !CreatorUIController.isInputOverVisualElement() && !HarnessRotateManipulator.IsRotating)
        {
            Texture2D CursorTexture = Resources.Load<Texture2D>(WHConstants.FINGER_POINTER);
            UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 4, 0), CursorMode.Auto);

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

    public static bool IsInsideCanvas(Vector3 position)
    {
        if (Physics.Raycast(position, Vector3.forward, out RaycastHit hitInfo, Mathf.Infinity, UiMask, QueryTriggerInteraction.Collide))
        {
            if (hitInfo.transform.name == "BuildingCanvas")
            {
                return true;
            }
        }
        return false;
    }

    public static bool CursorInsideCanvas()
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, UiMask, QueryTriggerInteraction.Collide))
            {
                if (hitInfo.transform.name == "BuildingCanvas")
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CheckObjectOverlap(GameObject GO)
    {
        var bound = GO.GetComponent<BoxCollider>().bounds;

        Collider[] collider = Physics.OverlapBox(bound.center, bound.extents, Quaternion.identity, UiWallObjectOverlap, QueryTriggerInteraction.Collide);
        if (collider.Length > 0)
        {
            foreach (var obj in collider)
            {
                if (obj.gameObject != GO)
                {
                    var item = CreatorItemFinder.FindItemWithGameObject(obj.gameObject);
                    if (item is NewWindow || item is NewDoor)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
