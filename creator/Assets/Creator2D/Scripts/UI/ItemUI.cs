using System;
using UnityEngine.UIElements;

public interface UIItemDelegate
{
    void OnCLick();
}
public class UIItem : IRenamable
{
    private Guid _id;
    public Guid Id => _id;
    private Foldout _foldout;
    public Foldout Foldout { get => _foldout; }
    private bool _value;

    public UIItemDelegate _delegate;

    public UIItem(string name, bool value = true, UIItemDelegate uIItemDelegate = null)
    {
        _foldout = CreateFoldout(name, value);
        Setup();
        _delegate = uIItemDelegate;
    }

    public UIItem(Foldout foldout)
    {
        _foldout = foldout;
        Setup();
    }

    private void Setup()
    {
        _value = _foldout.value;
        RegisterOnClick();
    }

    private Foldout CreateFoldout(string name, bool value)
    {
        Foldout foldout = new Foldout();
        foldout.name = name;
        foldout.text = name;
        foldout.value = value;
        foldout.AddToClassList("normal-font");
        return foldout;
    }

    private void RegisterOnClick()
    {
        _foldout.RegisterCallback<ClickEvent>(evt => OnClick(evt));
    }

    public void ToggleSelection(bool propagate)
    {
        // _value is used to store _foldout's value because _foldout.value reverts to previous state after exiting this function
        _value = !_value;
        _foldout.value = _value;
        _foldout.ToggleInClassList(WHCSSConstants.WHITE_BACKGROUND_COLOR);
        if (_delegate != null && propagate) _delegate.OnCLick();
    }

    private void OnClick(ClickEvent evt)
    {
        if (evt.target is Toggle) ToggleSelection(true);
        evt.StopPropagation();
    }

    public void SetName(string name)
    {
        _foldout.name = name;
        _foldout.text = name;
    }

    public void SetId(Guid id)
    {
        _id = id;
    }
}
