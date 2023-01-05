public static class WHConstants
{
    public const float FEET_TO_METER = 0.3048f; // 1ft = 0.3048 m
    public const float DefaultFloorHeight = 12.0f;
    public const string FeetUnit = "ft";
#if (ENV_TESTING || UNITY_EDITOR)
    public const string API_URL = "https://testingapi.app.earth9.net"; // TODO: use and fetch urls from environment 
    public const string ADMIN_API_URL = "https://testingadminapi.app.earth9.net";
    public const string WEB_URL = "https://testing.app.earth9.net";
    public const string VIDEO_CAPTURE_SUBMISSION_SUBPATH = "\\Documents\\earth9\\videoProcessing";
#elif ENV_PROD
    public const string API_URL = "https://api.app.earth9.net"; // TODO: use and fetch urls from environment 
    public const string ADMIN_API_URL = "https://adminapi.app.earth9.net";
    public const string WEB_URL = "https://app.earth9.net";
    public const string VIDEO_CAPTURE_SUBMISSION_SUBPATH = "\\Documents\\earth9-prod\\videoProcessing";
#endif
    public const string SUBMISSION_VERSION_ROUTE = "/creator-submissions/submissions/versions?buildingId=2aeb1d98-e772-4963-8ffc-192b6ccabdaf";
    public const string S3_BUCKET_PATH = "https://earth9.s3.amazonaws.com";
    public const string METABLOCK = "MetaBlock";
    public const float DefaultWall2DHeight = 0.4f;
    public const float DefaultWallBreadth = 0.3f;
    public const float DefaultWallHeight = DefaultFloorHeight * FEET_TO_METER;

    //Pointer Paths
    public const string CURSOR_PATH = "Sprites/Cursor/";
    public const string WALL_POINTER = CURSOR_PATH + "wall_pointer";
    public const string WINDOW_POINTER = CURSOR_PATH + "window_pointer";
    public const string DOOR_POINTER = CURSOR_PATH + "door_pointer";
    public const string ELEVATOR_POINTER = CURSOR_PATH + "elevator_pointer";
    public const string FINGER_POINTER = CURSOR_PATH + "Finger_Pointer";
    public const string DRAG_POINTER = CURSOR_PATH + "drag_icon";
    public const string ROTATE_POINTER = CURSOR_PATH + "rotate_icon";


    public const float DefaultWindowLength = 3.528f;
    public const float DefaultWindowBreadth = 0.3f;
    public const float DefaultWindowHeight = 2.0f;

    public const float DefaultWindow2DHeight = 0.375f;
    public const float DefaultWindowY = 1.0f;

    public const float DefaultDoorLength = 1.404f;
    public const float DefaultDoorHeight = 3.0f;
    public const float DefaultDoorBreadth = 0.3f;

    public const float DefaultDoor2DHeight = 0.375f;
    public const float DefaultDoorY = 0.0f;
    public const float DefaultElevatorLength = 3.125f;
    public const float DefaultElevator2DHeight = 3.125f;

    public const float DefaultZ = -0.01f;

    public static string USER = System.Windows.Forms.SystemInformation.UserName.ToString();

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    public static string PATH_DIVIDER = "/";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    public static string PATH_DIVIDER = "\\";
#endif

    // Assets and Building Item Names
    public const string ROOF = "Roof";
    public const string FLOOR_PLAN = "FloorPlan";
    public const string FLOOR = "Floor";
    public const string CEILING = "Ceiling";
    public const string BUILD = "Build";
    public const string WALL = "Wall";
    public const string DOOR = "Door";
    public const string ELEVATOR = "Elevator";
    public const string WINDOW = "Window";
    public const string FURNITURE = "Furniture";
}