using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine;
using MEC;

namespace TerrainEngine
{
    public class BuildingGenerator : MonoBehaviour
    {
        private const int   LOADING_GRID_CELLS_X = 25;
        private const int   LOADING_GRID_CELLS_Y = LOADING_GRID_CELLS_X;
        private const float DEFAULT_BUILDING_HEIGHT = 14f;

        //  Configured in Inspector
        [Header("Assets and settings")][Space(5)]
        public TerrainPlayer player;
        [Header("Unbuilt (OSM) procedurally generated buildings")]
        public Material osmWallMaterial;    
        public Material osmRoofMaterial;
        public Material osmPylonMaterial;
        [Header("Work-in-Progress (WIP) procedurally generated buildings")]
        public Material wipWallMaterial;
        public Material wipRoofMaterial;
        public Material wipPylonMaterial;
        [Space(5)]
        public bool procedurallyGeneratePartialBuilds = false;
        public bool procedurallyGeneratePendingApprovals = false;
        public bool procedurallyGenerateLives = false;
        public bool buidingColliders;
        public float distanceFilter = 0;

        // Inspector-based performance monitoring
        [Header("Download Performance")][Space(5)]
        public int totalDownloadRequests;
        [Space(5)]
        public float latestDownloadTime;
        public float minDownloadTime;
        public float maxDownloadTime;
        public float avgDownloadTime;
        public float totalDownloadTime;
        [Space(5)]
        public int latestBuildingCount;
        public int minBuildingCount;
        public int maxBuildingCount;
        public float avgBuildingCount;
        public int totalBuildingCount;
        [Space(5)]
        public float latestTimePerBuilding;
        public float avgTimePerBuilding;

        [Header("Processing activity")][Space(5)]
        public int buildingsToGenerate = 0;
        public int buildingsGenerated = 0;

        //  Private members
        //
        private object s_datalock = new object();
        private TerrainController controller;
        private RangeGrid loadingGrid;

        //  Building status metadata
        enum BuildingStatus
        {
            NotBuilt,
            UnderConstruction,
            FloorsCompleted,
            PendingApproval,
            Live,
        };

        struct BuildingStatusInfo
        {
            public BuildingStatus status;
            public string description;

            public BuildingStatusInfo(BuildingStatus status, string description)
            {
                this.status = status;
                this.description = description;
            }
        };

        private static readonly Dictionary<string, BuildingStatusInfo> s_buildingStatus = new Dictionary<string, BuildingStatusInfo>()
        {
            { "NOT_BUILT",          new BuildingStatusInfo(BuildingStatus.NotBuilt,          "Available") },
            { "UNDER_CONSTRUCTION", new BuildingStatusInfo(BuildingStatus.UnderConstruction, "Under Construction") },
            { "FLOORS_COMPLETED",   new BuildingStatusInfo(BuildingStatus.FloorsCompleted,   "Partially Built") },
            { "PENDING_APPROVAL",   new BuildingStatusInfo(BuildingStatus.PendingApproval,   "Pending Approval") },
            { "LIVE",               new BuildingStatusInfo(BuildingStatus.Live,              "Live") },
        };

        private BuildingsInProgress  buildingsInProgress;    // buildings currently being processed

        //  Game-ready data from which a building is generated
        private class GameReadyBuilding
        {
            public string           buildingObjectId;
            public OsmBuildingData  buildingData;      // OSM building data
            public int              iGeometry;         // polygon index (for multi-polygon buildings)
            public Wgs84Bounds      bbox;              // building's bounding box
            public List<Vector3>    worldVertices;     // wall vertices in world coordinates
            public Vector3[]        localVertices;     // wall vertices in local coordinates (relative to its geometric centerpoint)
            public Vector3          localCenterPt;     
            public float            baseHeight;        
            public ProceduralBuilding.RoofType roofType;
            public float            roofHeight;
            public BuildingStatusInfo statusInfo;
        };
        Dictionary<string, GameReadyBuilding> gameReadyBuildingData;

        //  BuildingGenerator task queue. Worker threads enqueue, main thread dequeues.
        Queue<Dictionary<string, GameReadyBuilding>> taskQueue = new Queue<Dictionary<string, GameReadyBuilding>>();

        //  GameObject parenting and tracking
        GameObject buildingsParent;
        GameObject pylonsParent;
        Dictionary<string, GameObject> buildingGameObjects;

        private void Awake()
        {
            controller = GetComponent<TerrainController>();
            Trace.Assert(controller != null, "Terrain Controller component was not found on gameObject {0}", gameObject.name);

            TerrainController.OnTerrainStateChanged += OnTerrainStateChanged;
        }

        public bool TryGetBuildingByID(string id, int iGeometry, out GameObject gameObject)
        {
            string buildingObjectID = String.Format("{0}[{1}]", id, iGeometry);

            return buildingGameObjects.TryGetValue(buildingObjectID, out gameObject);
        }

        //--------------------------------------------------------------------------------//
        //  Service cient entrypoint (main thread)

        public void GenerateBuildings()
        {
            Abortable abortable = new Abortable("BuildingGenerator.GetBuildingCoordinates");

            Vector3 playerPostion = player.transform.position;
            int rangeId = 0;

            if (loadingGrid == null)
            {
                loadingGrid = new RangeGrid();
                loadingGrid.InitializeFromArea(
                    ref TerrainRuntime.NearMetrics.worldBounds,
                    ref TerrainRuntime.NearMetrics.wgs84Bounds,
                    LOADING_GRID_CELLS_X, LOADING_GRID_CELLS_Y);

                // For now, we only have one range to compute. In the future we may
                // discriminate among multiple distance ranges
                loadingGrid.SetRange(rangeId, 0 /* near */, distanceFilter /* far */);

                ResetDownloadMetrics();
            }

            List<RangeGrid.Cell> areasInRange;

            lock (s_datalock)
            {
                //  Retrieve the cells that match criteria specified by rangeId
                if (loadingGrid.GetCellsInRange(playerPostion, rangeId, out areasInRange) > 0 &&
                    !abortable.shouldAbort)
                {
                    //  Strip out the cells that we've already processed or are processing
                    areasInRange.RemoveAll(p =>
                    {
                        RangeGrid.Cell.Status cellStatus;
                        return loadingGrid.TryGetStatus(p.row, p.col, out cellStatus) ?
                            cellStatus != RangeGrid.Cell.Status.None :
                            false;
                    });

                    //  Mark surviving cells as 'ToDownload'
                    foreach (RangeGrid.Cell cell in areasInRange)
                    {
                        loadingGrid.TrySetStatus(cell.row, cell.col, RangeGrid.Cell.Status.ToDownload);
                    }
                }
                else
                {
                    return;
                }
            }

            controller.RunAsync(() =>
            {
                foreach (RangeGrid.Cell cell in areasInRange)
                {
                    if (abortable.shouldAbort)
                    {
                        return;
                    }

                    lock (s_datalock)
                    {
                        loadingGrid.TrySetStatus(cell.row, cell.col, RangeGrid.Cell.Status.Downloading);
                    }

                    AreaBuildingData osmBuildingData;
                    WebExceptionStatus status = DownloadBuildings(ref cell.wgs84Bounds, out osmBuildingData);

                    if (status != WebExceptionStatus.Success)
                    {
                        controller.ReportFatalWebServiceError(status);
                        return;
                    }

                    if (abortable.shouldAbort)
                    {
                        return;
                    }

                    lock (s_datalock)
                    {
                        loadingGrid.TrySetStatus(cell.row, cell.col, RangeGrid.Cell.Status.Processing);
                    }

                    AsyncPreProcessData(cell, osmBuildingData, playerPostion);

                    lock (s_datalock)
                    {
                        loadingGrid.TrySetStatus(cell.row, cell.col, RangeGrid.Cell.Status.Processed);
                    }
                }
            });

            if (abortable.shouldAbort)
            {
                return;
            }

            Trace.Log(TerrainController.traceDebug, "--- TERRAIN BuildingGenerator.GetBuildingCoordinates() RunAsync: GetBuildingByBoundryBox() COMPLETE.");
            controller.QueueOnMainThread(() =>
            {
                if (abortable.shouldAbort)
                {
                    return;
                }
                StartCoroutine(CreateBuildings());
            });
        }

        private void InitShared()
        {
            lock (s_datalock)
            {
                if (buildingGameObjects == null)
                {
                    buildingGameObjects = new Dictionary<string, GameObject>();
                }

                if (buildingsInProgress == null)
                {
                    buildingsInProgress = new BuildingsInProgress();
                }
            }
        }

        //--------------------------------------------------------------------------------//
        //  Worker thread Processing

        [RuntimeAsync(nameof(DownloadBuildings))]
        public WebExceptionStatus DownloadBuildings(ref Wgs84Bounds area, out AreaBuildingData osmData)
        {
            Abortable abortable = new Abortable("BuildingGenerator.DownloadBuildings");

            osmData = null;

            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            try
            {
                var buildingCoordinates = new BoundryBoxCoordinates()
                {
                    bbox = new double[] {
                        area.bottom,
                        area.left,
                        area.top,
                        area.right }
                };

                long downloadStart = DateTime.Now.Ticks;

                List<OsmBuildingData> buildingData = OsmBuildings.GetBuildingByBoundryBox(buildingCoordinates).Result;

                if (abortable.shouldAbort)
                {
                    return WebExceptionStatus.RequestCanceled;
                }

                try
                {
                    osmData = new AreaBuildingData()
                    {
                        buildingData = buildingData,
                        areaBounds = new Wgs84Bounds(TerrainRuntime.NearMetrics.wgs84Bounds)
                    };
                }
                catch (Exception e)
                {
                    if (abortable.shouldAbort)
                    {
                        osmData = null;
                        return WebExceptionStatus.RequestCanceled;
                    }
                    Trace.Exception(e);
                }

                UpdateDownloadMetrics((float)(DateTime.Now.Ticks - downloadStart) / (float)TimeSpan.TicksPerMillisecond / 1000f, buildingData.Count);

                controller.UpdateState(TerrainController.TerrainState.BuildingDataReceived);
            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e,
                    "Exception exporting GetBuildingCoordinates  DETAILS:");
                return WebExceptionStatus.UnknownError;
            }

            return WebExceptionStatus.Success;
        }

        [RuntimeAsync(nameof(UpdateDownloadMetrics))]
        private void UpdateDownloadMetrics(float seconds, int buildingCount)
        {
            Interlocked.Increment(ref totalDownloadRequests);
            latestDownloadTime = seconds;
            latestBuildingCount = buildingCount;
            latestTimePerBuilding = buildingCount > 0 ?
                seconds / buildingCount : 0;

            minDownloadTime = (maxDownloadTime == 0) ? latestDownloadTime : Mathf.Min(minDownloadTime, latestDownloadTime);
            maxDownloadTime = Mathf.Max(maxDownloadTime, latestDownloadTime);
            minBuildingCount = (maxBuildingCount == 0) ? latestBuildingCount : (int)Math.Min(minBuildingCount, latestBuildingCount); ;
            maxBuildingCount = (int)Math.Max(maxBuildingCount, latestBuildingCount); ;

            totalDownloadTime += seconds;
            totalBuildingCount += buildingCount;
            avgDownloadTime = totalDownloadTime / totalDownloadRequests;
            avgTimePerBuilding = totalDownloadTime / totalBuildingCount;
            avgBuildingCount = totalBuildingCount / totalDownloadRequests;
        }

        private void ResetDownloadMetrics()
        {
            latestDownloadTime = 0.0f;
            latestBuildingCount = 0;
            latestTimePerBuilding = 0.0f;
            maxDownloadTime = 0.0f;
            minDownloadTime = 0.0f;
            maxBuildingCount = 0;
            minBuildingCount = 0;
            totalDownloadRequests = 0;
            totalDownloadTime = 0.0f;
            totalBuildingCount = 0;
            avgDownloadTime = 0.0f;
            avgTimePerBuilding = 0.0f;
            avgBuildingCount = 0.0f;
        }

        [RuntimeAsync(nameof(AsyncPreProcessData))]
        private void AsyncPreProcessData(RangeGrid.Cell area, AreaBuildingData osmBuildingData, Vector3 playerPosition)
        {
            Abortable abortable = new Abortable("BuildingGenerator.AsyncPreProcessData");

            gameReadyBuildingData = new Dictionary<string, GameReadyBuilding>();
            InitShared();

            //  Enumerate building data records
            for (int iBuilding = 0;
                !abortable.shouldAbort && osmBuildingData != null && iBuilding < osmBuildingData.buildingData.Count;
                 iBuilding++)
            {
                //  Enumerate footprint geometries
                for (int iGeometry = 0;
                     iGeometry < osmBuildingData.buildingData[iBuilding].geometry.coordinates.Count;
                     iGeometry++ )
                {
                    if (abortable.shouldAbort)
                    {
                        return;
                    }

                    //  Curate the raw OSM building data
                    GameReadyBuilding building = FilterAndGeoLocateFootprint(
                        ref TerrainRuntime.s_nearGridMetrics,
                        ref TerrainRuntime.s_nearGridMetrics.wgs84Bounds, // TODO: moving window for buildings
                        playerPosition,
                        osmBuildingData.buildingData[iBuilding],
                        iGeometry);

                    if (abortable.shouldAbort)
                    {
                        return;
                    }

                    if (building != null)
                    {
                        gameReadyBuildingData.Add(building.buildingObjectId, building);
                    }
                }
            }

            if (gameReadyBuildingData != null && gameReadyBuildingData.Count > 0)
            {
                lock (s_datalock)
                {
                    taskQueue.Enqueue(gameReadyBuildingData);
                }
            }

            osmBuildingData = null;
        }

        [RuntimeAsync("BuildingGenerator.FilterAndGeoLocateFootprint)")]
        private GameReadyBuilding FilterAndGeoLocateFootprint(
            ref GridMetrics gridMetrics,
            ref Wgs84Bounds wgs84AreaBounds,
            Vector3 playerPosition,
            OsmBuildingData buildingData,
            int iGeometry)
        {
            Abortable abortable = new Abortable("BuildingGenerator.FilterAndGeoLocateFootprint");

            List<Vector3> worldVertices = null;
            Wgs84Bounds bbox = null;

            //  Generate game object ID
            string buildingObjectID = String.Format("{0}[{1}]", buildingData.id, iGeometry);

            //  Ignore if building is already created or in progress
            lock (s_datalock)
            {
                if (buildingGameObjects.ContainsKey(buildingObjectID))
                {
                    return null;
                }
                if (buildingsInProgress.Exists(buildingObjectID))
                {
                    return null;
                }

                //  Add to buildings-in-progress
                buildingsInProgress.Add(buildingObjectID);
            }

            //  Reject if we shouldn't procedurally generate this building
            BuildingStatusInfo statusInfo;
            if (!ShouldProcedurallyGeneratForStatus(buildingData.status, out statusInfo))
            {
                return null;
            }

            //  Reject if fewer than three vertices
            List<List<float>> buildingFootprint = buildingData.geometry.coordinates[iGeometry];
            if (buildingFootprint.Count < 3)
            {
                return null;
            }

            // Bounds-check and geolocate each vertex
            foreach (List<float> point in buildingFootprint)
            {
                if (abortable.shouldAbort)
                {
                    return null;
                }

                float lat = point[1];
                float lon = point[0];

                //  Generate building WGS84 bounding box
                if (bbox == null)
                {
                    bbox = new Wgs84Bounds();
                    bbox.left = bbox.bottom = float.MaxValue;
                    bbox.right = bbox.top = float.MinValue;
                }

                if (bbox.left > lon) { bbox.left = lon; }
                if (bbox.bottom > lat) { bbox.bottom = lat; }
                if (bbox.right < lon) { bbox.right = lon; }
                if (bbox.top < lat) { bbox.top = lat; }

                if (worldVertices == null)
                {
                    worldVertices = new List<Vector3>();
                }

                //  Geolocate the vertex on the terrain.
                worldVertices.Add(gridMetrics.GeoLocate(lat, lon));
            }

            if (worldVertices == null || abortable.shouldAbort)
            {
                return null;
            }

            //  Remove noisy vertices
            for (int i = 0; i < worldVertices.Count; i++)
            {
                if (abortable.shouldAbort)
                {
                    return null;
                }

                int prev = i - 1;
                if (prev < 0)
                {
                    prev = worldVertices.Count - 1;
                }

                int next = i + 1;
                if (next >= worldVertices.Count)
                {
                    next = 0;
                }

                //  reject if too close to prior vertex
                if ((worldVertices[prev] - worldVertices[i]).magnitude < 0.01f)
                {
                    worldVertices.RemoveAt(i);
                    i--;
                    continue;
                }

                //  reject if too close to next vertex
                if ((worldVertices[next] - worldVertices[i]).magnitude < 0.01f)
                {
                    worldVertices.RemoveAt(next);
                    continue;
                }

                //  reject if angle between previous and next vertex is too acute
                float a1 = Angle2D(worldVertices[prev], worldVertices[i]);
                float a2 = Angle2D(worldVertices[i], worldVertices[next]);

                if (Mathf.Abs(a1 - a2) < 5)
                {
                    worldVertices.RemoveAt(i);
                    i--;
                }
            }

            if (worldVertices.Count < 3 || abortable.shouldAbort)
            {
                return null;
            }

            if (worldVertices.First() == worldVertices.Last())
            {
                worldVertices.Remove(worldVertices[worldVertices.Count - 1]);
                if (worldVertices.Count < 3)
                {
                    return null;
                }
            }

            //  Normalize vertex sort order
            if (IsReversed(worldVertices))
            {
                worldVertices.Reverse();
            }

            float baseHeight = 0;
            try
            {
                baseHeight = (float)Convert.ToDouble(Regex.Replace(buildingData.details.height, "[^0-9.]", ""));
                if (baseHeight == 0.0f)
                {
                    baseHeight = DEFAULT_BUILDING_HEIGHT;
                }
            }
            catch (Exception)
            {
                Trace.Warning("Bad OSM Height value '{0}' for building id {1}", buildingData.details.height, buildingData.id);
                baseHeight = DEFAULT_BUILDING_HEIGHT;
            }

            Vector3 localCenterPt = Vector3.zero;
            localCenterPt = worldVertices.Aggregate(localCenterPt, (current, point) => current + point) / worldVertices.Count;
            localCenterPt.y = worldVertices.Min(p => p.y);

            return new GameReadyBuilding()
            {
                buildingObjectId = buildingObjectID,
                buildingData     = buildingData,
                iGeometry        = iGeometry,
                bbox             = bbox,
                baseHeight       = baseHeight,
                worldVertices    = worldVertices,
                localCenterPt    = localCenterPt,
                localVertices    = worldVertices.Select(p => p - localCenterPt).ToArray(),
                roofType         = ProceduralBuilding.RoofType.flat,
                roofHeight       = 0,
                statusInfo       = statusInfo
            };
        }

        [RuntimeAsync("ShouldProcedurallyGeneratForStatus")]
        private bool ShouldProcedurallyGeneratForStatus(string statusKey, out BuildingStatusInfo info)
        {
#if false
            //  Randomized status values for dev-testing
            string[] testvalues = new string[]
            {
                "NOT_BUILT",
                "UNDER_CONSTRUCTION",
                "FLOORS_COMPLETED",  
                "PENDING_APPROVAL",  
                "LIVE"
            };
            statusKey = testvalues[new System.Random().Next(0, testvalues.Length - 1)];
#endif 

            if (s_buildingStatus.TryGetValue(statusKey, out info))
            {
                switch (info.status)
                {
                    case BuildingStatus.NotBuilt:
                        return true;
                    case BuildingStatus.UnderConstruction:
                    case BuildingStatus.FloorsCompleted:
                        return procedurallyGeneratePartialBuilds;
                    case BuildingStatus.PendingApproval:
                        return procedurallyGeneratePendingApprovals;
                    case BuildingStatus.Live:
                        return procedurallyGenerateLives;
                }
            }
            Trace.Warning("BuildingGenerator: missing or invalid status value '{0]'", statusKey);
            return false;
        }

        public static float Angle2D(Vector3 point1, Vector3 point2)
        {
            return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }

        public static float Angle2D(Vector2 point1, Vector2 point2)
        {
            return Mathf.Atan2((point2.y - point1.y), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }

        private static bool IsReversed(List<Vector3> points)
        {
            float minZ = float.MaxValue;
            int i2 = -1;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                if (p.z < minZ)
                {
                    minZ = p.z;
                    i2 = i;
                }
            }

            int i1 = i2 - 1;
            int i3 = i2 + 1;
            
            if (i1 < 0)
            {
                i1 += points.Count;
            }
            
            if (i3 >= points.Count)
            {
                i3 -= points.Count;
            }

            Vector3 p1 = points[i1];
            Vector3 p2 = points[i2];
            Vector3 p3 = points[i3];

            Vector3 s1 = p2 - p1;
            Vector3 s2 = p3 - p1;

            Vector3 side1 = s1;
            Vector3 side2 = s2;

            return Vector3.Cross(side1, side2).y <= 0;
        }

        //--------------------------------------------------------------------------------//
        //  Main thread processing

        private IEnumerator CreateBuildings()
        {
            Dictionary<string, GameReadyBuilding> gameReadyBuildings;

            while (!Abortable.ShouldAbortRoutine())
            {
                bool haveTask;

                lock (s_datalock)
                {
                    haveTask = taskQueue.TryDequeue(out gameReadyBuildings);
                }

                if (!haveTask)
                {
                    yield return Timing.WaitForSeconds(.333f);
                    continue;
                }

                Trace.Assert(gameReadyBuildings.Count > 0, "There should be no empty building generation tasks in the queue");

                InitShared();

                if (buildingsParent == null)
                {
                    buildingsParent = SceneObject.Create(
                        SceneObject.Mode.Player, ObjectName.GENERATED_BUILDING_CONTAINER);
                }

                if (pylonsParent == null)
                {
                    pylonsParent = SceneObject.Create(
                    SceneObject.Mode.Player, ObjectName.GENERATED_BUILDING_PYLONS_CONTAINER);
                }

                buildingsGenerated = 0;
                buildingsToGenerate = gameReadyBuildings.Count;

                foreach(GameReadyBuilding building in gameReadyBuildings.Values)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    if (CreateBuilding(
                            buildingsParent.transform,
                            pylonsParent.transform,
                            building))
                    {
                        Interlocked.Increment(ref buildingsGenerated);
                    }
                    else
                    {
                        Interlocked.Decrement(ref buildingsToGenerate);
                    }
                    

                    lock (s_datalock)
                    {
                        buildingsInProgress.Remove(building.buildingObjectId);
                    }
                }

                controller.UpdateState(TerrainController.TerrainState.BuildingsGenerated);
            }
        }

        private bool CreateBuilding(
            Transform transformParent,
            Transform pylonsTransformParent,
            GameReadyBuilding building)
        {
            Abortable abortable = new Abortable("BuildingGenerator.CreateBuilding");

#if false   // future: support holes defined in OSM results
            if (way.holes != null) AddHoles(globalContainer, way, points);
#endif
            if (abortable.shouldAbort)
            {
                return false;
            }

            //  Create game object
            GameObject buildingGameObject = SceneObject.Create(SceneObject.Mode.Player, ObjectName.GENERATED_BUILDING + "_" + building.buildingObjectId);
            buildingGameObject.transform.position = building.localCenterPt;
            try
            {
                buildingGameObjects.Add(building.buildingObjectId, buildingGameObject);
            }
            catch (ArgumentException)
            {
                Trace.Warning("Duplicaate building ID '{0}' encountered in BuildingGenerator.CreatePylons", building.buildingObjectId);
                Destroy(buildingGameObject);
                return false;
            }
            ProceduralBuilding proceduralBuilding = buildingGameObject.AddComponent<ProceduralBuilding>();

            if (abortable.shouldAbort)
            {
                return false;
            }

            //  Create mesh and add material
            proceduralBuilding.id = building.buildingObjectId;
            proceduralBuilding.buildingData = building.buildingData;
            proceduralBuilding.statusDescription = building.statusInfo.description;
            proceduralBuilding.baseHeight = building.baseHeight;
            proceduralBuilding.worldFootprint = building.worldVertices.ToArray();
            proceduralBuilding.localFootprint = building.localVertices;
            proceduralBuilding.roofHeight = building.roofHeight;
            proceduralBuilding.roofType = building.roofType;
            proceduralBuilding.generateWall = true;

            if (building.statusInfo.status == BuildingStatus.NotBuilt)
            {
                proceduralBuilding.wallMaterial  = osmWallMaterial;
                proceduralBuilding.roofMaterial  = osmWallMaterial;
                proceduralBuilding.pylonMaterial = osmPylonMaterial;
            }
            else
            {
                proceduralBuilding.wallMaterial  = wipWallMaterial;
                proceduralBuilding.roofMaterial  = wipRoofMaterial;
                proceduralBuilding.pylonMaterial = wipPylonMaterial;
            }

            proceduralBuilding.pylonsTransformParent = pylonsTransformParent;
            proceduralBuilding.Generate(buidingColliders);
            return true;
        }

        void OnTerrainStateChanged(
            TerrainController.TerrainState newState,
            string newStateMessage,
            TerrainController.TerrainState fullState)
        {
            if (newState == TerrainController.TerrainState.TerrainsGenerated)
            {
                lock (s_datalock)
                {
                    if (buildingGameObjects != null)
                    {
                        //  Update the y position of each building so it rests atop the terrain surface.
                        foreach (GameObject buildingGameObject in buildingGameObjects.Values)
                        {
                            if (buildingGameObject.transform.position.y == 0)
                            {
                                ProceduralBuilding proceduralBuilding = buildingGameObject.GetComponent<ProceduralBuilding>();
                                proceduralBuilding.UpdatePositionOnTerrain();
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (pylonsParent != null)
            {
                Destroy(pylonsParent);
                pylonsParent = null;
            }

            if (buildingGameObjects != null)
            {
                foreach (GameObject gameObject in buildingGameObjects.Values)
                {
                    ProceduralBuilding proceduralBuilding = GetComponent<ProceduralBuilding>();
                    if (proceduralBuilding != null)
                    {
                        proceduralBuilding.Dispose();
                    }
                    Destroy(gameObject);
                }
                buildingGameObjects = null;
            }

            if (buildingsParent != null)
            {
                Destroy(buildingsParent);
                buildingsParent = null;
            }

            loadingGrid.Dispose();
            buildingsInProgress.Dispose();
        }
    }

    public class BuildingsInProgress
    {
        private List<string> _ids;
        private object _datalock = new object();

        public void Add(string id)
        {
            lock (_datalock)
            {
                if (_ids == null)
                {
                    _ids = new List<string>();
                }

                if (!Exists(id))
                {
                    _ids.Add(id);
                }
            }
        }

        public void Remove(string id)
        {
            lock (_datalock)
            {
                if (_ids != null && Exists(id))
                {
                    _ids.RemoveAll(i => (i == id));
                }
            }
        }

        public bool Exists(string id)
        {
            lock (_datalock)
            {
                return _ids != null ? _ids.Exists(i => (i == id)) : false;
            }
        }

        public void Dispose()
        {
            _ids.Clear();
        }
    };
}