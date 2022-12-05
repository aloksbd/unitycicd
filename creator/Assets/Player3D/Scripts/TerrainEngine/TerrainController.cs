using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Drawing;
using MEC;
using TerrainEngine;

namespace TerrainEngine
{
    #region RuntimeAsync Attribute
    class RuntimeAsyncAttribute : System.Attribute
    {
        private string name;
        public double version;
        public RuntimeAsyncAttribute(string name)
        {
            this.name = name;
            version = 1.0;
        }
    }
    #endregion RuntimeAsync Attribute

    public class TerrainController : MonoBehaviour
    {
        #region Singleton Accessor
        //-------------------------------------------------------------------------------
        //  Singleton accessor
        public static TerrainController Get()
        {
            return SceneObject.Find(
                SceneObject.Mode.Player,
                ObjectName.TERRAIN_GENERATOR).GetComponent<TerrainController>();
        }
        #endregion Singleton Accessor

        #region Error Reporting and Eventing
        //-------------------------------------------------------------------------------
        //  Error reporting and eventing
        public enum FatalError
        {
            ServerError,
            ProcessingError
        };
        public static readonly Dictionary<FatalError, string> s_fatalErrorMsgs = new Dictionary<FatalError, string>()
        {
            { FatalError.ServerError,     "{0}" }, // web service errors are supplied in full via s_webExceptionMsgs (see below).
            { FatalError.ProcessingError, "Terrain processing error {0}. Please try try again." },
        };

        public static readonly Dictionary<WebExceptionStatus, string> s_webExceptionMsgs = new Dictionary<WebExceptionStatus, string>()
        {
            { WebExceptionStatus.ProtocolError, "Terrain web service experienced an internal error. Please try again." },
            { WebExceptionStatus.Timeout,       "Terrain web request took too long. Please check your internet connection and try again." },
        };

        //  Use this helper to report a fatal web service error
        public void ReportFatalWebServiceError(WebExceptionStatus status)
        {
            if (s_webExceptionMsgs.ContainsKey(status))
            {
                ReportFatalError(FatalError.ServerError, s_webExceptionMsgs[status]);
            }
        }

        //  Use this helper to report a fatal error of any type
        public void ReportFatalError(FatalError newError, string errorDetail)
        {
            if (s_fatalErrorReports == null)
            {
                s_fatalErrorReports = new List<FatalErrorReport>();
            }

            FatalErrorReport report = new FatalErrorReport(newError, errorDetail);
            s_fatalErrorReports.Add(report);

            if (OnTerrainFatalErrorReport != null)
            {
                QueueOnMainThread(() =>
                {
                    OnTerrainFatalErrorReport(newError, report.errorMsg, ref s_fatalErrorReports);
                });
            }

            Abortable.Abort();
        }

        public class FatalErrorReport
        {
            public FatalError error;
            public string errorMsg;

            private FatalErrorReport()
            {
                //  Don't use this constructor.
            }

            public FatalErrorReport(FatalError error, string errorDetail)
            {
                this.error = error;
                this.errorMsg = String.Format(s_fatalErrorMsgs[error], errorDetail);
            }
        }

        private List<FatalErrorReport> s_fatalErrorReports;

        public delegate void TerrainFatalErrorReport(FatalError newError, string newErrorMsg, ref List<FatalErrorReport> allErrorMsgs);
        public static event TerrainFatalErrorReport OnTerrainFatalErrorReport;
        #endregion

        #region State Machine Reporting and Eventing
        //-------------------------------------------------------------------------------
        //  Terrain Engine asynchronous state machine 
        public enum TerrainState
        {
            //  Exclusive states (only one can exist)
            Unloaded  = 0x00000001,
            Unloading = 0x00000002,
            Loading   = 0x00000004,
            Running   = 0x00000008,

            //  Additive states
            SceneInitialized     = 0x00000100,
            TexturesGenerated    = 0x00000200,
            FarTerrainsGenerated = 0x00000400,
            TerrainsGenerated    = 0x00000800,
            WorldIsGenerated     = 0x00001000,
            VersionLoaded        = 0x00004000,
            BuildingDataReceived = 0x00008000,
            BuildingsGenerated   = 0x00010000,
            UserAborted          = 0x10000000,
        };

        const UInt32 EXCLUSIVE_TERRAINSTATE_MASK = 0x000000FF;

        public static readonly Dictionary<TerrainState, string> s_stateMsgs = new Dictionary<TerrainState, string>()
        {
            { TerrainState.Unloaded,             "No terrain loaded." },
            { TerrainState.Loading,              "Terrain is loading." },
            { TerrainState.SceneInitialized,     "Retrieving real world data." },
            { TerrainState.TexturesGenerated,    "Detail textures have been generated." },
            { TerrainState.FarTerrainsGenerated, "Distant terrain has been generated." },
            { TerrainState.TerrainsGenerated,    "Terrain is ready." },
            { TerrainState.WorldIsGenerated,     "World is ready." },
            { TerrainState.Running,              "Terrain is running" },
            { TerrainState.Unloading,            "Terrain is unloading" },
            { TerrainState.VersionLoaded,        "Version is loaded"},
            { TerrainState.BuildingDataReceived, "Building data received"},
            { TerrainState.BuildingsGenerated,   "Building generated"},
            { TerrainState.UserAborted,          "Cancelling terrain load"},
        };
        private TerrainState _state = TerrainState.Unloaded;

        //  Use this function to update the terrain engine's state machine
        public void UpdateState(TerrainState state, string context = "")
        {
            Trace.Assert(s_stateMsgs.ContainsKey(state),
                "Invalid state value {0}", state);

            //  Preserve all non-exclusive states but allow only one exclusive state
            if ((EXCLUSIVE_TERRAINSTATE_MASK & (UInt32)state) != 0)
            {
                _state = (TerrainState)(~EXCLUSIVE_TERRAINSTATE_MASK & (UInt32)_state) | state;
            }
            else
            {
                _state |= state;
            }

            if (state == TerrainState.UserAborted)
            {
                Abortable.Abort();
            }

            Trace.Log(traceState, "TERRAIN State update: + " + s_stateMsgs[state] + " (0x" + state.ToString("X") + ") => (0x" + _state.ToString("X") + ") " +
                ((context == "") ? "" : (" (" + context) + ")"));

            if (OnTerrainStateChanged != null)
            {
                QueueOnMainThread(() =>
                {
                    OnTerrainStateChanged(state, s_stateMsgs[state], _state);
                });
            }
        }

        public void ResetState(TerrainState state = TerrainState.Unloaded, string context = "")
        {
            Trace.Assert(((UInt32)state & EXCLUSIVE_TERRAINSTATE_MASK) != 0,
                "Non-exclusive state {0} is not allowed for reset", state);

            _state = 0;
            UpdateState(state);
        }

        //  Check the current state of the terrain engine
        public bool IsInState(TerrainState state)
        {
            return (_state & state) != 0;
        }

        //  Event implementation
        public delegate void TerrainStateChanged(TerrainState newState, string newStateMessage, TerrainState fullState);
        public static event TerrainStateChanged OnTerrainStateChanged;

        //  Pause terrain engine Update()'s
        private bool isPaused = false;

        public bool Paused
        {
            get { return isPaused; }
            set { isPaused = value; }
        }
        #endregion

        #region Member Data
        //-------------------------------------------------------------------------------
        //  Member Data

        //  Terrain generator components
        //
        private BuildingGenerator buildingGenerator;

        // Main Settings (set in Inspector)
        public TerrainSettings.gridSizeOption terrainGridSize = TerrainSettings.gridSizeOption._8x8;
        public string latitudeUser = ""; // 27.98582
        public string longitudeUser = ""; // 86.9236
        public float areaSize = 25f;
        public int heightmapResolution = 1080;
        public int imageResolution = 1080;
        public float elevationExaggeration = 1.25f;
        public int smoothIterations = 1;
        public bool farTerrain = true;
        public int farTerrainHeightmapResolution = 512;
        public int farTerrainImageResolution = 1080;
        public float areaSizeFarMultiplier = 4f;

        // Performance Settings
        public float heightmapPixelError = 10f;
        public float farTerrainQuality = 10f;
        public int cellSize = 64;
        public int concurrentTasks = 4;
        public float elevationDelay = 0.5f;
        public float imageryDelay = 0.5f;

        // Advanced Settings
        public bool elevationOnly = false;
        public bool fastStartBuild = true;
        public bool showTileOnFinish = true;
        public bool progressiveTexturing = true;
        public bool spiralGeneration = true;
        public bool delayedLOD = false;

        [HideInInspector] public bool progressiveGeneration = false;
        [HideInInspector] public float terrainDistance;
        [HideInInspector] public float terrainCurvator;
        [HideInInspector] public int farTerrainCellSize;
        public float farTerrainBelowHeight = 100f;

        public bool stitchTerrainTiles = true;
        [Range(5, 100)] public int levelSmooth = 5;
        [Range(1, 7)] public int power = 1;
        public bool trend = false;
        public int stitchDistance = 4;
        public float stitchDelay = 0.25f;

        public static TerrainSettings Settings
        {
            get { return _settingExternal; }

            set
            {
                TerrainController._settingExternal = value;
                TerrainController._settingExternal.updated = true;
            }
        }
        public static TerrainSettings _settingExternal;

        [HideInInspector] public string dataBasePath = "C:\\Earth9_GIS"; //TODO: User Geo-Server
        #endregion

        #region Diagnostics
        //-------------------------------------------------------------------------------
        //  Diagnostic trace configurations
#if UNITY_EDITOR
        [HideInInspector] public static Trace.Config traceDebug = null; // new Trace.Config(true, true);
        [HideInInspector] public static Trace.Config traceState = null; // new Trace.Config(true, true);
        [HideInInspector] public static Trace.Config tileDiagnostics = null; // new Trace.Config(true, true);
#else
        public static Trace.Config traceDebug = null;
        public static Trace.Config traceState = null;
        public static Trace.Config tileDiagnostics = null;
#endif

        #endregion

        #region Multithreading State
        //-------------------------------------------------------------------------------
        //  Multithreading State Management

        private const int MAX_THREADS = 50;
        private int s_threadCount;
        private Thread s_mainThread;

        public bool OnMainThread()
        {
            return (System.Threading.Thread.CurrentThread == s_mainThread);
        }

        public int ThreadsStillRunning()
        {
            return s_threadCount;
        }

        private List<Action> _actions = new List<Action>();
        private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

        private List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();
        private List<Action> _currentActions = new List<Action>();

        public struct DelayedQueueItem
        {
            public float time;
            public Action action;
        }

        #endregion

        #region General Implementation

        bool InternalStartTerrain()
        {
            if (_state == TerrainState.Unloaded)
            {
                UpdateState(TerrainState.Loading);

#if UNITY_EDITOR
                UnityEditor.PlayerSettings.runInBackground = true;
#endif
                terrainDistance = (areaSize * 1000f) / 3f; //2f
                farTerrainCellSize = cellSize;

                terrainCurvator = 0.00001f;

                int tileResolution = (heightmapResolution / (int)terrainGridSize);

                if (cellSize > tileResolution)
                {
                    cellSize = tileResolution;
                }

                if (farTerrainCellSize > farTerrainHeightmapResolution)
                {
                    farTerrainCellSize = farTerrainHeightmapResolution;
                }

                //#if UNITY_EDITOR
                //ConnectionsManager.SetAsyncConnections();
                //#else
                ConnectionsManagerRuntime.SetAsyncConnections();
                //#endif

                //mapserviceTerrain = new WorldElevation.Terrain_ImageServer();
                TerrainRuntime.Initialize(gameObject);

                progressiveGeneration = false;
                return true;
            }
            return false;
        }

        private void Awake()
        {
            s_mainThread = System.Threading.Thread.CurrentThread;

            buildingGenerator = GetComponent<BuildingGenerator>();
            Trace.Assert(buildingGenerator != null, "Building generator component not found on Terrain Controller gameobject {0}", gameObject.name);
        }

        void Update()
        {
            if (isPaused)
            {
                return;
            }

            //  Handle state transitions
            switch (_state)
            {
                case TerrainState.Unloaded:
                    if (UpdateSettings())
                    {
                        InternalStartTerrain();
                    }
                    break;
            }

            //  Execute pending _actions (as _currentActions)
            lock (_actions)
            {
                _currentActions.Clear();
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
            foreach (var action in _currentActions)
            {
                action();
            }

            //  Execute pending _delayed (as _currentDelayed)
            lock (_delayed)
            {
                _currentDelayed.Clear();
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));

                // clear expired tasks
                foreach (var action in _currentDelayed)
                {
                    _delayed.Remove(action);
                }
            }
            foreach (var action in _currentDelayed)
            {
                action.action();
            }
        }

        public bool UpdateSettings()
        {
            if (_settingExternal != null && _settingExternal.updated)
            {
                // Read Main Settings (valeus assigned by MainMenu.cs)
                terrainGridSize = _settingExternal.terrainGridSize;
                latitudeUser = _settingExternal.latitudeUser;
                longitudeUser = _settingExternal.longitudeUser;
                areaSize = _settingExternal.areaSize;
                heightmapResolution = _settingExternal.heightmapResolution;
                imageResolution = _settingExternal.imageResolution;
                elevationExaggeration = _settingExternal.elevationExaggeration;
                smoothIterations = _settingExternal.smoothIterations;
                farTerrain = _settingExternal.farTerrain;
                farTerrainHeightmapResolution = _settingExternal.farTerrainHeightmapResolution;
                farTerrainImageResolution = _settingExternal.farTerrainImageResolution;
                areaSizeFarMultiplier = _settingExternal.areaSizeFarMultiplier;

                // Read Performance Settings
                heightmapPixelError = _settingExternal.heightmapPixelError;
                farTerrainQuality = _settingExternal.farTerrainQuality;
                cellSize = _settingExternal.cellSize;
                concurrentTasks = _settingExternal.concurrentTasks;
                elevationDelay = _settingExternal.elevationDelay;
                imageryDelay = _settingExternal.imageryDelay;

                // Read Advanced Settings
                elevationOnly = _settingExternal.elevationOnly;
                fastStartBuild = _settingExternal.fastStartBuild;
                showTileOnFinish = _settingExternal.showTileOnFinish;
                progressiveTexturing = _settingExternal.progressiveTexturing;
                spiralGeneration = _settingExternal.spiralGeneration;
                delayedLOD = _settingExternal.delayedLOD;
                farTerrainBelowHeight = _settingExternal.farTerrainBelowHeight;
                stitchTerrainTiles = _settingExternal.stitchTerrainTiles;
                levelSmooth = _settingExternal.levelSmooth;
                power = _settingExternal.power;
                stitchDistance = _settingExternal.stitchDistance;
                stitchDelay = _settingExternal.stitchDelay;

                //  Reset following read
                _settingExternal.updated = false;
                return true;
            }
            return false;
        }

        public void LoadTerrainHeights()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.LoadTerrainHeights");

                Trace.Log(TerrainController.traceDebug, "TERRAIN LoadTerrainHeights() RunAsync: SmoothAllHeights()");

                TerrainRuntime.SmoothAllHeights();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN LoadTerrainHeights() RunAsync: SmoothAllHeights() COMPLETE -> Queuing LoadTerrainHeightsFromTIFFDynamic(0) on main thread.");

                QueueOnMainThread(() =>
                {
                    RunCoroutine(TerrainRuntime.LoadTerrainHeightsFromTIFFDynamic(0),
                        "LoadTerrainHeights()");
                });
            });
        }

        public void GetTerrainHeightsFAR()
        {
            TerrainRuntime.s_terrain.terrainData.heightmapResolution = farTerrainHeightmapResolution + 1;

            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetTerrainHeightsFAR");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetTerrainHeightsFAR() RunAsync: SmoothFarTerrain()");

                TerrainRuntime.SmoothFarTerrain();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetTerrainHeightsFAR() RunAsync: SmoothFarTerrain() complete -> Running GetTerrainHeightsFromTIFFFAR() async");

                TerrainRuntime.GetTerrainHeightsFromTIFFFAR();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetTerrainHeightsFAR() RunAsync: GetTerrainHeightsFromTIFFFAR() complete -> Queuing ApplyTerrainHeightsFromTIFFFAR() on main thread.");

                QueueOnMainThread(() =>
                {
                    RunCoroutine(TerrainRuntime.ApplyTerrainHeightsFromTIFFFAR(),
                        "LoadTerrainHeightsFromTIFFFAR()");
                });
            });
        }

        public void LoadTerrainHeightsNORTH(int i)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.LoadTerrainHeightsNORTH");

                if (i == (int)terrainGridSize)
                {
                    Trace.Log(TerrainController.traceDebug, "TERRAIN LoadTerrainHeightsNORTH(i: {0}) RunAsync: SmoothNORTH()", i);

                    TerrainRuntime.SmoothNORTH(i);

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN LoadTerrainHeightsNORTH(i: {0}) RunAsync: SmoothNORTH() COMPLETE -> Queuing HeightsFromNORTH() to main thread.", i);
                }

                QueueOnMainThread(() =>
                {
                    if (i == (int)terrainGridSize)
                    {
                        RunCoroutine(HeightsFromNORTH(),
                            "LoadTerrainHeightsNORTH(i: {0})", i);
                    }
                });
            });
        }

        public void LoadTerrainHeightsSOUTH(int i)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.LoadTerrainHeightsSOUTH");

                if (i == (int)terrainGridSize)
                {
                    Trace.Log(TerrainController.traceDebug, "TERRAIN LoadTerrainHeightsSOUTH(i: {0}) RunAsync: SmoothSOUTH()", i);

                    TerrainRuntime.SmoothSOUTH();

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN LoadTerrainHeightsSOUTH(i: {0}) RunAsync: SmoothSOUTH() COMPLETE -> Queuing HeightsFromSOUTH() to main thread.", i);
                }

                QueueOnMainThread(() =>
                {
                    if (i == (int)terrainGridSize)
                    {
                        RunCoroutine(HeightsFromSOUTH(),
                            "LoadTerrainHeightsSOUTH(i: {0})", i);
                    }
                });
            });
        }

        public void LoadTerrainHeightsEAST(int i)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.LoadTerrainHeightsEAST");

                if (i == (int)terrainGridSize)
                {
                    Trace.Log(TerrainController.traceDebug, "TERRAIN LoadTerrainHeightsEAST(i: {0}) RunAsync: SmoothEAST()", i);

                    TerrainRuntime.SmoothEAST();

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN LoadTerrainHeightsEAST(i: {0}) RunAsync: SmoothEAST() COMPLETE -> Queuing HeightsFromEAST() to main thread.", i);
                }

                QueueOnMainThread(() =>
                {
                    if (i == (int)terrainGridSize)
                    {
                        RunCoroutine(HeightsFromEAST(),
                            "LoadTerrainHeightsEAST(i: {0})",
                            i);
                    }
                });
            });
        }

        public void LoadTerrainHeightsWEST(int i)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.LoadTerrainHeightsWEST");

                if (i == (int)terrainGridSize)
                {
                    Trace.Log(TerrainController.traceDebug, "TERRAIN LoadTerrainHeightsWEST(i: {0}) RunAsync: SmoothWEST()", i);

                    TerrainRuntime.SmoothWEST();

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN LoadTerrainHeightsWEST(i: {0}) RunAsync: SmoothWEST(}) COMPLETE -> Queuing HeightsFromWEST() to main thread.", i);
                }

                QueueOnMainThread(() =>
                {
                    if (i == (int)terrainGridSize)
                    {
                        RunCoroutine(HeightsFromWEST(),
                            "LoadTerrainHeightsWEST(i: {0})", i);
                    }
                });
            });
        }

        private IEnumerator<float> HeightsFromNORTH()
        {
            Abortable abortable = new Abortable("TerrainController.HeightsFromNORTH");

            Trace.Log(traceDebug, "HeightsFromNORTH()");

            for (int x = 0; x < (int)terrainGridSize; x++)
            {
                if (abortable.shouldAbort)
                {
                    yield break;
                }

                RunCoroutine(TerrainRuntime.LoadTerrainHeightsFromTIFFNORTH(TerrainInfiniteController.northIndex + x),
                    "HeightsFromNORTH()", TerrainInfiniteController.northIndex + x);

                float tileDelay = (elevationDelay * Mathf.Pow((TerrainRuntime.NearMetrics.heightMapResolution - 1) / cellSize, 2f)) + (elevationDelay * 2);
                yield return Timing.WaitForSeconds(tileDelay);
            }
        }

        private IEnumerator<float> HeightsFromSOUTH()
        {
            Abortable abortable = new Abortable("TerrainController.HeightsFromSOUTH");

            Trace.Log(traceDebug, "HeightsFromSOUTH()");

            for (int x = 0; x < (int)terrainGridSize; x++)
            {
                if (abortable.shouldAbort)
                {
                    yield break;
                }

                RunCoroutine(TerrainRuntime.LoadTerrainHeightsFromTIFFSOUTH(TerrainInfiniteController.southIndex + x),
                    "HeightsFromSOUTH()", TerrainInfiniteController.southIndex + x);

                float tileDelay = (elevationDelay * Mathf.Pow((TerrainRuntime.NearMetrics.heightMapResolution - 1) / cellSize, 2f)) + elevationDelay;
                yield return Timing.WaitForSeconds(tileDelay);
            }
        }

        private IEnumerator<float> HeightsFromEAST()
        {
            Abortable abortable = new Abortable("TerrainController.HeightsFromEAST");

            Trace.Log(traceDebug, "HeightsFromEAST()");

            for (int x = 0; x < (int)terrainGridSize; x++)
            {
                if (abortable.shouldAbort)
                {
                    yield break;
                }

                RunCoroutine(TerrainRuntime.LoadTerrainHeightsFromTIFFEAST(TerrainInfiniteController.eastIndex + (x * (int)terrainGridSize)),
                    "HeightsFromEAST()", TerrainInfiniteController.eastIndex + (x * (int)terrainGridSize));

                float tileDelay = (elevationDelay * Mathf.Pow((TerrainRuntime.NearMetrics.heightMapResolution - 1) / cellSize, 2f)) + elevationDelay;
                yield return Timing.WaitForSeconds(tileDelay);
            }
        }

        public IEnumerator<float> PrepareVersionData()
        {
            Abortable abortable = new Abortable("TerrainController.PrepareVersionData");

            if (abortable.shouldAbort)
            {
                yield break;
            }

            QueueOnMainThread(() =>
            {
                VersionDownloader.PrepareData();
            });
            yield return Timing.WaitForSeconds(3);
        }

        private IEnumerator<float> HeightsFromWEST()
        {
            Abortable abortable = new Abortable("TerrainController.HeightsFromWEST");

            Trace.Log(traceDebug, "HeightsFromWEST()");

            for (int x = 0; x < (int)terrainGridSize; x++)
            {
                if (abortable.shouldAbort)
                {
                    yield break;
                }

                RunCoroutine(TerrainRuntime.LoadTerrainHeightsFromTIFFWEST(TerrainInfiniteController.westIndex + (x * (int)terrainGridSize)),
                    "HeightsFromWEST()", TerrainInfiniteController.westIndex + (x * (int)terrainGridSize));

                float tileDelay = (elevationDelay * Mathf.Pow((TerrainRuntime.NearMetrics.heightMapResolution - 1) / cellSize, 2f)) + elevationDelay;
                yield return Timing.WaitForSeconds(tileDelay);
            }
        }

        public void GetHeightmaps()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmaps");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmaps() RunAsync: ServerInfoElevation()");

                TerrainRuntime.ServerInfoElevation();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmaps() RunAsync: ServerInfoElevation() COMPLETE.");
            });
        }

        public void GetBuildings()
        {
            Abortable abortable = new Abortable("TerrainController.GetBuildings");
            buildingGenerator.GenerateBuildings();
        }
 
        public void GetHeightmapFAR()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmapFAR");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmapFAR() RunAsync: ServerInfoElevationFAR()");

                TerrainRuntime.ServerInfoElevationFAR();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmapFAR() RunAsync: ServerInfoElevationFAR() COMPLETE.");
            });
        }

        public void GetHeightmapsNORTH(int index)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmapsNORTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmapsNORTH(i: {0}) RunAsync: ServerInfoElevationNORTH(i: {0})", index);

                TerrainRuntime.ServerInfoElevationNORTH(index);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmapsNORTH(i: {0}) RunAsync: ServerInfoElevationNORTH(i: {0}) COMPLETE.", index);
            });
        }

        public void GetHeightmapsSOUTH()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmapsSOUTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmapsSOUTH() RunAsync: ServerInfoElevationSOUTH()");

                TerrainRuntime.ServerInfoElevationSOUTH();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmapsSOUTH() RunAsync: ServerInfoElevationSOUTH() COMPLETE.");
            });
        }

        public void GetHeightmapsEAST()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmapsEAST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmapsEAST() RunAsync: ServerInfoElevationEAST()");

                TerrainRuntime.ServerInfoElevationEAST();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmapsEAST() RunAsync: ServerInfoElevationEAST() COMPLETE.");
            });
        }

        public void GetHeightmapsWEST()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetHeightmapsWEST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetHeightmapsWEST() RunAsync: ServerInfoElevationWEST()");

                TerrainRuntime.ServerInfoElevationWEST();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetHeightmapsWEST() RunAsync: ServerInfoElevationWEST() COMPLETE.");
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmap))]
        public void ServerConnectHeightmap(int i, string fileName)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmap");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmap(i: {0}) RunAsync: ElevationDownload(i: {0})", i);
                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(TerrainRuntime.ElevationDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmap(i: {0}) RunAsync: ElevationDownload(i: {0}) COMPLETE -> Queuing LoadHeights({0}) to main thread.", i);

                QueueOnMainThread(() =>
                {
                    TerrainRuntime.LoadHeights(i);
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmapFAR))]
        public void ServerConnectHeightmapFAR()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmapFAR");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmapFAR() RunAsync: ElevationDownloadFAR()");
                WebExceptionStatus status = WebRequestRetries.WebRequestMethodFar(TerrainRuntime.ElevationDownloadFAR);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmapFAR() RunAsync: ElevationDownloadFAR() COMPLETE -> Queuing LoadHeightsFAR() to main thread.");

                QueueOnMainThread(() =>
                {
                    TerrainRuntime.LoadHeightsFAR();
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmapNORTH))]
        public void ServerConnectHeightmapNORTH(int index, string fileName)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmapNORTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmapNORTH(i: {0}) RunAsync: ElevationDownloadNORTH(i: {0})", index);
                TerrainRuntime.ElevationDownloadNORTH(index, fileName);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmapNORTH(i: {0}) RunAsync: ElevationDownloadNORTH(i: {0}) COMPLETE -> Queuing LoadHeightsNORTH({0}) to main thread.", index);

                QueueOnMainThread(() =>
                {
                    //                //if(!TerrainInfiniteController.inProgressSouth)
                    //                TerrainRuntime.LoadHeightsNORTH(index);

                    if (!TerrainInfiniteController.inProgressSouth)
                    {
                        if (TerrainRuntime.s_northCounter == (int)terrainGridSize)
                        {
                            for (int x = 0; x < (int)terrainGridSize; x++)
                            {
                                TerrainRuntime.LoadHeightsNORTH(x + 1);
                            }
                        }
                    }
                    else
                    {
                        //TerrainRuntime.northCounter = 0;
                        //TerrainInfiniteController.northTerrains.Clear();
                    }

                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmapSOUTH))]
        public void ServerConnectHeightmapSOUTH(int i, string fileName)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmapSOUTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmapSOUTH(i: {0}) RunAsync: ElevationDownloadSOUTH(i: {0})", i);
                TerrainRuntime.ElevationDownloadSOUTH(i, fileName);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmapSOUTH(i: {0}) RunAsync: ElevationDownloadSOUTH(i: {0}) COMPLETE -> Queuing LoadHeightsSOUTH({0}) to main thread.", i);

                QueueOnMainThread(() =>
                {
                    if (!TerrainInfiniteController.inProgressNorth)
                    {
                        if (TerrainRuntime.s_southCounter == (int)terrainGridSize)
                        {
                            for (int x = 0; x < (int)terrainGridSize; x++)
                                TerrainRuntime.LoadHeightsSOUTH(x + 1);
                        }
                    }
                    else
                    {
                        //TerrainRuntime.southCounter = 0;
                        //TerrainInfiniteController.southTerrains.Clear();
                    }
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmapEAST))]
        public void ServerConnectHeightmapEAST(int i, string fileName)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmapEAST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmapEAST(i: {0}) RunAsync: ElevationDownloadEAST(i: {0})", i);
                TerrainRuntime.ElevationDownloadEAST(i, fileName);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmapEAST(i: {0}) RunAsync: ElevationDownloadEAST(i: {0}) COMPLETE -> Queuing LoadHeightsEAST({0}) to main thread.", i);

                QueueOnMainThread(() =>
                {
                    if (!TerrainInfiniteController.inProgressWest)
                    {
                        if (TerrainRuntime.s_eastCounter == (int)terrainGridSize)
                        {
                            for (int x = 0; x < (int)terrainGridSize; x++)
                                TerrainRuntime.LoadHeightsEAST(x + 1);
                        }
                    }
                    else
                    {
                        //TerrainRuntime.eastCounter = 0;
                        //TerrainInfiniteController.eastTerrains.Clear();
                    }
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectHeightmapWEST))]
        public void ServerConnectHeightmapWEST(int i, string fileName)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectHeightmapWEST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectHeightmapWEST(i: {0}) RunAsync: ElevationDownloadWEST(i: {0})", i);
                TerrainRuntime.ElevationDownloadWEST(i, fileName);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectHeightmapWEST(i: {0}) RunAsync: ElevationDownloadWEST(i: {0}) COMPLETE -> Queuing LoadHeightsWEST({0}) to main thread.", i);

                QueueOnMainThread(() =>
                {
                    if (!TerrainInfiniteController.inProgressEast)
                    {
                        if (TerrainRuntime.s_westCounter == (int)terrainGridSize)
                        {
                            for (int x = 0; x < (int)terrainGridSize; x++)
                                TerrainRuntime.LoadHeightsWEST(x + 1);
                        }
                    }
                    else
                    {
                        //TerrainRuntime.westCounter = 0;
                        //TerrainInfiniteController.westTerrains.Clear();
                    }
                });
            });
        }

        public void GenerateTerrainHeights()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GenerateTerrainHeights");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GenerateTerrainHeights() RunAsync: TiffData(fileName: {0})", TerrainRuntime.s_fileNameTerrainData);

                TerrainRuntime.TiffData(TerrainRuntime.s_fileNameTerrainData);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GenerateTerrainHeights() RunAsync: TiffData(fileName: {0}) compelte -> Queuing FinalizeTerrainHeights(width: {1}, length: {2}) to main thread.",
                    TerrainRuntime.s_fileNameTerrainData,
                    TerrainRuntime.s_heightMapTiff.width,
                    TerrainRuntime.s_heightMapTiff.length);

                QueueOnMainThread(() =>
                {
                    FinalizeTerrainHeights(TerrainRuntime.s_heightMapTiff.data,
                        TerrainRuntime.s_heightMapTiff.width,
                        TerrainRuntime.s_heightMapTiff.length);
                });
            });
        }

        public void FinalizeTerrainHeights(float[,] data, int width, int height)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.FinalizeTerrainHeights");

                Trace.Log(TerrainController.traceDebug, "TERRAIN FinalizeTerrainHeights(...) RunAsync: SmoothHeights(width: {0}, height: {1})",
                    width, height);

                TerrainRuntime.SmoothHeights(data, width, height);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN FinalizeTerrainHeights(...) RunAsync: SmoothHeights(width: {0}, height: {1}) COMPLETE -> Queuing FinalizeHeights() to main thread.",
                    width, height);

                QueueOnMainThread(() =>
                {
                    TerrainRuntime.FinalizeHeights();
                });
            });
        }

        public void TerrainFromRAW()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.TerrainFromRAW");

                Trace.Log(TerrainController.traceDebug, "TERRAIN TerrainFromRAW() RunAsync: RawData(filePath: {0})",
                   TerrainRuntime.s_geoDataPathElevation);

                TerrainRuntime.RawData(TerrainRuntime.s_geoDataPathElevation);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN TerrainFromRAW() RunAsync: RawData(filePath: {0}) COMPLETE -> Queuing FinalizeTerrainHeights(width: {1} height: {2}) to main thread.",
                   TerrainRuntime.s_geoDataPathElevation, TerrainRuntime.m_Width, TerrainRuntime.m_Height);

                QueueOnMainThread(() =>
                {
                    FinalizeTerrainHeights(TerrainRuntime.s_rawData, TerrainRuntime.m_Width, TerrainRuntime.m_Height);
                });
            });
        }

        public void TerrainFromTIFF()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.TerrainFromTIFF");

                Trace.Log(TerrainController.traceDebug, "TERRAIN TerrainFromTIFF() RunAsync: TiffData(filePath: {0})",
                   TerrainRuntime.s_geoDataPathElevation);

                TerrainRuntime.TiffData(TerrainRuntime.s_geoDataPathElevation);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN TerrainFromTIFF()  RunAsync: TiffData(filePath: {0}) COMPLETE -> Queuing FinalizeTerrainHeights(width: {1} height: {2}) to main thread.",
                   TerrainRuntime.s_geoDataPathElevation, TerrainRuntime.m_Width, TerrainRuntime.m_Height);

                QueueOnMainThread(() =>
                {
                    FinalizeTerrainHeights(TerrainRuntime.s_heightMapTiff.data,
                        TerrainRuntime.s_heightMapTiff.width, TerrainRuntime.s_heightMapTiff.length);
                });
            });
        }

        public void TerrainFromASCII()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.TerrainFromASCII");

                Trace.Log(TerrainController.traceDebug, "TERRAIN TerrainFromASCII() RunAsync: AsciiData(filePath: {0})",
                    TerrainRuntime.s_geoDataPathElevation);

                TerrainRuntime.AsciiData(TerrainRuntime.s_geoDataPathElevation);

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN TerrainFromASCII() RunAsync: AsciiData(filePath: {0}) COMPLETE -> Queuing FinalizeTerrainHeights(width: {1} height: {2}) to main thread.",
                    TerrainRuntime.s_geoDataPathElevation, TerrainRuntime.m_Width, TerrainRuntime.m_Height);

                QueueOnMainThread(() =>
                {
                    FinalizeTerrainHeights(TerrainRuntime.s_asciiData, TerrainRuntime.s_nCols, TerrainRuntime.s_nRows);
                });
            });
        }

        public void ApplyElevationData()
        {
            IEnumerable<string> names = Directory.GetFiles(TerrainRuntime.s_dataBasePathElevation, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".asc")
                    || s.EndsWith(".raw")
                    || s.EndsWith(".tif"));

            if (names.ToArray().Length == 0)
                UnityEngine.Debug.LogError("NO AVILABLE DATA - No elevation data is available in selected folder.");
            else
            {
                TerrainRuntime.s_geoDataPathElevation = names.ToArray()[0];

                if (TerrainRuntime.s_geoDataPathElevation.EndsWith(".asc") || TerrainRuntime.s_geoDataPathElevation.EndsWith(".raw") || TerrainRuntime.s_geoDataPathElevation.EndsWith(".tif"))
                {
                    String[] pathParts = TerrainRuntime.s_geoDataPathElevation.Split(char.Parse("."));
                    TerrainRuntime.s_geoDataExtensionElevation = pathParts[pathParts.Length - 1];

                    if (TerrainRuntime.s_geoDataExtensionElevation.Equals("raw"))
                    {
                        RunAsync(() =>
                        {
                            Abortable abortable = new Abortable("TerrainController.GetElevationFileInfo");

                            Trace.Log(TerrainController.traceDebug, "TERRAIN ApplyElevationData() RunAsync: GetElevationFileInfo()");

                            TerrainRuntime.GetElevationFileInfo();

                            Trace.Log(TerrainController.traceDebug, "--- TERRAIN ApplyElevationData() RunAsync: GetElevationFileInfo() COMPLETE -> Queueing ApplyOfflineTerrain() to main thread.");

                            QueueOnMainThread(() =>
                            {
                                TerrainRuntime.ApplyOfflineTerrain();
                            });
                        });
                    }
                }
                else
                    UnityEngine.Debug.LogError("UNKNOWN FORMAT - There are no valid ASCII, RAW or Tiff files in selected folder.");
            }
        }

        public void ApplyImageData()
        {
            TerrainRuntime.GetFolderInfo(TerrainRuntime.s_dataBasePathImagery);

            if (TerrainRuntime.s_totalImagesDataBase == 0)
            {
                TerrainRuntime.s_baseImagesOK = false;
                UnityEngine.Debug.LogError("There are no images in data base!");
            }
            else
                TerrainRuntime.s_baseImagesOK = true;

            if (TerrainRuntime.NearMetrics.cellCount > TerrainRuntime.s_totalImagesDataBase)
            {
                TerrainRuntime.s_baseImagesOK = false;
                UnityEngine.Debug.LogError("No sufficient images to texture terrains. Select a lower Grid Size for terrains");
            }
            else
                TerrainRuntime.s_baseImagesOK = true;

            if (TerrainRuntime.s_baseImagesOK)
            {
                Vector2Int imageDimensions = ImageUtils.GetJpegImageSize(TerrainRuntime.s_baseImageNames[0]);
                TerrainRuntime.s_baseImageWidth = imageDimensions.x;
                TerrainRuntime.s_baseImageHeight = imageDimensions.y;
                imageResolution = TerrainRuntime.s_baseImageWidth;

                for (int i = 0; i < TerrainRuntime.s_baseImageNames.Length; i++)
                {
                    TerrainRuntime.s_baseImageTextures.Add(new Texture2D(TerrainRuntime.s_baseImageWidth, TerrainRuntime.s_baseImageHeight, TextureFormat.RGB24, true, true));
                    TerrainRuntime.s_baseImageTextures[i].wrapMode = TextureWrapMode.Clamp;
                }

                RunAsync(() =>
                {
                    TerrainRuntime.s_baseImageBytes = new Dictionary<int, byte[]>();

                    Trace.Log(TerrainController.traceDebug, "TERRAIN ApplyImageData() RunAsync: DownloadImageData(forEach)");

                    for (int i = 0; i < TerrainRuntime.s_baseImageNames.Length; i++)
                    {
                        Trace.Log(TerrainController.traceDebug, "   Terrain ApplyImageData() RunAsync: DownloadImageData(fileName: {0})", TerrainRuntime.s_baseImageNames[i]);

                        TerrainRuntime.DownloadImageData(i, TerrainRuntime.s_baseImageNames[i]);
                    }

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN ApplyImageData() RunAsync: DownloadImageData(forEach) COMPLETE -> Queuing FillImages(length: {0}) to main thread",
                        TerrainRuntime.s_totalImagesDataBase);

                    QueueOnMainThread(() =>
                    {
                        RunCoroutine(TerrainRuntime.FillImages(TerrainRuntime.s_totalImagesDataBase),
                            "ApplyImageData()",
                            TerrainRuntime.s_totalImagesDataBase);
                    });
                });
            }
        }

        public void GetSatelliteImages()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImages");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImages() RunAsync: ServerInfoImagery()");
                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(TerrainRuntime.ServerInfoImagery, "");
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }
            });
        }

        public void GetSatelliteImagesFAR()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImagesFAR");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImagesFAR() RunAsync: ServerInfoImageryFAR()");
                WebExceptionStatus status = WebRequestRetries.WebRequestMethodFar(TerrainRuntime.ServerInfoImageryFAR);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }
            });
        }

        public void GetSatelliteImagesNORTH()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImagesNORTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImagesNORTH() RunAsync: ServerInfoImageryNORTH()");

                TerrainRuntime.ServerInfoImageryNORTH();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetSatelliteImagesNORTH() RunAsync: ServerInfoImageryNORTH() COMPLETE.");
            });
        }

        public void GetSatelliteImagesSOUTH()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImagesSOUTH");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImagesSOUTH() RunAsync: ServerInfoImagerySOUTH()");

                TerrainRuntime.ServerInfoImagerySOUTH();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetSatelliteImagesSOUTH() RunAsync: ServerInfoImagerySOUTH() COMPLETE.");
            });
        }

        public void GetSatelliteImagesEAST()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImagesEAST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImagesEAST() RunAsync: ServerInfoImageryEAST()");

                TerrainRuntime.ServerInfoImageryEAST();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetSatelliteImagesEAST() RunAsync: ServerInfoImageryEAST() COMPLETE.");
            });
        }

        public void GetSatelliteImagesWEST()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.GetSatelliteImagesWEST");

                Trace.Log(TerrainController.traceDebug, "TERRAIN GetSatelliteImagesWEST() RunAsync: ServerInfoImageryWEST()");

                TerrainRuntime.ServerInfoImageryWEST();

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN GetSatelliteImagesWEST() RunAsync: ServerInfoImageryWEST() COMPLETE.");
            });
        }

        [RuntimeAsync(nameof(ServerConnectImagery))]
        public void ServerConnectImagery(int i, string fileName)
        {

            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectImagery");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectImagery(i: {0}) RunAsync: ImageDownloader(i: {0})", i);
                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(TerrainRuntime.ImageDownloader, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }

                QueueOnMainThread(() =>
                {
                    if (TerrainRuntime.s_allBlack)
                    {
                        Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImagery(i: {0}) RunAsync: ImageDownloader(i: {0}) COMPLETE -> ERROR: no available imagery at zoom level", i);

                        UnityEngine.Debug.LogError("UNAVAILABLE IMAGERY - There is no available imagery at this zoom level. Decrease TERRAIN GRID SIZE/IMAGE RESOLUTION or increase AREA SIZE.");
                        TerrainRuntime.s_imageDownloadingStarted = false;
                        return;
                    }

                    if (progressiveTexturing)
                    {
                        Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImagery(i: {0}) RunAsync: ImageDownloader(i: {0}) COMPLETE -> Queueing FillImage(i: {0}) to main thread", i);

                        RunCoroutine(TerrainRuntime.FillImage(i), "FillImage(i: {0}) progressiveTexturing", "ServerConnectImagery(i: {0})", i);
                    }
                    else
                    {
                        if (TerrainRuntime.s_processedImageIndex == TerrainRuntime.NearMetrics.baseImagesTotal)
                        {
                            Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImagery(i: {0}) RunAsync: ImageDownloader(i: {0}) COMPLETE -> Queueing FillImages(count: {0}) to main thread",
                                TerrainRuntime.NearMetrics.baseImagesTotal);

                            RunCoroutine(TerrainRuntime.FillImages(TerrainRuntime.NearMetrics.baseImagesTotal), 
                                "ServerConnectImagery(i: {1})", TerrainRuntime.NearMetrics.baseImagesTotal, i);
                        }
                    }
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectImageryFAR))]
        public void ServerConnectImageryFAR()
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectImageryFAR");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectImageryFAR() RunAsync: ImageDownloaderFAR()");
                WebExceptionStatus status = WebRequestRetries.WebRequestMethodFar(TerrainRuntime.ImageDownloaderFAR);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }

                Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImageryFAR() RunAsync: ImageDownloaderFAR() COMPLETE -> Queuing FillImageFAR() on main thread.");

                QueueOnMainThread(() =>
                {
                    RunCoroutine(TerrainRuntime.FillImageFAR(),
                        "ServerConnectImageryFAR()");
                });
            });
        }

        [RuntimeAsync(nameof(ServerConnectImageryDirection))]
        public void ServerConnectImageryDirection(int i, string fileName, string direction)
        {
            RunAsync(() =>
            {
                Abortable abortable = new Abortable("TerrainController.ServerConnectImageryDirection");

                Trace.Log(TerrainController.traceDebug, "TERRAIN ServerConnectImagery" + direction + "(i: {0}) RunAsync: ImageDownloader" + direction + "(i: {0})", i);
                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(TerrainRuntime.ImageDownloaderDirection, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    ReportFatalWebServiceError(status);
                    return;
                }

                QueueOnMainThread(() =>
                {
                    if (TerrainRuntime.s_allBlack)
                    {
                        Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImagery" + direction + "(i: {0}) RunAsync: ImageDownloader" + direction + "(i: {0}) completed -> ERROR", i);

                        UnityEngine.Debug.LogError("UNAVAILABLE IMAGERY - There is no available imagery at this zoom level. Decrease TERRAIN GRID SIZE/IMAGE RESOLUTION or increase AREA SIZE.");
                    }

                    Trace.Log(TerrainController.traceDebug, "--- TERRAIN ServerConnectImagery" + direction + "(i: {0}) RunAsync: ImageDownloader" + direction + "(i: {0}) completed -> Queueing FillImage" + direction + "(i: {0}) on main thread.", i);

                    RunCoroutine(TerrainRuntime.FillImageDirection(i),
                        "ServerConnectImagery" + direction + "(i: {0})", i);
                });
            });
        }

        public void Unload()
        {
            UpdateState(TerrainController.TerrainState.Unloading);
            TerrainRuntime.UnloadAllAssets();
            buildingGenerator.Dispose();
            ResetState();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private void UnloadAllAssets()
        {
            try
            {
                Destroy(TerrainRuntime.s_terrain);
                Destroy(TerrainRuntime.s_firstTerrain);
                Destroy(TerrainRuntime.s_secondaryTerrain);
                Destroy(TerrainRuntime.s_currentTerrain);
                Destroy(TerrainRuntime.s_baseImageTextureFar);
                Destroy(TerrainRuntime.s_terrainData);

                TerrainRuntime.s_webClientTerrain = null;
                TerrainRuntime.s_heightMapTiff.data = null;
                TerrainRuntime.s_heightMapTiff.dataASCII = null;
                TerrainRuntime.s_heightMapFarTerrainImageDetail.data = null;
                TerrainRuntime.s_heightMapFarTerrainImageDetail.dataASCII = null;
                TerrainRuntime.s_finalHeights = null;
                TerrainRuntime.s_heightmapCell = null;
                TerrainRuntime.s_heightmapCellSec = null;
                TerrainRuntime.s_heightmapCellFar = null;
                TerrainRuntime.s_rawData = null;
                TerrainRuntime.s_webClientImage = null;
                TerrainRuntime.s_smData = null;
                TerrainRuntime.s_baseImageBytesFar = null;
                TerrainRuntime.s_asciiData = null;
                TerrainRuntime.s_terrainDict = null;
                TerrainRuntime.s_heights = null;
                TerrainRuntime.s_secondHeights = null;

                for (int i = 0; i < TerrainRuntime.s_croppedTerrains.Count; i++)
                    Destroy(TerrainRuntime.s_croppedTerrains[i]);

                for (int i = 0; i < TerrainRuntime.s_baseImageTextures.Count; i++)
                    Destroy(TerrainRuntime.s_baseImageTextures[i]);

                for (int i = 0; i < TerrainRuntime.s_baseImageBytes.Count; i++)
                    TerrainRuntime.s_baseImageBytes[i] = null;

                for (int i = 0; i < TerrainRuntime.s_tiffDataDynamic.Count; i++)
                    TerrainRuntime.s_tiffDataDynamic[i] = null;

                for (int i = 0; i < TerrainRuntime.s_terrains.Length; i++)
                    Destroy(TerrainRuntime.s_terrains[i]);

                if (TerrainRuntime.s_stitchingTerrainsList != null)
                {
                    for (int i = 0; i < TerrainRuntime.s_stitchingTerrainsList.Count; i++)
                        Destroy(TerrainRuntime.s_stitchingTerrainsList[i]);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        //  RunCoroutine() - Helper function for coroutine diagnostics
        public static CoroutineHandle RunCoroutine(
            IEnumerator<float> coroutine,
            string callerName = "",
            params object[] nameargs)
        {
            if (callerName != null && callerName != "")
            {
                Trace.Log(
                    TerrainController.traceDebug,
                    "TERRAIN " +
                    String.Format("Started coroutine: {0}()", coroutine.ToString()) + " from " +
                    String.Format(callerName, nameargs));
            }
            else
            {
                Trace.Log(
                    TerrainController.traceDebug,
                    "TERRAIN " + String.Format("Started coroutine: {0}()", coroutine.ToString()));
            }
            return Timing.RunCoroutine(coroutine);
        }

        #endregion General Implementation

        #region Multithreading Implementation

        public void QueueOnMainThread(Action action, float time = 0.0f)
        {
            if (OnMainThread())
            {
                action();
                return;
            }

            if (time != 0)
            {
                lock (_delayed)
                {
                    _delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
                }
            }
            else
            {
                lock (_actions)
                {
                    _actions.Add(action);
                }
            }
        }

        public Thread RunAsync(Action a)
        {
            while (s_threadCount >= MAX_THREADS)
            {
                Thread.Sleep(1);
            }

            Interlocked.Increment(ref s_threadCount);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        private void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
            finally
            {
                Interlocked.Decrement(ref s_threadCount);
            }
        }

        #endregion Multithreading Implementation

    }
}
