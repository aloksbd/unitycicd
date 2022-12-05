using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class HarnessRotateManipulator
{
  private GameObject Parent;
  private GameObject ParentItem;
  private Transform transform;
  public HarnessRotateManipulator current;
  public HarnessManipulator manipulator;
  public Vector3 currentCursorPosition;
  private const string drag_harness = "Icons/RotateHarness";
  public event Action ObjectRotated;
  public HarnessRotateManipulator(GameObject item, GameObject ParentComponent)
  {
    if (item.GetComponent<HarnessManipulator>() == null)
    {
      item.AddComponent<HarnessManipulator>();
    }

    manipulator = item.GetComponent<HarnessManipulator>();
    manipulator.current_cursor_path = drag_harness;

    manipulator.mouseDown += RotateParent;
    manipulator.mouseDrag += RotateParent;
    manipulator.mouseUp += RotateParent;

    current = this;
    Parent = item;
    ParentItem = ParentComponent;
  }

  public void RotateParent(Vector3 data)
  {
    if (currentCursorPosition != data)
    {
      GetRotatedAngle(data);
      currentCursorPosition = data;
    }
  }

  public void GetRotatedAngle(Vector3 endPosition)
  {
    Vector3 itemPosition = Parent.transform.position;
    var renderer = ParentItem.GetComponent<Renderer>();
    
    // var weakRotation = Parent.GetComponent<ObjectModel.IHasRotation>();
    // WeakRotation.IsAlive check
    // See in building canvas
    // Set value through the functions of the IRotation, and so on
    // if(weakRotation.i)

    var bounds = renderer.bounds;

    if (itemPosition != endPosition)
    {
      float transformationAngle = Vector3.Angle(endPosition, itemPosition);
      ParentItem.transform.RotateAround(bounds.center, Vector3.forward, transformationAngle);
      if (ObjectRotated != null)
      {
        ObjectRotated();
      }
    }
  }
}