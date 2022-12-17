#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using MEC;

namespace TerrainEngine
{
    public class TerrainPresenter : MonoBehaviour
    {
        private static TerrainEngine.TerrainController controller;

        //  -----------------------------------------------//
        //  UI GameObjects Configured in Inspector:
        [Header("Consumer-facing settings:")]
        [Space(5)]
        public bool ShowLatitudeLongitude;

        [Header("Hard-coded assignments:")]
        [Space(5)]
        public GameObject terrainGenerator;
        public HotkeyMenu hotkeyMenu;
        private TerrainPlayer terrainPlayer;

        public GameObject fadeIn;
        public float fadeTime;
        public GameObject loadingScreen;

        //  Loading UI Parent
        public GameObject loadingUI;
        public TextMeshProUGUI loadingUITitle;

        public TextMeshProUGUI heightMapsPercent;
        public TextMeshProUGUI baseImagesPercent;
        public Slider heightmapProgressBar;
        public Slider imageProgressBar;

        public GameObject taskCanvas;
        public TextMeshProUGUI heightMapsRetrieved;
        public TextMeshProUGUI baseImagesRetrieved;
        public TextMeshProUGUI buildingsRetrieved;
        public TextMeshProUGUI detailTerrrainCreated;
        public TextMeshProUGUI distantTerrrainCreated;
        public TextMeshProUGUI buildingsCreated;

        private Color taskColorComplete;
        private Color taskColorIncomplete;
        private Color progressColorComplete;
        private Color progressColorIncomplete;

        public GameObject errorCanvas;
        public TextMeshProUGUI errorDescription;

        public Button retryButton;
        public Button cancelButton;

        //  Pause Menu
        public GameObject pauseMenu;
        public Button teleportButton;
        public InputField latLonTextInput;
        private LatLonInput latLonInput;

        //  Building Detail Panel
        public BuildingDetailPanel buildingDetailPanel;

        private LayerMask terrainLayer = 256;

        //  Properties Configured in Inspector
        public float startHeight = 300f;    // From beneath terrain surface
        public float heightLimit = 1000f;   // From highest elevation in active world

        public bool enableDetailTextures = true;
        public Texture2D detailTexture;     //  'Neutral'
        public Texture2D detailNormal;      //  'MudRockyNormals'
        public Texture2D detailNormalFar;   //  'MudRockyNormalsFar'
        [Range(0, 100)] public float detailBelending = 25f;
        public float detailTileSize = 25f;

        //  Private objects and state
        private enum PresentationState
        {
            Processing,
            ProcessingCanceled,
            ProcessingWithErrors,
            Running,
            RunningPaused
        };
        private PresentationState presentationState = PresentationState.Processing;
        private PresentationState presentationStateOnDisable = PresentationState.Processing;
        private double canceledLatitude;
        private double canceledLongitude;

        private Terrain terrain;
        private RaycastHit hit;
        private readonly Vector3 rayPosition = new Vector3(1, 99000, 1);

        private Ray ray;
        private float terrainHeight;

        private double initialLat;
        private double initialLon;
        private double playerLat;
        private double playerLon;
        private static double EARTH_RADIUS = 6378137;
        private Vector3 realWorldPosition;

        public static bool enableDetailTexturesMenu;

        private bool isPaused = false;
        private AudioSource[] allAudioSources;
        private float startTime;
        private float processCompleteTime;
        private bool fadeEnabledInit;
        private Image fade;
        private Color fadeColorInit;
        private Color loadingColorInit;
        private Image loading;

        private int processedHeightmaps;
        private int processedImages;
        private int totalchunks;

        private AudioClip[] audioTracks;
        private AudioSource musicPlayer;
        private int trackIndex;

        private GameObject player;

        private readonly string[] loadingUITitleText =
        {
            "Generating terrain...",
            "Done!"
        };

        public static void TeleportToLatLong(
            float latitude, 
            float longitude, 
            float bearing = 0f)
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Player;

            TerrainPresenter terrainPresenter = SceneObject.Find(SceneObject.Mode.Player, ObjectName.TERRAIN_PRESENTER).GetComponent<TerrainPresenter>();
            terrainPresenter.latLonInput.Latitude = latitude;
            terrainPresenter.latLonInput.Longitude = longitude;
            controller.UpdateState(TerrainController.TerrainState.UserAborted);
            terrainPresenter.StartCoroutine(terrainPresenter.WaitForTerrainAborted(() =>
            {
                terrainPresenter.ReloadAndTeleport();
            }));
        }

        void Awake()
        {
            terrainPlayer = TerrainPlayer.Get(gameObject);
            player = terrainPlayer.gameObject;

            // Player raycast events
            PlayerController.OnPlayerLookingAtEnter += OnPlayerLookingAtEnter;
            PlayerController.OnPlayerLookingAtContinue += OnPlayerLookingAtContinue;
            PlayerController.OnPlayerLookingAtLeave += OnPlayerLookingAtLeave;

            latLonInput = new LatLonInput();

            //  Player controler events
            PlayerController.OnPlayerInteractionModeChanged += OnPlayerInteractionModeChanged;

            // Terrain controller events
            controller = TerrainController.Get();
            TerrainController.OnTerrainStateChanged += OnTerrainStateChanged;
            TerrainController.OnTerrainFatalErrorReport += OnTerrainFatalError;

            //  Elevator events
            ElevatorController.OnPlayerExitElevator += OnPlayerExitElevator;

            taskColorComplete = heightMapsRetrieved.color;
            taskColorIncomplete = baseImagesRetrieved.color;
            progressColorComplete = heightmapProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color;
            progressColorIncomplete = imageProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color;

            SetupLoadingUI();
            SetupHotkeyMenu();
            SetupMusic();

            QualitySettings.shadowDistance = (controller.areaSize * 1000f) / 4f;

#if UNITY_EDITOR
            SetupShadowSettings();
#endif
        }

        void SetupLoadingUI()
        {
            pauseMenu.SetActive(false);
            SetPresentationState(PresentationState.Processing);

            ResetTaskCompletion();

            heightmapProgressBar.minValue = imageProgressBar.minValue = 0;
            heightmapProgressBar.maxValue = imageProgressBar.maxValue = 100;

            fade = fadeIn.GetComponent<Image>();
            fade.enabled = fadeEnabledInit = (fade.enabled && (fadeTime > 0));
            fadeColorInit = new Color(fade.color.r, fade.color.g, fade.color.b, 1f);
            fade.color = fadeColorInit;

            loading = loadingScreen.GetComponent<Image>();
            loadingColorInit = new UnityEngine.Color(loading.color.r, loading.color.g, loading.color.b, 1f);
            loading.color = loadingColorInit;

            CheckDetailTextures();
        }

        private void SetupHotkeyMenu()
        {
            List<HotkeyMenu.Key> keys = new List<HotkeyMenu.Key>()
            {
                HotkeyMenu.Key.Build,
                HotkeyMenu.Key.ToggleFly,
                HotkeyMenu.Key.FlyUp,
                HotkeyMenu.Key.FlyDown,
                HotkeyMenu.Key.ToggleDetails,
                HotkeyMenu.Key.ToggleLocation,
                HotkeyMenu.Key.MainMenu,
                HotkeyMenu.Key.HotkeyMenu
            };
            hotkeyMenu.Populate(keys);
        }

        void ResetLoadingUI()
        {
            startTime = 0.0F;
            processCompleteTime = 0.0F;
            fade.enabled = (fadeEnabledInit && (fadeTime > 0));
            fade.color = fadeColorInit;
            loading.color = loadingColorInit;

            heightMapsPercent.text = 0 + "0%";
            heightmapProgressBar.value = 0;
            baseImagesPercent.text = 0 + "0%";
            imageProgressBar.value = 0;

            ResetTaskCompletion();

            errorDescription.text = "";
        }

        void ResetTaskCompletion()
        {
            heightMapsRetrieved.color =
            baseImagesRetrieved.color =
            heightMapsPercent.color =
            baseImagesPercent.color =
            buildingsRetrieved.color =
            detailTerrrainCreated.color =
            distantTerrrainCreated.color =
            buildingsCreated.color = taskColorIncomplete;

            heightmapProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color =
            imageProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = progressColorIncomplete;

            retryButton.gameObject.SetActive(false);
        }

        void SetPresentationState(PresentationState state)
        {
            this.presentationState = state;
            switch (state)
            {
                case PresentationState.Processing:
                    taskCanvas.SetActive(true);
                    errorCanvas.SetActive(false);
                    loadingUI.SetActive(true);
                    loadingScreen.SetActive(true);
                    terrainPlayer.EnableUIInput(true);
                    break;

                case PresentationState.ProcessingCanceled:
                    taskCanvas.SetActive(false);
                    errorCanvas.SetActive(true);
                    loadingUI.SetActive(true);
                    loadingScreen.SetActive(true);
                    terrainPlayer.EnableUIInput(true);
                    break;

                case PresentationState.ProcessingWithErrors:
                    taskCanvas.SetActive(true);
                    errorCanvas.SetActive(true);
                    loadingUI.SetActive(true);
                    loadingScreen.SetActive(true);
                    terrainPlayer.EnableUIInput(true);
                    break;

                case PresentationState.Running:
                    loadingScreen.SetActive(false);
                    loadingUI.SetActive(false);
                    taskCanvas.SetActive(false);
                    errorCanvas.SetActive(false);
                    ShowPauseOptions(false);
                    terrainPlayer.EnableUIInput(false);
                    break;

                case PresentationState.RunningPaused:
                    ShowPauseOptions(true);
                    break;

            }
        }

        void Update()
        {
            LatLonFromPlayerPosition();

            if (!controller.IsInState(TerrainController.TerrainState.Running))
            {
                terrainPlayer.EnableUIInput(true);

                if (controller.IsInState(TerrainController.TerrainState.WorldIsGenerated))
                {
                    SetRunning();
                }
                else if (controller.IsInState(TerrainController.TerrainState.Unloaded))
                {
                    CheckForPause();
                }
                else if (controller.IsInState(TerrainController.TerrainState.Loading))
                {
                    UpdateLoadingUI();
                }
            }
            else
            {
                TerrainHeightAtPlayerPosition(out terrainHeight);

                if (Input.GetKeyDown(KeyCode.R))
                {
                    player.transform.eulerAngles = new Vector3(0, player.transform.eulerAngles.y, 0);
                    player.transform.position += player.transform.up * startHeight;
                }

                if ((Time.timeSinceLevelLoad > (startTime + 2)) && fade.enabled)
                {
                    float fadeAmount = 1f - Mathf.InverseLerp(0f, fadeTime, Time.timeSinceLevelLoad - (startTime + 2));
                    fade.color = new UnityEngine.Color(fade.color.r, fade.color.g, fade.color.b, fadeAmount);

                    if (musicPlayer != null)
                    {
                        musicPlayer.volume = fadeAmount;
                    }
                }

                if (fade.color.a == 0f)
                {
                    fade.enabled = false;
                    if (musicPlayer != null)
                    {
                        musicPlayer.enabled = false;
                    }
                }

                CheckForPause();
            }
        }

        private bool TerrainHeightAtPlayerPosition(out float height)
        {
            height = 0.0f;
            Ray ray = new Ray(new Vector3(player.transform.position.x, 1000f, player.transform.position.z), Vector3.down);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayer))
            {
                int layer = hit.transform.gameObject.layer;
                terrain = hit.transform.gameObject.GetComponent<Terrain>();
            }
            if (terrain != null)
            {
                height = terrain.SampleHeight(player.transform.position);
                return true;
            }
            return false;
        }

        private void OnPlayerLookingAtEnter(ref GameObject gameObjectHit, ref RaycastHit hit)
        {
            buildingDetailPanel.TryPopulate(gameObjectHit);

            ProceduralBuilding pb = gameObjectHit.GetComponent<ProceduralBuilding>();
            hotkeyMenu.ShowKey(HotkeyMenu.Key.Build, pb != null);
            hotkeyMenu.ShowKey(HotkeyMenu.Key.ToggleDetails, pb != null);
        }

        private void OnPlayerLookingAtContinue(ref GameObject gameObjectHit, ref RaycastHit hit)
        {
            if (!pauseMenu.activeSelf)
            {
                buildingDetailPanel.TryPopulate(gameObjectHit);
            }
        }

        private void OnPlayerLookingAtLeave(ref GameObject gameObjectHit, ref RaycastHit hit)
        {
            buildingDetailPanel.Clear();

            hotkeyMenu.ShowKey(HotkeyMenu.Key.Build, false);
            hotkeyMenu.ShowKey(HotkeyMenu.Key.ToggleDetails, false);
        }

        private void OnPlayerExitElevator(GameObject elevatorDoor, int floor)
        {
            //  TODO: Place user outside the elevator on the right floor.
            //  For now, switch to Welcome screen
            SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
        }

        private void CheckForPause()
        {
            // Display Game Menu by pressing Escape button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (presentationState)
                {
                    case PresentationState.Processing:
                    case PresentationState.ProcessingWithErrors:
                        OnAbortTerrain();
                        break;

                    case PresentationState.Running:
                        SetPresentationState(PresentationState.RunningPaused);
                        break;

                    case PresentationState.RunningPaused:
                        SetPresentationState(PresentationState.Running);
                        break;
                }
            }
        }

        private void UpdateLoadingUI()
        {
            totalchunks = (int)Mathf.Pow((int)controller.terrainGridSize, 2);
            processedHeightmaps = TerrainRuntime.s_processedHeightmapIndex;
            processedImages = TerrainRuntime.s_processedImageIndex;

            float progressHeightmap = (float)processedHeightmaps / (float)totalchunks;
            float progressImage = (float)processedImages / (float)totalchunks;

            float progressHeightmapPercent = progressHeightmap * 100f;
            float progressbaseImagesPercent = progressImage * 100f;

            heightMapsPercent.text = (int)progressHeightmapPercent + "%";
            heightmapProgressBar.value = (int)progressHeightmapPercent;
            baseImagesPercent.text = (int)progressbaseImagesPercent + "%";
            imageProgressBar.value = (int)progressbaseImagesPercent;

            bool done = false;
            if (processCompleteTime > 0)
            {
                done = Time.timeSinceLevelLoad > (processCompleteTime + 0.5);
            }
            else
            {
                if (progressHeightmap >= 1.0)
                {
                    heightMapsRetrieved.color = 
                    heightMapsPercent.color = taskColorComplete;
                    heightmapProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = progressColorComplete;

                }

                if (progressImage >= 1.0)
                {
                    baseImagesRetrieved.color = 
                    baseImagesPercent.color = taskColorComplete;
                    imageProgressBar.gameObject.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = progressColorComplete;
                }
            }

            loadingUITitle.text = done ?
                loadingUITitleText[1] :
                loadingUITitleText[0];

            PlayNextSong();
        }


        //-------------------------------------------
        //  Player controller event handlers
        private void OnPlayerInteractionModeChanged(
            PlayerController.IAMode modeNew,
            PlayerController.IAMode modePrev)
        {
            hotkeyMenu.ShowKey(HotkeyMenu.Key.FlyUp, modeNew == PlayerController.IAMode.MinecraftFlyAlways);
            hotkeyMenu.ShowKey(HotkeyMenu.Key.FlyDown, modeNew == PlayerController.IAMode.MinecraftFlyAlways);
        }

        //-------------------------------------------
        //  Terrain event handlers

        void OnTerrainStateChanged(
                TerrainController.TerrainState newState,
                string newStateMessage,
                TerrainController.TerrainState fullState)
        {
           switch (newState)
            {
                case TerrainController.TerrainState.UserAborted:
                    ResetTaskCompletion();
                    errorDescription.text = "Canceling...";
                    break;

                case TerrainController.TerrainState.SceneInitialized:
                case TerrainController.TerrainState.Unloading:
                case TerrainController.TerrainState.Unloaded:
                    ResetTaskCompletion();
                    break;
                case TerrainController.TerrainState.TerrainsGenerated:
                    detailTerrrainCreated.color = taskColorComplete;
                    break;
                case TerrainController.TerrainState.FarTerrainsGenerated:
                    distantTerrrainCreated.color = taskColorComplete;
                    break;
                case TerrainController.TerrainState.BuildingDataReceived:
                    buildingsRetrieved.color = taskColorComplete;
                    break;
                case TerrainController.TerrainState.BuildingsGenerated:
                    buildingsCreated.color = taskColorComplete;
                    break;
            }

            if (controller.IsInState(TerrainController.TerrainState.TerrainsGenerated) &&
                controller.IsInState(TerrainController.TerrainState.FarTerrainsGenerated))
            {
                processCompleteTime = Time.timeSinceLevelLoad;
            }
        }

        void OnTerrainFatalError(
            TerrainController.FatalError newError,
            string newErrorMsg,
            ref List<TerrainController.FatalErrorReport> allErrorMsgs)
        {
            Trace.Log("TerrainPresenter received fatal error report {0}, '{1}'", newError, newErrorMsg);
            retryButton.gameObject.SetActive(true);
        }

        public void ShowPauseOptions(bool activate)
        {
            terrainPlayer.EnableUIInput(activate);
            Cursor.lockState = activate ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = activate;

            if (activate && latLonTextInput.text == "")
            {
                TerrainSettings settings = TerrainController.Settings;
                latLonTextInput.text = String.Format("{0}, {1}",
                    settings.latitudeUser,
                    settings.longitudeUser);
            }

            pauseMenu.SetActive(activate);
            fadeIn.SetActive(!activate);
            Time.timeScale = activate ? 0f : 1.0f;

            allAudioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
            foreach (AudioSource audio in allAudioSources)
            {
                if (!audio.gameObject.name.Equals("GameManager"))
                    audio.mute = activate;
            }

            if (activate)
            {
                buildingDetailPanel.gameObject.SetActive(false);
                Resources.UnloadUnusedAssets();
            }
        }

        private System.Collections.IEnumerator WaitForTerrainAborted(Action action)
        {
            while (Abortable.WaitForAbortables())
            {
                yield return Timing.WaitForSeconds(0.5f);
            }

            controller.Unload();
            ResetLoadingUI();

            action();
        }

        //-------------------------------------------
        //  Terrain loading UI button event handlers
        public void OnRetryTerrain()
        {
            SetPresentationState(PresentationState.ProcessingCanceled);
            controller.UpdateState(TerrainController.TerrainState.UserAborted);
            StartCoroutine(WaitForTerrainAborted(() =>
            {
                latLonInput.Latitude = Convert.ToDouble(TerrainController.Settings.latitudeUser);
                latLonInput.Longitude = Convert.ToDouble(TerrainController.Settings.longitudeUser);
                ReloadAndTeleport();
            }));
        }

        public void OnAbortTerrain()
        {
            canceledLatitude = Convert.ToDouble(TerrainController.Settings.latitudeUser);
            canceledLongitude = Convert.ToDouble(TerrainController.Settings.longitudeUser);

            SetPresentationState(PresentationState.ProcessingCanceled);
            controller.UpdateState(TerrainController.TerrainState.UserAborted);
            StartCoroutine(WaitForTerrainAborted(() =>
            {
                SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
            }));
        }

        //-------------------------------------------
        //  Pause Game Popup button event handlers
        public void OnExitToMainMenu()
        {
            SetPresentationState(PresentationState.Running);
            SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
        }

        public void OnResume()
        {
            SetPresentationState(PresentationState.Running);
        }

        public void OnLatLonInput()
        {
            teleportButton.interactable = latLonInput.Parse(latLonTextInput.text);
        }

        public void OnTeleportTo()
        {
            if (latLonInput.IsValid)
            {
                //  Prepare UI for terrain loading
                ShowPauseOptions(false);
                TerrainController.Get().UpdateState(TerrainController.TerrainState.UserAborted);
                StartCoroutine(WaitForTerrainAborted(() =>
                {
                    ReloadAndTeleport();
                }));
            }
        }

        private void ReloadAndTeleport()
        {
            Trace.Assert(latLonInput.IsValid, "Invalid latitude, longitude.");

            ResetLoadingUI();
            SetPresentationState(PresentationState.Processing);

            //  Disable the runtime to prevent updates and allow terrain to be destroyed.
            controller.Paused = true;
            controller.enabled = false;
            //  Remove the terrain and associated resources
            controller.Unload();

            //  Configure the new destination
            TerrainSettings settings = TerrainController.Settings;
            settings.latitudeUser = latLonInput.Latitude.ToString();
            settings.longitudeUser = latLonInput.Longitude.ToString();
            TerrainController.Settings = settings;

            //  Restart the runtime. The first Update() will detect the change
            //  in settings and reload the terrain
            controller.enabled = true;
            controller.Paused = false;
        }

        public void OnToggleLocation(InputAction.CallbackContext value)
        {
            if (value.started)
            {
                ShowLatitudeLongitude = !ShowLatitudeLongitude;
            }
        }

        private void LatLonFromPlayerPosition()
        {
            realWorldPosition = terrainPlayer.WorldPosition;

            double offsetLat = (realWorldPosition.z / EARTH_RADIUS) * 180 / Math.PI;
            playerLat = initialLat + offsetLat; // Moving NORTH/SOUTH

            double offsetLon = (realWorldPosition.x / (EARTH_RADIUS * Math.Cos(Math.PI * playerLat / 180))) * 180 / Math.PI;
            playerLon = initialLon + offsetLon; // Moving EAST/WEST
        }

        public void SetRunning()
        {
            initialLat = double.Parse(controller.latitudeUser);
            initialLon = double.Parse(controller.longitudeUser);

            if (startHeight <= 0f)
            {
                startHeight = 0.01f;
            }

            //  Position the player on the surface of the terrain
            if (TerrainHeightAtPlayerPosition(out terrainHeight))
            {
                terrainPlayer.WorldPosition = new Vector3(0, terrainHeight + terrain.gameObject.transform.position.y + startHeight, 0);
            }
            else
            {
                Trace.Warning("Terrain SetRunning: no terrain detected below.");
            }

            if (enableDetailTextures)
            {
                AddDetailTexturesToTerrains();
            }

            SetPresentationState(PresentationState.Running);
            terrainPlayer.ResetSpatialInput();

            startTime = Time.timeSinceLevelLoad;

            controller.ResetState(TerrainController.TerrainState.Running);
            controller.PrepareVersionData();
            controller.UpdateState(TerrainController.TerrainState.VersionLoaded);
        }

        private void CreateTempTerrain()
        {
            float terrainSize = 100000;

            SceneObject.Mode sceneMode = SceneObject.SceneModeOf(gameObject);
            Trace.Assert(sceneMode != SceneObject.Mode.INVALID, "GameObject does not belong to a SceneMode");

            GameObject terrainGameObject = SceneObject.Create(sceneMode, "Debug Terrain");
            terrainGameObject.transform.parent = SceneObject.GetPlayer(sceneMode).transform;
            terrainGameObject.transform.position = new Vector3(-(terrainSize / 2f), 0, -(terrainSize / 2f));
            terrainGameObject.AddComponent<Terrain>();

            TerrainData data = new TerrainData();
            data.size = new Vector3(terrainSize, 1, terrainSize);

            Terrain terrain = terrainGameObject.GetComponent<Terrain>();
            terrain.terrainData = data;

#if !UNITY_2019_1_OR_NEWER
            terrain.materialType = Terrain.MaterialType.Custom;
#endif
            terrain.materialTemplate = TerraLand.MaterialManager.GetTerrainMaterial();

            terrainGameObject.AddComponent<TerrainCollider>();
            terrainGameObject.GetComponent<TerrainCollider>().terrainData = data;

            terrainGameObject.layer = 8;

            if (enableDetailTextures)
                AddDetailTextures(terrain, detailBelending, false);
        }

        private void CheckDetailTextures()
        {
#if UNITY_EDITOR
            Texture2D[] detailTextures = new Texture2D[2] { detailTexture, detailNormal };

            foreach (Texture2D currentImage in detailTextures)
            {
                TextureImporter imageImport = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(currentImage)) as TextureImporter;

                if (imageImport != null && !imageImport.isReadable)
                {
                    imageImport.isReadable = true;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(currentImage), ImportAssetOptions.ForceUpdate);
                }
            }
#endif
        }

        private void AddDetailTexturesToTerrains()
        {
            List<Terrain> terrains = TerrainRuntime.s_croppedTerrains;

            foreach (Terrain t in terrains)
            {
                AddDetailTextures(t, detailBelending, false);
            }

            if (controller.farTerrain)
            {
                Terrain terrain1 = TerrainRuntime.s_firstTerrain;
                Terrain terrain2 = TerrainRuntime.s_secondaryTerrain;
                AddDetailTextures(terrain1, Mathf.Clamp(detailBelending * 1f, 0f, 100f), true);
                AddDetailTextures(terrain2, Mathf.Clamp(detailBelending * 1f, 0f, 100f), true);
            }
        }

        private void AddDetailTextures(Terrain terrain, float blend, bool farTerrain)
        {
            int startIndex = 0;

#if UNITY_2018_3_OR_NEWER
            try
            {
                if (terrain.terrainData.terrainLayers != null && terrain.terrainData.terrainLayers.Length > 0)
                    startIndex = terrain.terrainData.terrainLayers.Length;
                else
                    startIndex = 0;
            }
            catch (Exception e)
            {
                Trace.Exception(e);
                startIndex = 0;
            }

            TerrainLayer[] terrainLayers = new TerrainLayer[startIndex + 1];
#else
        startIndex = terrain.terrainData.splatPrototypes.Length;
        SplatPrototype[] terrainTextures = new SplatPrototype[startIndex + 1];
#endif

            for (int i = 0; i < startIndex + 1; i++)
            {
                try
                {
                    if (i < startIndex)
                    {
#if UNITY_2018_3_OR_NEWER
                        TerrainLayer currentLayer = terrain.terrainData.terrainLayers[i];

                        terrainLayers[i] = new TerrainLayer();
                        if (currentLayer.diffuseTexture != null) terrainLayers[i].diffuseTexture = currentLayer.diffuseTexture;

                        if (!farTerrain)
                        {
                            if (detailNormal != null)
                            {
                                terrainLayers[i].normalMapTexture = detailNormal;
                                terrainLayers[i].normalMapTexture.Apply();
                            }
                        }
                        else
                        {
                            if (detailNormalFar != null)
                            {
                                terrainLayers[i].normalMapTexture = detailNormalFar;
                                terrainLayers[i].normalMapTexture.Apply();
                            }
                        }

                        terrainLayers[i].tileSize = new Vector2(currentLayer.tileSize.x, currentLayer.tileSize.y);
                        terrainLayers[i].tileOffset = new Vector2(currentLayer.tileOffset.x, currentLayer.tileOffset.y);
                    }
                    else
                    {
                        terrainLayers[i] = new TerrainLayer();
                        if (detailTexture != null) terrainLayers[i].diffuseTexture = detailTexture;

                        if (!farTerrain)
                        {
                            if (detailNormal != null)
                            {
                                terrainLayers[i].normalMapTexture = detailNormal;
                                terrainLayers[i].normalMapTexture.Apply();
                            }
                        }
                        else
                        {
                            if (detailNormalFar != null)
                            {
                                terrainLayers[i].normalMapTexture = detailNormalFar;
                                terrainLayers[i].normalMapTexture.Apply();
                            }
                        }

                        if (!farTerrain)
                            terrainLayers[i].tileSize = new Vector2(detailTileSize, detailTileSize);
                        else
                            terrainLayers[i].tileSize = new Vector2(detailTileSize * 200f, detailTileSize * 200f);

                        terrainLayers[i].tileOffset = Vector2.zero;
                    }
#else
                    SplatPrototype currentSplatPrototye = terrain.terrainData.splatPrototypes[i];

                    terrainTextures[i] = new SplatPrototype();
                    if(currentSplatPrototye.texture != null) terrainTextures[i].texture = currentSplatPrototye.texture;

                    if(!farTerrain)
                    {
                        if(detailNormal != null)
                        {
                            terrainTextures[i].normalMap = detailNormal;
                            terrainTextures[i].normalMap.Apply();
                        }
                    }
                    else
                    {
                        if(detailNormalFar != null)
                        {
                            terrainTextures[i].normalMap = detailNormalFar;
                            terrainTextures[i].normalMap.Apply();
                        }
                    }

                    terrainTextures[i].tileSize = new Vector2(currentSplatPrototye.tileSize.x, currentSplatPrototye.tileSize.y);
                    terrainTextures[i].tileOffset = new Vector2(currentSplatPrototye.tileOffset.x, currentSplatPrototye.tileOffset.y);
                }
                else
                {
                    terrainTextures[i] = new SplatPrototype();
                    if(detailTexture != null) terrainTextures[i].texture = detailTexture;

                    if(!farTerrain)
                    {
                        if(detailNormal != null)
                        {
                            terrainTextures[i].normalMap = detailNormal;
                            terrainTextures[i].normalMap.Apply();
                        }
                    }
                    else
                    {
                        if(detailNormalFar != null)
                        {
                            terrainTextures[i].normalMap = detailNormalFar;
                            terrainTextures[i].normalMap.Apply();
                        }
                    }

                    if(!farTerrain)
                        terrainTextures[i].tileSize = new Vector2(detailTileSize, detailTileSize);
                    else
                        terrainTextures[i].tileSize = new Vector2(detailTileSize * 200f, detailTileSize * 200f);

                    terrainTextures[i].tileOffset = Vector2.zero;
                }
#endif
                }
                catch (Exception e)
                {
                    Trace.Exception(e);
                }
            }

#if UNITY_2018_3_OR_NEWER
            terrain.terrainData.terrainLayers = terrainLayers;
#else
        terrain.terrainData.splatPrototypes = terrainTextures;
#endif

            int length = terrain.terrainData.alphamapResolution;
            float[,,] smData = new float[length, length, startIndex + 1];

            try
            {
                for (int y = 0; y < length; y++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        if (startIndex + 1 > 1)
                        {
                            smData[y, z, 0] = 1f - (blend / 100f);
                            smData[y, z, 1] = blend / 100f;
                        }
                        else
                            smData[y, z, 0] = 1f;
                    }
                }

                terrain.terrainData.SetAlphamaps(0, 0, smData);
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }

            terrain.terrainData.RefreshPrototypes();
            terrain.Flush();

            smData = null;

#if UNITY_2018_3_OR_NEWER
            terrainLayers = null;
#else
        terrainTextures = null;
#endif
        }

        void OnGUI()
        {
            if (ShowLatitudeLongitude &&
                controller.IsInState(TerrainController.TerrainState.Running))
            {
                GUI.backgroundColor = new UnityEngine.Color(0.3f, 0.3f, 0.3f, 0.3f);
                GUI.Box(new Rect(10, Screen.height - 35, 220, 22), "Lat: " + playerLat.ToString("0.000000") + "   Lon: " + playerLon.ToString("0.000000"));
            }
        }

        private void UnloadResources()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

#if UNITY_EDITOR
        private void SetupShadowSettings()
        {
            SerializedObject qualitySettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            SerializedProperty levels = qualitySettings.FindProperty("m_QualitySettings");

            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                GetChildProperty(level, "shadowCascades").enumValueIndex = 2;
                GetChildProperty(level, "shadowCascade4Split").vector3Value = new Vector3(0.005f, 0.05f, 0.35f);
            }

            qualitySettings.ApplyModifiedProperties();

            print("Shadow Settings have been overwritten");
        }

        private static SerializedProperty GetChildProperty(SerializedProperty parent, string name)
        {
            SerializedProperty child = parent.Copy();
            child.Next(true);

            do
            {
                if (child.name == name) return child;
            }

            while (child.Next(false));

            return null;
        }
#endif

        private void SetupMusic()
        {
            musicPlayer = GetComponent<AudioSource>();
            if (musicPlayer != null)
            {
                audioTracks = new AudioClip[]
                {
                    Resources.Load("Menu/Music/BenSound-SciFi") as AudioClip,
                    Resources.Load("Menu/Music/Exclusion-Earthshine") as AudioClip,
                    Resources.Load("Menu/Music/Exclusion-Unity") as AudioClip,
                    Resources.Load("Menu/Music/Machinimasound-SeptemberSky") as AudioClip,
                    Resources.Load("Menu/Music/Mnykin-ElfSwamp") as AudioClip
                };
                trackIndex = UnityEngine.Random.Range(0, audioTracks.Length);
                musicPlayer.clip = audioTracks[trackIndex];
                musicPlayer.Play();
            }
        }

        private void PlayNextSong()
        {
            if (musicPlayer != null && !musicPlayer.isPlaying)
            {
                trackIndex++;

                if (trackIndex >= audioTracks.Length)
                    trackIndex = 0;

                musicPlayer.clip = audioTracks[trackIndex];
                musicPlayer.Play();
            }
        }

        public void OnEnable()
        {
            if (presentationStateOnDisable == PresentationState.ProcessingCanceled)
            {
                latLonInput.Latitude = canceledLatitude;
                latLonInput.Longitude = canceledLongitude;

                ReloadAndTeleport();
            }
            else
            {
                controller.Paused = false;
            }
        }

        public void OnDisable()
        {
            presentationStateOnDisable = presentationState;

            if (controller != null)
            {
                controller.Paused = true;
            }
        }
    }
}