using UnityEngine;

public interface NewIScalable
{
    Vector3 Scale { get; }
    void SetScale(Vector3 scale);
    void ScaleBy(float scale);
}