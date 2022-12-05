
/*
	The ASCII Grid file example:
	
	ncols         768 (number of colums)
	nrows         736 (number of rows)
	xllcorner     474721.00 (lower left corner X of the grid)        Longitude:  MIN: -180(West)(LEFT)        MAX: 180(East)(RIGHT)   TOTAL: 360
	yllcorner     418933.00 (lower left corner Y of the grid)        Latitude:   MIN: -90(South)(BOTTOM)      MAX: 90(North)(TOP)     TOTAL: 180
	cellsize      1.00 (cell spacing)
	nodata_value  -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -9999 -9999 -9999 -9999 -9999
	 -213.20 -9999 -9999 -9999 -9999
	
	The ASCII XYZ file example:
	
	47472.00, 418933.00: -213.20
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using BitMiracle.LibTiff.Classic;
using TerrainEngine;

namespace TerraLand
{
    public class TerrainGenerator : EditorWindow
    {
        [MenuItem("Tools/TerraUnity/TerraLand/Downloader", false, 0)]
        public static void Init()
        {
            TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "Downloader");
            TerrainGenerator window = (TerrainGenerator)GetWindow(typeof(TerrainGenerator));
            window.position = new Rect(5, 135, 430, 800);
            window.titleContent = new GUIContent("TerraLand Downloader", "Downloads Terrain Data from ESRI servers");
        }

        #region fields:

        public enum ServerGrid
        {
            _4x4 = 4,
            _8x8 = 8,
            _16x16 = 16,
            _32x32 = 32,
            _64x64 = 64,
            _128x128 = 128
        }
        static ServerGrid serverGrid = ServerGrid._32x32;

        public enum SizeNew
        {
            _1 = 1,
            _2x2 = 2,
            _3x3 = 3,
            _4x4 = 4,
            _5x5 = 5,
            _6x6 = 6,
            _7x7 = 7,
            _8x8 = 8,
            _9x9 = 9,
            _10x10 = 10,
            _11x11 = 11,
            _12x12 = 12,
            _13x13 = 13,
            _14x14 = 14,
            _15x15 = 15,
            _16x16 = 16,
            _32x32 = 32,
            _64x64 = 64
        }
        public static SizeNew enumValueNew = SizeNew._2x2;

        public enum Neighbourhood
        {
            Moore = 0,
            VonNeumann = 1
        }
        static Neighbourhood neighbourhood = Neighbourhood.Moore;

        Vector2 scrollPosition = Vector2.zero;
        private bool engineOff = false;

        float windowWidth;

        public static double top = 1d;
        public static double left = 0d;
        public static double bottom = 0d;
        public static double right = 1d;

        Terrain terrain;

        float terrainSizeX;
        float terrainSizeY;
        float terrainSizeFactor;

        string address = "Mount Everest";
        string geoReversedAddress = "";

        List<Vector2> coords;
        List<string> locations;

        public static double latitudeUser = 27.98582d;
        public static double longitudeUser = 86.9236d;

        int heightmapResolutionEditor = 2048;
        int heightmapResolutionStreaming = 2048;
        int heightmapResolutionSplit;
        int imageResolutionEditor = 2048;
        int imageResolutionStreaming = 2048;

        float progressBarElevation;
        bool showProgressElevation = false;
        bool convertingElevationTiles = false;
        bool stitchingElevationTiles = false;
        bool showProgressImagery = false;
        bool showProgressData = false;
        bool showProgressGenerateASCII = false;
        bool showProgressGenerateRAW = false;

        string[] editMode = new string[] { "ON", "OFF" };

        int gridNumber;
        int alphamapResolution = 512;

        int tileGrid = 2;
        float[,,] smData;
        float cellSizeX;
        float cellSizeY;
        float[] imageXOffset;
        float[] imageYOffset;
        int totalImages;
        string dataPath;

        float splatNormalizeX;
        float splatNormalizeY;

        double latCellSize;
        double lonCellSize;

        double[] latCellTop;
        double[] latCellBottom;
        double[] lonCellLeft;
        double[] lonCellRight;

        TerraLandWorldElevation.TopoBathy_ImageServer mapserviceElevation;
        TerraLandWorldImagery.World_Imagery_MapServer mapserviceImagery;

        string directoryPathElevation;
        string directoryPathImagery;
        string directoryPathInfo;
        string directoryPathTerrainlayers;

        int downloadedImageIndex;
        int downloadedHeightmapIndex;
        float normalizedProgressSatelliteImage;

        bool cancelOperation = false;
        bool cancelOperationHeightmapDownloader = false;
        bool terrainGenerationstarted = false;
        bool imageDownloadingStarted = false;

        double yMaxTop;
        double xMinLeft;
        double yMinBottom;
        double xMaxRight;
        double[] xMin;
        double[] yMin;
        double[] xMax;
        double[] yMax;
        double[] xMinFailedElevation;
        double[] yMinFailedElevation;
        double[] xMaxFailedElevation;
        double[] yMaxFailedElevation;
        double[] xMinFailedImagery;
        double[] yMinFailedImagery;
        double[] xMaxFailedImagery;
        double[] yMaxFailedImagery;

        bool finishedImporting = false;
        int textureOnFinish = 0;
        float elevationExaggeration = 1;

        int downloadIndexSRTM = 0;
        int downloadIndexSatellite = 0;
        int downloadIndexData = 0;
        int downloadIndexGenerationASCII = 0;
        int downloadIndexGenerationRAW = 0;

        int maxAsyncCalls = 50;

        int compressionQuality = 100;
        int anisotropicFilter = 4;

        int workerThreads;
        int completionPortThreads;
        int allThreads = 0;

        int frames2 = 0;
        int frames3 = 0;
        UnityEngine.Object failedFolder;
        FileAttributes attr;
        List<int> failedIndicesElevation;
        List<int> failedIndicesImagery;
        bool failedDownloading = false;

        int failedIndicesCountElevation;
        int failedIndicesCountImagery;

        bool showPresetManager = false;

        GameObject[] terrainGameObjects;
        Terrain[] terrains;
        TerrainData[] data;

        float tileWidth;
        float tileLength;
        float tileXPos;
        float tileZPos;
        int arrayPos;

        int totalTerrainsNew;
        string splitDirectoryPath;
        GameObject terrainsParent;
        GameObject splittedTerrains;
        int terrainChunks = 0;
        int gridPerTerrainEditor = 1;
        int gridStreamingWorld = 1;
        List<Terrain> croppedTerrains;

        public int neighbourhoodInt = 0;

        bool compressionActive = false;
        bool autoScale = false;

        bool allBlack = false;

        bool failedHeightmapAvailable = false;
        int totalFailedHeightmaps = 0;
        bool failedImageAvailable = false;
        int totalFailedImages = 0;

        UnityEngine.Object[] terraUnityImages;
        Texture2D logo;
        Texture2D heightMapLogo;
        Texture2D landMapLogo;
        Texture2D statusGreen;
        Texture2D statusRed;
        Texture2D satelliteImageTemp;
        Texture2D generateTerrainButton;
        Texture2D generateServerButton;

        string token = "";
        string terrainDataURL = "";

        WebClient webClientTerrain, webClientImagery;
        Stopwatch stopWatchTerrain = new Stopwatch();
        string downloadSpeedTerrain = "";
        string dataReceivedTerrain = "";

        bool saveTerrainDataASCII = false;
        bool saveTerrainDataRAW = false;
        bool saveTerrainDataTIFF = false;

        string projectPath;
        string fileNameTerrainData = "";
        string fileNameTerrainDataSaved = "";

        Rect rectToggle;
        bool extraOptions = false;


        #region multithreading variables

        int maxThreads = 8;
        private int numThreads;
        private int _count;

        private bool m_HasLoaded = false;

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

        public static float areaSizeLat = 16f;
        public static float areaSizeLon = 16f;
        bool squareArea = true;
        bool userCoordinates = false;

        float scaleFactor = 1;
        int smoothIterationsProgress = 1;
        int smoothIterations = 1;
        float smoothBlend = 0.8f;

        float smoothIterationProgress;
        float smoothProgress;
        bool showProgressSmoothen = false;
        int smoothStepIndex = 0;
        bool showProgressSmoothenOperation = false;
        int smoothIndex = 0;

        int smoothBlendIndex = 0;

        int tiffWidth;
        int tiffLength;
        float[,] tiffData;
        float[,] tiffDataASCII;
        float[,] tiffDataSplitted;
        float highestPoint;
        float lowestPoint;

        double UTMEasting;
        double UTMNorthing;
        string sUtmZone;
        double UTMEastingTop;
        double UTMNorthingTop;
        string sUtmZoneTop;
        double cellSize;
        string projectionStr;
        string sCentralMeridian;

        int engineModeIndex = 3;
        GUIContent[] engineMode = new GUIContent[5]
        {
            new GUIContent("MANUAL", "Set heightmap & satellite image resolutions manually and select number of tiles for the area"),
            new GUIContent("LOWEST", "Automatically sets heightmap & satellite image resolutions and number of tiles with the LOWEST quality"),
            new GUIContent("LOW", "Automatically sets heightmap & satellite image resolutions and number of tiles with the LOW quality"),
            new GUIContent("MEDIUM", "Automatically sets heightmap & satellite image resolutions and number of tiles with the MEDIUM quality"),
            new GUIContent("HIGH", "Automatically sets heightmap & satellite image resolutions and number of tiles with the HIGH quality")
        };

        float terrainSizeNewX = 16000;
        float terrainSizeNewY = 4000;
        float terrainSizeNewZ = 16000;
        float pixelError = 5f;
        int splitSizeNew;
        int splitSizeFinal;
        string terrainName;

        int chunkImageResolution;

        int heightmapResFinalX;
        int heightmapResFinalY;
        int heightmapResXAll;
        int heightmapResYAll;
        int heightmapResFinalXAll;
        int heightmapResFinalYAll;

        float[,] finalHeights;

        const float everestPeak = 8848.0f;
        float currentHeight;
        List<float> topCorner;
        List<float> bottomCorner;
        List<float> leftCorner;
        List<float> rightCorner;

        float progressDATA;
        float progressGenerateASCII;
        float progressGenerateRAW;

        string corePath;
        string downloadsPath;
        string presetsPath;
        string downloadDateStr;
        string unavailableTerrainStr = "No Terrains Selected.\n\nSelect Terrain(s) From The Scene Hierarchy Or Generate New Terrains First.";
        //string unavailableImageryStr = "There is no available imagery at this resolution for this tile or there was an unknown internet connection error!\n\nIn SATELLITE IMAGE DOWNLOADER section, decrease GRID PER TERRAIN or IMAGE RESOLUTION value or increase AREA SIZE EXTENTS.\nIt may also be possible that you are behind a blocked IP or network limitations or no internet connection detected!\n\nDo you want to continue and try downloading other tiles?";
        string unavailableImageryStr = "Failed to retrive imagery tile! It may be possible that this is temporary for this session and needs to retry or you are behind a blocked IP or encountered network limitations or no internet connection detected!\n\nDo you want to continue and try downloading other tiles?";

        string presetFilePath = "";

        int terrainResolutionTotal;
        int terrainResolutionChunk;
        int textureResolutionTotal;
        int textureResolutionChunk;

        int terrainResolutionDownloading;
        string imageImportingWarning = "EXTRA OPERATIONS WILL BE APPLIED. IMPORTING WILL BE SLOWER";
        string dataResamplingWarning = "NON POWER OF 2 GRID. CAUSES DATA RESAMPLING & QUALITY LOSS";

        List<string> failedTerrainNames;
        int threadsCount = 0;

        bool showResolutionPresetSection = true;
        bool showNewTerrainSection = true;
        bool showLocationSection = true;
        bool showAreaSizeSection = true;
        bool showHeghtmapDownloaderSection = true;
        bool showImageDownloaderSection = true;
        bool showFailedDownloaderSection = true;
        bool showServerSection = true;

        InteractiveMap mapWindow;
        int mapTypeIndex = 0;

        string[] infoFilePath;
        string[] allImageNames;
        int worldModeIndex = 0;

        GUIContent[] worldModeStr = new GUIContent[2]
        {
            new GUIContent("IN-EDITOR SCENE", "Terrains will be generated in the Editor and can be edited using Terrain Tools before level start"),
            new GUIContent("IN-GAME SCENE", "Terrain data tiles will be downloaded in a server and finally being streamed to generate terrains in runtime on demand based on player prosition")
        };

        bool dynamicWorld;
        string serverPath;
        bool serverSetUpElevation = false;
        bool serverSetUpImagery = false;
        int formatIndex = 0;
        string[] formatMode = new string[] { "RAW", "ASC", "TIF" };

        string tempPattern = "_Temp";
        bool failedTilesAvailable = false;
        byte[] tempImageBytes;

        GameObject imageImportTiles;

        int retries = 0;
        private int reducedheightmapResolution;

        public bool isTopoBathy = true;
        // Above sea-level heights
        //"https://elevation.arcgis.com/arcgis/services/WorldElevation/Terrain/ImageServer?token=";
        // Bathymetric merged with above sea-level heights
        private static string elevationURL = "https://elevation.arcgis.com/arcgis/services/WorldElevation/TopoBathy/ImageServer?token=";

        private const string tokenURL = "https://www.arcgis.com/sharing/rest/oauth2/token/authorize?client_id=n0dpgUwqazrQTyXZ&client_secret=3d4867add8ee47b6ac0c498198995298&grant_type=client_credentials&expiration=20160";

        private const int maxHeightmapResolution = 4096;
        private const int maxSatelliteImageResolution = 4096;
        private string currentDownloadsPath;

        private bool importingInProgress = false;

        #endregion

        #region methods


        public void OnEnable()
        {
            LoadResources();

            dataPath = Application.dataPath;
            projectPath = Application.dataPath.Replace("Assets", "");
            corePath = dataPath + "/TerraLand/TerraLand Core/";
            downloadsPath = corePath + "Downloads";
            presetsPath = corePath + "Presets/Downloader";

#if UNITY_WEBPLAYER
			SwitchPlatform();
#endif

            AutoLoad();
        }

        public void LoadResources()
        {
            TextureImporter imageImport;
            bool forceUpdate = false;

            terraUnityImages = Resources.LoadAll("TerraUnity/Images", typeof(Texture2D));
            logo = Resources.Load("TerraUnity/Images/Logo/TerraLand-Downloader_Logo") as Texture2D;
            heightMapLogo = Resources.Load("TerraUnity/Images/Button/Heightmap") as Texture2D;
            landMapLogo = Resources.Load("TerraUnity/Images/Button/Landmap") as Texture2D;
            statusGreen = Resources.Load("TerraUnity/Images/Button/StatusGreen") as Texture2D;
            statusRed = Resources.Load("TerraUnity/Images/Button/StatusRed") as Texture2D;
            satelliteImageTemp = Resources.Load("TerraUnity/Downloader/NotDownloaded") as Texture2D;
            generateTerrainButton = Resources.Load("TerraUnity/Images/Button/Terrain") as Texture2D;
            generateServerButton = Resources.Load("TerraUnity/Images/Button/Server") as Texture2D;

            foreach (Texture2D currentImage in terraUnityImages)
            {
                imageImport = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(currentImage)) as TextureImporter;

                if (imageImport.npotScale != TextureImporterNPOTScale.None)
                {
                    imageImport.npotScale = TextureImporterNPOTScale.None;
                    forceUpdate = true;
                }

                if (imageImport.mipmapEnabled)
                {
                    imageImport.mipmapEnabled = false;
                    forceUpdate = true;
                }

                if (forceUpdate)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(currentImage), ImportAssetOptions.ForceUpdate);
            }
        }

        public void OnDisable()
        {
            AutoSave();
        }

        private void SwitchPlatform()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
            else if (Application.platform == RuntimePlatform.OSXEditor)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        }

        public void OnGUI()
        {
            try
            {
                DrawUI();
            }
            catch
            {
                GUIUtility.ExitGUI();
            }
        }

        private void DrawUI()
        {
            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0, 0, 0, 1.0f);

            if (GUILayout.Button(logo))
                Help.BrowseURL("http://www.terraunity.com");

            GUI.backgroundColor = new Color(1, 1, 1, 1.0f);

            if (!engineOff)
            {
                GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

                if (Event.current.type == EventType.Repaint)
                    windowWidth = GUILayoutUtility.GetLastRect().width;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(5);

                if (showPresetManager)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.white;

                if (GUILayout.Button(new GUIContent("Preset Management", "Save & Load UI presets for later usage"), buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    showPresetManager = !showPresetManager;
                }

                GUI.backgroundColor = Color.white;

                if (showPresetManager)
                {
                    GUILayout.Space(-125);
                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.Space(40);
                    PresetManager();
                    GUILayout.Space(20);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = Color.green;
                worldModeIndex = GUILayout.SelectionGrid(worldModeIndex, worldModeStr, 2);
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (worldModeIndex == 0)
                    dynamicWorld = false;
                else if (worldModeIndex == 1)
                    dynamicWorld = true;

                if (showProgressElevation || showProgressImagery || showProgressData || showProgressGenerateASCII || showProgressGenerateRAW || showProgressSmoothen || showProgressSmoothenOperation)
                {
                    GUILayout.Space(10);

                    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                    GUILayout.Space(15);


                    // Heightmap Downloader Progress

                    if (showProgressElevation)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = Mathf.RoundToInt(progressBarElevation * 100f);

                        if (convertingElevationTiles)
                            EditorGUI.ProgressBar(rect, progressBarElevation, "Converting Elevation Tiles\t" + percentage + "%");
                        else if (stitchingElevationTiles)
                            EditorGUI.ProgressBar(rect, progressBarElevation, "Stitching Elevation Tiles\t" + percentage + "%");
                        else
                            EditorGUI.ProgressBar(rect, progressBarElevation, "Downloading Elevation Data\t" + percentage + "%");

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (downloadIndexSRTM != percentage)
                        {
                            Repaint();
                            downloadIndexSRTM = percentage;
                        }

                        if (!dynamicWorld && percentage > 0)
                        {
                            GUILayout.Space(25);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("SPEED: " + downloadSpeedTerrain + " Kbps", MessageType.None);
                            GUILayout.Space(10);
                            EditorGUILayout.HelpBox(dataReceivedTerrain, MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            //EditorGUILayout.BeginHorizontal();
                            //GUILayout.FlexibleSpace();
                            //rect.height = 8;
                            //rect.y = rect.y + 63;
                            //float percentSpeed = float.Parse(downloadSpeedTerrain) / 1024f;
                            //EditorGUI.ProgressBar(rect, percentSpeed, "");
                            //GUILayout.FlexibleSpace();
                            //EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && progressBarElevation == 1f)
                        progressBarElevation = 0f;


                    // Satellite Image Downloader Progress

                    if (showProgressImagery)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        if (!failedDownloading)
                            normalizedProgressSatelliteImage = Mathf.InverseLerp(0f, 1f, ((float)downloadedImageIndex / (float)totalImages));
                        else
                            normalizedProgressSatelliteImage = Mathf.InverseLerp(0f, 1f, ((float)downloadedImageIndex / (float)totalFailedImages));

                        string str = "";

                        if (downloadedImageIndex == 0)
                        {
                            if (totalImages == 1)
                                str = "Downloading Satellite Image";
                            else
                                str = "Initializing Satellite Image Downloader";
                        }
                        else if (downloadedImageIndex < totalImages)
                        {
                            if (!failedDownloading)
                                str = "Image   " + downloadedImageIndex + "   of   " + totalImages.ToString() + "   Downloaded";
                            else
                                str = "Image   " + downloadedImageIndex + "   of   " + totalFailedImages.ToString() + "   Failed Images Downloaded";
                        }
                        else
                            str = "Finished Downloading";

                        EditorGUI.ProgressBar(rect, normalizedProgressSatelliteImage, str);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (downloadIndexSatellite != downloadedImageIndex)
                        {
                            Repaint();
                            downloadIndexSatellite = downloadedImageIndex;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && normalizedProgressSatelliteImage == 1f)
                    {
                        showProgressImagery = false;
                        normalizedProgressSatelliteImage = 0f;
                    }


                    // Smoothen Operation Iteraion Progress

                    if (showProgressSmoothen)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = (int)(smoothIterationProgress);
                        EditorGUI.ProgressBar(rect, smoothIterationProgress / (float)smoothIterationsProgress, "Smoothing Step\t" + percentage + "  of  " + smoothIterationsProgress);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (smoothStepIndex != percentage)
                        {
                            Repaint();
                            smoothStepIndex = percentage;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && smoothIterationProgress == smoothIterationsProgress)
                    {
                        showProgressSmoothen = false;
                        smoothIterationProgress = 0f;
                    }


                    // Smoothen Operation Iteraion Progress

                    if (showProgressSmoothenOperation)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = Mathf.RoundToInt(smoothProgress * 100f);
                        EditorGUI.ProgressBar(rect, smoothProgress, "Smoothing Terrain Heights\t" + percentage + "%");

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (smoothIndex != percentage)
                        {
                            Repaint();
                            smoothIndex = percentage;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && Mathf.RoundToInt(smoothProgress * 100f) == 100)
                    {
                        showProgressSmoothenOperation = false;
                        smoothProgress = 0f;
                    }


                    // Data Loader Progress

                    if (showProgressData)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = Mathf.RoundToInt(progressDATA * 100f);
                        EditorGUI.ProgressBar(rect, progressDATA, "Loading Elevation Data\t" + percentage + "%");

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (downloadIndexData != percentage)
                        {
                            Repaint();
                            downloadIndexData = percentage;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && progressDATA == 1f)
                    {
                        showProgressData = false;
                        progressDATA = 0f;
                    }


                    // ASCII File Generation Progress

                    if (showProgressGenerateASCII)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = Mathf.RoundToInt(progressGenerateASCII * 100f);
                        EditorGUI.ProgressBar(rect, progressGenerateASCII, "Generating ASCII Grid Elevation File\t" + percentage + "%");

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (downloadIndexGenerationASCII != percentage)
                        {
                            Repaint();
                            downloadIndexGenerationASCII = percentage;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && progressGenerateASCII == 1f)
                    {
                        showProgressGenerateASCII = false;
                        progressGenerateASCII = 0f;
                    }

                    // RAW File Generation Progress

                    if (showProgressGenerateRAW)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.x = 47;
                        rect.width = position.width - 100;
                        rect.height = 18;

                        int percentage = Mathf.RoundToInt(progressGenerateRAW * 100f);
                        EditorGUI.ProgressBar(rect, progressGenerateRAW, "Generating RAW Elevation File\t" + percentage + "%");

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (downloadIndexGenerationRAW != percentage)
                        {
                            Repaint();
                            downloadIndexGenerationRAW = percentage;
                        }

                        GUILayout.Space(25);
                    }

                    if (Event.current.type == EventType.Repaint && progressGenerateRAW == 1f)
                    {
                        showProgressGenerateRAW = false;
                        progressGenerateRAW = 0f;
                    }

                    // Show Downloading Status

                    if (showProgressImagery)
                    {
                        GUILayout.Space(15);

                        GUI.backgroundColor = Color.clear;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        //if(showProgressElevation)
                        //	threadsCount = (allThreads + 3) - workerThreads;
                        //else
                        //	threadsCount = allThreads - workerThreads;

                        //threadsCount = Mathf.Clamp(threadsCount, 0, 1000);

                        threadsCount = Mathf.Clamp(allThreads - workerThreads, 0, 1000);

                        EditorGUILayout.HelpBox("THREADS:   " + (threadsCount).ToString(), MessageType.None);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;
                    }

                    GUILayout.Space(10);

                    EditorGUILayout.EndVertical();
                }

                if (worldModeIndex == 0)
                {
                    //if (splittedTerrains)
                    //{
                    //    try
                    //    {
                    //        heightmapResolutionSplit = heightmapResolutionEditor / (int)Mathf.Sqrt((float)terrainChunks);
                    //        splitSizeFinal = (int)Mathf.Sqrt((float)croppedTerrains.Count);
                    //        totalImages = Mathf.RoundToInt(Mathf.Pow(gridPerTerrainEditor, 2)) * terrainChunks;
                    //        gridNumber = Mathf.RoundToInt(Mathf.Sqrt(totalImages));
                    //        chunkImageResolution = (imageResolutionEditor * gridNumber) / (int)Mathf.Sqrt((float)terrainChunks);
                    //    }
                    //    catch { }
                    //}
                    //else if (terrain)
                    //{
                    //    terrainChunks = 1;
                    //    heightmapResolutionSplit = heightmapResolutionEditor;
                    //    splitSizeFinal = 1;
                    //    totalImages = Mathf.RoundToInt(Mathf.Pow(gridPerTerrainEditor, 2));
                    //    gridNumber = gridPerTerrainEditor;
                    //}
                    //else
                    //{
                    //    try
                    //    {
                            terrainChunks = totalTerrainsNew;
                            heightmapResolutionSplit = heightmapResolutionEditor / (int)Mathf.Sqrt((float)terrainChunks);
                            splitSizeFinal = (int)Mathf.Sqrt(terrainChunks);
                            totalImages = Mathf.RoundToInt(Mathf.Pow(gridPerTerrainEditor, 2)) * terrainChunks;
                            gridNumber = Mathf.RoundToInt(Mathf.Sqrt(totalImages));
                            chunkImageResolution = (imageResolutionEditor * gridNumber) / (int)Mathf.Sqrt((float)terrainChunks);
                    //    }
                    //    catch { }
                    //}
                }
                else if (worldModeIndex == 1)
                {
                    heightmapResolutionSplit = heightmapResolutionStreaming;
                    splitSizeFinal = 1;
                    gridStreamingWorld = (int)serverGrid;
                    totalImages = (int)Mathf.Pow(gridStreamingWorld, 2);
                    gridNumber = gridStreamingWorld;

                    //terrainChunks = 1;
                    terrainChunks = totalImages;
                }


                //***********************************************************************************************************************************************************************


                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                CoordinatesRanges();

                GUILayout.Space(5);

                if (worldModeIndex == 1)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nSTREAMING-SERVER SETTINGS\n","Define heightmap and satellite imagery resolution for each tile and total number of terrains which are going to be downloaded in server folder"));
                    GUI.backgroundColor = Color.white;

                    showServerSection = EditorGUILayout.Foldout(showServerSection, "");

                    if (showServerSection)
                    {
                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("TILES GRID", "Select number of terrain tiles for area generation"));

                        serverGrid = (ServerGrid)EditorGUILayout.EnumPopup(serverGrid);
                        splitSizeNew = (int)serverGrid;
                        totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));

                        GUI.backgroundColor = Color.green;

                        GUILayout.Space(5);

                        EditorGUILayout.HelpBox(totalTerrainsNew.ToString(), MessageType.None);
                        GUI.backgroundColor = Color.gray;
                        EditorGUILayout.HelpBox("TERRAINS", MessageType.None);
                        GUI.backgroundColor = Color.white;

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(40);

                        GUI.backgroundColor = Color.clear;
                        GUILayout.Button(heightMapLogo);
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("<<"))
                        {
                            heightmapResolutionStreaming /= 2;
                        }

                        GUILayout.Space(10);

                        if (GUILayout.Button(">>"))
                        {
                            heightmapResolutionStreaming *= 2;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("HEIGHTMAP RESOLUTION", "Heightmap resolution per tile"));
                        heightmapResolutionStreaming = EditorGUILayout.IntSlider(Mathf.ClosestPowerOfTwo(heightmapResolutionStreaming), 32, 1024);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        GUI.color = Color.green;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        GUIStyle myStyle = new GUIStyle(GUI.skin.box);
                        myStyle.fontSize = 20;
                        //myStyle.normal.textColor = Color.black;

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = GUILayoutUtility.GetLastRect().width - 50;
                        rectToggle.width = 100;
                        rectToggle.height = 30;

                        GUI.Box(rectToggle, new GUIContent(heightmapResolutionStreaming.ToString()), myStyle);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.color = Color.white;

                        GUILayout.Space(100);

                        GUI.backgroundColor = Color.clear;
                        GUILayout.Button(landMapLogo);
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("<<"))
                        {
                            imageResolutionStreaming /= 2;
                        }

                        GUILayout.Space(10);

                        if (GUILayout.Button(">>"))
                        {
                            imageResolutionStreaming *= 2;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("SATELLITE IMAGE RESOLUTION", "Satellite image resolution per tile"));
                        imageResolutionStreaming = EditorGUILayout.IntSlider(Mathf.ClosestPowerOfTwo(imageResolutionStreaming), 32, 2048);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        GUI.color = Color.green;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        myStyle = new GUIStyle(GUI.skin.box);
                        myStyle.fontSize = 20;
                        //myStyle.normal.textColor = Color.black;

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = GUILayoutUtility.GetLastRect().width - 50;
                        rectToggle.width = 100;
                        rectToggle.height = 30;

                        GUI.Box(rectToggle, new GUIContent(imageResolutionStreaming.ToString()), myStyle);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.color = Color.white;

                        GUILayout.Space(100);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("SMOOTH STEPS", "Smoothing iterations applied on heightmap data tiles before saving to file in server"));
                        smoothIterations = EditorGUILayout.IntSlider(smoothIterations, 0, 10);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(60);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("HEIGHTMAP FORMAT", "Heightmap data format which is going to be saved as data tiles in server"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        formatIndex = GUILayout.SelectionGrid(formatIndex, formatMode, 3);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(20);

                        if (formatIndex != 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("Only RAW format is supported in TerraLand's Streaming system, so choosing other formats will not generate terrains in game. Use Raw format unless for personal use!", MessageType.Error);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }

                if (worldModeIndex == 0)
                {
                    CheckTerrainSizeUnits();

                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nENGINE RESOLUTION PRESETS\n", "Select one of the pre-defined presets which automatically sets resolutions.\n\nIf you select MANUAL, sections will be revealed and you can set parameters manually"));
                    GUI.backgroundColor = Color.white;

                    showResolutionPresetSection = EditorGUILayout.Foldout(showResolutionPresetSection, "");

                    if (showResolutionPresetSection)
                    {
                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("RESOLUTION MODE", MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(15);

                        if (engineModeIndex == 1)
                            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
                        else if (engineModeIndex == 2)
                            GUI.backgroundColor = new Color(1f, 0.6f, 0.4f);
                        else if (engineModeIndex == 3)
                            GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
                        else if (engineModeIndex == 4)
                            GUI.backgroundColor = new Color(1f, 0.4f, 0.0f);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUI.BeginChangeCheck();
                        engineModeIndex = GUILayout.SelectionGrid(engineModeIndex, engineMode, 5);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (engineModeIndex == 0)
                            {
                                //showNewTerrainSection = true;
                                showHeghtmapDownloaderSection = true;
                                showImageDownloaderSection = true;
                            }
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (engineModeIndex == 1 || engineModeIndex == 2 || engineModeIndex == 3 || engineModeIndex == 4)
                        {
                            //if(!splittedTerrains && !terrain)
                            //{
                            //    terrainSizeNewX  = areaSizeLat * 1000f;
                            //    terrainSizeNewZ  = areaSizeLon * 1000f;
                            //    constrainedAspect = true;
                            //}

                            textureOnFinish = 0;
                        }

                        if (engineModeIndex == 1)
                        {
                            //if (!splittedTerrains && !terrain)
                            {
                                enumValueNew = SizeNew._1;
                                splitSizeNew = (int)enumValueNew;
                                totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));
                            }

                            heightmapResolutionEditor = 512;
                            gridPerTerrainEditor = 1;
                            imageResolutionEditor = 512;

                            //showNewTerrainSection = false;
                            showHeghtmapDownloaderSection = false;
                            showImageDownloaderSection = false;
                        }
                        else if (engineModeIndex == 2)
                        {
                            //if (!splittedTerrains && !terrain)
                            {
                                enumValueNew = SizeNew._1;
                                splitSizeNew = (int)enumValueNew;
                                totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));
                            }

                            heightmapResolutionEditor = 1024;
                            gridPerTerrainEditor = 1;
                            imageResolutionEditor = 2048;

                            //showNewTerrainSection = false;
                            showHeghtmapDownloaderSection = false;
                            showImageDownloaderSection = false;
                        }
                        else if (engineModeIndex == 3)
                        {
                            //if (!splittedTerrains && !terrain)
                            {
                                enumValueNew = SizeNew._2x2;
                                splitSizeNew = (int)enumValueNew;
                                totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));
                            }

                            heightmapResolutionEditor = 2048;
                            gridPerTerrainEditor = 1;
                            imageResolutionEditor = 4096;

                            //showNewTerrainSection = false;
                            showHeghtmapDownloaderSection = false;
                            showImageDownloaderSection = false;
                        }
                        else if (engineModeIndex == 4)
                        {
                            //if (!splittedTerrains && !terrain)
                            {
                                enumValueNew = SizeNew._2x2;
                                splitSizeNew = (int)enumValueNew;
                                totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));
                            }

                            heightmapResolutionEditor = 4096;
                            gridPerTerrainEditor = 2;
                            imageResolutionEditor = 4096;

                            //showNewTerrainSection = false;
                            showHeghtmapDownloaderSection = false;
                            showImageDownloaderSection = false;
                        }

                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(40);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        if (engineModeIndex == 0)
                            EditorGUILayout.HelpBox("SET HEIGHTMAP & IMAGERY RESOLUTIONS MANUALLY", MessageType.Warning);
                        else
                            EditorGUILayout.HelpBox("AUTOMATIC RESOLUTIONS - SELECT MANUAL FOR CUSTOM RESOLUTIONS", MessageType.Warning);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(30);

                        GUI.backgroundColor = Color.gray;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Total Terrain Resolution: " + terrainResolutionTotal, MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Total Image Resolution: " + textureResolutionTotal, MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (totalTerrainsNew > 1)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("Chunk Terrain Resolution: " + terrainResolutionChunk, MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("Chunk Image Resolution: " + textureResolutionChunk, MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Total Terrains: " + totalTerrainsNew, MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Total Images: " + totalImages, MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.HelpBox(new GUIContent("\nAREA LOCATION\n", "Search for any address or location or insert arbitrary geo-coordinates on Earth and select your area on a 2D map to define world region"));
                GUI.backgroundColor = Color.white;

                showLocationSection = EditorGUILayout.Foldout(showLocationSection, "");

                if (showLocationSection)
                {
                    GUILayout.Space(30);

                    if (InteractiveMap.updateArea && mapWindow != null)
                    {
                        latitudeUser = InteractiveMap.map_latlong_center.latitude;
                        longitudeUser = InteractiveMap.map_latlong_center.longitude;
                    }

                    buttonStyle = new GUIStyle(EditorStyles.miniButton);
                    buttonStyle.fixedHeight = 40;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("SELECT AREA ON MAP", "Displays 2D map of the Earth for area selection"), buttonStyle))
                        ShowMapAndRefresh(false);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(40);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("ADDRESS/LOCATION", MessageType.None, true);
                    address = EditorGUILayout.TextField(address);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("SEARCH", "Converts inserted geo-coordinates to location names and addresses to select from the query results"), buttonStyle))
                    {
                        TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "Search");
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Search, address);
                        coords = GeoCoder.AddressToLatLong(Regex.Replace(address, @"\s+", string.Empty));
                        locations = GeoCoder.foundLocations;

                        if (coords != null && locations != null)
                        {
                            if (coords[0] != null)
                            {
                                latitudeUser = coords[0].x;
                                longitudeUser = coords[0].y;
                                ShowMapAndRefresh(false);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(20);

                    if (coords != null && locations != null)
                    {
                        for (int i = 0; i < coords.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            GUI.backgroundColor = Color.gray;
                            EditorGUILayout.HelpBox(locations[i], MessageType.None, true);
                            GUI.backgroundColor = Color.white;

                            GUILayout.Space(10);
                            EditorGUILayout.TextArea(coords[i].x.ToString() + "   " + coords[i].y.ToString());

                            if (GUILayout.Button(new GUIContent("SET LOCATION", "Set this location as the center of our world and display it on map")))
                            {
                                latitudeUser = coords[i].x;
                                longitudeUser = coords[i].y;
                                ShowMapAndRefresh(false);
                            }

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(10);
                        }
                    }

                    if (!GeoCoder.recognized)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Address/Location Is Not Recognized", MessageType.Error, true);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                        GUILayout.Space(5);

                    GUILayout.Space(20);

                    if (!userCoordinates)
                    {
                        AreaBounds.Wgs84CenterPtToBBox(latitudeUser, longitudeUser, areaSizeLat, areaSizeLon, out top, out left, out bottom, out right);
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = new Color(0.0f, 0.5f, 1f, 1f);
                    EditorGUILayout.HelpBox(new GUIContent("LATITUDE", "Latitude of the center point in selected area"));
                    GUI.backgroundColor = Color.white;
                    latitudeUser = EditorGUILayout.DoubleField(latitudeUser);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = new Color(0.0f, 0.5f, 1f, 1f);
                    EditorGUILayout.HelpBox(new GUIContent("LONGITUDE", "Longitude of the center point in selected area"));
                    GUI.backgroundColor = Color.white;
                    longitudeUser = EditorGUILayout.DoubleField(longitudeUser);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    CoordinatesGUI();

                    //GUILayout.Space(40);
                    //
                    //EditorGUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();
                    //if (GUILayout.Button("\nGET ADDRESS FROM COORDINATES\n", buttonStyle))
                    //{
                    //    geoReversedAddress = GeoCoder.LatLongToAddress(longitudeUser, latitudeUser);
                    //}
                    //GUILayout.FlexibleSpace();
                    //EditorGUILayout.EndHorizontal();
                    //
                    //GUILayout.Space(10);
                    //
                    //if (!string.IsNullOrEmpty(geoReversedAddress))
                    //{
                    //    EditorGUILayout.BeginHorizontal();
                    //    GUILayout.FlexibleSpace();
                    //    EditorGUILayout.TextField(geoReversedAddress);
                    //    GUILayout.FlexibleSpace();
                    //    EditorGUILayout.EndHorizontal();
                    //}

                    GUILayout.Space(30);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (mapWindow != null)
                        EditorGUILayout.HelpBox("Close Interactive Map if you want\nto manually insert coordinates!", MessageType.Info);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(100);
                }
                else
                    GUILayout.Space(15);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.HelpBox(new GUIContent("\nAREA SIZE\n", "Select Area Size in kilometers and set world size in units"));
                GUI.backgroundColor = Color.white;

                showAreaSizeSection = EditorGUILayout.Foldout(showAreaSizeSection, "");

                if (showAreaSizeSection)
                {
                    MetricsGUI();
                    GUILayout.Space(100);
                }
                else
                    GUILayout.Space(15);

                if (worldModeIndex == 0)
                //if (worldModeIndex == 0 && engineModeIndex == 0)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nTERRAIN SETTINGS\n", "Define number of terrain tiles and each tile's quality and texturing resolution"));
                    GUI.backgroundColor = Color.white;
                    showNewTerrainSection = EditorGUILayout.Foldout(showNewTerrainSection, "");

                    if (showNewTerrainSection)
                    {
                        if (engineModeIndex == 0)
                        {
                            GUILayout.Space(30);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox(new GUIContent("TILES GRID", "Select of number of terrain tiles for area generation"));

                            enumValueNew = (SizeNew)EditorGUILayout.EnumPopup(enumValueNew);
                            splitSizeNew = (int)enumValueNew;
                            totalTerrainsNew = Mathf.RoundToInt(Mathf.Pow(splitSizeNew, 2));

                            GUI.backgroundColor = Color.green;

                            GUILayout.Space(5);

                            if (splitSizeNew > 1)
                            {
                                EditorGUILayout.HelpBox(totalTerrainsNew.ToString(), MessageType.None);
                                GUI.backgroundColor = Color.gray;
                                EditorGUILayout.HelpBox("TERRAINS", MessageType.None);
                                GUI.backgroundColor = Color.white;
                            }
                            else
                            {
                                GUI.backgroundColor = Color.gray;
                                EditorGUILayout.HelpBox("SINGLE TERRAIN", MessageType.None);
                                GUI.backgroundColor = Color.white;
                            }

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            if (!Mathf.IsPowerOfTwo(splitSizeNew))
                            {
                                GUILayout.Space(20);

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.HelpBox(dataResamplingWarning, MessageType.Warning);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }

                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox(new GUIContent("PIXEL ERROR QUALITY", "Pixel error value for each tile to define LOD & tessellation of surface based on player position. Lower values will bring higher poly count and less LOD switching"));
                            pixelError = EditorGUILayout.Slider(pixelError, 1f, 200f);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox(new GUIContent("SPLATMAP RESOLUTION", "Define resolution of splatmap used in terrain data to blend applied textures on terrain tiles. Higher values will bring more room for later blending operations on painted textures"));
                            alphamapResolution = EditorGUILayout.IntSlider(Mathf.ClosestPowerOfTwo(alphamapResolution), 16, 4096);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(50);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("EXISTING TERRAIN(S) IN SCENE", "If there are already created terrain(s) in scene, they will be revealed here or you can drag & drop previously created terrains in these slots for further processing"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("SINGLE TERRAIN", MessageType.None);
                        terrain = EditorGUILayout.ObjectField(terrain, typeof(Terrain), true) as Terrain;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("TERRAIN CHUNKS", MessageType.None);
                        splittedTerrains = EditorGUILayout.ObjectField(splittedTerrains, typeof(GameObject), true) as GameObject;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (splittedTerrains)
                                CheckTerrainChunks();
                        }

                        GUILayout.Space(100);
                    }
                    else
                    {
                        GUILayout.Space(15);

                        if (splitSizeNew == 0)
                            splitSizeNew = 1;

                        if (totalTerrainsNew == 0)
                            totalTerrainsNew = 1;

                        if (terrainSizeNewX == 0 || terrainSizeNewZ == 0)
                            SetUnitsTo1Meter();
                    }
                }

                if (worldModeIndex == 0 && engineModeIndex == 0)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nHEIGHTMAP DOWNLOADER\n", "Set heightmap resolution, bathymetry data, smoothing iterations, elevation exaggeration and data save options from here"));
                    GUI.backgroundColor = Color.white;
                    showHeghtmapDownloaderSection = EditorGUILayout.Foldout(showHeghtmapDownloaderSection, "");

                    if (showHeghtmapDownloaderSection)
                    {
                        GUILayout.Space(30);

                        GUI.backgroundColor = Color.clear;
                        GUILayout.Button(heightMapLogo);
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(40);

                        GUI.backgroundColor = Color.clear;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("INCLUDE BATHYMETRY (UNDER WATER)", "If enabled, TerraLand obtains bathymetry data merged with above water heights"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = (rectToggle.width / 2f) + 120f;

                        isTopoBathy = EditorGUI.Toggle(rectToggle, isTopoBathy);

                        GUILayout.Space(30);

                        GUI.backgroundColor = Color.gray;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("\nHEIGHTMAP RESOLUTION\n", "Total heightmap resolution for the selected area"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(20);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("<<"))
                        {
                            heightmapResolutionEditor /= 2;
                        }

                        GUILayout.Space(10);

                        if (GUILayout.Button(">>"))
                        {
                            heightmapResolutionEditor *= 2;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("PIXELS", MessageType.None);
                        heightmapResolutionEditor = EditorGUILayout.IntSlider(Mathf.ClosestPowerOfTwo(heightmapResolutionEditor), 32, maxHeightmapResolution);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        GUI.color = Color.green;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        GUIStyle myStyle = new GUIStyle(GUI.skin.box);
                        myStyle.fontSize = 20;
                        //myStyle.normal.textColor = Color.black;

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = GUILayoutUtility.GetLastRect().width - 50;
                        rectToggle.width = 100;
                        rectToggle.height = 30;

                        GUI.Box(rectToggle, new GUIContent(heightmapResolutionEditor.ToString()), myStyle);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.color = Color.white;

                        GUILayout.Space(30);

                        //Check if Terrain resolution is not below 32
                        if ((heightmapResolutionEditor / splitSizeNew) < 32)
                        {
                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("INCREASE HEIGHTMAP RESOLUTION", MessageType.Error);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (splittedTerrains && terrainResolutionChunk < 32)
                        {
                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("INCREASE HEIGHTMAP RESOLUTION", MessageType.Error);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        //Check if Terrain resolution is not above maximum range
                        if ((heightmapResolutionEditor / splitSizeNew) > maxHeightmapResolution)
                        {
                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("DECREASE HEIGHTMAP RESOLUTION", MessageType.Warning);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (splittedTerrains && heightmapResolutionSplit > maxHeightmapResolution)
                        {
                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("DECREASE HEIGHTMAP RESOLUTION", MessageType.Warning);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (terrain && heightmapResolutionEditor > maxHeightmapResolution)
                        {
                            GUILayout.Space(20);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("DECREASE HEIGHTMAP RESOLUTION", MessageType.Warning);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(40);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("SMOOTH STEPS", "Number of smoothing operation steps on heightmap surface to remove unwanted terraces and bandings artifacts. Value of 0 does not perform any smoothness operations and will bring original data obtained from servers. Atleast 1 step is required for most cases."));
                        smoothIterations = EditorGUILayout.IntSlider(smoothIterations, 0, 10);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("ELEVATION EXAGGERATION", "Increases or decreases overall height values in heightmap data to define final heights on terrain(s)"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("X", MessageType.None);
                        elevationExaggeration = EditorGUILayout.Slider(elevationExaggeration, 0.5f, 40f);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("SAVE DATA FILES", "Saves physical files of heightmap if any of the formats of ascii, raw and/or ESRI's tiff is selected for later usage. Heightmap data will be always embedded in terrain data so if none of the formats are selected, you can then export its heightmap using heightmap export tools"));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUI.backgroundColor = Color.clear;
                        EditorGUILayout.HelpBox("ASCII", MessageType.None);
                        GUI.backgroundColor = Color.white;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = (rectToggle.width / 2f) + 25f;
                        saveTerrainDataASCII = EditorGUI.Toggle(rectToggle, saveTerrainDataASCII);

                        GUILayout.Space(5);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUI.backgroundColor = Color.clear;
                        EditorGUILayout.HelpBox("RAW", MessageType.None);
                        GUI.backgroundColor = Color.white;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = (rectToggle.width / 2f) + 25f;
                        saveTerrainDataRAW = EditorGUI.Toggle(rectToggle, saveTerrainDataRAW);

                        GUILayout.Space(5);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUI.backgroundColor = Color.clear;
                        EditorGUILayout.HelpBox("TIFF", MessageType.None);
                        GUI.backgroundColor = Color.white;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = (rectToggle.width / 2f) + 25f;
                        saveTerrainDataTIFF = EditorGUI.Toggle(rectToggle, saveTerrainDataTIFF);

                        if (!saveTerrainDataASCII)
                        {
                            if (saveTerrainDataRAW || saveTerrainDataTIFF)
                            {
                                GUILayout.Space(30);

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.HelpBox("Use ASCII format for Georeferenced data collection between GIS programs", MessageType.Info);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }
                        }

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }

                if (worldModeIndex == 0 && engineModeIndex == 0)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nSATELLITE IMAGE DOWNLOADER\n", "Set satellite image resolution and number of images on each terrain tile and define if texturing must be applied on generating terrains"));
                    GUI.backgroundColor = Color.white;
                    showImageDownloaderSection = EditorGUILayout.Foldout(showImageDownloaderSection, "");

                    if (showImageDownloaderSection)
                    {
                        GUILayout.Space(30);

                        GUI.backgroundColor = Color.clear;
                        GUILayout.Button(landMapLogo);
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(40);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("GRID PER TERRAIN", "Grid value for each tile which defines total number of textures on each terrain"));
                        gridPerTerrainEditor = EditorGUILayout.IntSlider(gridPerTerrainEditor, 1, 32);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(60);

                        GUI.backgroundColor = Color.gray;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("\nIMAGE RESOLUTION\n", MessageType.None);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(20);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("<<"))
                        {
                            imageResolutionEditor /= 2;
                        }

                        GUILayout.Space(10);

                        if (GUILayout.Button(">>"))
                        {
                            imageResolutionEditor *= 2;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("PIXELS", MessageType.None);
                        imageResolutionEditor = EditorGUILayout.IntSlider(Mathf.ClosestPowerOfTwo(imageResolutionEditor), 32, maxSatelliteImageResolution);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        GUI.color = Color.green;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        GUIStyle myStyle = new GUIStyle(GUI.skin.box);
                        myStyle.fontSize = 20;
                        //myStyle.normal.textColor = Color.black;

                        rectToggle = GUILayoutUtility.GetLastRect();
                        rectToggle.x = GUILayoutUtility.GetLastRect().width - 50;
                        rectToggle.width = 100;
                        rectToggle.height = 30;

                        GUI.Box(rectToggle, new GUIContent(imageResolutionEditor.ToString()), myStyle);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.color = Color.white;

                        GUILayout.Space(100);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("APPLY SATELLITE IMAGES ON TERRAIN(S)", MessageType.None, true);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        if (textureOnFinish == 0)
                            GUI.backgroundColor = Color.green;
                        else
                            GUI.backgroundColor = Color.red;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        textureOnFinish = GUILayout.SelectionGrid(textureOnFinish, editMode, 2);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(50);

                        extraOptions = EditorGUILayout.Foldout(extraOptions, "OTHER OPTIONS");

                        if (extraOptions)
                        {
                            GUILayout.Space(10);

                            GUI.backgroundColor = Color.clear;
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("IMAGE QUALITY COMPRESSION", MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUI.backgroundColor = Color.white;

                            GUILayout.Space(5);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("QUALITY", MessageType.None);
                            compressionQuality = EditorGUILayout.IntSlider(compressionQuality, 0, 100);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(20);

                            GUI.backgroundColor = Color.clear;
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("IMPORT COMPRESSION", MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUI.backgroundColor = Color.white;

                            rectToggle = GUILayoutUtility.GetLastRect();
                            rectToggle.x = (rectToggle.width / 2f) + 75f;

                            compressionActive = EditorGUI.Toggle(rectToggle, compressionActive);

                            if (compressionActive)
                            {
                                this.ShowNotification(new GUIContent("SLOWER IMPORTING"));

                                frames2++;
                                if (frames2 > 50)
                                    this.RemoveNotification();
                            }
                            else
                                frames2 = 0;

                            GUILayout.Space(5);

                            GUI.backgroundColor = Color.clear;
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("AUTO IMAGE SCALING", MessageType.None);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUI.backgroundColor = Color.white;

                            rectToggle = GUILayoutUtility.GetLastRect();
                            rectToggle.x = (rectToggle.width / 2f) + 75f;

                            autoScale = EditorGUI.Toggle(rectToggle, autoScale);

                            if (autoScale)
                            {
                                this.ShowNotification(new GUIContent("SLOWER PROCESSING"));

                                frames3++;
                                if (frames3 > 50)
                                    this.RemoveNotification();
                            }
                            else
                                frames3 = 0;

                            if (compressionQuality < 100 || compressionActive || autoScale)
                            {
                                GUILayout.Space(30);

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.HelpBox(imageImportingWarning, MessageType.Warning);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }

                            GUILayout.Space(50);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("ANISOTROPIC", MessageType.None);
                            anisotropicFilter = EditorGUILayout.IntSlider(anisotropicFilter, 0, 9);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(30);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox(" ASYNC CALLS", MessageType.None, true);
                            maxAsyncCalls = EditorGUILayout.IntSlider(maxAsyncCalls, 2, 50);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }

                if (worldModeIndex == 0)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nFAILED IMAGES DOWNLOADER\n", "Drag & drop previously downloaded \"Satellite Images\" folder to recover failed download tiles if existing"));
                    GUI.backgroundColor = Color.white;

                    showFailedDownloaderSection = EditorGUILayout.Foldout(showFailedDownloaderSection, "");

                    if (showFailedDownloaderSection)
                    {
                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox(new GUIContent("\"SATELLITE IMAGES\" FOLDER", "Drag & drop previously downloaded \"Satellite Images\" folder to recover failed download tiles if existing"));

                        EditorGUI.BeginChangeCheck();
                        failedFolder = EditorGUILayout.ObjectField(failedFolder, typeof(UnityEngine.Object), true) as UnityEngine.Object;
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (failedFolder)
                                CheckFailedImagesGUI();
                        }

                        try
                        {
                            if (failedFolder)
                                attr = File.GetAttributes(AssetDatabase.GetAssetPath(failedFolder));

                            if (failedFolder != null && (attr & FileAttributes.Directory) != FileAttributes.Directory)
                            {
                                EditorUtility.DisplayDialog("FOLDER NOT AVAILABLE", "Drag & drop a folder which contains failed downloaded satellite images.", "Ok");
                                failedFolder = null;
                                return;
                            }
                        }
                        catch { }

                        GUI.backgroundColor = Color.clear;
                        if (failedFolder && failedImageAvailable)
                        {
                            int currentSecond = DateTime.Now.Second;

                            if (currentSecond % 2 == 0)
                                GUI.color = Color.clear;
                            else
                                GUI.color = Color.white;

                            GUILayout.Button(statusRed);
                        }
                        else
                            GUILayout.Button(statusGreen);
                        GUI.backgroundColor = Color.white;

                        GUI.color = Color.white;

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(10);

                        if (failedFolder)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            if (totalFailedImages == 0)
                                EditorGUILayout.HelpBox("NO FAILED IMAGES", MessageType.None);
                            else if (totalFailedImages == 1)
                                EditorGUILayout.HelpBox("1 FAILED IMAGE", MessageType.Warning);
                            else
                                EditorGUILayout.HelpBox(totalFailedImages.ToString() + "  FAILED IMAGES", MessageType.Warning);

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }

                        GUILayout.Space(25);

                        GUI.backgroundColor = Color.gray;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("\nGET FAILED IMAGES\n"))
                        {
                            dynamicWorld = false;
                            failedDownloading = true;
                            SetupDataContent();
                            DownloadFailedImageTiles(true);
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }
                else if (worldModeIndex == 1)
                {
                    GUI.backgroundColor = Color.gray;
                    EditorGUILayout.HelpBox(new GUIContent("\nFAILED TILES DOWNLOADER\n", "Pressing \"GET FAILED TILES\" button will ask you to select the root folder of the server to recover failed downloading tiles"));
                    GUI.backgroundColor = Color.white;

                    showFailedDownloaderSection = EditorGUILayout.Foldout(showFailedDownloaderSection, "");

                    if (showFailedDownloaderSection)
                    {
                        GUILayout.Space(30);

                        GUI.backgroundColor = Color.gray;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent("\nGET FAILED TILES\n", "It will ask you to select the root folder of the previously generated server in order to recover failed downloading tiles if existing")))
                        {
                            serverPath = EditorUtility.OpenFolderPanel("Select the root folder of the server to download failed heightmap & image tiles", projectPath, "TerraLandServer");
                            SetupDataContentServer(serverPath);
                            CheckFailedTilesElevation();
                            CheckFailedTilesImagery();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;

                        GUILayout.Space(100);
                    }
                    else
                        GUILayout.Space(15);
                }

                GUILayout.Space(40);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                buttonStyle = new GUIStyle(EditorStyles.miniButton);
                buttonStyle.fixedWidth = 130;
                buttonStyle.fixedHeight = 40;

                if (dynamicWorld)
                {
                    if (!terrainGenerationstarted || cancelOperationHeightmapDownloader)
                    {
                        if (GUILayout.Button(new GUIContent("GENERATE HEIGHTS", "Generate terrain heightmap only without texturing"), buttonStyle))
                        {
                            TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_HEIGHTS");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Streaming");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionStreaming.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionStreaming.ToString());

                            if (isTopoBathy)
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                            else
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                            CheckServerMessages();

                            serverSetUpElevation = false;
                            serverSetUpImagery = true;

                            SetServerLocation();
                            SetupDataContent();
                            InitializeDownloader();
                            SetupDownloaderElevation();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("CANCEL", buttonStyle))
                        {
                            if (EditorUtility.DisplayDialog("CANCELLING DOWNLOAD", "Are you sure you want to cancel downloading?", "No", "Yes"))
                                return;

                            Repaint();
                            terrainGenerationstarted = false;
                            cancelOperationHeightmapDownloader = true;
                            showProgressElevation = false;
                            showProgressGenerateASCII = false;
                            showProgressGenerateRAW = false;
                            showProgressSmoothen = false;
                            showProgressSmoothenOperation = false;
                            convertingElevationTiles = false;
                            stitchingElevationTiles = false;

                            if (Directory.Exists(projectPath + "Temporary Elevation Data"))
                                Directory.Delete(projectPath + "Temporary Elevation Data", true);

                            CheckImageDownloaderAndRecompile();
                        }
                    }
                }
                else
                {
                    if (!terrainGenerationstarted || cancelOperationHeightmapDownloader)
                    {
                        if (GUILayout.Button(new GUIContent("GENERATE HEIGHTS", "Generate terrain heightmap only without texturing"), buttonStyle))
                        {
                            TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_HEIGHTS");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Static");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionEditor.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionEditor.ToString());

                            if (isTopoBathy)
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                            else
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                            CheckServerMessages();

                            SetupDataContent();
                            InitializeDownloader();
                            CheckHeightmapResolution();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("CANCEL", buttonStyle))
                        {
                            if (EditorUtility.DisplayDialog("CANCELLING DOWNLOAD", "Are you sure you want to cancel downloading?", "No", "Yes"))
                                return;

                            Repaint();
                            terrainGenerationstarted = false;
                            cancelOperationHeightmapDownloader = true;
                            showProgressElevation = false;
                            showProgressGenerateASCII = false;
                            showProgressGenerateRAW = false;
                            showProgressSmoothen = false;
                            showProgressSmoothenOperation = false;
                            convertingElevationTiles = false;
                            stitchingElevationTiles = false;

                            if (Directory.Exists(projectPath + "Temporary Elevation Data"))
                                Directory.Delete(projectPath + "Temporary Elevation Data", true);

                            CheckImageDownloaderAndRecompile();
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);

                GUI.color = Color.green;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUIStyle myStyle2 = new GUIStyle(GUI.skin.box);
                myStyle2.fontSize = 12;
                //myStyle2.normal.textColor = Color.black;
                myStyle2.alignment = TextAnchor.MiddleCenter;

                rectToggle = GUILayoutUtility.GetLastRect();
                float offset = 20;
                float boxWidth = 200;
                float boxHeight = 25;
                rectToggle.x = offset;
                rectToggle.width = boxWidth;
                rectToggle.height = boxHeight;

                if (!dynamicWorld)
                    terrainResolutionTotal = heightmapResolutionEditor;
                else
                    terrainResolutionTotal = heightmapResolutionStreaming * gridStreamingWorld;

                if (splitSizeNew > 1)
                    GUI.Box(rectToggle, new GUIContent("Total:" + terrainResolutionTotal + "  Tile:" + terrainResolutionChunk, "Displays total and each tile's heightmap resolution"), myStyle2);
                else
                    GUI.Box(rectToggle, new GUIContent("Total:" + terrainResolutionTotal, "Displays total heightmap resolution for the terrain"), myStyle2);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;

                if (!terrain && (splittedTerrains || splitSizeNew > 1))
                {
                    if (!dynamicWorld)
                    {
                        if (splittedTerrains)
                        {
                            if (!Mathf.IsPowerOfTwo(croppedTerrains.Count))
                                terrainResolutionChunk = ((Mathf.NextPowerOfTwo(heightmapResolutionEditor / splitSizeFinal)) / 2);
                            else
                                terrainResolutionChunk = heightmapResolutionSplit;
                        }
                        else
                        {
                            if (!Mathf.IsPowerOfTwo(splitSizeNew))
                                terrainResolutionChunk = ((Mathf.NextPowerOfTwo(heightmapResolutionEditor / splitSizeNew)) / 2);
                            else
                                terrainResolutionChunk = heightmapResolutionEditor / splitSizeNew;
                        }
                    }
                    else
                        terrainResolutionChunk = heightmapResolutionStreaming;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (dynamicWorld)
                {
                    if (GUILayout.Button(new GUIContent(generateServerButton, "Starts downloading of needed tiles for the defined area and put them in a server so that we can stream the tiles in runtime based on player position")))
                    {
                        TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_TERRAINS");
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Streaming");
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionStreaming.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionStreaming.ToString());

                        if (isTopoBathy)
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                        else
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                        CheckServerMessages();

                        serverSetUpElevation = false;
                        serverSetUpImagery = false;
                        failedDownloading = false;

                        SetServerLocation();
                        SetupDataContent();
                        SetupImagery();
                        InitializeDownloader();
                        SetupDownloaderElevation();
                        GetSatelliteImages();
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(generateTerrainButton, "Generates terrain(s) in the editor which you can edit them at anytime in the editor and before the level start")))
                    {
                        TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_TERRAINS");
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Static");
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionEditor.ToString());
                        TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionEditor.ToString());

                        if (isTopoBathy)
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                        else
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                        CheckServerMessages();

                        failedDownloading = false;
                        SetupDataContent();
                        CheckHeightmapResolution();
                        SetupImagery();
                        if (cancelOperation) return;
                        InitializeDownloader();
                        GetSatelliteImages();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (dynamicWorld)
                {
                    if (!imageDownloadingStarted || cancelOperation)
                    {
                        if (GUILayout.Button(new GUIContent("GENERATE IMAGES", "Download satellite images only without getting the heightmaps"), buttonStyle))
                        {
                            TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_IMAGES");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Streaming");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionStreaming.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionStreaming.ToString());

                            if (isTopoBathy)
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                            else
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                            CheckServerMessages();

                            serverSetUpElevation = true;
                            serverSetUpImagery = false;
                            failedDownloading = false;

                            SetServerLocation();
                            SetupDataContent();
                            SetupImagery();
                            InitializeDownloader();
                            GetSatelliteImages();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("CANCEL", buttonStyle))
                        {
                            if (EditorUtility.DisplayDialog("CANCELLING DOWNLOAD", "Are you sure you want to cancel downloading?", "No", "Yes"))
                                return;

                            Repaint();
                            imageDownloadingStarted = false;
                            cancelOperation = true;
                            showProgressImagery = false;

                            if (!failedDownloading)
                            {
                                if (Directory.Exists(projectPath + "Temporary Imagery Data"))
                                    Directory.Delete(projectPath + "Temporary Imagery Data", true);
                            }

                            CheckHeightmapDownloaderAndRecompile();
                            FinalizeTerrainImagery();
                        }
                    }
                }
                else
                {
                    if (!imageDownloadingStarted || cancelOperation)
                    {
                        if (GUILayout.Button(new GUIContent("GENERATE IMAGES", "Download satellite images only without getting the heightmaps"), buttonStyle))
                        {
                            TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "GENERATE_IMAGES");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Mode, "Static");
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.MapSource, InteractiveMap.mapSource.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.LatLon, latitudeUser.ToString() + "_" + longitudeUser.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Grid, enumValueNew.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params, EventAction.AreaSize, areaSizeLat.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "HeihtmapResolution", heightmapResolutionEditor.ToString());
                            TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "ImageResolution", imageResolutionEditor.ToString());
                            if (isTopoBathy)
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "True");
                            else
                                TFeedback.FeedbackEvent(EventCategory.Params.ToString(), "TopoBathy", "False");

                            CheckServerMessages();

                            failedDownloading = false;
                            SetupDataContent();
                            SetupImagery();
                            if (cancelOperation) return;
                            InitializeDownloader();
                            GetSatelliteImages();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("CANCEL", buttonStyle))
                        {
                            if (EditorUtility.DisplayDialog("CANCELLING DOWNLOAD", "Are you sure you want to cancel downloading?", "No", "Yes"))
                                return;

                            Repaint();
                            imageDownloadingStarted = false;
                            cancelOperation = true;
                            showProgressImagery = false;

                            if (!failedDownloading)
                            {
                                if (Directory.Exists(projectPath + "Temporary Imagery Data"))
                                    Directory.Delete(projectPath + "Temporary Imagery Data", true);
                            }

                            CheckHeightmapDownloaderAndRecompile();
                            FinalizeTerrainImagery();
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);

                GUI.color = Color.green;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                rectToggle = GUILayoutUtility.GetLastRect();
                rectToggle.x = windowWidth - boxWidth - offset;
                rectToggle.width = boxWidth;
                rectToggle.height = boxHeight;

                if (!dynamicWorld)
                {
                    //if (terrain)
                    //    textureResolutionTotal = imageResolutionEditor * gridPerTerrainEditor;
                    //else if (splittedTerrains)
                    //    textureResolutionTotal = imageResolutionEditor * gridPerTerrainEditor * splitSizeFinal;
                    //else
                        textureResolutionTotal = imageResolutionEditor * gridPerTerrainEditor * splitSizeNew;
                }
                else
                    textureResolutionTotal = imageResolutionStreaming * gridStreamingWorld;

                if (splitSizeNew > 1)
                    GUI.Box(rectToggle, new GUIContent("Total:" + textureResolutionTotal + "  Tile:" + textureResolutionChunk, "Displays total and each tile's satellite image resolution"), myStyle2);
                else
                    GUI.Box(rectToggle, new GUIContent("Total:" + textureResolutionTotal, "Displays total satellite image resolution for the terrain"), myStyle2);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;

                if (!terrain && (splittedTerrains || splitSizeNew > 1))
                {
                    if (!dynamicWorld)
                    {
                        if (splittedTerrains)
                            textureResolutionChunk = chunkImageResolution;
                        else
                            textureResolutionChunk = imageResolutionEditor * gridPerTerrainEditor;
                    }
                    else
                        textureResolutionChunk = imageResolutionStreaming;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(40);
        }

        public static void OpenWebPage(string message, string title, string webaddress)
        {
            if (EditorUtility.DisplayDialog(title, message, "OK", "Cancel"))
            {
                try
                {
                    Process.Start(webaddress);
                }
                catch { }
            }
        }

        public static void CheckServerMessages()
        {
            if (!string.IsNullOrEmpty(ConnectionsManager.NewVersionWebPage))
                OpenWebPage("New version available. Do you wish to update to the latest version?", "TerraLand", ConnectionsManager.NewVersionWebPage);

            if (TProjectSettings.LastTeamMessageNum < ConnectionsManager.NewMessageIndex)
            {
                TProjectSettings.LastTeamMessageNum = ConnectionsManager.NewMessageIndex;
                EditorUtility.DisplayDialog("TerraLand", ConnectionsManager.Message, "OK");
            }
        }

        private void CoordinatesRanges()
        {
            if (latitudeUser > 90)
                latitudeUser = 90;
            else if (latitudeUser < -90)
                latitudeUser = -90;

            if (longitudeUser > 180)
                longitudeUser = 180;
            else if (longitudeUser < -180)
                longitudeUser = -180;

            if (top > 90)
                top = 90;
            else if (top < -89.999999)
                top = -89.999999;

            if (bottom < -90)
                bottom = -90;
            else if (bottom > 89.999999)
                bottom = 89.999999;

            if (right > 180)
                right = 180;
            else if (right < -179.999999)
                right = -179.999999;

            if (left < -180)
                left = -180;
            else if (left > 179.999999)
                left = 179.999999;

            if (bottom >= top)
                bottom = top - 0.000001;

            if (left >= right)
                left = right - 0.000001;
        }

        private void SetServerLocation()
        {
            serverPath = EditorUtility.OpenFolderPanel("Select a folder on your computer to create server", projectPath, "TerraLand Server");
        }

        private void CheckFailedTilesElevation()
        {
            // Check for failed Elevation tiles
            if (!string.IsNullOrEmpty(directoryPathElevation))
            {
                CheckFailedHeightmapsGUIServer();

                if (!failedHeightmapAvailable)
                {
                    EditorUtility.DisplayDialog("NO FAILED HEIGHTMAPS", "There are no failed heightmaps in the selected server.\n\nNote: If any of the heightmap tiles has been downloaded incorrectly, you can rename its filename and include \"_Temp\" at the end of the name, then finally press GET FAILED TILES button again to redownload.", "Ok");
                    serverSetUpElevation = true;
                    return;
                }

                failedDownloading = true;
                GetPresetInfo();
                InitializeDownloader();
                SetupDownloaderElevation();
            }
            else
            {
                EditorUtility.DisplayDialog("NO SAVE LOCATION", "Select the root folder of the server to download Failed Imagery", "Ok");
                return;
            }
        }

        private void CheckFailedTilesImagery()
        {
            // Check for failed Image tiles
            if (!string.IsNullOrEmpty(directoryPathImagery))
            {
                CheckFailedImagesGUIServer();

                if (totalFailedImages == 0)
                {
                    EditorUtility.DisplayDialog("NO FAILED IMAGES", "There are no failed images in the selected server.\n\nNote: If any of the image tiles has been downloaded incorrectly, you can rename its filename and include \"_Temp\" at the end of the name, then finally press GET FAILED TILES button again to redownload.", "Ok");
                    serverSetUpImagery = true;
                    return;
                }

                failedDownloading = true;
                GetPresetInfo();
                SetupImagery();
                if (cancelOperation) return;
                InitializeDownloader();
                GetSatelliteImages();
            }
            else
            {
                EditorUtility.DisplayDialog("NO SAVE LOCATION", "Select the root folder of the server to download Failed Imagery", "Ok");
                return;
            }
        }

        private void MetricsGUI()
        {
            GUILayout.Space(30);

            EditorGUI.BeginDisabledGroup(userCoordinates);
            if (squareArea)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("EXTENTS", "World size in kilometers"));
                areaSizeLat = EditorGUILayout.Slider(areaSizeLat, 0.01f, 500.0f);
                EditorGUILayout.HelpBox("KM", MessageType.None, true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                areaSizeLon = areaSizeLat;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("LAT EXTENTS", "World's width (Latitude) in kilometers"));
                areaSizeLat = EditorGUILayout.Slider(areaSizeLat, 0.01f, 500.0f);
                EditorGUILayout.HelpBox("KM", MessageType.None, true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("LON EXTENTS", "World's length (Longitude) in kilometers"));
                areaSizeLon = EditorGUILayout.Slider(areaSizeLon, 0.01f, 500.0f);
                EditorGUILayout.HelpBox("KM", MessageType.None, true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(15);

            GUI.backgroundColor = Color.clear;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("SQUARE AREA", MessageType.None);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            rectToggle = GUILayoutUtility.GetLastRect();
            rectToggle.x = (rectToggle.width / 2f) + 65f;
            squareArea = EditorGUI.Toggle(rectToggle, squareArea);

            EditorGUI.EndDisabledGroup();

            if (userCoordinates)
            {
                GUILayout.Space(15);
                EditorGUILayout.HelpBox("USER-DEFINED COORDINATES option in AREA LOCATION section is selected for area definition! If you want to define area based on world metrics, turn that option off!", MessageType.Warning);
            }

            WorldUnits();
        }

        private void WorldUnits()
        {
            GUILayout.Space(50);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("WORLD UNITS IN SCENE", MessageType.None);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.HelpBox("X", MessageType.None);
            terrainSizeNewX = EditorGUILayout.FloatField(terrainSizeNewX);

            GUILayout.Space(20);

            EditorGUILayout.HelpBox("Y", MessageType.None);
            terrainSizeNewZ = EditorGUILayout.FloatField(terrainSizeNewZ);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("SCALE FACTOR", MessageType.None);
            scaleFactor = EditorGUILayout.Slider(scaleFactor, 0.001f, 100);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            SetWorldSize();

            GUILayout.Space(20);

            float unit2Meters = ((areaSizeLat * 1000f / terrainSizeNewZ) + (areaSizeLon * 1000f / terrainSizeNewX)) / 2f;
            string meterStr = "";
            string terrainNO = "";

            string unitStr = "Each unit is  ";

            if (unit2Meters > 1)
                meterStr = "  Meters ";
            else
                meterStr = "  Meter ";

            if (totalTerrainsNew > 1)
                terrainNO = "Each terrain is  ";
            else
                terrainNO = "Terrain is  ";

            float newTerrainSizeX = areaSizeLon / (float)splitSizeNew;
            float newTerrainSizeY = areaSizeLat / (float)splitSizeNew;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(unitStr + unit2Meters + meterStr + "\n\n" + terrainNO + newTerrainSizeX + " x " + newTerrainSizeY + "  KM", MessageType.Info);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void CoordinatesGUI()
        {
            //GUI.backgroundColor = Color.clear;
            //GUILayout.Button(landMapLogo);
            //GUI.backgroundColor = Color.white;

            GUILayout.Space(60);

            if (!userCoordinates)
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("TOP", "Top coordinate of selected area in Decimal Degrees format. Turn on USER-DEFINED COORDINATES option to insert arbitrary values"));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.DoubleField(top);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("LFT", "Left coordinate of selected area in Decimal Degrees format. Turn on USER-DEFINED COORDINATES option to insert arbitrary values"));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.DoubleField(left);

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.HelpBox(new GUIContent("RGT", "Right coordinate of selected area in Decimal Degrees format. Turn on USER-DEFINED COORDINATES option to insert arbitrary values"));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.DoubleField(right);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(new GUIContent("BTM", "Bottom coordinate of selected area in Decimal Degrees format. Turn on USER-DEFINED COORDINATES option to insert arbitrary values"));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.DoubleField(bottom);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("TOP", MessageType.None, true);
                GUI.backgroundColor = Color.white;
                top = EditorGUILayout.DoubleField(top);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("LFT", MessageType.None, true);
                GUI.backgroundColor = Color.white;
                left = EditorGUILayout.DoubleField(left);

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.HelpBox("RGT", MessageType.None, true);
                GUI.backgroundColor = Color.white;
                right = EditorGUILayout.DoubleField(right);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                GUI.backgroundColor = Color.gray;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("BTM", MessageType.None, true);
                GUI.backgroundColor = Color.white;
                bottom = EditorGUILayout.DoubleField(bottom);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                latitudeUser = (top + bottom) / 2.0d;
                longitudeUser = (left + right) / 2.0d;
            }

            GUILayout.Space(20);

            GUI.backgroundColor = Color.clear;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(new GUIContent("USER-DEFINED COORDINATES", "Insert arbitrary coordinates for Top, Left, Bottom & Right fields to define an area from custom geo-coordinates"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            rectToggle = GUILayoutUtility.GetLastRect();
            rectToggle.x = (rectToggle.width / 2f) + 85f;
            userCoordinates = EditorGUI.Toggle(rectToggle, userCoordinates);
        }

        private void SetUnitsTo1Meter()
        {
            terrainSizeNewX = areaSizeLon * 1000f;
            terrainSizeNewZ = areaSizeLat * 1000f;
        }

        private void CheckTerrainSizeUnits()
        {
            terrainSizeFactor = areaSizeLat / areaSizeLon;

            if (splittedTerrains)
            {
                float tsX = 0;
                float tsY = 0;
                bool error = false;

                foreach (Terrain tr in croppedTerrains)
                {
                    if (tr.terrainData == null)
                    {
                        error = true;
                        break;
                    }

                    tsX += tr.terrainData.size.x;
                    tsY += tr.terrainData.size.z;
                }

                if (!error)
                {
                    terrainSizeX = tsX;
                    terrainSizeY = tsY;
                }
            }
            else if (terrain)
            {
                if (terrain.terrainData != null)
                {
                    terrainSizeX = terrain.terrainData.size.x;
                    terrainSizeY = terrain.terrainData.size.z;
                }
            }
        }

        private void ThrowException(string message)
        {
            EditorUtility.ClearProgressBar();
            throw new Exception(message);
        }

        public void CheckHeightmapResolution()
        {
            //Check if Terrain resolution is not below 32
            if ((heightmapResolutionEditor / splitSizeNew) < 32)
                ThrowException("Heightmap Resolution Is Below \"32\" For Each Terrain.\n\nIncrease Heightmap Resolution To Avoid Empty Areas In Terrain Chunks!");
            else if (splittedTerrains && terrainResolutionChunk < 32)
                ThrowException("Heightmap Resolution Is Below \"32\" For Each Terrain.\n\nIncrease Heightmap Resolution To Avoid Empty Areas In Terrain Chunks!");
            else
            {
                //Check if Terrain resolution is not above maximum range & optionally continue
                if ((heightmapResolutionEditor / splitSizeNew) > maxHeightmapResolution)
                {
                    if (splitSizeNew > 1)
                    {
                        if (EditorUtility.DisplayDialog("HIGH TERRAIN RESOLUTION", "Heightmap Resolution Is Above \"" + maxHeightmapResolution + "\" For Each Terrain.\n\nOptionally You Can Press \"Continue\" And Have A High Value For Heightmap Resolution On Terrain Chunks In Cost Of Performance.", "Cancel", "Continue"))
                            return;

                        SetupDownloaderElevation();
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog("HIGH TERRAIN RESOLUTION", "Heightmap Resolution Is Above \"" + maxHeightmapResolution + "\" For Terrain.\n\nOptionally You Can Press \"Continue\" And Have A High Value For Heightmap Resolution On Terrain In Cost Of Performance.", "Cancel", "Continue"))
                            return;

                        SetupDownloaderElevation();
                    }
                }
                else if (splittedTerrains && heightmapResolutionSplit > maxHeightmapResolution)
                {
                    if (EditorUtility.DisplayDialog("HIGH TERRAIN RESOLUTION", "Heightmap Resolution Is Above \"" + maxHeightmapResolution + "\" For Each Terrain.\n\nOptionally You Can Press \"Continue\" And Have A High Value For Heightmap Resolution On Terrain Chunks In Cost Of Performance.", "Cancel", "Continue"))
                        return;

                    SetupDownloaderElevation();
                }
                else if (terrain && heightmapResolutionEditor > maxHeightmapResolution)
                {
                    if (EditorUtility.DisplayDialog("HIGH TERRAIN RESOLUTION", "Heightmap Resolution Is Above \"" + maxHeightmapResolution + "\" For Terrain.\n\nOptionally You Can Press \"Continue\" And Have A High Value For Heightmap Resolution On Terrain In Cost Of Performance.", "Cancel", "Continue"))
                        return;

                    SetupDownloaderElevation();
                }
                else
                    SetupDownloaderElevation();
            }
        }

        private void SetupDownloaderElevation()
        {
            convertingElevationTiles = false;
            stitchingElevationTiles = false;
            showProgressElevation = true;
            terrainGenerationstarted = true;
            cancelOperationHeightmapDownloader = false;
            progressBarElevation = 0;
            progressDATA = 0;
            progressGenerateASCII = 0;
            progressGenerateRAW = 0;
            smoothIterationProgress = 0;
            smoothProgress = 0;
            retries = 0;

            if (!dynamicWorld)
            {
                if (!terrain && !splittedTerrains)
                    GenerateNewTerrainObject();

                if (splittedTerrains)
                {
                    CheckTerrainChunks();
                    splitSizeFinal = (int)Mathf.Sqrt((float)croppedTerrains.Count);
                }
                else if (terrain)
                {
                    terrainChunks = 1;
                    splitSizeFinal = 1;
                }

                RemoveLightmapStatic();
            }

            if (!dynamicWorld)
                terrainResolutionDownloading = heightmapResolutionEditor + splitSizeFinal;
            else
                terrainResolutionDownloading = heightmapResolutionStreaming;

            topCorner = new List<float>();
            bottomCorner = new List<float>();
            leftCorner = new List<float>();
            rightCorner = new List<float>();

            InitElevationServerRequest();

            if (!dynamicWorld)
                ServerConnectHeightmap(0, 0);
            else
                GetHeightmaps();
        }

        private void InitElevationServerRequest()
        {
            mapserviceElevation = new TerraLandWorldElevation.TopoBathy_ImageServer();
            //mapserviceElevation.Timeout = WEBSERVICE_TIMEOUT;
            if (isTopoBathy) elevationURL = "https://elevation.arcgis.com/arcgis/services/WorldElevation/TopoBathy/ImageServer?token=";
            else elevationURL = "https://elevation.arcgis.com/arcgis/services/WorldElevation/Terrain/ImageServer?token=";
            GenerateToken();
            mapserviceElevation.Url = elevationURL + token;
        }

        private void CheckTerrainChunks()
        {
            if (splittedTerrains.transform.childCount == 0)
            {
                EditorUtility.DisplayDialog("UNAVAILABLE TERRAINS", "There are no terrains available in the selected game object.", "Ok");
                splittedTerrains = null;
                return;
            }
            else
            {
                int counter = 0;

                foreach (Transform t in splittedTerrains.transform)
                {
                    if (t.GetComponent<Terrain>() != null)
                    {
                        if (counter == 0)
                            croppedTerrains = new List<Terrain>();

                        croppedTerrains.Add(t.GetComponent<Terrain>());
                        counter++;
                    }
                }

                terrainChunks = counter;
            }
        }

        //private void ReadXMLFile(string xmlPath)
        //{
        //    try
        //    {
        //        XmlDocument coordinatesDoc = new XmlDocument();
        //        coordinatesDoc.Load(xmlPath);
        //        XmlNode nodeLat = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Latitude");
        //        XmlNode nodeLon = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Longitude");
        //        XmlNode nodeTop = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Top");
        //        XmlNode nodeLft = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Left");
        //        XmlNode nodeBtm = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Bottom");
        //        XmlNode nodeRgt = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/Right");
        //        XmlNode nodeLatExt = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/LatExtents");
        //        XmlNode nodeLonExt = coordinatesDoc.DocumentElement.SelectSingleNode("/Coordinates/LonExtents");
        //
        //        top = double.Parse(nodeTop.InnerText);
        //        left = double.Parse(nodeLft.InnerText);
        //        bottom = double.Parse(nodeBtm.InnerText);
        //        right = double.Parse(nodeRgt.InnerText);
        //        latitudeUser = double.Parse(nodeLat.InnerText);
        //        longitudeUser = double.Parse(nodeLon.InnerText);
        //        areaSizeLat = float.Parse(nodeLatExt.InnerText);
        //        areaSizeLon = float.Parse(nodeLonExt.InnerText);
        //    }
        //    catch { }
        //}

        private void GenerateNewTerrainObject()
        {
            SetData();
            CreateTerrainData();
            CreateTerrainObject();

            if (splitSizeFinal == 1)
            {
                terrain = terrains[0];
                terrain.transform.position = (Vector3)GetAbsoluteWorldPosition();
            }
            else
            {
                splittedTerrains = terrainsParent;
                splittedTerrains.transform.position = (Vector3)GetAbsoluteWorldPosition();
            }
        }

        private Vector3d GetAbsoluteWorldPosition()
        {
            AreaBounds.Wgs84CenterPtToBBox(latitudeUser, longitudeUser, areaSizeLat, areaSizeLon, out top, out left, out bottom, out right);

            double _yMaxTop = AreaBounds.LatitudeToMercator(top);
            double _xMinLeft = AreaBounds.LongitudeToMercator(left);
            double _yMinBottom = AreaBounds.LatitudeToMercator(bottom);
            double _xMaxRight = AreaBounds.LongitudeToMercator(right);
            double _latSize = Math.Abs(_yMaxTop - _yMinBottom);
            double _lonSize = Math.Abs(_xMinLeft - _xMaxRight);
            double _worldSizeX = terrainSizeNewX * scaleFactor;
            double _worldSizeY = terrainSizeNewZ * scaleFactor;
            double _LAT = AreaBounds.LatitudeToMercator(latitudeUser);
            double _LON = AreaBounds.LongitudeToMercator(longitudeUser);
            double[] _latlonDeltaNormalized = AreaBounds.GetNormalizedDelta(_LAT, _LON, _yMaxTop, _xMinLeft, _latSize, _lonSize);
            Vector2d _initialWorldPositionXZ = AreaBounds.GetWorldPositionFromTile(_latlonDeltaNormalized[0], _latlonDeltaNormalized[1], _worldSizeY, _worldSizeX);
            Vector3d _initialWorldPosition = Vector3d.zero;

            if (splitSizeFinal == 1)
                _initialWorldPosition = new Vector3d(_initialWorldPositionXZ.x, 0, -_initialWorldPositionXZ.y);
            else
                _initialWorldPosition = new Vector3d(_initialWorldPositionXZ.x + _worldSizeY / 2, 0, -_initialWorldPositionXZ.y + _worldSizeX / 2);

            return _initialWorldPosition;
        }

        private void GenerateToken()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(tokenURL);
            req.KeepAlive = false;
            req.ProtocolVersion = HttpVersion.Version10;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

            try
            {
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string str = sr.ReadToEnd();
                token = str.Replace("{\"access_token\":\"", "").Replace("\",\"expires_in\":1209600}", "");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);

                terrainGenerationstarted = false;
                cancelOperationHeightmapDownloader = true;
                showProgressElevation = false;
                showProgressGenerateASCII = false;
                showProgressGenerateRAW = false;
                showProgressSmoothen = false;
                showProgressSmoothenOperation = false;
                convertingElevationTiles = false;
                stitchingElevationTiles = false;
            }
        }

        private double[] ToWebMercator(double mercatorY_lat, double mercatorX_lon)
        {
            const double earthRadiusEquatorial = 6378137; // 6378137 - 6371010
            const double earthRadiusPolar = 6356752.3142;
            double radiusEarthMeters = earthRadiusPolar + (90 - Math.Abs(mercatorY_lat)) / 90 * (earthRadiusEquatorial - earthRadiusPolar);
            //double radiusEarthMeters = 6378137d;

            //double latOffset = 0.02d;
            float latOffset = Mathf.InverseLerp(-90f, 90f, (float)mercatorY_lat) * 0.05f;
            //UnityEngine.Debug.Log(latOffset);
            latOffset = 0;

            double radiusEarthMetersHalf = radiusEarthMeters / 2d;
            double num = (mercatorY_lat - latOffset) * 0.017453292519943295d;  //0.9966760740043901
            double mercatorLat = radiusEarthMetersHalf * Math.Log((1.0 + Math.Sin(num)) / (1.0 - Math.Sin(num)));
            //double mercatorLat = 3189068.5d * Math.Log((1.0 + Math.Sin(num)) / (1.0 - Math.Sin(num)));

            double num2 = mercatorX_lon * 0.017453292519943295d;
            double mercatorLon = radiusEarthMeters * num2;

            //UnityEngine.Debug.Log(mercatorLat + "   " + mercatorLon);

            return new double[] { mercatorLat, mercatorLon };
        }

        private double[] GeoCoordsFromWebmercator(double x, double y)
        {
            double num3 = x / 6378137.0;
            double num4 = num3 * 57.295779513082323;
            double num5 = Math.Floor((num4 + 180.0) / 360.0);
            double num6 = num4 - (num5 * 360.0);
            double num7 = 1.5707963267948966 - (2.0 * Math.Atan(Math.Exp((-1.0 * y) / 6378137.0)));

            return new double[] { num7 * 57.295779513082323, num6 };
        }

        private static double RadToDeg(double rad)
        {
            double RAD2Deg = 180.0 / Math.PI;
            return rad * RAD2Deg;
        }

        private static double DegToRad(double deg)
        {
            double DEG2RAD = Math.PI / 180.0;
            return deg * DEG2RAD;
        }

        private void CheckThreadStatusImageDownloader()
        {
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
        }

        private void SetupDataContentProject()
        {
            if (!failedDownloading)
            {
                terrain = null;
                splittedTerrains = null;

                if (gridNumber > 10)
                {
                    int alphamapsNo = Mathf.FloorToInt(totalImages / 4f);

                    if (EditorUtility.DisplayDialog("HEAVY DATA LOAD", totalImages.ToString() + "  images will be downloaded and  " + alphamapsNo.ToString() + "  alphamaps will be created for terrain.\n\nThat will be heavy, Are you sure?", "No", "Yes"))
                    {
                        cancelOperation = true;
                        showProgressImagery = false;
                        ThrowException("Terrain Texturing has been canceled by user!");
                    }
                }

                if (!Directory.Exists(downloadsPath))
                    Directory.CreateDirectory(downloadsPath);

                if (Directory.Exists(projectPath + "Temporary Elevation Data"))
                    Directory.Delete(projectPath + "Temporary Elevation Data", true);

                Directory.CreateDirectory(projectPath + "Temporary Elevation Data");

                downloadDateStr = DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss");
                currentDownloadsPath = downloadsPath + "/" + downloadDateStr;

                if (saveTerrainDataASCII || saveTerrainDataTIFF || saveTerrainDataRAW)
                {
                    directoryPathElevation = currentDownloadsPath + "/Elevation";

                    if (!string.IsNullOrEmpty(directoryPathElevation))
                        Directory.CreateDirectory(directoryPathElevation);
                    else
                        ThrowException("Select a save location to download Elevation!");
                }

                if (textureOnFinish == 0)
                {
                    directoryPathImagery = currentDownloadsPath + "/Satellite Images";
                    directoryPathTerrainlayers = currentDownloadsPath + "/Terrain Layers";

                    if (!Directory.Exists(directoryPathTerrainlayers))
                        Directory.CreateDirectory(directoryPathTerrainlayers);
                }
                else
                    directoryPathImagery = EditorUtility.OpenFolderPanel("Select a folder to save satellite images", projectPath, "Imagery");

                if (!string.IsNullOrEmpty(directoryPathImagery))
                {
                    Directory.CreateDirectory(directoryPathImagery);
                    WritePresetFile(directoryPathImagery + "/Terrain Info.tlps");
                }
                else
                    ThrowException("Select a save location to download Imagery!");

                splitDirectoryPath = currentDownloadsPath + "/Terrain Tiles";
                if (!Directory.Exists(splitDirectoryPath)) Directory.CreateDirectory(splitDirectoryPath);
                AssetDatabase.Refresh();
                terrainName = "Terrain";

                for (int y = 0; y < splitSizeFinal; y++)
                {
                    for (int x = 0; x < splitSizeFinal; x++)
                    {
                        AssetDatabase.CreateAsset(new TerrainData(), splitDirectoryPath.Substring(splitDirectoryPath.LastIndexOf("Assets")) + "/" + terrainName + " " + (y + 1) + "-" + (x + 1) + ".asset");
                        EditorUtility.DisplayProgressBar("CREATING DATA", "Creating Terrain Data Assets", Mathf.InverseLerp(0f, splitSizeFinal, y));
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                downloadDateStr = Path.GetFileName(AssetDatabase.GetAssetPath(failedFolder).Replace("/Satellite Images", ""));
                currentDownloadsPath = downloadsPath + "/" + downloadDateStr;
                directoryPathElevation = currentDownloadsPath + "/Elevation";
                directoryPathImagery = currentDownloadsPath + "/Satellite Images";
                directoryPathTerrainlayers = currentDownloadsPath + "/Terrain Layers";
                splitDirectoryPath = currentDownloadsPath + "/Terrain Tiles";

                if (!Directory.Exists(directoryPathImagery))
                {
                    EditorUtility.DisplayDialog("FOLDER UNAVAILABLE", "Insert a folder containing failed satellite images to re-download!", "Ok");
                    failedDownloading = false;
                    cancelOperation = true;
                    ThrowException("\"Satellite Images\" folder is not inserted!");
                }

                if (failedFolder)
                    CheckFailedImagesGUI();

                if (failedFolder && totalFailedImages == 0)
                {
                    EditorUtility.DisplayDialog("NO FAILED IMAGES", "There are no failed images in the selected folder!", "Ok");
                    failedDownloading = false;
                    showProgressImagery = false;
                    cancelOperation = true;
                    ThrowException("No failed images have been detected!\n\nNote: If any of the images have been downloaded incorrectly, you can rename its filename and include \"_Temp\" at the end of the name, then finally press GET FAILED IMAGES button again to redownload.");
                }

                if (textureOnFinish == 0 && !terrain && !splittedTerrains)
                {
                    EditorUtility.DisplayDialog("UNAVAILABLE TERRAIN", unavailableTerrainStr, "Ok");
                    failedDownloading = false;
                    showProgressImagery = false;
                    cancelOperation = true;
                    ThrowException(unavailableTerrainStr);
                }
            }
        }

        private void SetupDataContentServer(string directoryRoot)
        {
            directoryPathElevation = serverPath + "/Elevation";
            directoryPathImagery = serverPath + "/Imagery";
            directoryPathInfo = serverPath + "/Info";

            if (!string.IsNullOrEmpty(directoryPathElevation))
                Directory.CreateDirectory(directoryPathElevation);
            else
                ThrowException("Select a save location to download Elevation!");

            if (!string.IsNullOrEmpty(directoryPathImagery))
                Directory.CreateDirectory(directoryPathImagery);
            else
                ThrowException("Select a save location to download Imagery!");

            if (!string.IsNullOrEmpty(directoryPathInfo))
                Directory.CreateDirectory(directoryPathInfo);
            else
                ThrowException("Select a save location to create Info file!");

            //SetCoordinates();
            //GenerateProjFile();
            GenerateXMLFile();
            WritePresetFile(directoryPathInfo + "/Terrain Info.tlps");

            failedTilesAvailable = false;
        }

        private void SetupDataContent()
        {
            if (dynamicWorld)
                SetupDataContentServer(serverPath);
            else
                SetupDataContentProject();
        }

        private void InitializeDownloader()
        {
            ConnectionsManager.SetAsyncConnections();

            downloadedHeightmapIndex = 0;
            downloadedImageIndex = 0;
            importingInProgress = false;

            tempImageBytes = satelliteImageTemp.EncodeToJPG();

            xMinLeft = left * 20037508.34 / 180.0;
            yMaxTop = Math.Log(Math.Tan((90.0 + top) * Math.PI / 360.0)) / (Math.PI / 180.0);
            yMaxTop = yMaxTop * 20037508.34 / 180.0;

            xMaxRight = right * 20037508.34 / 180.0;
            yMinBottom = Math.Log(Math.Tan((90.0 + bottom) * Math.PI / 360.0)) / (Math.PI / 180.0);
            yMinBottom = yMinBottom * 20037508.34 / 180.0;

            terrainSizeFactor = areaSizeLat / areaSizeLon;
            latCellSize = Math.Abs(yMaxTop - yMinBottom) / (double)gridNumber;
            lonCellSize = Math.Abs(xMinLeft - xMaxRight) / (double)gridNumber;

            int cellsOnTerrain = 0;

            if (dynamicWorld)
                //cellsOnTerrain = terrainChunks;
                cellsOnTerrain = (int)Mathf.Pow(gridNumber, 2);
            else
                cellsOnTerrain = totalImages;

            if (!failedDownloading)
            {
                xMin = new double[cellsOnTerrain];
                yMin = new double[cellsOnTerrain];
                xMax = new double[cellsOnTerrain];
                yMax = new double[cellsOnTerrain];
            }
            else
            {
                if (dynamicWorld)
                    SetFailedIndicesElevation();

                SetFailedIndicesImagery();
            }

            foreach (Transform t in Resources.FindObjectsOfTypeAll(typeof(Transform)))
            {
                if (t.name.Equals("Image Imports"))
                    DestroyImmediate(t.gameObject);
            }

            imageImportTiles = new GameObject("Image Imports");
            imageImportTiles.hideFlags = HideFlags.HideAndDontSave;
            TerrainGridManager(gridNumber, cellsOnTerrain);
        }

        private void TerrainGridManager(int grid, int cells)
        {
            int index = 0;
            cellSizeX = terrainSizeX / (float)grid;
            cellSizeY = terrainSizeY / (float)grid;

            imageXOffset = new float[cells];
            imageYOffset = new float[cells];

            latCellTop = new double[cells];
            latCellBottom = new double[cells];
            lonCellLeft = new double[cells];
            lonCellRight = new double[cells];

            for (int i = 0; i < grid; i++)
            {
                for (int j = 0; j < grid; j++)
                {
                    imageXOffset[index] = (terrainSizeX - (cellSizeX * ((float)grid - (float)j))) * -1f;
                    imageYOffset[index] = (terrainSizeY - cellSizeY - ((float)cellSizeY * (float)i)) * -1f;

                    latCellTop[index] = yMaxTop - (latCellSize * (double)i);
                    latCellBottom[index] = latCellTop[index] - latCellSize;
                    lonCellLeft[index] = xMinLeft + (lonCellSize * (double)j);
                    lonCellRight[index] = lonCellLeft[index] + lonCellSize;

                    if (!failedDownloading)
                    {
                        xMin[index] = lonCellLeft[index];
                        yMin[index] = latCellBottom[index];
                        xMax[index] = lonCellRight[index];
                        yMax[index] = latCellTop[index];
                    }

                    index++;
                }
            }
        }

        private void SetFailedIndicesElevation()
        {
            failedIndicesElevation = FailedIndicesElevation();

            if (failedIndicesElevation != null && failedIndicesElevation.Count > 0)
            {
                failedIndicesCountElevation = failedIndicesElevation.Count;

                xMinFailedElevation = new double[failedIndicesCountElevation];
                yMinFailedElevation = new double[failedIndicesCountElevation];
                xMaxFailedElevation = new double[failedIndicesCountElevation];
                yMaxFailedElevation = new double[failedIndicesCountElevation];
            }
        }

        private List<int> FailedIndicesElevation()
        {
            List<int> index = new List<int>();
            string[] names = LogicalComparer(directoryPathElevation, ".tif");
            string removeString = tempPattern + ".tif";

            if (names.Length > 0)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];

                    if (name.Contains(tempPattern))
                    {
                        string str = name.Substring(name.LastIndexOf(@"\") + 1).Replace(removeString, "");
                        int[] result = new Regex(@"\d+").Matches(str).Cast<Match>().Select(m => Int32.Parse(m.Value)).ToArray();

                        int x = result[0];
                        int y = result[1];
                        int ind = ((x - 1) * gridNumber + y) - 1;

                        index.Add(ind);
                    }
                }

                return index;
            }

            return null;
        }

        private void SetFailedIndicesImagery()
        {
            failedIndicesImagery = FailedIndicesImagery();

            if (failedIndicesImagery != null && failedIndicesImagery.Count > 0)
            {
                failedIndicesCountImagery = failedIndicesImagery.Count;

                xMinFailedImagery = new double[failedIndicesCountImagery];
                yMinFailedImagery = new double[failedIndicesCountImagery];
                xMaxFailedImagery = new double[failedIndicesCountImagery];
                yMaxFailedImagery = new double[failedIndicesCountImagery];
            }
        }

        private List<int> FailedIndicesImagery()
        {
            List<int> index = new List<int>();
            string removeString = tempPattern + ".jpg";

            if (!dynamicWorld)
                allImageNames = Directory.GetFiles(AssetDatabase.GetAssetPath(failedFolder), "*.jpg", SearchOption.AllDirectories);
            else
                allImageNames = Directory.GetFiles(directoryPathImagery, "*.jpg", SearchOption.AllDirectories);

            allImageNames = LogicalComparer(allImageNames);

            if (!splittedTerrains)
            {
                if (allImageNames.Length > 0)
                {
                    for (int i = 0; i < allImageNames.Length; i++)
                    {
                        string name = allImageNames[i];

                        if (name.Contains(tempPattern))
                            index.Add(i);
                    }
                    return index;
                }
                return null;
            }
            else
            {
                if (allImageNames.Length > 0)
                {
                    for (int i = 0; i < totalImages; i++)
                    {
                        string name = allImageNames[i];

                        if (name.Contains(tempPattern))
                        {
                            string str = name.Substring(name.LastIndexOf(@"\") + 1).Replace(removeString, "");
                            int[] result = new Regex(@"\d+").Matches(str).Cast<Match>().Select(m => Int32.Parse(m.Value)).ToArray();
                            int x = result[0];
                            int y = result[1];
                            int ind = ((x - 1) * gridNumber + y) - 1;
                            index.Add(ind);
                        }
                    }
                    return index;
                }
                return null;
            }
        }

        private void GetHeightmaps()
        {
            RunAsync(() =>
            {
                ServerInfoElevation();
            });
        }

        private void ServerInfoElevation()
        {
            if (!failedDownloading)
            {
                for (int i = 0; i < terrainChunks; i++)
                {
                    if (cancelOperationHeightmapDownloader)
                    {
                        showProgressElevation = false;
                        return;
                    }

                    xMin[i] = lonCellLeft[i];
                    yMin[i] = latCellBottom[i];
                    xMax[i] = lonCellRight[i];
                    yMax[i] = latCellTop[i];

                    ServerConnectHeightmap(i, i);
                }
            }
            else
            {
                for (int i = 0; i < failedIndicesCountElevation; i++)
                {
                    if (cancelOperationHeightmapDownloader)
                    {
                        showProgressElevation = false;
                        return;
                    }

                    int currentIndex = failedIndicesElevation[i];

                    xMinFailedElevation[i] = lonCellLeft[currentIndex];
                    yMinFailedElevation[i] = latCellBottom[currentIndex];
                    xMaxFailedElevation[i] = lonCellRight[currentIndex];
                    yMaxFailedElevation[i] = latCellTop[currentIndex];

                    ServerConnectHeightmap(i, currentIndex);
                }
            }
        }

        private void ServerConnectHeightmap(int i, int current)
        {
            RunAsync(() =>
            {
                if (!dynamicWorld)
                {
                    ElevationDownload();
                    //ElevationDownloadNEW(i, current);
                }
                else
                    ElevationDownload(i, current);

                QueueOnMainThread(() =>
                {
                    if (cancelOperationHeightmapDownloader)
                    {
                        showProgressElevation = false;
                        return;
                    }

                    if (dynamicWorld)
                    {
                        if (!failedDownloading)
                        {
                            if (downloadedHeightmapIndex == terrainChunks)
                                GenerateTerrainHeights();
                        }
                        else
                        {
                            if (downloadedHeightmapIndex == failedIndicesCountElevation)
                                GenerateTerrainHeights();
                        }
                    }
                });
            });
        }

        private void ElevationDownload()
        {
            int finalResolution = heightmapResolutionEditor + splitSizeFinal;
            reducedheightmapResolution = heightmapResolutionEditor;

            // Automatically retries downloading of data if failed in previous session.
            // First time it retries with the same resolution to make sure data is not
            // there and then reduces resolution by power of 2 until it gets down to 32.
            if (retries > 0)
            {
                reducedheightmapResolution = Mathf.Clamp(heightmapResolutionEditor / retries, 32, maxHeightmapResolution);
                finalResolution = reducedheightmapResolution + splitSizeFinal;

                if (splittedTerrains)
                    heightmapResolutionSplit = reducedheightmapResolution / (int)Mathf.Sqrt((float)terrainChunks);
                else
                    heightmapResolutionSplit = reducedheightmapResolution;

                if (retries > 1)
                    UnityEngine.Debug.Log("Reduced Heightmap Resolution To: " + reducedheightmapResolution);
            }

            terrainResolutionDownloading = finalResolution;

            try
            {
                //TerraLandWorldElevation.ImageServiceInfo isInfo = mapserviceElevation.GetServiceInfo();
                ////isInfo.DefaultCompressionQuality = 100;
                ////isInfo.DefaultCompression = "None";
                //isInfo.MaxScaleSpecified = true;
                //isInfo.MaxPixelSize = terrainResolutionDownloading;
                //isInfo.MaxScale = terrainResolutionDownloading;

                //		TerraLandWorldElevation.PointN location = new TerraLandWorldElevation.PointN();
                //		location.X = ToWebMercatorLon(longitudeUser);
                //		location.Y = ToWebMercatorLon(latitudeUser);

                //TODO: Check if below lines needed
                //TerraLandWorldElevation.MosaicRule mosaicRule = new TerraLandWorldElevation.MosaicRule();
                //mosaicRule.MosaicMethod = TerraLandWorldElevation.esriMosaicMethod.esriMosaicAttribute;

                //		TerraLandWorldElevation.PointN inputpoint2 = new TerraLandWorldElevation.PointN();
                //		inputpoint2.X = 0.2;
                //		inputpoint2.Y = 0.2;
                //		
                //		TerraLandWorldElevation.ImageServerIdentifyResult identifyresults = mapserviceElevation.Identify(location, mosaicRule, inputpoint2);
                //
                //		double pixelResolution = Double.Parse (identifyresults.CatalogItems.Records [0].Values [5].ToString());
                //		string dataSource = identifyresults.CatalogItems.Records [0].Values [8].ToString ();
                //		
                //		int terrainHeight = (int)((double)((areaSizeLat * 4.0) * 1000.0) / pixelResolution);
                //		int terrainWidth = (int)((double)((areaSizeLon * 4.0) * 1000.0) / pixelResolution);

                //define image description
                TerraLandWorldElevation.GeoImageDescription geoImgDesc = new TerraLandWorldElevation.GeoImageDescription();
                //geoImgDesc.MosaicRule = mosaicRule;
                //geoImgDesc.Height = terrainHeight;
                //geoImgDesc.Width = terrainWidth;
                //geoImgDesc.Height = heightmapResolutionSplit + 1;
                //geoImgDesc.Width = heightmapResolutionSplit + 1;

                //TerraLandWorldElevation.ImageServiceInfo isInfo = mapserviceElevation.GetServiceInfo();
                //UnityEngine.Debug.Log(isInfo.RasterFunctions);

                geoImgDesc.Height = terrainResolutionDownloading;
                geoImgDesc.Width = terrainResolutionDownloading;


                geoImgDesc.Compression = "LZW";
                //geoImgDesc.CompressionQuality = 100;
                //geoImgDesc.Interpolation = TerraLandWorldElevation.rstResamplingTypes.RSP_CubicConvolution;
                //geoImgDesc.NoDataInterpretationSpecified = true;
                //geoImgDesc.NoDataInterpretation = TerraLandWorldElevation.esriNoDataInterpretation.esriNoDataMatchAny;

                //newLat = document.getElementById("inputLat").value;
                //newLng = document.getElementById("inputLng").value;

                //TerraLandWorldElevation.Geometry geom = new TerraLandWorldElevation.Geometry();
                //TerraLandWorldElevation.SpatialReference sr;

                //MapPoint mapPointObjectToConvert = new MapPoint(longitude, latitude, TerraLandWorldElevation.SpatialReference.Wgs84);
                //MapPoint mapPoint = Esri.ArcGISRuntime.Geometry.GeometryEngine.Project(mapPointObjectToConvert, SpatialReferences.WebMercator) as MapPoint;

                //TerraLandWorldElevation.PointN p = new TerraLandWorldElevation.PointN();
                //p.SpatialReference


                //Point pointGeometry = GeometryEngine.project(23.63733, 37.94721, SpatialReference.create(102113));
                //Point pointGeometry = GeometryEngine.project(24.63733, 38.94721, SpatialReference.create(102113));

                //newPoint = TerraLandWorldElevation.Geometry.Point(top, left, TerraLandWorldElevation.SpatialReference({ wkid: 4326 }));
                //wmPoint = new esri.geometry.geographicToWebMercator(newPoint);


                TerraLandWorldElevation.EnvelopeN extentElevation = new TerraLandWorldElevation.EnvelopeN();

                //TerraLandWorldElevation.SpatialReference sr;
                //extentElevation.SpatialReference = sr.
                //SpatialReference srIn = SpatialReference.Create(wkidIn);

                //UnityEngine.Debug.Log(extentElevation.SpatialReference);

                //extentElevation.XMin = ToWebMercator(latitudeUser, left)[1];
                //extentElevation.YMin = ToWebMercator(bottom, longitudeUser)[0];
                //extentElevation.XMax = ToWebMercator(latitudeUser, right)[1];
                //extentElevation.YMax = ToWebMercator(top, longitudeUser)[0];



                //40075016.6855784

                xMinLeft = left * 20037508.34 / 180.0;
                yMaxTop = Math.Log(Math.Tan((90.0 + top) * Math.PI / 360.0)) / (Math.PI / 180.0);
                yMaxTop = yMaxTop * 20037508.34 / 180.0;

                xMaxRight = right * 20037508.34 / 180.0;
                yMinBottom = Math.Log(Math.Tan((90.0 + bottom) * Math.PI / 360.0)) / (Math.PI / 180.0);
                yMinBottom = yMinBottom * 20037508.34 / 180.0;

                //https://epsg.io/transform#s_srs=4326&t_srs=3857&x=-121.0909435&y=38.8277527
                //UnityEngine.Debug.Log(xMinLeft + "   "+ yMaxTop);

                extentElevation.XMin = xMinLeft;
                extentElevation.YMin = yMinBottom;
                extentElevation.XMax = xMaxRight;
                extentElevation.YMax = yMaxTop;
                geoImgDesc.Extent = extentElevation;

                TerraLandWorldElevation.ImageType imageType = new TerraLandWorldElevation.ImageType();
                imageType.ImageFormat = TerraLandWorldElevation.esriImageFormat.esriImageTIFF;


                // NOTE: Do not use esriImageReturnMimeData as it does not produce any results in higher resolution queries such as 4096
                // The above statement is not valid anymore!

                int downloadMode = 0; // 0 => Data, 1 => Url

                if (downloadMode == 0)
                {
                    imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnMimeData;
                    TerraLandWorldElevation.ImageResult result = mapserviceElevation.ExportImage(geoImgDesc, imageType);
                    //TODO: If needed, use the following async functions to monitor progress and show in UI as "DownloadTerrainData" does it!
                    //mapserviceElevation.ExportImageAsync(geoImgDesc, imageType);
                    //mapserviceElevation.ExportImageCompleted...
                    fileNameTerrainData = projectPath + "Temporary Elevation Data/" + "TempElevation.tif";
                    File.WriteAllBytes(fileNameTerrainData, result.ImageData);
                    GenerateTerrainHeights();
                }
                else if (downloadMode == 1)
                {
                    imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnURL;
                    TerraLandWorldElevation.ImageResult result = mapserviceElevation.ExportImage(geoImgDesc, imageType);
                    terrainDataURL = result.ImageURL;
                    DownloadTerrainData(terrainDataURL, fileNameTerrainData);
                }
            }
            catch (Exception e)
            {
                if (retries == 0)
                    retries = 1;
                else if (retries == 1)
                    retries = 2;
                else if (retries == 2)
                    retries = 4;
                else if (retries == 4)
                    retries = 8;
                else if (retries == 8)
                    retries = 16;
                else if (retries == 16)
                    retries = 32;
                else if (retries == 32)
                    retries = 64;

                if (retries == 64)
                {
                    UnityEngine.Debug.Log(e);

                    terrainGenerationstarted = false;
                    cancelOperationHeightmapDownloader = true;
                    showProgressElevation = false;
                    showProgressGenerateASCII = false;
                    showProgressGenerateRAW = false;
                    showProgressSmoothen = false;
                    showProgressSmoothenOperation = false;
                    convertingElevationTiles = false;
                    stitchingElevationTiles = false;

                    return;
                }
                else
                    ServerConnectHeightmap(0, 0);
            }

            if (cancelOperationHeightmapDownloader)
            {
                showProgressElevation = false;
                return;
            }
        }

        private void ElevationDownloadNEW(int i, int current)
        {
            int row = Mathf.CeilToInt((float)(current + 1) / (float)gridNumber);
            int column = (current + 1) - ((row - 1) * gridNumber);
            string imgName = "";

            //try
            {
                TerraLandWorldElevation.GeoImageDescription geoImgDesc = new TerraLandWorldElevation.GeoImageDescription();

                terrainResolutionDownloading = heightmapResolutionEditor;


                geoImgDesc.Height = terrainResolutionDownloading;
                geoImgDesc.Width = terrainResolutionDownloading;

                geoImgDesc.Compression = "LZW";
                geoImgDesc.CompressionQuality = 100;
                geoImgDesc.Interpolation = TerraLandWorldElevation.rstResamplingTypes.RSP_CubicConvolution;
                geoImgDesc.NoDataInterpretationSpecified = true;
                geoImgDesc.NoDataInterpretation = TerraLandWorldElevation.esriNoDataInterpretation.esriNoDataMatchAny;

                TerraLandWorldElevation.EnvelopeN extentElevation = new TerraLandWorldElevation.EnvelopeN();

                if (!failedDownloading)
                {
                    extentElevation.XMin = xMin[i];
                    extentElevation.YMin = yMin[i];
                    extentElevation.XMax = xMax[i];
                    extentElevation.YMax = yMax[i];
                }
                else
                {
                    extentElevation.XMin = xMinFailedElevation[i];
                    extentElevation.YMin = yMinFailedElevation[i];
                    extentElevation.XMax = xMaxFailedElevation[i];
                    extentElevation.YMax = yMaxFailedElevation[i];
                }

                geoImgDesc.Extent = extentElevation;
































                TerraLandWorldElevation.ImageType imageType = new TerraLandWorldElevation.ImageType();
                imageType.ImageFormat = TerraLandWorldElevation.esriImageFormat.esriImageTIFF;

                //NOTE: Do not use esriImageReturnMimeData as it does not produce any results in higher resolution queries such as 4096
                //imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnMimeData;
                imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnURL;

                TerraLandWorldElevation.ImageResult result = mapserviceElevation.ExportImage(geoImgDesc, imageType);


                terrainDataURL = result.ImageURL;

                //fileNameTerrainData = projectPath + "Temporary Elevation Data/" + "TempElevation.tif";
                fileNameTerrainData = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + ".tif";


                //File.WriteAllBytes(fileNameTerrainData, result.ImageData);
                //GenerateTerrainHeights();

                DownloadTerrainData(terrainDataURL, fileNameTerrainData);


                //imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + ".tif";
                //File.WriteAllBytes(imgName, result.ImageData);
                //
                //string tempFileName = imgName.Replace(".tif", tempPattern + ".tif");
                //
                //if (File.Exists(tempFileName))
                //    File.Delete(tempFileName);
                //
                ////DownloadTerrainData(result.ImageURL, imgName);
            }
            //catch (Exception e)
            //{
            //    imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + tempPattern + ".tif";
            //
            //    if (!File.Exists(imgName))
            //    {
            //        byte[] bytes = new byte[terrainResolutionDownloading * terrainResolutionDownloading];
            //        File.WriteAllBytes(imgName, bytes);
            //    }
            //
            //    // Following lines will remove tiles if were already available from previous download sessions
            //    imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + ".raw";
            //
            //    if (File.Exists(imgName))
            //        File.Delete(imgName);
            //
            //    failedTilesAvailable = true;
            //
            //    //UnityEngine.Debug.Log(e);
            //}
            //finally
            {
                downloadedHeightmapIndex++;

                if (!failedDownloading)
                    progressBarElevation = Mathf.InverseLerp(0, terrainChunks, downloadedHeightmapIndex);
                else
                    progressBarElevation = Mathf.InverseLerp(0, failedIndicesCountElevation, downloadedHeightmapIndex);
            }

            if (cancelOperationHeightmapDownloader)
            {
                showProgressElevation = false;
                return;
            }
        }

        private void ElevationDownload(int i, int current)
        {
            int row = Mathf.CeilToInt((float)(current + 1) / (float)gridNumber);
            int column = (current + 1) - ((row - 1) * gridNumber);
            string imgName = "";

            try
            {
                TerraLandWorldElevation.GeoImageDescription geoImgDesc = new TerraLandWorldElevation.GeoImageDescription();

                geoImgDesc.Height = terrainResolutionDownloading;
                geoImgDesc.Width = terrainResolutionDownloading;

                geoImgDesc.Compression = "LZW";
                geoImgDesc.CompressionQuality = 100;
                geoImgDesc.Interpolation = TerraLandWorldElevation.rstResamplingTypes.RSP_CubicConvolution;
                geoImgDesc.NoDataInterpretationSpecified = true;
                geoImgDesc.NoDataInterpretation = TerraLandWorldElevation.esriNoDataInterpretation.esriNoDataMatchAny;

                TerraLandWorldElevation.EnvelopeN extentElevation = new TerraLandWorldElevation.EnvelopeN();

                if (!failedDownloading)
                {
                    extentElevation.XMin = xMin[i];
                    extentElevation.YMin = yMin[i];
                    extentElevation.XMax = xMax[i];
                    extentElevation.YMax = yMax[i];
                }
                else
                {
                    extentElevation.XMin = xMinFailedElevation[i];
                    extentElevation.YMin = yMinFailedElevation[i];
                    extentElevation.XMax = xMaxFailedElevation[i];
                    extentElevation.YMax = yMaxFailedElevation[i];
                }

                geoImgDesc.Extent = extentElevation;

                TerraLandWorldElevation.ImageType imageType = new TerraLandWorldElevation.ImageType();
                imageType.ImageFormat = TerraLandWorldElevation.esriImageFormat.esriImageTIFF;

                imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnMimeData;
                //imageType.ImageReturnType = TerraLandWorldElevation.esriImageReturnType.esriImageReturnURL;

                TerraLandWorldElevation.ImageResult result = mapserviceElevation.ExportImage(geoImgDesc, imageType);

                imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + ".tif";
                File.WriteAllBytes(imgName, result.ImageData);

                string tempFileName = imgName.Replace(".tif", tempPattern + ".tif");

                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);

                //DownloadTerrainData(result.ImageURL, imgName);
            }
            catch (Exception e)
            {
                imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + tempPattern + ".tif";

                if (!File.Exists(imgName))
                {
                    byte[] bytes = new byte[terrainResolutionDownloading * terrainResolutionDownloading];
                    File.WriteAllBytes(imgName, bytes);
                }

                // Following lines will remove tiles if were already available from previous download sessions
                imgName = directoryPathElevation + "/" + row.ToString() + "-" + column.ToString() + ".raw";

                if (File.Exists(imgName))
                    File.Delete(imgName);

                failedTilesAvailable = true;

                UnityEngine.Debug.Log(e);
            }
            finally
            {
                downloadedHeightmapIndex++;

                if (!failedDownloading)
                    progressBarElevation = Mathf.InverseLerp(0, terrainChunks, downloadedHeightmapIndex);
                else
                    progressBarElevation = Mathf.InverseLerp(0, failedIndicesCountElevation, downloadedHeightmapIndex);
            }

            if (cancelOperationHeightmapDownloader)
            {
                showProgressElevation = false;
                return;
            }
        }

        private void DownloadTerrainData(string urlAddress, string path)
        {
            using (webClientTerrain = new WebClient())
            {
                webClientTerrain.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClientTerrain.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);

                //Uri URL = urlAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("http://" + urlAddress);
                Uri URL = new Uri(urlAddress);
                stopWatchTerrain.Start();

                try
                {
                    webClientTerrain.DownloadFileAsync(URL, path);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e);

                    terrainGenerationstarted = false;
                    cancelOperationHeightmapDownloader = true;
                    showProgressElevation = false;
                    showProgressGenerateASCII = false;
                    showProgressGenerateRAW = false;
                    showProgressSmoothen = false;
                    showProgressSmoothenOperation = false;
                    convertingElevationTiles = false;
                    stitchingElevationTiles = false;

                    return;
                }
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            downloadSpeedTerrain = string.Format((e.BytesReceived / 1024d / stopWatchTerrain.Elapsed.TotalSeconds).ToString("0.00"));
            progressBarElevation = (float)e.ProgressPercentage / 100f;
            dataReceivedTerrain = string.Format("{0} " + "--- " + "{1} MB", (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            stopWatchTerrain.Reset();

            if (e.Cancelled == true)
                UnityEngine.Debug.Log(e.Error);
            else
                GenerateTerrainHeights();
        }

        private void DownloadImageryData(string urlAddress, string location)
        {
            using (webClientImagery = new WebClient())
            {
                webClientImagery.DownloadFileCompleted += new AsyncCompletedEventHandler(CompletedImagery);
                Uri URL = new Uri(urlAddress);

                try
                {
                    webClientImagery.DownloadFileAsync(URL, location);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e);

                    cancelOperation = true;
                    showProgressImagery = false;
                    imageDownloadingStarted = false;
                    finishedImporting = true;
                    allThreads = 0;
                    CheckHeightmapDownloaderAndRecompile();

                    return;
                }
            }
        }

        private void CompletedImagery(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == true)
                UnityEngine.Debug.Log(e.Error);
        }

        private void GenerateTerrainHeights()
        {
            RunAsync(() =>
            {
                if (!dynamicWorld)
                {
                    showProgressElevation = false;
                    TiffData(fileNameTerrainData);
                }

                QueueOnMainThread(() =>
                {
                    FinalizeTerrainHeights();
                });
            });
        }

        private void FinalizeTerrainHeights()
        {
            RunAsync(() =>
            {
                if (!dynamicWorld)
                {
                    smoothIterationsProgress = smoothIterations;
                    FinalizeSmooth(tiffData, tiffWidth, tiffLength, smoothIterations, smoothBlendIndex, smoothBlend);
                }

                QueueOnMainThread(() =>
                {
                    if (!dynamicWorld)
                    {
                        LoadTerrainHeightsFromTIFF();
                        ManageNeighborings();
                    }

                    OfflineDataSave();
                    ShowDownloadsFolder();
                });
            });
        }

        private void ManageNeighborings()
        {
            if (splittedTerrains)
            {
                SetTerrainNeighbors();

                if (splittedTerrains.GetComponent<TerrainNeighbors>() == null)
                    splittedTerrains.AddComponent<TerrainNeighbors>();

            }
            else if (terrain)
            {
                if (terrain.gameObject.GetComponent<TerrainNeighbors>() == null)
                    terrain.gameObject.AddComponent<TerrainNeighbors>();
            }
        }

        private void ShowDownloadsFolder()
        {
            EditorUtility.FocusProjectWindow();
            UnityEngine.Object downloadsFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(currentDownloadsPath.Substring(currentDownloadsPath.LastIndexOf("Assets")));
            Selection.activeObject = downloadsFolder;
            EditorGUIUtility.PingObject(downloadsFolder);
        }

        /*
        private void SetCoordinates ()
        {
            // Calculate Easting, Northing & UTM Zone for Arc ASCII Grid file generation and Proj file generation
            nsBaseCmnGIS.cBaseCmnGIS baseGIS = new nsBaseCmnGIS.cBaseCmnGIS();
            string utmBottomLeft = baseGIS.iLatLon2UTM(bottom, left, ref UTMNorthing, ref UTMEasting, ref sUtmZone);
            string[] utmValues = utmBottomLeft.Split(',');

            UTMEasting = double.Parse(utmValues[0]);
            UTMNorthing = double.Parse(utmValues[1]);
            sUtmZone = utmValues[2];
        }
        */

        private void OfflineDataSave()
        {
            RunAsync(() =>
            {
                if (!dynamicWorld)
                {
                    // Create Projection & XML Info file
                    if (saveTerrainDataASCII || saveTerrainDataTIFF || saveTerrainDataRAW)
                    {
                        //SetCoordinates();
                        //GenerateProjFile();
                        GenerateXMLFile();
                    }

                    if (saveTerrainDataASCII)
                        SaveTerrainDataASCII();

                    if (saveTerrainDataRAW)
                        SaveTerrainDataRAW();
                }

                QueueOnMainThread(() =>
                {
                    if (!dynamicWorld)
                        if (saveTerrainDataTIFF)
                            SaveTerrainDataTIFF();

                    FinalizeTerrainElevation();
                });
            });
        }

        private void FinalizeTerrainElevation()
        {
            showProgressElevation = false;
            showProgressGenerateASCII = false;
            showProgressGenerateRAW = false;
            showProgressSmoothen = false;
            showProgressSmoothenOperation = false;
            terrainGenerationstarted = false;

            if (Directory.Exists(projectPath + "Temporary Elevation Data"))
                Directory.Delete(projectPath + "Temporary Elevation Data", true);

            if (!dynamicWorld)
                CheckImageDownloaderAndRecompile();
            else
                GenerateTilesFromHeightmap();

            AssetDatabase.Refresh();
        }

        private void GenerateTilesFromHeightmap()
        {
            convertingElevationTiles = true;
            showProgressElevation = true;

            RunAsync(() =>
            {
                string[] fileNames = LogicalComparer(directoryPathElevation, ".tif");
                string fileName = "";
                int index = 0;

                if (!failedDownloading)
                {
                    for (int x = 1; x <= gridNumber; x++)
                    {
                        for (int y = 1; y <= gridNumber; y++)
                        {
                            fileName = fileNames[index];

                            if (!fileName.Contains(tempPattern))
                                TiffDataFast(fileName, x, y);

                            index++;

                            progressBarElevation = Mathf.InverseLerp(0, terrainChunks, index);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < fileNames.Length; i++)
                    {
                        fileName = fileNames[i];

                        if (!fileName.Contains(tempPattern))
                            TiffDataFast(fileName);

                        progressBarElevation = Mathf.InverseLerp(0, fileNames.Length - 1, i);
                    }
                }

                QueueOnMainThread(() =>
                {
                    convertingElevationTiles = false;

                    fileNames = LogicalComparer(directoryPathElevation, ".raw");
                    int length = fileNames.Length;

                    if (!Mathf.IsPowerOfTwo(length))
                    {
                        EditorUtility.DisplayDialog("STITCHING OPERATION SKIPPED", "Re-download Failed Tiles in order to Stitch data files", "Ok");

                        showProgressElevation = false;
                        serverSetUpElevation = true;

                        if (serverSetUpElevation && serverSetUpImagery)
                        {
                            if (failedTilesAvailable)
                            {
                                EditorUtility.DisplayDialog("FAILED TILES AVAILABLE", "There are some failed tile downloads for this session.\n\nGo to FAILED TILES DOWNLOADER section and press GET FAILED TILES button to re-download failed tiles.", "Ok");
                                showFailedDownloaderSection = true;
                            }

                            Process.Start(serverPath.Replace(@"/", @"\") + @"\");
                        }
                    }
                    else
                        StitchTiles();
                });
            });
        }

        private void StitchTiles()
        {
            stitchingElevationTiles = true;
            showProgressElevation = true;

            RunAsync(() =>
            {
                string[] fileNames = LogicalComparer(directoryPathElevation, ".raw");
                int length = fileNames.Length;

                int grid = (int)Mathf.Sqrt(length);
                int index = 0;

                string tileName;
                string tileNameRgt;
                string tileNameTop;

                byte[] buffer;
                byte[] bufferRgt;
                byte[] bufferTop;

                bool hasTop = false;
                bool hasRgt = false;

                int resolution = tiffWidth + 1;
                int depth = 2;
                int count = resolution * depth;

                for (int i = 0; i < grid; i++)
                {
                    for (int j = 0; j < grid; j++)
                    {
                        tileName = fileNames[index];

                        if (i > 0)
                            hasTop = true;
                        else
                            hasTop = false;

                        if (j < grid - 1)
                            hasRgt = true;
                        else
                            hasRgt = false;

                        using (BinaryReader reader = new BinaryReader(File.Open(tileName, FileMode.Open, FileAccess.Read)))
                        {
                            buffer = reader.ReadBytes((resolution * resolution) * depth);
                            reader.Close();
                        }

                        if (hasTop && hasRgt)
                        {
                            tileNameTop = fileNames[index - grid];
                            tileNameRgt = fileNames[index + 1];

                            using (BinaryReader reader = new BinaryReader(File.Open(tileNameTop, FileMode.Open, FileAccess.Read)))
                            {
                                bufferTop = reader.ReadBytes((resolution * resolution) * depth);
                                reader.Close();
                            }

                            using (BinaryReader reader = new BinaryReader(File.Open(tileNameRgt, FileMode.Open, FileAccess.Read)))
                            {
                                bufferRgt = reader.ReadBytes((resolution * resolution) * depth);
                                reader.Close();
                            }

                            // Stitch BOTTOM's top row to TOP's bottom row
                            int offset = buffer.Length - count;

                            Buffer.BlockCopy(buffer, 0, bufferTop, offset, count);

                            FileStream fileStream = new FileStream(tileNameTop, FileMode.Create);
                            fileStream.Write(bufferTop, 0, bufferTop.Length);
                            fileStream.Close();

                            // Stitch LEFT's right column to RIGHT's left column
                            int offsetLft = count - depth;
                            int offsetRgt = 0;

                            for (int x = 0; x < resolution; x++)
                            {
                                if (x > 0)
                                    offsetLft += count;

                                offsetRgt = x * count;

                                bufferRgt[offsetRgt] = buffer[offsetLft];
                                bufferRgt[offsetRgt + 1] = buffer[offsetLft + 1];
                            }

                            FileStream fileStream2 = new FileStream(tileNameRgt, FileMode.Create);
                            fileStream2.Write(bufferRgt, 0, bufferRgt.Length);
                            fileStream2.Close();
                        }
                        else if (hasTop)
                        {
                            tileNameTop = fileNames[index - grid];

                            using (BinaryReader reader = new BinaryReader(File.Open(tileNameTop, FileMode.Open, FileAccess.Read)))
                            {
                                bufferTop = reader.ReadBytes((resolution * resolution) * depth);
                                reader.Close();
                            }

                            // Stitch BOTTOM's top row to TOP's bottom row
                            int offset = buffer.Length - count;

                            Buffer.BlockCopy(buffer, 0, bufferTop, offset, count);

                            FileStream fileStream = new FileStream(tileNameTop, FileMode.Create);
                            fileStream.Write(bufferTop, 0, bufferTop.Length);
                            fileStream.Close();
                        }
                        else if (hasRgt)
                        {
                            tileNameRgt = fileNames[index + 1];

                            using (BinaryReader reader = new BinaryReader(File.Open(tileNameRgt, FileMode.Open, FileAccess.Read)))
                            {
                                bufferRgt = reader.ReadBytes((resolution * resolution) * depth);
                                reader.Close();
                            }

                            // Stitch LEFT's right column to RIGHT's left column
                            int offsetLft = count - depth;
                            int offsetRgt = 0;

                            for (int x = 0; x < resolution; x++)
                            {
                                if (x > 0)
                                    offsetLft += count;

                                offsetRgt = x * count;

                                bufferRgt[offsetRgt] = buffer[offsetLft];
                                bufferRgt[offsetRgt + 1] = buffer[offsetLft + 1];
                            }

                            FileStream fileStream = new FileStream(tileNameRgt, FileMode.Create);
                            fileStream.Write(bufferRgt, 0, bufferRgt.Length);
                            fileStream.Close();
                        }

                        index++;

                        progressBarElevation = Mathf.InverseLerp(0, length, index);
                    }
                }

                QueueOnMainThread(() =>
                {
                    stitchingElevationTiles = false;
                    showProgressElevation = false;
                    serverSetUpElevation = true;

                    if (serverSetUpElevation && serverSetUpImagery)
                    {
                        if (failedTilesAvailable)
                        {
                            EditorUtility.DisplayDialog("FAILED TILES AVAILABLE", "There are some failed tile downloads for this session.\n\nGo to FAILED TILES DOWNLOADER section and press GET FAILED TILES button to re-download failed tiles.", "Ok");
                            showFailedDownloaderSection = true;
                        }

                        Process.Start(serverPath.Replace(@"/", @"\") + @"\");
                    }
                });
            });
        }

        private void RemoveLightmapStatic()
        {
#if UNITY_2019_2_OR_NEWER
            if (splittedTerrains)
            {
                foreach (Terrain t in croppedTerrains)
                {
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    flags = flags & ~(StaticEditorFlags.ContributeGI);
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }
            }
            else if (terrain)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(terrain.gameObject);
                flags = flags & ~(StaticEditorFlags.ContributeGI);
                GameObjectUtility.SetStaticEditorFlags(terrain.gameObject, flags);
            }
#else
            if (splittedTerrains)
            {
                foreach (Terrain t in croppedTerrains)
                {
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    flags = flags & ~(StaticEditorFlags.LightmapStatic);
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }
            }
            else if (terrain)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(terrain.gameObject);
                flags = flags & ~(StaticEditorFlags.LightmapStatic);
                GameObjectUtility.SetStaticEditorFlags(terrain.gameObject, flags);
            }
#endif
        }

        private void SaveTerrainDataASCII()
        {
            showProgressGenerateASCII = true;

            // Calculating Cell/Pixel Size in meters
            //nsBaseCmnGIS.cBaseCmnGIS baseGISTop = new nsBaseCmnGIS.cBaseCmnGIS();
            //string utmTopLeft = baseGISTop.iLatLon2UTM(top, left, ref UTMNorthingTop, ref UTMEastingTop, ref sUtmZoneTop);
            //string[] utmValuesTop = utmTopLeft.Split(',');
            //UTMNorthingTop = double.Parse(utmValuesTop[1]);
            //cellSize = Math.Abs((UTMNorthingTop - UTMNorthing) / (heightmapResolution));

            cellSize = 1;

            StreamWriter sw = new StreamWriter(directoryPathElevation + "/TerraLandWorldElevation.asc");

            sw.WriteLine("ncols         " + (tiffWidth).ToString());
            sw.WriteLine("nrows         " + (tiffLength).ToString());
            sw.WriteLine("xllcorner     " + UTMEasting);
            sw.WriteLine("yllcorner     " + UTMNorthing);
            sw.WriteLine("cellsize      " + cellSize);
            sw.WriteLine("nodata_value  " + "-9999.0");

            RAWElevationData(sw, tiffWidth, tiffLength, tiffDataASCII);

            sw.Close();

            showProgressGenerateASCII = false;
        }

        private void SaveTerrainDataASCII(float[,] cellData, string fileName)
        {
            int resolution = terrainResolutionDownloading + 1;

            // Calculating Cell/Pixel Size in meters
            //nsBaseCmnGIS.cBaseCmnGIS baseGISTop = new nsBaseCmnGIS.cBaseCmnGIS();
            //string utmTopLeft = baseGISTop.iLatLon2UTM(top, left, ref UTMNorthingTop, ref UTMEastingTop, ref sUtmZoneTop);
            //string[] utmValuesTop = utmTopLeft.Split(',');
            //UTMNorthingTop = double.Parse(utmValuesTop[1]);

            cellSize = 1;

            StreamWriter sw = new StreamWriter(fileName);

            sw.WriteLine("ncols         " + (resolution).ToString());
            sw.WriteLine("nrows         " + (resolution).ToString());
            sw.WriteLine("xllcorner     " + UTMEasting);
            sw.WriteLine("yllcorner     " + UTMNorthing);
            sw.WriteLine("cellsize      " + cellSize);
            sw.WriteLine("nodata_value  " + "-9999.0");

            RAWElevationData(sw, resolution, resolution, cellData);

            sw.Close();
        }

        private void RAWElevationData(StreamWriter sw, int width, int height, float[,] outputImageData)
        {
            string row = "";

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    row += outputImageData[i, j] + " ";
                }

                if (i < width - 1)
                    sw.Write(row.Remove(row.Length - 1) + Environment.NewLine);
                else
                    sw.Write(row.Remove(row.Length - 1));

                row = "";
            }
        }

        private void SaveTerrainDataRAW()
        {
            showProgressGenerateRAW = true;

            byte[] array = new byte[(tiffWidth * tiffLength) * 2];
            float num = 65536f;

            for (int i = 0; i < tiffWidth; i++)
            {
                for (int j = 0; j < tiffLength; j++)
                {
                    int num2 = j + i * (tiffWidth);
                    int value = (int)(((tiffDataASCII[i, j] + Mathf.Abs(lowestPoint)) / everestPeak) * num);
                    ushort value2 = (ushort)Mathf.Clamp(value, 0, 65535);
                    byte[] bytes = BitConverter.GetBytes(value2);
                    array[num2 * 2] = bytes[0];
                    array[num2 * 2 + 1] = bytes[1];

                    progressGenerateRAW = Mathf.InverseLerp(0f, (float)tiffWidth, (float)i);
                }
            }

            FileStream fileStream = new FileStream(directoryPathElevation + "/TerraLandWorldElevation.raw", FileMode.Create);
            fileStream.Write(array, 0, array.Length);
            fileStream.Close();
        }

        private void SaveTerrainDataRAW(float[,] cellData, string fileName)
        {
            int resolution = terrainResolutionDownloading + 1;
            byte[] array = new byte[(resolution * resolution) * 2];
            float num = 65536f;

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    int num2 = j + i * (resolution);
                    int value = (int)(((cellData[i, j] + Mathf.Abs(lowestPoint)) / everestPeak) * num);
                    ushort value2 = (ushort)Mathf.Clamp(value, 0, 65535);
                    byte[] bytes = BitConverter.GetBytes(value2);
                    array[num2 * 2] = bytes[0];
                    array[num2 * 2 + 1] = bytes[1];
                }
            }

            FileStream fileStream = new FileStream(fileName, FileMode.Create);
            fileStream.Write(array, 0, array.Length);
            fileStream.Close();
        }

        private void SaveTerrainDataTIFF()
        {
            fileNameTerrainDataSaved = directoryPathElevation + "/TerraLandWorldElevation.tif";

            if (File.Exists(fileNameTerrainDataSaved))
            {
                File.SetAttributes(fileNameTerrainDataSaved, FileAttributes.Normal);
                File.Delete(fileNameTerrainDataSaved);
            }

            File.Move(fileNameTerrainData, fileNameTerrainDataSaved);
            File.SetAttributes(fileNameTerrainDataSaved, FileAttributes.Normal);

            AssetDatabase.Refresh();
        }

        /*
        private void GenerateProjFile ()
        {
            string savePath = "";

            if(!dynamicWorld)
                savePath = directoryPathElevation + "/TerraLandWorldElevation.prj";
            else
                savePath = directoryPathInfo + "/TerraLandWorldElevation.prj";

            nsBaseCmnGIS.cBaseCmnGIS gis = new nsBaseCmnGIS.cBaseCmnGIS();
            string sGeoGCS = "GCS_WGS_1984",
            sUnit = "UNIT[\"Degree\",0.017453292519943295]", // ie, hard code: dCvtDeg2Rad.ToString()
            sEquatorialRadius = gis.dEquatorialRadius.ToString(), // ie, 6378137.0
            sDenominatorOfFlatteningRatio = gis.dDenominatorOfFlatteningRatio.ToString(), // ie, 298.257223563
            sSpheroid = "SPHEROID[\"WGS_1984\"," + sEquatorialRadius + "," + sDenominatorOfFlatteningRatio + "]";
            int iZoneNumber = Convert.ToInt32(sUtmZone.Substring(0, sUtmZone.Length - 1));
            double dCentralMeridian = gis.dSet_CentralMeridian_from_UtmZone(iZoneNumber);
            sCentralMeridian = dCentralMeridian.ToString("0.0");

            projectionStr = "PROJCS[\"WGS_1984_UTM_Zone_" + sUtmZone + "\",GEOGCS[\"" + sGeoGCS + "\"," +
                "DATUM[\"D_WGS_1984\"," + sSpheroid + "]," +
                    "PRIMEM[\"Greenwich\",0.0]," + sUnit + "]," + // ends the PROJCS[xxxx]]
                    "PROJECTION[\"Transverse_Mercator\"]," +
                    "PARAMETER[\"False_Easting\",500000.0]," +
                    "PARAMETER[\"False_Northing\",0.0]," +
                    "PARAMETER[\"Central_Meridian\"," + sCentralMeridian + "]," +
                    "PARAMETER[\"Scale_Factor\",0.9996]," +
                    "PARAMETER[\"Latitude_Of_Origin\",0.0]," +
                    "UNIT[\"Meter\",1.0]]";

            nsBaseFio.BaseFio bFio = new nsBaseFio.BaseFio();
            bFio.iWriteStringToFile_ASCII(savePath, projectionStr);
        }
        */

        private void GenerateXMLFile()
        {
            string savePath = "";

            if (!dynamicWorld)
                savePath = directoryPathElevation + "/TerraLandWorldElevation.xml";
            else
                savePath = directoryPathInfo + "/TerraLandWorldElevation.xml";

            new XDocument(
                new XElement("Coordinates",
                    new XElement("Latitude", latitudeUser),
                    new XElement("Longitude", longitudeUser),
                    new XElement("Top", top),
                    new XElement("Left", left),
                    new XElement("Bottom", bottom),
                    new XElement("Right", right),
                    new XElement("LatExtents", areaSizeLat.ToString()),
                    new XElement("LonExtents", areaSizeLon.ToString())
                )
            )
            .Save(savePath);
        }

        private void LoadTerrainHeightsFromTIFF()
        {
            CalculateResampleHeightmaps();

            int counter = 0;
            int currentRow = splitSizeFinal - 1;
            int xLength = heightmapResFinalX;
            int yLength = heightmapResFinalY;

            if (splittedTerrains)
            {
                for (int i = 0; i < splitSizeFinal; i++)
                {
                    for (int j = 0; j < splitSizeFinal; j++)
                    {
                        croppedTerrains[counter].terrainData.heightmapResolution = heightmapResFinalX;
                        tiffDataSplitted = new float[heightmapResFinalX, heightmapResFinalY];

                        int xStart = (currentRow * (heightmapResFinalX - 1));
                        int yStart = (j * (heightmapResFinalY - 1));

                        for (int x = 0; x < xLength; x++)
                            for (int y = 0; y < yLength; y++)
                                tiffDataSplitted[x, y] = finalHeights[xStart + x, yStart + y];

                        croppedTerrains[counter].terrainData.SetHeights(0, 0, tiffDataSplitted);

                        float realTerrainWidth = areaSizeLon * 1000.0f / splitSizeFinal;
                        float realTerrainLength = areaSizeLat * 1000.0f / splitSizeFinal;
                        croppedTerrains[counter].terrainData.size = new Vector3(realTerrainWidth, everestPeak * elevationExaggeration, realTerrainLength);
                        //croppedTerrains[counter].terrainData.size = new Vector3(tileWidth, everestPeak * elevationExaggeration, tileLength);

                        croppedTerrains[counter].Flush();

                        counter++;

                        EditorUtility.DisplayProgressBar("LOADING HEIGHTS", "Terrain  " + (counter + 1).ToString() + "  of  " + terrainChunks, Mathf.InverseLerp(0f, (float)(terrainChunks - 1), (float)(counter)));
                    }
                    currentRow--;
                }
            }
            else if (terrain)
            {
                terrain.terrainData.heightmapResolution = heightmapResFinalXAll;
                EditorUtility.DisplayProgressBar("LOADING HEIGHTS", "Loading Terrain Heights", Mathf.InverseLerp(0f, 1f, 1f));
                terrain.terrainData.SetHeights(0, 0, finalHeights);
                float realTerrainWidth = areaSizeLon * 1000.0f;
                float realTerrainLength = areaSizeLat * 1000.0f;
                terrain.terrainData.size = new Vector3(realTerrainWidth, everestPeak * elevationExaggeration, realTerrainLength);

                terrain.Flush();
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private void TiffDataFast(string fileName, int row, int column)
        {
            int resolution = terrainResolutionDownloading + 1;

            try
            {
                using (Tiff inputImage = Tiff.Open(fileName, "r"))
                {
                    tiffWidth = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    tiffLength = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    tiffData = new float[resolution, resolution];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    heightmapResXAll = tiffWidth;
                    heightmapResYAll = tiffLength;

                    for (int y = 0; y < tiffLength; y += tileHeight)
                    {
                        for (int x = 0; x < tiffWidth; x += tileWidth)
                        {
                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                                for (int j = 0; j < tileWidth; j++)
                                    if ((y + i) < tiffLength && (x + j) < tiffWidth)
                                        tiffData[y + i, x + j] = fBuffer[i, j];
                        }
                    }
                }
            }
            catch { }

            // Add Bottom Row (PO2 + 1 Resolution)
            for (int i = 0; i < resolution; i++)
                tiffData[i, resolution - 1] = tiffData[i, resolution - 2];

            // Add Right Column (PO2 + 1 Resolution)
            for (int i = 0; i < resolution; i++)
                tiffData[resolution - 1, i] = tiffData[resolution - 2, i];

            if (smoothIterations > 0)
                tiffData = SmoothedHeightsFast(tiffData, resolution, resolution, smoothIterations);

            if (formatIndex == 0)
            {
                string fileNameRaw = directoryPathElevation + "/" + row + "-" + column + ".raw";
                SaveTerrainDataRAW(tiffData, fileNameRaw);
            }
            else if (formatIndex == 1)
            {
                string fileNameAsc = directoryPathElevation + "/" + row + "-" + column + ".asc";
                SaveTerrainDataASCII(tiffData, fileNameAsc);
            }

            File.Delete(fileName);
        }

        private void TiffDataFast(string fileName)
        {
            int resolution = terrainResolutionDownloading + 1;
            string trim = fileName.Substring(fileName.LastIndexOf(@"\") + 1);
            string row = trim.Substring(0, trim.LastIndexOf("-"));
            string column = fileName.Substring(fileName.LastIndexOf("-") + 1).Replace(".tif", "");

            try
            {
                using (Tiff inputImage = Tiff.Open(fileName, "r"))
                {
                    tiffWidth = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    tiffLength = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    tiffData = new float[resolution, resolution];

                    int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                    byte[] buffer = new byte[tileHeight * tileWidth * 4];
                    float[,] fBuffer = new float[tileHeight, tileWidth];

                    heightmapResXAll = tiffWidth;
                    heightmapResYAll = tiffLength;

                    for (int y = 0; y < tiffLength; y += tileHeight)
                    {
                        for (int x = 0; x < tiffWidth; x += tileWidth)
                        {
                            inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                            Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                            for (int i = 0; i < tileHeight; i++)
                                for (int j = 0; j < tileWidth; j++)
                                    if ((y + i) < tiffLength && (x + j) < tiffWidth)
                                        tiffData[y + i, x + j] = fBuffer[i, j];
                        }
                    }
                }
            }
            catch { }

            // Add Bottom Row (PO2 + 1 Resolution)
            for (int i = 0; i < resolution; i++)
                tiffData[i, resolution - 1] = tiffData[i, resolution - 2];

            // Add Right Column (PO2 + 1 Resolution)
            for (int i = 0; i < resolution; i++)
                tiffData[resolution - 1, i] = tiffData[resolution - 2, i];

            if (smoothIterations > 0)
                tiffData = SmoothedHeightsFast(tiffData, resolution, resolution, smoothIterations);

            if (formatIndex == 0)
            {
                string fileNameRaw = directoryPathElevation + "/" + row + "-" + column + ".raw";
                SaveTerrainDataRAW(tiffData, fileNameRaw);
            }
            else if (formatIndex == 1)
            {
                string fileNameAsc = directoryPathElevation + "/" + row + "-" + column + ".asc";
                SaveTerrainDataASCII(tiffData, fileNameAsc);
            }

            File.Delete(fileName);
        }

        private void TiffData(string fileName)
        {
            highestPoint = float.MinValue;
            lowestPoint = float.MaxValue;

            using (Tiff inputImage = Tiff.Open(fileName, "r"))
            {
                tiffWidth = inputImage.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                tiffLength = inputImage.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                tiffData = new float[tiffLength, tiffWidth];
                tiffDataASCII = new float[tiffLength, tiffWidth];

                int tileHeight = inputImage.GetField(TiffTag.TILELENGTH)[0].ToInt();
                int tileWidth = inputImage.GetField(TiffTag.TILEWIDTH)[0].ToInt();

                byte[] buffer = new byte[tileHeight * tileWidth * 4];
                float[,] fBuffer = new float[tileHeight, tileWidth];

                heightmapResXAll = tiffWidth;
                heightmapResYAll = tiffLength;

                for (int y = 0; y < tiffLength; y += tileHeight)
                {
                    for (int x = 0; x < tiffWidth; x += tileWidth)
                    {
                        inputImage.ReadTile(buffer, 0, x, y, 0, 0);
                        Buffer.BlockCopy(buffer, 0, fBuffer, 0, buffer.Length);

                        for (int i = 0; i < tileHeight; i++)
                        {
                            for (int j = 0; j < tileWidth; j++)
                            {
                                if ((y + i) < tiffLength && (x + j) < tiffWidth)
                                {
                                    float current = fBuffer[i, j];
                                    tiffDataASCII[y + i, x + j] = current;

                                    if (i > 0 && i < tileHeight - 1 && j > 0 && j < tileWidth - 1)
                                    {
                                        if (highestPoint < current)
                                            highestPoint = current;

                                        if (lowestPoint > current)
                                            lowestPoint = current;
                                    }
                                }
                            }
                        }

                        progressDATA = Mathf.InverseLerp(0f, (float)tiffLength, (float)y);
                    }
                }
            }

            // Rotate terrain heigts and normalize values
            for (int y = 0; y < tiffWidth; y++)
            {
                for (int x = 0; x < tiffLength; x++)
                {
                    currentHeight = tiffDataASCII[(tiffWidth - 1) - y, x];

                    try
                    {
                        if (lowestPoint >= 0)
                            //tiffData[y, x] = (currentHeight - lowestPoint) / everestPeak;
                            tiffData[y, x] = currentHeight / everestPeak;
                        else
                            tiffData[y, x] = (currentHeight + Mathf.Abs(lowestPoint)) / everestPeak;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        tiffData[y, x] = 0f;
                    }

                    // Check Terrain Corners
                    // Top Row
                    if (y == 0)
                        topCorner.Add(currentHeight);

                    // Bottom Row
                    else if (y == tiffWidth - 1)
                        bottomCorner.Add(currentHeight);

                    // Left Column
                    if (x == 0)
                        leftCorner.Add(currentHeight);

                    // Right Column
                    else if (x == tiffLength - 1)
                        rightCorner.Add(currentHeight);
                }
            }

            CheckCornersTIFF();
        }

        private void CheckCornersTIFF()
        {
            // Check Top
            if (topCorner.All(o => o == topCorner.First()))
            {
                for (int y = 0; y < tiffWidth; y++)
                    for (int x = 0; x < tiffLength; x++)
                        if (y == 0)
                            tiffData[y, x] = tiffData[y + 1, x];
            }

            // Check Bottom
            if (bottomCorner.All(o => o == bottomCorner.First()))
            {
                for (int y = 0; y < tiffWidth; y++)
                    for (int x = 0; x < tiffLength; x++)
                        if (y == tiffWidth - 1)
                            tiffData[y, x] = tiffData[y - 1, x];
            }

            // Check Left
            if (leftCorner.All(o => o == leftCorner.First()))
            {
                for (int y = 0; y < tiffWidth; y++)
                    for (int x = 0; x < tiffLength; x++)
                        if (x == 0)
                            tiffData[y, x] = tiffData[y, x + 1];
            }

            // Check Right
            if (rightCorner.All(o => o == rightCorner.First()))
            {
                for (int y = 0; y < tiffWidth; y++)
                    for (int x = 0; x < tiffLength; x++)
                        if (x == tiffLength - 1)
                            tiffData[y, x] = tiffData[y, x - 1];
            }
        }

        private void CalculateResampleHeightmaps()
        {
            // Set chunk resolutions to a "Previous Power of 2" value
            if (splittedTerrains)
            {
                if (!Mathf.IsPowerOfTwo(croppedTerrains.Count))
                {
                    heightmapResFinalX = ((Mathf.NextPowerOfTwo(reducedheightmapResolution / splitSizeFinal)) / 2) + 1;
                    heightmapResFinalY = ((Mathf.NextPowerOfTwo(reducedheightmapResolution / splitSizeFinal)) / 2) + 1;
                    heightmapResFinalXAll = heightmapResFinalX * splitSizeFinal;
                    heightmapResFinalYAll = heightmapResFinalY * splitSizeFinal;

                    ResampleOperation();
                }
                else
                {
                    heightmapResolutionSplit = reducedheightmapResolution / (int)Mathf.Sqrt((float)terrainChunks);

                    heightmapResFinalX = heightmapResolutionSplit + 1;
                    heightmapResFinalY = heightmapResolutionSplit + 1;
                    heightmapResFinalXAll = terrainResolutionDownloading;
                    heightmapResFinalYAll = terrainResolutionDownloading;

                    finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                    finalHeights = tiffData;
                }
            }
            else if (terrain)
            {
                heightmapResFinalX = terrainResolutionDownloading;
                heightmapResFinalY = terrainResolutionDownloading;
                heightmapResFinalXAll = terrainResolutionDownloading;
                heightmapResFinalYAll = terrainResolutionDownloading;

                finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                finalHeights = tiffData;
            }
            else
            {
                if (!Mathf.IsPowerOfTwo(splitSizeNew))
                {
                    heightmapResFinalX = ((Mathf.NextPowerOfTwo(reducedheightmapResolution / splitSizeNew)) / 2) + 1;
                    heightmapResFinalY = ((Mathf.NextPowerOfTwo(reducedheightmapResolution / splitSizeNew)) / 2) + 1;
                    heightmapResFinalXAll = heightmapResFinalX * splitSizeNew;
                    heightmapResFinalYAll = heightmapResFinalY * splitSizeNew;

                    ResampleOperation();
                }
                else
                {
                    heightmapResFinalX = (reducedheightmapResolution / splitSizeNew) + 1;
                    heightmapResFinalY = (reducedheightmapResolution / splitSizeNew) + 1;
                    heightmapResFinalXAll = terrainResolutionDownloading;
                    heightmapResFinalYAll = terrainResolutionDownloading;

                    finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];
                    finalHeights = tiffData;
                }
            }
        }

        private void ResampleOperation()
        {
            float scaleFactorLat = ((float)heightmapResFinalXAll) / ((float)heightmapResXAll);
            float scaleFactorLon = ((float)heightmapResFinalYAll) / ((float)heightmapResYAll);

            finalHeights = new float[heightmapResFinalXAll, heightmapResFinalYAll];

            for (int x = 0; x < heightmapResFinalXAll; x++)
            {
                for (int y = 0; y < heightmapResFinalYAll; y++)
                {
                    finalHeights[x, y] = ResampleHeights((float)x / scaleFactorLat, (float)y / scaleFactorLon);
                }
            }
        }

        private float ResampleHeights(float X, float Y)
        {
            try
            {
                int X1 = Mathf.RoundToInt((X + heightmapResXAll % heightmapResXAll));
                int Y1 = Mathf.RoundToInt((Y + heightmapResYAll % heightmapResYAll));

                return tiffData[X1, Y1];
            }
            catch
            {
                return 0f;
            }
        }

        private void FinalizeSmooth(float[,] heightMapSmoothed, int width, int height, int iterations, int blendIndex, float blending)
        {
            if (iterations != 0)
            {
                int Tw = width;
                int Th = height;

                if (blendIndex == 1)
                {
                    float[,] generatedHeightMap = (float[,])heightMapSmoothed.Clone();
                    generatedHeightMap = SmoothedHeights(generatedHeightMap, Tw, Th, iterations);

                    showProgressSmoothenOperation = true;

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

                        smoothProgress = Mathf.InverseLerp(0f, (float)Th, (float)Ty);
                    }
                }
                else
                    heightMapSmoothed = SmoothedHeights(heightMapSmoothed, Tw, Th, iterations);

                //tiffData = heightMapSmoothed;
            }
        }

        private float[,] SmoothedHeights(float[,] heightMap, int tw, int th, int iterations)
        {
            showProgressSmoothen = true;

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
                                if (neighbourhood == Neighbourhood.Moore || (neighbourhood == Neighbourhood.VonNeumann && (Nx == xIndex || Ny == yIndex)))
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

                smoothIterationProgress = iter + 1;
            }

            return heightMap;
        }

        private float[,] SmoothedHeightsFast(float[,] heightMap, int tw, int th, int iterations)
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
                                if (neighbourhood == Neighbourhood.Moore || (neighbourhood == Neighbourhood.VonNeumann && (Nx == xIndex || Ny == yIndex)))
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

        private void SetWorldSize()
        {
            terrainSizeNewX = areaSizeLon * 1000f * scaleFactor;
            terrainSizeNewZ = areaSizeLat * 1000f * scaleFactor;
        }

        private void SetData()
        {
            SetWorldSize();
            tileWidth = terrainSizeNewX / splitSizeFinal;
            tileLength = terrainSizeNewZ / splitSizeFinal;
            tileXPos = (terrainSizeNewX / 2f) * -1f;
            tileZPos = (terrainSizeNewZ / 2f) * -1f;
        }

        private void CreateTerrainData()
        {
            data = new TerrainData[splitSizeFinal * splitSizeFinal];
            //terrainName = "Terrain";
            //
            //for (int y = 0; y < splitSizeFinal; y++)
            //{
            //    for (int x = 0; x < splitSizeFinal; x++)
            //    {
            //        AssetDatabase.CreateAsset(new TerrainData(), splitDirectoryPath.Substring(splitDirectoryPath.LastIndexOf("Assets")) + "/" + terrainName + " " + (y + 1) + "-" + (x + 1) + ".asset");
            //        EditorUtility.DisplayProgressBar("CREATING DATA", "Creating Terrain Data Assets", Mathf.InverseLerp(0f, splitSizeFinal, y));
            //    }
            //}

            EditorUtility.ClearProgressBar();
        }

        private void CreateTerrainObject()
        {
            terrainGameObjects = new GameObject[splitSizeFinal * splitSizeFinal];
            terrains = new Terrain[splitSizeFinal * splitSizeFinal];
            arrayPos = 0;

            if (splitSizeFinal > 1)
            {
                if (address != "")
                    terrainsParent = new GameObject("Terrains  " + downloadDateStr + "  ---  " + address + "  " + splitSizeFinal + "x" + splitSizeFinal);
                else
                    terrainsParent = new GameObject("Terrains  " + downloadDateStr + "  ---  " + splitSizeFinal + "x" + splitSizeFinal);
            }

            int currentRow = splitSizeFinal;

            for (int y = 0; y < splitSizeFinal; y++)
            {
                for (int x = 0; x < splitSizeFinal; x++)
                {
                    TerrainData td = (TerrainData)AssetDatabase.LoadAssetAtPath(splitDirectoryPath.Substring(splitDirectoryPath.LastIndexOf("Assets")) + "/" + terrainName + " " + (currentRow) + "-" + (x + 1) + ".asset", typeof(TerrainData)) as TerrainData;
                    terrainGameObjects[arrayPos] = Terrain.CreateTerrainGameObject(td);

                    terrainGameObjects[arrayPos].name = terrainName + " " + (currentRow) + "-" + (x + 1);
                    terrains[arrayPos] = terrainGameObjects[arrayPos].GetComponent<Terrain>();

#if UNITY_2018_3_OR_NEWER
                    terrains[arrayPos].drawInstanced = true;
                    terrains[arrayPos].groupingID = 0;
                    terrains[arrayPos].allowAutoConnect = true;
#endif

                    data[arrayPos] = terrains[arrayPos].terrainData;
                    data[arrayPos].heightmapResolution = 32;
                    data[arrayPos].size = new Vector3(tileWidth, terrainSizeNewY, tileLength);

                    terrainGameObjects[arrayPos].GetComponent<TerrainCollider>().terrainData = data[arrayPos];
                    terrainGameObjects[arrayPos].transform.position = new Vector3(x * tileWidth + tileXPos, 0, y * tileLength + tileZPos);

                    arrayPos++;

                    EditorUtility.DisplayProgressBar("CREATING TERRAIN", "Creating Terrain Objects", Mathf.InverseLerp(0f, splitSizeFinal, y));
                }
                currentRow--;
            }

            EditorUtility.ClearProgressBar();

            if (splitSizeFinal > 1)
            {
                int length = terrainGameObjects.Length;
                string[] terrainNames = new string[length];
                GameObject tempParent = new GameObject("Temp Parent");

                for (int i = 0; i < terrainGameObjects.Length; i++)
                {
                    terrainNames[i] = terrainGameObjects[i].name;
                    terrainGameObjects[i].transform.parent = tempParent.transform;
                }

                terrainNames = LogicalComparer(terrainNames);

                for (int i = 0; i < length; i++)
                {
                    terrainGameObjects[i] = tempParent.transform.Find(terrainNames[i]).gameObject;
                    terrainGameObjects[i].transform.parent = terrainsParent.transform;
                }

                DestroyImmediate(tempParent);
            }

            int terrainsCount = terrains.Length;

            for (int y = 0; y < terrainsCount; y++)
                terrains[y].heightmapPixelError = pixelError;
        }

        //	private void RepositionTerrainChunks ()
        //	{
        //		int counter = 0;
        //		
        //		for(int y = 0; y < terrainsLong ; y++)
        //		{
        //			for(int x = 0; x < terrainsWide; x++)
        //			{
        //				//croppedTerrains[counter].terrainData.size = new Vector3(newWidth, oldHeight, newLength);
        //				croppedTerrains[counter].transform.position = new Vector3(x * newWidth + xPos, yPos, y * newLength + zPos);
        //				
        //				counter++;
        //			}
        //		}
        //	}

        private void SetTerrainNeighbors()
        {
#if UNITY_2018_3_OR_NEWER
            for (int i = 0; i < (int)Mathf.Pow(splitSizeFinal, 2); i++)
            {
                croppedTerrains[i].groupingID = 0;
                croppedTerrains[i].allowAutoConnect = true;
            }
#else
        terrainsLong = splitSizeFinal;
        terrainsWide = splitSizeFinal;
        arrayPos = 0;
		
		for(int y = 0; y < terrainsLong ; y++)
		{
			for(int x = 0; x < terrainsWide; x++)
			{
				try
                {
					int indexLft = arrayPos - 1;
					int indexTop = arrayPos - terrainsWide;
					int indexRgt = arrayPos + 1;
					int indexBtm = arrayPos + terrainsWide;

					if(y == 0)
					{
						if(x == 0)
							croppedTerrains[arrayPos].SetNeighbors(null, null, croppedTerrains[indexRgt], croppedTerrains[indexBtm]);
						else if(x == terrainsWide - 1)
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], null, null, croppedTerrains[indexBtm]);
						else
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], null, croppedTerrains[indexRgt], croppedTerrains[indexBtm]);
					}
					else if(y == terrainsLong - 1)
					{
						if(x == 0)
							croppedTerrains[arrayPos].SetNeighbors(null, croppedTerrains[indexTop], croppedTerrains[indexRgt], null);
						else if(x == terrainsWide - 1)
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], croppedTerrains[indexTop], null, null);
						else
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], croppedTerrains[indexTop], croppedTerrains[indexRgt], null);
					}
					else
					{
						if(x == 0)
							croppedTerrains[arrayPos].SetNeighbors(null, croppedTerrains[indexTop], croppedTerrains[indexRgt], croppedTerrains[indexBtm]);
						else if(x == terrainsWide - 1)
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], croppedTerrains[indexTop], null, croppedTerrains[indexBtm]);
						else
							croppedTerrains[arrayPos].SetNeighbors(croppedTerrains[indexLft], croppedTerrains[indexTop], croppedTerrains[indexRgt], croppedTerrains[indexBtm]);
					}
					
					arrayPos++;
				}
				catch{}
				
				EditorUtility.DisplayProgressBar("SETTING NEIGHBORS", "Setting Terrain Neighbors", Mathf.InverseLerp(0f, terrainsWide, y));
			}
		}

		for(int i = 0; i < terrainsWide * terrainsLong ; i++)
			croppedTerrains[i].Flush();
		
		EditorUtility.ClearProgressBar();
#endif
        }

        //	private static void StitchTerrains (Terrain[] terrainsLst, int stitchWidthInt, int terrainResInt, int tWidthInt, int tHeightInt)
        //	{
        //		foreach (Terrain t in terrainsLst) {
        //			Undo.RecordObject(t.terrainData, "Stitching Terrain");
        //		}
        //		
        //		stitchWidthInt = Mathf.Clamp(stitchWidthInt, 1, (terrainResInt - 1) / 2);
        //		int counter = 0;
        //		int total = tHeightInt * (tWidthInt - 1) + (tHeightInt - 1) * tWidthInt;
        //		
        //		for (int h = 0; h < tHeightInt; h++) {
        //			for (int w = 0; w < tWidthInt - 1; w++) {
        //				EditorUtility.DisplayProgressBar("STITCHING TERRAINS", "Stitching Terrain Tiles", Mathf.InverseLerp(0, total, ++counter));
        //				BlendData (terrainsLst[h * tWidthInt + w].terrainData, terrainsLst[h * tWidthInt + w + 1].terrainData, Direction.Across, false);
        //			}
        //		}
        //		
        //		for (int h = 0; h < tHeightInt - 1; h++) {
        //			for (int w = 0; w < tWidthInt; w++) {
        //				EditorUtility.DisplayProgressBar("STITCHING TERRAINS", "Stitching Terrain Tiles", Mathf.InverseLerp(0, total, ++counter));
        //				BlendData (terrainsLst[h * tWidthInt + w].terrainData, terrainsLst[(h + 1) * tWidthInt + w].terrainData, Direction.Down, false);
        //			}
        //		}
        //		
        //		EditorUtility.ClearProgressBar();
        //	}
        //	
        //	private static void BlendData (TerrainData terrain1, TerrainData terrain2, Direction thisDirection, bool singleTerrain) {
        //		
        //		float[,] heightmapData = terrain1.GetHeights(0, 0, terrainRes, terrainRes);
        //		float[,] heightmapData2 = terrain2.GetHeights(0, 0, terrainRes, terrainRes);
        //		int pos = terrainRes - 1;
        //		
        //		if (thisDirection == Direction.Across) {
        //			for (int i = 0; i < terrainRes; i++) {
        //				for (int j = 1; j < stitchWidth; j++) {
        //					
        //					float mix = Mathf.Lerp(heightmapData[i, pos - j], heightmapData2[i, j], .5f);
        //					
        //					if (j == 1) {
        //						heightmapData[i, pos] = mix;
        //						heightmapData2[i, 0] = mix;
        //					}
        //					
        //					float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(1, stitchWidth - 1, j));
        //					heightmapData[i, pos - j] = Mathf.Lerp(mix, heightmapData[i, pos - j], t);
        //					
        //					if (!singleTerrain)
        //						heightmapData2[i, j] = Mathf.Lerp(mix, heightmapData2[i, j], t);
        //					else
        //						heightmapData[i, j] = Mathf.Lerp(mix, heightmapData2[i, j], t);
        //				}
        //			}
        //			if (singleTerrain) {
        //				for (int i = 0; i < terrainRes; i++) {
        //					heightmapData[i, 0] = heightmapData[i, pos];
        //				}
        //			}
        //		}
        //		else {
        //			for (int i = 0; i < terrainRes; i++) {
        //				for (int j = 1; j < stitchWidth; j++) {
        //					
        //					float mix = Mathf.Lerp(heightmapData2[pos - j, i], heightmapData[j, i], .5f);
        //					
        //					if (j == 1) {
        //						heightmapData2[pos, i] = mix;
        //						heightmapData[0, i] = mix;
        //					}
        //					
        //					float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(1, stitchWidth - 1, j));
        //					
        //					if (!singleTerrain) {
        //						heightmapData2[pos - j, i] = Mathf.Lerp(mix, heightmapData2[pos - j, i], t);
        //					}
        //					else {
        //						heightmapData[pos - j, i] = Mathf.Lerp(mix, heightmapData2[pos - j, i], t);
        //					}
        //					
        //					heightmapData[j, i] = Mathf.Lerp(mix, heightmapData[j, i], t);
        //				}
        //			}
        //			if (singleTerrain) {
        //				for (int i = 0; i < terrainRes; i++) {
        //					heightmapData[pos, i] = heightmapData[0, i];
        //				}
        //			}
        //		}
        //		
        //		terrain1.SetHeights(0, 0, heightmapData);
        //		
        //		if (!singleTerrain) {
        //			terrain2.SetHeights(0, 0, heightmapData2);
        //		}
        //	}

        private void SetupImagery()
        {
            cancelOperation = false;
            SetGridParams();

            if (!failedDownloading)
            {
                if (!dynamicWorld && !cancelOperation)
                    SetTerrainSizes();

                //if (!cancelOperation)
                //SetSaveDirectories();
            }
            else
            {
                if (!dynamicWorld)
                {
                    SetTerrainSizes();
                    SetTerrainsInProgress();
                }
            }

            if (cancelOperation)
                return;

            if (!dynamicWorld)
                SetTempDirectory();

            SetProgressBarImagery();
        }

        private void SetGridParams()
        {
            if (splittedTerrains)
            {
                CheckTerrainChunks();
                splitSizeFinal = (int)Mathf.Sqrt((float)croppedTerrains.Count);
            }
            else if (terrain)
            {
                terrainChunks = 1;
                splitSizeFinal = 1;
            }
        }

        private void SetProgressBarImagery()
        {
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

            if (showProgressElevation)
                allThreads = workerThreads + 2;
            else
                allThreads = workerThreads;

            imageDownloadingStarted = true;
            cancelOperation = false;
            showProgressImagery = true;
            finishedImporting = false;
            allBlack = false;
            downloadedImageIndex = 0;
            maxThreads = maxAsyncCalls + 1;

            EditorApplication.update += CheckThreadStatusImageDownloader;
            EditorGUILayout.HelpBox(completionPortThreads.ToString(), MessageType.None);
        }

        private void SetTempDirectory()
        {
            if (Directory.Exists(projectPath + "Temporary Imagery Data"))
                Directory.Delete(projectPath + "Temporary Imagery Data", true);

            Directory.CreateDirectory(projectPath + "Temporary Imagery Data");
        }

        private void GetPresetInfo()
        {
            if (!dynamicWorld)
                infoFilePath = Directory.GetFiles(AssetDatabase.GetAssetPath(failedFolder), "*.tlps", SearchOption.AllDirectories);
            else
                infoFilePath = Directory.GetFiles(directoryPathInfo, "*.tlps", SearchOption.AllDirectories);

            presetFilePath = infoFilePath[0];

            if (infoFilePath.Length == 0)
            {
                EditorUtility.DisplayDialog("TERRAIN INFO NOT AVILABLE", "There must be a text file \"Terrain Info\" in selected folder to continue.\n\nTerrain Info will be automatically created when you start downloading satellite images.", "Ok");
                return;
            }

            if (presetFilePath.Contains("tlps"))
                ReadPresetFile();
        }

        private void SetTerrainSizes()
        {
            if (splittedTerrains)
            {
                float tsX = 0;
                float tsY = 0;

                foreach (Terrain tr in croppedTerrains)
                {
                    tsX += tr.terrainData.size.x;
                    tsY += tr.terrainData.size.z;
                }

                terrainSizeX = tsX;
                terrainSizeY = tsY;
            }
            else if (terrain)
            {
                terrainSizeX = terrain.terrainData.size.x;
                terrainSizeY = terrain.terrainData.size.z;
            }
            else
            {
                if (textureOnFinish == 0)
                {
                    EditorUtility.DisplayDialog("UNAVAILABLE TERRAIN", unavailableTerrainStr, "Ok");
                    cancelOperation = true;
                    showProgressImagery = false;
                    ThrowException(unavailableTerrainStr);
                }
            }
        }

        private void SetTerrainsInProgress()
        {
            if (splittedTerrains)
            {
                AssetDatabase.Refresh();
                failedTerrainNames = new List<string>();

                foreach (Terrain t in croppedTerrains)
                {
                    try
                    {
                        if (t.terrainData.terrainLayers != null && t.terrainData.terrainLayers.Length > 0)
                        {
                            int splatCount = t.terrainData.terrainLayers.Length;

                            for (int i = 0; i < splatCount; i++)
                            {
                                string textureName = t.terrainData.terrainLayers[i].diffuseTexture.name;

                                if (textureName.Contains(tempPattern))
                                    failedTerrainNames.Add(t.name);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void GetSatelliteImages()
        {
            RunAsync(() =>
            {
                ServerInfoImagery();

                QueueOnMainThread(() =>
                {
                    if (cancelOperation)
                    {
                        showProgressImagery = false;
                        AssetDatabase.Refresh();
                        return;
                    }

                    AssetDatabase.Refresh();
                });
            });
        }

        private void ServerInfoImagery()
        {
            mapserviceImagery = new TerraLandWorldImagery.World_Imagery_MapServer();

            //TileImageInfo tileImageInfo = mapservice.GetTileImageInfo(mapserviceImagery.GetDefaultMapName());
            //tileImageInfo.CompressionQuality = compressionQuality;
            //mapinfo = mapservice.GetServerInfo(mapserviceImagery.GetDefaultMapName());
            //mapdesc = mapinfo.DefaultMapDescription;

            if (!failedDownloading)
            {
                for (int i = 0; i < totalImages; i++)
                {
                    if (cancelOperation)
                    {
                        showProgressImagery = false;
                        return;
                    }

                    xMin[i] = lonCellLeft[i];
                    yMin[i] = latCellBottom[i];
                    xMax[i] = lonCellRight[i];
                    yMax[i] = latCellTop[i];

                    ServerConnectImagery(i, i);
                }
            }
            else
            {
                for (int i = 0; i < failedIndicesCountImagery; i++)
                {
                    if (cancelOperation)
                    {
                        showProgressImagery = false;
                        return;
                    }

                    int currentIndex = failedIndicesImagery[i];

                    xMinFailedImagery[i] = lonCellLeft[currentIndex];
                    yMinFailedImagery[i] = latCellBottom[currentIndex];
                    xMaxFailedImagery[i] = lonCellRight[currentIndex];
                    yMaxFailedImagery[i] = latCellTop[currentIndex];

                    ServerConnectImagery(i, currentIndex);
                }
            }
        }

        private void ServerConnectImagery(int i, int current)
        {
            RunAsync(() =>
            {
                ImageDownloader(i, current);

                QueueOnMainThread(() =>
                {
                    if (cancelOperation)
                    {
                        showProgressImagery = false;
                        AssetDatabase.Refresh();
                        return;
                    }

                    //if (allBlack && EditorUtility.DisplayDialog("UNAVAILABLE IMAGERY", unavailableImageryStr, "No", "Yes"))
                    //{
                    //    cancelOperation = true;
                    //    showProgressImagery = false;
                    //    imageDownloadingStarted = false;
                    //    finishedImporting = true;
                    //
                    //    if (!dynamicWorld)
                    //    {
                    //        AssetDatabase.Refresh();
                    //
                    //        if (Directory.Exists(directoryPathImagery))
                    //            Directory.Delete(directoryPathImagery, true);
                    //
                    //        if (Directory.Exists(directoryPathTerrainlayers))
                    //            Directory.Delete(directoryPathTerrainlayers, true);
                    //
                    //        AssetDatabase.Refresh();
                    //    }
                    //
                    //    allThreads = 0;
                    //    CheckHeightmapDownloaderAndRecompile();
                    //    return;
                    //}
                    //
                    //allBlack = false;

                    if (!dynamicWorld)
                    {
                        AssetDatabase.Refresh();

                        int row = Mathf.CeilToInt((float)(current + 1) / (float)gridNumber);
                        int column = (current + 1) - ((row - 1) * gridNumber);
                        string imgName = directoryPathImagery + "/" + row + "-" + column + ".jpg";
                        string failedImgName = directoryPathImagery + "/" + row + "-" + column + tempPattern + ".jpg";
                        string tempFile = projectPath + "Temporary Imagery Data/" + row + "-" + column + ".jpg";

                        if (File.Exists(tempFile))
                        {
                            // Delete temp image if already exists
                            if (File.Exists(failedImgName)) File.Delete(failedImgName);

                            // Delete previously downloaded image if already exists
                            if (File.Exists(imgName)) File.Delete(imgName);

                            string tileName = "Terrain " + row + "-" + column;
                            GameObject tile = new GameObject(tileName);
                            tile.hideFlags = HideFlags.HideAndDontSave;
                            tile.transform.parent = imageImportTiles.transform;
                            ImportImage(tempFile, imgName, imageResolutionEditor, anisotropicFilter);
                        }

                        if (!failedDownloading)
                        {
                            if (downloadedImageIndex == totalImages && !finishedImporting)
                                FinalizeTerrainImagery();
                        }
                        else
                        {
                            if (downloadedImageIndex == totalFailedImages && !finishedImporting)
                                FinalizeTerrainImagery();
                        }
                    }
                });
            });
        }

        private void ImageDownloader(int i, int current)
        {
            if (!allBlack)
            {
                int row = Mathf.CeilToInt((float)(current + 1) / gridNumber);
                int column = (current + 1) - ((row - 1) * gridNumber);
                string imgName = "";

                try
                {
                    TerraLandWorldImagery.MapServerInfo mapinfo = mapserviceImagery.GetServerInfo(mapserviceImagery.GetDefaultMapName());
                    TerraLandWorldImagery.MapDescription mapdesc = mapinfo.DefaultMapDescription;
                    TerraLandWorldImagery.EnvelopeN extent = new TerraLandWorldImagery.EnvelopeN();

                    if (!failedDownloading)
                    {
                        extent.XMin = xMin[i];
                        extent.YMin = yMin[i];
                        extent.XMax = xMax[i];
                        extent.YMax = yMax[i];
                    }
                    else
                    {
                        extent.XMin = xMinFailedImagery[i];
                        extent.YMin = yMinFailedImagery[i];
                        extent.XMax = xMaxFailedImagery[i];
                        extent.YMax = yMaxFailedImagery[i];
                    }

                    mapdesc.MapArea.Extent = extent;

                    TerraLandWorldImagery.ImageType imgtype = new TerraLandWorldImagery.ImageType();
                    imgtype.ImageFormat = TerraLandWorldImagery.esriImageFormat.esriImageJPG;
                    imgtype.ImageReturnType = TerraLandWorldImagery.esriImageReturnType.esriImageReturnMimeData;
                    //imgtype.ImageReturnType = esriImageReturnType.esriImageReturnURL;

                    TerraLandWorldImagery.ImageDisplay imgdisp = new TerraLandWorldImagery.ImageDisplay();

                    int resolution = imageResolutionEditor;
                    if (dynamicWorld) resolution = imageResolutionStreaming;

                    imgdisp.ImageHeight = (int)(resolution * terrainSizeFactor);
                    imgdisp.ImageWidth = resolution;

                    imgdisp.ImageDPI = 72; // Default is 96

                    TerraLandWorldImagery.ImageDescription imgdesc = new TerraLandWorldImagery.ImageDescription();
                    imgdesc.ImageDisplay = imgdisp;
                    imgdesc.ImageType = imgtype;

                    TerraLandWorldImagery.MapImage mapimg = mapserviceImagery.ExportMapImage(mapdesc, imgdesc);
                    //mapservice.ExportMapImageCompleted += new ExportMapImageCompletedEventHandler(MapImageCompleted);
                    //mapservice.ExportMapImageAsync(mapdesc, imgdesc);

                    //if(!dynamicWorld)
                    //{
                    //    // Crop the satellite images if area is rectangular so that they match to their cell positions
                    //    if(areaIsRectangleLat)
                    //    {
                    //        EnvelopeN extentESRI = new EnvelopeN();
                    //        extentESRI = (EnvelopeN)mapimg.Extent;
                    //
                    //        cropSizeX = (int)((double)resolution * (extent.XMax - extent.XMin) / (extentESRI.XMax - extentESRI.XMin));
                    //        cropOffsetX = (resolution - cropSizeX) / 2;
                    //        cropSizeY = resolution;
                    //        cropOffsetY = 0;
                    //    }
                    //    else if(areaIsRectangleLon)
                    //    {
                    //        EnvelopeN extentESRI = new EnvelopeN();
                    //        extentESRI = (EnvelopeN)mapimg.Extent;
                    //
                    //        cropSizeX = resolution;
                    //        cropOffsetX = 0;
                    //        cropSizeY = (int)((double)resolution * (extent.YMax - extent.YMin) / (extentESRI.YMax - extentESRI.YMin));
                    //        cropOffsetY = (resolution - cropSizeY) / 2;
                    //    }
                    //}


                    byte[] imageData = mapimg.ImageData;
                    //string imgURL = mapimg.ImageURL;

                    if (!dynamicWorld)
                        imgName = projectPath + "Temporary Imagery Data/" + row + "-" + column + ".jpg";
                    else
                        imgName = directoryPathImagery + "/" + row + "-" + column + ".jpg";

                    string tempFileName = imgName.Replace(".jpg", tempPattern + ".jpg");

                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);

                    File.WriteAllBytes(imgName, imageData);
                    //DownloadImageryData(imgURL, imgName);
                }
                catch (Exception e)
                {
                    imgName = directoryPathImagery + "/" + row + "-" + column + tempPattern + ".jpg";

                    //if (downloadedImageIndex == 0 && !failedDownloading)
                    //CheckImageColors(imgName);

                    if (!File.Exists(imgName))
                        File.WriteAllBytes(imgName, tempImageBytes);

                    // Following lines will remove tiles if were already available from previous download sessions
                    imgName = directoryPathImagery + "/" + row + "-" + column + ".jpg";

                    if (File.Exists(imgName))
                        File.Delete(imgName);

                    failedTilesAvailable = true;

                    UnityEngine.Debug.Log(e);

                    if (downloadedImageIndex == 0)
                        allBlack = true;

                    if (!dynamicWorld)
                        downloadedImageIndex++;
                }
                finally
                {
                    if (dynamicWorld)
                        downloadedImageIndex++;
                }

                if (cancelOperation)
                {
                    showProgressImagery = false;
                    return;
                }
            }
        }

        private void MapImageCompleted(object sender, TerraLandWorldImagery.ExportMapImageCompletedEventArgs e)
        {
            TerraLandWorldImagery.MapImage mapimg = e.Result;
            string imgURL = mapimg.ImageURL;

            UnityEngine.Debug.Log(imgURL);

            string imgName = "";
            int row = 1;
            int column = 1;

            if (!dynamicWorld)
                imgName = projectPath + "Temporary Imagery Data/" + row + "-" + column + ".jpg";
            else
                imgName = directoryPathImagery + "/" + row + "-" + column + ".jpg";

            string tempFileName = imgName.Replace(".jpg", tempPattern + ".jpg");

            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            //File.WriteAllBytes(imgName, imageData);
            DownloadImageryData(imgURL, imgName);
        }

        private void ImportImage(string tempPath, string imgName, int resolution, int anisoLevel)
        {
            File.Move(tempPath, imgName);
            AssetDatabase.Refresh();
            TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(imgName.Substring(imgName.LastIndexOf("Assets")));

            if (textureImporter != null)
            {
                textureImporter.isReadable = true;
                textureImporter.mipmapEnabled = true;
                textureImporter.wrapMode = TextureWrapMode.Clamp;
                textureImporter.anisoLevel = anisoLevel;

                TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings();
                platformSettings.overridden = true;

                //platformSettings.format = TextureImporterFormat.Automatic;
                platformSettings.format = TextureImporterFormat.RGB24;

                platformSettings.maxTextureSize = resolution;
                textureImporter.SetPlatformTextureSettings(platformSettings);

                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(imgName.Substring(imgName.LastIndexOf("Assets")), typeof(UnityEngine.Object)) as UnityEngine.Object;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset), ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            downloadedImageIndex++;
        }

        public void FinalizeTerrainImagery()
        {
            if (!dynamicWorld && !allBlack)
            {
                if (textureOnFinish == 0)
                {
                    if (!importingInProgress)
                        ImageTilerDownloader();
                }  
                else
                    Process.Start(directoryPathImagery.Replace(@"/", @"\") + @"\");
            }

            cancelOperation = true;
            showProgressImagery = false;
            imageDownloadingStarted = false;
            finishedImporting = true;
            failedDownloading = false;
            allThreads = 0;
            normalizedProgressSatelliteImage = 0f;

            AssetDatabase.Refresh();

            if (!dynamicWorld)
            {
                ManageNeighborings();
                CheckFailedImages();
            }

            if (Directory.Exists(projectPath + "Temporary Imagery Data"))
                Directory.Delete(projectPath + "Temporary Imagery Data", true);

            CheckHeightmapDownloaderAndRecompile();

            if (dynamicWorld)
            {
                serverSetUpImagery = true;

                if (serverSetUpElevation && serverSetUpImagery)
                {
                    if (failedTilesAvailable)
                    {
                        EditorUtility.DisplayDialog("FAILED TILES AVAILABLE", "There are some failed tile downloads for this session.\n\nGo to FAILED TILES DOWNLOADER section and press GET FAILED TILES button to re-download failed tiles.", "Ok");
                        showFailedDownloaderSection = true;
                    }

                    Process.Start(serverPath.Replace(@"/", @"\") + @"\");
                }
            }
            else
                ShowDownloadsFolder();
        }

        private void DownloadFailedImageTiles(bool showNotifications)
        {
            if (failedFolder)
            {
                GetPresetInfo();
                SetupImagery();
                if (cancelOperation) return;
                InitializeDownloader();
                GetSatelliteImages();
            }
            else
            {
                if (showNotifications)
                    EditorUtility.DisplayDialog("FOLDER NOT AVILABLE", "No Folders Assigned, Please First Select A Folder From The Project Panel.", "Ok");

                return;
            }
        }

        private void CheckHeightmapDownloaderAndRecompile()
        {
            if (!terrainGenerationstarted)
                allThreads = 0;

            Resources.UnloadUnusedAssets();
        }

        private void CheckImageDownloaderAndRecompile()
        {
            if (!imageDownloadingStarted)
                allThreads = 0;

            Resources.UnloadUnusedAssets();
        }

        private string[] LogicalComparer(string filePath, string fileType)
        {
            string[] names = Directory.GetFiles(filePath, "*" + fileType, SearchOption.AllDirectories);

            //NaturalStringComparer stringComparer = new NaturalStringComparer();
            //List<string> namesList = names.ToList();
            //namesList.Sort(stringComparer);

            ns.NumericComparer ns = new ns.NumericComparer();
            Array.Sort(names, ns);
            return names;
        }

        private string[] LogicalComparer(string[] names)
        {
            //NaturalStringComparer stringComparer = new NaturalStringComparer();
            //List<string> namesList = names.ToList();
            //namesList.Sort(stringComparer);

            ns.NumericComparer ns = new ns.NumericComparer();
            Array.Sort(names, ns);
            return names;
        }

        public void ImageTilerDownloader()
        {
            importingInProgress = true;
            AssetDatabase.Refresh();
            string[] allImageNames = LogicalComparer(directoryPathImagery, ".jpg");

            if (!splittedTerrains)
            {
                TerrainLayer[] terrainLayers = new TerrainLayer[totalImages];

                for (int i = 0; i < totalImages; i++)
                {
                    // TODO: Check the following line
                    Texture2D satelliteImage = AssetDatabase.LoadAssetAtPath(allImageNames[i].Substring(allImageNames[i].LastIndexOf("Assets")), typeof(Texture2D)) as Texture2D;

                    // Texturing Terrain
                    terrainLayers[i] = new TerrainLayer();
                    string layerName = directoryPathTerrainlayers.Substring(directoryPathTerrainlayers.LastIndexOf("Assets")) + "/" + satelliteImage.name.Replace(tempPattern, "") + ".terrainlayer";
                    AssetDatabase.CreateAsset(terrainLayers[i], layerName);
                    terrainLayers[i].diffuseTexture = satelliteImage;
                    terrainLayers[i].tileSize = new Vector2(cellSizeX, cellSizeY);
                    terrainLayers[i].tileOffset = new Vector2(imageXOffset[i], imageYOffset[i]);
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(terrainLayers[i]), ImportAssetOptions.ForceUpdate);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                terrain.terrainData.terrainLayers = terrainLayers;
                terrain.terrainData.RefreshPrototypes();
                terrain.Flush();

                if (terrain.terrainData.terrainLayers == null || terrain.terrainData.terrainLayers.Length == 0)
                    return;

                terrain.terrainData.alphamapResolution = alphamapResolution;
                splatNormalizeX = terrainSizeX / alphamapResolution;
                splatNormalizeY = terrainSizeY / alphamapResolution;

                float[] lengthz = new float[totalImages];
                float[] widthz = new float[totalImages];
                float[] lengthzOff = new float[totalImages];
                float[] widthzOff = new float[totalImages];

                for (int i = 0; i < totalImages; i++)
                {
                    lengthz[i] = terrain.terrainData.terrainLayers[i].tileSize.y / splatNormalizeY;
                    widthz[i] = terrain.terrainData.terrainLayers[i].tileSize.x / splatNormalizeX;
                    lengthzOff[i] = terrain.terrainData.terrainLayers[i].tileOffset.y / splatNormalizeY;
                    widthzOff[i] = terrain.terrainData.terrainLayers[i].tileOffset.x / splatNormalizeX;

                    smData = new float[Mathf.RoundToInt(lengthz[i]), Mathf.RoundToInt(widthz[i]), terrain.terrainData.alphamapLayers];

                    for (int y = 0; y < Mathf.RoundToInt(lengthz[i]); y++)
                        for (int z = 0; z < Mathf.RoundToInt(widthz[i]); z++)
                            smData[y, z, i] = 1;

                    int alphaXOffset = Mathf.RoundToInt(-widthzOff[i]);
                    int alphaYOffset = Mathf.RoundToInt(-lengthzOff[i]);

                    terrain.terrainData.SetAlphamaps(alphaXOffset, alphaYOffset, smData);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                terrain.terrainData.RefreshPrototypes();
                terrain.Flush();

                smData = null;

                UnityEngine.Object terrainDataAsset = terrain.terrainData;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(terrainDataAsset), ImportAssetOptions.ForceUpdate);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                int imagesPerTerrain = Mathf.RoundToInt(Mathf.Pow(gridPerTerrainEditor, 2));
                int index = 0;
                float terrainSizeSplittedX = croppedTerrains[0].terrainData.size.x;
                float terrainSizeSplittedY = croppedTerrains[0].terrainData.size.z;
                float cellSizeSplittedX = terrainSizeSplittedX / (float)gridPerTerrainEditor;
                float cellSizeSplittedY = terrainSizeSplittedY / (float)gridPerTerrainEditor;
                imageXOffset = new float[imagesPerTerrain];
                imageYOffset = new float[imagesPerTerrain];

                for (int i = 0; i < gridPerTerrainEditor; i++)
                {
                    for (int j = 0; j < gridPerTerrainEditor; j++)
                    {
                        imageXOffset[index] = (terrainSizeSplittedX - (cellSizeSplittedX * ((float)gridPerTerrainEditor - (float)j))) * -1f;
                        imageYOffset[index] = (terrainSizeSplittedY - cellSizeSplittedY - ((float)cellSizeSplittedY * (float)i)) * -1f;

                        if (imageXOffset[index] > 0f)
                            imageXOffset[index] = 0f;
                        if (imageYOffset[index] > 0f)
                            imageYOffset[index] = 0f;

                        index++;
                    }
                }

                List<Terrain> stitchingTerrains = OrderedTerrainChunks(splittedTerrains);

                index = 0;
                int imageIndex = 0;
                int offset = -1;
                int totalTiles = stitchingTerrains.Count;
                int gridTerrains = (int)Mathf.Sqrt(totalTiles);
                int gridImages = (int)Mathf.Sqrt(totalImages);
                int pad = gridImages - gridPerTerrainEditor;
                int reverse = gridImages * (gridPerTerrainEditor - 1);
                int[] index2D = new int[totalImages];
                
                foreach (Terrain terrainSplitted in stitchingTerrains)
                {
                    for (int i = 0; i < imagesPerTerrain; i++)
                    {
                        offset++;
                
                        if (gridPerTerrainEditor > 1 && i > 0 && i % gridPerTerrainEditor == 0)
                            offset += pad;
                
                        index2D[imageIndex] = offset;
                        imageIndex++;
                    }
                
                    if (gridPerTerrainEditor > 1 && (index + 1) % gridTerrains != 0)
                        offset -= reverse;
                
                    index++;
                }

                index = 0;
                imageIndex = 0;

                foreach (Terrain terrainSplitted in stitchingTerrains)
                {
                    //// Only update terrains which have failed textures in them
                    //if (failedDownloading && !failedTerrainNames.Contains(terrainSplitted.name, StringComparer.OrdinalIgnoreCase))
                    //{
                    //    imageIndex += (int)Mathf.Pow(gridPerTerrainEditor, 2f);
                    //    continue;
                    //}

                    TerrainLayer[] terrainLayers = new TerrainLayer[imagesPerTerrain];

                    for (int i = 0; i < imagesPerTerrain; i++)
                    {
                        string name = allImageNames[index2D[imageIndex]].Substring(allImageNames[index2D[imageIndex]].LastIndexOf("Assets"));
                        Texture2D satelliteImage = AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D)) as Texture2D;

                        // Texturing Terrain
                        terrainLayers[i] = new TerrainLayer();
                        string layerName = directoryPathTerrainlayers.Substring(directoryPathTerrainlayers.LastIndexOf("Assets")) + "/" + satelliteImage.name.Replace(tempPattern, "") + ".terrainlayer";
                        AssetDatabase.CreateAsset(terrainLayers[i], layerName);
                        terrainLayers[i].diffuseTexture = satelliteImage;
                        terrainLayers[i].tileSize = new Vector2(cellSizeSplittedX, cellSizeSplittedY);
                        terrainLayers[i].tileOffset = new Vector2(imageXOffset[i], imageYOffset[i]);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(terrainLayers[i]), ImportAssetOptions.ForceUpdate);
                        imageIndex++;
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    terrainSplitted.terrainData.terrainLayers = terrainLayers;
                    terrainSplitted.terrainData.RefreshPrototypes();
                    terrainSplitted.Flush();

                    if (terrainSplitted.terrainData.terrainLayers == null || terrainSplitted.terrainData.terrainLayers.Length == 0)
                        return;

                    splatNormalizeX = terrainSplitted.terrainData.size.x / terrainSplitted.terrainData.alphamapResolution;
                    splatNormalizeY = terrainSplitted.terrainData.size.z / terrainSplitted.terrainData.alphamapResolution;

                    float[] lengthz = new float[imagesPerTerrain];
                    float[] widthz = new float[imagesPerTerrain];
                    float[] lengthzOff = new float[imagesPerTerrain];
                    float[] widthzOff = new float[imagesPerTerrain];

                    for (int i = 0; i < imagesPerTerrain; i++)
                    {
                        lengthz[i] = terrainSplitted.terrainData.terrainLayers[i].tileSize.y / splatNormalizeY;
                        widthz[i] = terrainSplitted.terrainData.terrainLayers[i].tileSize.x / splatNormalizeX;
                        lengthzOff[i] = terrainSplitted.terrainData.terrainLayers[i].tileOffset.y / splatNormalizeY;
                        widthzOff[i] = terrainSplitted.terrainData.terrainLayers[i].tileOffset.x / splatNormalizeX;

                        smData = new float[Mathf.RoundToInt(lengthz[i]), Mathf.RoundToInt(widthz[i]), terrainSplitted.terrainData.alphamapLayers];

                        for (int y = 0; y < Mathf.RoundToInt(lengthz[i]); y++)
                            for (int z = 0; z < Mathf.RoundToInt(widthz[i]); z++)
                                smData[y, z, i] = 1;

                        int alphaXOffset = Mathf.RoundToInt(-widthzOff[i]);
                        int alphaYOffset = Mathf.RoundToInt(-lengthzOff[i]);

                        terrainSplitted.terrainData.SetAlphamaps(alphaXOffset, alphaYOffset, smData);

                        EditorUtility.DisplayProgressBar("TEXTURING TERRAIN " + (index + 1).ToString(), "Image   " + (i + 1).ToString() + "  of  " + imagesPerTerrain.ToString(), Mathf.InverseLerp(0.0f, (float)(imagesPerTerrain - 1), (float)(i + 1)));
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    terrainSplitted.terrainData.RefreshPrototypes();
                    terrainSplitted.Flush();

                    smData = null;

                    UnityEngine.Object terrainDataAsset = terrainSplitted.terrainData;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(terrainDataAsset), ImportAssetOptions.ForceUpdate);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    index++;
                }

                EditorUtility.ClearProgressBar();
            }

            importingInProgress = false;
        }

        private void CheckFailedImages()
        {
            string[] allImageNames = LogicalComparer(directoryPathImagery, ".jpg");
            totalFailedImages = 0;

            foreach (string imageName in allImageNames)
            {
                if (imageName.Contains(tempPattern))
                {
                    failedImageAvailable = true;
                    totalFailedImages++;
                }
            }

            if (failedImageAvailable && textureOnFinish == 0)
                showFailedDownloaderSection = true;

            if (totalFailedImages == 0)
                failedImageAvailable = false;
        }

        private void CheckFailedImagesGUI()
        {
            string[] names = LogicalComparer(directoryPathImagery, ".jpg");
            totalFailedImages = 0;

            foreach (string imageName in names)
            {
                if (imageName.Contains(tempPattern))
                {
                    failedImageAvailable = true;
                    totalFailedImages++;
                }
            }

            if (totalFailedImages == 0)
                failedImageAvailable = false;
        }

        private void CheckFailedHeightmapsGUIServer()
        {
            string[] names = LogicalComparer(directoryPathElevation, ".tif");

            if (names != null && names.Length > 0)
            {
                //String[] pathParts = names[0].Split(char.Parse("."));

                totalFailedHeightmaps = 0;

                foreach (string name in names)
                {
                    if (name.Contains(tempPattern))
                    {
                        failedHeightmapAvailable = true;
                        totalFailedHeightmaps++;
                    }
                }

                if (totalFailedHeightmaps == 0)
                    failedHeightmapAvailable = false;

                //if(names[0].EndsWith(".asc") || names[0].EndsWith(".raw") || names[0].EndsWith(".tif"))
                //{
                //    String[] pathParts = names[0].Split(char.Parse("."));
                //    elevationFormat = pathParts[pathParts.Length - 1];
                //
                //    if(elevationFormat.EndsWith("raw"))
                //        formatIndex = 0;
                //    else if(elevationFormat.EndsWith("asc"))
                //        formatIndex = 1;
                //    else if(elevationFormat.EndsWith("tif"))
                //        formatIndex = 2;
                //
                //    totalFailedHeightmaps = 0;
                //
                //    foreach(string name in names)
                //    {
                //        if(name.Contains(tempPattern))
                //        {
                //            failedHeightmapAvailable = true;
                //            totalFailedHeightmaps++;
                //        }
                //    }
                //
                //    if(totalFailedHeightmaps == 0)
                //        failedHeightmapAvailable = false;
                //}
                //else
                //{
                //    UnityEngine.Debug.LogError("UNKNOWN FORMAT - There are no valid ASCII, RAW or Tiff files in selected server's Elevation directory.");
                //    return;
                //}
            }
            else
            {
                totalFailedHeightmaps = 0;
                failedHeightmapAvailable = false;
            }
        }

        private void CheckFailedImagesGUIServer()
        {
            string[] names = LogicalComparer(directoryPathImagery, ".jpg");
            totalFailedImages = 0;

            foreach (string imageName in names)
            {
                if (imageName.Contains(tempPattern))
                {
                    failedImageAvailable = true;
                    totalFailedImages++;
                }
            }

            if (totalFailedImages == 0)
                failedImageAvailable = false;
        }

        public List<Terrain> OrderedTerrainChunks(GameObject terrainsParentGo)
        {
            string names = "";

            foreach (Transform child in terrainsParentGo.transform)
                names += child.name + Environment.NewLine;

            string[] lines = names.Replace("\r", "").Split('\n');
            lines = LogicalComparer(lines);

            List<Terrain> stitchingTerrains = new List<Terrain>();

            foreach (string s in lines)
                if (s != "")
                    stitchingTerrains.Add(terrainsParentGo.transform.Find(s).GetComponent<Terrain>());

            names = null;

            return stitchingTerrains;
        }

        public Texture2D ImageCropper(Texture2D source, int offsetX, int targetWidth, int targetHeight, int offsetY)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight);
            result.SetPixels(source.GetPixels(offsetX, offsetY, targetWidth, targetHeight));
            result.Apply();

            return result;
        }

        private void convertIntVarsToEnums()
        {
            switch (neighbourhoodInt)
            {
                case 0:
                    neighbourhood = Neighbourhood.Moore;
                    break;
                case 1:
                    neighbourhood = Neighbourhood.VonNeumann;
                    break;
            }
        }

        //private void CheckImageColors(string fileName)
        //{
        //    Bitmap bmp = new Bitmap(fileName);
        //
        //    // Lock the bitmap's bits.  
        //    Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        //    BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
        //
        //    // Get the address of the first line.
        //    IntPtr ptr = bmpData.Scan0;
        //
        //    // Declare an array to hold the bytes of the bitmap.
        //    int bytes = bmpData.Stride * bmp.Height;
        //    byte[] rgbValues = new byte[bytes];
        //
        //    // Copy the RGB values into the array.
        //    System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
        //
        //    allBlack = true;
        //
        //    // Scanning for non-zero bytes
        //    for (int i = 0; i < rgbValues.Length; i++)
        //    {
        //        if (rgbValues[i] != 0)
        //        {
        //            allBlack = false;
        //            break;
        //        }
        //    }
        //
        //    // Unlock the bits.
        //    bmp.UnlockBits(bmpData);
        //    bmp.Dispose();
        //}

        //private void CheckImageColors()
        //{
        //    string[] allImageNames = Directory.GetFiles(projectPath + "Temporary Imagery Data", "*.jpg", SearchOption.AllDirectories);
        //
        //    if (allImageNames != null)
        //    {
        //        Bitmap bmp = new Bitmap(allImageNames[0]);
        //
        //        // Lock the bitmap's bits.  
        //        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        //        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
        //
        //        // Get the address of the first line.
        //        IntPtr ptr = bmpData.Scan0;
        //
        //        // Declare an array to hold the bytes of the bitmap.
        //        int bytes = bmpData.Stride * bmp.Height;
        //        byte[] rgbValues = new byte[bytes];
        //
        //        // Copy the RGB values into the array.
        //        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
        //
        //        allBlack = true;
        //
        //        // Scanning for non-zero bytes
        //        for (int index = 0; index < rgbValues.Length; index++)
        //        {
        //            if (rgbValues[index] != 0)
        //            {
        //                allBlack = false;
        //                break;
        //            }
        //        }
        //
        //        // Unlock the bits.
        //        bmp.UnlockBits(bmpData);
        //        bmp.Dispose();
        //    }
        //}

        private void ShowMapAndRefresh(bool checkIsOpened)
        {
            if (checkIsOpened)
            {
                if (mapWindow != null)
                {
                    InteractiveMap.requestIndex = 0;
                    InteractiveMap.map_latlong_center = new InteractiveMap.latlong_class(latitudeUser, longitudeUser);
                    mapWindow = (InteractiveMap)GetWindow(typeof(InteractiveMap), false, "Interactive Map", true);
                    mapWindow.RequestMap();
                }
            }
            else
            {
                InteractiveMap.requestIndex = 0;
                InteractiveMap.map_latlong_center = new InteractiveMap.latlong_class(latitudeUser, longitudeUser);
                mapWindow = (InteractiveMap)GetWindow(typeof(InteractiveMap), false, "Interactive Map", true);
                mapWindow.RequestMap();
            }
        }


        #region multithreading functions

        protected void QueueOnMainThread(Action action)
        {
            QueueOnMainThread(action, 0f);
        }

        protected void QueueOnMainThread(Action action, float time)
        {
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

        protected Thread RunAsync(Action a)
        {
            while (numThreads >= maxThreads)
            {
                Thread.Sleep(1);
            }
            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        private void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }
        }

        protected virtual void Start()
        {
            m_HasLoaded = true;
        }

        protected virtual void Update()
        {
            if (m_HasLoaded == false)
                Start();

            if (_actions != null && _actions.Count > 0)
            {
                lock (_actions)
                {
                    _currentActions.Clear();
                    _currentActions.AddRange(_actions);
                    _actions.Clear();
                }

                foreach (var a in _currentActions)
                    a();
            }

            if (_delayed != null && _delayed.Count > 0)
            {
                lock (_delayed)
                {
                    _currentDelayed.Clear();
                    _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));

                    foreach (var item in _currentDelayed)
                        _delayed.Remove(item);
                }

                foreach (var delayed in _currentDelayed)
                    delayed.action();
            }

            #endregion

            if (downloadIndexSatellite != downloadedImageIndex)
                Repaint();

            //if (areaSelectionMode != 1)
            //{
            //    AreaBounds.MetricsToBBox(latitudeUser, longitudeUser, areaSizeLat, areaSizeLon, out top, out left, out bottom, out right);
            //
            //    //double destLat = 0;
            //    //double destLon = 0;
            //    //
            //    ////double initialBearingRadiansTop = AreaBounds.DegreesToRadians(0d);
            //    ////double initialBearingRadiansLeft = AreaBounds.DegreesToRadians(270d);
            //    ////double initialBearingRadiansBottom = AreaBounds.DegreesToRadians(180d);
            //    ////double initialBearingRadiansRight = AreaBounds.DegreesToRadians(90d);
            //    //
            //    //double initialBearingRadiansTop = AreaBounds.DegreesToRadians(0d);
            //    //double initialBearingRadiansLeft = AreaBounds.DegreesToRadians(270d);
            //    //double initialBearingRadiansBottom = AreaBounds.DegreesToRadians(180d);
            //    //double initialBearingRadiansRight = AreaBounds.DegreesToRadians(90d);
            //    //
            //    //double distanceKilometresLat = areaSizeLat / 2d;
            //    //double distanceKilometresLon = areaSizeLon / 2d;
            //    //
            //    //AreaBounds.FindPointAtDistanceFrom(latitudeUser, longitudeUser, initialBearingRadiansTop, distanceKilometresLat, out destLat, out destLon);
            //    //top = destLat.ToString();
            //    //
            //    //AreaBounds.FindPointAtDistanceFrom(latitudeUser, longitudeUser, initialBearingRadiansLeft, distanceKilometresLon, out destLat, out destLon);
            //    //left = destLon.ToString();
            //    //
            //    //AreaBounds.FindPointAtDistanceFrom(latitudeUser, longitudeUser, initialBearingRadiansBottom, distanceKilometresLat, out destLat, out destLon);
            //    //bottom = destLat.ToString();
            //    //
            //    //AreaBounds.FindPointAtDistanceFrom(latitudeUser, longitudeUser, initialBearingRadiansRight, distanceKilometresLon, out destLat, out destLon);
            //    //right = destLon.ToString();
            //
            //    //UnityEngine.Debug.Log(top + "   " + destLat);
            //    //UnityEngine.Debug.Log(right + "   " + destLon);
            //
            //    //UnityEngine.Debug.Log(left + "   " + destLon);
            //    //UnityEngine.Debug.Log(bottom + "   " + destLat);
            //}
        }

        private void PresetManager()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE PRESET"))
            {
                if (!Directory.Exists(presetsPath))
                {
                    Directory.CreateDirectory(presetsPath);
                    AssetDatabase.Refresh();
                }

                presetFilePath = EditorUtility.SaveFilePanel("Save Settings As Preset File", presetsPath, address, "tlps");

                if (!string.IsNullOrEmpty(presetFilePath))
                    WritePresetFile(presetFilePath);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("LOAD PRESET"))
            {
                presetFilePath = EditorUtility.OpenFilePanel("Load Preset File", presetsPath, "tlps");

                if (presetFilePath.Contains("tlps"))
                    ReadPresetFile();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void AutoSave()
        {
            if (!Directory.Exists(downloadsPath))
                Directory.CreateDirectory(downloadsPath);

            presetFilePath = presetsPath + "/Downloader AutoSave.tlps";
            WritePresetFile(presetFilePath);
        }

        private void AutoLoad()
        {
            presetFilePath = presetsPath + "/Downloader AutoSave.tlps";

            if (File.Exists(presetFilePath) && presetFilePath.Contains("tlps"))
                ReadPresetFile();
        }

        private void WritePresetFile(string fileName)
        {
            string preset = "Terrain Generation Settings\n"

                + "\nAddress: " + address
                + "\nLatitude: " + latitudeUser + " Degree"
                + "\nLongitude: " + longitudeUser + " Degree"

                + "\nNewTerrainGrid: " + enumValueNew
                + "\nNewTerrainSizeX: " + terrainSizeNewX
                + "\nNewTerrainSizeZ: " + terrainSizeNewZ
                + "\nNewTerrainPixelError: " + pixelError

                + "\nLatExtents: " + areaSizeLat
                + "\nLonExtents: " + areaSizeLon
                + "\nSquareArea: " + squareArea
                + "\nArbitraryTop: " + top
                + "\nArbitraryLeft: " + left
                + "\nArbitraryBottom: " + bottom
                + "\nArbitraryRight: " + right

                + "\nMapType: " + mapTypeIndex

                + "\nSaveASCII: " + saveTerrainDataASCII
                + "\nSaveRAW: " + saveTerrainDataRAW
                + "\nSaveTIFF: " + saveTerrainDataTIFF

                + "\nTerrainResolutionEditor: " + heightmapResolutionEditor
                + "\nTerrainSmooth: " + smoothIterations
                + "\nElevationExaggeration: " + elevationExaggeration

                + "\nGridTerrain: " + tileGrid

                + "\nImageGridPerTerrain: " + gridPerTerrainEditor
                + "\nImageResolutionEditor: " + imageResolutionEditor
                + "\nTextureTerrain: " + textureOnFinish
                + "\nQuality: " + compressionQuality
                + "\nImportCompression: " + compressionActive
                + "\nAutoScale: " + autoScale
                + "\nAnisotropic: " + anisotropicFilter
                + "\nAlphaResolution: " + alphamapResolution
                + "\nAsyncCalls: " + maxAsyncCalls

                + "\nEngineResolutionMode: " + engineModeIndex

                + "\nResolutionPresetSection: " + showResolutionPresetSection
                + "\nNewTerrainSection: " + showNewTerrainSection
                + "\nLocationSection: " + showLocationSection
                + "\nAreaSizeSection: " + showAreaSizeSection
                + "\nHeghtmapDownloaderSection: " + showHeghtmapDownloaderSection
                + "\nImageDownloaderSection: " + showImageDownloaderSection
                + "\nFailedDownloaderSection: " + showFailedDownloaderSection

                + "\nSplitSizeNew: " + splitSizeNew
                + "\nTotalTerrainsNew: " + totalTerrainsNew

                + "\nSmoothBlendIndex: " + smoothBlendIndex
                + "\nSmoothBlend: " + smoothBlend

                + "\nServerSection: " + showServerSection
                + "\nServerGrid: " + serverGrid
                + "\nWorldMode: " + worldModeIndex
                + "\nformatMode: " + formatIndex

                + "\nGridPerWorld: " + gridStreamingWorld
                + "\nTerrainResolutionStreaming: " + heightmapResolutionStreaming
                + "\nImageResolutionStreaming: " + imageResolutionStreaming;


            File.WriteAllText(fileName, preset);
        }

        private void ReadPresetFile()
        {
            try
            {
                string text = File.ReadAllText(presetFilePath);
                string[] dataLines = text.Split('\n');
                string[][] dataPairs = new string[dataLines.Length][];
                int lineNum = 0;

                foreach (string line in dataLines)
                    dataPairs[lineNum++] = line.Split(' ');

                address = "";

                for (int i = 1; i < dataPairs[2].Length; i++)
                {
                    address += dataPairs[2][i];

                    if (i < dataPairs[2].Length - 1)
                        address += " ";
                }

                latitudeUser = double.Parse(dataPairs[3][1]);
                longitudeUser = double.Parse(dataPairs[4][1]);
                enumValueNew = (SizeNew)Enum.Parse(typeof(SizeNew), dataPairs[5][1]);
                terrainSizeNewX = float.Parse(dataPairs[6][1]);
                terrainSizeNewZ = float.Parse(dataPairs[7][1]);
                pixelError = float.Parse(dataPairs[8][1]);
                areaSizeLat = float.Parse(dataPairs[9][1]);
                areaSizeLon = float.Parse(dataPairs[10][1]);

                if (dataPairs[11][1].Contains("True"))
                    squareArea = true;
                else
                    squareArea = false;

                top = double.Parse(dataPairs[12][1]);
                left = double.Parse(dataPairs[13][1]);
                bottom = double.Parse(dataPairs[14][1]);
                right = double.Parse(dataPairs[15][1]);
                mapTypeIndex = int.Parse(dataPairs[16][1]);

                if (dataPairs[17][1].Contains("True"))
                    saveTerrainDataASCII = true;
                else
                    saveTerrainDataASCII = false;

                if (dataPairs[18][1].Contains("True"))
                    saveTerrainDataRAW = true;
                else
                    saveTerrainDataRAW = false;

                if (dataPairs[19][1].Contains("True"))
                    saveTerrainDataTIFF = true;
                else
                    saveTerrainDataTIFF = false;

                heightmapResolutionEditor = int.Parse(dataPairs[20][1]);
                smoothIterations = int.Parse(dataPairs[21][1]);
                elevationExaggeration = float.Parse(dataPairs[22][1]);
                tileGrid = int.Parse(dataPairs[23][1]);
                gridPerTerrainEditor = int.Parse(dataPairs[24][1]);
                imageResolutionEditor = int.Parse(dataPairs[25][1]);
                textureOnFinish = int.Parse(dataPairs[26][1]);
                compressionQuality = int.Parse(dataPairs[27][1]);

                if (dataPairs[28][1].Contains("True"))
                    compressionActive = true;
                else
                    compressionActive = false;

                if (dataPairs[29][1].Contains("True"))
                    autoScale = true;
                else
                    autoScale = false;

                anisotropicFilter = int.Parse(dataPairs[30][1]);
                alphamapResolution = int.Parse(dataPairs[31][1]);
                maxAsyncCalls = int.Parse(dataPairs[32][1]);
                engineModeIndex = int.Parse(dataPairs[33][1]);

                if (dataPairs[34][1].Contains("True"))
                    showResolutionPresetSection = true;
                else
                    showResolutionPresetSection = false;

                if (dataPairs[35][1].Contains("True"))
                    showNewTerrainSection = true;
                else
                    showNewTerrainSection = false;

                if (dataPairs[36][1].Contains("True"))
                    showLocationSection = true;
                else
                    showLocationSection = false;

                if (dataPairs[37][1].Contains("True"))
                    showAreaSizeSection = true;
                else
                    showAreaSizeSection = false;

                if (dataPairs[38][1].Contains("True"))
                    showHeghtmapDownloaderSection = true;
                else
                    showHeghtmapDownloaderSection = false;

                if (dataPairs[39][1].Contains("True"))
                    showImageDownloaderSection = true;
                else
                    showImageDownloaderSection = false;

                if (dataPairs[40][1].Contains("True"))
                    showFailedDownloaderSection = true;
                else
                    showFailedDownloaderSection = false;

                splitSizeNew = int.Parse(dataPairs[41][1]);
                totalTerrainsNew = int.Parse(dataPairs[42][1]);

                smoothBlendIndex = int.Parse(dataPairs[43][1]);
                smoothBlend = float.Parse(dataPairs[44][1]);

                if (dataPairs[45][1].Contains("True"))
                    showServerSection = true;
                else
                    showServerSection = false;

                serverGrid = (ServerGrid)Enum.Parse(typeof(ServerGrid), dataPairs[46][1]);
                worldModeIndex = int.Parse(dataPairs[47][1]);
                formatIndex = int.Parse(dataPairs[48][1]);
                gridStreamingWorld = int.Parse(dataPairs[49][1]);
                heightmapResolutionStreaming = int.Parse(dataPairs[50][1]);
                imageResolutionStreaming = int.Parse(dataPairs[51][1]);
            }
            catch
            {
                UnityEngine.Debug.Log("Preset file is not valid or it's outdated!");
            }

            if (!failedDownloading)
                ShowMapAndRefresh(true);
        }

        public void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion
    }
}

