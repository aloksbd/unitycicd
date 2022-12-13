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
        _renderer.material.color = HarnessConstant.DEFAULT_NODE_COLOR;
        this._originalColor = HarnessConstant.DEFAULT_NODE_COLOR;
        this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);

        eventHandler = go.AddComponent<HarnessEventHandler>();

        eventHandler.mouseHover += HandleHovered;
        eventHandler.mouseExit += HandleExit;
        eventHandler.drag += DragStart;
        eventHandler.mouseUp += HandleReleased;
    }

    public void HandleHovered()
    {
        HighLight();
    }

    public void HandleExit()
    {
        RemoveHighLight();
    }

    public void DragStart(Vector3 data)
    {
        if (eventHandler.isInsideCanvas())
        {
            HarnessEventHandler.selected = true;

            this._wallRenderer.SetPosition(this._position, new Vector3(data.x, data.y, -0.2f));
            this._handleGO.transform.position = new Vector3(data.x, data.y, HarnessConstant.HOVER_NODE_ZOFFSET);
            _data = data;
        }
    }

    public void HandleReleased()
    {
        var pos0 = this._wallRenderer.GetPosition(0);
        var pos1 = this._wallRenderer.GetPosition(1);

        float angle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * 180 / Mathf.PI;

        //NewBuildingController.UpdateWallHandle(this._item.name, this._handleGO, this._position, pos0, pos1, angle);

        HarnessEventHandler.selected = false;
    }


    public void HighLight()
    {
        if (this._wallRenderer != null)
        {
            this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.HOVER_NODE_ZOFFSET);
            this._handleGO.transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.HOVER_NODE_SIZE, 0.05f);
            this._renderer.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        }
    }

    public void RemoveHighLight()
    {
        if (this._wallRenderer != null)
        {
            this._handleGO.transform.position = new Vector3(this._handleGO.transform.position.x, this._handleGO.transform.position.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);

            this._handleGO.transform.localScale = new Vector3((this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (this._wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
            this._renderer.material.color = this._originalColor;
        }
    }

}