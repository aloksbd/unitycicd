using UnityEngine;
using ObjectModel;
using System.Collections.Generic;

public class TransformHandle
{
    private HarnessEventHandler eventHandler;
    private GameObject _handleGO;
    private Renderer _renderer;
    private LineRenderer _wallRenderer;
    private NewWall _item;
    private int _position;
    private Camera _camera;
    private Vector3 _data;
    private Color _originalColor;


    public TransformHandle(GameObject go, NewWall item, LineRenderer line, int pos)
    {
        this._handleGO = go;
        this._wallRenderer = line;
        this._position = pos;
        this._item = item;

        _renderer = go.GetComponent<MeshRenderer>();
        _renderer.material = Resources.Load("Materials/handles", typeof(Material)) as Material;
        _renderer.material.color = HarnessConstant.DEFAULT_HANDLE_COLOR;
        this._originalColor = HarnessConstant.DEFAULT_HANDLE_COLOR;
        this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.DEFAULT_HANDLE_ZOFFSET);

        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        Camera _camera = cam.GetComponent<Camera>();

        eventHandler = go.AddComponent<HarnessEventHandler>();

        eventHandler.mouseHover += HandleHovered;
        eventHandler.mouseExit += HandleExit;
        eventHandler.drag += DragStart;
        eventHandler.mouseUp += HandleReleased;
    }

    public void HandleHovered()
    {
        Trace.Log("HandleHovered");
        HighLight();
    }

    public void HandleExit()
    {
        Trace.Log("HandleExit");
        RemoveHighLight();
    }

    public void DragStart(Vector3 data)
    {
        Trace.Log($"DragStart");
        HarnessEventHandler.selected = true;
        _data = data;

        HarnessEventHandler.selected = true;

        this._wallRenderer.SetPosition(this._position, new Vector3(data.x, data.y, -0.2f));
        this._handleGO.transform.position = new Vector3(data.x, data.y, HarnessConstant.HOVER_HANDLE_ZOFFSET);
        _data = data;

    }

    public void HandleReleased()
    {
        var pos0 = this._wallRenderer.GetPosition(0);
        var pos1 = this._wallRenderer.GetPosition(1);
        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

        Update3DPos(pos0, pos1, angle);
        Update2DPos(pos0, pos1, angle);

        this._handleGO.transform.position = new Vector3(_data.x, _data.y, HarnessConstant.DEFAULT_HANDLE_ZOFFSET);

        HarnessEventHandler.selected = false;
    }

    private void Update3DPos(Vector3 pos0, Vector3 pos1, float angle)
    {
        this._item.SetDimension(Vector3.Distance(pos0, pos1), WHConstants.DefaultWallHeight, WHConstants.DefaultWallBreadth);
        this._item.SetPosition(pos0);
        this._item.SetRotation(0, -angle, 0);
    }

    private void Update2DPos(Vector3 pos0, Vector3 pos1, float angle)
    {
        GameObject parent = this._handleGO.transform.parent.gameObject;

        var length = Vector3.Distance(pos0, pos1);
        float lineWidth = this._wallRenderer.endWidth;

        BoxCollider lineCollider = parent.GetComponent<BoxCollider>();
        lineCollider.transform.parent = this._wallRenderer.transform;
        lineCollider.center = new Vector3(length / 2, 0.0f, 0.0f);
        lineCollider.size = new Vector3(length, lineWidth, 1f);

        parent.transform.position = pos0;
        parent.transform.eulerAngles = new Vector3(0, 0, angle);

        if (this._position == 0)
            FindSibilingHandle();
    }

    public void FindSibilingHandle()
    {
        var parent = this._handleGO.transform.parent.gameObject;
        var sibiling = parent.transform.GetChild(1).gameObject;

        var points = this._wallRenderer.GetPosition(1);
        sibiling.transform.parent = this._wallRenderer.transform;

        sibiling.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_HANDLE_ZOFFSET);
        sibiling.transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_HANDLE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_HANDLE_SIZE, 0.05f);
        sibiling.transform.rotation = new Quaternion(0, 0, 0, 0);
    }

    public void HighLight()
    {
        if (this._wallRenderer != null)
        {
            this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.HOVER_HANDLE_ZOFFSET);
            this._handleGO.transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_HANDLE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_HANDLE_SIZE, 0.05f);
            this._renderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
    }

    public void RemoveHighLight()
    {
        if (this._wallRenderer != null)
        {
            this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.DEFAULT_HANDLE_ZOFFSET);

            this._handleGO.transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_HANDLE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_HANDLE_SIZE, 0.05f);
            this._renderer.material.color = this._originalColor;
        }
    }

}