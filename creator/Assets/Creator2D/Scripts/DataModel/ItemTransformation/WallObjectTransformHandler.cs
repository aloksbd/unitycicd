using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class WallObjectTransformHandler : ITransformHandler
{
    private InputEventHandler eventHandler;
    private GameObject _objectGO;
    public CreatorItem item;
    private SpriteRenderer _objectRenderer;
    private Color _originalColor;
    private string _objectType;
    private Vector3 _offset;
    private GameObject _parent;
    private LineRenderer _parent_wall;
    private Vector3 InitialPos;


    public WallObjectTransformHandler(GameObject go, CreatorItem item, string type)
    {
        this._objectGO = go;
        this.item = item;
        this._objectRenderer = go.GetComponent<SpriteRenderer>();

        this._originalColor = this._objectRenderer.color;

        eventHandler = go.GetComponent<InputEventHandler>() == null ? go.AddComponent<InputEventHandler>() : go.GetComponent<InputEventHandler>();

        eventHandler.MouseClick += Clicked;
        eventHandler.MouseDrag += Dragged;
        eventHandler.MouseHovered += Hovered;
        eventHandler.MouseExit += Exit;
        eventHandler.MouseDragStart += DragStart;
        eventHandler.MouseDragEnd += Released;

        this._objectType = type;
    }

    public enum WallObjectState
    {
        CLICKABLE,
        CLICKED
    }
    public WallObjectState wallObjectState;

    public void Clicked()
    {
        CreatorHotKeyController.Instance.DeselectAllItems();
        TransformDatas.SelectedWallObject = this;

        wallObjectState = WallObjectState.CLICKED;
        CreatorHotKeyController.Instance.hotkeyMenu.Populate(CreatorHotKeyController.Instance.objectKeys);
        Highlight();
    }
    float direction;
    public void DragStart(Vector3 data)
    {
        Highlight();
        HarnessEventHandler.selected = true;
        InitialPos = this._objectGO.transform.localPosition;
        _offset = this._objectGO.transform.localPosition - data;

        _parent = this._objectGO.transform.parent.gameObject;
        _parent_wall = _parent.GetComponent<LineRenderer>();
        direction = Mathf.Sign(_parent_wall.GetPosition(1).x - _parent_wall.GetPosition(0).x);
    }

    public void Dragged(Vector3 data)
    {
        Vector3 pos;

        //If line is Left -> Right
        if (direction > 0)
        {
            pos = data + _offset;
        }
        //If line is right -> Left
        else
        {
            pos = -data;
        }

        pos.x = Mathf.Clamp(pos.x, 0, GetMaxClamp());
        this._objectGO.transform.localPosition = new Vector3(pos.x, 0f, WHConstants.DefaultZ);
    }

    private float GetMaxClamp()
    {
        var bound = _parent_wall.bounds;
        var object_bound = _objectRenderer.bounds;

        return Vector3.Distance(bound.max, bound.min) - (object_bound.size.x * 1.2f);
    }

    public void Hovered()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();

        if (buildingInventoryController.currentBlock == null && !CreatorUIController.isInputOverVisualElement())
        {
            InputEventHandler.selected = true;
            Highlight();
        }
    }

    public void Exit()
    {
        if (wallObjectState == WallObjectState.CLICKABLE)
        {
            InputEventHandler.selected = false;
            RemoveHighlight();
        }
    }

    public void Released(Vector3 data)
    {
        if (wallObjectState == WallObjectState.CLICKABLE)
        {
            if (InputEventHandler.CheckWallObjectOverlap(this._objectGO))
            {
                Trace.Log($"REPOSITION TO INTIAL POS");
                this._objectGO.transform.localPosition = InitialPos;
            }
            else
            {
                Trace.Log($"OK");
                InputEventHandler.selected = false;
                var wallItem = this.item.Parent;
                NewBuildingController.UpdateWallObject(this.item.name, wallItem, this._objectGO.transform.position, this._objectGO.transform.localPosition, this._objectType);
                RemoveHighlight();
            }
        }
    }

    public void Highlight()
    {
        if (this._objectRenderer != null)
        {
            this._objectRenderer.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
    }

    public void RemoveHighlight()
    {
        if (this._objectRenderer != null)
        {
            this._objectRenderer.color = this._originalColor;
        }
    }
}