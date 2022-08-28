using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]

public class PlayerMovementBehaviour : MonoBehaviour
{
    private const float DOUBLE_BUTTON_TIME = 0.750f;

    //  For Unity's Insprectr Panel:
    [Header("Movement Settings")]
    public float MoveSpeed = 20.0f;
    public float LookSpeed = 15.0f;
    public float DoublePressTime = DOUBLE_BUTTON_TIME;
    public float MinVerticalLook = -90.0f; // degrees (-90.0f = straight down)
    public float MaxVerticalLook =  60.0f; // degrees ( 60.0f = straight up)

    //  Internal state
    private CharacterController     characterController;
    private PlayerController.IAMode interactionMode;

    private bool    enableMouseLook;
    private bool    enableGravity;

    private const float GRAVITY = -15.0f;
    private float   gravity;

    private double  jumpBtnDown = 0.0d;   // Time when jump button was first pressed.
    private bool    jumping = false;

    private Vector3 moveDelta;
    private Vector2 lookDelta;
    private float   vertRotation;
    private float   horzRotation;

    //  Unity Input System's first mouse Look/Axis reports are flaky. 
    //  We'll defer mouse look updates for a short time interval:
    private const float LOOK_DEFER_START = 0.5f; // in seconds.
    private float   lookCountdown = LOOK_DEFER_START; 

    public void SetupBehavior(PlayerController.IAMode mode)
    {
        //  FYI: SetupBehavior() method may be invoked multiple times over 
        //  this component's lifetime.
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            throw new Exception("Player CharacterController component not found");
        }

        vertRotation = transform.rotation.y;
        horzRotation = 180.0f; //transform.rotation.x; // TODO: How is this not initialized to the Inspector's value?

        SetInteractionMode(mode);
    }

    public void UpdateMoveDelta(Vector3 delta)
    {
        moveDelta = -delta;
    }

    public void UpdateLookDelta(Vector2 delta)
    {
        if (enableMouseLook)
        {
            if (lookCountdown <= 0.0f)
            {
                lookDelta = delta;
            }
            else
            {
                lookCountdown -= Time.deltaTime;
            }
        }
    }

    private void SetInteractionMode(PlayerController.IAMode mode)
    {
        ResetJumpButtonState();

        switch (mode)
        {
            case PlayerController.IAMode.Minecraft:
                EnableMouseLook(true);
                EnableGravity(true);
                break;

            case PlayerController.IAMode.MinecraftFlyAlways:
                EnableMouseLook(true);
                EnableGravity(false);
                break;

            case PlayerController.IAMode.Pointing:
                EnableMouseLook(false);
                EnableGravity(false);
                break;

            default:
                throw new Exception("Invalid Interaction Mode: " + interactionMode);
        }
        
        interactionMode = mode;
    }

    void EnableMouseLook(bool enable)
    {
        //  Enable/disable rotation physics on player's Rigidbody, if it exists.
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.freezeRotation = enable;
        }

        lookCountdown = LOOK_DEFER_START;
        enableMouseLook = enable;
    }

    void EnableGravity(bool enable)
    {        
        enableGravity = enable;  
        gravity = enable ? GRAVITY : 0.0f;      
    }

    public void OnJumpButton(InputAction.CallbackContext value)
    {
        if (PlayerController.IAMode.Minecraft == interactionMode)
        {
            if (TryJumpButtonTime(value.time))
            {
                if (InputActionPhase.Started == value.action.phase)
                {
                    Debug.Log((enableGravity ? "Stopping " : "Starting ") + "Gravity");
                    EnableGravity(!enableGravity);
                    ResetJumpButtonState();
                }
            }
            else if (InputActionPhase.Started == value.action.phase)
            {
                Debug.Log("Starting jump timer at " + value.time);
                jumpBtnDown = value.time;
            }
        }
    }

    bool TryJumpButtonTime(double timeNow)
    {
        if (jumpBtnDown != 0.0f)
        {
            if ((timeNow - jumpBtnDown) < DoublePressTime)
            {
                return true;
            }
            ResetJumpButtonState();
        }
        return false;
    }

    void ResetJumpButtonState()
    {
        jumpBtnDown = 0.0f;
    }

    void FixedUpdate()
    {
        MoveThePlayer();
        TurnThePlayer();
        TryJumpButtonTime(Time.timeAsDouble);
    }

    void MoveThePlayer()
    {
        Vector3 movement = moveDelta * MoveSpeed * Time.deltaTime;
        movement = transform.TransformDirection(movement);
        movement.y += (gravity * Time.deltaTime);        
        characterController.Move(movement);

        //Debug.Log("movement: " + movement);
    }
    
    void TurnThePlayer()
    {
        horzRotation += (lookDelta.x * LookSpeed * Time.deltaTime);
        vertRotation += (lookDelta.y * LookSpeed * Time.deltaTime);
        vertRotation = Math.Clamp(vertRotation, MinVerticalLook, MaxVerticalLook);

        transform.localEulerAngles = new Vector3(
            vertRotation, 
            horzRotation,
            0);
    }
}
