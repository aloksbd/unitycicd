using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HarnessManipulator : MonoBehaviour
{
    // public HarnessManipulator current;
    public EventTrigger myEventTrigger;
    private Texture2D current_cursor;

    public string current_cursor_path;

    public void Start()
    {
        // current = this;
        current_cursor = Resources.Load<Texture2D>(current_cursor_path);
    }


    public event Action mouseEnter;
    public event Action mouseExit;

    public event Action<Vector3> mouseDrag;
    public event Action<Vector3> mouseDown;
    public event Action<Vector3> mouseUp;

    public void OnMouseEnter()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, new Vector2(current_cursor.width / 2, current_cursor.height / 2), CursorMode.Auto);
        mouseEnter?.Invoke();
    }

    public void OnMouseExit()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        mouseExit?.Invoke();
    }

    public void OnMouseDrag()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, new Vector2(current_cursor.width / 2, current_cursor.height / 2), CursorMode.Auto);
        if (mouseDrag != null)
        {
            mouseDrag(getMousePosition().GetValueOrDefault());
        }
    }

    public void OnMouseDown()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, new Vector2(current_cursor.width / 2, current_cursor.height / 2), CursorMode.Auto);
        if (mouseDown != null)
        {
            mouseDown(getMousePosition().GetValueOrDefault());
        }
    }

    public void OnMouseUp()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (mouseUp != null)
        {
            mouseUp(getMousePosition().GetValueOrDefault());
        }
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