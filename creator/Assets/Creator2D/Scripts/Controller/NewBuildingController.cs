using System.Collections.Generic;
using ObjectModel;
using UnityEngine;
using UnityEngine.UIElements;

public class NewBuildingController
{
    public static bool buildingCreated = false;
    private static CreatorItem building = null;
    private static CreatorItem floorPlan;
    private static CreatorItem roof;

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

    public static CreatorItem GetCurrentRoof()
    {
        return roof;
    }

    public static void SetCurrentRoof(CreatorItem item)
    {
        roof = item;
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
        roof.SetPosition(new Vector3(0, 0, posZ + WHConstants.DefaultWallHeight));

        string name = NamingController.GetName(WHConstants.FLOOR_PLAN, GetBuilding().uiItem.Foldout.Children());
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
            CloneChildren(floorPlan, CreatorItemFinder.FindByName(SelectedFloorPlanName));
        }
        else if (belowFloorPlan != null)
        {
            CreatorItem floorItem = CreatorItemFinder.FindByName(WHConstants.FLOOR + "001", belowFloorPlan);
            CreatorItem ceilingItem = CreatorItemFinder.FindByName(WHConstants.CEILING + "001", belowFloorPlan);
            floorPlan.AddChild(floorItem.Clone());
            floorPlan.AddChild(ceilingItem.Clone());
            List<CreatorItem> listElevator = CreatorItemFinder.FindAll<NewElevator>(belowFloorPlan);
            foreach (var elevator in listElevator)
            {
                floorPlan.AddChild(elevator.Clone());
            }
        }

        return floorPlan;
    }

    public static void LinkFloorPlan(CreatorItem floorPlan, CreatorItem BaseItem)
    {
        var height = BaseItem.GetComponent<NewIHasDimension>().Dimension.Height;
        var SetDimension = new ResizeCommand(floorPlan.Id, new Dimension(0, height, 0));
        NewUndoRedo.AddAndExecuteCommand(SetDimension);
        // var roofPosition = roof.Position;
        // roof.SetPosition(new Vector3(0, 0, roofPosition.z + height - WHConstants.DefaultWallHeight));

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
            NewBuildingController.UnLinkFloorPlan(linkFloorName, unLinkButton, floorPlan, BaseItem);
        });

        toggleVisualElement.Insert(toggleVisualElement.childCount - 1, unLinkButton);
        toggleVisualElement.Insert(toggleVisualElement.childCount - 1, linkFloorName);

        LinkedFloorPlan.Link(floorPlan, BaseItem);
    }

    private static void CloneChildren(CreatorItem floorPlan, CreatorItem BaseItem)
    {
        foreach (var child in BaseItem.children)
        {
            var item = child.Clone();
            floorPlan.AddChild(item);

            if (item is NewWall)
            {
                AttachNodes(item as NewWall, floorPlan);
            }
        }
    }

    private static void UnLinkFloorPlan(Label label, Button unLinkButton, CreatorItem floorPlan, CreatorItem baseItem)
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
            if (child.name.Contains(WHConstants.FLOOR_PLAN))
            {
                previousFloorPlan = child;
            }
        }
        return previousFloorPlan;
    }

    public static void CreateRoof()
    {
        var createCommand = new CreatorItemCreateCommand(new CreatorRoofFactory(), WHConstants.ROOF);
        var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, GetBuilding().Id);
        var setFloorPlanCommand = new SetCurrentFloorPlanCommand(WHConstants.ROOF, floorPlan != null ? floorPlan.name : "", true);
        var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, setFloorPlanCommand });
        NewUndoRedo.AddAndExecuteCommand(multiCommand);
        roof = CreatorItemFinder.FindByName(WHConstants.ROOF);
    }

    public static void CreateFloor(List<Vector3> boundary)
    {
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            string name = NamingController.GetName(WHConstants.FLOOR, floorPlan.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorFloorFactory(), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlan.Id);
            var setBoundaryCommand = new SetBoundaryCommand(createCommand.Id, boundary);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, setBoundaryCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);
        }
        CreatorUIController.UnSavedProgress = true;
    }

    private static void CheckAndRemoveFromParent(CreatorItem floorPlanItem)
    {
        CreatorItem parentFloorPlanItem = LinkedFloorPlan.GetParentItem(floorPlanItem);
        if (parentFloorPlanItem != null)
        {
            Label linkFloorName = new Label();
            Label labelElement = floorPlanItem.uiItem.Foldout.Q<Label>("linkFloorName-" + floorPlanItem.name);
            Button unLinkButton = floorPlanItem.uiItem.Foldout.Q<Button>("unLinkButton-" + floorPlanItem.name);
            UnLinkFloorPlan(labelElement, unLinkButton, floorPlanItem, parentFloorPlanItem);
        }
    }

    public static void CreateElevator(Vector3 position, UnityEngine.Sprite sprite)
    {
        foreach (var child in GetBuilding().children)
        {
            if (child.name.Contains(WHConstants.FLOOR_PLAN))
            {
                var floorPlan = child;
                var height = floorPlan.GetComponent<NewIHasDimension>().Dimension.Height;

                string name = NamingController.GetName(WHConstants.ELEVATOR, floorPlan.uiItem.Foldout.Children());
                var createCommand = new CreatorItemCreateCommand(new CreatorElevatorFactory(position, sprite, height), name);
                var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlan.Id);
                var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
                NewUndoRedo.AddAndExecuteCommand(multiCommand);
            }
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
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
            string name = NamingController.GetName(WHConstants.CEILING, floorPlan.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorCeilingFactory(), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlan.Id);
            var setPositionCommand = new SetPositionCommand(createCommand.Id, new Vector3(0, 0, height));
            var setBoundaryCommand = new SetBoundaryCommand(createCommand.Id, boundary);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand, setPositionCommand, setBoundaryCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);
        }
        CreatorUIController.UnSavedProgress = true;
    }

    public static void CreateWall(Vector3 startPosition, Vector3 endPosition, bool attach = true, bool isExterior = false)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            string name = NamingController.GetName(WHConstants.WALL, floorPlanItem.uiItem.Foldout.Children());
            var createCommand = new CreatorItemCreateCommand(new CreatorWallFactory(startPosition, endPosition, isExterior), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, floorPlanItem.Id);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);

            var item = CreatorItemFinder.FindByName(name, floorPlanItem);
            AttachNodes(item as NewWall, floorPlanItem, attach);
        }
        CreatorUIController.UnSavedProgress = true;
    }

    public static void AttachNodes(NewWall item, CreatorItem floor, bool attach = true)
    {
        WallTransform wallTransform = item.GetWallTranformer();
        var listener = wallTransform.wallListener;

        for (int i = 0; i < listener.wallRenderer.positionCount; i++)
        {
            Node node = new Node(i, listener.wallGO, floor);
            listener.AttachNode(i, node, floor, attach);
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
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(ItemName, floorPlanItem);

            UpdateWall3DPos(parentItem, pos0, pos1, angle);

            parentItem.gameObject.transform.position = pos0;
            parentItem.gameObject.GetComponent<LineRenderer>().SetPositions(new Vector3[] { pos0, pos1 });
        }
        CreatorUIController.UnSavedProgress = true;
    }

    public static void UpdateWallHandle(string ItemName, Vector3 pos0, Vector3 pos1, float angle, bool attach = true)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(ItemName, floorPlanItem);
            LineRenderer wallRenderer = parentItem.gameObject.GetComponent<LineRenderer>();

            //Deletes the window or door if the fall outside the wall
            foreach (var child in parentItem.children)
            {
                if (child is NewWindow || child is NewDoor)
                {
                    if (child.gameObject.transform.localPosition.x > GetMaxClamp(wallRenderer, child.gameObject))
                    {
                        var deleteCommand = new DeleteItemCommand(child.Id, false);
                        deleteCommand.Execute();
                        break;
                    }
                }
            }

            var length = Vector3.Distance(pos0, pos1);
            float lineWidth = wallRenderer.endWidth;
            wallRenderer.SetPositions(new Vector3[] { pos0, pos1 });

            BoxCollider lineCollider = parentItem.gameObject.GetComponent<BoxCollider>();
            lineCollider.transform.parent = wallRenderer.transform;
            lineCollider.center = new Vector3(length / 2, 0.0f, 0.0f);
            lineCollider.size = new Vector3(length, lineWidth, 1f);

            parentItem.gameObject.transform.position = pos0;
            parentItem.gameObject.transform.eulerAngles = new Vector3(0, 0, angle);

            WallListener wall = TransformDatas.wallListenersList[parentItem.gameObject];

            foreach (var n in wall.nodes)
            {
                var points = wallRenderer.GetPosition(n.Key);

                //Update the position of the nodes based on the wall's new position
                n.Value.nodeGO.transform.position = new Vector3(points.x, points.y, HarnessConstant.DEFAULT_NODE_ZOFFSET);
                n.Value.nodeGO.transform.localScale = new Vector3((wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, (wallRenderer.widthMultiplier + 0.05f) * HarnessConstant.DEFAULT_NODE_SIZE, 0.05f);
                n.Value.nodeGO.transform.rotation = new Quaternion(0, 0, 0, 0);
            }

            if (attach)
            {
                for (int i = 0; i < wall.wallRenderer.positionCount; i++)
                {
                    wall.AttachNode(i, wall.nodes[i], floorPlanItem);
                }
            }
            UpdateWall3DPos(parentItem, pos0, pos1, angle);
        }
        CreatorUIController.UnSavedProgress = true;
    }

    public static float GetMaxClamp(LineRenderer line, GameObject GO)
    {
        var bound = line.bounds;
        var object_bound = GO.GetComponent<SpriteRenderer>().bounds;

        return Vector3.Distance(bound.max, bound.min) - (object_bound.size.x * 1.2f);
    }

    public static void UpdateWallObject(string itemName, CreatorItem itemParent, Vector3 position, Vector3 localPosition, GameObject parent, string type)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(itemParent.name, floorPlanItem);
            CreatorItem objItem = CreatorItemFinder.FindByName(itemName, parentItem);

            if (type == WHConstants.WINDOW)
            {
                objItem.GetComponent<NewIHasDimension>().SetDimension(WHConstants.DefaultWindowLength, WHConstants.DefaultWindowHeight, WHConstants.DefaultWindowBreadth);
                objItem.SetPosition(new Vector3(Mathf.Abs(position.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultWindowY));
            }
            if (type == WHConstants.DOOR)
            {
                objItem.GetComponent<NewIHasDimension>().SetDimension(WHConstants.DefaultDoorLength, WHConstants.DefaultDoorHeight, WHConstants.DefaultDoorBreadth);
                objItem.SetPosition(new Vector3(Mathf.Abs(position.x - parent.gameObject.transform.position.x), 0, WHConstants.DefaultDoorY));
            }
            objItem.GetComponent<NewIHasRotation>().SetRotation(0, 0, 0);
            objItem.gameObject.transform.localPosition = localPosition + new Vector3(0, 0, WHConstants.DefaultZ);
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    public static void UpdateObjectPosition(string itemName, Vector3 position)
    {
        var floor = NewBuildingController.GetBuilding().children;
        foreach (CreatorItem floorPlanItem in floor)
        {
            if (floorPlanItem.name.Contains(WHConstants.FLOOR_PLAN))
            {
                CreatorItem parentItem = CreatorItemFinder.FindByName(itemName, floorPlanItem);
                parentItem.SetPosition(new Vector3(position.x, position.y, WHConstants.DefaultZ));
                parentItem.gameObject.transform.position = position;
            }
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    public static void UpdateObjectRotation(string itemName, float angle)
    {
        var floor = NewBuildingController.GetBuilding().children;
        foreach (CreatorItem floorPlanItem in floor)
        {
            if (floorPlanItem.name.Contains(WHConstants.FLOOR_PLAN))
            {
                CreatorItem parentItem = CreatorItemFinder.FindByName(itemName, floorPlanItem);
                parentItem.GetComponent<NewIHasRotation>().SetRotation(0f, -angle, 0f);
                parentItem.gameObject.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    private static void DeleteObject(string itemName)
    {
        var floor = NewBuildingController.GetBuilding().children;
        foreach (CreatorItem floorPlanItem in floor)
        {
            if (floorPlanItem.name.Contains(WHConstants.FLOOR_PLAN))
            {
                CreatorItem item = CreatorItemFinder.FindByName(itemName, floorPlanItem);
                var deleteCommand = new DeleteItemCommand(item.Id, false);
                deleteCommand.Execute();
            }
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    public static void CreateDoor(string wallName, Vector3 startPosition, UnityEngine.Sprite sprite)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(wallName, floorPlanItem);
            if (parentItem is NewWall && ((NewWall)parentItem).IsExterior && floorPlanItem != floorPlan)
            {
                continue;
            }
            else
            {
                string name = NamingController.GetName(WHConstants.DOOR, parentItem.uiItem.Foldout.Children());

                var createCommand = new CreatorItemCreateCommand(new CreatorDoorFactory(parentItem, startPosition, sprite), name);
                var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, parentItem.Id);
                var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
                NewUndoRedo.AddAndExecuteCommand(multiCommand);

                var position = createCommand.createdItem.gameObject.transform.localPosition;
                createCommand.createdItem.gameObject.transform.localPosition = new Vector3(position.x, 0, WHConstants.DefaultZ);
            }
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    public static void CreateWindow(string wallName, Vector3 startPosition, UnityEngine.Sprite sprite)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(wallName, floorPlanItem);
            string name = NamingController.GetName(WHConstants.WINDOW, parentItem.uiItem.Foldout.Children());

            var createCommand = new CreatorItemCreateCommand(new CreatorWindowFactory(parentItem, startPosition, sprite), name);
            var createAndAddParentCommand = new CreateCreatorItemWithParentCommand(createCommand, parentItem.Id);
            var multiCommand = new MultipleCommand(new List<ICommand>() { createAndAddParentCommand });
            NewUndoRedo.AddAndExecuteCommand(multiCommand);

            var position = createCommand.createdItem.gameObject.transform.localPosition;
            createCommand.createdItem.gameObject.transform.localPosition = new Vector3(position.x, 0, WHConstants.DefaultZ);
        }
        DeselectAll();
        CreatorUIController.UnSavedProgress = true;
    }

    public static void DeleteFloorPlan(ClickEvent evt, UIItem uIItem)
    {
        var floorPlanName = uIItem.Foldout.name;
        var item = CreatorItemFinder.FindByName(floorPlanName);
        var previousFloorPlan = GetPreviousFloorPlan();
        if (previousFloorPlan != null && previousFloorPlan == item)
        {
            return; // We are not allowing to delete the floorplan if there is only one TODO need to show a warning dialog
        }
        var parentItem = LinkedFloorPlan.GetParentItem(item);
        if (parentItem != null)
        {
            LinkedFloorPlan.UnLink(item, parentItem);
        }
        List<CreatorItem> linkedItemList = LinkedFloorPlan.GetLinkedItems(item);
        for (var i = 0; i < linkedItemList.Count; i++)
        {
            if (linkedItemList[i] != item)
            {
                CheckAndRemoveFromParent(linkedItemList[i]);
            }
        }
        LinkedFloorPlan.Remove(item);
        var deleteCommand = new DeleteItemCommand(item.Id, true);
        deleteCommand.Execute();
        evt.StopPropagation();

        int floorPlanNumber = NamingController.GetItemNameNumber(floorPlanName);
        float adjustmentHeight = item.GetComponent<NewIHasDimension>().Dimension.Height;
        AdjustFloorPlans(floorPlanNumber, -1, -adjustmentHeight);
    }

    public static void DetachNodes(string itemName, int key)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(itemName, floorPlanItem);

            WallListener wall = TransformDatas.wallListenersList[parentItem.gameObject];

            TransformDatas.allNodeList.Remove(wall.nodes[key].nodeGO);
            GameObject.Destroy(wall.nodes[key].nodeGO);

            Node newnode = new Node(key, wall.wallGO, wall.nodes[key].floor);
            wall.nodes[key] = newnode;
            wall.UpdateNodeListner(newnode);
        }
        CreatorUIController.UnSavedProgress = true;
    }

    public static void DetachWall(string itemName)
    {
        CheckAndRemoveFromParent(floorPlan);
        List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);

        foreach (CreatorItem floorPlanItem in linkedFloors)
        {
            CreatorItem parentItem = CreatorItemFinder.FindByName(itemName, floorPlanItem);
            WallListener wall = TransformDatas.wallListenersList[parentItem.gameObject];

            //This will trigger an event on the node which notifies all its listner
            wall.nodes[0].DetachNode();
            wall.nodes[1].DetachNode();
        }
    }

    public static void DeleteItem(string itemName, bool isWall = false)
    {
        var itemCheck = CreatorItemFinder.FindByName(itemName, floorPlan);
        if (!(itemCheck is NewElevator))
        {
            CheckAndRemoveFromParent(floorPlan);
            List<CreatorItem> linkedFloors = LinkedFloorPlan.GetLinkedItems(floorPlan);
            foreach (CreatorItem floorPlanItem in linkedFloors)
            {
                var item = CreatorItemFinder.FindByName(itemName, floorPlanItem);

                //Clearing the wall link informations
                if (item is NewWall)
                {
                    WallListener wall = TransformDatas.wallListenersList[item.gameObject];
                    TransformDatas.allNodeList.Remove(wall.nodes[0].nodeGO);
                    TransformDatas.allNodeList.Remove(wall.nodes[1].nodeGO);
                    TransformDatas.wallListenersList.Remove(wall.wallGO);
                }

                var deleteCommand = new DeleteItemCommand(item.Id, false);
                deleteCommand.Execute();
            }
        }
        else
        {
            DeleteObject(itemName);
        }
    }

    public static void AdjustFloorPlans(int floorPlanNumber, int adjustByNumber, float adjustByHeight)
    {
        var floorPlanList = building.children;
        foreach (var item in floorPlanList)
        {
            try
            {
                if (item != null && item.name.Contains(WHConstants.FLOOR_PLAN))
                {
                    int itemFloorPlanNumber = NamingController.GetItemNameNumber(item.name);

                    if (adjustByNumber == 0 && itemFloorPlanNumber == floorPlanNumber)
                    {
                        var dimension = item.GetComponent<NewIHasDimension>().Dimension;
                        item.GetComponent<NewIHasDimension>().SetDimension(dimension.Length, dimension.Height + adjustByHeight, dimension.Width);
                        continue;
                    };

                    if (itemFloorPlanNumber >= floorPlanNumber)
                    {
                        string name = WHConstants.FLOOR_PLAN + NamingController.GetFormattedNumber(itemFloorPlanNumber + adjustByNumber);
                        foreach (CreatorItem linkedItem in LinkedFloorPlan.GetLinkedItems(item))
                        {
                            if (item != linkedItem)
                            {
                                Label labelElement = linkedItem.uiItem.Foldout.Q<Label>("linkFloorName-" + linkedItem.name);
                                if (labelElement != null)
                                {
                                    labelElement.text = name;
                                }
                            }
                        }
                        Label itemLabelElement = item.uiItem.Foldout.Q<Label>("linkFloorName-" + item.name);
                        if (itemLabelElement != null)
                        {
                            Button itemUnLinkButton = item.uiItem.Foldout.Q<Button>("unLinkButton-" + item.name);
                            itemLabelElement.name = "linkFloorName-" + name;
                            itemUnLinkButton.name = "unLinkButton-" + name;
                        }
                        var position = item.GetComponent<NewIHasPosition>().Position;
                        item.GetComponent<NewIHasPosition>().SetPosition(new Vector3(position.x, position.y, position.z + adjustByHeight));
                        item.SetName(name);
                        item.uiItem.SetName(name);
                    }
                }

                if (item.name.Contains(WHConstants.ROOF))
                {
                    var position = item.GetComponent<NewIHasPosition>().Position;
                    item.GetComponent<NewIHasPosition>().SetPosition(new Vector3(position.x, position.y, position.z + adjustByHeight));
                }

            }
            catch
            {
                Trace.Error("Item Name doesnot have numbered suffix");
            }
        }
    }

    public static void DeselectAll()
    {
        CreatorHotKeyController.Instance.DeselectAllItems();
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();
    }
}