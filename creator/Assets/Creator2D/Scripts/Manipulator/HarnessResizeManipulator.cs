using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class HarnessResizeManipulator
{
    private GameObject Parent;
    private GameObject ParentItem;
    public HarnessResizeManipulator current;
    public HarnessManipulator manipulator;
    public Vector3 OldCursorPositon;
    private const string up_down_harness = "Icons/UpDownHarness";
    private const string left_right_harness = "Icons/LeftRightHarness";
    private const string corner_harness = "Icons/CornerHarness";
    private string harnessName;

    private Vector3 dragStartPosition;
    public HarnessResizeManipulator(GameObject item, GameObject ActualParent, string name)
    {
        if (item.GetComponent<HarnessManipulator>() == null)
        {
            item.AddComponent<HarnessManipulator>();
        }

        manipulator = item.GetComponent<HarnessManipulator>();
        if (name == ObjectName.TOP_RESIZE_NAME || name == ObjectName.BOTTOM_RESIZE_NAME)
        {
            manipulator.current_cursor_path = up_down_harness;
        }
        else if (name == ObjectName.LEFT_RESIZE_NAME || name == ObjectName.RIGHT_RESIZE_NAME)
        {
            manipulator.current_cursor_path = left_right_harness;
        }
        else
        {
            manipulator.current_cursor_path = corner_harness;
        }

        manipulator.mouseDrag += ResizeParent;

        current = this;
        Parent = item;
        harnessName = name;
        ParentItem = ActualParent;
    }
    public event Action ParentResized;

    public void ResizeParent(Vector3 data)
    {
        LineRenderer line2d = ParentItem.GetComponent<LineRenderer>();
        if (line2d != null)
        {
            Vector3 position0 = line2d.GetPosition(0);
            Vector3 position1 = line2d.GetPosition(1);
            var boundingValues = line2d.GetComponent<Renderer>();
            Vector3 center = boundingValues.bounds.center;
            Vector3 extents = boundingValues.bounds.extents;

            if (OldCursorPositon != data)
            {
                OldCursorPositon = data;

                if (harnessName == ObjectName.TOP_RESIZE_NAME)
                {
                    var newMultiplier = (data.y - center.y - extents.y) * 2;
                    line2d.widthMultiplier = Mathf.Abs(newMultiplier) * 1.0f;
                    Vector3 originalPositon = Parent.transform.position;
                    Parent.transform.position = new Vector3(originalPositon.x, data.y, originalPositon.z);
                }
                if (harnessName == ObjectName.BOTTOM_RESIZE_NAME)
                {
                    var newMultiplier = (data.y + center.y + extents.y) * 2;
                    line2d.widthMultiplier = MathF.Abs(newMultiplier) * 1.0f;
                    Vector3 originalPositon = Parent.transform.position;
                    Parent.transform.position = new Vector3(originalPositon.x, data.y, originalPositon.z);
                }

                if (harnessName == ObjectName.LEFT_RESIZE_NAME)
                {
                    line2d.SetPosition(0, new Vector3(data.x, position0.y, position0.z));
                    Parent.transform.position = new Vector3(data.x, position0.y, position0.z);

                }

                if (harnessName == ObjectName.RIGHT_RESIZE_NAME)
                {
                    line2d.SetPosition(1, new Vector3(data.x, position1.y, position1.z));
                    Parent.transform.position = new Vector3(data.x, position1.y, position1.z);
                }

                if (ParentResized != null)
                {
                    ParentResized();
                }
            }
        }
        else
        {
            if (OldCursorPositon != data)
            {
                float width = data.x - ParentItem.transform.position.x;
                float height = data.y - ParentItem.transform.position.y;

                if (harnessName == ObjectName.TOP_RESIZE_NAME || harnessName == ObjectName.BOTTOM_RESIZE_NAME)
                {
                    ParentItem.transform.localScale = new Vector3(ParentItem.transform.localScale.x, height, ParentItem.transform.localScale.z);
                }


                if (harnessName == ObjectName.RIGHT_RESIZE_NAME || harnessName == ObjectName.LEFT_RESIZE_NAME)
                {
                    ParentItem.transform.localScale = new Vector3(width, ParentItem.transform.localScale.y, ParentItem.transform.localScale.z);
                }
            }

            if (ParentResized != null)
            {
                ParentResized();
            }
        }
    }
}