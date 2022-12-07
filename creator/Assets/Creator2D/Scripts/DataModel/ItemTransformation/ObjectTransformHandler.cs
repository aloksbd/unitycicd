using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ObjectTransformHandler : ITransformHandler
{
    private HarnessEventHandler eventHandler;
    private GameObject _objectGO;
    private NewItemWithMesh _objectItem;
    private List<GameObject> handles = new List<GameObject>();
    private SpriteRenderer _objectRenderer;
    private Color _originalColor;
    private Camera _camera;
    private string _objectType;


    public ObjectTransformHandler(GameObject go, NewItemWithMesh item, string type)
    {
        this._objectGO = go;
        this._objectItem = item;
        this._objectRenderer = go.GetComponent<SpriteRenderer>();

        this._originalColor = this._objectRenderer.color;

        eventHandler = go.GetComponent<HarnessEventHandler>() == null ? go.AddComponent<HarnessEventHandler>() : go.GetComponent<HarnessEventHandler>();

        eventHandler.drag += Dragged;
        eventHandler.mouseHover += Hovered;
        eventHandler.mouseExit += Exit;
        eventHandler.mouseDown += DragStart;
        eventHandler.mouseUp += Released;

        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        _camera = cam.GetComponent<Camera>();

        this._objectType = type;
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
    Vector3 finalPos;
    public void Dragged(Vector3 data)
    {
        var parent = _objectGO.transform.parent;
        var wall = parent.GetComponent<LineRenderer>();
        var bound = wall.bounds;

        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");

        Trace.Log($"CLAMP :: 0 {Mathf.Abs(bound.max.x) + Mathf.Abs(bound.min.x)}");

        var pos = new Vector3(data.x + (x * y) * HarnessConstant.MOVEMENT_SENSITIVITY, 0f, 0f);
        pos.x = Mathf.Clamp(pos.x, 0, Mathf.Abs(bound.max.x) + Mathf.Abs(bound.min.x));

        this._objectGO.transform.localPosition = pos;
    }

    public void Hovered()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        if (!HarnessEventHandler.selected && buildingInventoryController.currentBlock == null && !CreatorUIController.isInputOverVisualElement())
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
        NewBuildingController.UpdateObject(this._objectItem.name, this._objectGO.transform.position, this._objectGO.transform.localPosition, this._objectGO.transform.parent.gameObject, this._objectType);

        RemoveHighlight();
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