using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class CreatorHotKeyController : MonoBehaviour
{
    public static CreatorHotKeyController Instance;

    public HotkeyMenu hotkeyMenu;
    public ImageFade blackCurtain;
    public List<HotkeyMenu.Key> wallKeys;
    public List<HotkeyMenu.Key> nodeKeys;
    public List<HotkeyMenu.Key> objectKeys;
    public List<HotkeyMenu.Key> defaultKeys;
    public List<HotkeyMenu.Key> generalKeys;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        defaultKeys = new List<HotkeyMenu.Key>(){
            HotkeyMenu.Key.Explore,
            HotkeyMenu.Key.HotkeyMenu
        };
        hotkeyMenu.Populate(defaultKeys);

        wallKeys = new List<HotkeyMenu.Key>(){
            HotkeyMenu.Key.DetachNode,
            HotkeyMenu.Key.DeleteItem,
            HotkeyMenu.Key.Cancel,
            HotkeyMenu.Key.Explore,
            HotkeyMenu.Key.HotkeyMenu
        };

        nodeKeys = new List<HotkeyMenu.Key>(){
            HotkeyMenu.Key.DetachNode,
            HotkeyMenu.Key.Cancel,
            HotkeyMenu.Key.Explore,
            HotkeyMenu.Key.HotkeyMenu
        };

        objectKeys = new List<HotkeyMenu.Key>(){
            HotkeyMenu.Key.DeleteItem,
            HotkeyMenu.Key.Cancel,
            HotkeyMenu.Key.Explore,
            HotkeyMenu.Key.HotkeyMenu
        };

        generalKeys = new List<HotkeyMenu.Key>(){
            HotkeyMenu.Key.Cancel,
            HotkeyMenu.Key.Explore,
            HotkeyMenu.Key.HotkeyMenu
        };
    }

    private class Nested
    {
        static Nested() { }

        internal static readonly CreatorHotKeyController instance = new CreatorHotKeyController();
    }

    public void OnDetachNode(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            if (TransformDatas.SelectedWall != null)
            {
                NewBuildingController.DetachWall(TransformDatas.SelectedWall.WallItem.name);
                TransformDatas.SelectedWall.Exit();
            }

            else if (TransformDatas.SelectedNode != null)
            {
                TransformDatas.SelectedNode.DetachNode();
                TransformDatas.SelectedNode.NodeExit();
            }
            DeselectAllItems();
        }
    }

    public void OnToggleKeyHelp(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            GameObject menuUI = hotkeyMenu.gameObject;

            if (menuUI != null)
            {
                menuUI.SetActive(!menuUI.activeSelf);
            }
        }
    }

    public void OnDeleteItem(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            if (TransformDatas.SelectedWall != null)
            {
                var temp = TransformDatas.SelectedWall.WallItem;
                DeselectAllItems();
                if (temp is NewWall)
                {
                    NewBuildingController.DetachWall(temp.name);
                }
                NewBuildingController.DeleteItem(temp.name);
            }

            if (TransformDatas.SelectedObject != null)
            {
                var temp = TransformDatas.SelectedObject.item;
                DeselectAllItems();
                NewBuildingController.DeleteItem(temp.name, false);
            }

            if (TransformDatas.SelectedWallObject != null)
            {
                var temp = TransformDatas.SelectedWallObject.item;
                DeselectAllItems();
                NewBuildingController.DeleteItem(temp.name, false);
            }
        }
    }


    public void OnDisplayNodes(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            foreach (var listner in TransformDatas.wallListenersList)
            {
                foreach (var node in listner.Value.nodes)
                {
                    Trace.Log($"{listner.Value.wallGO.name} LISTENING :: {node.Value.nodeGO.name} in floor {node.Value.floor.name}");
                }
            }
        }
    }

    public void OnExplore(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            TerrainBootstrap.Latitude = BuildingCanvas.centerLatLon[1];
            TerrainBootstrap.Longitude = BuildingCanvas.centerLatLon[0];
            TerrainEngine.TerrainController controller = TerrainEngine.TerrainController.Get();
            controller.latitudeUser = BuildingCanvas.centerLatLon[1].ToString();
            controller.longitudeUser = BuildingCanvas.centerLatLon[0].ToString();
            SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
            CreatorUIController.previousBuildingID = CreatorUIController.buildingID;
        }
    }

    public void OnLeftClicked(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
            {
                if (hitInfo.transform.name == "BuildingCanvas" && hitInfo.transform.tag != WHConstants.METABLOCK)
                {
                    DeselectAllItems();
                }
            }
        }
    }
    public void OnRightClicked(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();

            if (buildingInventoryController.currentBlock != null)
            {
                CreatorUIController.DeselectFlyOutButton();
                buildingInventoryController.currentBlock = null;
                buildingInventoryController.DeSelectAllObject();
                UnityEngine.Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);
            }
        }
    }

    public void Cancel(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();

            if (buildingInventoryController.currentBlock != null)
            {
                CreatorUIController.DeselectFlyOutButton();
                buildingInventoryController.currentBlock = null;
                buildingInventoryController.DeSelectAllObject();
                UnityEngine.Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);
            }

            else
            {
                DeselectAllItems();
            }
        }
    }
    public void OnCopy(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            if (TransformDatas.SelectedWall != null)
            {
                TransformDatas.ClipboardItem = TransformDatas.SelectedWall.WallItem;
            }
        }
    }

    public void OnPaste(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            if (TransformDatas.ClipboardItem != null)
            {
                var line = TransformDatas.SelectedWall.WallGO.GetComponent<LineRenderer>();
                var pos0 = line.GetPosition(0);
                var pos1 = line.GetPosition(1);

                var offset = new Vector3(1.5f, 1.5f, 0f);
                pos0 = pos0 - offset;
                pos1 = pos1 - offset;
                NewBuildingController.CreateWall(pos0, pos1, false);

                DeselectAllItems();
            }
        }
    }

    public void OnDeselect(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            DeselectAllItems();
        }
    }

    public void DeselectAllItems()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();

        if (buildingInventoryController.currentBlock != null)
        {
            CreatorUIController.DeselectFlyOutButton();
            buildingInventoryController.currentBlock = null;
            buildingInventoryController.DeSelectAllObject();
        }

        else
        {
            if (TransformDatas.SelectedNode != null)
            {
                TransformDatas.SelectedNode.nodeState = Node.NodeState.CLICKABLE;
                TransformDatas.SelectedNode.NodeExit();
                TransformDatas.SelectedNode = null;
            }

            if (TransformDatas.SelectedWallObject != null)
            {
                TransformDatas.SelectedWallObject.wallObjectState = WallObjectTransformHandler.WallObjectState.CLICKABLE;
                TransformDatas.SelectedWallObject.Exit();
                TransformDatas.SelectedWallObject = null;
            }

            if (TransformDatas.SelectedObject != null)
            {
                TransformDatas.SelectedObject.objectState = ObjectTransformHandler.ObjectState.CLICKABLE;
                TransformDatas.SelectedObject.Exit();
                TransformDatas.SelectedObject = null;
            }

            if (TransformDatas.SelectedWall != null)
            {
                foreach (var node in TransformDatas.SelectedWall.wallListener.nodes)
                {
                    node.Value.nodeState = Node.NodeState.CLICKABLE;
                    node.Value.NodeExit();
                }
                TransformDatas.SelectedWall.wallState = WallTransform.WallState.CLICKABLE;
                TransformDatas.SelectedWall.Exit();

                TransformDatas.SelectedWall = null;
            }
        }
        hotkeyMenu.Populate(defaultKeys);

        UnityEngine.Cursor.SetCursor(null, Vector3.zero, CursorMode.Auto);
    }
}