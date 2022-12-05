using System.Collections.Generic;
using UnityEngine;

public class TerrainPlayer : MonoBehaviour
{
    public GameObject FallbackTerrain; // Assigned via the Inspector

    private Camera _camera = null;
    private FloatingOriginAdvanced _floatingOrigin = null;
    bool _initialized = false;

    public static TerrainPlayer Get(GameObject gameObject)
    {
        GameObject playerObject = SceneObject.PlayerObjectOf(gameObject);
        Trace.Assert(playerObject != null,
            "There is no scene player for the GameObject '{0}'.", gameObject.name);

        TerrainPlayer terrainPlayer = playerObject.GetComponent<TerrainPlayer>();
        if (terrainPlayer != null)
        {
            FireTerrainLoadStage(LoadStage.PlayerInit);

            return terrainPlayer.Initialize() ? terrainPlayer : null;
        }

        return terrainPlayer;
    }

    public bool Initialize()
    {
        if (!_initialized)
        {
            if (_camera == null)
            {
                GameObject cameraObject = SceneObject.CameraObjectOf(gameObject);
                Trace.Assert(cameraObject != null,
                    "There is no scene camera for the GameObject '{0}'.", gameObject.name);

                _camera = cameraObject.GetComponent<Camera>();
                Trace.Assert(_camera != null,
                    "Scene camara '(0}' is missing Camera component", cameraObject.name);
            }

            if (_floatingOrigin == null)
            {
                _floatingOrigin = GetComponent<FloatingOriginAdvanced>();
                Trace.Assert(_floatingOrigin != null, 
                    "GameObject '{0}' is missing FloatingOriginAdvanced component", gameObject.name);
            }

            Rigidbody rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                rigidBody.mass = 1f;
                rigidBody.drag = Mathf.Infinity;
                rigidBody.angularDrag = Mathf.Infinity;
                rigidBody.useGravity = false;
                rigidBody.isKinematic = true;
                rigidBody.interpolation = RigidbodyInterpolation.None;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            _initialized = true;
        }

        return _initialized;
    }

    public Camera Camera
    {
        get 
        {
            Initialize();
            return _camera; 
        }
    }

    public FloatingOriginAdvanced FloatingOrigin
    {
        get 
        {
            Initialize();
            return _floatingOrigin; 
        }
    }

    [HideInInspector()]
    public Vector3 BoundsOrigin
    {
        get { return PlayerController.Get(gameObject).BoundsOrigin;  }
        set { PlayerController.Get(gameObject).BoundsOrigin = value; }
    }

    public void ResetSpatialInput()
    {
        PlayerController.Get(gameObject).ResetSpatialInput();
    }

    public void EnableUIInput(bool enable)
    {
        Initialize();
        PlayerController.Get(gameObject).EnableUIInput(enable);
    }

    public float PhysicalHeight()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if( controller != null)
        {
            return controller.height;
        }
        return 0;
    }

    public bool HitTestBelow(out RaycastHit hit)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
        {
            return true;
        }
        return false;
    }

    public Vector3 WorldPosition
    {
        get
        {
            return (_floatingOrigin != null && _floatingOrigin.enabled) ?
                _floatingOrigin.absolutePosition : 
                gameObject.transform.position;
        }

        set
        {
            gameObject.transform.position = value;

            if (_floatingOrigin != null)
            {
                _floatingOrigin.worldOffset = Vector3.zero;
                _floatingOrigin.absolutePosition = value;
            }
            BoundsOrigin = value;
        }
    }

    #region Events
    public enum LoadStage
    {
        SceneInit,
        PlayerInit,
        LoadBegin,
        TexturesGenerated,
        TerrainsGenerated,
        PlayerPositioned,
        LoadComplete,
    };
    public static readonly Dictionary<LoadStage, string> s_loadStageMsgs = new Dictionary<LoadStage, string>()
    {
        { LoadStage.SceneInit,          "Initializing scene." },
        { LoadStage.PlayerInit,         "Initializing player." },
        { LoadStage.LoadBegin,          "Loading world terrain..." },
        { LoadStage.TexturesGenerated,  "Terrain textures have been generated..." },
        { LoadStage.TerrainsGenerated,  "Terrain objects have been generated..." },
        { LoadStage.PlayerPositioned,   "Player is positioned." },
        { LoadStage.LoadComplete,       "Terrain is ready." },
    };
    public delegate void TerrainLoadStageChanged(LoadStage newStage, string loadStageMessage);

    public enum ServerError
    {
        ConnectionFailed
    };
    public static readonly Dictionary<ServerError, string> s_serverErrorMsgs = new Dictionary<ServerError, string>()
    {
        { ServerError.ConnectionFailed,          "Connection to terrain server failed." },
    };
    public delegate void TerrainServerError(ServerError newStage, string serverErrorMessage);

    #endregion

    #region Events Triggers
    //  External class don't trigger events directly
    public static event TerrainLoadStageChanged OnTerrainLoadStageChanged;
    //  External classes call this trigger wrapper instead
    public static void FireTerrainLoadStage(LoadStage newStage)
    {
        if (OnTerrainLoadStageChanged != null)
        {
            OnTerrainLoadStageChanged(newStage, s_loadStageMsgs[newStage]);
        }
    }

    //  External class don't trigger events directly
    public static event TerrainServerError OnTerrainServerError; 
    //  External classes call this trigger wrapper instead
    public static void FireServerConnectionError(ServerError serverError)
    {
        Trace.Log(s_serverErrorMsgs[serverError]);
        if (OnTerrainLoadStageChanged != null)
        {
            OnTerrainServerError(serverError, s_serverErrorMsgs[serverError]);
        }
    }

    #endregion

    #region Event Handlers

    public void OnServerError(ServerError serverError, string errorMsg)
    {
        Trace.Warning(errorMsg);

        GameObject terrainPresenter = SceneObject.Find(
            SceneObject.Mode.Player, ObjectName.TERRAIN_PRESENTER);

        Trace.Assert(terrainPresenter != null,
            "Missing gameobject {0} in the {1} scene mode",
            ObjectName.TERRAIN_PRESENTER, SceneObject.Mode.Player);

        switch (serverError)
        {
            case ServerError.ConnectionFailed:
                //  TODO: ServerError.ConnectionFailed handler
                break;

            default:
                break;
        }
    }

    public void OnTerrainLoadStage(LoadStage loadStage, string loadStageMessage)
    {
        Trace.Log(loadStageMessage);
    }

    #endregion

    void Awake()
    {
        OnTerrainServerError += OnServerError;
        OnTerrainLoadStageChanged += OnTerrainLoadStage;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    #region Transform accessors

    public Vector3 position
    {
        get { return gameObject.transform.position; }
        set { gameObject.transform.position = value; }
    }

    public Vector3 right
    {
        get { return gameObject.transform.right; }
        set { gameObject.transform.right = value; }
    }

    public Vector3 up
    {
        get { return gameObject.transform.up; }
        set { gameObject.transform.up = value; }
    }

    public Quaternion rotation
    {
        get { return gameObject.transform.rotation; }
        set { gameObject.transform.rotation = value; }
    }

    public Quaternion localRotation
    {
        get { return gameObject.transform.localRotation; }
        set { gameObject.transform.localRotation = value; }
    }

    public Vector3 eulerAngles
    {
        get { return gameObject.transform.eulerAngles; }
        set { gameObject.transform.eulerAngles = value; }
    }

    public Vector3 localEulerAngles
    {
        get { return gameObject.transform.localEulerAngles; }
        set { gameObject.transform.localEulerAngles = value; }
    }

    public Vector3 forward
    {
        get { return gameObject.transform.forward; }
        set { gameObject.transform.forward = value; }
    }

    public void LookAt(Vector3 worldPosition)
    {
        gameObject.transform.LookAt(worldPosition);
    }
    public void LookAt(Transform target)
    {
        gameObject.transform.LookAt(target);

    }

    public void Translate(float x, float y, float z)
    {
        gameObject.transform.Translate(x, y, z);
    }
    public void Translate(Vector3 translation)
    {
        gameObject.transform.Translate(translation);
    }
    public void Translate(float x, float y, float z, Transform relativeTo)
    {
        gameObject.transform.Translate(x, y, z, relativeTo);
    }
    public void Translate(Vector3 translation, Transform relativeTo)
    {
        gameObject.transform.Translate(translation, relativeTo);
    }

    #endregion
};
