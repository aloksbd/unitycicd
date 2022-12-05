using System;
using UnityEngine;
using UnityEngine.EventSystems;
public class HarnessDragManipulator
{
    public HarnessDragManipulator current;

    public GameObject ParentItem;
    public GameObject Parent;
    public HarnessManipulator manipulator;
    private const string drag_harness = "Icons/drag_icon";
    private static int lengthOfLineRenderer = 2;
    public HarnessDragManipulator(GameObject item, GameObject parent)
    {
        if (item.GetComponent<HarnessManipulator>() == null)
        {
            item.AddComponent<HarnessManipulator>();
        }

        manipulator = item.GetComponent<HarnessManipulator>();
        manipulator.current_cursor_path = drag_harness;

        // manipulator.mouseDown += MoveParent;
        manipulator.mouseDrag += MoveParent;
        // manipulator.mouseUp += MoveParent;

        current = this;
        ParentItem = item;
        Parent = parent;
    }
    public event Action ObjectDragged;
    public void MoveParent(Vector3 data)
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        if (line2d != null)
        {
            Vector3 position0 = line2d.GetPosition(0);
            if (data != new Vector3(position0.x, position0.y, -1.0f))
            {
                Parent.transform.position = new Vector3(data.x, data.y, -0.2f);
                MoveLineRender(data);
                if (ObjectDragged != null)
                {
                    ObjectDragged();
                }
            }
        }
        else
        {
            Parent.transform.position = new Vector3(data.x, data.y, -0.2f);
            if (ObjectDragged != null)
            {
                ObjectDragged();
            }
        }
    }

    public void MoveLineRender(Vector3 data)
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        var position0 = line2d.GetPosition(0);
        var position1 = line2d.GetPosition(1);
        var points = new Vector3[lengthOfLineRenderer];
        for (int i = 0; i < lengthOfLineRenderer; i++)
        {
            if (i == 0)
            {
                points[i] = new Vector3(data.x, data.y, -0.2f);
            }
            else
            {
                points[i] = new Vector3(data.x + (position1.x - position0.x), data.y, -0.2f);
            }
        }

        line2d.SetPositions(points);
        // ParentItem.transform.position = points[0];

    }
}