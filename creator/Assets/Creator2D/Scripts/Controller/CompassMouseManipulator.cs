using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassMouseManipulator : PointerManipulator
{
    public CompassMouseManipulator()
    {
    }
    protected override void RegisterCallbacksOnTarget()
    {
        // target.RegisterCallback<PointerDownEvent>(OnMouseDown);
        target.RegisterCallback<PointerDownEvent>(OnPointerDownCompass, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(OnPointerUpCompass);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMoveCompass);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDownCompass);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUpCompass);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveCompass);
    }

    private bool compassMove = false;
    private Vector3 PointerDownPoint;
    private void OnPointerDownCompass(PointerDownEvent evt)
    {
        compassMove = true;
        PointerDownPoint = evt.position;
    }

    private void OnPointerUpCompass(PointerUpEvent evt)
    {
        compassMove = false;
    }

    private void OnPointerMoveCompass(PointerMoveEvent evt)
    {
        if (!compassMove) return;
        Vector3 difference = (PointerDownPoint - evt.position);
        if (Math.Abs(difference.x) > target.worldBound.width * 2) return;
        Quaternion currentRotation = target.transform.rotation;
        target.transform.rotation = Quaternion.AngleAxis(difference.x, Vector3.forward);
        Camera.main.transform.rotation = Quaternion.AngleAxis(difference.x, Vector3.forward);
    }

}