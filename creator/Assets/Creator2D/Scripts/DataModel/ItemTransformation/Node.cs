using UnityEngine;
using System;
using System.Collections.Generic;

public class Node
{
    public InputEventHandler eventHandler;
    public Action<Node> onNodeHovered;
    public Action<Node> onNodeExit;
    public Action<Vector3, Node> onNodeDrag;
    public Action<Node> onNodeReleased;
    public Action<Node> OnNodeDetach;
    public GameObject nodeGO;
    private Renderer _renderer;
    private Color _originalColor;
    private LayerMask _layerMask;
    public CreatorItem floor;
    public Vector3 offset;
    public enum NodeState
    {
        CLICKABLE,
        CLICKED
    }
    public NodeState nodeState = NodeState.CLICKABLE;


    public Node(int index, GameObject parentGO, CreatorItem floor)
    {
        this.floor = floor;
        this.nodeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);

        var wallRenderer = parentGO.GetComponent<LineRenderer>();
        var points = wallRenderer.GetPosition(index);

        _renderer = this.nodeGO.GetComponent<MeshRenderer>();
        _renderer.material = Resources.Load("Materials/handles", typeof(Material)) as Material;
        _renderer.material.color = HarnessConstant.DEFAULT_NODE_COLOR;
        this._originalColor = HarnessConstant.DEFAULT_NODE_COLOR;

        this.nodeGO.transform.SetParent(parentGO.transform);

        this.nodeGO.tag = "Node";
        this.nodeGO.layer = LayerMask.NameToLayer("Node");
        this._layerMask.value = 1 << this.nodeGO.layer;

        this.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
        this.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
        this.nodeGO.name = $"{index}_{parentGO.name}_{ObjectName.RESIZE_HARNESS}";
        this.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);

        TransformDatas.allNodeList.Add(this.nodeGO, this);

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        eventHandler = this.nodeGO.GetComponent<InputEventHandler>() == null ? this.nodeGO.AddComponent<InputEventHandler>() : this.nodeGO.GetComponent<InputEventHandler>();
        eventHandler.MouseHovered += NodeHovered;
        eventHandler.MouseExit += NodeExit;
        eventHandler.MouseDragStart += NodeDragStart;
        eventHandler.MouseDrag += NodeDragged;
        eventHandler.MouseDragEnd += NodeReleased;
        eventHandler.MouseClick += NodeClicked;
    }


    public void NodeClicked()
    {
        CreatorHotKeyController.Instance.DeselectAllItems();

        TransformDatas.SelectedNode = this;
        nodeState = NodeState.CLICKED;
        CreatorHotKeyController.Instance.hotkeyMenu.Populate(CreatorHotKeyController.Instance.nodeKeys);

        HighLight();
    }

    public void DetachNode()
    {
        OnNodeDetach?.Invoke(this);
    }

    public void NodeHovered()
    {
        if (nodeState == NodeState.CLICKABLE)
        {
            InputEventHandler.selected = true;
            HighLight();
            onNodeHovered?.Invoke(this);
        }
    }

    public void NodeDragStart(Vector3 data)
    {
        CreatorHotKeyController.Instance.DeselectAllItems();
        HighLight();
    }

    public void NodeDragged(Vector3 data)
    {
        onNodeDrag?.Invoke(data, this);
    }

    public void NodeExit()
    {
        if (nodeState == NodeState.CLICKABLE)
        {
            InputEventHandler.selected = false;
            RemoveHighLight();
            onNodeExit?.Invoke(this);
        }
    }

    public void NodeExitWall()
    {
        RemoveHighLight();
    }

    public void NodeReleaseWall()
    {
        RemoveHighLight();
    }

    public void NodeReleased(Vector3 data)
    {
        if (nodeState == NodeState.CLICKABLE)
        {
            InputEventHandler.selected = false;

            RemoveHighLight();
            onNodeReleased?.Invoke(this);
        }
    }

    public void AdjustAttachedObjects()
    {
        var parent = this.nodeGO.transform.parent;
        var wall = parent.GetComponent<LineRenderer>();
        var bound = wall.bounds;

        for (int i = 2; i < parent.childCount; i++)
        {
            var GO = parent.GetChild(i).gameObject;
            var object_bound = GO.GetComponent<SpriteRenderer>().bounds;

            if (GO.transform.localPosition.x > Vector3.Distance(bound.max, bound.min) - (object_bound.size.x * 1.2f))
            {
                CreatorItem item = CreatorItemFinder.FindItemWithGameObject(GO);
                if (item is NewWindow || item is NewDoor)
                {
                    NewBuildingController.DeleteItem(item.name);
                }
            }
        }
    }

    public void HighLight()
    {
        try
        {
            var lineRenderer = this.nodeGO.transform.parent.GetComponent<LineRenderer>();

            this.nodeGO.transform.localScale = new Vector3((lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, (lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, 0.05f);
            this.nodeGO.transform.position = new Vector3(this.nodeGO.transform.position.x, this.nodeGO.transform.position.y, HarnessConstant.HOVER_NODE_ZOFFSET);
            this._renderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
        catch
        {
            Trace.Log($"Node GO not found");
        }
    }

    public void RemoveHighLight()
    {
        try
        {
            var lineRenderer = this.nodeGO.transform.parent.GetComponent<LineRenderer>();

            this.nodeGO.transform.localScale = new Vector3((lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
            this.nodeGO.transform.position = new Vector3(this.nodeGO.transform.position.x, this.nodeGO.transform.position.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
            this._renderer.material.color = this._originalColor;
        }
        catch
        {
            Trace.Log($"Node GO not found");
        }
    }
}