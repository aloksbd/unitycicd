using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mousesensitivity = 10;
    float xRotation = 0f;
    float desiredX;

    private void Update()
    {
        Look();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mousesensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mousesensitivity;

        desiredX = transform.localRotation.eulerAngles.y + mouseX;

        xRotation -= mouseY;

        transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0f);
    }
}
