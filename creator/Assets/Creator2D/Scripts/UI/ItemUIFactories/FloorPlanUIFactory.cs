using UnityEngine;
using UnityEngine.UIElements;
using ObjectModel;
using System.Collections.Generic;

public class FloorPlanUIFactory : IItemUIFactory
{
    public UIItem Create(string name)
    {
        UIItem itemUI = new UIItem(name);
        Foldout floorPlanFoldout = itemUI.Foldout;
        floorPlanFoldout.RemoveFromClassList("normal-font");
        floorPlanFoldout.AddToClassList("bold-font");
        floorPlanFoldout.AddToClassList("full-width");
        AddDeleteButton(floorPlanFoldout, itemUI);
        AddHeightElement(floorPlanFoldout, name);
        return itemUI;
    }

    private void AddDeleteButton(Foldout foldout, UIItem itemUI)
    {
        Label labelElement = foldout.Q<Label>();
        VisualElement toggleVisualElement = labelElement.parent;
        Button deleteButton = new Button();
        deleteButton.AddToClassList("floorplan-delete-button");
        deleteButton.RegisterCallback<ClickEvent>(evt =>
        {
            NewBuildingController.DeleteFloorPlan(evt, itemUI);
        });
        toggleVisualElement.Add(deleteButton);
    }

    private void AddHeightElement(Foldout foldout, string name)
    {
        foldout.AddToClassList(WHCSSConstants.PINK_BACKGROUND_COLOR);
        foldout.style.paddingLeft = 10;
        foldout.style.marginTop = 5;
        VisualElement HeightElement = new VisualElement();
        HeightElement.AddToClassList("row-container");
        HeightElement.AddToClassList("align-center");
        HeightElement.Add(CreateHeightTextField(foldout, name));
        HeightElement.Add(CreateHeightButton());
        foldout.Add(HeightElement);
    }

    private TextField CreateHeightTextField(Foldout foldout, string name)
    {
        TextField heightField = new TextField();
        heightField.AddToClassList("col-md-10");
        heightField.AddToClassList("floor-height-input"); //TODO this css is getting overriden?
        heightField.style.marginRight = 0;
        heightField.style.marginLeft = 0;
        heightField.style.paddingRight = 0;
        heightField.style.paddingLeft = 0;
        heightField.name = name + "-height";
        heightField.label = "Floor Height";
        heightField.maxLength = 5;
        heightField.value = WHConstants.DefaultFloorHeight.ToString();

        //TODO
        heightField.RegisterCallback<InputEvent>((evt) =>
        {
            var newHeight = float.Parse(evt.newData);
            var previousHeight = float.Parse(evt.previousData);
            int floorPlanNumber = NamingController.GetItemNameNumber(foldout.name);
            float adjustmentHeight = newHeight - previousHeight;
            NewBuildingController.AdjustFloorPlans(floorPlanNumber, 0, adjustmentHeight * WHConstants.FEET_TO_METER);
        });
        return heightField;
    }

    private Button CreateHeightButton()
    {
        Button heightUnit = new Button();
        heightUnit.AddToClassList("col-md-2");
        heightUnit.AddToClassList("col-xs-2");
        heightUnit.style.height = 20;
        heightUnit.style.marginLeft = 0;
        heightUnit.text = WHConstants.FeetUnit;
        return heightUnit;
    }
}
