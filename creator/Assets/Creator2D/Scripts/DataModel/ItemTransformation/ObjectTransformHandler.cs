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

        eventHandler = go.AddComponent<HarnessEventHandler>();
        eventHandler.drag += Dragged;
        eventHandler.mouseHover += Hovered;
        eventHandler.mouseExit += Exit;
        eventHandler.mouseDown += DragStart;
        eventHandler.mouseUp += Released;

        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        _camera = cam.GetComponent<Camera>();

        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();

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
        var parent = _objectGO.transform.parent;
        var wall = parent.GetComponent<LineRenderer>();
        var bound = wall.bounds;

        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");

        Vector3 moveDirection = new Vector3(x + y, 0f, 0f);

        if (parent.transform.position.x >= 0)
        {
            moveDirection = new Vector3(-(x + y), 0f, 0f);
        }
        moveDirection = Quaternion.AngleAxis(_camera.transform.eulerAngles.z, Vector3.forward) * moveDirection;

        moveDirection.y = 0f;

        if (this._objectGO.transform.localPosition.x > 0 && this._objectGO.transform.localPosition.x < (bound.size.x - this._objectRenderer.bounds.size.x))
        {
            this._objectGO.transform.localPosition += moveDirection;
        }
        else if (this._objectGO.transform.localPosition.x <= 0)
        {
            this._objectGO.transform.localPosition = new Vector3(0.1f, 0f, 0f);
        }
        else if (this._objectGO.transform.localPosition.x >= (bound.size.x - this._objectRenderer.bounds.size.x))
        {
            this._objectGO.transform.localPosition = new Vector3(bound.size.x - this._objectRenderer.bounds.size.x, this._objectGO.transform.localPosition.y, this._objectGO.transform.localPosition.z);
        }
        Update3DPos(this._objectGO.transform.position, parent.gameObject);
    }

    private void Update3DPos(Vector3 pos0, GameObject parent)
    {
        if (this._objectType == "window")
        {

            this._objectItem.SetDimension(WHConstants.DefaultWindowLength, WHConstants.DefaultWindowHeight, WHConstants.DefaultWindowBreadth);
            this._objectItem.SetPosition(new Vector3(Mathf.Abs(pos0.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultWindowY));
        }
        if (this._objectType == "door")
        {
            this._objectItem.SetDimension(WHConstants.DefaultDoorLength, WHConstants.DefaultDoorHeight, WHConstants.DefaultDoorBreadth);
            this._objectItem.SetPosition(new Vector3(Mathf.Abs(pos0.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultDoorY));
        }
        this._objectItem.SetRotation(0, 0, 0);

    }

    public void Hovered()
    {
        Trace.Log("ObjectHovered");
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        if (!HarnessEventHandler.selected && buildingInventoryController.currentBlock == null && !CreatorUIController.isInputOverVisualElement())
        {
            Highlight();
        }
    }

    public void Exit()
    {
        Trace.Log("ObjectExit");
        HarnessEventHandler.selected = false;
        RemoveHighlight();
    }

    public void Released()
    {
        Trace.Log("ObjectReleased");
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
        if (this._objectRenderer != null)
        {
            Trace.Log("HighLight Object");
            this._objectRenderer.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
    }

    public void RemoveHighlight()
    {
        if (this._objectRenderer != null)
        {
            Trace.Log("RemoveHighLight Object");
            this._objectRenderer.color = this._originalColor;
        }
    }
}