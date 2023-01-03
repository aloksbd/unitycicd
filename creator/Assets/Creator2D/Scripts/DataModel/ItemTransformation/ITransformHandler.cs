using UnityEngine;

public interface ITransformHandler
{
    void DragStart(Vector3 data);
    void Dragged(Vector3 data);
    void Released(Vector3 data);
    void Hovered();
    void Exit();
    void Highlight();
    void RemoveHighlight();
}