using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarnessObjectEvent : MonoBehaviour
{
  public static HarnessObjectEvent current;
  private void Awake()
  {
    current = this;
  }

  public event Action onMouseDowned;
  public event Action onMouseDragged;
  public event Action onMouseReleased;

  public void mouseDown()
  {
    if (onMouseDowned != null)
    {
      onMouseDowned();
    }
  }

  public void mouseDragged()
  {
    if (onMouseDragged != null)
    {
      onMouseDragged();
    }
  }

  public void mouseRelease()
  {
    if (onMouseReleased != null)
    {
      onMouseReleased();
    }
  }
}