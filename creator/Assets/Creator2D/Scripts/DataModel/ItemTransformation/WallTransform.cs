using UnityEngine;
using System.Collections.Generic;

public class WallTransform : ITransformHandler
{
    public static List<WallListener> wallListeners = new List<WallListener>();
    private HarnessEventHandler eventHandler;
    private WallListener _wallListener;
    private LineRenderer _wallRenderer;
    private Color _originalColor;


    public WallTransform(GameObject go, CreatorItem item)
    {
        this._wallRenderer = go.GetComponent<LineRenderer>();
        this._originalColor = this._wallRenderer.material.color;

        _wallListener = new WallListener(go, item as NewWall);
        wallListeners.Add(_wallListener);

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
            foreach (var node in _wallListener.nodes)
            {
                var x = Input.GetAxis("Mouse X");
                var y = Input.GetAxis("Mouse Y");

                Vector3 moveDirection = new Vector3(x, y, 0.0f);
                moveDirection = Quaternion.AngleAxis(eventHandler._camera.transform.eulerAngles.z, Vector3.forward) * moveDirection;
                moveDirection *= eventHandler._camera.orthographicSize / 10.0f;

                var pos = new Vector3(node.Value.nodeGO.transform.position.x + moveDirection.x * HarnessConstant.MOVEMENT_SENSITIVITY, node.Value.nodeGO.transform.position.y + moveDirection.y * HarnessConstant.MOVEMENT_SENSITIVITY, -0.2f);
                node.Value.DragStart(pos);
            }
        }
    }

    public void Hovered()
    {
        if (!HarnessEventHandler.selected && !CreatorUIController.isInputOverVisualElement())
        {
            Highlight();
            foreach (var node in _wallListener.nodes)
            {
                node.Value.HandleHovered();
            }
        }
    }

    public void Exit()
    {
        HarnessEventHandler.selected = false;
        RemoveHighlight();
        foreach (var node in _wallListener.nodes)
        {
            node.Value.HandleExitWall();
        }
    }

    public void Released()
    {
        HarnessEventHandler.selected = false;
        foreach (var node in _wallListener.nodes)
        {
            node.Value.HandlReleaseWall();
        }
        var line = _wallListener.wallGO.GetComponent<LineRenderer>();
        var pos0 = line.GetPosition(0);
        var pos1 = line.GetPosition(1);

        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

        NewBuildingController.UpdateWall(_wallListener.wallItem.name, pos0, pos1, angle);

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
}