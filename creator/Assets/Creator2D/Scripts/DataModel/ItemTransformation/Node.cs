using UnityEngine;
using System;
using System.Collections.Generic;

public class Node
{
    public static Dictionary<GameObject, Node> allNodeList = new Dictionary<GameObject, Node>();
    public HarnessEventHandler eventHandler;
    public Action<Node, GameObject> onNodeHovered;
    public Action<Node> onNodeExit;
    public Action<Vector3, Node, GameObject> onNodeDrag;
    public Action<Node> onNodeReleased;
    public GameObject nodeGO;
    private Renderer _renderer;
    private Color _originalColor;
    private LayerMask _layerMask;
    public bool _isMoving = false;

    public Node(int index, GameObject parentGO)
    {
        this.nodeGO = GameObject.CreatePrimitive(PrimitiveType.Quad);

        var wallRenderer = parentGO.GetComponent<LineRenderer>();
        var points = wallRenderer.GetPosition(index);

        _renderer = this.nodeGO.GetComponent<MeshRenderer>();
        _renderer.material = Resources.Load("Materials/handles", typeof(Material)) as Material;
        _renderer.material.color = HarnessConstant.DEFAULT_NODE_COLOR;
        this._originalColor = HarnessConstant.DEFAULT_NODE_COLOR;

        this.nodeGO.transform.SetParent(parentGO.transform);
        this.nodeGO.tag = "Node";
        this.nodeGO.layer = LayerMask.NameToLayer("Node");
        this.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
        this.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
        this.nodeGO.name = $"{index}_{parentGO.name}_{ObjectName.RESIZE_HARNESS}";
        this.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);

        allNodeList.Add(this.nodeGO, this);

        eventHandler = this.nodeGO.GetComponent<HarnessEventHandler>() == null ? this.nodeGO.AddComponent<HarnessEventHandler>() : this.nodeGO.GetComponent<HarnessEventHandler>();
        eventHandler.mouseHover += HandleHovered;
        eventHandler.mouseExit += HandleExit;
        eventHandler.drag += DragStart;
        eventHandler.mouseUp += HandleReleased;

        this._layerMask.value = 1 << this.nodeGO.layer;
    }

    public void HandleHovered()
    {
        HighLight();
        GameObject attachableTo = CheckCast();
        onNodeHovered?.Invoke(this, attachableTo);
    }

    public void DragStart(Vector3 data)
    {
        CheckCast();
        GameObject attachableTo = CheckCast();
        if (eventHandler.isInsideCanvas())
            onNodeDrag?.Invoke(data, this, attachableTo);
    }

    public void HandleExit()
    {
        RemoveHighLight();
        onNodeExit?.Invoke(this);
    }

    public void HandleExitWall()
    {
        RemoveHighLight();
    }

    public void HandlReleaseWall()
    {
        RemoveHighLight();
    }

    public void HandleReleased()
    {
        RemoveHighLight();
        onNodeReleased?.Invoke(this);
        AdjustAttachedObjects();
    }

    public void AdjustAttachedObjects()
    {
        var parent = this.nodeGO.transform.parent;
        var wall = parent.GetComponent<LineRenderer>();
        var bound = wall.bounds;
        Trace.Log($"AdjustAttachedObjects :: {parent.childCount}");

        for (int i = 2; i < parent.childCount; i++)
        {
            var GO = parent.GetChild(i).gameObject;
            var object_bound = GO.GetComponent<SpriteRenderer>().bounds;

            Trace.Log($"ADJUST {GO.name} :: {GO.transform.localPosition.x} :: {Vector3.Distance(bound.max, bound.min)} :: {object_bound.size.x * 1.2f}");
            if (GO.transform.localPosition.x > Vector3.Distance(bound.max, bound.min) - (object_bound.size.x * 1.2f))
            {
                CreatorItem item = CreatorItemFinder.FindItemWithGameObject(GO);
                if (item is NewWindow || item is NewDoor)
                {
                    Trace.Log($"DeleteItem :: {item.name}");
                    NewBuildingController.DeleteItem(item.name);
                }
            }
        }
    }

    public GameObject IsAttachableTo()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.tag == "Node")
            {
                if (hit.collider.name != this.nodeGO.name)
                {
                    return hit.collider.gameObject;
                }
            }
        }
        return null;
    }

    public GameObject CheckCast()
    {
        Collider[] hitColliders = Physics.OverlapBox(this.nodeGO.transform.position, this.nodeGO.transform.localScale, Quaternion.identity, _layerMask, QueryTriggerInteraction.Collide);
        if (hitColliders.Length > 0)
        {
            foreach (var hit in hitColliders)
            {
                if (hit.tag == "Node" && hit.name != this.nodeGO.name)
                {
                    return hit.gameObject;
                }
            }
        }
        return null;
    }

    public void HighLight()
    {
        var lineRenderer = this.nodeGO.transform.parent.GetComponent<LineRenderer>();

        this.nodeGO.transform.localScale = new Vector3((lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, (lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, 0.05f);
        this.nodeGO.transform.position = new Vector3(this.nodeGO.transform.position.x, this.nodeGO.transform.position.y, HarnessConstant.HOVER_NODE_ZOFFSET);
        this._renderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
    }

    public void RemoveHighLight()
    {
        var lineRenderer = this.nodeGO.transform.parent.GetComponent<LineRenderer>();

        this.nodeGO.transform.localScale = new Vector3((lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (lineRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
        this.nodeGO.transform.position = new Vector3(this.nodeGO.transform.position.x, this.nodeGO.transform.position.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
        this._renderer.material.color = this._originalColor;
    }
}