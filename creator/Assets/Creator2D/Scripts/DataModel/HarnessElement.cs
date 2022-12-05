using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
public class HarnessElement
{
    private GameObject parent;
    public GameObject Harness;
    public GameObject boundaryContainer;
    public GameObject dragObject;
    public GameObject rotateCircle;
    public GameObject leftCube;
    public GameObject rightCube;
    public GameObject bottomCube;
    public GameObject topCube;


    public int lengthOfLineRenderer = 6;
    public float resize_box_scale = 0.05f;
    public float rotate_sphere_scale = 0.1f;
    public float SpaceAroundTheObject = 0.1f;

    public HarnessElement(CreatorItem item)
    {
        GameObject selectedGameObject = item.gameObject;

        Harness = SceneObject.Create(SceneObject.Mode.Creator, selectedGameObject.name + ObjectName.HARNESS_ELEMENT);
        Harness.transform.position = selectedGameObject.transform.position;
        Harness.transform.parent = selectedGameObject.transform;

        parent = selectedGameObject;

        LineRenderer WallComponent = selectedGameObject.GetComponent<LineRenderer>();

        SpriteRenderer SpriteComponent = selectedGameObject.GetComponent<SpriteRenderer>();

        if (WallComponent != null)
        {
            WallHarnessElement wallHarness = new WallHarnessElement(selectedGameObject, Harness);
        }
        else if (SpriteComponent != null)
        {
            SpriteHarnessElement spriteHarness = new SpriteHarnessElement(selectedGameObject, Harness);
        }
        else
        {
            ObjectHarnessElement objectHarness = new ObjectHarnessElement(selectedGameObject, Harness);
        }
    }
}