using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerVisualBehaviour : MonoBehaviour
{
    private Camera playerCamera;

    private Image  crosshairs;
    private Image  crosshairs_hit;
    private Vector3 pointer;
    private PlayerController.IAMode interactiveMode;

    // Start is called before the first frame update
    public void SetupBehavior(PlayerController.IAMode mode)
    {
        if (SceneObject.ActiveCamera != null)
        {
            playerCamera = SceneObject.ActiveCamera.GetComponent<Camera>();
            Trace.Assert(playerCamera != null, "Null camera");

            //  Show centered crosshairs in Minecraft modes
            crosshairs = GetImage(ObjectName.CROSSHAIRS_IMAGE);
            Trace.Assert(crosshairs != null, "Null Unity.UI.Image at {0}",
                ObjectName.PLAYER_MODE + "/" + ObjectName.CROSSHAIRS_IMAGE);

            crosshairs_hit = GetImage(ObjectName.CROSSHAIRS_HIT_IMAGE);
            Trace.Assert(crosshairs_hit != null, "Null Unity.UI.Image at {0}",
                ObjectName.PLAYER_MODE + "/" + ObjectName.CROSSHAIRS_HIT_IMAGE);

            crosshairs.enabled = (mode != PlayerController.IAMode.Pointing);
            crosshairs_hit.enabled = (mode != PlayerController.IAMode.Pointing);

            //  Unlock and show cursor for Pointing mode
            //  Lock and hide cursor for Minecraft modes
            switch (mode)
            {
                case PlayerController.IAMode.Pointing:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case PlayerController.IAMode.Minecraft:
                case PlayerController.IAMode.MinecraftFlyAlways:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                default:
                    Trace.Assert(false, "Undefined cursor behavior for interaction mode {0} ", mode);
                    break;
            }

            pointer = new Vector3(
                playerCamera.pixelWidth / 2,
                playerCamera.pixelHeight / 2,
                0);
        }

        interactiveMode = mode;
    }

    private Image GetImage(string name)
    {
        GameObject ob = SceneObject.Find(SceneObject.Mode.Player, name);

        return (ob != null) ?
            ob.GetComponent<Image>() :
            null;
    }

    public bool HitTest(out RaycastHit hitOut)
    {
        if (playerCamera != null)
        {
            if (interactiveMode == PlayerController.IAMode.Pointing)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                pointer.Set(mousePos.x, mousePos.y, 0);
            }

            Ray ray = playerCamera.ScreenPointToRay(pointer);

            bool hit = Physics.Raycast(ray, out hitOut);
            crosshairs_hit.enabled = ((interactiveMode != PlayerController.IAMode.Pointing) && hit);
            return hit;
        }
        hitOut = new RaycastHit();
        return false;
    }
}
