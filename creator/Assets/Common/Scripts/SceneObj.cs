using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

//  SceneObject.cs - Allows single scene switching among scene "modes"
//
//  A scene mode is represented by a hierarchy of GameObjects whose root GameObject
//  is selectively enabled or disabled. Premises:
//
//  1. Only one scene mode can be active at a time.
//  2. Each scene mode must have a single GameObject of type ScenePlayer among its children.
//  3. Each ScenePlayer must have a single GameObject of type Camera among its children.

//  SceneObject singleton interface
//  
//  -  Allows scripts to activate a scene mode within a scene. 
//  -  Allows scripts to determine the scene mode to which a particular GameObject belongs.
//  -  Allows scripts to determine the ScenePlayer and camera for the scene mode to which the
//     GameObject belongs.
//  -  Replaces 'GameObject.Find()' and 'new GameObject()' to identify and
//     create gameobjects parented to a SceneMode, resp.  GameObject.Find() doesn't
//     identify inactive GameObjects. Creating GameObjects using its operator new() 
//     is burdensome and unreliable for creating a lineage of GameObjects belonging
//     to a particular scene mode.

public class SceneObject
{
    //  Gameplay modes are implemented as first-tier game objects.
    //  Gameplay modes are mutually exclusive; there can only be a single
    //  mode (or none) active at a given time.
    public enum Mode
    {
        Welcome = 0,
        Player,
        Creator,
        Elevator,

        //  The following value doubles as the total count of available modes and
        //  an invalid or unassigned value
        INVALID,
        COUNT = INVALID
    };

    //  Static list of gameplay modes implemented as first-tier game objects.
    //  See the ModeInstance implementation below for details.
    private static readonly Dictionary<Mode, ModeInstance> s_modes = new Dictionary<Mode, ModeInstance>()
    {
        { Mode.Welcome,  new ModeInstance(ObjectName.WELCOME_MODE,  false /* useSkybox */,  false /* useEventSystem */) },
        { Mode.Player,   new ModeInstance(ObjectName.PLAYER_MODE,   true  /* useSkybox */,  true  /* useEventSystem */) },
        { Mode.Creator,  new ModeInstance(ObjectName.CREATOR_MODE,  false /* useSkybox */,  false /* useEventSystem */) },
        { Mode.Elevator, new ModeInstance(ObjectName.ELEVATOR_MODE, false /* useSkybox */,  true  /* useEventSystem */) }
    };

    //  Class members
    private Mode activeMode = Mode.INVALID; // no mode is active at time of construction
    private Mode prevActiveMode = Mode.INVALID; // no mode is active at time of construction

    //  Singleton pattern
    private static SceneObject s_instance = null;

    public static SceneObject Get()
    {
        if (s_instance == null)
        {
            s_instance = new SceneObject();
            s_instance.Initialize();
        }
        return s_instance;
    }

    //  constants
    private static readonly string[] PATH_TOKENS = { "/", "\\" };

    //-------------------------------------//
    //  Public properties and methods

    //  Retrieve the scene mode to which the specified object belongs
    public static SceneObject.Mode SceneModeOf(GameObject gameObject)
    {
        Get();
        GameObject sceneModeObject = SceneModeObjectOf(gameObject);
        if (sceneModeObject != null)
        {
            SceneMode sceneMode = sceneModeObject.GetComponent<SceneMode>();
            return sceneMode.Mode;
        }
        return Mode.INVALID;
    }

    //  Retrieve the ScenePlayer GameObject of the scene mode to which the specified object belongs
    public static GameObject PlayerObjectOf(GameObject gameObject)
    {
        Get();
        SceneObject.Mode mode = SceneModeOf(gameObject);
        return (mode != Mode.INVALID) ?
            GetPlayer(mode) :
            null;
    }

    //  Retrieve the camera GameObject of the scene mode to which the specified object belongs
    public static GameObject CameraObjectOf(GameObject gameObject)
    {
        Get();
        SceneObject.Mode mode = SceneModeOf(gameObject);
        return (mode != Mode.INVALID) ?
            GetCamera(mode) :
            null;
    }

    //  Retrieve the camera GameObject of the scene mode to which the specified object belongs
    public static PlayerController PlayerControllerOf(GameObject gameObject)
    {
        Get();
        SceneObject.Mode mode = SceneModeOf(gameObject);
        return (mode != Mode.INVALID) ?
            GetPlayerController(mode) :
            null;
    }

    //  Retrieve the GameObject of the scene mode to which the specified object belongs
    public static GameObject SceneModeObjectOf(GameObject gameObject)
    {
        Get();

        while (gameObject != null)
        {
            SceneMode sceneMode = gameObject.GetComponent<SceneMode>();
            if (sceneMode != null)
            {
                return gameObject;
            }

            Transform transform = gameObject.GetComponent<Transform>();
            gameObject = (transform != null && transform.parent != null) ?
                transform.parent.gameObject :
                null;
        }
        return null;
    }

    //  Replacements for GameObject.Find() that doesn't skip inactive gameobjects
    public static GameObject Find(Mode mode)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        Trace.Assert(instance.gameObject != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

        return instance.gameObject;
    }

    public static GameObject Find(Mode mode, string path)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        GameObject obj = instance.gameObject;
        Trace.Assert(obj != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

        //  Enumerate the components of the GameObject path looking for the matching child gameobject.
        //  If found, repeat until we reach the last node.
        string[] childObjectNames = path.Split(PATH_TOKENS, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; (i < childObjectNames.Length) && (obj != null); i++)
        {
            Transform[] childTransforms = obj.GetComponentsInChildren<Transform>(true);
            obj = null;
            foreach (Transform t in childTransforms)
            {
                if (t.name == childObjectNames[i])
                {
                    obj = t.gameObject;
                    if (i == childObjectNames.Length - 1)
                    {
                        return obj;
                    }
                    break;
                }
            }
        }
        return null;
    }

    //  Create one or a lineage of Gameobjects parented to the specified scene mode.     
    //  If any of the GameObjects of the lineage defined by the 'path' argument do not
    //  exist, they will be created.
    public static GameObject Create(
        Mode mode,
        string path)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        GameObject obj = instance.gameObject;
        Trace.Assert(obj != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Create().");

        //  Enumerate the components of the GameObject path looking for a matching child gameobject.
        //  If it doesn't exist, create it. 
        string[] childObjectNames = path.Split(PATH_TOKENS, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < childObjectNames.Length; i++)
        {
            GameObject childObj = null;
            Transform[] childTransforms = obj.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in childTransforms)
            {
                if (t.name == childObjectNames[i])
                {
                    //  It exists; break out of child transforms
                    childObj = t.gameObject;
                    if (i == childObjectNames.Length - 1)
                    {
                        return childObj; // leaf object already exists, nothing to create. 
                    }
                    break;
                }
            }

            if (childObj == null)
            {
                //  Doesn't exist, create it
                childObj = new GameObject(childObjectNames[i]);
                childObj.transform.parent = obj.transform;
            }
            obj = childObj;
        }
        return obj;
    }

    public Mode PrevActiveMode
    {
        get
        {
            return prevActiveMode;
        }
    }

    public Mode ActiveMode
    {
        get
        {
            return activeMode;
        }

        set
        {
            Trace.Log("SceneObject.ActiveMode.set({0}), gameobject '{1}'", value, s_modes[value].sceneObjectName);

            SceneObject SceneObject = Get();
            if (SceneObject.activeMode != value)
            {
                ModeInstance instance = s_modes[value];
                Trace.Assert(
                    instance.gameObject != null,
                    "SceneObject.Initialize() must be invoked before switching modes");

                if (SceneObject.activeMode != Mode.INVALID)
                {
                    ModeInstance prevInstance = s_modes[SceneObject.activeMode];
                    prevInstance.SetActive(false, DisplayStyle.None);
                }

                SceneObject.prevActiveMode = SceneObject.activeMode;

                //  Sky and ambient light settings
                RenderSettings.ambientMode = instance.useSkybox ?
                    UnityEngine.Rendering.AmbientMode.Skybox :
                    UnityEngine.Rendering.AmbientMode.Flat;
                if (!instance.useSkybox)
                {
                    RenderSettings.ambientLight = new Color(0, 0, 0);
                }

                EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem>(true /* includInactive */);
                if (instance.useEventSystem)
                {
                    eventSystem.gameObject.SetActive(true);
                }
                else
                {
                    // Incase of Creator and Welcome Mode the Event System is colliding with Default UI Toolkit Event System so it was causing an issue
                    eventSystem.gameObject.SetActive(false);
                }

                SceneObject.activeMode = value;

                //  Activate the object
                instance.gameObject.SetActive(true);
                instance.SetActive(true, DisplayStyle.Flex);

                instance.SceneMode.enabled = true;

                //  Input settings
                ActivateInteractionMode(value);
            }
        }
    }

    public static GameObject ActivePlayer
    {
        get
        {
            Mode m = Get().ActiveMode;
            if (m != Mode.INVALID)
            {
                ModeInstance instance = s_modes[m];
                Trace.Assert(instance.gameObject != null,
                    "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

                return instance.playerGameObject;
            }
            return null;
        }
    }

    public static GameObject GetPlayer(Mode mode)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        Trace.Assert(instance.gameObject != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

        return instance.playerGameObject;
    }

    public static GameObject ActiveCamera
    {
        get
        {
            Mode m = Get().ActiveMode;
            if (m != Mode.INVALID)
            {
                ModeInstance instance = s_modes[m];
                Trace.Assert(instance.gameObject != null,
                    "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

                return instance.cameraGameObject;
            }
            return null;
        }
    }

    public static GameObject GetCamera(Mode mode)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        Trace.Assert(instance.gameObject != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

        return instance.cameraGameObject;
    }

    public static PlayerController GetPlayerController(Mode mode)
    {
        Get();
        ModeInstance instance = s_modes[mode];
        Trace.Assert(instance.gameObject != null,
            "Initialize() must be invoked and all ModeInstance.Gameobjects be assigned before calling Find().");

        return instance.playerController;
    }

    //-------------------------------------//
    //  Private implementation

    private void Initialize()
    {
        SceneMode[] sceneModes = GameObject.FindObjectsOfType<SceneMode>(true /* includInactive */);

        Trace.Assert(
            sceneModes.Length == (int)Mode.COUNT,
            "Count of SceneMode objects must equal SceneObject.Mode.COUNT.");

        int matches = 0; ;

        foreach (Mode m in s_modes.Keys)
        {
            ModeInstance instance = s_modes[m];

            foreach (SceneMode sceneMode in sceneModes)
            {
                //  Initialize ModeInstance members
                if (sceneMode.gameObject.name == instance.sceneObjectName)
                {
                    instance.gameObject = sceneMode.gameObject;
                    sceneMode.Mode = m;

                    ScenePlayer scenePlayer = instance.gameObject.GetComponentInChildren<ScenePlayer>(true /* includeInactive */);
                    Trace.Assert(scenePlayer != null,
                        "SceneMode GameObject {0} does not have a ScenePlayer component among its child objects. Fix this in Inspector.",
                        instance.sceneObjectName);
                    instance.playerGameObject = scenePlayer.gameObject;
                    instance.playerController = instance.playerGameObject.GetComponent<PlayerController>();

                    if (instance.playerController == null &&
                        sceneMode.RequiresPlayerController())
                    {
                        Trace.Assert(instance.playerController != null,
                            "InteractionMode for scene mode object '{0}' requires a PlayerController component. Fix this in Inspector.",
                            instance.gameObject.name);
                    }

                    instance.uIDocument = instance.gameObject.GetComponentInChildren<UIDocument>(true /* includeInactive */);

                    Camera camera = instance.playerGameObject.GetComponentInChildren<Camera>(true /* includeInactive */);
                    Trace.Assert(camera != null,
                        "ScenePlayer GameObject {0} does not have a camera component among its child objects. Fix this in Inspector.",
                        instance.playerGameObject.name);
                    instance.cameraGameObject = camera.gameObject;

                    try
                    {
                        instance.gameObject.SetActive(false);
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }
                    matches++;
                }
            }
        }
        Trace.Assert(
            matches == (int)Mode.COUNT,
            "Count of SceneMode objects in the scene must equal SceneObject.Mode.COUNT. Fix the SceneModes in the scene or the definition of SceneObject.Mode +/- s_modes[].");
    }

    //  Private impl
    private static bool IsActive(Mode mode)
    {
        return (mode == Get().activeMode);
    }

    private void ActivateInteractionMode(
        Mode mode)
    {
        ModeInstance instance = s_modes[mode];
        if (instance.playerController != null)
        {
            instance.playerController.ActivateInteractionMode();
        }
        else
        {
            //  default is pointing mode
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
    }
    
    private void SetInteractionMode(
        Mode mode,
        PlayerController.IAMode interactionMode)
    {
        ModeInstance instance = s_modes[mode];
        instance.InteractionMode = interactionMode;
        if (instance.playerController != null)
        {
            instance.playerController.SetInteractionMode(interactionMode);
        }
    }

    //  Nested (child) class representing mode instance properties and settings
    private class ModeInstance
    {
        public PlayerController.IAMode InteractionMode
        {
            get { return SceneMode.InteractionMode; }
            set { SceneMode.InteractionMode = value; }
        }

        public SceneMode SceneMode
        {
            get { return gameObject.GetComponent<SceneMode>(); }
        }

        private ModeInstance()
        {
            Trace.Assert(false, "ModeInstance default constructor is disabled. Don't call this.");
        }

        public ModeInstance(
            string sceneObjectName,
            bool useSkybox,
            bool useEventSystem)
        {
            this.sceneObjectName = sceneObjectName;
            this.useSkybox = useSkybox;
            this.useEventSystem = useEventSystem;
        }

        public string sceneObjectName = "";
        public GameObject gameObject = null;
        public GameObject playerGameObject = null;
        public GameObject cameraGameObject = null;
        public PlayerController playerController = null;
        public UIDocument uIDocument = null;
        public bool useSkybox = false;
        public bool useEventSystem = false;
        public void SetActive(bool active, DisplayStyle display)
        {
            if (uIDocument != null)
            {
                foreach (Transform t in gameObject.transform)
                {
                    if (t.name == uIDocument.gameObject.name)
                    {
                        if (uIDocument.rootVisualElement != null)
                        {
                            uIDocument.rootVisualElement.style.display = display;
                        }
                    }
                    else
                    {
                        t.gameObject.SetActive(active);
                    }
                }
            }
            else
            {
                gameObject.SetActive(active);
            }
        }
    }
}
