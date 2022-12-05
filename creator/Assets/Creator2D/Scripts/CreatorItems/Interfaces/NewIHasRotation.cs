using UnityEngine;

public interface NewIHasRotation
{
    Vector3 EulerAngles { get; }
    void RotateBy(float x, float y, float z);
    void SetRotation(float x, float y, float z);
}