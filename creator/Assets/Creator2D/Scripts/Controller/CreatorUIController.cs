using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using TerrainEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using UnityEngine.Networking;
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

    public static string buildingID = null;
    public static GameObject buildingGO;
    public static string previousBuildingID;
    public static VisualElement getRoot()
    {
        return CreatorUIController.root;
    }
    public static bool UnSavedProgress;

    async public void Awake()
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

        UserProfileDesign();
        VisualElement topPanel = CreatorUIController.root.Q<VisualElement>("top-menu-panel");
        VisualElement mainPanel = CreatorUIController.root.Q<VisualElement>("mainPanel");
        topPanel.BringToFront();
        topPanel.style.position = Position.Absolute;
        topPanel.style.left = 0;
        topPanel.style.top = 0;

        mainPanel.style.marginTop = 60;

        GameObject player = SceneObject.GetPlayer(SceneObject.Mode.Creator);
        Trace.Assert(player != null, "SceneObject.Mode.Creator does not have a Player gameobject");
        player.AddComponent<VersionChanger>();

#if UNITY_EDITOR
        if (WelcomeUIController.buildingID != null && WelcomeUIController.buildingID != "")
        {
            OsmBuildingData buildingData = await OsmBuildings.GetBuildingDetail(WelcomeUIController.buildingID);
            CreateBuildingCanvas(buildingData);
            previousBuildingID = WelcomeUIController.buildingID;
        }
#else
        if (TerrainController.Get() == null || TerrainController.Get().GetState() == TerrainController.TerrainState.Unloaded)
        {
            OsmBuildingData buildingData = await OsmBuildings.GetBuildingDetail();
            previousBuildingID = buildingData.id;
            CreateBuildingCanvas(buildingData);
        }
#endif
    }

    private async void UserProfileDesign()
    {
        UserProfileData userData = await UserProfile.GetUserProfileData();
        Label userNameLabel = CreatorUIController.root.Q<Label>("UserName");
        userNameLabel.text = userData.firstname + " " + userData.lastname;
        VisualElement avatarFrame = CreatorUIController.root.Q<VisualElement>("avatar-frame");

        if (userData.profilePicture != null && userData.profilePicture.location != null & userData.profilePicture.filename != null)
        {
            string url = WHConstants.S3_BUCKET_PATH + "/" + userData.profilePicture.location + "/" + userData.profilePicture.filename;
            StartCoroutine(DownloadProfilePicture(url, avatarFrame));
        }
        else
        {
            StyleBackground backgroundImage = new StyleBackground(Resources.Load<Texture2D>("Icons/avatar_icon"));
            avatarFrame.style.backgroundImage = backgroundImage;
        }

        VisualElement UserFrame = CreatorUIController.root.Q<VisualElement>("user-frame");
        UserFrame.AddManipulator(new Clickable(evt => BuildUserProfileDropDown()));
    }

    void BuildUserProfileDropDown()
    {
        VisualElement UserDropDownMenu = CreatorUIController.root.Q<VisualElement>("user-dropdown-menu");
        var opened = UserDropDownMenu.ClassListContains("dropdown-menu-open");
        if (!opened)
        {
            VisualElement dropDownContainer = new VisualElement();
            dropDownContainer.name = "dropdown-container";
            VisualElement MyProfileElement = new VisualElement();
            MyProfileElement.AddToClassList("dropdown-item");
            MyProfileElement.AddToClassList("dropdown-menu-open");

            TextElement profileText = new TextElement();
            profileText.text = "My Profile";
            MyProfileElement.Add(profileText);
            profileText.AddManipulator(new Clickable(evt => goToProfile()));
            dropDownContainer.Add(MyProfileElement);

            VisualElement separator = new VisualElement();
            separator.AddToClassList("dropdown-menu-divider");
            dropDownContainer.Add(separator);

            VisualElement LogoutElement = new VisualElement();
            LogoutElement.AddToClassList("dropdown-item");
            LogoutElement.AddToClassList("dropdown-menu-open");

            TextElement LogoutText = new TextElement();
            LogoutText.text = "Logout";
            LogoutElement.Add(LogoutText);
            LogoutElement.AddManipulator(new Clickable(evt => logoutUser()));
            dropDownContainer.Add(LogoutElement);

            UserDropDownMenu.Add(dropDownContainer);
            UserDropDownMenu.AddToClassList("dropdown-menu-open");
        }
        else
        {
            VisualElement dropDownContainer = CreatorUIController.root.Q<VisualElement>("dropdown-container");
            if (dropDownContainer != null)
            {
                UserDropDownMenu.Remove(dropDownContainer);
            }
            UserDropDownMenu.RemoveFromClassList("dropdown-menu-open");
        }
    }

    //Open the profile page of the user
    void goToProfile()
    {
        Application.OpenURL($"{WHConstants.WEB_URL}/account");
    }

    // Logout the user from app
    void logoutUser()
    {
        string unsubmittedId = CreatorSubmission.GetUsersUnSubmittedBuildingId();
        var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);

        if (unsubmittedId == null || UnSavedProgress)
        {
            LoadingUIController.ActiveMode = LoadingUIController.Mode.UnsavedLogout;
            loadingUI.SetActive(true);
        }

        else
        {
            AuthenticationHandler.Logout();
            var bootstrap = GameObject.Find(ObjectName.BOOTSTRAP_OBJECT).GetComponent<AppBootstrap>();
            bootstrap.Init();
            loadingUI.SetActive(false);
        }
    }

    IEnumerator DownloadProfilePicture(string url, VisualElement avatarFrame)
    {
        using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerTexture();
            yield return uwr.SendWebRequest();
            StyleBackground backgroundImage = new StyleBackground(DownloadHandlerTexture.GetContent(uwr));
            avatarFrame.style.backgroundImage = backgroundImage;
        }
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
        TerrainBootstrap.Latitude = BuildingCanvas.centerLatLon[1];
        TerrainBootstrap.Longitude = BuildingCanvas.centerLatLon[0];
        TerrainEngine.TerrainController controller = TerrainController.Get();
        controller.latitudeUser = BuildingCanvas.centerLatLon[1].ToString();
        controller.longitudeUser = BuildingCanvas.centerLatLon[0].ToString();
        SceneObject.Get().ActiveMode = SceneObject.Mode.Player;
        previousBuildingID = buildingID;
    }

    public async static void CreateBuildingCanvas(OsmBuildingData buildingData)
    {
        CreatorItem buildingItem = NewBuildingController.GetBuilding();
        if (buildingItem != null)
        {
            NewBuildingController.SetCurrentFloorPlan(null);
            NewBuildingController.SetBuilding(null);
            buildingItem.Destroy();
        }
        BuildingCanvas buildingCanvas = BuildingCanvas.Get();
        buildingID = buildingData.id;
        string fbxName = WHConstants.PATH_DIVIDER + "myCreation.fbx";
        string fbxPath = CacheFolderUtils.fbxFolder(buildingID);
        if (File.Exists(fbxPath + fbxName))
        {
            ImportFbx(buildingCanvas, buildingData, fbxPath + fbxName);
            return;
        }
        else if (Directory.Exists(TerrainRuntime.LOCALCACHE_AUTHORED_BUILDINGS_FBX_FOLDER + buildingID))
        {
            string filePath = Directory.GetFiles(TerrainRuntime.LOCALCACHE_AUTHORED_BUILDINGS_FBX_FOLDER + buildingID)[0];
            ImportFbx(buildingCanvas, buildingData, filePath);
            return;
        }
        else if (!Directory.Exists(TerrainRuntime.LOCALCACHE_AUTHORED_BUILDINGS_FBX_FOLDER + buildingID) && buildingData.asset != null)
        {
            string filePath = TerrainRuntime.LOCALCACHE_AUTHORED_BUILDINGS_FBX_FOLDER + buildingID + WHConstants.PATH_DIVIDER + buildingData.asset.fbx.filename;
            await VersionDownloader.DownloadFileTaskAsync(WHConstants.S3_BUCKET_PATH + "/" + buildingData.asset.fbx.location + "/" + buildingData.asset.fbx.filename, filePath);
            ImportFbx(buildingCanvas, buildingData, filePath);
            return;
        }
        else
        {
            buildingCanvas.GenerateCanvas(buildingData);
        }
    }


    private static void ImportFbx(BuildingCanvas buildingCanvas, OsmBuildingData buildingData, string filePath)
    {
        buildingCanvas.GenerateCanvas(buildingData, false);
        var floorBoundary = new List<Vector3>();
        foreach (var coord in BuildingCanvas.boundaryCoordinates)
        {
            floorBoundary.Add(new Vector3(coord.x, 0, coord.y));
        }
        if (WHFbxImporter2D.ImportObjects(filePath, floorBoundary) == 1)
        {
            SetupAddFloorDropdown();
            Debug.Log("Successfully imported fbx objects.");
        }
        else
        {
            Debug.Log("Error on imporing fbx objects.");
        }
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
            if (item.name.Contains("FloorPlan"))
            {
                floorOptionList.Add(item.name);
            }
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

    async void OnSave()
    {
        Debug.Log("On Save pressed");
        GameObject building = null;
        try
        {
            var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
            LoadingUIController.ActiveMode = LoadingUIController.Mode.Saving;
            loadingUI.SetActive(true);
            GameObject structure = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.CREATOR_STRUCTURE);
            building = Item3d.getBuildingGameObject();
            if (building != null)
            {
                building.transform.parent = structure.transform;
            }
            string subPath = CacheFolderUtils.fbxFolder(buildingID);
            string path = subPath + WHConstants.PATH_DIVIDER + "myCreation.fbx";
            bool subPathExists = System.IO.Directory.Exists(path);
            if (!subPathExists)
            {
                string buildingId = CreatorUIController.buildingID;
                await CreatorSubmission.ActiveBuilds(buildingId);
            }
            WHFbxExporter.ExportObjects(path, path.Substring(0, path.LastIndexOf(WHConstants.PATH_DIVIDER)), structure);
            loadingUI.SetActive(false);
            UnSavedProgress = false;
        }
        catch (Exception e)
        {
            Trace.Exception(e);
        }
        finally
        {
            if (building != null)
            {
                Destroy(building);
            }
        }
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

    void OnZoomIn(VisualElement button)
    {
        CreatorEventManager._ZoomBuildingCanvas(-1);
    }

    void OnZoomOut(VisualElement button)
    {
        CreatorEventManager._ZoomBuildingCanvas(1);
    }


    public static bool isInputOverVisualElement()
    {
        VisualElement picked = CreatorUIController.root.panel.Pick(RuntimePanelUtils.ScreenToPanel(CreatorUIController.root.panel, Input.mousePosition));
        return (picked != null && picked.name != mainPanelName);
    }

    public static VisualElement getVisualElement()
    {
        VisualElement picked = CreatorUIController.root.panel.Pick(RuntimePanelUtils.ScreenToPanel(CreatorUIController.root.panel, Input.mousePosition));
        return picked;
    }
}