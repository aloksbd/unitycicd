using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerVisualBehaviour : MonoBehaviour
{
    private Camera playerCamera;

    private const string CROSSHAIRS_IMAGE_NAME = "/Canvas/Crosshairs";
    private const string CROSSHAIRS_HIT_IMAGE_NAME = "/Canvas/Crosshairs_Hit";
    private Image  crosshairs;
    private Image  crosshairs_hit;
    private Vector3 pointer;
    PlayerController.IAMode interactiveMode;

    // Start is called before the first frame update
    public void SetupBehavior(PlayerController.IAMode mode)
    {
        playerCamera = (Camera)FindObjectOfType(typeof(Camera));
        if (playerCamera == null)
        {
            throw new Exception("Player/Camera not found.");
        }

        //  Show centered crosshairs in Minecraft modes
        crosshairs = GetImage(CROSSHAIRS_IMAGE_NAME);
        if (crosshairs == null)
        {
            throw new Exception("Null Unity.UI.Image at " + CROSSHAIRS_IMAGE_NAME);
        }

        crosshairs_hit = GetImage(CROSSHAIRS_HIT_IMAGE_NAME);
        if (crosshairs == null)
        {
            throw new Exception("Null Unity.UI.Image at " + CROSSHAIRS_HIT_IMAGE_NAME);
        }

        crosshairs.enabled = (mode != PlayerController.IAMode.Pointing);
        crosshairs_hit.enabled = (mode != PlayerController.IAMode.Pointing);

        //  Hide cursor Minecraft modes
        Cursor.lockState = (mode == PlayerController.IAMode.Pointing) ?
            CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = (mode == PlayerController.IAMode.Pointing);

        pointer = new Vector3(
            playerCamera.pixelWidth/2,
            playerCamera.pixelHeight/2,
            0); 

        interactiveMode = mode;
    }

    private Image GetImage(string name)
    {
        GameObject ob = GameObject.Find(name);
        if (ob != null)
        {
            return ob.GetComponent<Image>();
        }
        return null;
    }

    public bool HitTest(out RaycastHit hitOut)
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
}
