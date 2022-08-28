using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    //  Player ordinal (for multiple players on some device)
    private int playerID; 

    //  For Unity's Insprectr Panel:
    public enum IAMode
    {
        Minecraft = 0,
        MinecraftFlyAlways = 1,
        Pointing
    };

    public IAMode InteractionMode;

    [Header("Sub Behaviours")]
    public PlayerMovementBehaviour  playerMovementBehavior;
    public PlayerVisualBehaviour    playerVisualBehavior;

    [Header("Input Settings")]
    public  PlayerInput playerInput;
    public  float       MoveSmoothing = 3f;

    //  Private state
    private Vector3     rawMove;
    private Vector3     smoothMove;
    private Vector2     rawLook;

    private string      actionMapPlayerControls = "Player Controls";
    private string      actionMapMenuControls = "Menu Controls";

    //  Current Control Scheme
    private string      currentControlScheme;

    [Header("GUI Controller")]
    public GameObject fbxController;
    private bool guiActive = false;

    //  Accessor methods
    public int GetPlayerID()
    {
        return playerID;
    }

    public void SetInteractionMode(IAMode mode)
    {
        InteractionMode = mode;
        playerMovementBehavior.SetupBehavior(mode);
        playerVisualBehavior.SetupBehavior(mode);
    }

    public IAMode GetInteractionMode()
    {
        return InteractionMode;
    }

    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }

    //  Called before the first frame update
    void Start()
    {
        SetupPlayer(0);
        // fbxController = GetComponent<FBXController>();
        fbxController.SetActive(guiActive);
    }

    //  Called from the GameManager when the game is being setup.
    public void SetupPlayer(int newPlayerID)
    {
        playerID = newPlayerID;

        currentControlScheme = playerInput.currentControlScheme;

        rawMove = new Vector3();
        rawLook = new Vector3();
        playerMovementBehavior.SetupBehavior(InteractionMode);
        playerVisualBehavior.SetupBehavior(InteractionMode);
    }

    //  InputSystem Unity Event Handlers

    public void OnMove(InputAction.CallbackContext value)
    {
        Debug.Log("OnMove");
        Vector2 Movement = value.ReadValue<Vector2>();
        rawMove.x = Movement.x;
        rawMove.z = Movement.y;
    }

    //  MoveX/Y/Z is for keyboard only. 
    //  Input System keyboard via Vector2 jerks the key-up.
    public void OnMoveX(InputAction.CallbackContext value)
    {
        Debug.Log("OnMoveX");
        rawMove.x = value.ReadValue<float>();
    }

    public void OnMoveY(InputAction.CallbackContext value)
    {
        Debug.Log("OnMoveY");
        rawMove.y = value.ReadValue<float>();
    }

    public void OnMoveZ(InputAction.CallbackContext value)
    {
        Debug.Log("OnMoveZ");
        rawMove.z = value.ReadValue<float>();
    }

    public void OnFly(InputAction.CallbackContext value)
    {
        rawMove.y = value.ReadValue<float>();
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        rawLook = value.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext value)
    {
        //Debug.Log("PlayerController.OnFire(" + value + ")");
    }

    public void OnGUIActivate(InputAction.CallbackContext value)
    {
        guiActive = !guiActive;
        fbxController.SetActive(guiActive);
        Time.timeScale = guiActive ? 0 : 1;
        Cursor.visible = guiActive;
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
        if(playerInput.currentControlScheme != currentControlScheme)
        {
            currentControlScheme = playerInput.currentControlScheme;
            RemoveAllBindingOverrides();
        }
    }

    //  Update Loop
    void Update()
    {
        //  Smooth and transfer input
        smoothMove = Vector3.Lerp(smoothMove, rawMove, Time.deltaTime * MoveSmoothing);

        playerMovementBehavior.UpdateLookDelta(rawLook);
        playerMovementBehavior.UpdateMoveDelta(smoothMove);
        
        RaycastHit hit;
        if (playerVisualBehavior.HitTest(out hit))
        {
            // Debug.Log("Raycast hit: " + hit);
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
