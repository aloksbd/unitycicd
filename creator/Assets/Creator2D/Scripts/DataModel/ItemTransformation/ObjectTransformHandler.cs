using UnityEngine;

public class ObjectTransformHandler : ITransformHandler
{
    private InputEventHandler eventHandler;
    private GameObject _objectGO;
    public CreatorItem item;
    private SpriteRenderer _objectRenderer;
    private string _objectType;
    private Vector3 _offset;
    private GameObject BoundingBox;
    private Bounds _bounds;
    public GameObject RotatorGO;
    private Vector3 _originalScale, _hoverScale;
    private Color _originalColor;

    //This is used to instantiate the new rotator to the same position as in previous floor
    private Vector3 _previousPosition = Vector3.zero;

    public ObjectTransformHandler(GameObject go, CreatorItem item, string type, bool isClone = false)
    {
        this._objectGO = go;
        this.item = item;
        this._objectRenderer = go.GetComponent<SpriteRenderer>();

        this._originalColor = this._objectRenderer.color;
        this._originalScale = this._objectGO.transform.localScale;
        this._hoverScale = new Vector3(this._originalScale.x + 0.05f, this._originalScale.y + 0.05f, 1f);

        eventHandler = go.GetComponent<InputEventHandler>() == null ? go.AddComponent<InputEventHandler>() : go.GetComponent<InputEventHandler>();

        eventHandler.MouseDrag += Dragged;
        eventHandler.MouseHovered += Hovered;
        eventHandler.MouseExit += Exit;
        eventHandler.MouseDragStart += DragStart;
        eventHandler.MouseDragEnd += Released;
        eventHandler.MouseClick += Clicked;

        this._objectType = type;

        var rotator = this._objectGO.transform.Find("RotateHarness");
        if (rotator != null)
        {
            _previousPosition = rotator.transform.position;
            GameObject.Destroy(rotator.gameObject);
        }
        AttachRotateHarness();
    }

    public enum ObjectState
    {
        CLICKABLE,
        CLICKED
    }
    public ObjectState objectState;

    public void Clicked()
    {
        Trace.Log($"CLICKED");
        CreatorHotKeyController.Instance.DeselectAllItems();
        TransformDatas.SelectedObject = this;

        objectState = ObjectState.CLICKED;
        CreatorHotKeyController.Instance.hotkeyMenu.Populate(CreatorHotKeyController.Instance.objectKeys);
        Highlight();
    }

    public void DragStart(Vector3 data)
    {
        Highlight();
        InputEventHandler.selected = true;

        _offset = this._objectGO.transform.position - data;
    }

    public void Dragged(Vector3 data)
    {
        if (InputEventHandler.CursorInsideCanvas())
        {
            this._objectGO.transform.position = data + _offset;
        }
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
        if (objectState == ObjectState.CLICKABLE)
        {
            InputEventHandler.selected = false;
            RemoveHighlight();
        }
    }

    public void Released(Vector3 data)
    {
        InputEventHandler.selected = false;

        NewBuildingController.UpdateObjectPosition(this.item.name, this._objectGO.transform.position);
        RemoveHighlight();
    }

    public void Highlight()
    {
        if (this._objectRenderer != null)
        {
            this._objectRenderer.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
            this._objectGO.transform.localScale = this._hoverScale;
        }
    }

    public void RemoveHighlight()
    {
        if (this._objectRenderer != null)
        {
            this._objectRenderer.color = this._originalColor;
            this._objectGO.transform.localScale = this._originalScale;
        }
    }

    private void AttachRotateHarness()
    {
        if (objectState == ObjectState.CLICKABLE)
        {
            var renderer = this._objectGO.GetComponent<Renderer>();
            var bounds = renderer.bounds;

            RotatorGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var rend = RotatorGO.GetComponent<MeshRenderer>();
            rend.material = Resources.Load("Materials/handles", typeof(Material)) as Material;
            rend.material.color = HarnessConstant.DEFAULT_NODE_COLOR; ;

            RotatorGO.transform.position = _previousPosition != Vector3.zero ? _previousPosition : new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y + HarnessConstant.ROTATOR_SIZE + HarnessConstant.HARNESS_SPACE, bounds.center.z);

            RotatorGO.transform.localScale = new Vector3(HarnessConstant.ROTATOR_SIZE, HarnessConstant.ROTATOR_SIZE, HarnessConstant.ROTATOR_SIZE);
            RotatorGO.name = ObjectName.ROTATE_HARNESS;
            HarnessRotateManipulator rotateManipulator = new HarnessRotateManipulator(RotatorGO, _objectGO, item);

            RotatorGO.transform.parent = _objectGO.transform;
        }
    }

    public void ShowRotateHarness(bool show)
    {
        RotatorGO.SetActive(show);
    }
}