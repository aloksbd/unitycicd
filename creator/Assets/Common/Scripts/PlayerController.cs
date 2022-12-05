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
    private const IAMode DEFAULT_NONPOINTING_IAMODE = IAMode.MinecraftFlyAlways;

    public IAMode InteractionMode;
    private IAMode lastNonPointingIAMode = DEFAULT_NONPOINTING_IAMODE;

    [Header("Sub Behaviours")]
    public PlayerMovementBehaviour playerMovementBehavior;
    public PlayerVisualBehaviour playerVisualBehavior;

    [Header("Input Settings")]
    public PlayerInput playerInput;
    public float MoveSmoothing = 3f;

    //  Private state
    private Vector3 rawMove;
    private Vector3 smoothMove;
    private Vector2 rawLook;

    private string actionMapPlayerControls = "Player Controls";
    private string actionMapMenuControls = "Menu Controls";

    //  Current Control Scheme
    private string currentControlScheme;

    //  Diagnostics
    Trace.Config inputTraces = null; // new Trace.Config(true, true);
    Trace.Config collisionTraces = null; // new Trace.Config(true, true);

    public static PlayerController Get(GameObject gameObject)
    {
        return SceneObject.PlayerControllerOf(gameObject);
    }

    //  Accessor methods
    public int GetPlayerID()
    {
        return playerID;
    }

    public void SetInteractionMode(IAMode mode)
    {
        //  assign a new interaction mode
        if (mode != InteractionMode)
        {
            InteractionMode = mode;
            playerMovementBehavior.SetupBehavior(mode);
            playerVisualBehavior.SetupBehavior(mode);
        }
    }

    public IAMode ActivateInteractionMode()
    {
        //  realize the current interaction mode
        playerMovementBehavior.SetupBehavior(InteractionMode);
        playerVisualBehavior.SetupBehavior(InteractionMode);

        return InteractionMode;
    }

    public IAMode GetInteractionMode()
    {
        return InteractionMode;
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
                lastNonPointingIAMode = GetInteractionMode();
            }
            SetInteractionMode(IAMode.Pointing);
        }
        else
        {
            SetInteractionMode(lastNonPointingIAMode);
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
#if ADMIN
        player.AddComponent<PegasusGameController>();
#endif
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
        playerMovementBehavior.SetupBehavior(InteractionMode);
        playerVisualBehavior.SetupBehavior(InteractionMode);
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

    public void OnFly(InputAction.CallbackContext value)
    {
        if (playerMovementBehavior.inputEnabled)
        {
            Trace.Log(inputTraces, "OnFly({0})", value.ToString());
            rawMove.y = value.ReadValue<float>();
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
        }

        RaycastHit hit;
        if (playerVisualBehavior.HitTest(out hit))
        {
            Trace.Log(inputTraces, "Raycast hit: " + hit.ToString());
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
