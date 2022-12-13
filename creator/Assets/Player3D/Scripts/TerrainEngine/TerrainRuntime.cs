using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using BitMiracle.LibTiff.Classic;
using MEC;

namespace TerrainEngine
{
    public class TerrainRuntime : MonoBehaviour
    {
        //  Constants
        public const int WEBSERVICE_TIMEOUT = 5000;
        public const int TERRAIN_LAYER = 8;

        //  Static properties
        private static TerrainController controller;
        private static TerrainPlayer terrainPlayer;
        private static FloatingOriginAdvanced floatingOriginAdvanced;

        private static object s_datalock = new object();

        public static GridMetrics s_nearGridMetrics;
        public static GridMetrics NearMetrics
        {
            //  read-only for external classes
            get { return s_nearGridMetrics; }
        }

        public static CellMetrics s_farCellMetrics;
        public static CellMetrics FarMetrics
        {
            //  read-only for external classes
            get { return s_farCellMetrics; }
        }

        private static Wgs84Bounds s_farImageCorner;

        private enum Neighbourhood
        {
            Moore = 0,
            VonNeumann = 1
        }
        private static Neighbourhood s_neighbourhood = Neighbourhood.Moore;

        public enum Directions
        {
            East = 0,
            West = 1,
            North = 2,
            South = 3
        }
        public static Directions s_direction;
        public static int s_directionIndex;

        public static bool s_imageDownloadingStarted = false;
        public static Terrain s_terrain;
        private static GameObject s_splittedTerrains;
        public static Terrain s_firstTerrain;
        public static Terrain s_secondaryTerrain;
        public static Terrain s_currentTerrain;
        private static bool s_secondaryTerrainInProgress = true;
        public static List<Terrain> s_croppedTerrains;
        private static int s_stitchDistance;
        private static int s_terrainResolutionDownloading;

        private static List<float> s_topCorner, s_bottomCorner, s_leftCorner, s_rightCorner;
        private static GameObject s_terrainsParent = null;
        private static GameObject s_farTerrainsParent = null;

        private static int s_terrainsLong, s_terrainsWide;
        private static float s_oldWidth, s_oldHeight, s_oldLength;

        private static float s_newWidth, s_newLength;
        private static float s_xPos, s_yPos, s_zPos;
        private static int s_newHeightMapResolution, s_newEvenHeightMapResolution;

        private static List<GameObject> s_terrainGameObjects;
        private static List<GameObject> s_farTerrainGameObjects;
        public static TerrainData s_terrainData;

        private static TerraLandWorldImagery.World_Imagery_MapServer s_mapserviceImagery;
        private static TerraLandWorldElevation.TopoBathy_ImageServer s_mapserviceElevation;
        private static TerraLandWorldImagery.MapServerInfo s_mapinfo;
        private static TerraLandWorldImagery.MapDescription s_mapdesc;

        private static string s_token = "";
        public static string s_fileNameTerrainData;

        public static WebClient s_webClientTerrain;

        public static HeightMapImage s_heightMapTiff;
        public static HeightMapImage s_heightMapFarTerrainImageDetail;

        private static List<float> s_highestPoints;
        private static float s_highestPoint;
        private static float s_lowestPoint;
        private static float s_initialTerrainWidth;

        private static int heightmapResX;
        private static int heightmapResY;
        private static int heightmapResFinalX;
        private static int heightmapResFinalY;
        private static int heightmapResXAll;
        private static int heightmapResYAll;
        private static int heightmapResFinalXAll;
        private static int heightmapResFinalYAll;

        private static int heightmapResXFAR;
        private static int heightmapResYFAR;
        private static int heightmapResFinalXFAR;
        private static int heightmapResFinalYFAR;
        private static int heightmapResFinalXAllFAR;
        private static int heightmapResFinalYAllFAR;

        private static float s_currentHeight;

        private const float s_smoothBlend = 0.8f;
        private static int s_smoothBlendIndex = 0;

        public static float[,] s_finalHeights;

        public static int s_totalImagesDataBase;

        public static float[,] s_heightmapCell;
        public static float[,] s_heightmapCellSec;
        public static float[,] s_heightmapCellFar;

        public static string s_dataBasePathElevation;
        public static string s_geoDataPathElevation;
        public static string s_geoDataExtensionElevation;
        public static string s_dataBasePathImagery;

        public static int m_Width = 1;
        public static int m_Height = 1;
        public static float[,] s_rawData;		//  m_Width x m_Height

        // https://elevation.arcgis.com/arcgis/services/WorldElevation/Terrain/ImageServer?token=
        private const string ELEVATON_URL = "https://elevation.arcgis.com/arcgis/services/WorldElevation/TopoBathy/ImageServer?token=";
        private const string TOKEN_URL = "https://www.arcgis.com/sharing/rest/oauth2/token/authorize?client_id=n0dpgUwqazrQTyXZ&client_secret=3d4867add8ee47b6ac0c498198995298&grant_type=client_credentials&expiration=20160";

        private enum Depth
        {
            Bit8 = 1,
            Bit16
        }
        private static Depth m_Depth = Depth.Bit16;

        private enum ByteOrder
        {
            Mac = 1,
            Windows
        }
        private static ByteOrder m_ByteOrder = ByteOrder.Windows;

        private static string[] terrainNames;

        private static int s_generatedTerrainsCount = 0;
        private static int s_taskIndex;
        private static List<int> s_spiralIndex;
        private static List<Vector2> s_spiralCell;

        public static List<Texture2D> s_baseImageTextures;
        public static Texture2D s_baseImageTextureFar;

        public static float[,,] s_smData;    //  lengthz x widthz x terrainSplitted.terrainData.alphamapLayers	

        private static float s_splatNormalizeX;
        private static float s_splatNormalizeY;

        private static int s_baseImagesPerTerrain;
        private static bool s_multipleTerrainsTiling;
        private static int s_tileGrid;

        public static string[] s_baseImageNames; // Output of Directory.GetFiles 
        public static WebClient s_webClientImage;
        public static Dictionary<int, byte[]> s_baseImageBytes; // Array of downloaded/cached satellite image bytes, near terrain
        public static byte[] s_baseImageBytesFar;   // Array of downloaded/cached satellite image bytes, far terrain
        public static int s_baseImageWidth;           // = controller.imageResolution 
        public static int s_baseImageHeight;          // = controller.imageResolution 
        public static bool s_baseImagesOK = true;

        public static bool s_allBlack = false;

        private static List<CellMetrics> s_directionCoordinates;

        public static int s_processedImageIndex;

        private static int s_compressionQuality = 100;
        private static bool s_availableImageryCheked = false;

        public static int s_nCols;
        public static int s_nRows;
        public static float[,] s_asciiData;
        public static int s_processedHeightmapIndex;
        private static int s_downloadedFarTerrains;
        private static int s_downloadedFarTerrainImages;
        private static readonly string LOCALCACHE_FOLDER_ROOT = Path.GetTempPath() + "Earth9_GIS/";
        private static readonly string LOCALCACHE_ELEVATIONS_FOLDER = LOCALCACHE_FOLDER_ROOT + "/Elevation/";
        private static readonly string LOCALCACHE_BASEIMAGES_FOLDER = LOCALCACHE_FOLDER_ROOT + "/Imagery/";
        public static List<float[,]> s_tiffDataDynamic;

        public static Dictionary<int[], Terrain> s_terrainDict;
        public static Terrain[] s_terrains;

        private static int s_concurrentUpdates = 0;
        private static bool s_hasTop = false;
        private static bool s_hasRight = false;

        private static int s_stitchedTerrainsCount = 0;

        private static double s_latUser;
        private static double s_lonUser;

        public static int s_northCounter = 0;
        public static int s_southCounter = 0;
        public static int s_eastCounter = 0;
        public static int s_westCounter = 0;
        private static int s_northCounterGenerated = 0;
        private static int s_southCounterGenerated = 0;
        private static int s_eastCounterGenerated = 0;
        private static int s_westCounterGenerated = 0;

        public static List<Terrain> s_stitchingTerrainsList;

        private static float s_realTerrainHeight;
        private static float s_realTerrainWidth;
        private static float s_realTerrainLength;

        private static string s_farTerrainTiffName;
        private static double s_areaOffsetMeters;

        private static GameObject s_farTerrainDummy;
        private static bool s_farTerrainInProgress;
        private static float s_farTerrainSize;
        private static bool s_statusIsOKNorth;
        private static bool s_statusIsOKSouth;
        private static bool s_statusIsOKEast;
        private static bool s_statusIsOKWest;

        public static float[,] s_heights;
        public static float[,] s_secondHeights;

        private static bool s_stitchingInProgress;
        private static List<Terrain> s_terrainsInProgress;

        private static bool s_generationIsBusyNORTH;
        private static bool s_generationIsBusySOUTH;
        private static bool s_generationIsBusyEAST;
        private static bool s_generationIsBusyWEST;


        public static string s_terrainGridName = "";
        public static int    s_terrainGridCount = 0;

        //  Async heightmap data
        private class FillHeightsData
        {
            public int row;
            public int col;
            public float[,] data;
        };

        //  Async heightmap data for FAR terrain
        //
        //  There is a significant performance penalty in generating heightmap data on the main thread for
        //  the low-res yet large expanse comprising the FAR terrain. Therefore we
        //      1) generate the data asynchronously on a worker thread (GetTerrainHeightsFromTIFFFAR()), and
        //      2) because Unity requires we do so on the main thread, apply it via 
        //         coroutine (ApplyTerrainHeightsFromTIFFFAR()) on the main thread.
        //  This class represents the intermediate data buffer generated in step 1 and aplied in step 2.
        private class FillHeightsFARData
        {
            public List<FillHeightsData> all = new List<FillHeightsData>();

            public void Apply(Terrain terrainTile, float[,] terrainHeights)
            {
                foreach (FillHeightsData data in all)
                {
                    if (controller.delayedLOD)
                    {
                        terrainTile.terrainData.SetHeightsDelayLOD(data.row, data.col, data.data);
                    }
                    else
                    {
                        terrainTile.terrainData.SetHeights(data.row, data.col, data.data);
                    }
                }
                all.Clear();  //  data is no longer needed
            }
        }
        private static FillHeightsFARData farHeightsData;

        public static bool UnloadAllAssets()
        {
            if (s_terrainsParent != null)
            {
                TerrainInfiniteController infiniteTerrain = s_terrainsParent.GetComponent<TerrainInfiniteController>();
                if (infiniteTerrain != null)
                {
                    infiniteTerrain.UnloadAllAssets();
                    Destroy(infiniteTerrain);
                }

                Destroy(s_terrainsParent);
                s_terrainsParent = null;
            }

            Destroy(s_terrain);
            s_terrain = null;
            
            Destroy(s_firstTerrain);
            s_firstTerrain = null;
            
            Destroy(s_secondaryTerrain);
            s_secondaryTerrain = null;
            
            Destroy(s_currentTerrain);
            s_currentTerrain = null;
            
            Destroy(s_baseImageTextureFar);
            s_baseImageTextureFar = null;
            
            Destroy(s_terrainData);
            s_terrainData = null;
            
            Destroy(s_splittedTerrains);
            s_splittedTerrains = null;
            
            Destroy(s_farTerrainsParent);
            s_farTerrainsParent = null;

            s_webClientTerrain = null;
            s_finalHeights = null;
            s_heightmapCell = null;
            s_heightmapCellSec = null;
            s_heightmapCellFar = null;
            s_rawData = null;
            s_webClientImage = null;
            s_smData = null;
            s_baseImageBytesFar = null;
            s_asciiData = null;
            s_terrainDict = null;
            s_heights = null;
            s_farImageCorner = null;
            s_terrainsInProgress = null;
            s_secondHeights = null;
            s_heightMapTiff = null;
            s_directionCoordinates = null;
            s_heightMapFarTerrainImageDetail = null;
            s_baseImageBytes = null;
            s_nearGridMetrics = null;
            s_processedImageIndex = 0;
            s_processedHeightmapIndex = 0;


            if (s_heightMapTiff != null)
            {
                s_heightMapTiff.data = null;
                s_heightMapTiff.dataASCII = null;
                s_heightMapTiff = null;
            }

            if (s_heightMapFarTerrainImageDetail != null)
            {
                s_heightMapFarTerrainImageDetail.data = null;
                s_heightMapFarTerrainImageDetail.dataASCII = null;
                s_heightMapFarTerrainImageDetail = null;
            }

            if (s_croppedTerrains != null)
            {
                for (int i = 0; i < s_croppedTerrains.Count; i++)
                {
                    Destroy(s_croppedTerrains[i]);
                }
                s_croppedTerrains = null;
            }

            if (s_baseImageTextures != null)
            {
                for (int i = 0; i < s_baseImageTextures.Count; i++)
                {
                    Destroy(s_baseImageTextures[i]);
                }
                s_baseImageTextures = null;
            }

            if (s_tiffDataDynamic != null)
            {
                for (int i = 0; i < s_tiffDataDynamic.Count; i++)
                {
                    s_tiffDataDynamic[i] = null;
                }
                s_tiffDataDynamic = null;
            }

            if (s_terrains != null)
            {
                for (int i = 0; i < s_terrains.Length; i++)
                {
                    Destroy(s_terrains[i]);
                }
                s_terrains = null;
            }

            if (s_stitchingTerrainsList != null)
            {
                for (int i = 0; i < s_stitchingTerrainsList.Count; i++)
                {
                    Destroy(s_stitchingTerrainsList[i]);
                }
                s_stitchingTerrainsList = null;
            }

            if (s_terrainGameObjects != null)
            {
                for (int i = 0; i < s_terrainGameObjects.Count; i++)
                {
                    Destroy(s_terrainGameObjects[i]);
                }
                s_terrainGameObjects = null;
            }

            if (s_farTerrainGameObjects != null)
            {
                for (int i = 0; i < s_farTerrainGameObjects.Count; i++)
                {
                    Destroy(s_farTerrainGameObjects[i]);
                }
                s_farTerrainGameObjects = null;
            }

            return true;
        }

        public static void Initialize(GameObject gameObject)
        {
            if (Abortable.count != 0)
            {
                Trace.Warning("There should be no inner cancellables at outset of TerrainRuntime.Initialize()");
            }

            Abortable abortable = new Abortable("TerrainRuntime.Initialize");

            if (terrainPlayer == null)
            {
                terrainPlayer = TerrainPlayer.Get(gameObject);
                Trace.Assert(terrainPlayer != null,
                    "TerrainPlayer component or scene player not found for GameObject '{0}'.", gameObject.name);
            }

            if (s_mapserviceImagery == null)
            {
                s_mapserviceImagery = new TerraLandWorldImagery.World_Imagery_MapServer();
                s_mapserviceImagery.Timeout = WEBSERVICE_TIMEOUT;
            }

            if (s_mapserviceElevation == null)
            {
                s_mapserviceElevation = new TerraLandWorldElevation.TopoBathy_ImageServer();
                s_mapserviceElevation.Timeout = WEBSERVICE_TIMEOUT;
            }

            controller = TerrainController.Get();

            s_terrainGridName = "initial";
            s_nearGridMetrics = new GridMetrics();
            NearMetrics.InitializeArea(
                Convert.ToDouble(controller.latitudeUser), Convert.ToDouble(controller.longitudeUser),
                controller.areaSize, controller.areaSize,
                (int)controller.terrainGridSize, (int)controller.terrainGridSize); ;
            NearMetrics.InitializeHeightMapMetrics(controller.heightmapResolution);
            NearMetrics.InitializeBaseImageMetrics();
            NearMetrics.InitializeCellMetrics(s_terrainGridName);

            s_generatedTerrainsCount = 0;
            s_taskIndex = controller.concurrentTasks;
            s_concurrentUpdates = 0;
            s_stitchDistance = controller.stitchDistance;
            s_terrainsInProgress = new List<Terrain>();
            s_directionCoordinates = new List<CellMetrics>();
            s_heightMapTiff = new HeightMapImage();
            s_heightMapFarTerrainImageDetail = new HeightMapImage();

            s_farImageCorner = new Wgs84Bounds();
            GetTerrainBoundsFar();

            try
            {
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            if (controller.concurrentTasks > NearMetrics.cellCount)
            {
                controller.concurrentTasks = NearMetrics.cellCount;
            }
            else if (controller.concurrentTasks < 1)
            {
                controller.concurrentTasks = 1;
            }

            if (controller.progressiveGeneration)
            {
                controller.spiralGeneration = false;
            }

            if (controller.spiralGeneration)
            {
                SpiralOrder();
            }
            else
            {
                NormalOrder();
            }

            SetupDownloaderElevation();
            controller.GetHeightmaps();
            controller.GetBuildings();
            if (controller.farTerrain)
            {
                controller.GetHeightmapFAR();
            }

            if (!controller.elevationOnly)
            {
                GetImagesInfo();
                ImageTilerOnline();
                controller.GetSatelliteImages();

                if (controller.farTerrain)
                {
                    controller.GetSatelliteImagesFAR();
                }
            }
        }

        private static void SpiralOrder()
        {
            try
            {
                s_spiralIndex = new List<int>();
                int[,] indexFromCenter = new int[NearMetrics.cellCountEdge, NearMetrics.cellCountEdge];
                int length = NearMetrics.cellCountEdge;
                int index = 0;

                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < length; j++)
                    {
                        indexFromCenter[i, j] = index++;
                    }
                }

                SpiralOrderOperation(indexFromCenter);

                s_spiralIndex.Reverse();
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static void NormalOrder()
        {
            for (int i = 0; i < NearMetrics.cellCount; i++)
            {
                s_spiralIndex.Add(i);
            }
        }

        public static void GetTerrainBoundsFar() // TODO: Deprecate to FarMetrics
        {
            try
            {
                s_latUser = double.Parse(controller.latitudeUser);
                s_lonUser = double.Parse(controller.longitudeUser);

                //offsets in meters
                double dn = (NearMetrics.spanY_km * 1000f * controller.areaSizeFarMultiplier) / 2.0;
                double de = (NearMetrics.spanX_km * 1000f * controller.areaSizeFarMultiplier) / 2.0;

                //Coordinate offsets in radians
                double dLat = dn / GeoConst.EARTH_RADIUS_EQM;
                double dLon = de / (GeoConst.EARTH_RADIUS_EQM * Math.Cos(Math.PI * s_latUser / 180));

                s_farImageCorner.top    = s_latUser + dLat * 180 / Math.PI;
                s_farImageCorner.left  = s_lonUser - dLon * 180 / Math.PI;
                s_farImageCorner.bottom = s_latUser - dLat * 180 / Math.PI;
                s_farImageCorner.right = s_lonUser + dLon * 180 / Math.PI;
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        public static void GetTerrainBoundsNORTH(string terrainGridName)
        {
            try
            {
                s_terrainGridName = terrainGridName;
                s_terrainGridCount = NearMetrics.cellCount;
                s_areaOffsetMeters = (NearMetrics.spanY_km * 1000f) / NearMetrics.cellCountEdge;
                double areaOffsetDegrees = (s_areaOffsetMeters / GeoConst.EARTH_RADIUS_EQM) * 180 / Math.PI;
                NearMetrics.wgs84Bounds.OffsetLatitude(areaOffsetDegrees);

                NearMetrics.InitializeCellMetrics(terrainGridName);
                GetDynamicTerrainNORTH(0);

                if (!controller.elevationOnly)
                {
                    controller.GetSatelliteImagesNORTH();
                }

                if (controller.farTerrain)
                {
                    s_farImageCorner.OffsetLatitude(areaOffsetDegrees);

                    s_farTerrainDummy.transform.position = new Vector3
                        (
                            s_farTerrainDummy.transform.position.x,
                            s_farTerrainDummy.transform.position.y,
                            s_farTerrainDummy.transform.position.z + (float)s_areaOffsetMeters
                        );
                }

                if (!TerrainInfiniteController.inProgressSouth && !TerrainInfiniteController.inProgressEast && !TerrainInfiniteController.inProgressWest)
                {
                    //if(TerrainInfiniteController.isOneStepNorth)
                    s_statusIsOKNorth = true;
                }
                else
                    s_statusIsOKNorth = false;

                if (controller.farTerrain && s_statusIsOKNorth && !s_farTerrainInProgress)
                {
                    s_farTerrainInProgress = true;
                    SwitchFarTerrains(s_farTerrainDummy.transform.position);
                    controller.GetHeightmapFAR();

                    if (!controller.elevationOnly)
                    {
                        controller.GetSatelliteImagesFAR();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        public static void GetTerrainBoundsSOUTH(string terrainGridName)
        {
            try
            {
                s_terrainGridName = terrainGridName;
                s_terrainGridCount = NearMetrics.cellCount;
                s_areaOffsetMeters = (NearMetrics.spanY_km * 1000f) / NearMetrics.cellCountEdge;
                double areaOffsetDegrees = (s_areaOffsetMeters / GeoConst.EARTH_RADIUS_EQM) * 180 / Math.PI;
                NearMetrics.wgs84Bounds.OffsetLatitude(-areaOffsetDegrees);

                NearMetrics.InitializeCellMetrics(terrainGridName);
                GetDynamicTerrainSOUTH();

                if (!controller.elevationOnly)
                {
                    controller.GetSatelliteImagesSOUTH();
                }

                if (controller.farTerrain)
                {
                    s_farImageCorner.OffsetLatitude(-areaOffsetDegrees);

                    s_farTerrainDummy.transform.position = new Vector3
                        (
                            s_farTerrainDummy.transform.position.x,
                            s_farTerrainDummy.transform.position.y,
                            s_farTerrainDummy.transform.position.z - (float)s_areaOffsetMeters
                        );
                }

                //direction = "south";

                if (!TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressEast && !TerrainInfiniteController.inProgressWest)
                {
                    //if(TerrainInfiniteController.isOneStepSouth)
                    s_statusIsOKSouth = true;
                }
                else
                {
                    s_statusIsOKSouth = false;
                }

                if (controller.farTerrain && s_statusIsOKSouth && !s_farTerrainInProgress)
                {
                    s_farTerrainInProgress = true;
                    SwitchFarTerrains(s_farTerrainDummy.transform.position);
                    controller.GetHeightmapFAR();

                    if (!controller.elevationOnly)
                    {
                        controller.GetSatelliteImagesFAR();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        public static void GetTerrainBoundsEAST(string terrainGridName)
        {
            try
            {
                double centerLat = (NearMetrics.wgs84Bounds.top + NearMetrics.wgs84Bounds.bottom) / 2.0;
                s_areaOffsetMeters = (NearMetrics.spanY_km * 1000f) / NearMetrics.cellCountEdge;
                double areaOffsetDegrees = (s_areaOffsetMeters / (GeoConst.EARTH_RADIUS_EQM * Math.Cos(Math.PI * centerLat / 180))) * 180 / Math.PI;
                NearMetrics.wgs84Bounds.OffsetLongitude(areaOffsetDegrees);

                s_terrainGridName = terrainGridName;
                s_terrainGridCount = NearMetrics.cellCount;
                NearMetrics.InitializeCellMetrics(terrainGridName);
                GetDynamicTerrainEAST();

                if (!controller.elevationOnly)
                {
                    controller.GetSatelliteImagesEAST();
                }

                if (controller.farTerrain)
                {
                    s_farImageCorner.OffsetLongitude(areaOffsetDegrees);

                    s_farTerrainDummy.transform.position = new Vector3
                        (
                            s_farTerrainDummy.transform.position.x + (float)s_areaOffsetMeters,
                            s_farTerrainDummy.transform.position.y,
                            s_farTerrainDummy.transform.position.z
                        );
                }

                //direction = "east";

                if (!TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth && !TerrainInfiniteController.inProgressWest)
                {
                    //if(TerrainInfiniteController.isOneStepEast)
                    s_statusIsOKEast = true;
                }
                else
                    s_statusIsOKEast = false;

                if (controller.farTerrain && s_statusIsOKEast && !s_farTerrainInProgress)
                {
                    s_farTerrainInProgress = true;
                    SwitchFarTerrains(s_farTerrainDummy.transform.position);
                    controller.GetHeightmapFAR();

                    if (!controller.elevationOnly)
                    {
                        controller.GetSatelliteImagesFAR();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        public static void GetTerrainBoundsWEST(string terrainGridName)
        {
            try
            {
                double centerLat = (NearMetrics.wgs84Bounds.top + NearMetrics.wgs84Bounds.bottom) / 2.0;
                s_areaOffsetMeters = (NearMetrics.spanY_km * 1000f) / NearMetrics.cellCountEdge;
                double areaOffsetDegrees = (s_areaOffsetMeters / (GeoConst.EARTH_RADIUS_EQM * Math.Cos(Math.PI * centerLat / 180))) * 180 / Math.PI;
                NearMetrics.wgs84Bounds.OffsetLongitude(-areaOffsetDegrees);

                s_terrainGridName = terrainGridName;
                s_terrainGridCount = NearMetrics.cellCount;
                NearMetrics.InitializeCellMetrics(terrainGridName);
                GetDynamicTerrainWEST();

                if (!controller.elevationOnly)
                {
                    controller.GetSatelliteImagesWEST();
                }

                if (controller.farTerrain)
                {
                    s_farImageCorner.OffsetLongitude(-areaOffsetDegrees);

                    s_farTerrainDummy.transform.position = new Vector3
                        (
                            s_farTerrainDummy.transform.position.x - (float)s_areaOffsetMeters,
                            s_farTerrainDummy.transform.position.y,
                            s_farTerrainDummy.transform.position.z
                        );
                }

                if (!TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth && !TerrainInfiniteController.inProgressEast)
                {
                    //if(TerrainInfiniteController.isOneStepWest)
                    s_statusIsOKWest = true;
                }
                else
                {
                    s_statusIsOKWest = false;
                }

                if (controller.farTerrain && s_statusIsOKWest && !s_farTerrainInProgress)
                {
                    s_farTerrainInProgress = true;
                    SwitchFarTerrains(s_farTerrainDummy.transform.position);
                    controller.GetHeightmapFAR();

                    if (!controller.elevationOnly)
                    {
                        controller.GetSatelliteImagesFAR();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static void SwitchFarTerrains(Vector3 currentPos)
        {
            if (s_secondaryTerrainInProgress)
                s_terrain = s_secondaryTerrain;
            else
                s_terrain = s_firstTerrain;

            s_terrain.transform.position = currentPos;
        }

        private static void SwitchFarTerrainsCompleted()
        {
            if (s_secondaryTerrainInProgress)
            {
                s_firstTerrain.drawHeightmap = false;
                s_secondaryTerrain.drawHeightmap = true;
            }
            else
            {
                s_secondaryTerrain.drawHeightmap = false;
                s_firstTerrain.drawHeightmap = true;
            }

            s_farTerrainInProgress = false;
            s_secondaryTerrainInProgress = !s_secondaryTerrainInProgress;
        }

        public static void GetDynamicTerrainNORTH(int index)
        {
            controller.GetHeightmapsNORTH(index);
        }

        public static void GetDynamicTerrainSOUTH()
        {
            controller.GetHeightmapsSOUTH();
        }

        public static void GetDynamicTerrainEAST()
        {
            controller.GetHeightmapsEAST();
        }

        public static void GetDynamicTerrainWEST()
        {
            controller.GetHeightmapsWEST();
        }

        private static void SpiralOrderOperation(int[,] matrix)
        {
            if (matrix.Length == 0)
                return;

            int topIndex = 0;
            int downIndex = NearMetrics.cellCountEdge - 1;
            int leftIndex = 0;
            int rightIndex = NearMetrics.cellCountEdge - 1;

            while (true)
            {
                // top row
                for (int j = leftIndex; j <= rightIndex; ++j)
                {
                    s_spiralIndex.Add(matrix[topIndex, j]);
                }

                topIndex++;

                if (topIndex > downIndex || leftIndex > rightIndex)
                {
                    break;
                }

                // rightmost column
                for (int i = topIndex; i <= downIndex; ++i)
                {
                    s_spiralIndex.Add(matrix[i, rightIndex]);
                }

                rightIndex--;

                if (topIndex > downIndex || leftIndex > rightIndex)
                {
                    break;
                }

                // bottom row
                for (int j = rightIndex; j >= leftIndex; --j)
                {
                    s_spiralIndex.Add(matrix[downIndex, j]);
                }

                downIndex--;

                if (topIndex > downIndex || leftIndex > rightIndex)
                {
                    break;
                }

                // leftmost column
                for (int i = downIndex; i >= topIndex; --i)
                {
                    s_spiralIndex.Add(matrix[i, leftIndex]);
                }

                leftIndex++;

                if (topIndex > downIndex || leftIndex > rightIndex)
                {
                    break;
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoElevation))]
        public static void ServerInfoElevation()
        {
            try
            {
                for (int i = 0; i < NearMetrics.cellCount; i++)
                {
                    CellMetrics metrics;

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }

                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);
                    controller.ServerConnectHeightmap(i, metrics.slippyTileName);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(ServerInfoElevationFAR))]
        public static void ServerInfoElevationFAR()
        {
            controller.ServerConnectHeightmapFAR();
        }

        [RuntimeAsync(nameof(ServerInfoElevationNORTH))]
        public static void ServerInfoElevationNORTH(int index)
        {
            if (TerrainInfiniteController.s_northTerrains.Count > 0)
            {
                s_northCounter = 0;

                for (int x = 0; x < NearMetrics.cellCountEdge; x++)
                {
                    int i = TerrainInfiniteController.northIndex + x;

                    CellMetrics metrics;

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }
                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

                    s_direction = Directions.North;
                    s_directionIndex = i; 
                    
                    controller.ServerConnectHeightmapNORTH(i, metrics.slippyTileName);
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoElevationSOUTH))]
        public static void ServerInfoElevationSOUTH()
        {
            if (TerrainInfiniteController.s_southTerrains.Count > 0)
            {
                s_southCounter = 0;

                for (int x = 0; x < NearMetrics.cellCountEdge; x++)
                {
                    int i = TerrainInfiniteController.southIndex + x;

                    CellMetrics metrics;

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }
                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

                    s_direction = Directions.South;
                    s_directionIndex = i;

                    controller.ServerConnectHeightmapSOUTH(i, metrics.slippyTileName);
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoElevationEAST))]
        public static void ServerInfoElevationEAST()
        {
            if (TerrainInfiniteController.s_eastTerrains.Count > 0)
            {
                s_eastCounter = 0;

                for (int x = 0; x < NearMetrics.cellCountEdge; x++)
                {
                    int i = TerrainInfiniteController.eastIndex + (x * NearMetrics.cellCountEdge);

                    CellMetrics metrics;

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }
                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

                    s_direction = Directions.East;
                    s_directionIndex = i;
                    
                    controller.ServerConnectHeightmapEAST(i, metrics.slippyTileName);
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoElevationWEST))]
        public static void ServerInfoElevationWEST()
        {
            if (TerrainInfiniteController.s_westTerrains.Count > 0)
            {
                s_westCounter = 0;

                for (int x = 0; x < NearMetrics.cellCountEdge; x++)
                {
                    int i = TerrainInfiniteController.westIndex + (x * NearMetrics.cellCountEdge);

                    CellMetrics metrics;

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }
                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

                    s_direction = Directions.West;
                    s_directionIndex = i;
                    
                    controller.ServerConnectHeightmapWEST(i, metrics.slippyTileName);
                    Thread.Sleep(500);
                }
            }
        }

        private static void SetupDownloaderElevation()
        {
            DirectoryInfo di = new DirectoryInfo(LOCALCACHE_FOLDER_ROOT);

            if (!Directory.Exists(LOCALCACHE_FOLDER_ROOT))
            {
                Directory.CreateDirectory(LOCALCACHE_FOLDER_ROOT);

                if (!Directory.Exists(LOCALCACHE_ELEVATIONS_FOLDER))
                {
                    Directory.CreateDirectory(LOCALCACHE_ELEVATIONS_FOLDER);
                }
                if (!Directory.Exists(LOCALCACHE_BASEIMAGES_FOLDER))
                {
                    Directory.CreateDirectory(LOCALCACHE_BASEIMAGES_FOLDER);
                }
            }
            else
            {
                if (!Directory.Exists(LOCALCACHE_ELEVATIONS_FOLDER))
                {
                    Directory.CreateDirectory(LOCALCACHE_ELEVATIONS_FOLDER);
                }
                if (!Directory.Exists(LOCALCACHE_BASEIMAGES_FOLDER))
                {
                    Directory.CreateDirectory(LOCALCACHE_BASEIMAGES_FOLDER);
                }
            }

            GenerateNewTerrainObject();

            if (controller.farTerrain)
            {
                s_farTerrainTiffName = LOCALCACHE_ELEVATIONS_FOLDER + "FarTerrain.tif";
                CreateFarTerrainObject();
            }

#if !UNITY_2_6 && !UNITY_2_6_1 && !UNITY_3_0 && !UNITY_3_0_0 && !UNITY_3_1 && !UNITY_3_2 && !UNITY_3_3 && !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2 && !UNITY_4_3 && !UNITY_4_4 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
            RemoveLightmapStatic();
#endif

            s_terrainResolutionDownloading = controller.heightmapResolution + NearMetrics.splitSizeFinal;

            s_topCorner = new List<float>();
            s_bottomCorner = new List<float>();
            s_leftCorner = new List<float>();
            s_rightCorner = new List<float>();

            s_tiffDataDynamic = new List<float[,]>();

            for (int i = 0; i < NearMetrics.cellCount; i++)
            {
                s_tiffDataDynamic.Add(new float[NearMetrics.heightMapResolution, NearMetrics.heightMapResolution]);
            }

            s_highestPoints = new List<float>();
            s_processedHeightmapIndex = 0;
            s_downloadedFarTerrains = 0;
            s_downloadedFarTerrainImages = 0;

            s_realTerrainHeight = GeoConst.EVEREST_PEAK_METERS * controller.elevationExaggeration;
        }

        [RuntimeAsync(nameof(HeightMapImageDownload))]
        public static WebExceptionStatus HeightMapImageDownload(string fileName)
        {
            //  Retrieve the CellMetrics for the requested filename
            CellMetrics metrics;

            lock (s_datalock)
            {
                metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.slippyTileName == fileName && x.terrainGridName == s_terrainGridName);
            }
            Trace.Assert(metrics != null,
                "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.",
                fileName);

            try
            {
                string slippyTileFilePath = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                Trace.Log(TerrainController.traceDebug,
                   "Tile check: Index {0}: Filename: '{1}'",
                   metrics.key,
                   metrics.slippyTileName);

                if (!File.Exists(slippyTileFilePath))
                {
                    if (s_directionIndex == -1)
                    {
                        GenerateToken(false, false, false, false, metrics.key);
                    }
                    else
                    {
                        if (s_direction == Directions.East)
                        {
                            if (s_directionIndex == TerrainInfiniteController.eastIndex)
                            {
                                GenerateToken(false, false, true, false, s_directionIndex);
                            }
                        }
                        else if (s_direction == Directions.West)
                        {
                            if (s_directionIndex == TerrainInfiniteController.westIndex)
                            {
                                GenerateToken(false, false, false, true, s_directionIndex);
                            }
                        }
                        else if (s_direction == Directions.North)
                        {
                            if (s_directionIndex == TerrainInfiniteController.northIndex)
                            {
                                GenerateToken(true, false, false, false, s_directionIndex);
                            }
                        }
                        else if (s_direction == Directions.South)
                        {
                            if (s_directionIndex == TerrainInfiniteController.southIndex)
                            {
                                GenerateToken(false, true, false, false, s_directionIndex);
                            }
                        }
                    }

                    s_mapserviceElevation.Url = ELEVATON_URL + s_token;
                    TerraLandWorldElevation.GeoImageDescription geoImgDesc = new TerraLandWorldElevation.GeoImageDescription();

                    geoImgDesc.Height = NearMetrics.heightMapResolution;
                    geoImgDesc.Width = NearMetrics.heightMapResolution;
                    geoImgDesc.Compression = "LZW";
                    geoImgDesc.CompressionQuality = 100;
                    geoImgDesc.Interpolation = TerraLandWorldElevation.rstResamplingTypes.RSP_CubicConvolution;
                    geoImgDesc.NoDataInterpretationSpecified = true;
                    geoImgDesc.NoDataInterpretation = TerraLandWorldElevation.esriNoDataInterpretation.esriNoDataMatchAny;

                    TerraLandWorldElevation.EnvelopeN extent = new TerraLandWorldElevation.EnvelopeN();
                    extent.XMin = metrics.mercatorBounds.left;
                    extent.YMin = metrics.mercatorBounds.bottom;
                    extent.XMax = metrics.mercatorBounds.right;
                    extent.YMax = metrics.mercatorBounds.top;
                    geoImgDesc.Extent = extent;
                    TerraLandWorldElevation.ImageType imageType = new TerraLandWorldElevation.ImageType();
                    imageType.ImageFormat = TerraLandWorldElevation.esriImageFormat.esriImageTIFF;
                    imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnMimeData;

                    TerraLandWorldElevation.ImageResult result = s_mapserviceElevation.ExportImage(geoImgDesc, imageType);

                    File.WriteAllBytes(slippyTileFilePath, result.ImageData);
                }
                else
                {
                    Trace.Log(TerrainController.traceDebug,
                         "Heightmap got from cache: Index: {0}, Filename: '{1}'",
                         metrics.key,
                         metrics.slippyTileName);
                }
            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e,
                    "Exception exporting ImageDownloader #{0}. DETAILS:",
                    metrics.key);
                return WebExceptionStatus.UnknownError;
            }

            return WebExceptionStatus.Success;
        }

        [RuntimeAsync(nameof(ElevationDownload))]
        public static WebExceptionStatus ElevationDownload(string fileName)
        {
            try
            {
                s_directionIndex = -1;
                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(HeightMapImageDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
                    return status;
                }

                lock (s_datalock)
                {
                    if (s_processedHeightmapIndex == 0)
                    {
                        CalculateResampleHeightmaps();
                    }
                }
            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e,
                    "Exception exporting elevation file '{0}'",
                    fileName);
                return WebExceptionStatus.UnknownError;
            }
            finally
            {
                Interlocked.Increment(ref s_processedHeightmapIndex);
            }
            return WebExceptionStatus.Success;
        }

        public static void LoadHeights(int i)
        {
            if (controller.progressiveGeneration)
            {
                TerrainController.RunCoroutine(LoadTerrainHeightsFromTIFFDynamic(i),
                    "LoadHeights(i: {0}) progressiveGeneration",
                    "LoadTerrainHeightsFromTIFFDynamic(i: {0})",
                    i);
            }
            else
            {
                lock (s_datalock)
                {
                    if (s_processedHeightmapIndex == NearMetrics.cellCount)
                    {
                        controller.LoadTerrainHeights();
                    }
                }
            }
        }

        [RuntimeAsync(nameof(ElevationDownloadFAR))]
        public static WebExceptionStatus ElevationDownloadFAR()
        {
            try
            {
                double XMin = s_farImageCorner.left * 20037508.34 / 180.0;
                double yMaxTopFar = Math.Log(Math.Tan((90.0 + s_farImageCorner.top) * Math.PI / 360.0)) / (Math.PI / 180.0);
                double YMax = yMaxTopFar * 20037508.34 / 180.0;
                double XMax = s_farImageCorner.right * 20037508.34 / 180.0;
                double yMinBottomFar = Math.Log(Math.Tan((90.0 + s_farImageCorner.bottom) * Math.PI / 360.0)) / (Math.PI / 180.0);
                double YMin = yMinBottomFar * 20037508.34 / 180.0;

                if (s_farCellMetrics == null)
                {
                    Bounds2D mercator = new Bounds2D()
                    {
                        top = YMax,
                        bottom = YMin,
                        left = XMin,
                        right = XMax,
                    };

                    s_farCellMetrics = new CellMetrics()
                    {
                        key = -1,
                        mercatorBounds = mercator,
                        wgs84Bounds = s_farImageCorner,
                        terrainGridName = s_terrainGridName
                    };
                    s_farCellMetrics.slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(s_farCellMetrics);
                }

                string filePath = LOCALCACHE_ELEVATIONS_FOLDER + FarMetrics.slippyTileName;
                if (!File.Exists(filePath))
                {
                    GenerateToken(false, false, false, false, 0);
                    s_mapserviceElevation.Url = ELEVATON_URL + s_token;

                    TerraLandWorldElevation.GeoImageDescription geoImgDesc = new TerraLandWorldElevation.GeoImageDescription();

                    geoImgDesc.Height = controller.farTerrainHeightmapResolution + 1;
                    geoImgDesc.Width = controller.farTerrainHeightmapResolution + 1;

                    geoImgDesc.Compression = "LZW";
                    geoImgDesc.CompressionQuality = 100;
                    geoImgDesc.Interpolation = TerraLandWorldElevation.rstResamplingTypes.RSP_CubicConvolution;
                    geoImgDesc.NoDataInterpretationSpecified = true;
                    geoImgDesc.NoDataInterpretation = TerraLandWorldElevation.esriNoDataInterpretation.esriNoDataMatchAny;

                    TerraLandWorldElevation.EnvelopeN extentElevation = new TerraLandWorldElevation.EnvelopeN();
                    extentElevation.XMin = XMin;
                    extentElevation.YMax = YMax;
                    extentElevation.XMax = XMax;
                    extentElevation.YMin = YMin;
                    geoImgDesc.Extent = extentElevation;

                    TerraLandWorldElevation.ImageType imageType = new TerraLandWorldElevation.ImageType();
                    imageType.ImageFormat = TerraLandWorldElevation.esriImageFormat.esriImageTIFF;
                    imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnMimeData;

                    TerraLandWorldElevation.ImageResult result = s_mapserviceElevation.ExportImage(geoImgDesc, imageType);

                    using (var ms = new MemoryStream(result.ImageData))
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                    }
                }
            }
            catch (WebException e)
            {
                return e.Status; 
            }
            catch (Exception e)
            {
                Trace.Exception(e, "Exception exporting FAR elevation tile. DETAILS:");
                return WebExceptionStatus.UnknownError;
            }
            finally
            {
                Interlocked.Increment(ref s_downloadedFarTerrains);
            }
            return WebExceptionStatus.Success;
        }

        public static void LoadHeightsFAR()
        {
            controller.GetTerrainHeightsFAR();
        }

        [RuntimeAsync(nameof(ElevationDownloadNORTH))]
        public static void ElevationDownloadNORTH(int i, string fileName)
        {
            try
            {
                if (i == TerrainInfiniteController.northIndex)
                {
                    GenerateToken(true, false, false, false, i);
                }

                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(HeightMapImageDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
                    return;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                return;
            }
            finally
            {
                Interlocked.Increment(ref s_northCounter);
            }
        }

        [RuntimeAsync(nameof(ElevationDownloadSOUTH))]
        public static void ElevationDownloadSOUTH(int i, string fileName)
        {
            try
            {
                if (i == TerrainInfiniteController.southIndex)
                {
                    GenerateToken(false, true, false, false, i);
                }

                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(HeightMapImageDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
                    return;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                return;
            }
            finally
            {
                Interlocked.Increment(ref s_southCounter);
            }
        }

        [RuntimeAsync(nameof(ElevationDownloadEAST))]
        public static void ElevationDownloadEAST(int i, string fileName)
        {
            try
            {
                if (i == TerrainInfiniteController.eastIndex)
                {
                    GenerateToken(false, false, true, false, i);
                }

                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(HeightMapImageDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
                    return;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                return;
            }
            finally
            {
                Interlocked.Increment(ref s_eastCounter);
            }
        }

        [RuntimeAsync(nameof(ElevationDownloadWEST))]
        public static void ElevationDownloadWEST(int i, string fileName)
        {
            try
            {
                if (i == TerrainInfiniteController.westIndex)
                {
                    GenerateToken(false, false, false, true, i);
                }

                WebExceptionStatus status = WebRequestRetries.WebRequestMethod(HeightMapImageDownload, fileName);
                if (status != WebExceptionStatus.Success)
                {
                    controller.ReportFatalWebServiceError(status);
                    return;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                return;
            }
            finally
            {
                Interlocked.Increment(ref s_westCounter);
            }
        }

        public static void LoadHeightsNORTH(int index)
        {
            controller.LoadTerrainHeightsNORTH(index);
        }

        public static void LoadHeightsSOUTH(int i)
        {
            controller.LoadTerrainHeightsSOUTH(i);
        }

        public static void LoadHeightsEAST(int i)
        {
            controller.LoadTerrainHeightsEAST(i);
        }

        public static void LoadHeightsWEST(int i)
        {
            controller.LoadTerrainHeightsWEST(i);
        }

        [RuntimeAsync(nameof(SmoothNORTH))]
        public static void SmoothNORTH(int index)
        {
            try
            {
                for (int x = 0; x < (int)NearMetrics.cellCountEdge; x++)
                {
                    int indx = TerrainInfiniteController.northIndex + x;

                    CellMetrics metrics;
                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == indx && x.terrainGridName == s_terrainGridName);

                        Trace.Assert(
                            metrics != null,
                            "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.",
                            metrics.slippyTileName);

                        var fileNameTerrain = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                        s_tiffDataDynamic[indx] = TiffDataDynamic(fileNameTerrain, indx);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(SmoothSOUTH))]
        public static void SmoothSOUTH()
        {
            try
            {
                for (int x = 0; x < (int)NearMetrics.cellCountEdge; x++)
                {
                    int indx = TerrainInfiniteController.southIndex + x;

                    CellMetrics metrics;
                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == indx && x.terrainGridName == s_terrainGridName);
                        Trace.Assert(metrics != null,
                            "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.",
                            metrics.slippyTileName);

                        var fileNameTerrain = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                        s_tiffDataDynamic[indx] = TiffDataDynamic(fileNameTerrain, indx);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(SmoothEAST))]
        public static void SmoothEAST()
        {
            try
            {
                for (int x = 0; x < (int)NearMetrics.cellCountEdge; x++)
                {
                    int indx = TerrainInfiniteController.eastIndex + (x * NearMetrics.cellCountEdge);

                    CellMetrics metrics;
                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == indx && x.terrainGridName == s_terrainGridName);
                        Trace.Assert(metrics != null,
                            "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.",
                            metrics.slippyTileName);

                        var fileNameTerrain = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                        s_tiffDataDynamic[indx] = TiffDataDynamic(fileNameTerrain, indx);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(SmoothWEST))]
        public static void SmoothWEST()
        {
            try
            {
                for (int x = 0; x < (int)NearMetrics.cellCountEdge; x++)
                {
                    int indx = TerrainInfiniteController.westIndex + (x * NearMetrics.cellCountEdge);

                    CellMetrics metrics;
                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == indx && x.terrainGridName == s_terrainGridName);

                        Trace.Assert(metrics != null,
                            "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.",
                            metrics.slippyTileName);

                        var fileNameTerrain = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                        s_tiffDataDynamic[indx] = TiffDataDynamic(fileNameTerrain, indx);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(SmoothAllHeights))]
        public static void SmoothAllHeights()
        {
            Abortable abortable = new Abortable("TerrainRuntime.SmoothAllHeights");

            try
            {
                List<CellMetrics> metricsFiltered;
                lock (s_datalock)
                {
                    metricsFiltered = NearMetrics.cellMetrics.Where(x => x.key != -1 && x.terrainGridName == s_terrainGridName).ToList();
                }
                
                foreach (CellMetrics metrics in metricsFiltered)
                {
                    if (abortable.shouldAbort)
                    {
                        return;
                    }

                    string filePath = LOCALCACHE_ELEVATIONS_FOLDER + metrics.slippyTileName;
                    if (File.Exists(filePath))
                    {
                        lock (s_datalock)
                        {
                            if (abortable.shouldAbort)
                            {
                                return;
                            }

                            var tiffdata = TiffDataDynamic(filePath, metrics.key);
                            s_tiffDataDynamic[metrics.key] = tiffdata;
                        }
                    }
                }

                lock (s_datalock)
                {
                    s_highestPoint = s_highestPoints.Max();
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(SmoothFarTerrain))]
        public static void SmoothFarTerrain()
        {
            try
            {
                if (FarMetrics != null)
                {
                    string filePath = LOCALCACHE_ELEVATIONS_FOLDER + FarMetrics.slippyTileName;
                    if (File.Exists(filePath))
                    {
                        lock (s_datalock)
                        {
                            s_heightMapFarTerrainImageDetail.data = TiffDataDynamicFAR(filePath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        [RuntimeAsync(nameof(DownloadImageData))]
        public static void DownloadImageData(int i, string urlAddress)
        {
            using (s_webClientImage = new WebClient())
            {
                //Uri URL = urlAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("http://" + urlAddress);
                Uri URL = new Uri(urlAddress);

                try
                {
                    byte[] bytes = s_webClientImage.DownloadData(URL);
                    lock (s_datalock)
                    {
                        s_baseImageBytes[i] = bytes;
                    }
                }
                catch (Exception e)
                {
                    Trace.Exception(e);
                    return;
                }
            }
        }

        public static IEnumerator<float> FillImage(int index)
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            s_baseImageTextures[index].LoadImage(s_baseImageBytes[index]);

            lock (s_datalock)
            {
                if (s_processedImageIndex == NearMetrics.cellCount)
                {
                    controller.UpdateState(TerrainController.TerrainState.TexturesGenerated);

                    if (controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
                    {
                        if (controller.farTerrain)
                        {
                            if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                            {
                                if (Abortable.ShouldAbortRoutine())
                                {
                                    yield break;
                                }

                                TerrainController.RunCoroutine(WorldIsGenerated(),
                                    "FillImage(i: {0}) FarTerrainsGenerated,",
                                    index);
                            }
                        }
                        else
                        {
                            TerrainController.RunCoroutine(WorldIsGenerated(),
                                "FillImage(i: {0})", index);
                        }
                    }
                }
            }
            yield return 0;
        }

        public static IEnumerator<float> FillImageFAR()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            s_baseImageTextureFar.LoadImage(s_baseImageBytesFar);

            Interlocked.Increment(ref s_downloadedFarTerrainImages);

            yield return 0;
        }

        public static IEnumerator<float> FillImageDirection(int index)
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            s_baseImageTextures[index].LoadImage(s_baseImageBytes[index]);

            yield return 0;
        }

        public static IEnumerator<float> FillImages(int length)
        {
            for (int z = 0; z < length; z++)
            {
                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }

                if (controller.spiralGeneration)
                {
                    s_baseImageTextures[s_spiralIndex[z]].LoadImage(s_baseImageBytes[s_spiralIndex[z]]);

                    //satImage = Image.FromFile(tempFolder + "SatelliteImage" + (z + 1).ToString() + ".jpg");
                    //SaveSatelliteImage(s_baseImageBytes[s_spiralIndex[z]], z);
                }
                else
                {
                    s_baseImageTextures[z].LoadImage(s_baseImageBytes[z]);

                    //satImage = Image.FromFile(tempFolder + "SatelliteImage" + (z + 1).ToString() + ".jpg");
                    //SaveSatelliteImage(s_baseImageBytes[z], z);
                }

                yield return Timing.WaitForSeconds(controller.imageryDelay);
            }
        }

        private static void GetImagesInfo()
        {
            s_baseImageTextures = new List<Texture2D>();
            s_baseImageBytes = new Dictionary<int, byte[]>();
            s_processedImageIndex = 0;
            s_baseImagesOK = true;
            s_baseImageWidth = controller.imageResolution;
            s_baseImageHeight = controller.imageResolution;

            for (int i = 0; i < NearMetrics.baseImagesTotal; i++)
            {
                s_baseImageBytes[i] = (new byte[(int)Mathf.Pow(controller.imageResolution, 2)]);

                // Don't use decompressed formats as decompression at runtime causes lags and spikes and lowers the FPS
                s_baseImageTextures.Add(new Texture2D(s_baseImageWidth, s_baseImageHeight, TextureFormat.RGB24, true, true));

                s_baseImageTextures[i].wrapMode = TextureWrapMode.Clamp;
                s_baseImageTextures[i].name = (i + 1).ToString();

                //                byte[] bytes = new byte[s_baseImageWidth * s_baseImageHeight];
                //                bytes = s_baseImageTextures[i].EncodeToJPG(100);
                //                File.WriteAllBytes(tempFolder + "SatelliteImage" + (i + 1).ToString() + ".jpg", bytes);
            }

            if (NearMetrics.cellCount > 1)
            {
                s_multipleTerrainsTiling = true;
                s_baseImagesPerTerrain = (int)((float)NearMetrics.baseImagesTotal / (float)NearMetrics.cellCount);

                //TODO: Check if s_tileGrid = 1 always ==> s_tileGrid = NearMetrics.cellsPerTerrain;
                s_tileGrid = (int)(Mathf.Sqrt((float)s_baseImagesPerTerrain));
            }
            else
            {
                s_multipleTerrainsTiling = false;
                s_tileGrid = (int)(Mathf.Sqrt((float)NearMetrics.baseImagesTotal));
            }

            // Prepare Image for Far Terrain
            if (controller.farTerrain)
            {
                s_baseImageBytesFar = new byte[(int)Mathf.Pow(controller.farTerrainImageResolution, 2)];

                // Don't use decompressed formats as decompression at runtime causes lags and spikes and lowers the FPS
                // Optionally we can use Gamma(Non-Linear) color space for more clear colors
                s_baseImageTextureFar = new Texture2D(controller.farTerrainImageResolution, controller.farTerrainImageResolution, TextureFormat.RGB24, true, true);

                s_baseImageTextureFar.wrapMode = TextureWrapMode.Clamp;
                s_baseImageTextureFar.mipMapBias = -0.5f;
                s_baseImageTextureFar.name = "FarTerrainImage";

                //                byte[] bytes = new byte[controller.farTerrainImageResolution * controller.farTerrainImageResolution];
                //                bytes = s_baseImageTextureFar.EncodeToJPG(100);
                //                File.WriteAllBytes(tempFolder + "FarSatelliteImage.jpg", bytes);
            }
        }

        [RuntimeAsync(nameof(ServerInfoImagery))]
        public static WebExceptionStatus ServerInfoImagery(string value = "")
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImagery");

            try
            {
                string defaultMapName = s_mapserviceImagery.GetDefaultMapName();
                TerraLandWorldImagery.TileImageInfo tileImageInfo = s_mapserviceImagery.GetTileImageInfo(defaultMapName);
                tileImageInfo.CompressionQuality = s_compressionQuality;
                CellMetrics metrics;

                if (abortable.shouldAbort)
                {
                    return WebExceptionStatus.RequestCanceled;
                }

                for (int i = 0; i < NearMetrics.baseImagesTotal; i++)
                {
                    if (abortable.shouldAbort)
                    {
                        return WebExceptionStatus.RequestCanceled;
                    }

                    lock (s_datalock)
                    {
                        metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
                    }
                    Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

                    controller.ServerConnectImagery(i, metrics.slippyTileName);
                }
            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e, "Exception exporting ServerConnectImagery. DETAILS:");
                return WebExceptionStatus.UnknownError;
            }

            return WebExceptionStatus.Success;
        }

        [RuntimeAsync(nameof(ServerInfoImageryFAR))]
        public static WebExceptionStatus ServerInfoImageryFAR()
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImagery");

            try
            {
                if (abortable.shouldAbort)
                {
                    return WebExceptionStatus.RequestCanceled;
                }

                string defaultMapName = s_mapserviceImagery.GetDefaultMapName();
                TerraLandWorldImagery.TileImageInfo tileImageInfo = s_mapserviceImagery.GetTileImageInfo(defaultMapName);
                tileImageInfo.CompressionQuality = s_compressionQuality;

                controller.ServerConnectImageryFAR();

            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e, "Exception exporting ServerConnectImagery #{0}. DETAILS:");
                return WebExceptionStatus.UnknownError;
            }

            return WebExceptionStatus.Success;
        }

        [RuntimeAsync(nameof(ServerInfoImageryNORTH))]
        public static void ServerInfoImageryNORTH()
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImageryNORTH");

            for (int x = 0; x < NearMetrics.cellCountEdge; x++)
            {
                if (abortable.shouldAbort)
                {
                    return;
                }

                int i = TerrainInfiniteController.s_northIndexImagery[x];
                int j = TerrainInfiniteController.northIndex + s_terrainGridCount;

                CellMetrics metrics;

                lock (s_datalock)
                {
                    metrics = new CellMetrics()
                    {
                        key = i,
                        mercatorBounds = new Bounds2D(NearMetrics.cellMetrics[j].mercatorBounds),
                        terrainGridName = s_terrainGridName
                    };
                }

                var slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(metrics, s_directionCoordinates);
                if (slippyTileName != "")
                {
                    metrics.slippyTileName = slippyTileName;
                    s_directionCoordinates.Add(metrics);
                    controller.ServerConnectImageryDirection(i, slippyTileName, "NORTH");
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoImagerySOUTH))]
        public static void ServerInfoImagerySOUTH()
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImagerySOUTH");

            for (int x = 0; x < NearMetrics.cellCountEdge; x++)
            {
                if (abortable.shouldAbort)
                {
                    return;
                }

                int i = TerrainInfiniteController.s_southIndexImagery[x];
                int j = TerrainInfiniteController.southIndex + s_terrainGridCount;

                CellMetrics metrics;

                lock (s_datalock)
                {
                    metrics = new CellMetrics()
                    {
                        key = i,
                        mercatorBounds = new Bounds2D(NearMetrics.cellMetrics[j].mercatorBounds),
                        terrainGridName = s_terrainGridName
                    };
                }
                var slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(metrics, s_directionCoordinates);
                if (slippyTileName != "")
                {
                    metrics.slippyTileName = slippyTileName;
                    s_directionCoordinates.Add(metrics);
                    controller.ServerConnectImageryDirection(i, slippyTileName, "SOUTH");
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoImageryEAST))]
        public static void ServerInfoImageryEAST()
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImageryEAST");

            for (int x = 0; x < NearMetrics.cellCountEdge; x++)
            {
                if (abortable.shouldAbort)
                {
                    return;
                }

                int i = TerrainInfiniteController.s_eastIndexImagery[x];
                int j = TerrainInfiniteController.eastIndex + (x * NearMetrics.cellCountEdge) + s_terrainGridCount;

                CellMetrics metrics = new CellMetrics()
                {
                    key = i,
                    mercatorBounds = new Bounds2D(NearMetrics.cellMetrics[j].mercatorBounds),
                    terrainGridName = s_terrainGridName
                };
                var slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(metrics, s_directionCoordinates);
                if (slippyTileName != "")
                {
                    metrics.slippyTileName = slippyTileName;
                    s_directionCoordinates.Add(metrics);
                    controller.ServerConnectImageryDirection(i, slippyTileName, "EAST");
                    Thread.Sleep(500);
                }
            }
        }

        [RuntimeAsync(nameof(ServerInfoImageryWEST))]
        public static void ServerInfoImageryWEST()
        {
            Abortable abortable = new Abortable("TerrainRuntime.ServerInfoImageryEAST");

            for (int x = 0; x < NearMetrics.cellCountEdge; x++)
            {
                if (abortable.shouldAbort)
                {
                    return;
                }

                int i = TerrainInfiniteController.s_westIndexImagery[x];
                int j = TerrainInfiniteController.westIndex + (x * NearMetrics.cellCountEdge) + s_terrainGridCount;

                CellMetrics metrics = new CellMetrics()
                {
                    key = i,
                    mercatorBounds = new Bounds2D(NearMetrics.cellMetrics[j].mercatorBounds),
                    terrainGridName = s_terrainGridName
                };
                var slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(metrics, s_directionCoordinates);
                if (slippyTileName != "")
                {
                    metrics.slippyTileName = slippyTileName;
                    s_directionCoordinates.Add(metrics);
                    controller.ServerConnectImageryDirection(i, slippyTileName, "WEST");
                    Thread.Sleep(500);
                }

            }
        }

        [RuntimeAsync(nameof(ImageDownloader))]
        public static WebExceptionStatus ImageDownloader(string fileName)
        {
            Abortable abortable = new Abortable("TerrainRuntime.ImageDownloader");

            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            if (!s_allBlack)
            {
                CellMetrics metrics;
                lock (s_datalock)
                {
                    metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.slippyTileName == fileName && x.terrainGridName == s_terrainGridName);
                }
                Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", fileName);

                try
                {
                    string slippyTileFilePath = LOCALCACHE_BASEIMAGES_FOLDER + metrics.slippyTileName;
                    if (!File.Exists(slippyTileFilePath))
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }

                        try
                        {
                            string defaultMapName = s_mapserviceImagery.GetDefaultMapName();
                            s_mapinfo = s_mapserviceImagery.GetServerInfo(defaultMapName);
                        }
                        catch (WebException e)
                        {
                            return e.Status;
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                            return WebExceptionStatus.UnknownError;
                        }
                        s_mapdesc = s_mapinfo.DefaultMapDescription;
                        TerraLandWorldImagery.EnvelopeN extent = new TerraLandWorldImagery.EnvelopeN();
                        extent.XMin = metrics.mercatorBounds.left;
                        extent.YMin = metrics.mercatorBounds.bottom;
                        extent.XMax = metrics.mercatorBounds.right;
                        extent.YMax = metrics.mercatorBounds.top;
                        s_mapdesc.MapArea.Extent = extent;

                        TerraLandWorldImagery.ImageType imgtype = new TerraLandWorldImagery.ImageType();
                        imgtype.ImageFormat = TerraLandWorldImagery.esriImageFormat.esriImageJPG;
                        imgtype.ImageReturnType = TerraLandWorldImagery.esriImageReturnType.esriImageReturnMimeData;

                        TerraLandWorldImagery.ImageDisplay imgdisp = new TerraLandWorldImagery.ImageDisplay();
                        imgdisp.ImageHeight = controller.imageResolution;
                        imgdisp.ImageWidth = controller.imageResolution;
                        imgdisp.ImageDPI = 96;

                        TerraLandWorldImagery.ImageDescription imgdesc = new TerraLandWorldImagery.ImageDescription();
                        imgdesc.ImageDisplay = imgdisp;
                        imgdesc.ImageType = imgtype;
                        TerraLandWorldImagery.MapImage mapimg = s_mapserviceImagery.ExportMapImage(s_mapdesc, imgdesc);

                        using (var ms = new MemoryStream(mapimg.ImageData))
                        {
                            using (var fs = new FileStream(slippyTileFilePath, FileMode.Create))
                            {
                                ms.WriteTo(fs);
                            }
                        }

                        s_baseImageBytes[metrics.key] = FileToByteArray(slippyTileFilePath, metrics);
                    }
                    else
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }
                        s_baseImageBytes[metrics.key] = FileToByteArray(slippyTileFilePath, metrics);
                    }
                    if (!s_availableImageryCheked)
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }
                        CheckImageColors(metrics.key);
                    }
                }
                catch (WebException e)
                {
                    return e.Status;
                }
                catch (Exception e)
                {
                    Trace.Exception(e,
                        "Exception exporting ImageDownloader #{0}. DETAILS:",
                        metrics.key);
                    return WebExceptionStatus.UnknownError;
                }
                finally
                {
                    Interlocked.Increment(ref s_processedImageIndex);
                }

            }
            return WebExceptionStatus.Success;
        }

        [RuntimeAsync(nameof(ImageDownloaderDirection))]
        public static WebExceptionStatus ImageDownloaderDirection(string fileName)
        {
            Abortable abortable = new Abortable("TerrainRuntime.ImageDownloaderDirection");
            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            if (!s_allBlack)
            {
                CellMetrics metrics;

                lock (s_datalock)
                {
                    metrics = s_directionCoordinates.FirstOrDefault(x => x.slippyTileName == fileName && x.terrainGridName == s_terrainGridName);
                }
                Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", fileName);

                try
                {
                    string slippyTileFilePath = LOCALCACHE_BASEIMAGES_FOLDER + metrics.slippyTileName;
                    if (!File.Exists(slippyTileFilePath))
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }

                        try
                        {
                            s_mapinfo = s_mapserviceImagery.GetServerInfo(s_mapserviceImagery.GetDefaultMapName());
                        }
                        catch (WebException e)
                        {
                            return e.Status;
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                            return WebExceptionStatus.UnknownError;
                        }
                        s_mapdesc = s_mapinfo.DefaultMapDescription;
                        TerraLandWorldImagery.EnvelopeN extent = new TerraLandWorldImagery.EnvelopeN();
                        extent.XMin = metrics.mercatorBounds.left;
                        extent.YMin = metrics.mercatorBounds.bottom;
                        extent.XMax = metrics.mercatorBounds.right;
                        extent.YMax = metrics.mercatorBounds.top;
                        s_mapdesc.MapArea.Extent = extent;

                        TerraLandWorldImagery.ImageType imgtype = new TerraLandWorldImagery.ImageType();
                        imgtype.ImageFormat = TerraLandWorldImagery.esriImageFormat.esriImageJPG;
                        imgtype.ImageReturnType = TerraLandWorldImagery.esriImageReturnType.esriImageReturnMimeData;

                        TerraLandWorldImagery.ImageDisplay imgdisp = new TerraLandWorldImagery.ImageDisplay();
                        imgdisp.ImageHeight = controller.imageResolution;
                        imgdisp.ImageWidth = controller.imageResolution;
                        imgdisp.ImageDPI = 96;

                        TerraLandWorldImagery.ImageDescription imgdesc = new TerraLandWorldImagery.ImageDescription();
                        imgdesc.ImageDisplay = imgdisp;
                        imgdesc.ImageType = imgtype;

                        TerraLandWorldImagery.MapImage mapimg = s_mapserviceImagery.ExportMapImage(s_mapdesc, imgdesc);

                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }

                        using (var ms = new MemoryStream(mapimg.ImageData))
                        {
                            using (var fs = new FileStream(slippyTileFilePath, FileMode.Create))
                            {
                                ms.WriteTo(fs);
                            }
                        }
                        s_baseImageBytes[metrics.key] = FileToByteArray(slippyTileFilePath, metrics);
                    }
                    else
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }

                        s_baseImageBytes[metrics.key] = FileToByteArray(slippyTileFilePath, metrics);
                    }

                    if (!s_availableImageryCheked)
                    {
                        if (abortable.shouldAbort)
                        {
                            return WebExceptionStatus.RequestCanceled;
                        }

                        CheckImageColors(metrics.key);
                    }
                }
                catch (WebException e)
                {
                    return e.Status;
                }
                catch (Exception e)
                {
                    Trace.Exception(e,
                        "Exception exporting ImageDownloader #{0}. DETAILS:",
                        metrics.key);
                    return WebExceptionStatus.UnknownError;
                }
                finally
                {
                    Interlocked.Increment(ref s_processedImageIndex);
                }

            }
            return WebExceptionStatus.Success;
        }

        public static byte[] FileToByteArray(string fileName, CellMetrics metrics)
        {
            byte[] fileData = null;

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    fileData = binaryReader.ReadBytes((int)fileStream.Length);
                }
            }


            return fileData;
        }

        [RuntimeAsync(nameof(ImageDownloaderFAR))]
        public static WebExceptionStatus ImageDownloaderFAR()
        {
            try
            {
                double XMin = s_farImageCorner.left * 20037508.34 / 180.0;
                double yMaxTopFar = Math.Log(Math.Tan((90.0 + s_farImageCorner.top) * Math.PI / 360.0)) / (Math.PI / 180.0);
                double YMax = yMaxTopFar * 20037508.34 / 180.0;
                double XMax = s_farImageCorner.right * 20037508.34 / 180.0;
                double yMinBottomFar = Math.Log(Math.Tan((90.0 + s_farImageCorner.bottom) * Math.PI / 360.0)) / (Math.PI / 180.0);
                double YMin = yMinBottomFar * 20037508.34 / 180.0;

                if (s_farCellMetrics == null)
                {
                    Bounds2D mercator = new Bounds2D()
                    {
                        top = YMax,
                        bottom = YMin,
                        left = XMin,
                        right = XMax,
                    };

                    s_farCellMetrics = new CellMetrics()
                    {
                        key = -1,
                        mercatorBounds = mercator,
                        wgs84Bounds = s_farImageCorner,
                        terrainGridName = s_terrainGridName
                    };
                    s_farCellMetrics.slippyTileName = SlippyTilesHelper.GetSlippyTilesNameByImage(FarMetrics);
                }

                string filePath = LOCALCACHE_BASEIMAGES_FOLDER + FarMetrics.slippyTileName;
                if (!File.Exists(filePath))
                {
                    s_mapinfo = s_mapserviceImagery.GetServerInfo(s_mapserviceImagery.GetDefaultMapName());
                    s_mapdesc = s_mapinfo.DefaultMapDescription;

                    TerraLandWorldImagery.EnvelopeN extent = new TerraLandWorldImagery.EnvelopeN();

                    extent.XMin = XMin;
                    extent.YMax = YMax;
                    extent.XMax = XMax;
                    extent.YMin = YMin;
                    s_mapdesc.MapArea.Extent = extent;

                    TerraLandWorldImagery.ImageType imgtype = new TerraLandWorldImagery.ImageType();
                    imgtype.ImageFormat = TerraLandWorldImagery.esriImageFormat.esriImageJPG;
                    imgtype.ImageReturnType = TerraLandWorldImagery.esriImageReturnType.esriImageReturnMimeData;

                    TerraLandWorldImagery.ImageDisplay imgdisp = new TerraLandWorldImagery.ImageDisplay();
                    imgdisp.ImageHeight = controller.farTerrainImageResolution;
                    imgdisp.ImageWidth = controller.farTerrainImageResolution;
                    imgdisp.ImageDPI = 96;

                    TerraLandWorldImagery.ImageDescription imgdesc = new TerraLandWorldImagery.ImageDescription();
                    imgdesc.ImageDisplay = imgdisp;
                    imgdesc.ImageType = imgtype;

                    // far terrain coordinates
                    TerraLandWorldImagery.MapImage mapimg = s_mapserviceImagery.ExportMapImage(s_mapdesc, imgdesc);

                    using (var ms = new MemoryStream(mapimg.ImageData))
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                    }
                }
                s_baseImageBytesFar = FileToByteArray(filePath, FarMetrics);
            }
            catch (WebException e)
            {
                return e.Status;
            }
            catch (Exception e)
            {
                Trace.Exception(e, "Exception exporting ImageDownloader #{0}. DETAILS:");
                return WebExceptionStatus.UnknownError;
            }

            return WebExceptionStatus.Success;
        }

        private static void CheckImageColors(int i)
        {
            //MemoryStream ms = new MemoryStream(s_baseImageBytes[i]);
            //Bitmap bmp = new Bitmap(ms);
            //
            //// Lock the bitmap's bits.  
            //Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            //
            //// Get the address of the first line.
            //IntPtr ptr = bmpData.Scan0;
            //
            //// Declare an array to hold the bytes of the bitmap.
            //int bytes  = bmpData.Stride * bmp.Height;
            //byte[] rgbValues = new byte[bytes];
            //
            //// Copy the RGB values into the array.
            //System.TerrainController.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            //
            //s_allBlack = true;
            //
            //// Scanning for non-zero bytes
            //for (int index = 0; index < rgbValues.Length; index++)
            //{
            //    if (rgbValues[index] != 0) 
            //    {
            //        s_allBlack = false;
            //        break;
            //    }
            //}
            //
            //// Unlock the bits.
            //bmp.UnlockBits(bmpData);
            //bmp.Dispose();
            //
            //s_availableImageryCheked = true;
        }

        private static void GenerateToken(bool north, bool south, bool east, bool west, int i)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(TOKEN_URL);

            req.KeepAlive = false;
            req.ProtocolVersion = HttpVersion.Version10;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

            CellMetrics metrics = NearMetrics.cellMetrics.FirstOrDefault(x => x.key == i && x.terrainGridName == s_terrainGridName);
            Trace.Assert(metrics != null, "CellMetrics for {0} MUST exist. NearMetrics.cellMetrics is the collection of all tiles required by the terrain.", metrics.slippyTileName);

            try
            {
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string str = sr.ReadToEnd();
                s_token = str.Replace("{\"access_token\":\"", "").Replace("\",\"expires_in\":1209600}", "");
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static void GenerateNewTerrainObject()
        {
            SetData();

            CreateTerrainObject();

            if (NearMetrics.cellCountEdge == 1)
            {
                s_terrain = s_currentTerrain;
                s_initialTerrainWidth = NearMetrics.spanX_m;
            }
            else
            {
                s_splittedTerrains = s_terrainsParent;

                s_splittedTerrains.AddComponent<TerrainInfiniteController>();

                CheckTerrainChunks();

                s_initialTerrainWidth = NearMetrics.spanX_m / NearMetrics.splitSizeFinal;
            }

            controller.UpdateState(TerrainController.TerrainState.SceneInitialized);

            AddTerrainsToFloatingOrigin();
        }

        private static void AddTerrainsToFloatingOrigin()
        {
            floatingOriginAdvanced = terrainPlayer.FloatingOrigin;
            floatingOriginAdvanced.CollectObjects();
        }

        private static void SetData()
        {
            s_terrainsLong = NearMetrics.cellCountEdge;
            s_terrainsWide = NearMetrics.cellCountEdge;

            s_oldWidth = NearMetrics.spanX_m;
            s_oldHeight = NearMetrics.spanY_m;
            s_oldLength = NearMetrics.spanZ_m;

            s_newWidth = s_oldWidth / s_terrainsWide;
            s_newLength = s_oldLength / s_terrainsLong;

            s_xPos = (NearMetrics.spanX_m / 2f) * -1f;
            s_yPos = 0f;
            s_zPos = (NearMetrics.spanZ_m / 2f) * -1f;

            s_newHeightMapResolution = ((NearMetrics.heightmapResolutionSplit - 1) / NearMetrics.cellCountEdge) + 1;
            s_newEvenHeightMapResolution = s_newHeightMapResolution - 1;
        }

        private static void CreateTerrainObject()
        {
            try
            {
                int arrayPos = 0;
                GameObject tempParent = null;

                if (NearMetrics.cellCountEdge > 1)
                {
                    s_terrainsParent = SceneObject.Create(SceneObject.Mode.Player, ObjectName.NEAR_TERRAIN_CONTAINER);
                    terrainNames = new string[(int)Mathf.Pow(NearMetrics.cellCountEdge, 2)];
                    tempParent = SceneObject.Create(SceneObject.Mode.Player, ObjectName.NEAR_TERRAIN_TEMP);
                }
                s_terrainGameObjects = new List<GameObject>();

                int currentRow = NearMetrics.cellCountEdge;

                for (int y = 0; y < s_terrainsLong; y++)
                {
                    for (int x = 0; x < s_terrainsWide; x++)
                    {
                        GameObject terrainGameObject = SceneObject.Create(
                            SceneObject.Mode.Player,
                            ObjectName.NEAR_TERRAIN_TEMP + "/Terrain_" + (currentRow) + "-" + (x + 1));
                        terrainGameObject.AddComponent<Terrain>();

                        if (NearMetrics.cellCountEdge > 1)
                        {
                            terrainNames[arrayPos] = terrainGameObject.name;
                        }

                        s_terrainData = new TerrainData();
                        s_terrainData.heightmapResolution = s_newEvenHeightMapResolution;
                        s_terrainData.size = new Vector3(s_newWidth, s_oldHeight, s_newLength);
                        s_terrainData.name = currentRow + "-" + (x + 1);
                        s_terrainData.alphamapResolution = 16;

                        s_currentTerrain = terrainGameObject.GetComponent<Terrain>();
                        s_currentTerrain.terrainData = s_terrainData;
                        s_currentTerrain.heightmapPixelError = controller.heightmapPixelError;
                        s_currentTerrain.basemapDistance = NearMetrics.spanX_m * 4f;

#if !UNITY_2019_1_OR_NEWER
                    s_currentTerrain.materialType = Terrain.MaterialType.Custom;
#endif
                        s_currentTerrain.materialTemplate = TerraLand.MaterialManager.GetTerrainMaterial();

                        s_currentTerrain.materialTemplate.renderQueue = 1900;
                        //s_currentTerrain.materialTemplate.renderQueue = -1;
                        //if (s_currentTerrain.materialTemplate.HasProperty("_MeshDistance")) s_currentTerrain.materialTemplate.SetFloat("_MeshDistance", controller.terrainDistance);

                        if (s_currentTerrain.materialTemplate.HasProperty("_MeshDistance")) s_currentTerrain.materialTemplate.SetFloat("_MeshDistance", 0f);
                        if (s_currentTerrain.materialTemplate.HasProperty("_Curvature")) s_currentTerrain.materialTemplate.SetFloat("_Curvature", controller.terrainCurvator);
                        if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness0")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness0", 0f);
                        if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness1")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness1", 0f);
                        if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness2")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness2", 0f);
                        if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness3")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness3", 0f);

                        if (controller.showTileOnFinish)
                        {
                            s_currentTerrain.drawHeightmap = false;
                        }

#if UNITY_2018_3_OR_NEWER
                        s_currentTerrain.drawInstanced = true;
                        s_currentTerrain.groupingID = 0;
                        s_currentTerrain.allowAutoConnect = true;
#endif

                        terrainGameObject.AddComponent<TerrainCollider>();
                        terrainGameObject.GetComponent<TerrainCollider>().terrainData = s_terrainData;

                        terrainGameObject.transform.position = new Vector3(x * s_newWidth + s_xPos, s_yPos, y * s_newLength + s_zPos);

                        terrainGameObject.layer = TERRAIN_LAYER;

                        s_terrainGameObjects.Add(terrainGameObject);
                        arrayPos++;
                    }
                    currentRow--;
                }

                if (NearMetrics.cellCountEdge > 1)
                {
                    terrainNames = LogicalComparer(terrainNames);

                    for (int i = 0; i < terrainNames.Length; i++)
                    {
                        tempParent.transform.Find(terrainNames[i]).transform.parent = s_terrainsParent.transform;
                        s_terrainsParent.transform.Find(terrainNames[i]).name = (i + 1).ToString() + " " + terrainNames[i];
                    }

                    s_spiralCell = new List<Vector2>();
                    DestroyImmediate(tempParent);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool SampleHeightAtPosition(Vector3 worldPosition, out float height)
        {
            height = float.MaxValue;

            if (s_splittedTerrains)
            {
                int iCell = NearMetrics.GetBoundingCellIndex(worldPosition);

                if ((iCell >= 0) && 
                    (iCell <= s_croppedTerrains.Count - 1) &&
                    (NearMetrics.cellMetrics.Count == s_croppedTerrains.Count()) &&
                    (s_croppedTerrains[iCell] != null))
                {
                    height = s_croppedTerrains[iCell].SampleHeight(worldPosition);
                    return true;
                }
            }
            else if (s_terrain)
            {
                height = s_terrain.SampleHeight(worldPosition);
            }

            return false;
        }


        private static void CreateFarTerrainObject()
        {
            s_farTerrainsParent = SceneObject.Create(SceneObject.Mode.Player, ObjectName.FAR_TERRAIN_CONTAINER);

            s_farTerrainGameObjects = new List<GameObject>();

            s_farTerrainSize = controller.areaSize * controller.areaSizeFarMultiplier * 1000f;

            for (int i = 1; i <= 2; i++)
            {
                GameObject terrainGameObject = SceneObject.Create(
                    SceneObject.Mode.Player,
                    ObjectName.FAR_TERRAIN_CONTAINER + "/" + i.ToString());

                terrainGameObject.AddComponent<Terrain>();

                s_terrainData = new TerrainData();
                s_terrainData.heightmapResolution = controller.farTerrainHeightmapResolution + 1;
                s_terrainData.size = new Vector3(s_farTerrainSize, s_oldHeight, s_farTerrainSize);
                s_terrainData.name = "Far Terrain " + i.ToString();
                s_terrainData.alphamapResolution = 16;

                s_currentTerrain = terrainGameObject.GetComponent<Terrain>();
                s_currentTerrain.terrainData = s_terrainData;
                s_currentTerrain.heightmapPixelError = controller.farTerrainQuality;
                s_currentTerrain.basemapDistance = s_farTerrainSize * 4f;
                //s_currentTerrain.basemapDistance = NearMetrics.spanX_m / 4f;

#if !UNITY_2019_1_OR_NEWER
                s_currentTerrain.materialType = Terrain.MaterialType.Custom;
#endif
                s_currentTerrain.materialTemplate = TerraLand.MaterialManager.GetTerrainMaterial();

                s_currentTerrain.materialTemplate.renderQueue = 1899;
                //s_currentTerrain.materialTemplate.renderQueue = -1;
                //if (s_currentTerrain.materialTemplate.HasProperty("_MeshDistance")) s_currentTerrain.materialTemplate.SetFloat("_MeshDistance", controller.terrainDistance * controller.areaSizeFarMultiplier);

                if (s_currentTerrain.materialTemplate.HasProperty("_MeshDistance")) s_currentTerrain.materialTemplate.SetFloat("_MeshDistance", controller.terrainDistance);
                if (s_currentTerrain.materialTemplate.HasProperty("_Curvature")) s_currentTerrain.materialTemplate.SetFloat("_Curvature", controller.terrainCurvator / controller.areaSizeFarMultiplier);
                if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness0")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness0", 0f);
                if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness1")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness1", 0f);
                if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness2")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness2", 0f);
                if (s_currentTerrain.materialTemplate.HasProperty("_Smoothness3")) s_currentTerrain.materialTemplate.SetFloat("_Smoothness3", 0f);

                s_currentTerrain.drawHeightmap = false;
                s_currentTerrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

#if UNITY_2018_3_OR_NEWER
                s_currentTerrain.drawInstanced = true;
#endif

                terrainGameObject.AddComponent<TerrainCollider>();
                terrainGameObject.GetComponent<TerrainCollider>().terrainData = s_terrainData;
                terrainGameObject.GetComponent<TerrainCollider>().enabled = false;

                terrainGameObject.transform.position = new Vector3
                    (
                        -(s_farTerrainSize / 2f),
                        -controller.farTerrainBelowHeight,
                        -(s_farTerrainSize / 2f)
                    );

                terrainGameObject.layer = 8;

                s_farTerrainGameObjects.Add(terrainGameObject);

                if (i == 1)
                {
                    s_firstTerrain = s_currentTerrain;
                }
                else if (i == 2)
                {
                    s_secondaryTerrain = s_currentTerrain;
                }
            }

            s_terrain = s_firstTerrain;
            s_secondaryTerrainInProgress = true;

            s_farTerrainDummy = SceneObject.Create(
                SceneObject.Mode.Player,
                ObjectName.FAR_TERRAIN_TEMP);

            if (s_farTerrainsParent != null)
            {
                s_farTerrainDummy.transform.parent = s_farTerrainsParent.transform;
            }
            s_farTerrainGameObjects.Add(s_farTerrainDummy);

            s_farTerrainDummy.transform.position = s_terrain.transform.position;
        }

        private static string[] LogicalComparer(string[] names)
        {
            ns.NumericComparer ns = new ns.NumericComparer();
            Array.Sort(names, ns);

            return names;
        }

        private static void CheckTerrainChunks()
        {
            if (s_splittedTerrains.transform.childCount > 0)
            {
                int counter = 0;

                foreach (Transform t in s_splittedTerrains.transform)
                {
                    if (t.GetComponent<Terrain>() != null)
                    {
                        if (counter == 0)
                        {
                            s_croppedTerrains = new List<Terrain>();
                        }

                        s_croppedTerrains.Add(t.GetComponent<Terrain>());
                        counter++;
                    }
                }
                NearMetrics.cellCount = counter;
            }
        }

        private static void RemoveLightmapStatic()
        {
#if UNITY_EDITOR
#if UNITY_2019_2_OR_NEWER
            if (s_splittedTerrains)
            {
                foreach (Terrain t in s_croppedTerrains)
                {
                    UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    flags = flags & ~(UnityEditor.StaticEditorFlags.ContributeGI);
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }
            }
            else if (s_terrain)
            {
                UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(s_terrain.gameObject);
                flags = flags & ~(UnityEditor.StaticEditorFlags.ContributeGI);
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(s_terrain.gameObject, flags);
            }
#else
            if (s_splittedTerrains)
            {
                foreach (Terrain t in s_croppedTerrains)
                {
                    UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    flags = flags & ~(UnityEditor.StaticEditorFlags.LightmapStatic);
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }
            }
            else if (s_terrain)
            {
                UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(s_terrain.gameObject);
                flags = flags & ~(UnityEditor.StaticEditorFlags.LightmapStatic);
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(s_terrain.gameObject, flags);
            }
#endif
#endif
        }

        [RuntimeAsync(nameof(SmoothHeights))]
        public static void SmoothHeights(float[,] terrainData, int width, int height)
        {
            if (controller.smoothIterations > 0)
            {
                FinalizeSmooth(terrainData, width, height, controller.smoothIterations, s_smoothBlendIndex, s_smoothBlend);
            }

            CalculateResampleHeightmaps();
        }

        public static void FinalizeHeights()
        {
            TerrainController.RunCoroutine(LoadTerrainHeightsFromTIFF(),
                "FinalizeHeights()");

            if (s_generatedTerrainsCount == NearMetrics.cellCount)
            {
                if (s_splittedTerrains)
                {
                    ManageNeighborings();
                }
            }
        }

        [RuntimeAsync(nameof(GetElevationFileInfo))]
        public static void GetElevationFileInfo()
        {
            if (s_geoDataExtensionElevation.Equals("raw"))
            {
                GetRAWInfo();
            }
            else if (s_geoDataExtensionElevation.Equals("tif"))
            {
                GetTIFFInfo();
            }
            else if (s_geoDataExtensionElevation.Equals("asc"))
            {
                GetASCIIInfo();
            }
        }

        public static void ApplyOfflineTerrain()
        {
            if (s_geoDataExtensionElevation.Equals("raw"))
            {
                controller.heightmapResolution = m_Width;
            }
            else if (s_geoDataExtensionElevation.Equals("tif"))
            {
                controller.heightmapResolution = s_heightMapTiff.width;
            }
            else if (s_geoDataExtensionElevation.Equals("asc"))
            {
                controller.heightmapResolution = s_nRows;
            }

            if (s_geoDataExtensionElevation.Equals("raw"))
            {
                controller.TerrainFromRAW();
            }
            else if (s_geoDataExtensionElevation.Equals("tif"))
            {
                controller.TerrainFromTIFF();
            }
            else if (s_geoDataExtensionElevation.Equals("asc"))
            {
                controller.TerrainFromASCII();
            }
        }

        [RuntimeAsync(nameof(TiffData))]
        public static void TiffData(string fileName)
        {
            try
            {
                using (Tiff inputImage = Tiff.Open(fileName, "r"))
                {
                    s_heightMapTiff.width = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    s_heightMapTiff.length = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    s_heightMapTiff.data = new float[s_heightMapTiff.length, s_heightMapTiff.width];
                    s_heightMapTiff.dataASCII = new float[s_heightMapTiff.length, s_heightMapTiff.width];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    heightmapResXAll = s_heightMapTiff.width;
                    heightmapResYAll = s_heightMapTiff.length;

                    for (int y = 0; y < s_heightMapTiff.length; y += tileHeight)
                    {
                        for (int x = 0; x < s_heightMapTiff.width; x += tileWidth)
                        {
                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                            {
                                for (int j = 0; j < tileWidth; j++)
                                {
                                    if ((y + i) < s_heightMapTiff.length && (x + j) < s_heightMapTiff.width)
                                    {
                                        s_heightMapTiff.dataASCII[y + i, x + j] = fBuffer[i, j];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            s_highestPoint = s_heightMapTiff.dataASCII.Cast<float>().Max();
            s_lowestPoint = s_heightMapTiff.dataASCII.Cast<float>().Min();

            // Rotate terrain heights and normalize values
            for (int y = 0; y < s_heightMapTiff.width; y++)
            {
                for (int x = 0; x < s_heightMapTiff.length; x++)
                {
                    s_currentHeight = s_heightMapTiff.dataASCII[(s_heightMapTiff.width - 1) - y, x];

                    try
                    {
                        if (s_lowestPoint >= 0)
                        {
                            s_heightMapTiff.data[y, x] = (s_currentHeight - s_lowestPoint) / GeoConst.EVEREST_PEAK_METERS;
                        }
                        else
                        {
                            s_heightMapTiff.data[y, x] = (s_currentHeight + Mathf.Abs(s_lowestPoint)) / GeoConst.EVEREST_PEAK_METERS;
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Trace.Exception(e);
                        s_heightMapTiff.data[y, x] = 0f;
                    }

                    // Check Terrain Corners
                    // Top Row
                    if (y == 0)
                    {
                        s_topCorner.Add(s_currentHeight);
                    }
                    // Bottom Row
                    else if (y == s_heightMapTiff.width - 1)
                    {
                        s_bottomCorner.Add(s_currentHeight);
                    }

                    // Left Column
                    if (x == 0)
                    {
                        s_leftCorner.Add(s_currentHeight);
                    }
                    // Right Column
                    else if (x == s_heightMapTiff.length - 1)
                    {
                        s_rightCorner.Add(s_currentHeight);
                    }
                }
            }

            CheckCornersTIFF();
        }

        [RuntimeAsync(nameof(TiffDataDynamic))]
        public static float[,] TiffDataDynamic(string fileName, int index)
        {
            Abortable abortable = new Abortable("TerrainRuntime.TiffDataDynamic");
            if (abortable.shouldAbort)
            {
                return null;
            }

            try
            {
                using (Tiff inputImage = Tiff.Open(fileName, "r"))
                {
                    s_heightMapTiff.width = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    s_heightMapTiff.length = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    s_heightMapTiff.data = new float[s_heightMapTiff.length, s_heightMapTiff.width];
                    s_heightMapTiff.dataASCII = new float[s_heightMapTiff.length, s_heightMapTiff.width];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    heightmapResXAll = s_heightMapTiff.width;
                    heightmapResYAll = s_heightMapTiff.length;

                    for (int y = 0; y < s_heightMapTiff.length; y += tileHeight)
                    {
                        for (int x = 0; x < s_heightMapTiff.width; x += tileWidth)
                        {
                            if (abortable.shouldAbort)
                            {
                                return null;
                            }

                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                            {
                                for (int j = 0; j < tileWidth; j++)
                                {
                                    if (abortable.shouldAbort)
                                    {
                                        return null;
                                    }

                                    if ((y + i) < s_heightMapTiff.length && (x + j) < s_heightMapTiff.width)
                                    {
                                        s_heightMapTiff.dataASCII[y + i, x + j] = fBuffer[i, j];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            if (abortable.shouldAbort)
            {
                return null;
            }

            lock (s_datalock)
            {
                if (s_processedHeightmapIndex == 0)
                {
                    s_lowestPoint = s_heightMapTiff.dataASCII.Cast<float>().Min();
                }
            }

            if (!controller.IsInState(TerrainController.TerrainState.WorldIsGenerated))
            {
                s_highestPoints.Add(s_heightMapTiff.dataASCII.Cast<float>().Max());
            }
            else
            {
                s_highestPoints[index] = s_heightMapTiff.dataASCII.Cast<float>().Max();
            }

            // Rotate terrain heights and normalize values
            for (int y = 0; y < s_heightMapTiff.width; y++)
            {
                for (int x = 0; x < s_heightMapTiff.length; x++)
                {
                    if (abortable.shouldAbort)
                    {
                        return null;
                    }

                    s_currentHeight = s_heightMapTiff.dataASCII[(s_heightMapTiff.width - 1) - y, x];

                    try
                    {
                        if (s_lowestPoint >= 0)
                        {
                            s_heightMapTiff.data[y, x] = (s_currentHeight - s_lowestPoint) / GeoConst.EVEREST_PEAK_METERS;
                        }
                        else
                        {
                            s_heightMapTiff.data[y, x] = (s_currentHeight + Mathf.Abs(s_lowestPoint)) / GeoConst.EVEREST_PEAK_METERS;
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Trace.Exception(e);
                        s_heightMapTiff.data[y, x] = 0f;
                    }
                }
            }

            if (abortable.shouldAbort)
            {
                return null;
            }

            CheckCornersTIFF();

            if (controller.smoothIterations > 0)
            {
                if (abortable.shouldAbort)
                {
                    return null;
                }

                FinalizeSmooth(s_heightMapTiff.data, s_heightMapTiff.width,
                    s_heightMapTiff.length, controller.smoothIterations, s_smoothBlendIndex, s_smoothBlend);
            }

            return s_heightMapTiff.data;
        }

        [RuntimeAsync(nameof(TiffDataDynamicFAR))]
        public static float[,] TiffDataDynamicFAR(string fileName)
        {
            try
            {
                using (Tiff inputImage = Tiff.Open(fileName, "r"))
                {
                    s_heightMapFarTerrainImageDetail.width = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    s_heightMapFarTerrainImageDetail.length = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    s_heightMapFarTerrainImageDetail.data = new float[s_heightMapFarTerrainImageDetail.length, s_heightMapFarTerrainImageDetail.width];
                    s_heightMapFarTerrainImageDetail.dataASCII = new float[s_heightMapFarTerrainImageDetail.length, s_heightMapFarTerrainImageDetail.width];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    for (int y = 0; y < s_heightMapFarTerrainImageDetail.length; y += tileHeight)
                    {
                        for (int x = 0; x < s_heightMapFarTerrainImageDetail.width; x += tileWidth)
                        {
                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                            {
                                for (int j = 0; j < tileWidth; j++)
                                {
                                    if ((y + i) < s_heightMapFarTerrainImageDetail.length && (x + j) < s_heightMapFarTerrainImageDetail.width)
                                    {
                                        s_heightMapFarTerrainImageDetail.dataASCII[y + i, x + j] = fBuffer[i, j];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            // Rotate terrain heights and normalize values
            for (int y = 0; y < s_heightMapFarTerrainImageDetail.width; y++)
            {
                for (int x = 0; x < s_heightMapFarTerrainImageDetail.length; x++)
                {
                    s_currentHeight = s_heightMapFarTerrainImageDetail.dataASCII[(s_heightMapFarTerrainImageDetail.width - 1) - y, x];

                    try
                    {
                        if (s_lowestPoint >= 0)
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = (s_currentHeight - s_lowestPoint) / GeoConst.EVEREST_PEAK_METERS;
                        }
                        else
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = (s_currentHeight + Mathf.Abs(s_lowestPoint)) / GeoConst.EVEREST_PEAK_METERS;
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Trace.Exception(e);
                        s_heightMapFarTerrainImageDetail.data[y, x] = 0f;
                    }
                }
            }

            CheckCornersTIFFFAR();

            //if(controller.smoothIterations > 0)
            //FinalizeSmoothFAR(s_heightMapFarTerrainImageDetail.data, s_heightMapFarTerrainImageDetail.width, s_heightMapFarTerrainImageDetail.length, controller.smoothIterations, s_smoothBlendIndex, s_smoothBlend);

            return s_heightMapFarTerrainImageDetail.data;
        }

        [RuntimeAsync(nameof(RawData))]
        public static void RawData(string fileName)
        {
            PickRawDefaults(fileName);

            byte[] buffer;

            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read)))
            {
                buffer = reader.ReadBytes((m_Width * m_Height) * (int)m_Depth);
                reader.Close();
            }

            s_rawData = new float[m_Width, m_Height];

            if (m_Depth == Depth.Bit16)
            {
                float num = 1.525879E-05f;

                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        int num2 = Clamp(x, 0, m_Width - 1) + Clamp(y, 0, m_Height - 1) * m_Width;

                        if (m_ByteOrder == ByteOrder.Mac == BitConverter.IsLittleEndian)
                        {
                            byte b = buffer[num2 * 2];
                            buffer[num2 * 2] = buffer[num2 * 2 + 1];
                            buffer[num2 * 2 + 1] = b;
                        }

                        ushort num3 = BitConverter.ToUInt16(buffer, num2 * 2);
                        float num4 = (float)num3 * num;
                        s_currentHeight = num4;

                        s_rawData[(m_Width - 1) - y, x] = num4;
                    }
                }
            }
            else
            {
                float num10 = 0.00390625f;

                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        int index = Clamp(x, 0, m_Width - 1) + (Clamp(y, 0, m_Height - 1) * m_Width);
                        byte num14 = buffer[index];
                        float num15 = num14 * num10;
                        s_currentHeight = num15;

                        s_rawData[(m_Width - 1) - y, x] = num15;
                    }
                }
            }

            s_highestPoint = s_rawData.Cast<float>().Max() * GeoConst.EVEREST_PEAK_METERS;
            s_lowestPoint = s_rawData.Cast<float>().Min() * GeoConst.EVEREST_PEAK_METERS;
            float lowestPointNormalized = s_rawData.Cast<float>().Min();

            if (m_Depth == Depth.Bit16)
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        if (lowestPointNormalized >= 0)
                        {
                            s_rawData[(m_Width - 1) - y, x] -= lowestPointNormalized;
                        }
                        else
                        {
                            s_rawData[(m_Width - 1) - y, x] += Mathf.Abs(lowestPointNormalized);
                        }

                        // Check Terrain Corners
                        // Top Row
                        if (y == 0)
                        {
                            s_topCorner.Add(s_currentHeight);
                        }
                        // Bottom Row
                        else if (y == m_Width - 1)
                        {
                            s_bottomCorner.Add(s_currentHeight);
                        }

                        // Left Column
                        if (x == 0)
                        {
                            s_leftCorner.Add(s_currentHeight);
                        }
                        // Right Column
                        else if (x == m_Height - 1)
                        {
                            s_rightCorner.Add(s_currentHeight);
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        s_rawData[(m_Width - 1) - y, x] -= lowestPointNormalized;

                        // Check Terrain Corners
                        // Top Row
                        if (y == 0)
                        {
                            s_topCorner.Add(s_currentHeight);
                        }
                        // Bottom Row
                        else if (y == m_Width - 1)
                        {
                            s_bottomCorner.Add(s_currentHeight);
                        }

                        // Left Column
                        if (x == 0)
                        {
                            s_leftCorner.Add(s_currentHeight);
                        }
                        // Right Column
                        else if (x == m_Height - 1)
                        {
                            s_rightCorner.Add(s_currentHeight);
                        }
                    }
                }
            }

            CheckCornersRAW();
        }

        private static void PickRawDefaults(string fileName)
        {
            FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            int length = (int)stream.Length;
            stream.Close();

            m_Depth = Depth.Bit16;
            int num2 = length / (int)m_Depth;
            int num3 = Mathf.RoundToInt(Mathf.Sqrt((float)num2));
            int num4 = Mathf.RoundToInt(Mathf.Sqrt((float)num2));

            if (((num3 * num4) * (int)m_Depth) == length)
            {
                m_Width = num3;
                m_Height = num4;

                heightmapResX = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(m_Width) / (float)NearMetrics.splitSizeFinal);
                heightmapResY = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(m_Height) / (float)NearMetrics.splitSizeFinal);
                heightmapResXAll = m_Width;
                heightmapResYAll = m_Height;

                return;
            }
            else
            {
                m_Depth = Depth.Bit8;
                num2 = length / (int)m_Depth;
                num3 = (int)Math.Round(Math.Sqrt((float)num2));
                num4 = (int)Math.Round(Math.Sqrt((float)num2));

                if (((num3 * num4) * (int)m_Depth) == length)
                {
                    m_Width = num3;
                    m_Height = num4;

                    heightmapResX = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(m_Width) / (float)NearMetrics.splitSizeFinal);
                    heightmapResY = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(m_Height) / (float)NearMetrics.splitSizeFinal);
                    heightmapResXAll = m_Width;
                    heightmapResYAll = m_Height;

                    return;
                }

                m_Depth = Depth.Bit16;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        [RuntimeAsync(nameof(AsciiData))]
        public static void AsciiData(string fileName)
        {
            StreamReader sr = new StreamReader(fileName, Encoding.ASCII, true);

            //ncols
            string[] line1 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            s_nCols = (Convert.ToInt32(line1[1]));
            //nrows
            string[] line2 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            s_nRows = (Convert.ToInt32(line2[1]));

            //            //xllcorner
            //            string[] line3 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            //            //yllcorner
            //            string[] line4 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            //            //cellsize
            //            string[] line5 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            //            //nodata
            //            string[] line6 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            //xllcorner
            sr.ReadLine();
            //yllcorner
            sr.ReadLine();
            //cellsize
            sr.ReadLine();
            //nodata
            sr.ReadLine();

            s_asciiData = new float[s_nCols, s_nRows];

            heightmapResX = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(s_nRows) / (float)NearMetrics.splitSizeFinal);
            heightmapResY = Mathf.FloorToInt((float)Mathf.ClosestPowerOfTwo(s_nCols) / (float)NearMetrics.splitSizeFinal);
            heightmapResXAll = s_nRows;
            heightmapResYAll = s_nCols;

            for (int y = 0; y < s_nRows; y++)
            {
                string[] line = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                for (int x = 0; x < s_nCols; x++)
                {
                    s_currentHeight = float.Parse(line[x].Replace(',', '.'));
                    s_asciiData[(s_nRows - 1) - y, x] = s_currentHeight / GeoConst.EVEREST_PEAK_METERS;
                }
            }

            sr.Close();

            s_highestPoint = s_asciiData.Cast<float>().Max() * GeoConst.EVEREST_PEAK_METERS;
            s_lowestPoint = s_asciiData.Cast<float>().Min() * GeoConst.EVEREST_PEAK_METERS;
            float lowestPointNormalized = s_asciiData.Cast<float>().Min();

            for (int y = 0; y < s_nRows; y++)
            {
                for (int x = 0; x < s_nCols; x++)
                {
                    if (lowestPointNormalized >= 0)
                    {
                        s_asciiData[(s_nRows - 1) - y, x] -= lowestPointNormalized;
                    }
                    else
                    {
                        s_asciiData[(s_nRows - 1) - y, x] += Mathf.Abs(lowestPointNormalized);
                    }

                    // Check Terrain Corners
                    // Top Row
                    if (y == 0)
                    {
                        s_topCorner.Add(s_currentHeight);
                    }
                    // Bottom Row
                    else if (y == s_nRows - 1)
                    {
                        s_bottomCorner.Add(s_currentHeight);
                    }

                    // Left Column
                    if (x == 0)
                    {
                        s_leftCorner.Add(s_currentHeight);
                    }
                    // Right Column
                    else if (x == s_nCols - 1)
                    {
                        s_rightCorner.Add(s_currentHeight);
                    }
                }
            }

            CheckCornersASCII();
        }

        private static void CheckCornersTIFF()
        {
            // Check Top
            if (s_topCorner.All(o => o == s_topCorner.First()))
            {
                for (int y = 0; y < s_heightMapTiff.width; y++)
                {
                    for (int x = 0; x < s_heightMapTiff.length; x++)
                    {
                        if (y == 0)
                        {
                            s_heightMapTiff.data[y, x] = s_heightMapTiff.data[y + 1, x];
                        }
                    }
                }
            }

            // Check Bottom
            if (s_bottomCorner.All(o => o == s_bottomCorner.First()))
            {
                for (int y = 0; y < s_heightMapTiff.width; y++)
                {
                    for (int x = 0; x < s_heightMapTiff.length; x++)
                    {
                        if (y == s_heightMapTiff.width - 1)
                        {
                            s_heightMapTiff.data[y, x] = s_heightMapTiff.data[y - 1, x];
                        }
                    }
                }
            }

            // Check Left
            if (s_leftCorner.All(o => o == s_leftCorner.First()))
            {
                for (int y = 0; y < s_heightMapTiff.width; y++)
                {
                    for (int x = 0; x < s_heightMapTiff.length; x++)
                    {
                        if (x == 0)
                        {
                            s_heightMapTiff.data[y, x] = s_heightMapTiff.data[y, x + 1];
                        }
                    }
                }
            }

            // Check Right
            if (s_rightCorner.All(o => o == s_rightCorner.First()))
            {
                for (int y = 0; y < s_heightMapTiff.width; y++)
                {
                    for (int x = 0; x < s_heightMapTiff.length; x++)
                    {
                        if (x == s_heightMapTiff.length - 1)
                        {
                            s_heightMapTiff.data[y, x] = s_heightMapTiff.data[y, x - 1];
                        }
                    }
                }
            }
        }

        private static void CheckCornersTIFFFAR()
        {
            // Check Top
            if (s_topCorner.All(o => o == s_topCorner.First()))
            {
                for (int y = 0; y < s_heightMapFarTerrainImageDetail.width; y++)
                {
                    for (int x = 0; x < s_heightMapFarTerrainImageDetail.length; x++)
                    {
                        if (y == 0)
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = s_heightMapFarTerrainImageDetail.data[y + 1, x];
                        }
                    }
                }
            }

            // Check Bottom
            if (s_bottomCorner.All(o => o == s_bottomCorner.First()))
            {
                for (int y = 0; y < s_heightMapFarTerrainImageDetail.width; y++)
                {
                    for (int x = 0; x < s_heightMapFarTerrainImageDetail.length; x++)
                    {
                        if (y == s_heightMapFarTerrainImageDetail.width - 1)
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = s_heightMapFarTerrainImageDetail.data[y - 1, x];
                        }
                    }
                }
            }

            // Check Left
            if (s_leftCorner.All(o => o == s_leftCorner.First()))
            {
                for (int y = 0; y < s_heightMapFarTerrainImageDetail.width; y++)
                {
                    for (int x = 0; x < s_heightMapFarTerrainImageDetail.length; x++)
                    {
                        if (x == 0)
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = s_heightMapFarTerrainImageDetail.data[y, x + 1];
                        }
                    }
                }
            }

            // Check Right
            if (s_rightCorner.All(o => o == s_rightCorner.First()))
            {
                for (int y = 0; y < s_heightMapFarTerrainImageDetail.width; y++)
                {
                    for (int x = 0; x < s_heightMapFarTerrainImageDetail.length; x++)
                    {
                        if (x == s_heightMapFarTerrainImageDetail.length - 1)
                        {
                            s_heightMapFarTerrainImageDetail.data[y, x] = s_heightMapFarTerrainImageDetail.data[y, x - 1];
                        }
                    }
                }
            }
        }

        private static void CheckCornersASCII()
        {
            // Check Top
            if (s_topCorner.All(o => o == s_topCorner.First()))
            {
                for (int y = 0; y < s_nRows; y++)
                {
                    for (int x = 0; x < s_nCols; x++)
                    {
                        if (y == 0)
                        {
                            s_asciiData[(s_nRows - 1) - y, x] = s_asciiData[(s_nRows - 1) - (y + 1), x];
                        }
                    }
                }
            }

            // Check Bottom
            if (s_bottomCorner.All(o => o == s_bottomCorner.First()))
            {
                for (int y = 0; y < s_nRows; y++)
                {
                    for (int x = 0; x < s_nCols; x++)
                    {
                        if (y == s_nRows - 1)
                        {
                            s_asciiData[(s_nRows - 1) - y, x] = s_asciiData[(s_nRows - 1) - (y - 1), x];
                        }
                    }
                }
            }

            // Check Left
            if (s_leftCorner.All(o => o == s_leftCorner.First()))
            {
                for (int y = 0; y < s_nRows; y++)
                {
                    for (int x = 0; x < s_nCols; x++)
                    {
                        if (x == 0)
                        {
                            s_asciiData[(s_nRows - 1) - y, x] = s_asciiData[(s_nRows - 1) - y, x + 1];
                        }
                    }
                }
            }

            // Check Right
            if (s_rightCorner.All(o => o == s_rightCorner.First()))
            {
                for (int y = 0; y < s_nRows; y++)
                {
                    for (int x = 0; x < s_nCols; x++)
                    {
                        if (x == s_nCols - 1)
                        {
                            s_asciiData[(s_nRows - 1) - y, x] = s_asciiData[(s_nRows - 1) - y, x - 1];
                        }
                    }
                }
            }
        }

        private static void CheckCornersRAW()
        {
            // Check Top
            if (s_topCorner.All(o => o == s_topCorner.First()))
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        if (y == 0)
                        {
                            s_rawData[(m_Width - 1) - y, x] = s_rawData[(m_Width - 1) - (y + 1), x];
                        }
                    }
                }
            }

            // Check Bottom
            if (s_bottomCorner.All(o => o == s_bottomCorner.First()))
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        if (y == m_Width - 1)
                        {
                            s_rawData[(m_Width - 1) - y, x] = s_rawData[(m_Width - 1) - (y - 1), x];
                        }
                    }
                }
            }

            // Check Left
            if (s_leftCorner.All(o => o == s_leftCorner.First()))
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        if (x == 0)
                        {
                            s_rawData[(m_Width - 1) - y, x] = s_rawData[(m_Width - 1) - y, x + 1];
                        }
                    }
                }
            }

            // Check Right
            if (s_rightCorner.All(o => o == s_rightCorner.First()))
            {
                for (int y = 0; y < m_Width; y++)
                {
                    for (int x = 0; x < m_Height; x++)
                    {
                        if (x == m_Height - 1)
                        {
                            s_rawData[(m_Width - 1) - y, x] = s_rawData[(m_Width - 1) - y, x - 1];
                        }
                    }
                }
            }
        }

        [RuntimeAsync(nameof(FinalizeSmooth))]
        public static void FinalizeSmooth(float[,] heightMapSmoothed, int width, int height, int iterations, int blendIndex, float blending)
        {
            if (iterations != 0)
            {
                int Tw = width;
                int Th = height;

                if (blendIndex == 1)
                {
                    float[,] generatedHeightMap = (float[,])heightMapSmoothed.Clone();
                    generatedHeightMap = SmoothedHeights(generatedHeightMap, Tw, Th, iterations);

                    for (int Ty = 0; Ty < Th; Ty++)
                    {
                        for (int Tx = 0; Tx < Tw; Tx++)
                        {
                            float oldHeightAtPoint = heightMapSmoothed[Tx, Ty];
                            float newHeightAtPoint = generatedHeightMap[Tx, Ty];
                            float blendedHeightAtPoint = 0.0f;

                            blendedHeightAtPoint = (newHeightAtPoint * blending) + (oldHeightAtPoint * (1.0f - blending));

                            heightMapSmoothed[Tx, Ty] = blendedHeightAtPoint;
                        }
                    }
                }
                else
                    heightMapSmoothed = SmoothedHeights(heightMapSmoothed, Tw, Th, iterations);
            }
        }

        [RuntimeAsync(nameof(SmoothedHeights))]
        private static float[,] SmoothedHeights(float[,] heightMap, int tw, int th, int iterations)
        {
            int Tw = tw;
            int Th = th;
            int xNeighbours;
            int yNeighbours;
            int xShift;
            int yShift;
            int xIndex;
            int yIndex;
            int Tx;
            int Ty;

            for (int iter = 0; iter < iterations; iter++)
            {
                for (Ty = 0; Ty < Th; Ty++)
                {
                    if (Ty == 0)
                    {
                        yNeighbours = 2;
                        yShift = 0;
                        yIndex = 0;
                    }
                    else if (Ty == Th - 1)
                    {
                        yNeighbours = 2;
                        yShift = -1;
                        yIndex = 1;
                    }
                    else
                    {
                        yNeighbours = 3;
                        yShift = -1;
                        yIndex = 1;
                    }

                    for (Tx = 0; Tx < Tw; Tx++)
                    {
                        if (Tx == 0)
                        {
                            xNeighbours = 2;
                            xShift = 0;
                            xIndex = 0;
                        }
                        else if (Tx == Tw - 1)
                        {
                            xNeighbours = 2;
                            xShift = -1;
                            xIndex = 1;
                        }
                        else
                        {
                            xNeighbours = 3;
                            xShift = -1;
                            xIndex = 1;
                        }

                        int Ny;
                        int Nx;
                        float hCumulative = 0.0f;
                        int nNeighbours = 0;

                        for (Ny = 0; Ny < yNeighbours; Ny++)
                        {
                            for (Nx = 0; Nx < xNeighbours; Nx++)
                            {
                                if (s_neighbourhood == Neighbourhood.Moore || (s_neighbourhood == Neighbourhood.VonNeumann && (Nx == xIndex || Ny == yIndex)))
                                {
                                    float heightAtPoint = heightMap[Tx + Nx + xShift, Ty + Ny + yShift]; // Get height at point
                                    hCumulative += heightAtPoint;
                                    nNeighbours++;
                                }
                            }
                        }

                        float hAverage = hCumulative / nNeighbours;
                        heightMap[Tx + xIndex + xShift, Ty + yIndex + yShift] = hAverage;
                    }
                }
            }

            return heightMap;
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFF()
        {
            int counter = 0;
            int currentRow = NearMetrics.splitSizeFinal - 1;
            int xLength = heightmapResFinalX;
            int yLength = heightmapResFinalY;
            int xStart = 0;
            int yStart = 0;

            if (s_splittedTerrains)
            {
                for (int i = 0; i < NearMetrics.splitSizeFinal; i++)
                {
                    for (int j = 0; j < NearMetrics.splitSizeFinal; j++)
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        if (counter >= s_taskIndex - controller.concurrentTasks && counter < s_taskIndex)
                        {
                            s_croppedTerrains[counter].terrainData.heightmapResolution = heightmapResFinalX;
                            float[,] tiffDataSplitted = new float[heightmapResFinalX, heightmapResFinalY];

                            if (!controller.spiralGeneration)
                            {
                                xStart = (currentRow * (heightmapResFinalX - 1));
                                yStart = (j * (heightmapResFinalY - 1));
                            }
                            else
                            {
                                xStart = ((NearMetrics.splitSizeFinal - ((int)s_spiralCell[counter].x - 1)) - 1) * (heightmapResFinalX - 1);
                                yStart = ((int)s_spiralCell[counter].y - 1) * (heightmapResFinalY - 1);
                            }
                            try
                            {
                                for (int x = 0; x < xLength; x++)
                                {
                                    for (int y = 0; y < yLength; y++)
                                    {
                                        tiffDataSplitted[x, y] = s_finalHeights[xStart + x, yStart + y];
                                    }
                                }

                                 TerrainController.RunCoroutine(FillHeights(s_croppedTerrains[counter], heightmapResFinalX, tiffDataSplitted),
                                    "LoadTerrainHeightsFromTIFF() s_splittedTerrains", heightmapResFinalX);

                                s_realTerrainWidth = NearMetrics.spanX_m / NearMetrics.splitSizeFinal;
                                s_realTerrainLength = NearMetrics.spanZ_m / NearMetrics.splitSizeFinal;

                                s_croppedTerrains[counter].terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                                s_croppedTerrains[counter].Flush();
                            }
                            catch (Exception e)
                            {
                                Trace.Exception(e);
                            }
                        }

                        counter++;
                    }
                    currentRow--;
                }

                yield return 0;
            }
            else if (s_terrain)
            {
                s_terrain.terrainData.heightmapResolution = heightmapResFinalXAll;

                try
                {
                     TerrainController.RunCoroutine(FillHeights(s_terrain, heightmapResFinalXAll, s_finalHeights),
                        "LoadTerrainHeightsFromTIFF()",
                        "FillHeights(terrain: , width: {0}, s_heights: )",
                        heightmapResFinalX);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_terrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_terrain.Flush();
                }
                catch (Exception e)
                {
                    Trace.Exception(e);
                }
            }
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFFDynamic(int i)
        {
            if (s_splittedTerrains)
            {
                if (controller.progressiveGeneration)
                {
                    if (s_concurrentUpdates > controller.concurrentTasks - 1)
                    {
                        yield return Timing.WaitForSeconds(1);

                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        TerrainController.RunCoroutine(LoadTerrainHeightsFromTIFFDynamic(i),
                            "LoadTerrainHeightsFromTIFFDynamic(i: {0})", i);
                    }
                    else
                    {
                        try
                        {
                            if (Abortable.ShouldAbortRoutine())
                            {
                                yield break;
                            }

                            s_croppedTerrains[i].terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                            TerrainController.RunCoroutine(FillHeightsDynamic(s_croppedTerrains[i], NearMetrics.heightMapResolution, s_tiffDataDynamic[i]),
                                "LoadTerrainHeightsFromTIFFDynamic(i: {0}) progressiveGeneration cropped", i);

                            s_realTerrainWidth = NearMetrics.spanX_m;
                            s_realTerrainLength = NearMetrics.spanZ_m;

                            s_croppedTerrains[i].terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                            s_croppedTerrains[i].Flush();
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                        }
                    }
                }
                else
                {
                    for (int x = 0; x < NearMetrics.cellCount; x++)
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        if (controller.fastStartBuild)
                        {
                            try
                            {
                                int index = s_spiralIndex[x];

                                s_croppedTerrains[index].terrainData.heightmapResolution = NearMetrics.heightMapResolution;
                                s_croppedTerrains[index].terrainData.SetHeights(0, 0, s_tiffDataDynamic[index]);

                                s_realTerrainWidth = NearMetrics.spanX_m;
                                s_realTerrainLength = NearMetrics.spanZ_m;

                                s_croppedTerrains[index].terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                                s_croppedTerrains[index].Flush();

                                s_croppedTerrains[index].drawHeightmap = true;
                            }
                            catch (Exception e)
                            {
                                Trace.Exception(e);
                            }
                        }
                        else
                        {
                            if (x >= s_taskIndex - controller.concurrentTasks && x < s_taskIndex)
                            {
                                try
                                {
                                    int index = s_spiralIndex[x];

                                    s_croppedTerrains[index].terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                                    TerrainController.RunCoroutine(FillHeightsDynamic(s_croppedTerrains[index], NearMetrics.heightMapResolution, s_tiffDataDynamic[index]),
                                        "LoadTerrainHeightsFromTIFFDynamic(i: {0})", i);

                                    s_realTerrainWidth = NearMetrics.spanX_m;
                                    s_realTerrainLength = NearMetrics.spanZ_m;

                                    s_croppedTerrains[index].terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                                    s_croppedTerrains[index].Flush();

                                    s_croppedTerrains[index].drawHeightmap = true;
                                }
                                catch (Exception e)
                                {
                                    Trace.Exception(e);
                                }
                            }
                        }
                    }

                    if (controller.stitchTerrainTiles)
                    {
                        TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, 0f, NearMetrics.cellCount),
                            "LoadTerrainHeightsFromTIFFDynamic(i: {0})", i);
                    }
                    else
                    {
                        if (!controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
                        {
                            controller.UpdateState(
                                TerrainController.TerrainState.TerrainsGenerated,
                                "LoadTerrainHeightsFromTIFFDynamic");

                            if (controller.elevationOnly)
                            {
                                if (controller.farTerrain)
                                {
                                    if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                                    {
                                        TerrainController.RunCoroutine(WorldIsGenerated(),
                                            "LoadTerrainHeightsFromTIFFDynamic(i: {0}) elevationOnly FarTerrainsGenerated", i);
                                    }
                                }
                                else
                                {
                                    TerrainController.RunCoroutine(WorldIsGenerated(),
                                        "LoadTerrainHeightsFromTIFFDynamic(i: {0}) elevationOnly", i);
                                }
                            }
                            else
                            {
                                if (controller.IsInState(TerrainController.TerrainState.TexturesGenerated))
                                {
                                    if (controller.farTerrain)
                                    {
                                        if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                                        {
                                            TerrainController.RunCoroutine(WorldIsGenerated(),
                                                "LoadTerrainHeightsFromTIFFDynamic(i: {0}) FarTerrainsGenerated", i);
                                        }
                                    }
                                    else
                                    {
                                        TerrainController.RunCoroutine(WorldIsGenerated(),
                                            "LoadTerrainHeightsFromTIFFDynamic(i: {0})", i);
                                    }
                                }
                            }
                        }
                    }
                }

                yield return 0;
            }
            else if (s_terrain)
            {
                try
                {
                    s_terrain.terrainData.heightmapResolution = controller.heightmapResolution + 1;

                    TerrainController.RunCoroutine(FillHeightsDynamic(s_terrain, controller.heightmapResolution + 1, s_finalHeights),
                        "LoadTerrainHeightsFromTIFFDynamic(i: {0})", i);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_terrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_terrain.Flush();
                }
                catch (Exception e)
                {
                    Trace.Exception(e);
                }
            }
        }

        public static void GetTerrainHeightsFromTIFFFAR()
        {
            GetTerrainHeightsFromTIFFFAR_Params(s_terrain, controller.farTerrainHeightmapResolution + 1, s_heightMapFarTerrainImageDetail.data);
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFFNORTH(int i)
        {
            try
            {
                if (TerrainInfiniteController.s_northTerrains.Count > 0)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    Terrain s_currentTerrain = s_splittedTerrains.transform.Find(TerrainInfiniteController.s_northTerrains[0]).GetComponent<Terrain>();

                    s_currentTerrain.terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                    TerrainController.RunCoroutine(FillHeightsNORTH(s_currentTerrain, NearMetrics.heightMapResolution, s_tiffDataDynamic[i]),
                        "LoadTerrainHeightsFromTIFFNORTH(i: {0})", i);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_currentTerrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_currentTerrain.Flush();

                    TerrainInfiniteController.s_northTerrains.Remove(s_currentTerrain.name);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            yield return 0;
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFFSOUTH(int i)
        {
            try
            {
                if (TerrainInfiniteController.s_southTerrains.Count > 0)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    Terrain s_currentTerrain = s_splittedTerrains.transform.Find(TerrainInfiniteController.s_southTerrains[0]).GetComponent<Terrain>();

                    s_currentTerrain.terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                    TerrainController.RunCoroutine(FillHeightsSOUTH(s_currentTerrain, NearMetrics.heightMapResolution, s_tiffDataDynamic[i]),
                        "LoadTerrainHeightsFromTIFFSOUTH(i: {0})", i);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_currentTerrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_currentTerrain.Flush();

                    TerrainInfiniteController.s_southTerrains.Remove(s_currentTerrain.name);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            yield return 0;
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFFEAST(int i)
        {
            try
            {
                if (TerrainInfiniteController.s_eastTerrains.Count > 0)
                {
                    Terrain s_currentTerrain = s_splittedTerrains.transform.Find(TerrainInfiniteController.s_eastTerrains[0]).GetComponent<Terrain>();

                    s_currentTerrain.terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                     TerrainController.RunCoroutine(FillHeightsEAST(s_currentTerrain, NearMetrics.heightMapResolution, s_tiffDataDynamic[i]),
                        "LoadTerrainHeightsFromTIFFEAST(i: {0})", i);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_currentTerrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_currentTerrain.Flush();

                    TerrainInfiniteController.s_eastTerrains.Remove(s_currentTerrain.name);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            yield return 0;
        }

        public static IEnumerator<float> LoadTerrainHeightsFromTIFFWEST(int i)
        {
            try
            {
                if (TerrainInfiniteController.s_westTerrains.Count > 0)
                {
                    Terrain s_currentTerrain = s_splittedTerrains.transform.Find(TerrainInfiniteController.s_westTerrains[0]).GetComponent<Terrain>();

                    s_currentTerrain.terrainData.heightmapResolution = NearMetrics.heightMapResolution;

                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                     TerrainController.RunCoroutine(FillHeightsWEST(s_currentTerrain, NearMetrics.heightMapResolution, s_tiffDataDynamic[i]),
                        "LoadTerrainHeightsFromTIFFWEST(i: {0})", i);

                    s_realTerrainWidth = NearMetrics.spanX_m;
                    s_realTerrainLength = NearMetrics.spanZ_m;

                    s_currentTerrain.terrainData.size = RealTerrainSize(s_realTerrainWidth, s_realTerrainLength, s_highestPoint);
                    s_currentTerrain.Flush();

                    TerrainInfiniteController.s_westTerrains.Remove(s_currentTerrain.name);
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            yield return 0;
        }

        private static IEnumerator<float> FillHeightsDynamic(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            Interlocked.Increment(ref s_concurrentUpdates);

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                if (controller.showTileOnFinish)
                {
                    terrainTile.drawHeightmap = true;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            Interlocked.Increment(ref s_generatedTerrainsCount);
            s_concurrentUpdates--;

            if (s_generatedTerrainsCount < NearMetrics.cellCount)
            {
                if (!controller.progressiveGeneration)
                {
                    if (s_generatedTerrainsCount % controller.concurrentTasks == 0)
                    {
                        s_taskIndex += controller.concurrentTasks;

                         TerrainController.RunCoroutine(LoadTerrainHeightsFromTIFFDynamic(0),
                            "FillHeightsDynamic()");
                    }
                }
            }
            else
            {
                if (s_splittedTerrains)
                {
                    if (controller.stitchTerrainTiles)
                    {
                        // TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, controller.elevationDelay, NearMetrics.cellCount));
                         TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, 0, NearMetrics.cellCount),
                            "FillHeightsDynamic()");
                    }
                    else
                    {
                        //   if(controller.showTileOnFinish)
                        //   {
                        //       foreach(Terrain t in s_croppedTerrains)
                        //       {
                        //           t.drawHeightmap = true;
                        //       }
                        //   }

                        if (!controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
                        {
                            controller.UpdateState(
                                TerrainController.TerrainState.TerrainsGenerated,
                                "FillHeightsDynamic");

                            if (controller.elevationOnly)
                            {
                                if (controller.farTerrain)
                                {
                                    if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                                    {
                                         TerrainController.RunCoroutine(WorldIsGenerated(),
                                            "FillHeightsDynamic() elevationOnly farTerrain");
                                    }
                                }
                                else
                                {
                                     TerrainController.RunCoroutine(WorldIsGenerated(),
                                        "FillHeightsDynamic() elevationOnly");
                                }
                            }
                            else
                            {
                                if (controller.IsInState(TerrainController.TerrainState.TexturesGenerated))
                                {
                                    if (controller.farTerrain)
                                    {
                                        if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                                        {
                                             TerrainController.RunCoroutine(WorldIsGenerated(),
                                                "FillHeightsDynamic() farTerrain");
                                        }
                                    }
                                    else
                                    {
                                         TerrainController.RunCoroutine(WorldIsGenerated(),
                                            "FillHeightsDynamic()");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void GetTerrainHeightsFromTIFFFAR_Params(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            Trace.Log(TerrainController.traceDebug, "Enter FillHeightsDynamicFAR()");

            int gridCount = (terrainRes - 1) / controller.farTerrainCellSize;
            int xyStart = Mathf.FloorToInt(gridCount / controller.areaSizeFarMultiplier / 2f);
            int center = (gridCount / 2) - 1;
            farHeightsData = new FillHeightsFARData();

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    if (s_downloadedFarTerrains >= 2)
                    {
                        // Only load terrain parts in view if there are too many tiles
                        if (i <= center - xyStart || i > center + xyStart || j <= center - xyStart || j > center + xyStart)
                        {
                            try
                            {
                                s_heightmapCellFar = new float[controller.farTerrainCellSize, controller.farTerrainCellSize];
                                int row = i * controller.farTerrainCellSize;
                                int col = j * controller.farTerrainCellSize;

                                for (int x = 0; x < controller.farTerrainCellSize; x++)
                                {
                                    //  Uncomment the following log trace for additional diagnostic detail.
                                    try
                                    {
                                        Array.Copy
                                        (
                                            terrainHeights,
                                            (x + col) * (terrainRes) + row,
                                            s_heightmapCellFar,
                                            x * controller.farTerrainCellSize,
                                            controller.farTerrainCellSize
                                        );
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.Log("GetTerrainHeightsFromTIFFFAR_Params() - Array Copy x: {0}, row: {1}, col: {2}, source index: {3}, dest index: {4}, length: {5}",
                                            x, row, col, s_heightmapCellFar, x * controller.farTerrainCellSize, controller.farTerrainCellSize);
                                        Trace.Error(e.Message);
                                    }
                                }

                                FillHeightsData data = new FillHeightsData();
                                data.row = row;
                                data.col = col;
                                data.data = s_heightmapCellFar;
                                farHeightsData.all.Add(data);
                            }
                            catch (Exception e)
                            {
                                Trace.Exception(e);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            s_heightmapCellFar = new float[controller.farTerrainCellSize, controller.farTerrainCellSize];
                            int row = i * controller.farTerrainCellSize;
                            int col = j * controller.farTerrainCellSize;

                            for (int x = 0; x < controller.farTerrainCellSize; x++)
                            {
                                //  Uncomment the following log trace for additional diagnostic detail.
                                //Trace.Log(TerrainController.traceDebug, "FillHeightsDynamicFAR() - Array Copy x: {0}, row: {1}, col: {2}, source index: {3}, dest index: {4:G}, length: {5}",
                                //    x, row, col, (x + col) * (terrainRes) + row, (x * controller.farTerrainCellSize), controller.farTerrainCellSize);

                                Array.Copy
                                (
                                    terrainHeights,
                                    (x + col) * (terrainRes) + row,
                                    s_heightmapCellFar,
                                    x * controller.farTerrainCellSize,
                                    controller.farTerrainCellSize
                                );
                            }

                            FillHeightsData data = new FillHeightsData();
                            data.row = row;
                            data.col = col;
                            data.data = s_heightmapCellFar;
                            farHeightsData.all.Add(data);
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                        }
                    }
                }
            }

            try
            {
                // Top Row
                s_heightmapCellFar = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCellFar[0, x] = terrainHeights[terrainRes - 1, x];
                }

                FillHeightsData rowData = new FillHeightsData();
                rowData.row = 0;
                rowData.col = terrainRes - 1;
                rowData.data = s_heightmapCellFar;
                farHeightsData.all.Add(rowData);

                // Right Column
                s_heightmapCellFar = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCellFar[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                FillHeightsData colData = new FillHeightsData();
                colData.row = terrainRes - 1;
                colData.col = 0;
                colData.data = s_heightmapCellFar;
                farHeightsData.all.Add(colData);
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        public static IEnumerator<float> ApplyTerrainHeightsFromTIFFFAR()
        {
            farHeightsData.Apply(s_terrain, s_heightMapFarTerrainImageDetail.data);

            yield return 0;

            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            if (controller.delayedLOD)
            {
                s_terrain.ApplyDelayedHeightmapModification();
            }

            s_terrain.terrainData.size = RealTerrainSizeFAR(s_farTerrainSize, s_farTerrainSize, s_highestPoint);
            s_terrain.Flush();

            if (s_downloadedFarTerrains == 1)
            {
                s_terrain.drawHeightmap = true;

                if (!controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    controller.UpdateState(TerrainController.TerrainState.FarTerrainsGenerated);

                    if (controller.elevationOnly &&
                        controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
                    {
                         TerrainController.RunCoroutine(WorldIsGenerated(),
                            "FillHeightsDynamicFAR() elevationOnly");
                    }
                    else if (
                        !controller.elevationOnly &&
                        controller.IsInState(TerrainController.TerrainState.TexturesGenerated) &&
                        controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                         TerrainController.RunCoroutine(WorldIsGenerated(),
                            "FillHeightsDynamicFAR()");
                    }
                }
            }
            else
            {
                SwitchFarTerrainsCompleted();
            }
        }

        private static IEnumerator<float> FillHeightsNORTH(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            s_generationIsBusyNORTH = true;
            s_terrainsInProgress.Add(terrainTile);

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                s_terrainsInProgress.Remove(terrainTile);
                terrainTile.drawHeightmap = true;

                Interlocked.Increment(ref s_northCounterGenerated);

                if (controller.stitchTerrainTiles && s_northCounterGenerated % NearMetrics.cellCountEdge == 0)
                {
                    s_generationIsBusyNORTH = false;

                     TerrainController.RunCoroutine(WaitAndStitchNORTH(),
                        "FillHeightsNORTH()");
                }
                else
                {
                    ManageNeighborings();
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static IEnumerator<float> FillHeightsSOUTH(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            s_generationIsBusySOUTH = true;
            s_terrainsInProgress.Add(terrainTile);

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                s_terrainsInProgress.Remove(terrainTile);
                terrainTile.drawHeightmap = true;

                Interlocked.Increment(ref s_southCounterGenerated);

                if (controller.stitchTerrainTiles && s_southCounterGenerated % NearMetrics.cellCountEdge == 0)
                {
                    s_generationIsBusySOUTH = false;

                     TerrainController.RunCoroutine(WaitAndStitchSOUTH(),
                        "FillHeightsSOUTH()");
                }
                else
                {
                    ManageNeighborings();
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static IEnumerator<float> FillHeightsEAST(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            s_generationIsBusyEAST = true;
            s_terrainsInProgress.Add(terrainTile);

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                s_terrainsInProgress.Remove(terrainTile);
                terrainTile.drawHeightmap = true;

                Interlocked.Increment(ref s_eastCounterGenerated);

                if (controller.stitchTerrainTiles && s_eastCounterGenerated % NearMetrics.cellCountEdge == 0)
                {
                    s_generationIsBusyEAST = false;

                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                     TerrainController.RunCoroutine(WaitAndStitchEAST(),
                        "FillHeightsEAST()");
                }
                else
                {
                    ManageNeighborings();
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static IEnumerator<float> FillHeightsWEST(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            s_generationIsBusyWEST = true;
            s_terrainsInProgress.Add(terrainTile);

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                s_terrainsInProgress.Remove(terrainTile);
                terrainTile.drawHeightmap = true;

                Interlocked.Increment(ref s_westCounterGenerated);

                if (controller.stitchTerrainTiles && s_westCounterGenerated % NearMetrics.cellCountEdge == 0)
                {
                    s_generationIsBusyWEST = false;

                     TerrainController.RunCoroutine(WaitAndStitchWEST(),
                        "FillHeightsWEST()");
                }
                else
                {
                    ManageNeighborings();
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        private static IEnumerator<float> WaitAndStitchNORTH()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            if (!s_stitchingInProgress && !TerrainInfiniteController.inProgressWest && !TerrainInfiniteController.inProgressEast)
            {
                 TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, controller.stitchDelay, s_croppedTerrains.Count),
                    "WaitAndStitchNORTH()");
            }
            else
            {
                if (!TerrainInfiniteController.inProgressWest && !TerrainInfiniteController.inProgressEast)
                {
                    yield return Timing.WaitForSeconds(1f);

                     TerrainController.RunCoroutine(WaitAndStitchNORTH(),
                        "WaitAndStitchNORTH()");
                }
            }

            yield return 0;
        }

        private static IEnumerator<float> WaitAndStitchSOUTH()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            if (!s_stitchingInProgress && !TerrainInfiniteController.inProgressWest && !TerrainInfiniteController.inProgressEast)
            {
                 TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, controller.stitchDelay, s_croppedTerrains.Count),
                    "WaitAndStitchSOUTH()");
            }
            else
            {
                if (!TerrainInfiniteController.inProgressWest && !TerrainInfiniteController.inProgressEast)
                {
                    yield return Timing.WaitForSeconds(1f);

                     TerrainController.RunCoroutine(WaitAndStitchSOUTH(),
                        "WaitAndStitchSOUTH()");
                }
            }

            yield return 0;
        }

        private static IEnumerator<float> WaitAndStitchEAST()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            if (!s_stitchingInProgress && !TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth)
            {
                 TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, controller.stitchDelay, s_croppedTerrains.Count),
                    "WaitAndStitchEAST()");
            }
            else
            {
                if (!TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth)
                {
                    yield return Timing.WaitForSeconds(1f);

                     TerrainController.RunCoroutine(WaitAndStitchEAST(),
                        "WaitAndStitchEAST()");
                }
            }

            yield return 0;
        }

        private static IEnumerator<float> WaitAndStitchWEST()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            if (!s_stitchingInProgress && !TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth)
            {
                 TerrainController.RunCoroutine(StitchTerrain(s_croppedTerrains, controller.stitchDelay, s_croppedTerrains.Count),
                    "WaitAndStitchWEST()");
            }
            else
            {
                if (!TerrainInfiniteController.inProgressNorth && !TerrainInfiniteController.inProgressSouth)
                {
                    yield return Timing.WaitForSeconds(1f);

                     TerrainController.RunCoroutine(WaitAndStitchWEST(),
                        "WaitAndStitchWEST()");
                }
            }

            yield return 0;
        }

        private static void StitchNORTH()
        {
            if (TerrainInfiniteController.hybridEast)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_northTerrainsNeighbor.Union(TerrainInfiniteController.s_eastTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchNORTH()");
            }
            else if (TerrainInfiniteController.hybridWest)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_northTerrainsNeighbor.Union(TerrainInfiniteController.s_westTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchNORTH()");
            }
            else
            {
                 TerrainController.RunCoroutine(StitchTerrain(TerrainInfiniteController.s_northTerrainsNeighbor, controller.stitchDelay, TerrainInfiniteController.s_northTerrainsNeighbor.Count),
                    "StitchNORTH()");
            }
        }

        private static void StitchSOUTH()
        {
            if (TerrainInfiniteController.hybridEast)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_southTerrainsNeighbor.Union(TerrainInfiniteController.s_eastTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchSOUTH()");
            }
            else if (TerrainInfiniteController.hybridWest)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_southTerrainsNeighbor.Union(TerrainInfiniteController.s_westTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchSOUTH()");
            }
            else
            {
                 TerrainController.RunCoroutine(StitchTerrain(TerrainInfiniteController.s_southTerrainsNeighbor, controller.stitchDelay, TerrainInfiniteController.s_southTerrainsNeighbor.Count),
                    "StitchSOUTH()");
            }
        }

        private static void StitchEAST()
        {
            if (TerrainInfiniteController.hybridNorth)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_eastTerrainsNeighbor.Union(TerrainInfiniteController.s_northTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchEAST()");
            }
            else if (TerrainInfiniteController.hybridSouth)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_eastTerrainsNeighbor.Union(TerrainInfiniteController.s_southTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count),
                    "StitchEAST()");
            }
            else
            {
                 TerrainController.RunCoroutine(StitchTerrain(TerrainInfiniteController.s_eastTerrainsNeighbor, controller.stitchDelay, TerrainInfiniteController.s_eastTerrainsNeighbor.Count),
                    "StitchEAST()");
            }
        }

        private static void StitchWEST()
        {
            if (TerrainInfiniteController.hybridNorth)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_westTerrainsNeighbor.Union(TerrainInfiniteController.s_northTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count), "StitchWEST()",
                    "StitchWEST()");
            }
            else if (TerrainInfiniteController.hybridSouth)
            {
                List<Terrain> mixedTerrains = TerrainInfiniteController.s_westTerrainsNeighbor.Union(TerrainInfiniteController.s_southTerrainsNeighbor).ToList();

                 TerrainController.RunCoroutine(StitchTerrain(mixedTerrains, controller.stitchDelay, mixedTerrains.Count), "StitchWEST()",
                    "StitchWEST()");
            }
            else
            {
                 TerrainController.RunCoroutine(StitchTerrain(TerrainInfiniteController.s_westTerrainsNeighbor, controller.stitchDelay, TerrainInfiniteController.s_westTerrainsNeighbor.Count),
                    "StitchWEST()");
            }
        }

        private static IEnumerator<float> StitchTerrain(List<Terrain> allTerrains, float delay, int terrainCount)
        {
            ManageNeighborings();

            s_stitchingInProgress = true;

            s_stitchedTerrainsCount = 0;
            s_terrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());
            s_terrains = allTerrains.ToArray();

            if (s_terrains.Length > 0)
            {
                int sizeX = (int)s_terrains[0].terrainData.size.x;
                int sizeZ = (int)s_terrains[0].terrainData.size.z;

                foreach (Terrain ter in s_terrains)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    try
                    {
                        int[] posTer = new int[]
                        {
                            (int)(Mathf.Round(ter.transform.position.x / sizeX)),
                            (int)(Mathf.Round(ter.transform.position.z / sizeZ))
                        };

                        s_terrainDict.Add(posTer, ter);
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }
                }

                //Checks neighbours and stitches them
                foreach (var item in s_terrainDict)
                {
                    int[] posTer = item.Key;
                    Terrain topTerrain = null;
                    Terrain leftTerrain = null;
                    Terrain rightTerrain = null;
                    Terrain bottomTerrain = null;

                    s_terrainDict.TryGetValue(new int[]
                    {
                        posTer [0],
                        posTer [1] + 1
                    },
                        out topTerrain
                    );

                    s_terrainDict.TryGetValue(new int[]
                    {
                        posTer [0] - 1,
                        posTer [1]
                    },
                        out leftTerrain
                    );

                    s_terrainDict.TryGetValue(new int[]
                    {
                        posTer [0] + 1,
                        posTer [1]
                    },
                        out rightTerrain
                    );

                    s_terrainDict.TryGetValue(new int[]
                    {
                        posTer [0],
                        posTer [1] - 1
                    },
                        out bottomTerrain
                    );

                    if (rightTerrain != null) s_hasRight = true; else s_hasRight = false;
                    if (topTerrain != null) s_hasTop = true; else s_hasTop = false;

                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                     TerrainController.RunCoroutine(StitchTerrains(item.Value, rightTerrain, topTerrain, s_hasRight, s_hasTop, true, delay, terrainCount),
                        "StitchTerrain()");

                    yield return Timing.WaitForSeconds(delay);
                }
            }

            yield return 0;
        }

        private static IEnumerator<float> StitchTerrains(Terrain ter, Terrain rightTerrain, Terrain topTerrain, bool hasRight, bool hasTop, bool smooth, float delay, int terrainCount)
        {
            int YLength = NearMetrics.heightMapResolution - s_stitchDistance;

            if (hasRight)
            {
                int y = s_stitchDistance - 1;

                s_heights = ter.terrainData.GetHeights(YLength, 0, s_stitchDistance, ter.terrainData.heightmapResolution);
                s_secondHeights = rightTerrain.terrainData.GetHeights(0, 0, s_stitchDistance, rightTerrain.terrainData.heightmapResolution);

                for (int x = 0; x < NearMetrics.heightMapResolution; x++)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    s_heights[x, y] = Average(s_heights[x, y], s_secondHeights[x, 0]);

                    if (smooth)
                    {
                        s_heights[x, y] += Mathf.Abs(s_heights[x, y - 1] - s_secondHeights[x, 1]) / controller.levelSmooth;
                    }

                    s_secondHeights[x, 0] = s_heights[x, y];

                    for (int i = 1; i < s_stitchDistance; i++)
                    {
                        s_heights[x, y - i] = (Average(s_heights[x, y - i], s_heights[x, y - i + 1]) + Mathf.Abs(s_heights[x, y - i] - s_heights[x, y - i + 1]) / controller.levelSmooth) * (s_stitchDistance - i) / s_stitchDistance + s_heights[x, y - i] * i / s_stitchDistance;
                        s_secondHeights[x, i] = (Average(s_secondHeights[x, i], s_secondHeights[x, i - 1]) + Mathf.Abs(s_secondHeights[x, i] - s_secondHeights[x, i - 1]) / controller.levelSmooth) * (s_stitchDistance - i) / s_stitchDistance + s_secondHeights[x, i] * i / s_stitchDistance;
                    }
                }

                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }

                // Right Columns
                ter.terrainData.SetHeights(YLength, 0, s_heights);
                ter.Flush();

                // Left Columns
                rightTerrain.terrainData.SetHeights(0, 0, s_secondHeights);
                rightTerrain.Flush();
            }

            if (hasTop)
            {
                int x = s_stitchDistance - 1;

                s_heights = ter.terrainData.GetHeights(0, YLength, ter.terrainData.heightmapResolution, s_stitchDistance);
                s_secondHeights = topTerrain.terrainData.GetHeights(0, 0, topTerrain.terrainData.heightmapResolution, s_stitchDistance);

                for (int y = 0; y < NearMetrics.heightMapResolution; y++)
                {
                    s_heights[x, y] = Average(s_heights[x, y], s_secondHeights[0, y]);

                    if (smooth)
                    {
                        s_heights[x, y] += Mathf.Abs(s_heights[x - 1, y] - s_secondHeights[1, y]) / controller.levelSmooth;
                    }

                    s_secondHeights[0, y] = s_heights[x, y];

                    for (int i = 1; i < s_stitchDistance; i++)
                    {
                        s_heights[x - i, y] = (Average(s_heights[x - i, y], s_heights[x - i + 1, y]) + Mathf.Abs(s_heights[x - i, y] - s_heights[x - i + 1, y]) / controller.levelSmooth) * (s_stitchDistance - i) / s_stitchDistance + s_heights[x - i, y] * i / s_stitchDistance;
                        s_secondHeights[i, y] = (Average(s_secondHeights[i, y], s_secondHeights[i - 1, y]) + Mathf.Abs(s_secondHeights[i, y] - s_secondHeights[i - 1, y]) / controller.levelSmooth) * (s_stitchDistance - i) / s_stitchDistance + s_secondHeights[i, y] * i / s_stitchDistance;
                    }
                }

                // Top Rows
                ter.terrainData.SetHeights(0, YLength, s_heights);
                ter.Flush();

                // Bottom Rows
                topTerrain.terrainData.SetHeights(0, 0, s_secondHeights);
                topTerrain.Flush();
            }

            Interlocked.Increment(ref s_stitchedTerrainsCount);

            if (s_stitchedTerrainsCount == terrainCount)
            {
                TerrainInfiniteController.hybridNorth = false;
                TerrainInfiniteController.hybridSouth = false;
                TerrainInfiniteController.hybridEast = false;
                TerrainInfiniteController.hybridWest = false;
            }

            yield return 0;
        }

        private static IEnumerator<float> RepairCorners(float delay)
        {
            foreach (var item in s_terrainDict)
            {
                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }

                int[] posTer = item.Key;
                //Terrain topTerrain = null;
                //Terrain leftTerrain = null;
                Terrain rightTerrain = null;
                Terrain bottomTerrain = null;

                s_terrainDict.TryGetValue(new int[]
                {
                    posTer [0] + 1,
                    posTer [1]
                },
                    out rightTerrain
                );

                s_terrainDict.TryGetValue(new int[]
                {
                    posTer [0],
                    posTer [1] - 1
                },
                    out bottomTerrain
                );

                if (rightTerrain != null && bottomTerrain != null)
                {
                    Terrain rightBottom = null;

                    s_terrainDict.TryGetValue(new int[]
                    {
                        posTer [0] + 1,
                        posTer [1] - 1
                    },
                        out rightBottom
                    );

                    if (rightBottom != null)
                    {
                         TerrainController.RunCoroutine(StitchTerrainsRepair(item.Value, rightTerrain, bottomTerrain, rightBottom, delay),
                            "RepairCorners()");
                    }
                }
            }

            yield return 0;
        }

        private static IEnumerator<float> StitchTerrainsRepair(Terrain terrain11, Terrain terrain21, Terrain terrain12, Terrain terrain22, float delay)
        {
            yield return Timing.WaitForSeconds(delay);

            try
            {
                if (Abortable.ShouldAbortRoutine())
                {
                    yield break;
                }

                int size = terrain11.terrainData.heightmapResolution - 1;
                List<float> s_heights = new List<float>();

                s_heights.Add(terrain11.terrainData.GetHeights(size, 0, 1, 1)[0, 0]);
                s_heights.Add(terrain21.terrainData.GetHeights(0, 0, 1, 1)[0, 0]);
                s_heights.Add(terrain12.terrainData.GetHeights(size, size, 1, 1)[0, 0]);
                s_heights.Add(terrain22.terrainData.GetHeights(0, size, 1, 1)[0, 0]);

                float[,] height = new float[1, 1];
                height[0, 0] = s_heights.Max();

                terrain11.terrainData.SetHeights(size, 0, height);
                terrain21.terrainData.SetHeights(0, 0, height);
                terrain12.terrainData.SetHeights(size, size, height);
                terrain22.terrainData.SetHeights(0, size, height);

                terrain11.Flush();
                terrain12.Flush();
                terrain21.Flush();
                terrain22.Flush();

                ManageNeighborings();
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
        }

        static float Average(float first, float second)
        {
            return Mathf.Pow((Mathf.Pow(first, controller.power) + Mathf.Pow(second, controller.power)) / 2.0f, 1 / controller.power);
        }

        private static IEnumerator<float> FillHeights(Terrain terrainTile, int terrainRes, float[,] terrainHeights)
        {
            if (s_splittedTerrains)
            {
                yield return Timing.WaitForSeconds(controller.elevationDelay);
            }

            int gridCount = (terrainRes - 1) / controller.cellSize;

            for (int i = 0; i < gridCount; i++)
            {
                for (int j = 0; j < gridCount; j++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        s_heightmapCell = new float[controller.cellSize, controller.cellSize];
                        int row = i * controller.cellSize;
                        int col = j * controller.cellSize;

                        for (int x = 0; x < controller.cellSize; x++)
                        {
                            Array.Copy
                            (
                                terrainHeights,
                                (x + col) * (terrainRes) + row,
                                s_heightmapCell,
                                x * controller.cellSize,
                                controller.cellSize
                            );
                        }

                        if (controller.delayedLOD)
                        {
                            terrainTile.terrainData.SetHeightsDelayLOD(row, col, s_heightmapCell);
                        }
                        else
                        {
                            terrainTile.terrainData.SetHeights(row, col, s_heightmapCell);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    yield return Timing.WaitForSeconds(controller.elevationDelay);
                }
            }

            try
            {
                // Fill Top Row
                s_heightmapCell = new float[1, terrainRes];

                for (int x = 0; x < terrainRes; x++)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    s_heightmapCell[0, x] = terrainHeights[terrainRes - 1, x];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(0, terrainRes - 1, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(0, terrainRes - 1, s_heightmapCell);
                }

                // Fill Right Column
                s_heightmapCell = new float[terrainRes, 1];

                for (int x = 0; x < terrainRes; x++)
                {
                    if (Abortable.ShouldAbortRoutine())
                    {
                        yield break;
                    }

                    s_heightmapCell[x, 0] = terrainHeights[x, terrainRes - 1];
                }

                if (controller.delayedLOD)
                {
                    terrainTile.terrainData.SetHeightsDelayLOD(terrainRes - 1, 0, s_heightmapCell);
                }
                else
                {
                    terrainTile.terrainData.SetHeights(terrainRes - 1, 0, s_heightmapCell);
                }

                if (controller.delayedLOD)
                {
                    terrainTile.ApplyDelayedHeightmapModification();
                }

                if (controller.showTileOnFinish)
                {
                    terrainTile.drawHeightmap = true;
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            Interlocked.Increment(ref s_generatedTerrainsCount);

            if (s_generatedTerrainsCount < NearMetrics.cellCount)
            {
                if (s_generatedTerrainsCount % controller.concurrentTasks == 0)
                {
                    s_taskIndex += controller.concurrentTasks;

                     TerrainController.RunCoroutine(LoadTerrainHeightsFromTIFF(),
                        "FillHeights()");
                }
            }
            else
            {
                if (s_splittedTerrains)
                {
                    ManageNeighborings();
                }
            }
        }

        public static void ManageNeighborings()
        {
            s_terrainsLong = NearMetrics.splitSizeFinal;
            s_terrainsWide = NearMetrics.splitSizeFinal;
            SetTerrainNeighbors();
        }

        private static void SetTerrainNeighbors()
        {
            GetTerrainList();

             TerrainController.RunCoroutine(PerformNeighboring(s_stitchingTerrainsList),
                "SetTerrainNeighbors()");
        }

        private static void GetTerrainList()
        {
            s_stitchingTerrainsList = new List<Terrain>();

            for (int x = 0; x < NearMetrics.cellCountEdge; x++)
            {
                for (int y = 0; y < NearMetrics.cellCountEdge; y++)
                {
                    s_stitchingTerrainsList.Add(TerrainInfiniteController._grid[y, x]);
                }
            }
        }

        private static IEnumerator<float> PerformNeighboring(List<Terrain> terrains)
        {
            int counter = 0;

            for (int y = 0; y < NearMetrics.cellCountEdge; y++)
            {
                for (int x = 0; x < NearMetrics.cellCountEdge; x++)
                {
                    try
                    {
                        if (Abortable.ShouldAbortRoutine())
                        {
                            yield break;
                        }

                        if (y == 0)
                        {
                            // TopLeft Corner
                            if (x == 0)
                            {
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                            // TopRight Corner
                            else if (x == NearMetrics.cellCountEdge - 1)
                            {
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                            else
                            {
                                //terrains[counter].drawHeightmap = true;
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                        }
                        else if (y == NearMetrics.cellCountEdge - 1)
                        {
                            // BottomLeft Corner
                            if (x == 0)
                            {
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                            // BottomRight Corner
                            else if (x == NearMetrics.cellCountEdge - 1)
                            {
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                            else
                            {
                                terrains[counter].groupingID = 0;
                                terrains[counter].allowAutoConnect = true;
                            }
                        }
                        else
                        {
                            terrains[counter].groupingID = 0;
                            terrains[counter].allowAutoConnect = true;
                        }

                        terrains[counter].Flush();
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    counter++;
                }
            }

            s_stitchingInProgress = false;

            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }
            CheckInitialization();

            yield return 0;
        }

        private static void CheckInitialization()
        {
            if (!controller.IsInState(TerrainController.TerrainState.TerrainsGenerated))
            {
                controller.UpdateState(TerrainController.TerrainState.TerrainsGenerated, "CheckInitialization()");

                if (controller.elevationOnly)
                {
                    if (controller.farTerrain)
                    {
                        if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                        {
                             TerrainController.RunCoroutine(WorldIsGenerated(),
                                "CheckInitialization() elevationOnly farTerrain");
                        }
                    }
                    else
                    {
                         TerrainController.RunCoroutine(WorldIsGenerated(),
                            "CheckInitialization() elevationOnly");
                    }
                }
                else
                {
                    if (controller.IsInState(TerrainController.TerrainState.TexturesGenerated))
                    {
                        if (controller.farTerrain)
                        {
                            if (controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
                            {
                                 TerrainController.RunCoroutine(WorldIsGenerated(),
                                    "CheckInitialization() farTerrain");
                            }
                        }
                        else
                        {
                             TerrainController.RunCoroutine(WorldIsGenerated(),
                                "CheckInitialization()");
                        }
                    }
                }
            }
        }

        [RuntimeAsync(nameof(CalculateResampleHeightmaps))]
        public static void CalculateResampleHeightmaps()
        {
            // Set chunk resolutions to a "Previous Power of 2" value
            if (s_splittedTerrains)
            {
                if (!Mathf.IsPowerOfTwo(s_croppedTerrains.Count))
                {
                    heightmapResFinalX = ((Mathf.NextPowerOfTwo(controller.heightmapResolution / NearMetrics.splitSizeFinal)) / 2) + 1;
                    heightmapResFinalY = ((Mathf.NextPowerOfTwo(controller.heightmapResolution / NearMetrics.splitSizeFinal)) / 2) + 1;
                    heightmapResFinalXAll = heightmapResFinalX * NearMetrics.splitSizeFinal;
                    heightmapResFinalYAll = heightmapResFinalY * NearMetrics.splitSizeFinal;

                    ResampleOperation();
                }
                else
                {
                    heightmapResFinalX = NearMetrics.heightmapResolutionSplit + 1;
                    heightmapResFinalY = NearMetrics.heightmapResolutionSplit + 1;
                    heightmapResFinalXAll = s_terrainResolutionDownloading;
                    heightmapResFinalYAll = s_terrainResolutionDownloading;

                    s_finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                    s_finalHeights = s_heightMapTiff.data;
                }
            }
            else if (s_terrain)
            {
                heightmapResFinalX = s_terrainResolutionDownloading;
                heightmapResFinalY = s_terrainResolutionDownloading;
                heightmapResFinalXAll = s_terrainResolutionDownloading;
                heightmapResFinalYAll = s_terrainResolutionDownloading;

                s_finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                s_finalHeights = s_heightMapTiff.data;
            }
            else
            {
                if (!Mathf.IsPowerOfTwo(NearMetrics.cellCountEdge))
                {
                    heightmapResFinalX = ((Mathf.NextPowerOfTwo(controller.heightmapResolution / NearMetrics.cellCountEdge)) / 2) + 1;
                    heightmapResFinalY = ((Mathf.NextPowerOfTwo(controller.heightmapResolution / NearMetrics.cellCountEdge)) / 2) + 1;
                    heightmapResFinalXAll = heightmapResFinalX * NearMetrics.cellCountEdge;
                    heightmapResFinalYAll = heightmapResFinalY * NearMetrics.cellCountEdge;

                    ResampleOperation();
                }
                else
                {
                    heightmapResFinalX = NearMetrics.heightMapResolution;
                    heightmapResFinalY = NearMetrics.heightMapResolution;
                    heightmapResFinalXAll = s_terrainResolutionDownloading;
                    heightmapResFinalYAll = s_terrainResolutionDownloading;

                    s_finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                    s_finalHeights = s_heightMapTiff.data;
                }
            }
        }

        private static Vector3 RealTerrainSize(float width, float length, float height)
        {
            /*
            terrainEverestDiffer = GeoConst.EVEREST_PEAK_METERS / s_highestPoint;
            s_realTerrainHeight = ((s_initialTerrainWidth * NearMetrics.splitSizeFinal) * ((height * terrainEverestDiffer) / width)) * controller.elevationExaggeration;

            if(s_realTerrainHeight <= 0f ||  float.IsNaN(s_realTerrainHeight) || float.IsInfinity(s_realTerrainHeight) || float.IsPositiveInfinity(s_realTerrainHeight) || float.IsNegativeInfinity(s_realTerrainHeight))
                s_realTerrainHeight = 0.001f;
            */

            float realTerrainSizeZ = s_initialTerrainWidth * NearMetrics.spanAspectRatio;
            Vector3 finalTerrainSize = new Vector3(s_initialTerrainWidth, s_realTerrainHeight, realTerrainSizeZ);

            return finalTerrainSize;
        }

        private static Vector3 RealTerrainSizeFAR(float width, float length, float height)
        {
            Vector3 finalTerrainSize = new Vector3(width, s_realTerrainHeight, length);

            return finalTerrainSize;
        }

        [RuntimeAsync(nameof(ResampleOperation))]
        private static void ResampleOperation()
        {
            float scaleFactorLat = ((float)heightmapResFinalXAll) / ((float)heightmapResXAll);
            float scaleFactorLon = ((float)heightmapResFinalYAll) / ((float)heightmapResYAll);

            s_finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];

            for (int x = 0; x < heightmapResFinalXAll; x++)
            {
                for (int y = 0; y < heightmapResFinalYAll; y++)
                {
                    s_finalHeights[x, y] = ResampleHeights((float)x / scaleFactorLat, (float)y / scaleFactorLon);
                }
            }
        }

        [RuntimeAsync(nameof(ResampleHeights))]
        private static float ResampleHeights(float X, float Y)
        {
            try
            {
                int X1 = Mathf.RoundToInt((X + heightmapResXAll % heightmapResXAll));
                int Y1 = Mathf.RoundToInt((Y + heightmapResYAll % heightmapResYAll));

                return s_heightMapTiff.data[X1, Y1];
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                return 0f;
            }
        }

        [RuntimeAsync(nameof(GetRAWInfo))]
        public static void GetRAWInfo()
        {
            PickRawDefaults(s_geoDataPathElevation);

            byte[] buffer;

            using (BinaryReader reader = new BinaryReader(File.Open(s_geoDataPathElevation, FileMode.Open, FileAccess.Read)))
            {
                buffer = reader.ReadBytes((m_Width * m_Height) * (int)m_Depth);
                reader.Close();
            }

            s_rawData = new float[m_Width, m_Height];

            if (m_Depth == Depth.Bit16)
            {
                float num = 1.525879E-05f;

                for (int i = 0; i < m_Width; i++)
                {
                    for (int j = 0; j < m_Height; j++)
                    {
                        int num2 = Mathf.Clamp(j, 0, m_Width - 1) + Mathf.Clamp(i, 0, m_Height - 1) * m_Width;

                        if (m_ByteOrder == ByteOrder.Mac == BitConverter.IsLittleEndian)
                        {
                            byte b = buffer[num2 * 2];
                            buffer[num2 * 2] = buffer[num2 * 2 + 1];
                            buffer[num2 * 2 + 1] = b;
                        }

                        ushort num3 = BitConverter.ToUInt16(buffer, num2 * 2);
                        float num4 = (float)num3 * num;
                        s_rawData[(m_Width - 1) - i, j] = num4;
                    }
                }
            }
            else
            {
                float num10 = 0.00390625f;

                for (int k = 0; k < m_Width; k++)
                {
                    for (int m = 0; m < m_Height; m++)
                    {
                        int index = Mathf.Clamp(m, 0, m_Width - 1) + (Mathf.Clamp(k, 0, m_Height - 1) * m_Width);
                        byte num14 = buffer[index];
                        float num15 = num14 * num10;
                        s_rawData[(m_Width - 1) - k, m] = num15;
                    }
                }
            }

            s_highestPoint = s_rawData.Cast<float>().Max() * GeoConst.EVEREST_PEAK_METERS;
            s_lowestPoint = s_rawData.Cast<float>().Min() * GeoConst.EVEREST_PEAK_METERS;
        }

        [RuntimeAsync(nameof(GetTIFFInfo))]
        public static void GetTIFFInfo()
        {
            try
            {
                using (Tiff inputImage = Tiff.Open(s_geoDataPathElevation, "r"))
                {
                    s_heightMapTiff.width = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    s_heightMapTiff.length = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    s_heightMapTiff.dataASCII = new float[s_heightMapTiff.length, s_heightMapTiff.width];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    for (int y = 0; y < s_heightMapTiff.length; y += tileHeight)
                    {
                        for (int x = 0; x < s_heightMapTiff.width; x += tileWidth)
                        {
                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                            {
                                for (int j = 0; j < tileWidth; j++)
                                {
                                    if ((y + i) < s_heightMapTiff.length && (x + j) < s_heightMapTiff.width)
                                    {
                                        s_heightMapTiff.dataASCII[y + i, x + j] = fBuffer[i, j];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            s_highestPoint = s_heightMapTiff.dataASCII.Cast<float>().Max();
            s_lowestPoint = s_heightMapTiff.dataASCII.Cast<float>().Min();
        }

        [RuntimeAsync(nameof(GetASCIIInfo))]
        public static void GetASCIIInfo()
        {
            StreamReader sr = new StreamReader(s_geoDataPathElevation, Encoding.ASCII, true);

            //ncols
            string[] line1 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            s_nCols = (Convert.ToInt32(line1[1]));
            //nrows
            string[] line2 = sr.ReadLine().Replace(',', '.').Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            s_nRows = (Convert.ToInt32(line2[1]));

            //xllcorner
            sr.ReadLine();
            //yllcorner
            sr.ReadLine();
            //cellsize
            sr.ReadLine();
            //nodata
            sr.ReadLine();

            s_asciiData = new float[s_nCols, s_nRows];

            for (int y = 0; y < s_nRows; y++)
            {
                string[] line = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                for (int x = 0; x < s_nCols; x++)
                {
                    s_asciiData[(s_nRows - 1) - y, x] = (float.Parse(line[x].Replace(',', '.'))) / GeoConst.EVEREST_PEAK_METERS;
                }
            }

            sr.Close();

            s_highestPoint = s_asciiData.Cast<float>().Max() * GeoConst.EVEREST_PEAK_METERS;
            s_lowestPoint = s_asciiData.Cast<float>().Min() * GeoConst.EVEREST_PEAK_METERS;
        }

        public static void GetFolderInfo(string path)
        {
            IEnumerable<string> names = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where
                (
                    s => s.EndsWith(".jpg")
                    || s.EndsWith(".png")
                    || s.EndsWith(".gif")
                    || s.EndsWith(".bmp")
                    || s.EndsWith(".tga")
                    || s.EndsWith(".psd")
                    || s.EndsWith(".tiff")
                    || s.EndsWith(".iff")
                    || s.EndsWith(".pict")
                );

            s_baseImageNames = names.ToArray();
            s_baseImageNames = LogicalComparer(s_baseImageNames);
            s_totalImagesDataBase = s_baseImageNames.Length;

            if (NearMetrics.cellCount > 1)
            {
                s_multipleTerrainsTiling = true;
                s_baseImagesPerTerrain = (int)((float)s_totalImagesDataBase / (float)NearMetrics.cellCount);
                s_tileGrid = (int)(Mathf.Sqrt((float)s_baseImagesPerTerrain));
            }
            else
            {
                s_multipleTerrainsTiling = false;
                s_tileGrid = (int)(Mathf.Sqrt((float)s_totalImagesDataBase));
                NearMetrics.cellSpanX = NearMetrics.spanX_m;
                NearMetrics.cellSpanY = NearMetrics.spanZ_m;
            }
        }

        public static List<Terrain> OrderedTerrainChunks(GameObject terrainsParentGo)
        {
            string names = "";

            foreach (Transform child in terrainsParentGo.transform)
            {
                names += child.name + Environment.NewLine;
            }

            String[] lines = names.Replace("\r", "").Split('\n');
            lines = LogicalComparer(lines);

            List<Terrain> stitchingTerrains = new List<Terrain>();

            foreach (string s in lines)
            {
                if (s != "")
                {
                    stitchingTerrains.Add(terrainsParentGo.transform.Find(s).GetComponent<Terrain>());
                }
            }

            names = null;

            return stitchingTerrains;
        }

        private static void ImageTilerOnline()
        {
            s_imageDownloadingStarted = true;
            s_availableImageryCheked = false;
            s_allBlack = false;
            int counter = 0;
            int tileImages = NearMetrics.baseImagesTotal;

            if (!s_splittedTerrains)
            {
                TerrainLayer[] terrainLayers = new TerrainLayer[tileImages];

                for (int i = 0; i < tileImages; i++)
                {
                    try
                    {
                        Texture2D satelliteImage = s_baseImageTextures[i];

                        // Texturing Terrain
                        terrainLayers[i] = new TerrainLayer();
                        terrainLayers[i].diffuseTexture = satelliteImage;
                        terrainLayers[i].tileSize = new Vector2(NearMetrics.baseImageSpanX, NearMetrics.baseImageSpanY);
                        terrainLayers[i].tileOffset = new Vector2(NearMetrics.cellMetrics[i].imageOffsetY, NearMetrics.cellMetrics[i].imageOffsetY);
                        terrainLayers[i].metallic = 0f;
                        terrainLayers[i].smoothness = 0f;
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }
                }

                s_terrain.terrainData.terrainLayers = terrainLayers;

                s_splatNormalizeX = NearMetrics.cellSpanX / s_terrain.terrainData.alphamapResolution;
                s_splatNormalizeY = NearMetrics.cellSpanY / s_terrain.terrainData.alphamapResolution;

                int lengthz = (int)(NearMetrics.baseImageSpanY / s_splatNormalizeY);
                int widthz = (int)(NearMetrics.baseImageSpanX / s_splatNormalizeX);

                for (int i = 0; i < tileImages; i++)
                {
                    try
                    {
                        int lengthzOff = (int)(NearMetrics.cellMetrics[i].imageOffsetY / s_splatNormalizeY);
                        int widthzOff = (int)(NearMetrics.cellMetrics[i].imageOffsetY / s_splatNormalizeX);

                        s_smData = new float[lengthz, widthz, s_terrain.terrainData.alphamapLayers];

                        for (int y = 0; y < lengthz; y++)
                        {
                            for (int z = 0; z < widthz; z++)
                            {
                                s_smData[y, z, i] = 1;
                            }
                        }

                        s_terrain.terrainData.SetAlphamaps(-widthzOff, -lengthzOff, s_smData);
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }
                }

                s_terrain.terrainData.RefreshPrototypes();
                s_terrain.Flush();
                s_smData = null;
            }
            else
            {
                int index = 0;
                float terrainSizeSplittedX = s_croppedTerrains[0].terrainData.size.x;
                float terrainSizeSplittedY = s_croppedTerrains[0].terrainData.size.z;

                float cellSizeSplittedX = terrainSizeSplittedX / (float)s_tileGrid;
                float cellSizeSplittedY = terrainSizeSplittedY / (float)s_tileGrid;

                //  TODO: can this be done within GridMetrics.InitializeCellMetrics()?
                for (int i = 0; i < s_tileGrid; i++)
                {
                    for (int j = 0; j < s_tileGrid; j++)
                    {
                        NearMetrics.cellMetrics[index].imageOffsetX = (terrainSizeSplittedX - (cellSizeSplittedX * ((float)s_tileGrid - (float)j))) * -1f;
                        NearMetrics.cellMetrics[index].imageOffsetY = (terrainSizeSplittedY - cellSizeSplittedY - ((float)cellSizeSplittedY * (float)i)) * -1f;

                        index++;
                    }
                }

                List<Terrain> stitchingTerrains = OrderedTerrainChunks(s_splittedTerrains);

                int[] cellIndex = new int[tileImages];
                index = 0;

                for (int i = 0; i < NearMetrics.cellCount; i++)
                {
                    cellIndex[index++] = i;
                }

                counter = 0;
                index = 0;

                foreach (Terrain terrainSplitted in stitchingTerrains)
                {
                    TerrainLayer[] terrainLayers = new TerrainLayer[s_baseImagesPerTerrain];

                    for (int i = 0; i < s_baseImagesPerTerrain; i++)
                    {
                        try
                        {
                            Texture2D satelliteImage = s_baseImageTextures[cellIndex[index]];

                            // Texturing Terrain
                            terrainLayers[i] = new TerrainLayer();
                            terrainLayers[i].diffuseTexture = satelliteImage;
                            terrainLayers[i].tileSize = new Vector2(cellSizeSplittedX, cellSizeSplittedY);
                            terrainLayers[i].tileOffset = new Vector2(NearMetrics.cellMetrics[i].imageOffsetX, NearMetrics.cellMetrics[i].imageOffsetY);
                            terrainLayers[i].metallic = 0f;
                            terrainLayers[i].smoothness = 0f;
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                        }

                        index++;
                    }

                    terrainSplitted.terrainData.terrainLayers = terrainLayers;

                    s_splatNormalizeX = terrainSplitted.terrainData.size.x / terrainSplitted.terrainData.alphamapResolution;
                    s_splatNormalizeY = terrainSplitted.terrainData.size.z / terrainSplitted.terrainData.alphamapResolution;

                    int lengthz = (int)(cellSizeSplittedY / s_splatNormalizeY);
                    int widthz = (int)(cellSizeSplittedX / s_splatNormalizeX);

                    for (int i = 0; i < s_baseImagesPerTerrain; i++)
                    {
                        try
                        {
                            int lengthzOff = (int)(NearMetrics.cellMetrics[i].imageOffsetY / s_splatNormalizeY);
                            int widthzOff = (int)(NearMetrics.cellMetrics[i].imageOffsetX / s_splatNormalizeX);

                            s_smData = new float[lengthz, widthz, terrainSplitted.terrainData.alphamapLayers];

                            for (int y = 0; y < lengthz; y++)
                            {
                                for (int z = 0; z < widthz; z++)
                                {
                                    s_smData[y, z, i] = 1;
                                }
                            }

                            terrainSplitted.terrainData.SetAlphamaps(-widthzOff, -lengthzOff, s_smData);
                        }
                        catch (Exception e)
                        {
                            Trace.Exception(e);
                        }
                    }

                    terrainSplitted.terrainData.RefreshPrototypes();
                    terrainSplitted.Flush();
                    s_smData = null;

                    counter++;
                }
            }

            // Setup Far Terrains
            if (controller.farTerrain)
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (i == 1)
                    {
                        s_currentTerrain = s_firstTerrain;
                    }
                    else if (i == 2)
                    {
                        s_currentTerrain = s_secondaryTerrain;
                    }

                    TerrainLayer[] terrainLayers = new TerrainLayer[1];

                    try
                    {
                        // Texturing Terrain
                        terrainLayers[0] = new TerrainLayer();
                        terrainLayers[0].diffuseTexture = s_baseImageTextureFar;
                        terrainLayers[0].tileSize = new Vector2(s_farTerrainSize, s_farTerrainSize);
                        terrainLayers[0].tileOffset = Vector2.zero;
                        terrainLayers[0].metallic = 0f;
                        terrainLayers[0].smoothness = 0f;
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    s_currentTerrain.terrainData.terrainLayers = terrainLayers;

                    int length = s_currentTerrain.terrainData.alphamapResolution;
                    s_smData = new float[length, length, 1];

                    try
                    {
                        for (int y = 0; y < length; y++)
                        {
                            for (int z = 0; z < length; z++)
                            {
                                s_smData[y, z, 0] = 1f;
                            }
                        }

                        s_currentTerrain.terrainData.SetAlphamaps(0, 0, s_smData);
                    }
                    catch (Exception e)
                    {
                        Trace.Exception(e);
                    }

                    s_currentTerrain.terrainData.RefreshPrototypes();
                    s_currentTerrain.Flush();
                    s_smData = null;
                }
            }
        }

        private static IEnumerator<float> WorldIsGenerated()
        {
            if (Abortable.ShouldAbortRoutine())
            {
                yield break;
            }

            yield return Timing.WaitForSeconds(1);

            controller.UpdateState(TerrainController.TerrainState.WorldIsGenerated);
        }
    }
}

