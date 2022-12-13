using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class WallTransformHandler : ITransformHandler
{
    private HarnessEventHandler eventHandler;
    private GameObject _wallGO;
    private NewWall _wallItem;
    public List<GameObject> handles = new List<GameObject>();
    private LineRenderer _wallRenderer;
    private Color _originalColor;


    public WallTransformHandler(GameObject go, NewWall item)
    {
        this._wallGO = go;
        this._wallItem = item;
        this._wallRenderer = this._wallGO.GetComponent<LineRenderer>();

        this._originalColor = this._wallRenderer.material.color;

        //AttachResizeHarness();

        eventHandler = go.GetComponent<HarnessEventHandler>() == null ? go.AddComponent<HarnessEventHandler>() : go.GetComponent<HarnessEventHandler>();

        eventHandler.drag += Dragged;
        eventHandler.mouseHover += Hovered;
        eventHandler.mouseExit += Exit;
        eventHandler.mouseDown += DragStart;
        eventHandler.mouseUp += Released;
    }

    private enum state
    {
        idle,
        dragging
    }
    private state _state = state.idle;

    public void DragStart(Vector3 data)
    {
        HarnessEventHandler.selected = true;
    }


    public void Dragged(Vector3 data)
    {
        _state = state.dragging;

        if (eventHandler.isInsideCanvas())
        {
            var position0 = _wallRenderer.GetPosition(0);
            var position1 = _wallRenderer.GetPosition(1);
            var points = new Vector3[2];

            var x = Input.GetAxis("Mouse X");
            var y = Input.GetAxis("Mouse Y");

            Vector3 moveDirection = new Vector3(x, y, 0.0f);
            moveDirection = Quaternion.AngleAxis(eventHandler._camera.transform.eulerAngles.z, Vector3.forward) * moveDirection;
            moveDirection *= eventHandler._camera.orthographicSize / 10.0f;

            points[0] = new Vector3(position0.x + moveDirection.x * HarnessConstant.MOVEMENT_SENSITIVITY, position0.y + moveDirection.y * HarnessConstant.MOVEMENT_SENSITIVITY, -0.2f);
            points[1] = new Vector3(position1.x + moveDirection.x * HarnessConstant.MOVEMENT_SENSITIVITY, position1.y + moveDirection.y * HarnessConstant.MOVEMENT_SENSITIVITY, -0.2f);

            this._wallRenderer.SetPositions(points);
            this._wallGO.transform.position = points[0];

            float angle = Mathf.Atan2(points[1].y - points[0].y, points[1].x - points[0].x) * 180 / Mathf.PI;
            NewBuildingController.UpdateWall(this._wallItem.name, points[0], points[1], angle);
        }
    }

    public void Hovered()
    {
        if (!HarnessEventHandler.selected && !CreatorUIController.isInputOverVisualElement())
        {
            Highlight();
        }
    }

    public void Exit()
    {
        HarnessEventHandler.selected = false;
        RemoveHighlight();
    }

    public void Released()
    {
        HarnessEventHandler.selected = false;
        if (_state == state.dragging)
        {
            _state = state.idle;
            RemoveHighlight();
            return;
        }
        RemoveHighlight();
    }

    public void Highlight()
    {
        if (this._wallRenderer != null)
        {
            this._wallRenderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
    }

    public void RemoveHighlight()
    {
        if (this._wallRenderer != null)
        {
            this._wallRenderer.material.color = this._originalColor;
        }
    }

    public void AttachResizeHarness()
    {
        for (int i = 0; i < this._wallRenderer.positionCount; i++)
        {
            this.handles.Add(GameObject.CreatePrimitive(PrimitiveType.Quad));
            var points = this._wallRenderer.GetPosition(i);
            this.handles[i].transform.parent = this._wallGO.transform;

            this.handles[i].transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
            this.handles[i].transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
            this.handles[i].name = $"{i}_{ObjectName.RESIZE_HARNESS}";
            this.handles[i].transform.rotation = new Quaternion(0, 0, 0, 0);

            TransformHandle handle = new TransformHandle(this.handles[i], this._wallItem, this._wallRenderer, i);
        }
    }
}