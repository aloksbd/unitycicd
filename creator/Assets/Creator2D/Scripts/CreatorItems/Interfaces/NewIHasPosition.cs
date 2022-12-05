using UnityEngine;

public interface NewIHasPosition
{
    Vector3 Position { get; }
    void MoveBy(Vector3 vector);
    void SetPosition(Vector3 position);
}