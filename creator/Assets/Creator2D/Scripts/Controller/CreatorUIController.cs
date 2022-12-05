using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
public class CreatorUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Button saveButton;
    private Button submitButton;
    private Button addFloorButton;
    private Button mainMenuButton;
    private Button backToGameButton;
    private Button compassButton;
    private Button colorPickerButton;
    private Label versionLabel;
    private Label messageLabel;
    private VisualElement buttonsWrapper;

    // Start is called before the first frame update

    private BuildingInventoryController buildingInventoryController;

    [Header("Meta Blocks")]
    public MetaBlock[] availableMetaBlocks;

    private static VisualElement root;
    MetaBlock currentBlock;
    private string hideClassName = "hide";
    private string showClassName = "show";

    private static string mainPanelName = "main-panel";

    private string flyOutButtonName = "fly-out-button";
    private const string buttonActiveClassName = "button-active";

    private static List<VisualElement> flyOutElementList = new List<VisualElement>();

    public static VisualElement getRoot()
    {
        return CreatorUIController.root;
    }

    public void Start()
    {
        m_UIDocument = GetComponent<UIDocument>();
        CreatorUIController.root = m_UIDocument.rootVisualElement;
        CreatorUIController.root.Query(className: "toptool-button").
            ForEach((element) =>
            {
                SetupButton(element);
                var elemParent = element.parent;
                if (elemParent != null && elemParent.parent != null && elemParent.parent.name == flyOutButtonName)
                {
                    element.AddToClassList("bg-white");
                    flyOutElementList.Add(element);
                }
            });

        buildingInventoryController = BuildingInventoryController.Get();
        buildingInventoryController.SetMetaBlocks(availableMetaBlocks);
        buildingInventoryController.SetupBuildingInventories();

        addFloorButton = CreatorUIController.root.Query<Button>("add-floor-button");
        addFloorButton.clicked += OnAddFloor;

        messageLabel = CreatorUIController.root.Query<Label>("message-label");

        saveButton = CreatorUIController.root.Query<Button>("draft-button");
        saveButton.clicked += OnSave;

        submitButton = CreatorUIController.root.Query<Button>("submit-button");
        submitButton.clicked += OnSubmit;

        mainMenuButton = CreatorUIController.root.Query<Button>("main-menu-button");
        mainMenuButton.clicked += OnMainMenu;

        backToGameButton = CreatorUIController.root.Query<Button>("back-to-game-button");
        backToGameButton.clicked += OnBackToGame;

        compassButton = CreatorUIController.root.Query<Button>("compass-button");
        compassButton.AddManipulator(new CompassMouseManipulator());

        StartCoroutine(PrepareVersionData());

        GameObject player = SceneObject.GetPlayer(SceneObject.Mode.Creator);
        Trace.Assert(player != null, "SceneObject.Mode.Creator does not have a Player gameobject");
        player.AddComponent<VersionChanger>();

        //TODO convert gameobjects to UI Hierachy Elements and PAN Objects

        // if(WHFbxImporter.ImportObjects(_getFBXSubPath()) == 1){
        //     Debug.Log("Successfully imported fbx objects.");
        // }else{
        //     Debug.Log("Error on imporing fbx objects.");
        // }
    }
    IEnumerator PrepareVersionData()
    {
        VersionDownloader.PrepareData();
        yield return new WaitForSeconds(3);
    }
    private void OnSubmit()
    {
        DialogComponent dialog = new DialogComponent();
        CreatorUIController.root.Add(dialog);

        dialog.confirmed += () => Debug.Log("Email being sent");
        dialog.confirmed += () => CreatorUIController.root.Remove(dialog);
        dialog.cancelled += () => Debug.Log("Cancelled. email not sent");
        dialog.cancelled += () => CreatorUIController.root.Remove(dialog);
    }
    private void OnMainMenu()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
    }

    private void OnBackToGame()
    {
        SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
    }

    private static string SelectedFloorName = null;
    private void OnAddFloor()
    {
        NewBuildingController.CreateFloorPlan(SelectedFloorName);
        SetupAddFloorDropdown();
        SelectedFloorName = null;
    }

    private const string _add_floor_drop_down = "add-floor-drop-down";
    public static string _copy_from_below = "Copy From Below";

    private static List<string> GetFloorOptions()
    {
        List<string> floorOptionList = new List<string>();
        floorOptionList.Add(_copy_from_below);
        foreach (var item in NewBuildingController.GetBuilding().children)
        {
            floorOptionList.Add(item.name);
        }
        return floorOptionList;
    }

    public static void SetupAddFloorDropdown()
    {
        List<string> choices = GetFloorOptions();
        if (choices.Count > 1)
        {
            VisualElement AddFloorContent = CreatorUIController.root.Query<VisualElement>("add-floor");
            DropdownField AddFloorDropDown = AddFloorContent.Q<DropdownField>(_add_floor_drop_down);
            if (AddFloorDropDown == null)
            {
                AddFloorDropDown = new DropdownField();
                AddFloorDropDown.name = _add_floor_drop_down;
                AddFloorDropDown.AddToClassList("add-floor-drop-down");
                AddFloorDropDown.RegisterValueChangedCallback(RegisterAddFloorDropdownCallBacks);
                AddFloorContent.Insert(AddFloorContent.childCount - 1, AddFloorDropDown);
            }
            AddFloorDropDown.choices = choices;
            AddFloorDropDown.value = "Copy From Below";
        }
    }

    private static void RegisterAddFloorDropdownCallBacks(ChangeEvent<string> evt)
    {
        DropdownField df = evt.currentTarget as DropdownField;
        SelectedFloorName = df.value;
        // string AssetType = df.name.Replace(dropdownNameSuffix, "");
        // // SetUpMetaBlockButtons(AssetType, df.value);
    }

    private void SetupButton(VisualElement button)
    {
        // Reference to the VisualElement inside the button that serves
        // as the button's icon.
        var buttonIcon = button.Q(className: "toptool-button-icon");

        // Icon's path in our project.
        var iconPath = "Icons/" + button.parent.name.Replace(" ", "_").ToLower() + "_icon";

        // Loads the actual asset from the above path.
        var iconAsset = Resources.Load<Texture2D>(iconPath);

        // Applies the above asset as a background image for the icon.
        buttonIcon.style.backgroundImage = iconAsset;

        button.RegisterCallback<PointerUpEvent, VisualElement>(ButtonMethod, button);
        // Sets a basic tooltip to the button itself.
        button.tooltip = button.parent.name;
        // button.AddManipulator(new ToolTipManipulator());
    }

    void ButtonMethod(PointerUpEvent _evt, VisualElement button)
    {
        // Instantiates our primitive object on a left click.
        var primitiveTypeName = string.Concat("On", button.parent.name.Replace(" ", ""));
        Type thisType = this.GetType();
        MethodInfo theMethod = thisType.GetMethod(primitiveTypeName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (theMethod != null)
        {
            theMethod.Invoke(this, new object[1] { button });
        }
    }

    void OnSelector(VisualElement button)
    {
        DeselectFlyOutButton();
        SelectFlyOutButtonControl(button);
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();
    }

    void OnBuildingDrag(VisualElement button)
    {
        DeselectFlyOutButton();
        SelectFlyOutButtonControl(button);
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();
    }

    void OnVideoCamera(VisualElement button)
    {
        DeselectFlyOutButton();
        SelectFlyOutButtonControl(button);
        buildingInventoryController.currentBlock = null;
        buildingInventoryController.DeSelectAllObject();
    }

    public static void DeselectFlyOutButton()
    {
        flyOutElementList.
            ForEach((element) =>
            {
                if (element.ClassListContains(buttonActiveClassName))
                {
                    var elemParent = element.parent;
                    element.RemoveFromClassList(buttonActiveClassName);
                    elemParent.RemoveFromClassList(buttonActiveClassName);

                    var buttonIcon = element.Q(className: "toptool-button-icon");
                    var iconPath = "Icons/" + elemParent.name.Replace(" ", "_").ToLower() + "_icon";
                    var iconAsset = Resources.Load<Texture2D>(iconPath);
                    buttonIcon.style.backgroundImage = iconAsset;
                }
            });
    }

    private void SelectFlyOutButtonControl(VisualElement button)
    {

        var elemParent = button.parent;
        button.AddToClassList(buttonActiveClassName);
        elemParent.AddToClassList(buttonActiveClassName);

        var buttonIcon = button.Q(className: "toptool-button-icon");
        var iconPath = "Icons/" + elemParent.name.Replace(" ", "_").ToLower() + "_white_icon";
        var iconAsset = Resources.Load<Texture2D>(iconPath);
        buttonIcon.style.backgroundImage = iconAsset;
    }
    void OnCopyAll(VisualElement button)
    {
        Debug.Log("CopyAllButtonPressed");
    }

    void OnContentCut(VisualElement button)
    {
        Debug.Log("ContentCutButtonPressed");
    }

    void OnSave()
    {
        Debug.Log("On Save pressed");
        messageLabel.RemoveFromClassList("hide");
        messageLabel.AddToClassList("show");
        messageLabel.text = "Generating FBX...";
        GameObject structure = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.CREATOR_STRUCTURE);
        GameObject building = Item3d.getBuildingGameObject();
        if (building != null)
        {
            building.transform.parent = structure.transform;
        }
        string subPath = _getFBXSubPath();
        string path = subPath + "\\myCreation.fbx";
        bool subPathExists = System.IO.Directory.Exists(subPath);
        if (!subPathExists)
        {
            System.IO.Directory.CreateDirectory(subPath);
        }
        WHFbxExporter.ExportObjects(path, path.Substring(0, path.LastIndexOf("\\")), structure);
        if (building != null)
        {
            Destroy(building);
        }
        messageLabel.text = "FBX Saved Successfully.";
        StartCoroutine(HideMessage());
    }

    IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(2f);
        messageLabel.RemoveFromClassList("show");
        messageLabel.AddToClassList("hide");
    }

    void OnThreeDRotation(VisualElement button)
    {
        VisualElement threeDPanel = CreatorUIController.root.Q<VisualElement>("threed-panel");
        VisualElement mainPanel = CreatorUIController.root.Q<VisualElement>("main-panel");
        if (threeDPanel.ClassListContains(hideClassName))
        {
            threeDPanel.RemoveFromClassList(hideClassName);
            threeDPanel.AddToClassList(showClassName);
            mainPanel.RemoveFromClassList("col-xs-10");
            mainPanel.AddToClassList("col-xs-6");
        }
        else
        {
            threeDPanel.RemoveFromClassList(showClassName);
            threeDPanel.AddToClassList(hideClassName);
            mainPanel.RemoveFromClassList("col-xs-6");
            mainPanel.AddToClassList("col-xs-10");
        }
    }

    private string _getFBXSubPath()
    {
        /**
        todo: 1. refine folder structure . what about cross platform?
              2. move the method in utils.
         path = C:/Users/<PC_USER_NAME>/Documents/earth9/<PLOT_ID>/<BUILDING_ID>
        
        **/
        return "C:\\Users\\" + System.Windows.Forms.SystemInformation.UserName.ToString() + "\\Documents\\earth9\\eeb52773-318c-4a4b-a16b-c5ff0bb72623\\eeb52773-318c-4a4b-a16b-c5ff0bb72623";
    }

    public static bool isInputOverVisualElement()
    {
        VisualElement picked = CreatorUIController.root.panel.Pick(RuntimePanelUtils.ScreenToPanel(CreatorUIController.root.panel, Input.mousePosition));
        return (picked != null && picked.name != mainPanelName);
    }

    public static VisualElement getVisualElement(Vector2 mousePos)
    {
        VisualElement picked = CreatorUIController.root.panel.Pick(RuntimePanelUtils.ScreenToPanel(CreatorUIController.root.panel, mousePos));
        return picked;
    }
}