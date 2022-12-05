using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.EventSystems;
public class WallHarnessElement
{
    private GameObject Parent;
    private GameObject Harness;
    private GameObject BoundingBox;
    private Bounds WallBoundary;
    private GameObject dragObject;
    private GameObject rotateCircle;
    private GameObject leftCube;
    private GameObject rightCube;
    private HarnessOptions harnessOptions;
    private float resize_box_scale = 0.05f;
    private float rotate_sphere_scale = 0.1f;
    public WallHarnessElement(GameObject parent, GameObject harness)
    {
        Parent = parent;
        Harness = harness;
        var renderer = Parent.GetComponent<Renderer>();
        WallBoundary = renderer.bounds;

        harnessOptions = new HarnessOptions();

        // Attach the harness elements to the parent object
        // AttachBoundingBox();
        AttachDragHarness();
        AttachResizeHarness();
        AttachOptions();
    }

    // Attach the bounding box for the wall
    private void AttachBoundingBox()
    {
        BoundingBox = SceneObject.Create(SceneObject.Mode.Creator, ObjectName.BOUNDING_BOX);
        // BoundingBox = new GameObject();
        LineRenderer boundingBoxLines = BoundingBox.AddComponent<LineRenderer>();
        LineRenderer parentWall = Parent.GetComponent<LineRenderer>();
        // set values for the bounding box
        var boundaryColor = new Color(0, 0, 0.9f, 1.0f);
        boundingBoxLines.widthMultiplier = parentWall.widthMultiplier;
        boundingBoxLines.name = ObjectName.BOUNDING_BOX;
        boundingBoxLines.startColor = boundaryColor;
        boundingBoxLines.endColor = boundaryColor;

        // Position the bounding box using the bounds center and extents
        boundingBoxLines.SetPosition(0, new Vector3(parentWall.GetPosition(0).x - 0.01f, parentWall.GetPosition(0).y, parentWall.GetPosition(0).z - 0.01f));
        boundingBoxLines.SetPosition(1, new Vector3(parentWall.GetPosition(1).x + 0.01f, parentWall.GetPosition(1).y, parentWall.GetPosition(1).z - 0.01f));

        BoundingBox.transform.parent = Harness.transform;

    }

    // Reposition the bounding box after the change
    private void RepositionBoundingBox()
    {
        var renderer = Parent.GetComponent<Renderer>();
        LineRenderer parentWall = Parent.GetComponent<LineRenderer>();
        WallBoundary = renderer.bounds;

        LineRenderer boundingBoxLines = BoundingBox.GetComponent<LineRenderer>();
        boundingBoxLines.SetPosition(0, new Vector3(parentWall.GetPosition(0).x - 0.01f, parentWall.GetPosition(0).y, parentWall.GetPosition(0).z - 0.01f));
        boundingBoxLines.SetPosition(1, new Vector3(parentWall.GetPosition(1).x + 0.01f, parentWall.GetPosition(1).y, parentWall.GetPosition(1).z - 0.01f));
    }

    // Attach the drag harness for moving of the object
    private void AttachDragHarness()
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        var renderer = Parent.GetComponent<Renderer>();
        WallBoundary = renderer.bounds;
        dragObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dragObject.transform.position = new Vector3(WallBoundary.center.x, WallBoundary.center.y, WallBoundary.center.z);
        dragObject.transform.localScale = new Vector3(WallBoundary.extents.x * 2 - line2d.widthMultiplier, WallBoundary.extents.y * 2, resize_box_scale);
        dragObject.GetComponent<Renderer>().material.color = Parent.GetComponent<Renderer>().material.color;
        dragObject.name = ObjectName.DRAG_HARNESS;
        dragObject.transform.parent = Harness.transform;

        HarnessDragManipulator drag_manipulator = new HarnessDragManipulator(dragObject, Parent);
        drag_manipulator.ObjectDragged += HandleObjectChanged;
    }

    public void RepositionMoveHarness()
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;
        dragObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        dragObject.transform.localScale = new Vector3(bounds.extents.x * 2 - line2d.widthMultiplier, bounds.extents.y * 2, resize_box_scale);
    }

    // Attach resize harness
    private void AttachResizeHarness()
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        leftCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y, bounds.center.z);
        leftCube.transform.localScale = new Vector3(line2d.widthMultiplier + 0.05f, line2d.widthMultiplier + 0.05f, resize_box_scale);
        leftCube.name = ObjectName.LEFT_RESIZE_HARNESS;
        HarnessResizeManipulator leftResizeManipulator = new HarnessResizeManipulator(leftCube, Parent, ObjectName.LEFT_RESIZE_NAME);
        leftResizeManipulator.ParentResized += HandleObjectChanged;

        rightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y, bounds.center.z);
        rightCube.transform.localScale = new Vector3(line2d.widthMultiplier + 0.05f, line2d.widthMultiplier + 0.05f, resize_box_scale);
        rightCube.name = ObjectName.RIGHT_RESIZE_HARNESS;
        HarnessResizeManipulator rightResizeManipulator = new HarnessResizeManipulator(rightCube, Parent, ObjectName.RIGHT_RESIZE_NAME);
        rightResizeManipulator.ParentResized += HandleObjectChanged;

        leftCube.transform.parent = Harness.transform;
        rightCube.transform.parent = Harness.transform;
    }

    private void RepositionResizeHarness()
    {
        LineRenderer line2d = Parent.GetComponent<LineRenderer>();
        var renderer = Parent.GetComponent<Renderer>();
        var bounds = renderer.bounds;

        leftCube.transform.position = new Vector3(bounds.center.x - bounds.extents.x, bounds.center.y, bounds.center.z);
        rightCube.transform.position = new Vector3(bounds.center.x + bounds.extents.x, bounds.center.y, bounds.center.z);

        leftCube.transform.localScale = new Vector3(line2d.widthMultiplier + 0.05f, line2d.widthMultiplier + 0.05f, resize_box_scale);
        rightCube.transform.localScale = new Vector3(line2d.widthMultiplier + 0.05f, line2d.widthMultiplier + 0.05f, resize_box_scale);
    }

    private void HandleObjectChanged()
    {
        RepositionMoveHarness();
        // RepositionBoundingBox();
        RepositionResizeHarness();
    }

    private void AttachOptions()
    {
        Debug.Log("Mouse position " + Input.mousePosition);
        Debug.Log("Parent position " + Camera.main.WorldToScreenPoint(Parent.transform.position));

        var renderer = Parent.GetComponent<Renderer>();
        WallBoundary = renderer.bounds;

        SelectedHarness harnessOptions = new SelectedHarness();
        VisualElement windowRoot = CreatorUIController.getRoot();
        windowRoot.Add(harnessOptions);
        Debug.Log("Position " + harnessOptions.style.position);

        Vector3 center = WallBoundary.center;
        Vector3 extents = WallBoundary.extents;

        Vector3 ElementPosition = new Vector3(center.x + extents.x + 0.1f, center.y + extents.y + 0.1f, 0.0f);
        Vector3 worldPosition = Camera.main.WorldToScreenPoint(ElementPosition);
        Debug.Log("World element position " + worldPosition);

        harnessOptions.style.position = Position.Absolute;
        harnessOptions.style.top = worldPosition.y;
        harnessOptions.style.left = worldPosition.x;

        Debug.Log("Harness position top  " + harnessOptions.style.top);
        Debug.Log("Harness position left  " + harnessOptions.style.left);

        harnessOptions.leftFlipped += leftFlip;
        harnessOptions.topFlipped += topFlip;
        harnessOptions.Copied += copy;
        harnessOptions.Deleted += delete;
    }

    private void leftFlip()
    {
        harnessOptions.flipHorizontally(Parent);
        LineRenderer parentWall = Parent.GetComponent<LineRenderer>();
        Vector3 position0 = parentWall.GetPosition(0);
        Vector3 position1 = parentWall.GetPosition(1);
        parentWall.SetPosition(0, position1);
        parentWall.SetPosition(1, position0);
    }

    private void topFlip()
    {
        harnessOptions.flipVertically(Parent);
    }

    private void copy()
    {
        harnessOptions.copy(Parent);
    }

    private void delete()
    {
        harnessOptions.delete(Parent);
    }
}
