using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
public class SpriteHarnessElement
{
    private GameObject Parent;
    private GameObject Harness;
    private GameObject BoundingBox;
    private Bounds ObjectBoundary;
    private GameObject dragObject;
    private GameObject rotateCircle;
    private GameObject leftCube;
    private GameObject rightCube;
    private float resize_box_scale = 0.05f;
    private float rotate_sphere_scale = 0.1f;
    public float SpaceAroundTheObject = 0.1f;
    public SpriteHarnessElement(GameObject parent, GameObject harness)
    {
        Parent = parent;
        Harness = harness;
        var renderer = parent.GetComponent<Renderer>();
        ObjectBoundary = renderer.bounds;

        // Attach the harness elements to the parent object
        AttachBoundingBox();
        // AttachResizeHarness();
        AttachDragHarness();
    }

    // Attach the bounding box for the wall
    private void AttachBoundingBox()
    {
        BoundingBox = new GameObject();
        LineRenderer boundingBoxLines = BoundingBox.AddComponent<LineRenderer>();
        SpriteRenderer parentWall = Parent.GetComponent<SpriteRenderer>();
        BoxCollider boxCollider = Parent.GetComponent<BoxCollider>();
        boundingBoxLines.widthMultiplier = WHConstants.DefaultWindow2DHeight;
        // set values for the bounding box
        var boundaryColor = new Color(0, 0, 0.9f, 1.0f);

        boundingBoxLines.name = "Boundary box";
        boundingBoxLines.startColor = boundaryColor;
        boundingBoxLines.endColor = boundaryColor;

        boundingBoxLines.SetPosition(0, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(1, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y, ObjectBoundary.center.z));

        BoundingBox.transform.parent = Harness.transform;

    }

    // Reposition the bounding box after the change
    private void RepositionBoundingBox()
    {
        var renderer = Parent.GetComponent<Renderer>();
        SpriteRenderer parentWall = Parent.GetComponent<SpriteRenderer>();
        ObjectBoundary = renderer.bounds;

        LineRenderer boundingBoxLines = BoundingBox.GetComponent<LineRenderer>();
        boundingBoxLines.SetPosition(0, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(1, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
    }

    // Attach the drag harness for moving of the object
    private void AttachDragHarness()
    {
        SpriteRenderer line2d = Parent.GetComponent<SpriteRenderer>();
        BoxCollider boxCollider = Parent.GetComponent<BoxCollider>();
        var renderer = Parent.GetComponent<Renderer>();
        ObjectBoundary = renderer.bounds;
        dragObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dragObject.transform.position = new Vector3(ObjectBoundary.center.x, ObjectBoundary.center.y, ObjectBoundary.center.z);
        dragObject.GetComponent<Renderer>().material.color = Color.yellow;
        dragObject.name = "Drag harness button";
        HarnessDragManipulator drag_manipulator = new HarnessDragManipulator(dragObject, Parent);
        drag_manipulator.ObjectDragged += HandleObjectChanged;

        dragObject.transform.parent = Harness.transform;
    }

    public void RepositionMoveHarness()
    {
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;
        dragObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
    }

    // Attach resize harness
    private void AttachResizeHarness()
    {
        SpriteRenderer line2d = Parent.GetComponent<SpriteRenderer>();
        BoxCollider boxCollider = Parent.GetComponent<BoxCollider>();
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        leftCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y, bounds.center.z);
        leftCube.transform.localScale = new Vector3(boxCollider.size.y + 0.05f, boxCollider.size.y + 0.05f, resize_box_scale);
        leftCube.name = "Left resize harness";
        HarnessResizeManipulator leftResizeManipulator = new HarnessResizeManipulator(leftCube, Parent, "left");
        leftResizeManipulator.ParentResized += HandleObjectChanged;

        rightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y, bounds.center.z);
        rightCube.transform.localScale = new Vector3(boxCollider.size.y + 0.05f, boxCollider.size.y + 0.05f, resize_box_scale);
        rightCube.name = "Right resize harness";
        HarnessResizeManipulator rightResizeManipulator = new HarnessResizeManipulator(rightCube, Parent, "right");
        rightResizeManipulator.ParentResized += HandleObjectChanged;

        leftCube.transform.parent = Harness.transform;
        rightCube.transform.parent = Harness.transform;
    }

    private void RepositionResizeHarness()
    {
        SpriteRenderer line2d = Parent.GetComponent<SpriteRenderer>();
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        leftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y, bounds.center.z);
        rightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y, bounds.center.z);
    }

    private void HandleObjectChanged()
    {
        RepositionMoveHarness();
        RepositionBoundingBox();
        RepositionResizeHarness();
    }
}