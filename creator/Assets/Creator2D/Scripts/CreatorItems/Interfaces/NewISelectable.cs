using UnityEngine;
public interface NewISelectable
{
    bool IsSelected { get; }
    void Select();
    void Deselect();
}