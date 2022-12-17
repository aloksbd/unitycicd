using System;
using UnityEngine;

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
    public event Action mouseClicked;
    private static bool moving = false;
    private float downClickTime;
    private float ClickDeltaTime = 0.2f;

    public void OnMouseEnter()
    {
        if (!moving && !CreatorUIController.isInputOverVisualElement())
        {
            Texture2D CursorTexture = Resources.Load<Texture2D>("Sprites/Cursor/Finger_Pointer");
            UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);
            mouseHover?.Invoke();
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
        if (!IsClick())
        {
            moving = true;
            drag(getMousePosition().GetValueOrDefault());
        }
    }


    public void OnMouseDown()
    {
        downClickTime = Time.time;
        if (mouseDown != null)
        {
            Vector3 clickPos = getMousePosition().GetValueOrDefault();
            mouseDown(clickPos);
        }
    }

    public void OnMouseUp()
    {
        if (IsClick())
        {
            mouseClicked?.Invoke();
        }
        moving = false;
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        mouseUp?.Invoke();
    }

    public bool IsClick()
    {
        if (Time.time - downClickTime <= ClickDeltaTime)
        {
            return true;
        }
        return false;
    }

    public Vector3? getMousePosition()
    {
        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);

        float rayDistance;
        if (plane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);

        }
        return null;
    }

    public bool isInsideCanvas()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            if (hitInfo.transform.name == "BuildingCanvas" || hitInfo.transform.tag == "METABLOCK" || hitInfo.transform.tag == "Node")
            {
                return true;
            }
        }
        return false;
    }

    public void isMovable(GameObject GO)
    {
        RaycastHit hit;

        if (Physics.BoxCast(GO.transform.position, GO.transform.localScale / 2, GO.transform.forward, out hit, Quaternion.identity, 1f))
        {
            Trace.Log($"WE HIT :: {hit.transform.name}");
            Debug.DrawRay(GO.transform.position, GO.transform.forward, Color.green, 10f);
        }
    }
}