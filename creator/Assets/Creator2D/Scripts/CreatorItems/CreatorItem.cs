using UnityEngine;
using System.Collections.Generic;
using System;

public class CreatorItem : IRenamable, NewIHasPosition, UIItemDelegate, NewISelectable
{
    public GameObject gameObject;
    private Guid _id;
    public Guid Id { get => _id; }
    public UIItem uiItem;
    private string _name;
    private Vector3 _position;
    public Vector3 Position { get => _position; }
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => _isSelected = value; }
    public string name { get => _name; }

    public List<CreatorItem> children = new List<CreatorItem>();
    public CreatorItem Parent;

    public CreatorItem(GameObject gameObject, UIItem itemUI)
    {
        this._id = Guid.NewGuid();
        this.uiItem = itemUI;
        this.gameObject = gameObject;
    }

    public void SetId(Guid id)
    {
        _id = id;
        uiItem.SetId(id);
    }

    public void SetName(string name)
    {
        _name = name;
        gameObject.name = _id.ToString();
        uiItem.SetName(name);
    }

    public void AddChild(CreatorItem child)
    {
        children.Add(child);
        child.Parent = this;
        child.gameObject.transform.parent = gameObject.transform;
        uiItem.Foldout.Add(child.uiItem.Foldout);
    }

    public void RemoveFromParent()
    {
        if (Parent != null)
        {
            Parent.children.Remove(this);
            Parent.uiItem.Foldout.Remove(this.uiItem.Foldout);
        }
        UnityEngine.Object.Destroy(gameObject);
        uiItem.Foldout.RemoveFromHierarchy();
    }

    public void SetPosition(Vector3 position)
    {
        // gameObject.transform.position = position;
        _position = position;
    }

    public void MoveBy(Vector3 vector)
    {
        gameObject.transform.position += vector;
    }

    public virtual void Select()
    {
        _isSelected = true;
    }

    public virtual void Deselect()
    {
        _isSelected = false;
    }

    /// <summary>
    /// Method <c>Destroy</c> removes its references from children and parent.
    /// </summary>
    public void Destroy()
    {
        RemoveFromParent();
        foreach (var child in children)
        {
            child.Parent = null;
        }
    }

    public virtual Interface GetComponent<Interface>()
    {
        try
        {
            return (Interface)(object)this;
        }
        catch
        {
            throw new InvalidCastException();
        }
    }

    public virtual CreatorItem Clone()
    {
        CreatorItem item = new CreatorItem(GameObject.Instantiate(gameObject), new UIItem(_name));
        item.SetName(_name);
        CloneChildren(item);
        return item;
    }

    protected virtual void CloneChildren(CreatorItem clone)
    {
        foreach (var child in children)
        {
            clone.AddChild(child.Clone());
        }
    }

    public void OnCLick()
    {
        if (_isSelected) Deselect();
        else Select();
    }
}

public class CreatorFloorPlanItem : CreatorItem, NewIHasDimension
{
    private Dimension _dimension;
    public Dimension Dimension => _dimension;

    public CreatorFloorPlanItem(GameObject gameObject, UIItem itemUI) : base(gameObject, itemUI)
    {
        this.IsSelected = true;
    }
    public override void Select()
    {
        base.Select();
        var setFloorPlanCommand = new SetCurrentFloorPlanCommand(this.name, NewBuildingController.CurrentFloorPlan() != null ? NewBuildingController.CurrentFloorPlan().name : "", true);
        var multiCommand = new MultipleCommand(new List<ICommand>() { setFloorPlanCommand });
        NewUndoRedo.AddAndExecuteCommand(multiCommand);
    }

    public override void Deselect()
    {
        base.Deselect();
        var setFloorPlanCommand = new SetCurrentFloorPlanCommand(this.name, null, false);
        var multiCommand = new MultipleCommand(new List<ICommand>() { setFloorPlanCommand });
        NewUndoRedo.AddAndExecuteCommand(multiCommand);
    }

    public void SetDimension(float length, float height, float width)
    {
        _dimension = new Dimension(length, height, width);
    }
}