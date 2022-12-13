using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //  Player ordinal (for multiple players on some device)
    private int playerID;

    //  For Unity's Insprectr Panel:
    public enum IAMode
    {
        Pointing = 0,       // default spatial input processing
        Minecraft,          // mouse look, WASD lateral keyboard movement, SPACE/SHIFT vertical movement, and gravity
        MinecraftFlyAlways, // mouse look, WASD lateral keyboard movement, SPACE/SHIFT vertical movement, zero gravity
    };
    private const IAMode DEFAULT_NONPOINTING_IAMODE = IAMode.Minecraft;

    public  bool   AllowFly = true;
    private IAMode _interactionMode = DEFAULT_NONPOINTING_IAMODE; 
    private IAMode _lastNonPointingIAMode = DEFAULT_NONPOINTING_IAMODE;

    [Header("Sub Behaviours")]
    public PlayerMovementBehaviour playerMovementBehavior;
    public PlayerVisualBehaviour playerVisualBehavior;

    [Header("Input Settings")]
    public PlayerInput playerInput;
    public float MoveSmoothing = 3f;

    //  Events
    public delegate void PlayerInteractionModeChanged(IAMode modeNew, IAMode modePrev);
    public static event PlayerInteractionModeChanged OnPlayerInteractionModeChanged;

    public delegate void PlayerLookingAtEnter(ref GameObject gameObject, ref RaycastHit hit);
    public static event PlayerLookingAtEnter OnPlayerLookingAtEnter;

    public delegate void PlayerLookingAtContinue(ref GameObject gameObject, ref RaycastHit hit);
    public static event PlayerLookingAtContinue OnPlayerLookingAtContinue;

    public delegate void PlayerLookingAtLeave(ref GameObject gameObjectLeave, ref RaycastHit hitLeave);
    public static event PlayerLookingAtLeave OnPlayerLookingAtLeave;

    //  Private state
    private Vector3 rawMove;
    private Vector3 smoothMove;
    private Vector2 rawLook;

    private RaycastHit hitCurrent;
    private GameObject gameObjectHitCurrent;

    private string actionMapPlayerControls = "Player Controls";
    private string actionMapMenuControls = "Menu Controls";

    //  Current Control Scheme
    private string currentControlScheme;

    //  Diagnostic console logging
    Trace.Config hitTestTraces = null; // new Trace.Config();
    Trace.Config inputTraces = null; // new Trace.Config();
    Trace.Config collisionTraces = null; // new Trace.Config();

    public static PlayerController Get(GameObject gameObject)
    {
        return SceneObject.PlayerControllerOf(gameObject);
    }

    //  Accessor methods
    public int GetPlayerID()
    {
        return playerID;
    }

    public IAMode GetInteractionMode()
    {
        return _interactionMode;
    }

    public bool SetInteractionMode(IAMode mode)
    {
        //  assign a new interaction mode
        if (mode != _interactionMode)
        {
            if (mode == IAMode.MinecraftFlyAlways && !AllowFly)
            {
                return false;
            }

            IAMode modePrev = _interactionMode;
            _interactionMode = mode;
            playerMovementBehavior.SetupBehavior(mode);
            playerVisualBehavior.SetupBehavior(mode);

            if (OnPlayerInteractionModeChanged != null)
            {
                OnPlayerInteractionModeChanged(mode, modePrev);
            }
        }
        return true;
    }

    public IAMode ActivateInteractionMode()
    {
        //  realize the current interaction mode
        playerMovementBehavior.SetupBehavior(_interactionMode);
        playerVisualBehavior.SetupBehavior(_interactionMode);

        return _interactionMode;
    }

    [HideInInspector()]
    public Vector3 BoundsOrigin
    {
        get { return playerMovementBehavior.BoundsOrigin; }
        set { playerMovementBehavior.BoundsOrigin = value; }
    }

    public void EnableUIInput(bool enable)
    {
        if (enable)
        {
            if (GetInteractionMode() != IAMode.Pointing)
            {
                _lastNonPointingIAMode = GetInteractionMode();
            }
            SetInteractionMode(IAMode.Pointing);
        }
        else
        {
            SetInteractionMode(_lastNonPointingIAMode);
        }
    }

    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }

    //  Called before the first frame update
    void Start()
    {
        SetupPlayer(0);

        GameObject player = SceneObject.GetPlayer(SceneObject.Mode.Player);
        player.AddComponent<VersionChanger>();
    }

    //  Called from the GameManager when the game is being setup.
    public void SetupPlayer(int newPlayerID)
    {
        playerID = newPlayerID;

        currentControlScheme = playerInput.currentControlScheme;

        rawMove = new Vector3();
        rawLook = new Vector3();

        //  Set up the initial interaction and Scene mode
        playerMovementBehavior.SetupBehavior(_interactionMode);
        playerVisualBehavior.SetupBehavior(_interactionMode);
    }

    //  InputSystem Unity Event Handlers

    public void OnMove(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnMove({0})", value.ToString());
            Vector2 Movement = value.ReadValue<Vector2>();
            rawMove.x = Movement.x;
            rawMove.z = Movement.y;
        }
    }

    //  MoveX/Y/Z is for keyboard only. 
    //  Input System keyboard via Vector2 jerks the key-up.
    public void OnMoveX(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnMoveX({0})", value.ToString());
            rawMove.x = value.ReadValue<float>();
        }
    }

    public void OnMoveY(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnMoveY({0})", value.ToString());
            rawMove.y = value.ReadValue<float>();
        }
    }

    public void OnMoveZ(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnMoveZ({0})", value.ToString());
            rawMove.z = value.ReadValue<float>();
        }
    }

    public void OnToggleFly(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled && value.started)
        {
            if (GetInteractionMode() == IAMode.Minecraft && AllowFly)
            {
                SetInteractionMode(IAMode.MinecraftFlyAlways);
            }
            else if (GetInteractionMode() == IAMode.MinecraftFlyAlways)
            {
                SetInteractionMode(IAMode.Minecraft);
            }
        }
    }

    public void OnFlyMotion(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnFly({0})", value.ToString());
            rawMove.y = value.ReadValue<float>();
        }
    }

    public void OnToggleKeyHelp(InputAction.CallbackContext value)
    {
        GameObject hotkeyMenu = SceneObject.Find(
            SceneObject.Get().ActiveMode,
            ObjectName.HOTKEY_HELP);

        if (hotkeyMenu != null && value.started)
        {
            hotkeyMenu.SetActive(!hotkeyMenu.activeSelf);
        }
    }

    public void ResetSpatialInput()
    {
        ResetMovement();
        ResetLook();
    }

    public void ResetMovement()
    {
        rawMove = Vector3.zero;
    }

    public void ResetLook()
    {
        rawLook = Vector3.zero;
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnLook({0})", value.ToString());
            rawLook = value.ReadValue<Vector2>();
        }
    }

    public void OnFire(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "PlayerController.OnFire({0})", value.ToString());
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(collisionTraces, "PlayerController.OnCollisionEnter(" + collision.ToString() + ")");
        }
    }

    public void OnCreatorEdit(InputAction.CallbackContext context)
    {
        if (playerMovementBehavior.inputEnabled &&
            (context.phase == InputActionPhase.Performed))
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Creator;
        }
    }

    public void OnDeviceLost()
    {
        Debug.Log("PlayerController.OnDeviceLost()");
        //  Todo: Display input device lost visuals.
    }

    public void OnDeviceRegained()
    {
        Debug.Log("PlayerController.OnDeviceRegained()");
        StartCoroutine(WaitForDeviceToBeRegained());
    }

    IEnumerator WaitForDeviceToBeRegained()
    {
        yield return new WaitForSeconds(0.1f);
        //  Todo: Hide input device lost visuals.
    }

    //This is called from Player Input, when a button has been pushed, that correspons with the 'TogglePause' action
    public void OnTogglePause(InputAction.CallbackContext value)
    {
        Debug.Log("PlayerController.OnTogglePause(" + value + ")");
        /*
                if(value.started)
                {
                    GameManager.Instance.TogglePauseState(this);
                }
        */
    }

    //  InputSystem Callbacks

    public void OnControlsChanged()
    {
        if (playerInput.currentControlScheme != currentControlScheme)
        {
            currentControlScheme = playerInput.currentControlScheme;
            RemoveAllBindingOverrides();
        }
    }

    //  Update Loop
    void Update()
    {
        if (playerMovementBehavior.inputEnabled)
        {
            //  Smooth and transfer input
            smoothMove = Vector3.Lerp(smoothMove, rawMove, Time.deltaTime * MoveSmoothing);

            playerMovementBehavior.UpdateLookDelta(rawLook);
            playerMovementBehavior.UpdateMoveDelta(smoothMove);

            RaycastHit hit;
            GameObject gameObjectHit = null;
            if (playerVisualBehavior.HitTest(out hit))
            {
                gameObjectHit = hit.transform.gameObject;
            }

            if (gameObjectHit != gameObjectHitCurrent)
            {
                if (gameObjectHitCurrent != null)
                {
                    Trace.Log(hitTestTraces, "No longer looking at {0}", gameObjectHitCurrent.name);
                    if (OnPlayerLookingAtLeave != null)
                    {
                        OnPlayerLookingAtLeave(ref gameObjectHitCurrent, ref hitCurrent);
                    }
                }

                if (gameObjectHit != null)
                {
                    Trace.Log(hitTestTraces, "Looking at {0}", gameObjectHit.name);
                    if (OnPlayerLookingAtEnter != null)
                    {
                        OnPlayerLookingAtEnter(ref gameObjectHit, ref hit);
                    }
                }
            }
            else if (gameObjectHit != null)
            {
                // Trace.Log(hitTestTraces, "Still looking at {0}", gameObjectHit.name);
                if (OnPlayerLookingAtContinue != null)
                {
                    OnPlayerLookingAtContinue(ref gameObjectHit, ref hit);
                }
            }

            hitCurrent = hit;
            gameObjectHitCurrent = gameObjectHit;
        }
        else if (gameObjectHitCurrent != null)
        {
            if (OnPlayerLookingAtLeave != null)
            {
                OnPlayerLookingAtLeave(ref gameObjectHitCurrent, ref hitCurrent);
            }
            gameObjectHitCurrent = null;
        }
    }

    public void SetInputActiveState(bool gameIsPaused)
    {
        switch (gameIsPaused)
        {
            case true:
                playerInput.DeactivateInput();
                break;

            case false:
                playerInput.ActivateInput();
                break;
        }
    }

    void RemoveAllBindingOverrides()
    {
        InputActionRebindingExtensions.RemoveAllBindingOverrides(playerInput.currentActionMap);
    }

    //  Switching Action Maps ----    
    public void EnableGameplayControls()
    {
        playerInput.SwitchCurrentActionMap(actionMapPlayerControls);
    }

    public void EnablePauseMenuControls()
    {
        playerInput.SwitchCurrentActionMap(actionMapMenuControls);
    }
}
