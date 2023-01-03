using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class HarnessRotateManipulator
{
    private GameObject rotatorGO;
    private GameObject Parent;
    private CreatorItem Item;
    private Transform transform;
    public HarnessRotateManipulator current;
    public HarnessManipulator manipulator;
    public Vector3 currentCursorPosition;
    public event Action ObjectRotated;
    public static bool IsRotating;


    public HarnessRotateManipulator(GameObject GO, GameObject parent, CreatorItem creatorItem)
    {
        this.Item = creatorItem;
        if (GO.GetComponent<HarnessManipulator>() == null)
        {
            GO.AddComponent<HarnessManipulator>();
        }
        manipulator = GO.GetComponent<HarnessManipulator>() == null ? GO.AddComponent<HarnessManipulator>() : GO.GetComponent<HarnessManipulator>();

        manipulator.current_cursor_path = WHConstants.ROTATE_POINTER;

        manipulator.mouseEnter += Highlight;
        manipulator.mouseDown += RotateStart;
        manipulator.mouseDrag += RotateParent;
        manipulator.mouseUp += Released;
        manipulator.mouseExit += RemoveHighlight;

        current = this;

        rotatorGO = GO;
        Parent = parent;
    }

    Vector3 initial;

    public void RotateStart(Vector3 data)
    {
        Highlight();
        initial = data;
    }

    public void RotateParent(Vector3 data)
    {
        Highlight();
        RotateObject(data);
    }

    public float RotateObject(Vector3 endPosition)
    {
        float angle = Mathf.Atan2(endPosition.y - initial.y, endPosition.x - initial.x) * 180 / Mathf.PI;
        Parent.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        return angle;
    }

    public void Released(Vector3 data)
    {
        RemoveHighlight();

        var angle = RotateObject(data);
        NewBuildingController.UpdateObjectRotation(this.Item.name, angle);
    }

    public void Highlight()
    {
        var rend = rotatorGO.GetComponent<MeshRenderer>();
        rend.material.color = HarnessConstant.HOVER_HIGHLIGHT_COLOR;
        IsRotating = true;
    }

    public void RemoveHighlight()
    {
        var rend = rotatorGO.GetComponent<MeshRenderer>();
        rend.material.color = HarnessConstant.DEFAULT_NODE_COLOR;
        IsRotating = false;
    }
}