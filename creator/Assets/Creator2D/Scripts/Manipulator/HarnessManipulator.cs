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

    public void Update()
    {
    }

    public event Action<Vector3> mouseDrag;
    public event Action<Vector3> mouseDown;
    public event Action<Vector3> mouseUp;

    public void OnMouseEnter()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, Vector2.zero, CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void OnMouseDrag()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, Vector2.zero, CursorMode.Auto);
        if (mouseDrag != null)
        {
            mouseDrag(getMousePosition());
        }
    }

    public void OnMouseDown()
    {
        UnityEngine.Cursor.SetCursor(current_cursor, Vector2.zero, CursorMode.Auto);
        if (mouseDown != null)
        {
            mouseDown(getMousePosition());
        }
    }

    public void OnMouseUp()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (mouseUp != null)
        {
            mouseUp(getMousePosition());
        }
    }

    public Vector3 getMousePosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // return Input.mousePosition;
    }
}