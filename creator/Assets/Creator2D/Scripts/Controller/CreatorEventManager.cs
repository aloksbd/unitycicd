using System;
using UnityEngine;
using ObjectModel;
using UnityEngine.UIElements;

public class CreatorEventManager : MonoBehaviour
{
    private GameObject BuildingCanvas;
    private float MIN_SCALE = 4.0f; // zoom-in and zoom-out limits
    private static float MAX_SCALE = 20f;
    public float zoomSpeed = 30f;
    private Camera _camera;
    public static void SetMaxScale(float maxScale)
    {
        MAX_SCALE = maxScale;
    }
    BuildingInventoryController buildingInventoryController;

    void Start()
    {
        GameObject cam = SceneObject.GetCamera(SceneObject.Mode.Creator);
        _camera = cam.GetComponent<Camera>();
        BuildingCanvas = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.BUILDING_CANVAS);
    }

    private bool _lineRender = false;
    private bool _canvasDrag = false;
    private GameObject _lineRendererGO;
    private Vector3 _lineRenderStartPosition;
    private static Color _lineRendererColor = Color.blue;
    private string _Position_Label = "position-label";

    private bool _canDrop = false;

    // Update is called once per frame
    void Update()
    {
        buildingInventoryController = BuildingInventoryController.Get();
        Label positionLabel = CreatorUIController.getRoot().Q<Label>(_Position_Label);
        // UnityEngine.Cursor.SetCursor(currentBlock.BlockTexture, Vector2.zero, CursorMode.Auto);
        if (_IsMouseOverBuildingCanvas())
        {

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ConvertCoordinate.GeoPosition LatLong = ConvertCoordinate.WorldPositionToLatLon(new Vector2(worldPosition.x + BuildingCanvas.transform.position.x, worldPosition.y + BuildingCanvas.transform.position.y));
            positionLabel.text = "Position: " + LatLong.latitude + ", " + LatLong.longitude;
        }
        else
        {
            positionLabel.text = "";
        }

        _CanDrop();

        if (Math.Abs(Input.mouseScrollDelta.y) > 0f)
        {
            _ZoomBuildingCanvas(Input.mouseScrollDelta.y > 0f ? 1 : -1);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
            {
                _lineRender = false;
                _canvasDrag = false;
                if (buildingInventoryController.currentBlock && NewBuildingController.CurrentFloorPlan() != null)
                {
                    if (hitInfo.transform.gameObject == BuildingCanvas)
                    {
                        if (buildingInventoryController.currentBlock.CategoryName == "Build")
                        {
                            if (buildingInventoryController.currentBlock.AssetType == "Elevator")
                            {
                                //TODO: CreateItem Elevator
                            }
                            else if (buildingInventoryController.currentBlock.AssetType == "Wall")
                            {
                                _lineRender = true;
                                CreateLine(hitInfo);
                            }
                        }

                    }
                    else if (hitInfo.transform.tag == WHConstants.METABLOCK)
                    {
                        if (buildingInventoryController.currentBlock.AssetType == "Door")
                        {
                            if (_CheckCanDropOnWall(ray, hitInfo))
                            {
                                CreatorItem item = CreatorItemFinder.FindItemWithGameObject(hitInfo.transform.gameObject);
                                NewBuildingController.CreateDoor(item.name, hitInfo.point);
                            }
                        }
                        else if (buildingInventoryController.currentBlock.AssetType == "Window")
                        {
                            if (_CheckCanDropOnWall(ray, hitInfo))
                            {
                                CreatorItem item = CreatorItemFinder.FindItemWithGameObject(hitInfo.transform.gameObject);
                                NewBuildingController.CreateWindow(item.name, new Vector3(hitInfo.point.x, hitInfo.point.y, 0));
                            }
                        }
                    }

                }
                else if (hitInfo.transform.tag == WHConstants.METABLOCK)
                {
                    //TODO if there is a selected Object then do some other action
                    CreatorItem item = CreatorItemFinder.FindItemWithGameObject(hitInfo.transform.gameObject);
                    item.Select();
                }
                else
                {
                    buildingInventoryController.currentBlock = null;
                    if (hitInfo.transform.gameObject == BuildingCanvas && !CreatorUIController.isInputOverVisualElement())
                    {
                        _canvasDrag = true;
                    }
                }
            }
        }
        // else
        // {
        //     Trace.Log("CreatorEventManager null on GameObject{0}", gameObject.name);
        // }

        if (Input.GetMouseButton(0))
        {
            if (_lineRender)
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
                {
                    if (buildingInventoryController.currentBlock && hitInfo.transform.gameObject == BuildingCanvas)
                    {
                        if (buildingInventoryController.currentBlock.CategoryName == "Build")
                        {
                            if (buildingInventoryController.currentBlock.AssetType == "Wall")
                            {
                                UpdateLine(hitInfo);
                            }
                        }
                    }
                }
                else
                {
                    GenerateLine();
                }
            }
            else if (_canvasDrag)
            {
                _DragBuildingCanvas();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _canvasDrag = false;
            if (_lineRender)
            {
                GenerateLine();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            DestroyBlock();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            NewUndoRedo.Undo();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            NewUndoRedo.Redo();
        }
        /*
            ->Remove this later, Just for testing
        */
        if (Input.GetKeyDown(KeyCode.C))
        {
            PlayerPrefs.DeleteKey("access_token");
        }
    }

    private void _CanDrop()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (buildingInventoryController.currentBlock != null && (buildingInventoryController.currentBlock.AssetType == "Window" || buildingInventoryController.currentBlock.AssetType == "Door") && !CreatorUIController.isInputOverVisualElement() && Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            if (hitInfo.transform.tag == WHConstants.METABLOCK)
            {
                if (_CheckCanDropOnWall(ray, hitInfo))
                {
                    Texture2D CursorTexture = Resources.Load<Texture2D>("Sprites/Cursor/Add_Icon");
                    UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);
                    _canDrop = true;
                }
                else
                {
                    Texture2D CursorTexture = Resources.Load<Texture2D>("Sprites/Cursor/Stop_Icon");
                    UnityEngine.Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 2, CursorTexture.height / 2), CursorMode.Auto);
                    _canDrop = false;
                }
            }
            else
            {
                //UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                _canDrop = true;
            }
        }
        else
        {
            //UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _canDrop = true;
        }
    }

    private bool _CheckCanDropOnWall(Ray ray, RaycastHit hitInfo)
    {
        CreatorItem item = CreatorItemFinder.FindItemWithGameObject(hitInfo.transform.gameObject);
        return (item != null && item is NewWall && ((NewWall)item).CanAddItem(ray.origin, buildingInventoryController.currentBlock.AssetType == "Window" ? WHConstants.DefaultWindowLength : WHConstants.DefaultDoorLength));
    }
    private bool _IsMouseOverBuildingCanvas()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            if (hitInfo.transform.gameObject == BuildingCanvas && !CreatorUIController.isInputOverVisualElement())
            {
                return true;
            }
        }
        return false;
    }

    // For camera movement
    private void _DragBuildingCanvas()
    {
        var meshRenderer = BuildingCanvas.GetComponent<MeshRenderer>();
        Bounds canvasBounds = meshRenderer.bounds;
        GameObject playerObject = SceneObject.GetPlayer(SceneObject.Mode.Creator);

        var x = Input.GetAxis("Mouse X");
        var y = Input.GetAxis("Mouse Y");

        Vector3 moveDirection = new Vector3(-x, -y, 0.0f);
        moveDirection = Quaternion.AngleAxis(_camera.transform.eulerAngles.z, Vector3.forward) * moveDirection;
        moveDirection *= _camera.orthographicSize / 10.0f;

        var newPosition = playerObject.transform.position + moveDirection;
        if (newPosition.x > canvasBounds.min.x && newPosition.x < canvasBounds.max.x && newPosition.y > canvasBounds.min.y && newPosition.y < canvasBounds.max.y)
        {
            playerObject.transform.position = newPosition;
        }
    }
    private void _ZoomBuildingCanvas(int zoomDir)
    {
        if (_IsMouseOverBuildingCanvas())
        {
            // apply zoom
            _camera.orthographicSize += zoomDir * Time.deltaTime * zoomSpeed;
            // clamp camera distance
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, MIN_SCALE, MAX_SCALE);
        }
    }

    private void CreateLine(RaycastHit hitInfo)
    {
        _lineRendererGO = new GameObject();
        LineRenderer lineRenderer = _lineRendererGO.AddComponent<LineRenderer>();
        lineRenderer.widthMultiplier = WHConstants.DefaultWall2DHeight;
        lineRenderer.SetPosition(0, new Vector3(hitInfo.point.x, hitInfo.point.y, -0.2f));
    }

    private void UpdateLine(RaycastHit hitInfo)
    {
        LineRenderer lineRenderer = _lineRendererGO.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(1, new Vector3(hitInfo.point.x, hitInfo.point.y, -0.2f));
    }

    private void GenerateLine()
    {
        LineRenderer lineRenderer = _lineRendererGO.GetComponent<LineRenderer>();
        if (lineRenderer.GetPosition(0) != lineRenderer.GetPosition(1))
        {
            NewBuildingController.CreateWall(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1));
        }
        Destroy(_lineRendererGO);
        _lineRender = false;
    }

    public void DestroyBlock()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool isHit = Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity);
        if (isHit)
        {
            if (hitInfo.transform.tag == WHConstants.METABLOCK)
            {
                CreatorItem item = CreatorItemFinder.FindItemWithGameObject(hitInfo.transform.gameObject);
                NewBuildingController.DeleteItem(item.name);
            }
        }

    }
}
