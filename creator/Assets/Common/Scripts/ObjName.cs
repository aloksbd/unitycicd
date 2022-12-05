/// <summary>
//
//  The static ObjectName class defines GameObject names for use by scripts
//  
//  We define these as const values in one script so that we can easily make
//  make changes to the gameObject hierarchy without having to touch a large
//  number of client scripts.
//
/// </summary>

public static class ObjectName
{
    //  Mode Containers
    public const string WELCOME_MODE = "Welcome Mode";
    public const string PLAYER_MODE = "Player Mode";
    public const string CREATOR_MODE = "Creator Mode";
    public const string ELEVATOR_MODE = "Elevator Mode";

    //----------------------------------------------
    //  Player Mode GameObjects

    //  UI container 
    public const string UI_CONTAINER = "UI";
    //  UI container chldren
    public const string CROSSHAIRS_IMAGE = UI_CONTAINER + "/Crosshairs";
    public const string CROSSHAIRS_HIT_IMAGE = UI_CONTAINER + "/Crosshairs_Hit";
    public const string UPLOADTEXT = UI_CONTAINER + "/UploadText";

    //  Elevator container
    public const string ELEVATOR_CONTAINER = "Elevator";

    //  Terrain management
    public const string TERRAIN_PRESENTER = "Terrain Presentation";
    public const string TERRAIN_GENERATOR = "Terrain Generator";

    //  Terrain containers
    public const string TERRAIN_CONTAINER = "Terrain";
    public const string NEAR_TERRAIN_CONTAINER = TERRAIN_CONTAINER + "/Near";
    public const string NEAR_TERRAIN_TEMP = TERRAIN_CONTAINER + "/Near_Temp";
    public const string FAR_TERRAIN_CONTAINER = TERRAIN_CONTAINER + "/Far";
    public const string FAR_TERRAIN_TEMP = TERRAIN_CONTAINER + "/Far_Temp";

    //  Building containers
    public const string BUILDING_CONTAINER = "Buildings";
    public const string GENERATED_BUILDING_CONTAINER = TERRAIN_CONTAINER + "/Buildings";
    public const string GENERATED_BUILDING_PYLONS_CONTAINER = TERRAIN_CONTAINER + "/BuildingPylons";
    //  Buildings container children
    public const string BUILDING = BUILDING_CONTAINER + "/Building";
    public const string GENERATED_BUILDING = GENERATED_BUILDING_CONTAINER + "/Building";
    public const string GENERATED_BUILDING_PYLONS = GENERATED_BUILDING_PYLONS_CONTAINER + "/Building";

    //  Video Capture
    public const string PEGASUS_CAMERA_GAMEOBJECT = "PegasusCamera";
    public const string PEGASUS_TARGET_GAMEOBJECT = "PegasusTarget";
    public const string PEGASUS_MANAGER_GAMEOBJECT = "PegasusManager";
    public const string PEGASUS_VIDEO_CAMERA = "VideoCamera";
    public const string CREATOR_ASSET_TYPE_FBX = "fbx";
    public const string CREATOR_ASSET_TYPE_VIDEO = "video";

    //----------------------------------------------
    //  Creator Mode GameObjects
    public const string HARNESS_ELEMENT = "HarnessElement";
    public const string BUILDING_CANVAS = "BuildingCanvas";
    public const string CREATOR_BUILDING = "Building";
    public const string CREATOR_STRUCTURE = "Structure";

    // Harness objects
    public const string BOUNDING_BOX = "BoundaryBox";
    public const string DRAG_HARNESS = "DragHarness";
    public const string ROTATE_HARNESS = "RotateHarness";
    public const string LEFT_RESIZE_HARNESS = "LeftResizeHarness";
    public const string RESIZE_HARNESS = "ResizeHarness";

    public const string TOP_LEFT_RESIZE_HARNESS = "TopLeftResizeHarness";
    public const string TOP_RESIZE_HARNESS = "TopResizeHarness";
    public const string TOP_RIGHT_RESIZE_HARNESS = "TopRightResizeHarness";
    public const string RIGHT_RESIZE_HARNESS = "RightResizeHarness";
    public const string BOTTOM_RIGHT_RESIZE_HARNESS = "BottomRightResizeHarness";
    public const string BOTTOM_RESIZE_HARNESS = "BottomResizeHarness";
    public const string BOTTOM_LEFT_RESIZE_HARNESS = "BottomLeftResizeHarness";

    // Resize harness operations
    public const string LEFT_RESIZE_NAME = "left";
    public const string TOP_LEFT_RESIZE_NAME = "top_left";
    public const string TOP_RESIZE_NAME = "up";
    public const string TOP_RIGHT_RESIZE_NAME = "top_right";
    public const string RIGHT_RESIZE_NAME = "right";
    public const string BOTTOM_RIGHT_RESIZE_NAME = "bottom_right";
    public const string BOTTOM_RESIZE_NAME = "down";
    public const string BOTTOM_LEFT_RESIZE_NAME = "bottom_left";
}
