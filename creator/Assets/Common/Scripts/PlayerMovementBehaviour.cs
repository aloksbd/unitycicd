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
    [Header("Basic Movement Settings")][Space(5)]
    public float GroundSpeed = 10.0f;
    public float FlySpeed = 75.0f;
    public float LookSpeed = 15.0f;
    public float DoublePressTime = DOUBLE_BUTTON_TIME;
    public float MinVerticalLook = -90.0f; // degrees (-90.0f = straight down)
    public float MaxVerticalLook =  60.0f; // degrees ( 60.0f = straight up)

    public enum VerticalBoundsType
    {
        None,            // no movement limitations
        Relative,  // movement is limited to a height relative to a start position (BoundsOrigin property)
        Absolute   // movement is limited to an absolute height. 
    };
    public enum HorizontalBoundsType
    {
        None,    // no movement limitations
        Radius,  // movement is limited to a circular radius relative to a start position (BoundsOrigin property)
    };

    [Header("Movement Constraints")][Space(5)]
    public VerticalBoundsType verticalBoundsType = VerticalBoundsType.None;
    public float VerticalBoundsDistance = 0f;
    public HorizontalBoundsType horizontalBoundsType = HorizontalBoundsType.None;
    public float HorizontalBoundsDistance = 0f;
    private Vector3 _boundsOrigin;

    //  Internal state
    private CharacterController     _characterController;
    private PlayerController.IAMode _interactionMode;

    private bool    _enableMouseLook;
    private bool    _enableGravity;

    private const float GRAVITY = -20.0f;
    private float   _gravity;

    private double  _jumpBtnDown = 0.0d;   // Time when jump button was first pressed.
    private bool    _jumping = false;

    [HideInInspector()]
    public bool     inputEnabled = true;

    private Vector3 _moveDelta;
    private Vector2 _lookDelta;
    private float   _vertRotation;
    private float   _horzRotation;

    //  Unity Input System's first mouse Look/Axis reports are flaky. 
    //  We'll defer mouse look updates for a short time interval:
    private const float LOOK_DEFER_START = 0.5f; // in seconds.
    private float   _lookCountdown = LOOK_DEFER_START;

    public void SetupBehavior(PlayerController.IAMode mode)
    {
        //  FYI: SetupBehavior() method may be invoked multiple times over 
        //  this component's lifetime.
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            throw new Exception("Player CharacterController component not found");
        }

        //_vertRotation = transform.rotation.y;
        //_horzRotation = 180.0f; //transform.rotation.x; // TODO: How is this not initialized to the Inspector's value?

        SetInteractionMode(mode);
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }

    public void UpdateMoveDelta(Vector3 delta)
    {
        _moveDelta = -delta;
    }

    public void UpdateLookDelta(Vector2 delta)
    {
        if (inputEnabled && _enableMouseLook)
        {
            if (_lookCountdown <= 0.0f)
            {
                _lookDelta = delta;
            }
            else
            {
                _lookCountdown -= Time.deltaTime;
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
                EnableMouseLook(false);      //Modified by Aman -> should be false
                EnableGravity(false);        //Modified by Aman -> should be false
                break;

            default:
                throw new Exception("Invalid Interaction Mode: " + _interactionMode);
        }
        
        _interactionMode = mode;
    }

    void EnableMouseLook(bool enable)
    {
        //  Enable/disable rotation physics on player's Rigidbody, if it exists.
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.freezeRotation = enable;
        }

        _lookCountdown = LOOK_DEFER_START;
        _enableMouseLook = enable;
    }

    void EnableGravity(bool enable)
    {        
        _enableGravity = enable;  
        _gravity = enable ? GRAVITY : 0.0f;      
    }

    public void OnJumpButton(InputAction.CallbackContext value)
    {
        if (inputEnabled)
        {
            if (PlayerController.IAMode.Minecraft == _interactionMode)
            {
                if (TryJumpButtonTime(value.time))
                {
                    if (InputActionPhase.Started == value.action.phase)
                    {
                        Debug.Log((_enableGravity ? "Stopping " : "Starting ") + "Gravity");
                        EnableGravity(!_enableGravity);
                        ResetJumpButtonState();
                    }
                }
                else if (InputActionPhase.Started == value.action.phase)
                {
                    Debug.Log("Starting jump timer at " + value.time);
                    _jumpBtnDown = value.time;
                }
            }
        }
    }

    bool TryJumpButtonTime(double timeNow)
    {
        if (inputEnabled)
        {
            if (_jumpBtnDown != 0.0f)
            {
                if ((timeNow - _jumpBtnDown) < DoublePressTime)
                {
                    return true;
                }
                ResetJumpButtonState();
            }
        }
        return false;
    }

    void ResetJumpButtonState()
    {
        _jumpBtnDown = 0.0f;
    }

    void FixedUpdate()
    {
        MoveThePlayer();
        TurnThePlayer();
        TryJumpButtonTime(Time.timeAsDouble);
    }

    [HideInInspector()]
    public Vector3 BoundsOrigin
    {
        get { return _boundsOrigin; }
        set { _boundsOrigin = value; }
    }

    private Vector3 AbsolutePosition
    {
        get
        {
            FloatingOriginAdvanced floatingOrigin = GetComponent<FloatingOriginAdvanced>();
            if (floatingOrigin && floatingOrigin.enabled)
            {
                return floatingOrigin.absolutePosition;
            }
            return transform.position;
        }
    }

    void BoundsCheck(ref Vector3 movement)
    {
        Vector3 absolutPos = AbsolutePosition;

        if (VerticalBoundsDistance > 0 &&
            verticalBoundsType != VerticalBoundsType.None)
        {
            float deltaY = 0f;

            if (verticalBoundsType == VerticalBoundsType.Relative)
            {
                deltaY = (movement.y + absolutPos.y) - (VerticalBoundsDistance + _boundsOrigin.y);
            }
            else if (verticalBoundsType == VerticalBoundsType.Absolute)
            {
                deltaY = (movement.y + absolutPos.y) - VerticalBoundsDistance;
            }

            if (deltaY > 0)
            {
                Trace.Log(null, "Exceeding vertical bounds by {0}", deltaY);
                movement.y -= deltaY;
            }
        }

        if (HorizontalBoundsDistance > 0 &&
            horizontalBoundsType == HorizontalBoundsType.Radius)
        {
            float moveSq = (movement.x * movement.x) + (movement.z * movement.z);
            float aposSq = (absolutPos.x * absolutPos.x) + (absolutPos.z * absolutPos.z);
            float distSq = HorizontalBoundsDistance * HorizontalBoundsDistance;
            float origSq = (_boundsOrigin.x * _boundsOrigin.x) + (_boundsOrigin.z * _boundsOrigin.z);
            float deltaSq = (moveSq + aposSq) - (distSq + origSq);

            if (deltaSq > 0)
            {
                float ratio = (float)(Math.Sqrt(distSq + origSq) / Math.Sqrt(moveSq + aposSq));
                Trace.Assert(ratio < 1.0f, "Horizontal limit/traveled ratio < 1.0f");
                Trace.Log(null, "Exceeding horizontal bounds by {0}, reducing travel distance x, z by {1}.", Math.Sqrt(deltaSq), ratio);
                movement.x = (ratio * (movement.x + absolutPos.x)) - absolutPos.x;
                movement.z = (ratio * (movement.z + absolutPos.z)) - absolutPos.z;
            }
        }
    }

    void MoveThePlayer()
    {
        if (inputEnabled)
        {
            float speed = (_interactionMode == PlayerController.IAMode.Minecraft) ?
                GroundSpeed : 
                FlySpeed;

            Vector3 movement = _moveDelta * speed * Time.deltaTime;
            movement = transform.TransformDirection(movement);
            movement.y += (_gravity * Time.deltaTime);

            BoundsCheck(ref movement);

            //  IMPORTANT:
            //  We use a floating origin in order to allow player navigation through a 
            //  very large world (See FloatingOriginAdvanced.cs).  The floating origin works as
            //  follows: whenever the player position exceeds a predefined distance
            //  from the current world origin at (0,0,0), the transforms of every top-level GameObject in 
            //  the scene are offset by the player's position before resetting the player's position
            //  to (0,0,0).  The world origin is therefore periodically redefined as the player's position
            //  relative to the objects in the scene.
            //
            //  There's a catch. Unity's CharacterController maintains its own internal physics
            //  transform independent of, and not synchonized by default with, the transform of its host
            //  GameObject. So a call to CharacterController::Move() subsequent to a floating origin
            //  reset uses its internal transform rather than the GameObject's revised value of (0,0,0).
            //  The result of the call to Move() is an effective teleport of the player by a distance
            //  at least as far as the predefined floating origin max distance. (See
            //  https://issuetracker.unity3d.com/issues/charactercontroller-overrides-objects-position-when-teleporting-with-transform-dot-position)
            //  
            //  There are two ways to solve this:
            //  1. Enable Physics -> 'Auto Sync Transforms' in Project Preferences
            //  2. Make a just-in-time call to Physics.SyncTransforms() prior to an operation that should
            //     use the GameObject transform.
            //
            //  We chose to go with #2 to avoid a performance hit in intercepting and synchronizing all transform 
            //  changes taking place throughout the scene. If we see bad behavior caused by unsynchronized
            //  transforms crop up again elsewhere, we should reconsider going with #1.

            Physics.SyncTransforms(); 
            _characterController.Move(movement);

            //Debug.Log("movement: " + movement);
        }
    }
    
    void TurnThePlayer()
    {
        if (inputEnabled)
        {
            _horzRotation += (_lookDelta.x * LookSpeed * Time.deltaTime);
            _vertRotation += (_lookDelta.y * LookSpeed * Time.deltaTime);
            _vertRotation = Math.Clamp(_vertRotation, MinVerticalLook, MaxVerticalLook);

            transform.localEulerAngles = (SceneObject.Get().ActiveMode == SceneObject.Mode.Elevator)? Vector3.zero : new Vector3(
                _vertRotation,
                _horzRotation,
                0);
        }
    }
}
