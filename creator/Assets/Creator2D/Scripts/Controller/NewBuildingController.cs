using System.Collections.Generic;
using ObjectModel;
using UnityEngine;
using UnityEngine.UIElements;

public class NewBuildingController
{
    public static bool buildingCreated = false;
    private static CreatorItem building = null;
    private static CreatorItem floorPlan;

    public static CreatorItem CurrentFloorPlan()
    {
        return floorPlan;
    }
    public static void SetBuilding(CreatorItem item)
    {
        building = item;
    }

    public static void SetCurrentFloorPlan(CreatorItem item)
    {
        floorPlan = item;
    }

    public static CreatorItem GetBuilding()
    {
        if (building == null) building = new CreatorBuildingFactory().Create();
        buildingCreated = true;
        return building;
    }

    public static CreatorItem CreateFloorPlan(string SelectedFloorPlanName)
    {
        float posZ = 0;
        CreatorItem belowFloorPlan = GetPreviousFloorPlan();
        if (belowFloorPlan != null)
        {
            var z = belowFloorPlan.GetComponent<NewIHasPosition>().Position.z;
            var floorHeight = belowFloorPlan.GetComponent<NewIHasDimension>().Dimension.Height;
            posZ = z + floorHeight;
        }

        string name = NamingController.GetName("FloorPlan", GetBuilding().uiItem.Foldout.Children());
        var createCommand = new CreatorItemCreateCommand(new CreatorFloorPlanFactory(), name);
        var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, GetBuilding().Id);
        var SetPosition = new SetPositionCommand(createCommand.Id, new Vector3(0, 0, posZ));
        var SetDimension = new ResizeCommand(createCommand.Id, new Dimension(0, WHConstants.DefaultWallHeight, 0));
        var setFloorPlanCommand = new SetCurrentFloorPlanCommand(name, floorPlan != null ? floorPlan.name : "", true);
        var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, SetPosition, SetDimension, setFloorPlanCommand });
        NewUndoRedo.AddAndExecuteCommand(multiCommand);

        if (SelectedFloorPlanName != null && SelectedFloorPlanName != CreatorUIController._copy_from_below)
        {
            LinkFloorPlan(floorPlan, CreatorItemFinder.FindByName(SelectedFloorPlanName));
        }
        else if (belowFloorPlan != null)
        {
            CreatorItem floorItem = CreatorItemFinder.FindByName("Floor001", belowFloorPlan);
            CreatorItem ceilingItem = CreatorItemFinder.FindByName("Ceiling001", belowFloorPlan);
            floorPlan.AddChild(floorItem.Clone());
            floorPlan.AddChild(ceilingItem.Clone());
        }
        return floorPlan;
    }

    private static void LinkFloorPlan(CreatorItem floorPlan, CreatorItem BaseItem)
    {
        Label labelElement = floorPlan.uiItem.Foldout.Q<Label>();
        VisualElement toggleVisualElement = labelElement.parent;

        Label linkFloorName = new Label();
        linkFloorName.name = "linkFloorName-" + floorPlan.name;
        linkFloorName.text = BaseItem.name;
        linkFloorName.AddToClassList("link-floor-label");

        Button unLinkButton = new Button();
        unLinkButton.name = "unLinkButton-" + floorPlan.name;
        unLinkButton.AddToClassList("link-floor-icon");
        unLinkButton.RegisterCallback<ClickEvent>(evt =>
        {
            NewBuildingController.UnLinkFloorPlan(evt, linkFloorName, unLinkButton, floorPlan, BaseItem);
        });

        toggleVisualElement.Insert(toggleVisualElement.childCount - 1, unLinkButton);
        toggleVisualElement.Insert(toggleVisualElement.childCount - 1, linkFloorName);

        LinkedFloorPlan.Link(floorPlan, BaseItem);
        foreach (var child in BaseItem.children)
        {
            floorPlan.AddChild(child.Clone());
        }

    }

    private static void UnLinkFloorPlan(ClickEvent evt, Label label, Button unLinkButton, CreatorItem floorPlan, CreatorItem baseItem)
    {
        LinkedFloorPlan.UnLink(floorPlan, baseItem);
        label.RemoveFromHierarchy();
        unLinkButton.RemoveFromHierarchy();
    }

    private static CreatorItem GetPreviousFloorPlan()
    {
        CreatorItem previousFloorPlan = null;

        foreach (var child in GetBuilding().children)
        {
            if (child.name.Contains("FloorPlan"))
            {
                previousFloorPlan = child;
            }
        }
        return previousFloorPlan;
    }

    public static void CreateFloor(List<Vector3> boundary)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            string name = NamingController.GetName("Floor", floorPlan.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorFloorFactory(), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlan.Id);
            var setBoundaryCommand = new SetBoundaryCommand(createCommand.Id, boundary);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, setBoundaryCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);
        }
    }

    public static void CreateCeiling(List<Vector3> boundary)
    {
        float height = WHConstants.DefaultFloorHeight;
        if (floorPlan != null)
        {
            height = floorPlan.GetComponent<NewIHasDimension>().Dimension.Height;
        }
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            string name = NamingController.GetName("Ceiling", floorPlan.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorCeilingFactory(), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlan.Id);
            var setPositionCommand = new SetPositionCommand(createCommand.Id, new Vector3(0, 0, height));
            var setBoundaryCommand = new SetBoundaryCommand(createCommand.Id, boundary);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, setPositionCommand, setBoundaryCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);
        }
    }

    public static void CreateWall(Vector3 startPosition, Vector3 endPosition)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            string name = NamingController.GetName("Wall", floorPlanItem.uiItem.Foldout.Children());
            var createCommand = new CreatorItemCreateCommand(new CreatorWallFactory(startPosition, endPosition), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlanItem.Id);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);
        }
    }

    public static void UpdateWall3DPos(CreatorItem parentItem, Vector3 pos0, Vector3 pos1, float angle)
    {
        parentItem.GetComponent<NewIHasDimension>().SetDimension(Vector3.Distance(pos0, pos1), WHConstants.DefaultWallHeight, WHConstants.DefaultWallBreadth);
        parentItem.SetPosition(pos0);
        parentItem.GetComponent<NewIHasRotation>().SetRotation(0, -angle, 0);
    }

    public static void UpdateWall(string ItemName, Vector3 pos0, Vector3 pos1, float angle)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(ItemName, floorPlanItem);

            UpdateWall3DPos(parentItem, pos0, pos1, angle);

            parentItem.gameObject.transform.position = pos0;
            parentItem.gameObject.GetComponent<LineRenderer>().SetPositions(new Vector3[] { pos0, pos1 });
        }
    }

    public static void UpdateWallHandle(string ItemName, Dictionary<int, Node> nodes, Vector3 pos0, Vector3 pos1, float angle)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(ItemName, floorPlanItem);
            LineRenderer wallRenderer = parentItem.gameObject.GetComponent<LineRenderer>();

            var length = Vector3.Distance(pos0, pos1);
            float lineWidth = wallRenderer.endWidth;
            wallRenderer.SetPositions(new Vector3[] { pos0, pos1 });

            BoxCollider lineCollider = parentItem.gameObject.GetComponent<BoxCollider>();
            lineCollider.transform.parent = wallRenderer.transform;
            lineCollider.center = new Vector3(length / 2, 0.0f, 0.0f);
            lineCollider.size = new Vector3(length, lineWidth, 1f);

            parentItem.gameObject.transform.position = pos0;
            parentItem.gameObject.transform.eulerAngles = new Vector3(0, 0, angle);

            foreach (var n in nodes)
            {
                var points = wallRenderer.GetPosition(n.Key);

                //Update the position of the nodes based on the wall's new position
                n.Value.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
                n.Value.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
                n.Value.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);
            }

            UpdateWall3DPos(parentItem, pos0, pos1, angle);
        }
    }


    public static void UpdateObject(string itemName, Vector3 pos0, Vector3 localPos, GameObject parent, string type)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(itemName, floorPlanItem);
            if (type == "window")
            {
                parentItem.GetComponent<NewIHasDimension>().SetDimension(WHConstants.DefaultWindowLength, WHConstants.DefaultWindowHeight, WHConstants.DefaultWindowBreadth);
                parentItem.SetPosition(new Vector3(Mathf.Abs(pos0.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultWindowY));
            }
            if (type == "door")
            {
                parentItem.GetComponent<NewIHasDimension>().SetDimension(WHConstants.DefaultDoorLength, WHConstants.DefaultDoorHeight, WHConstants.DefaultDoorBreadth);
                parentItem.SetPosition(new Vector3(Mathf.Abs(pos0.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultDoorY));
            }
            parentItem.GetComponent<NewIHasRotation>().SetRotation(0, 0, 0);

            parentItem.gameObject.transform.localPosition = localPos;
        }
        DeselectAll();
    }

    public static void CreateDoor(string wallName, Vector3 startPosition)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(wallName, floorPlanItem);
            string name = NamingController.GetName("Door", parentItem.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorDoorFactory(parentItem, startPosition), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, parentItem.Id);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);

            var position = createCommand.createdItem.gameObject.transform.localPosition;
            createCommand.createdItem.gameObject.transform.localPosition = new Vector3(position.x, 0, 0);
        }
        DeselectAll();
    }

    public static void CreateWindow(string wallName, Vector3 startPosition)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(wallName, floorPlanItem);
            string name = NamingController.GetName("Window", parentItem.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorWindowFactory(parentItem, startPosition), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, parentItem.Id);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);

            var position = createCommand.createdItem.gameObject.transform.localPosition;
            createCommand.createdItem.gameObject.transform.localPosition = new Vector3(position.x, 0, 0);
        }
        DeselectAll();
    }

    public static void DeleteFloorPlan(ClickEvent evt, string floorPlanName)
    {
        var deleteCommand = new DeleteItemCommand(floorPlanName, true);
        deleteCommand.Execute();
        evt.StopPropagation();
    }

    public static void DeleteItem(string itemName)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            var deleteCommand = new DeleteItemCommand(itemName, false);
            deleteCommand.Execute();
        }
    }

    public static void AdjustFloorPlans(IEnumerable<VisualElement> floorPlanList, int floorPlanNumber, int adjustByNumber, float adjustByHeight)
    {
        foreach (var element in floorPlanList)
        {
            try
            {
                Foldout item;
                if (element is Foldout) item = (Foldout)element;
                else continue;
                if (item != null && item.text.Contains("FloorPlan"))
                {
                    int itemFloorPlanNumber = NamingController.GetItemNameNumber(item.text);

                    if (adjustByNumber == 0 && itemFloorPlanNumber == floorPlanNumber) continue;

                    if (itemFloorPlanNumber >= floorPlanNumber)
                    {
                        string name = "FloorPlan" + NamingController.GetFormattedNumber(itemFloorPlanNumber + adjustByNumber);
                        GameObject floorPlanGO = GetBuilding().gameObject.transform.Find(item.name).gameObject;
                        if (floorPlanGO != null && floorPlanGO.activeSelf)
                        {
                            floorPlanGO.transform.position += new Vector3(0, adjustByHeight, 0);
                            floorPlanGO.name = name;
                        }

                        item.text = name;
                        item.name = name;
                    }
                }

            }
            catch
            {
                Trace.Error("Item Name doesnot have numbered suffix");
            }
        }
    }

    private static void DeselectAll()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();
    }
}