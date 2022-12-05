using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HarnessEventHandler : MonoBehaviour
{
    public string current_cursor_path;

    public static bool selected;
    public Camera _camera;


    public void Start()
    {
        selected = false;
        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        _camera = cam.GetComponent<Camera>();
    }

    public event Action<Vector3> mouseDrag;
    public event Action<Vector3> drag;
    public event Action<Vector3> mouseDown;
    public event Action mouseUp;
    public event Action mouseHover;
    public event Action mouseExit;
    private static bool moving = false;


    public void OnMouseEnter()
    {
        if (!moving && !CreatorUIController.isInputOverVisualElement())
        {
            if (mouseHover != null)
            {
                Texture2D CursorTexture = Resources.Load<Texture2D>("Sprites/Cursor/Finger_Pointer");
                UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);
                mouseHover?.Invoke();
            }
        }
    }

    public void OnMouseExit()
    {
        if (!moving)
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            mouseExit?.Invoke();
            selected = false;
        }
    }

    public void OnMouseDrag()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            if (hitInfo.transform.name == "BuildingCanvas")
            {
                moving = true;
                drag(getMousePosition().GetValueOrDefault());
            }
        }
    }


    public void OnMouseDown()
    {
        //UnityEngine.Cursor.SetCursor(current_cursor, Vector2.zero, CursorMode.Auto);
        if (mouseDown != null)
        {
            Vector3 clickPos = getMousePosition().GetValueOrDefault();
            mouseDown(clickPos);
        }
    }

    public void OnMouseUp()
    {
        moving = false;
        if (mouseUp != null)
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            mouseUp?.Invoke();
        }
    }

    public Vector3? getMousePosition()
    {
        // var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // worldPos.z = 0;

        // //Trace.Log("worldPos: " + worldPos);
        // return worldPos;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);

        float rayDistance;
        if (plane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);

        }
        return null;
    }

    void OnCollisionStay(Collision other)
    {
        Trace.Log("CollisionStay: " + other.gameObject.name);
    }

}