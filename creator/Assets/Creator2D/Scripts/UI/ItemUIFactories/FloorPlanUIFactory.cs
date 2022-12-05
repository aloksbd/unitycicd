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
        AddDeleteButton(floorPlanFoldout, name);
        AddHeightElement(floorPlanFoldout, name);
        return itemUI;
    }

    private void AddDeleteButton(Foldout foldout, string name)
    {
        Label labelElement = foldout.Q<Label>();
        VisualElement toggleVisualElement = labelElement.parent;
        Button deleteButton = new Button();
        deleteButton.AddToClassList("floorplan-delete-button");
        deleteButton.RegisterCallback<ClickEvent>(evt =>
        {
            NewBuildingController.DeleteFloorPlan(evt, name);
        });
        toggleVisualElement.Add(deleteButton);
    }

    private void AddHeightElement(Foldout foldout, string name)
    {
        foldout.AddToClassList(WHCSSConstants.PINK_BACKGROUND_COLOR);
        VisualElement HeightElement = new VisualElement();
        HeightElement.AddToClassList("row-container");
        HeightElement.Add(CreateHeightTextField(foldout, name));
        HeightElement.Add(CreateHeightButton());
        foldout.Add(HeightElement);
    }

    private TextField CreateHeightTextField(Foldout foldout, string name)
    {
        TextField heightField = new TextField();
        heightField.AddToClassList("col-md-10");
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
            IEnumerable<VisualElement> children = NewBuildingController.GetBuilding().uiItem.Foldout.Children();
            int floorPlanNumber = NamingController.GetItemNameNumber(foldout.name);
            float adjustmentHeight = float.Parse(evt.newData) - float.Parse(evt.previousData);
            NewBuildingController.AdjustFloorPlans(children, floorPlanNumber, 0, adjustmentHeight);
        });
        return heightField;
    }

    private Button CreateHeightButton()
    {
        Button heightUnit = new Button();
        heightUnit.AddToClassList("col-md-2");
        heightUnit.AddToClassList("col-xs-2");
        heightUnit.style.marginLeft = 0;
        heightUnit.text = WHConstants.FeetUnit;
        return heightUnit;
    }
}
