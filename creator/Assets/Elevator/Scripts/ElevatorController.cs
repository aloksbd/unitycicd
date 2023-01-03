using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using ObjectModel;
using Sprite = UnityEngine.Sprite;
using System.Linq;
using Unity.VisualScripting;

public class ElevatorController : MonoBehaviour
{
    //  Constants
    private const int       INVALID_FLOOR = -1;
    private const int       FLOORWHEEL_ITEM_COUNT = 13;
    private const int       FLOORWHEEL_ITEM_CURRENT = FLOORWHEEL_ITEM_COUNT / 2;
    private const float     FLOOR_TRAVEL_DURATION = 1.0f; // seconds
    private const float     ARRIVAL_PAUSE_DURATION = 2.0f; // seconds
    private const float     CROSSFADE_DURATION = 1.25f; // seconds

    private static ElevatorController s_instance = null;

    //  Inspector properties
    public PlayerController playerController;
    public GameObject       elevatorPanel;
    public HotkeyMenu       hotkeyMenu;
    public ImageFade        blackCurtain;

    //  Events
    public delegate void    PlayerExitElevator(GameObject elevatorDoor, int floor);
    public static event     PlayerExitElevator OnPlayerExitElevator;

    public static Action<int, bool>    OnPlayerEnterElevatorDoor;

    //  Internal UI elements
    TMP_InputField          floorInputField;
    Image                   directionIndicator;
    ImageFade               directionFade;
    Sprite                  directionUp;
    Sprite                  directionDown;
    Sprite                  directionStopped;
    Button                  goButton;
    Scrollbar               scrollbar;
    GameObject              floorWheel;

    //  Internal state: Elevator panel
    enum Mode
    {
        FloorSelection,
        Moving
    };

    private GameObject      elevatorDoor;
    private int             floorCount;
    private int             startingFloor;
    private int             destinationFloor;
    private int             currentFloor;
    private bool            hasLobby;
    private bool            hasRoof;
    private Mode            mode = Mode.FloorSelection;
    private bool            manualUpdate;
    private bool            canceledInMotion;

    private Dictionary<int, TextMeshProUGUI> floorItems = new Dictionary<int, TextMeshProUGUI>();
    private Dictionary<int, string> floorNames = new Dictionary<int, string>();

    //  Diagnostics
    private Trace.Config    traceConfig = new Trace.Config();

    [SerializeField] private GameObject elevatorKiosk;
    [SerializeField] private GameObject elevatorPlayer;
    [SerializeField] private Transform currentBuildingTransform;

    [SerializeField] private ElevatorDoor[] elevatorDoors;

    public static bool IsPlayerOnRoof { get; private set; }

    private void Awake()
    {
        s_instance = this;
        elevatorPlayer.SetActive(true);
        elevatorKiosk.SetActive(false);
    }

    public static void LoadBuildingAndFloorData(Transform buildingT, int floorNumber)
    {
        ElevatorController controller = Get();
        controller.currentBuildingTransform = buildingT;
        controller.startingFloor = controller.destinationFloor = controller.currentFloor = floorNumber;
    }

    private void GetBuildingData()
    {
        elevatorDoors = currentBuildingTransform.GetComponentsInChildren<ElevatorDoor>(true);
        floorCount = elevatorDoors.Select(e => e.FloorNumber).Distinct().Count();
    }

    public static ElevatorController Get()
    {
        return s_instance;
    }

    public static void EnterElevatorFromRoof()
    {
        EnterElevator(null, -1, -1, true, true);
    }

    public static bool EnterElevator(
        Transform _elevatorBuildingRef, //  elevator door or containing wall. This object's transform will be used to set the orient the player following exit
        int _floorCount,          //  Number of floors not including roof
        int _currentFloor,       //  zero-based floor number
        bool _hasLobbyFloor,      //  Determines whether 1st floor will be labeld "L" and Lobby hotkey is enabled
        bool _hasRoofFloor)       //  Determines whether the elevato allows access to the roof
    {
        //Trace.Assert(elevatorDoor != null, "Elevator door gameobject must not be null");
        if (_currentFloor >= -1)// && _startingFloor < _floorCount)
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Elevator;
            ElevatorController controller = Get();
            Trace.Assert(controller != null, "Elevator Controller property was not assigned in Inspector");

            controller.hasLobby = _hasLobbyFloor;
            controller.hasRoof = _hasRoofFloor;
            //controller.floorCount = _floorCount + (_hasRoofFloor ? 1 : 0);

            //controller.startingFloor =
            //controller.destinationFloor =
            //controller.currentFloor = _currentFloor;

            controller.blackCurtain.FadeOut(CROSSFADE_DURATION);
            controller.InitializePanel();
            controller.SetMode(Mode.FloorSelection, true);
            controller.ShowPanel(true);

            return true;
        }

        Trace.Warning("Invalid elevator floor count or starting floor");
        return false;
    }

    private int Floor
    {
        get
        {
            switch (mode)
            {
                case Mode.FloorSelection:
                    return destinationFloor;
                case Mode.Moving:
                    return currentFloor;
            }
            Trace.Assert(false, "Invalid elevator mode");
            return INVALID_FLOOR;
        }
    }

    private void ShowPanel(bool show)
    {
        playerController.EnableUIInput(show);
        elevatorPanel.SetActive(true /*show*/);
    }

    private void InitializePanel()
    {
        floorInputField = elevatorPanel.transform.Find("FloorIndicator/FloorInput").gameObject.GetComponent<TMP_InputField>();
        directionIndicator = elevatorPanel.transform.Find("FloorIndicator/DirectionIndicator").gameObject.GetComponent<Image>();
        directionFade = elevatorPanel.transform.Find("FloorIndicator/DirectionIndicator").gameObject.GetComponent<ImageFade>();
        directionUp = Resources.Load<Sprite>("Images/elevator_uparrow");
        directionDown = Resources.Load<Sprite>("Images/elevator_downarrow");
        directionStopped = Resources.Load<Sprite>("Images/elevator_stopped");
        goButton = elevatorPanel.transform.Find("FloorIndicator/GoButton").gameObject.GetComponent<Button>();
        scrollbar = elevatorPanel.transform.Find("Scrollbar").gameObject.GetComponent<Scrollbar>();
        floorWheel = elevatorPanel.transform.Find("WheelPanel").gameObject;

        floorItems.Clear();
        floorNames.Clear();

        floorItems = new Dictionary<int, TextMeshProUGUI>();
        floorNames = new Dictionary<int, string>();

        GetBuildingData();

        if (currentFloor == -1)
        {
            currentFloor = startingFloor = destinationFloor = floorCount - 1;
        }

        scrollbar.numberOfSteps = floorCount;
        scrollbar.size = 1.0f / (float)floorCount;

        //  Floor names
        for (int f = 0; f < floorCount; f++)
        {
            if (f == 0 && hasLobby)
            {
                floorNames.Add(f, "L");
            }
            else if (f == (floorCount - 1) && hasRoof)
            {
                floorNames.Add(f, "R");
            }
            else
            {
                floorNames.Add(f, string.Format("{0}", f + 1));
            }
        }

        //  Floor items
        for (int f = 0; f < FLOORWHEEL_ITEM_COUNT; f++)
        {
            TextMeshProUGUI item = floorWheel.transform.GetChild((FLOORWHEEL_ITEM_COUNT - 1) - f).gameObject.GetComponent<TextMeshProUGUI>();
            floorItems.Add(f, item);
        }

        List<HotkeyMenu.Key> keys = new List<HotkeyMenu.Key>()
        {
            HotkeyMenu.Key.Lobby,
            HotkeyMenu.Key.Roof,
            HotkeyMenu.Key.Cancel,
            HotkeyMenu.Key.Leave,
            HotkeyMenu.Key.HotkeyMenu
        };
        hotkeyMenu.Populate(keys);

        UpdateScrollBar();
        UpdateFloorWheel();
        UpdateFloorInput();
        UpdateGoButton();
        UpdateHotkeys();
    }

    private void UpdateFloorWheel()
    {
        int nameOffset = (FLOORWHEEL_ITEM_CURRENT - Floor);

        for (int f = 0; f < FLOORWHEEL_ITEM_COUNT; f++)
        {
            int iName = (f - nameOffset);
            if (!floorItems.ContainsKey(f))
            {
                InitializePanel();
            }
            if (iName < 0 || iName >= floorNames.Count)
            {
                floorItems[f].text = "";
            }
            else
            {
                floorItems[f].text = floorNames[iName];
            }
        }
    }

    private void UpdateScrollBar()
    {
        manualUpdate = true;

        scrollbar.value = (float)Floor / (float)(floorCount - 1);
        //  Note: updating the value of the scrollbar triggers
        //  OnFloorScrollValueChanged(), which in turn updates all other UI elements.

        manualUpdate = false;
    }

    private void UpdateFloorInput()
    {
        if (Floor == 0 && hasLobby)
        {
            floorInputField.text = "L";
        }
        else if (Floor == floorCount - 1 && hasRoof)
        {
            floorInputField.text = "R";
        }
        else
        {
            floorInputField.text = string.Format("{0}", Floor + 1);
        }
    }

    private void UpdateGoButton()
    {
        goButton.interactable = (mode == Mode.FloorSelection && destinationFloor != startingFloor);
    }

    private void UpdateHotkeys()
    {
        hotkeyMenu.ShowKey(HotkeyMenu.Key.Lobby, mode == Mode.FloorSelection && hasLobby);
        hotkeyMenu.ShowKey(HotkeyMenu.Key.Roof, mode == Mode.FloorSelection && hasRoof);
        hotkeyMenu.ShowKey(HotkeyMenu.Key.Leave, mode == Mode.FloorSelection);
        hotkeyMenu.ShowKey(HotkeyMenu.Key.Cancel, mode == Mode.Moving);
    }

    //  Panel event handlers
    public void OnFloorInputValueChanged(string value)
    {
        foreach (int floor in floorNames.Keys)
        {
            if (floorNames[floor] == value)
            {
                if (!manualUpdate)
                {
                    // triggered from user interaction; update destination floor
                    destinationFloor = floor;
                    UpdateScrollBar();
                }
                return;
            }
        }
        // Invalid floor...
        goButton.interactable = false;
    }

    public void OnFloorInputEndEdit(string value)
    {
        Trace.Log(traceConfig, "ElevatorController.OnFloorInputEndEdit({0})", value);
    }

    public void OnGoButtonClick()
    {
        if (goButton.interactable)
        {
            Trace.Log(traceConfig, "ElevatorController.OnButtonClick()");
            SetMode(Mode.Moving);
            StartCoroutine(MoveElevator());
        }
    }

    public void OnFloorScrollValueChanged(float value)
    {
        float floor = scrollbar.value * (float)(floorCount - 1);
        Trace.Log(traceConfig, "ElevatorController: scroolbar.value = {0}, floor = {1}", scrollbar.value, floor);

        if (!manualUpdate)
        {
            // triggered from user interaction; update destination floor
            destinationFloor = (int)floor;
        }
        UpdateFloorWheel();
        UpdateFloorInput();
        UpdateGoButton();
    }

    public void OnAccept(InputAction.CallbackContext value)
    {
        OnGoButtonClick();
    }

    public void OnLobbyHotKey(InputAction.CallbackContext value)
    {
        if (value.started && mode == Mode.FloorSelection && hasLobby)
        {
            destinationFloor = 0;
            UpdateScrollBar();
        }
    }

    public void OnRoofHotkey(InputAction.CallbackContext value)
    {
        if (value.started && mode == Mode.FloorSelection && hasRoof)
        {
            destinationFloor = (floorCount - 1);
            UpdateScrollBar();
        }
    }

    public void OnCancelOrLeave(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            destinationFloor = currentFloor = startingFloor;

            if (mode == Mode.Moving)
            {
                canceledInMotion = true;
                StopCoroutine(MoveElevator());
                UpdateScrollBar();
                SetMode(Mode.FloorSelection);
            }
            else if (mode == Mode.FloorSelection)
            {
                UpdateScrollBar();
                ExitPlayer();
            }
        }
    }

    private void SetMode(Mode mode, bool init = false)
    {
        if (mode != this.mode || init)
        {
            this.mode = mode;
            goButton.gameObject.SetActive(mode == Mode.FloorSelection);
            directionIndicator.gameObject.SetActive(mode == Mode.Moving);
            UpdateHotkeys();
            ShowPanel(mode == Mode.FloorSelection);

            if (mode == Mode.Moving)
            {
                directionIndicator.sprite = null;
                if (destinationFloor > currentFloor)
                {
                    directionIndicator.sprite = directionUp;
                }
                else if (destinationFloor < currentFloor)
                {
                    directionIndicator.sprite = directionDown;
                }
            }
        }
    }

    private IEnumerator MoveElevator()
    {
        while (currentFloor != destinationFloor)
        {
            Trace.Log(traceConfig, "ElevatorController.MoveElevator() - currentFloor: {0}, destinationFloor: {1}",
                currentFloor, destinationFloor);

            UpdateScrollBar();
            directionFade.FadeOut(FLOOR_TRAVEL_DURATION);

            if (currentFloor < destinationFloor)
            {
                currentFloor++;
            }
            else if (currentFloor > destinationFloor)
            {
                currentFloor--;
            }

            if (currentFloor == destinationFloor)
            {
                UpdateScrollBar();
                directionFade.Cancel(1.0f);
                directionIndicator.sprite = directionStopped;

                //  TODO:
                //  - Ring arrival
                //  - Start open door animation

                //  - Execute one last time delay before exiting the user.
                yield return new WaitForSeconds(ARRIVAL_PAUSE_DURATION);

                blackCurtain.FadeIn(CROSSFADE_DURATION);
                yield return new WaitForSeconds(CROSSFADE_DURATION);
                break;
            }

            yield return new WaitForSeconds(FLOOR_TRAVEL_DURATION);
        }

        Trace.Log(traceConfig, "ElevatorController.MoveElevator(): Exiting player.");

        if (!canceledInMotion)
        {
            ExitPlayer();
        }
        canceledInMotion = false;
    }

    public void ExitPlayer()
    {
        SetMode(Mode.FloorSelection);

        if (OnPlayerExitElevator != null)
        {
            //OnPlayerExitElevator(elevatorDoor, destinationFloor);
            ElevatorFloorReached(null, destinationFloor);
        }
        else
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
        }
    }

    Vector3 playerSpawnPos;
    Quaternion playerSpawnRot;
    private void ElevatorFloorReached(GameObject _, int floorNumber)
    {
        Transform playerPos;
        if (hasRoof && (floorCount - 1) == floorNumber)
        {
            playerPos = elevatorDoors[0].PlayerT;
            playerSpawnPos = playerPos.position;
            playerSpawnPos.y = WHConstants.DefaultWallHeight * floorCount;
        }
        else
        {
            playerPos = elevatorDoors.First(e => e.FloorNumber == floorNumber).PlayerT;
            playerSpawnPos = playerPos.position;
        }
        playerSpawnPos.y += 0.2f;
        playerSpawnRot = playerPos.rotation;
        playerSpawnRot *= Quaternion.Euler(0, 180f, 0);

        SceneObject.GetPlayer(SceneObject.Mode.Player).transform.SetPositionAndRotation(playerSpawnPos, playerSpawnRot);
        SceneObject.Get().ActiveMode = SceneObject.Mode.Player;

        currentFloor = floorNumber;

        OnPlayerEnterElevatorDoor(currentFloor, currentFloor == (floorCount - 1));

        IsPlayerOnRoof = currentFloor == (floorCount - 1);
    }
}
