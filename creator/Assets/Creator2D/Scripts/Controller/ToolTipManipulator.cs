using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class ToolTipManipulator : Manipulator
{
	// private Tooltip tooltip;
    private VisualElement element;
	public ToolTipManipulator()
	{
		// this.tooltip = tooltip;
	}
	protected override void RegisterCallbacksOnTarget()
	{
		target.RegisterCallback<MouseEnterEvent>(MouseIn);
		target.RegisterCallback<MouseOutEvent>(MouseOut);
	}
	protected override void UnregisterCallbacksFromTarget()
	{
		target.UnregisterCallback<MouseEnterEvent>(MouseIn);
		target.UnregisterCallback<MouseOutEvent>(MouseOut);
	}
	private void MouseIn(MouseEnterEvent e)
	{
		// element.Show(target);
        if (element == null)
        {
            element = new VisualElement();
            Color bgColor = new Color(0.3f, 0.4f, 0.6f);
            element.style.backgroundColor = new StyleColor(bgColor);
            element.style.position = Position.Absolute;
            element.style.left = this.target.worldBound.center.x;
            element.style.top = this.target.worldBound.yMin;
            var label = new Label(this.target.tooltip);
            // label.style.color = Color.black;
            label.style.color = Color.white;

            element.Add(label);
            CreatorUIController.getRoot().Add(element);
        
        }
        element.style.visibility = Visibility.Visible;
        element.BringToFront();
	}
	private void MouseOut(MouseOutEvent e)
	{
		// element.Close();
        element.style.visibility = Visibility.Hidden;
	}
	// ============================================================================================================
}
