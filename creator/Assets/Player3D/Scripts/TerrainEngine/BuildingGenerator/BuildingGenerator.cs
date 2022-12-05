using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine;
using MEC;

namespace TerrainEngine
{
    public class BuildingGenerator : MonoBehaviour
    {
        const float DEFAULT_BUILDING_HEIGHT = 15f;
        const int   BUILDING_COUNT_MAX = 400;
        const int   BUILDINGS_PER_UPDATE = 12;

        //  Configureable via Inspector
        public TerrainPlayer player;
        public float         distanceFilter = 0;
        public Material      wallMaterial;
        public Material      roofMaterial;
        public Material      pylonMaterial;

        public int buildingsToGenerate = 0;
        public int buildingsGenerated = 0;

        //  Private members
        //
        private object s_datalock = new object();
        private TerrainController controller;
        private AreaBuildingData areaBuildingData;
        private BuildingsInProgress buildingsInProgress;

        GameObject buildingsParent;
        GameObject pylonsParent;
        Dictionary<string, GameObject> buildingGameObjects;
       

        private void Awake()
        {
            controller = GetComponent<TerrainController>();
            Trace.Assert(controller != null, "Terrain Controller component was not found on gameObject {0}", gameObject.name);

            TerrainController.OnTerrainStateChanged += OnTerrainStateChanged;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void GenerateBuildings()
        {
            Abortable abortable = new Abortable("BuildingGenerator.GetBuildingCoordinates");

            controller.RunAsync(() =>
            {
                if (abortable.shouldAbort)
                {
                    return;
                }
                WebExceptionStatus status = WebRequestRetries.WebRequestMethodFar(DownloadBuildings);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
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
                TerrainController.RunCoroutine(CreateBuildings(), "CreateBuildings()");
            });
        }

        [RuntimeAsync(nameof(DownloadBuildings))]
        public WebExceptionStatus DownloadBuildings()
        {
            Abortable abortable = new Abortable("BuildingGenerator.DownloadBuildings");

            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            try
            {
                var buildingCoordinates = new BoundryBoxCoordinates()
                {
                    bbox = new double[] {
                        TerrainRuntime.NearMetrics.wgs84Bounds.bottom,
                        TerrainRuntime.NearMetrics.wgs84Bounds.left,
                        TerrainRuntime.NearMetrics.wgs84Bounds.top,
                        TerrainRuntime.NearMetrics.wgs84Bounds.right }
                };

                List<BuildingData> buildingData = Buildings.GetBuildingByBoundryBox(buildingCoordinates).Result;

                if (abortable.shouldAbort)
                {
                    return WebExceptionStatus.RequestCanceled;
                }

                try
                {
                    areaBuildingData = new AreaBuildingData()
                    {
                        buildingData = buildingData,
                        areaBounds = new Wgs84Bounds(TerrainRuntime.NearMetrics.wgs84Bounds)
                    };
                }
                catch (Exception e)
                {
                    if (abortable.shouldAbort)
                    {
                        return WebExceptionStatus.RequestCanceled;
                    }
                    Trace.Exception(e);
                }

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

        private IEnumerator<float> CreateBuildings()
        {
            int buildings_generated = 0;

            while (areaBuildingData == null)
            {
                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }
                yield return Timing.WaitForSeconds(.25f);
            }

            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

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

            if (buildingGameObjects == null)
            {
                buildingGameObjects = new Dictionary<string, GameObject>();
            }

            if (buildingsInProgress == null)
            {
                buildingsInProgress = new BuildingsInProgress();
            }

            buildingsGenerated = 0;

            for (int iBuilding = 0;
                !Abortable.ShouldAbortRoutine() && 
                    iBuilding < areaBuildingData.buildingData.Count /* && buildingsGenerated < BUILDING_COUNT_MAX */; 
                iBuilding++)
            {
                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }

                foreach (List<List<float>> buildingFootprint in areaBuildingData.buildingData[iBuilding].geometry.coordinates)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    //  Check if building is already created or in progress
                    lock (s_datalock)
                    {
                        if (buildingGameObjects.ContainsKey(areaBuildingData.buildingData[iBuilding].id))
                        {
                            continue;
                        }
                        if (buildingsInProgress.Exists(areaBuildingData.buildingData[iBuilding].id))
                        {
                            continue;
                        }
                    }

                    //  Add to buildings-in-progress
                    buildingsInProgress.Add(areaBuildingData.buildingData[iBuilding].id);

                    //  Curate the OSM data
                    Wgs84Bounds bbox;
                    List<Vector3> worldVertices = FilterAndGeoLocateFootprint(
                        ref TerrainRuntime.s_nearGridMetrics,
                        ref TerrainRuntime.s_nearGridMetrics.wgs84Bounds, // TODO: moving window for buildings
                        areaBuildingData.buildingData[iBuilding].center,
                        buildingFootprint,
                        out bbox);

                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    if (worldVertices != null)
                    {
                        CreateBuilding(
                                buildingsParent.transform,
                                pylonsParent.transform,
                                areaBuildingData.buildingData[iBuilding],
                                worldVertices);

                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        buildingsGenerated++;
                    }

                    //  Remove from buildings-in-progress
                    buildingsInProgress.Remove(areaBuildingData.buildingData[iBuilding].id);
                }

                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }
                else if ((buildings_generated % BUILDINGS_PER_UPDATE) == 0)
                {
                    yield return 0;
                }
            }

            Trace.Log("BuidingGenerator: buildings generated = {0}", buildingsGenerated);

            controller.UpdateState(TerrainController.TerrainState.BuildingsGenerated);
        }

        public List<Vector3> FilterAndGeoLocateFootprint(
            ref GridMetrics gridMetrics,
            ref Wgs84Bounds wgs84Bounds,
            Point wgs84Center,
            List<List<float>> buildingFootprint,
            out Wgs84Bounds bbox)
        {
            Abortable abortable = new Abortable("BuildingGenerator.FilterAndGeoLocateFootprint");

            bbox = null;
            List<Vector3> worldVertices = null;
            bbox = null;

            if (buildingFootprint.Count < 3)
            {
                return null;
            }

            if (distanceFilter > 0)
            {
                Vector3 worldCenter = gridMetrics.GeoLocate(wgs84Center.coordinates[1], wgs84Center.coordinates[0]);
                float d2 = (worldCenter.x - player.transform.position.x) * (worldCenter.x - player.transform.position.x) +
                           (worldCenter.y - player.transform.position.y) * (worldCenter.y - player.transform.position.y) +
                           (worldCenter.z - player.transform.position.z) * (worldCenter.z - player.transform.position.z);

                if (d2 > distanceFilter * distanceFilter)
                {
                    return null;
                }
            }


            foreach (List<float> point in buildingFootprint)
            {
                if (abortable.shouldAbort)
                {
                    return null;
                }

                float lat = point[1];
                float lon = point[0];

                if (lat > wgs84Bounds.top || lat < wgs84Bounds.bottom)
                {
                    return null;
                }
                if (lon > wgs84Bounds.right || lon < wgs84Bounds.left)
                {
                    return null;
                }

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

                worldVertices.Add(gridMetrics.GeoLocate(lat, lon));
            }

            if (worldVertices != null)
            {
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

                    if ((worldVertices[prev] - worldVertices[i]).magnitude < 0.01f)
                    {
                        worldVertices.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if ((worldVertices[next] - worldVertices[i]).magnitude < 0.01f)
                    {
                        worldVertices.RemoveAt(next);
                        continue;
                    }

                    float a1 = Angle2D(worldVertices[prev], worldVertices[i]);
                    float a2 = Angle2D(worldVertices[i], worldVertices[next]);

                    if (Mathf.Abs(a1 - a2) < 5)
                    {
                        worldVertices.RemoveAt(i);
                        i--;
                    }
                }

                if (abortable.shouldAbort)
                {
                    return null;
                }

                if (worldVertices.Count < 3)
                {
                    worldVertices = null;
                }
                else if (worldVertices.First() == worldVertices.Last())
                {
                    worldVertices.Remove(worldVertices[worldVertices.Count - 1]);
                    if (worldVertices.Count < 3)
                    {
                        worldVertices = null;
                    }
                }

                if (abortable.shouldAbort)
                {
                    return null;
                }

                if (worldVertices != null)
                {
                    if (IsReversed(worldVertices))
                    {
                        worldVertices.Reverse();
                    }
                }
            }

            return worldVertices;
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

        private bool CreateBuilding(
            Transform transformParent,
            Transform pylonsTransformParent,
            BuildingData buildingData,
            List<Vector3> worldVertices)
        {
            Abortable abortable = new Abortable("BuildingGenerator.CreateBuilding");

            float baseHeight = 0;
            try
            {
                baseHeight = (float)Convert.ToDouble(Regex.Replace(buildingData.details.height, "[^0-9.]", ""));
            }
            catch (Exception)
            {
                Trace.Warning("bad string");
                return false;
            }

            if (baseHeight == 0.0f)
            {
                baseHeight = DEFAULT_BUILDING_HEIGHT;
            }
            ProceduralBuilding.RoofType roofType = ProceduralBuilding.RoofType.flat;
            float roofHeight = 0;

            Vector3 centerPoint = Vector3.zero;
            centerPoint = worldVertices.Aggregate(centerPoint, (current, point) => current + point) / worldVertices.Count;
            centerPoint.y = worldVertices.Min(p => p.y);
            Vector3[] localFootprint = worldVertices.Select(p => p - centerPoint).ToArray();

#if false   // future: support holes defined in OSM results
            if (way.holes != null) AddHoles(globalContainer, way, points);
#endif
            if (abortable.shouldAbort)
            {
                return false;
            }

            //  Create game object
            GameObject buildingGameObject = SceneObject.Create(SceneObject.Mode.Player, ObjectName.GENERATED_BUILDING + "_" + buildingData.id);
            buildingGameObject.transform.position = centerPoint;
            try
            {
                buildingGameObjects.Add(buildingData.id, buildingGameObject);
                buildingsInProgress.Add(buildingData.id);
            }
            catch (ArgumentException)
            {
                Trace.Warning("Duplicaate building ID '{0}' encountered in BuildingGenerator.CreatePylons", buildingData.id);
                Destroy(buildingGameObject);
                return false;
            }
            ProceduralBuilding proceduralBuilding = buildingGameObject.AddComponent<ProceduralBuilding>();

            if (abortable.shouldAbort)
            {
                return false;
            }

            //  Create mesh and add material
            proceduralBuilding.id = buildingData.id;
            proceduralBuilding.buildingData = buildingData;
            proceduralBuilding.baseHeight = baseHeight;
            proceduralBuilding.worldFootprint = worldVertices.ToArray();
            proceduralBuilding.localFootprint = localFootprint;
            proceduralBuilding.roofHeight = roofHeight;
            proceduralBuilding.roofType = roofType;
            proceduralBuilding.generateWall = true;
            proceduralBuilding.wallMaterial = wallMaterial;
            proceduralBuilding.roofMaterial = roofMaterial;
            proceduralBuilding.pylonMaterial = pylonMaterial;
            proceduralBuilding.pylonsTransformParent = pylonsTransformParent;

            proceduralBuilding.Generate();
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

            areaBuildingData = null;

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