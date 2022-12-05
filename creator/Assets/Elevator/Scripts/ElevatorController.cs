using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public static bool IsElevator(GameObject gameObject)
    {
        //  Do a name match for the container or its first-level child gameObjects:
        return (gameObject.transform.parent != null &&
                gameObject.transform.parent.name == ObjectName.ELEVATOR_CONTAINER) ||
                gameObject.name == ObjectName.ELEVATOR_CONTAINER;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneObject.Get().ActiveMode = SceneObject.Mode.Welcome;
        }
    }
}
