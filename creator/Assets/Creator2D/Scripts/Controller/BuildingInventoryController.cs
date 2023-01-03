using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingInventoryController
{
    /* Define member variables*/
    private const string buildingInventoryName = "building-inventory";
    private const string inventoryName = "inventories";
    private const string inventoryContentName = "inventoryContent";
    private const string scrollViewContentName = "ScrollViewContent";
    private const string unselectedContentClassName = "unselectedContent";
    private const string buttonContainerClassName = "buttons-container";
    private const string boundaryClassName = "border-boundary";
    private const string contentNameSuffix = "Content";
    private const string dropdownName = "Inventory-DropDown";

    private VisualElement buildingInventoryRoot;
    private MetaBlock[] availableMetaBlocks;
    public MetaBlock currentBlock;

    private static BuildingInventoryController b_instance = null;

    public static BuildingInventoryController Get()
    {
        if (b_instance == null)
        {
            b_instance = new BuildingInventoryController();
            b_instance.Initialize();
        }
        return b_instance;
    }

    private void Initialize()
    {
        buildingInventoryRoot = CreatorUIController.getRoot().Q<VisualElement>(buildingInventoryName);
        buildingInventoryRoot.style.marginTop = 10;
        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        cam.GetComponent<CreatorEventManager>().enabled = true;
    }

    public void SetMetaBlocks(MetaBlock[] availableMetaBlocks)
    {
        b_instance.availableMetaBlocks = availableMetaBlocks;
    }

    public void SetupBuildingInventories()
    {
        List<string> inventoryList = new List<string>();
        inventoryList.Add("All");
        inventoryList.Add("Build");
        inventoryList.Add("Decorator");
        VisualElement Inventories = buildingInventoryRoot.Q<VisualElement>(inventoryName);
        Label label = new Label();
        label.text = "Categories";
        label.AddToClassList("category-label");
        label.AddToClassList("bold-font");
        Inventories.Add(label);
        DropdownField inventoryDropDown = new DropdownField();
        inventoryDropDown.AddToClassList("row-container");
        inventoryDropDown.AddToClassList("add-floor-drop-down");
        inventoryDropDown.name = dropdownName;
        inventoryDropDown.choices = inventoryList;
        inventoryDropDown.RegisterValueChangedCallback(RegisterDropdownCallBacks);
        inventoryDropDown.value = "All";
        Inventories.Add(inventoryDropDown);
        SetUpMetaBlockButtons("All");
    }

    private void RegisterDropdownCallBacks(ChangeEvent<string> evt)
    {
        DropdownField df = evt.currentTarget as DropdownField;
        SetUpMetaBlockButtons(df.value);
    }

    private void SetUpMetaBlockButtons(string CategoryName)
    {
        VisualElement inventoryContent = buildingInventoryRoot.Q<VisualElement>(inventoryContentName);
        ScrollView existScrollViewContent = inventoryContent.Q<ScrollView>(scrollViewContentName);
        if (existScrollViewContent != null)
        {
            inventoryContent.Remove(existScrollViewContent);
        }
        ScrollView scrollViewContent = new ScrollView();
        scrollViewContent.name = scrollViewContentName;
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.AddToClassList(buttonContainerClassName);
        scrollViewContent.AddToClassList("metablock-scroll");

        // OnChange Categories setup meta block button
        foreach (var metaBlock in getBlocks(CategoryName))
        {
            SetUpMetaBlockButton(buttonContainer, metaBlock);
        }
        scrollViewContent.Add(buttonContainer);
        inventoryContent.Add(scrollViewContent);
    }

    private MetaBlock[] getBlocks(string CategoryName)
    {
        return CategoryName == "All" ? availableMetaBlocks : Array.FindAll(availableMetaBlocks, elem => elem.CategoryName == CategoryName);
    }

    private void SetUpMetaBlockButton(VisualElement buttonContainer, MetaBlock metaBlock)
    {
        VisualElement item = new VisualElement();
        item.AddToClassList("metablock-item");

        VisualElement button = new VisualElement();
        button.AddToClassList(metaBlockClassName);
        button.AddToClassList("unity-button");
        button.AddToClassList("grow");

        VisualElement buttonIcon = new VisualElement();
        buttonIcon.AddToClassList("metablock-button-icon");

        // Icon's path in our project.
        var iconPath = "InventoryItems/" + metaBlock.IconName.Replace(" ", "_").ToLower();
        // Loads the actual asset from the above path.
        var iconAsset = Resources.Load<Texture2D>(iconPath);
        metaBlock.BlockTexture = iconAsset;

        // Applies the above asset as a background image for the icon.
        buttonIcon.style.backgroundImage = iconAsset;
        button.Add(buttonIcon);
        Label metaLabel = new Label(metaBlock.BlockName);
        metaLabel.AddToClassList("metablock-label");
        button.Add(metaLabel);

        // Instantiates our primitive object on a left click.
        button.RegisterCallback<PointerUpEvent, string>(SelectObject, metaBlock.BlockName);

        // Sets a basic tooltip to the button itself.
        button.tooltip = metaBlock.BlockName;
        // button.AddManipulator(new ToolTipManipulator());
        item.Add(button);
        buttonContainer.Add(item);
    }

    public void DeSelectAllObject()
    {
        GetAllMetaBlocks().Where(
                (metaBlock) => MetaBlockIsCurrentlySelected(metaBlock)
            ).ForEach(UnselectMetaBlock);
    }

    private void SelectObject(PointerUpEvent evt, string BlockName)
    {
        if (NewBuildingController.CurrentFloorPlan() == null) return;
        VisualElement clickedMetaBlock = evt.currentTarget as VisualElement;

        if (!MetaBlockIsCurrentlySelected(clickedMetaBlock))
        {
            GetAllMetaBlocks().Where(
                (metaBlock) => metaBlock != clickedMetaBlock && MetaBlockIsCurrentlySelected(metaBlock)
            ).ForEach(UnselectMetaBlock);
            SelectMetaBlock(clickedMetaBlock, BlockName);
        }
    }

    private const string currentlySelectedMetaBlockClassName = "metablock-button-border";
    private const string metaBlockClassName = "metablock-button";

    private bool MetaBlockIsCurrentlySelected(VisualElement clickedMetaBlock)
    {
        return clickedMetaBlock.ClassListContains(currentlySelectedMetaBlockClassName);
    }

    private UQueryBuilder<VisualElement> GetAllMetaBlocks()
    {
        return buildingInventoryRoot.Query<VisualElement>(className: metaBlockClassName);
    }

    private void SelectMetaBlock(VisualElement metaBlock, string BlockName)
    {
        CreatorHotKeyController.Instance.DeselectAllItems();
        CreatorHotKeyController.Instance.hotkeyMenu.Populate(CreatorHotKeyController.Instance.generalKeys);

        metaBlock.AddToClassList(currentlySelectedMetaBlockClassName);
        currentBlock = Array.Find(availableMetaBlocks, element => element.BlockName == BlockName);
        CreatorUIController.DeselectFlyOutButton();
    }

    private void UnselectMetaBlock(VisualElement metaBlock)
    {
        metaBlock.RemoveFromClassList(currentlySelectedMetaBlockClassName);
    }

}