using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
public class ObjectHarnessElement
{
    private GameObject Parent;
    private GameObject Harness;
    public GameObject BoundingBox;
    private Bounds ObjectBoundary;
    private GameObject dragObject;
    private GameObject rotateCircle;
    private GameObject topLeftCube;
    private GameObject topRightCube;
    private GameObject bottomLeftCube;
    private GameObject bottomRightCube;
    private GameObject leftVertice;
    private GameObject rightVertice;
    private GameObject topVertice;
    private GameObject bottomVertice;
    private float resize_box_scale = 0.15f;
    private float vertice_scale = 0.05f;
    private float rotate_sphere_scale = 0.1f;
    public float SpaceAroundTheObject = 0.1f;
    public ObjectHarnessElement(GameObject parent, GameObject harness)
    {
        Parent = parent;
        Harness = harness;
        var renderer = parent.GetComponent<Renderer>();
        ObjectBoundary = renderer.bounds;

        // Attach the harness elements to the parent object
        // AttachBoundingBox();
        AttachDragHarness();
        AttachResizeHarness();
        // AttachRotateHarness();
    }

    private void AttachBoundingBox()
    {
        BoundingBox = new GameObject();
        LineRenderer boundingBoxLines = BoundingBox.AddComponent<LineRenderer>();
        boundingBoxLines.widthMultiplier = 0.05f;
        boundingBoxLines.positionCount = 5;
        boundingBoxLines.name = ObjectName.BOUNDING_BOX;

        boundingBoxLines.SetPosition(0, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(1, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(2, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y - ObjectBoundary.extents.y - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(3, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y - ObjectBoundary.extents.y - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(4, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));

        BoundingBox.transform.parent = Harness.transform;
    }

    private void RepositionBoundingBox()
    {
        var renderer = Parent.GetComponent<Renderer>();
        ObjectBoundary = renderer.bounds;

        LineRenderer boundingBoxLines = BoundingBox.GetComponent<LineRenderer>();
        boundingBoxLines.SetPosition(0, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(1, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(2, new Vector3(ObjectBoundary.center.x + ObjectBoundary.extents.x + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.y - ObjectBoundary.extents.y - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(3, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y - ObjectBoundary.extents.y - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.z));
        boundingBoxLines.SetPosition(4, new Vector3(ObjectBoundary.center.x - ObjectBoundary.extents.x - 0.01f - SpaceAroundTheObject, ObjectBoundary.center.y + ObjectBoundary.extents.y + 0.01f + SpaceAroundTheObject, ObjectBoundary.center.z));
    }

    // Attach the drag harness for moving of the object
    private void AttachDragHarness()
    {
        Debug.Log("Drag harness attached");
        dragObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dragObject.transform.position = new Vector3(ObjectBoundary.center.x, ObjectBoundary.center.y, ObjectBoundary.center.z);
        dragObject.transform.localScale = new Vector3(ObjectBoundary.extents.x * 2, ObjectBoundary.extents.y * 2, resize_box_scale);
        dragObject.GetComponent<Renderer>().material.color = Parent.GetComponent<Renderer>().material.color;
        dragObject.name = ObjectName.DRAG_HARNESS;
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
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        topLeftCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topLeftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        topLeftCube.transform.localScale = new Vector3(resize_box_scale, resize_box_scale, resize_box_scale);
        topLeftCube.name = ObjectName.TOP_LEFT_RESIZE_HARNESS;
        HarnessResizeManipulator topLeftResizeManipulator = new HarnessResizeManipulator(topLeftCube, Parent, ObjectName.TOP_LEFT_RESIZE_NAME);
        topLeftResizeManipulator.ParentResized += HandleObjectChanged;

        topRightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topRightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        topRightCube.transform.localScale = new Vector3(resize_box_scale, resize_box_scale, resize_box_scale);
        topRightCube.name = ObjectName.TOP_RIGHT_RESIZE_HARNESS;
        HarnessResizeManipulator topRightResizeManipulator = new HarnessResizeManipulator(topRightCube, Parent, ObjectName.TOP_RIGHT_RESIZE_NAME);
        topRightResizeManipulator.ParentResized += HandleObjectChanged;

        bottomLeftCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomLeftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);
        bottomLeftCube.transform.localScale = new Vector3(resize_box_scale, resize_box_scale, resize_box_scale);
        bottomLeftCube.name = ObjectName.BOTTOM_LEFT_RESIZE_HARNESS;
        HarnessResizeManipulator bottomLeftResizeManipulator = new HarnessResizeManipulator(bottomLeftCube, Parent, ObjectName.BOTTOM_LEFT_RESIZE_NAME);
        bottomLeftResizeManipulator.ParentResized += HandleObjectChanged;

        bottomRightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomRightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);
        bottomRightCube.transform.localScale = new Vector3(resize_box_scale, resize_box_scale, resize_box_scale);
        bottomRightCube.name = ObjectName.BOTTOM_RIGHT_RESIZE_HARNESS;
        HarnessResizeManipulator bottomRightResizeManipulator = new HarnessResizeManipulator(bottomRightCube, Parent, ObjectName.BOTTOM_RIGHT_RESIZE_NAME);
        bottomRightResizeManipulator.ParentResized += HandleObjectChanged;

        leftVertice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftVertice.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y, bounds.center.z);
        leftVertice.transform.localScale = new Vector3(vertice_scale, (bounds.extents.y + SpaceAroundTheObject) * 2, vertice_scale);
        leftVertice.name = ObjectName.LEFT_RESIZE_HARNESS;
        HarnessResizeManipulator leftResizeManipulator = new HarnessResizeManipulator(leftVertice, Parent, ObjectName.LEFT_RESIZE_NAME);
        bottomRightResizeManipulator.ParentResized += HandleObjectChanged;

        rightVertice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightVertice.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y, bounds.center.z);
        rightVertice.transform.localScale = new Vector3(vertice_scale, (bounds.extents.y + SpaceAroundTheObject) * 2, vertice_scale);
        rightVertice.name = ObjectName.RIGHT_RESIZE_HARNESS;
        HarnessResizeManipulator rightResizeManipulator = new HarnessResizeManipulator(rightVertice, Parent, ObjectName.RIGHT_RESIZE_NAME);
        rightResizeManipulator.ParentResized += HandleObjectChanged;

        topVertice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topVertice.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        topVertice.transform.localScale = new Vector3((bounds.extents.x + SpaceAroundTheObject) * 2, vertice_scale, vertice_scale);
        topVertice.name = ObjectName.TOP_RESIZE_HARNESS;
        HarnessResizeManipulator topResizeManipulator = new HarnessResizeManipulator(topVertice, Parent, ObjectName.TOP_RESIZE_NAME);
        topResizeManipulator.ParentResized += HandleObjectChanged;

        bottomVertice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomVertice.transform.position = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);
        bottomVertice.transform.localScale = new Vector3((bounds.extents.x + SpaceAroundTheObject) * 2, vertice_scale, vertice_scale);
        bottomVertice.name = ObjectName.BOTTOM_RESIZE_HARNESS;
        HarnessResizeManipulator bottomResizeManipulator = new HarnessResizeManipulator(bottomVertice, Parent, ObjectName.BOTTOM_RESIZE_NAME);
        bottomResizeManipulator.ParentResized += HandleObjectChanged;

        topLeftCube.transform.parent = Harness.transform;
        topRightCube.transform.parent = Harness.transform;
        bottomLeftCube.transform.parent = Harness.transform;
        bottomRightCube.transform.parent = Harness.transform;

        // Attach vertices to parent
        leftVertice.transform.parent = Harness.transform;
        rightVertice.transform.parent = Harness.transform;
        topVertice.transform.parent = Harness.transform;
        bottomVertice.transform.parent = Harness.transform;
    }

    private void RepositionResizeHarness()
    {
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        topLeftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        topRightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        bottomLeftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);
        bottomRightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);

        topLeftCube.transform.localScale = new Vector3(resize_box_scale / Parent.transform.localScale.x, resize_box_scale / Parent.transform.localScale.y, resize_box_scale / Parent.transform.localScale.x);
        topRightCube.transform.localScale = new Vector3(resize_box_scale / Parent.transform.localScale.x, resize_box_scale / Parent.transform.localScale.y, resize_box_scale / Parent.transform.localScale.x);
        bottomLeftCube.transform.localScale = new Vector3(resize_box_scale / Parent.transform.localScale.x, resize_box_scale / Parent.transform.localScale.y, resize_box_scale / Parent.transform.localScale.x);
        bottomRightCube.transform.localScale = new Vector3(resize_box_scale / Parent.transform.localScale.x, resize_box_scale / Parent.transform.localScale.y, resize_box_scale / Parent.transform.localScale.x);

        // Make operations for the vertices
        leftVertice.transform.position = new Vector3(bounds.center.x - bounds.extents.x - SpaceAroundTheObject, bounds.center.y, bounds.center.z);
        leftVertice.transform.localScale = new Vector3(vertice_scale / Parent.transform.localScale.x, ((bounds.extents.y + SpaceAroundTheObject) * 2) / Parent.transform.localScale.y, vertice_scale);

        rightVertice.transform.position = new Vector3(bounds.center.x + bounds.extents.x + SpaceAroundTheObject, bounds.center.y, bounds.center.z);
        rightVertice.transform.localScale = new Vector3(vertice_scale / Parent.transform.localScale.x, ((bounds.extents.y + SpaceAroundTheObject) * 2) / Parent.transform.localScale.y, vertice_scale);

        topVertice.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y + SpaceAroundTheObject, bounds.center.z);
        topVertice.transform.localScale = new Vector3(((bounds.extents.x + SpaceAroundTheObject) * 2) / Parent.transform.localScale.x, vertice_scale / Parent.transform.localScale.y, vertice_scale);

        bottomVertice.transform.position = new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y - SpaceAroundTheObject, bounds.center.z);
        bottomVertice.transform.localScale = new Vector3(((bounds.extents.x + SpaceAroundTheObject) * 2) / Parent.transform.localScale.x, vertice_scale / Parent.transform.localScale.y, vertice_scale);
    }

    // Attach rotate harness// Attach rotate harness
    private void AttachRotateHarness()
    {
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        rotateCircle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rotateCircle.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y + 0.2f, bounds.center.z);
        rotateCircle.transform.localScale = new Vector3(rotate_sphere_scale, rotate_sphere_scale, rotate_sphere_scale);
        rotateCircle.name = ObjectName.ROTATE_HARNESS;

        rotateCircle.transform.parent = Harness.transform;
    }

    // Reposition the rotate harness
    private void RepositionRotateHarness()
    {
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;
        rotateCircle.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y + 0.2f, bounds.center.z);
    }

    private void HandleObjectChanged()
    {
        RepositionMoveHarness();
        // RepositionBoundingBox();
        RepositionResizeHarness();
        // RepositionRotateHarness();
    }
}
