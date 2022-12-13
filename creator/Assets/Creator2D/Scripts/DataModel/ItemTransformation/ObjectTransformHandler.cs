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

    public void Dragged(Vector3 data)
    {
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");

        Vector3 moveDirection = new Vector3((x + y), 0.0f, 0.0f);
        moveDirection = Quaternion.AngleAxis(eventHandler._camera.transform.eulerAngles.z, Vector3.forward) * moveDirection;
        moveDirection *= eventHandler._camera.orthographicSize / 10.0f;

        var pos = new Vector3(data.x + moveDirection.x * HarnessConstant.MOVEMENT_SENSITIVITY, 0f, 0f);
        pos.x = Mathf.Clamp(pos.x, 0, GetMaxClamp());

        this._objectGO.transform.localPosition = pos;
    }

    private float GetMaxClamp()
    {
        var parent = _objectGO.transform.parent;
        var wall = parent.GetComponent<LineRenderer>();
        var bound = wall.bounds;
        var object_bound = _objectRenderer.bounds;

        return Vector3.Distance(bound.max, bound.min) - (object_bound.size.x * 1.2f);
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