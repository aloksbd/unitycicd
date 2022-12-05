using UnityEngine;

public interface NewIFlipable : NewIScalable
{
    void FlipHorizontal();
    void FlipVertical();
    Vector3 GetAdjustedPositionFor3D();
}