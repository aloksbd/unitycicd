using UnityEngine;

//  Attach a SceneMode component to a top-level gameobject that 
//  serves as an exclusive scene mode, of which only one (or none) may be
//  active at any given time.
//
//  Attaching a SceneMode component allows its GameObject to be discoverable
//  even if inactive

public class SceneMode : MonoBehaviour
{
    //  Inspector properties
    public PlayerController.IAMode InteractionMode;

    //  Private members
    private SceneObject.Mode mode = SceneObject.Mode.INVALID;

    public SceneObject.Mode Mode
    {
        get { return mode; }
        set 
        {
            Trace.Assert((this.mode == SceneObject.Mode.INVALID) || (this.mode == value),
                "The SceneMode's mode value cannot be changed once assigned.");

            Trace.Assert(value != SceneObject.Mode.INVALID, 
                "The SceneMode is being assigned an invalid mode value");

            this.mode = value;
        }
    }

    public bool RequiresPlayerController()
    {
        return InteractionMode == PlayerController.IAMode.Minecraft ||
               InteractionMode == PlayerController.IAMode.MinecraftFlyAlways;
    }

}

