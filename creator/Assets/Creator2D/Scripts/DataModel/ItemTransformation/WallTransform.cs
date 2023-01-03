using UnityEngine;
using System.Collections.Generic;
using ObjectModel;

public class WallTransform : ITransformHandler
{
    public InputEventHandler eventHandler;
    private WallListener _wallListener;
    private LineRenderer _wallRenderer;
    private Color _originalColor;
    public WallListener wallListener
    {
        private set { _wallListener = value; }
        get { return _wallListener; }
    }

    public CreatorItem WallItem
    {
        get { return this._wallListener.wallItem; }
    }

    public GameObject WallGO
    {
        get { return this._wallListener.wallGO; }
    }

    public enum WallState
    {
        CLICKABLE,
        CLICKED
    }
    public WallState wallState;

    public WallTransform(GameObject go, CreatorItem item)
    {
        this._wallRenderer = go.GetComponent<LineRenderer>();
        this._originalColor = this._wallRenderer.material.color;

        wallListener = new WallListener(go, item as NewWall);

        TransformDatas.wallListenersList.Add(go, wallListener);
        wallState = WallState.CLICKABLE;

        RegisterEvents(go);
    }

    private void RegisterEvents(GameObject go)
    {
        eventHandler = go.GetComponent<InputEventHandler>() == null ? go.AddComponent<InputEventHandler>() : go.GetComponent<InputEventHandler>();

        eventHandler.MouseHovered += Hovered;
        eventHandler.MouseClick += WallClicked;
        eventHandler.MouseDragStart += DragStart;
        eventHandler.MouseDrag += Dragged;
        eventHandler.MouseDragEnd += Released;
        eventHandler.MouseExit += Exit;
    }

    public void Hovered()
    {
        if (IsInteractable())
        {
            if (wallState == WallState.CLICKABLE)
            {
                if (!InputEventHandler.selected && !CreatorUIController.isInputOverVisualElement())
                {
                    Highlight();
                    foreach (var node in wallListener.nodes)
                    {
                        node.Value.NodeHovered();
                    }
                }
            }
        }
    }

    public void WallClicked()
    {
        if (IsInteractable())
        {
            CreatorHotKeyController.Instance.DeselectAllItems();

            TransformDatas.SelectedWall = this;
            wallState = WallState.CLICKED;

            Highlight();
            foreach (var node in wallListener.nodes)
            {
                node.Value.nodeState = Node.NodeState.CLICKED;
            }

            CreatorHotKeyController.Instance.hotkeyMenu.Populate(CreatorHotKeyController.Instance.wallKeys);
        }
    }

    public bool IsInteractable()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();

        if (buildingInventoryController.currentBlock == null)
        {
            return true;
        }
        else if (buildingInventoryController.currentBlock.AssetType == "Wall")
        {
            return true;
        }
        Trace.Log($"{buildingInventoryController.currentBlock.AssetType}");
        return false;
    }

    public void DragStart(Vector3 data)
    {
        CreatorHotKeyController.Instance.DeselectAllItems();
        InputEventHandler.selected = true;

        //Determine the offset of each moving node from the mouse position
        foreach (var node in wallListener.nodes)
        {
            node.Value.offset = node.Value.nodeGO.transform.position - data;
        }
    }

    public void Dragged(Vector3 data)
    {
        //if (eventHandler.isInsideCanvas())
        {
            Highlight();
            foreach (var node in wallListener.nodes)
            {
                //Set the position of each node as per the mouse position
                var pos = data + node.Value.offset;
                node.Value.NodeDragged(pos);
            }
        }
    }

    public void Exit()
    {
        if (wallState == WallState.CLICKABLE)
        {
            InputEventHandler.selected = false;
            RemoveHighlight();
            foreach (var node in wallListener.nodes)
            {
                node.Value.NodeExit();
            }
        }
    }

    public void Released(Vector3 data)
    {
        if (wallState == WallState.CLICKABLE)
        {
            InputEventHandler.selected = false;
            for (int i = 0; i < wallListener.nodes.Count; i++)
            {
                foreach (var wall in TransformDatas.wallListenersList)
                {
                    if (wall.Value.nodes.ContainsValue(wallListener.nodes[i]))
                    {
                        var line = wall.Value.wallGO.GetComponent<LineRenderer>();
                        var pos0 = line.GetPosition(0);
                        var pos1 = line.GetPosition(1);
                        var dist = Vector3.Distance(pos0, pos1);
                        var attach = pos0 != pos1 && dist > HarnessConstant.WALL_LENGTH_THRESHOLD;

                        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;
                        NewBuildingController.UpdateWallHandle(wall.Value.wallItem.name, pos0, pos1, angle, attach);
                    }
                }
                wallListener.nodes[i].NodeReleased(data);
            }
            RemoveHighlight();
        }
    }

    public void DetachWall()
    {
        NewBuildingController.DetachWall(TransformDatas.SelectedWall.wallListener.wallItem.name);
    }

    public void Highlight()
    {
        if (this._wallRenderer != null)
        {
            this._wallRenderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
            foreach (var node in wallListener.nodes)
            {
                node.Value.NodeHovered();
            }
        }
    }

    public void RemoveHighlight()
    {
        if (this._wallRenderer != null)
        {
            this._wallRenderer.material.color = this._originalColor;
            foreach (var node in wallListener.nodes)
            {
                node.Value.NodeExit();
            }
        }
    }
}