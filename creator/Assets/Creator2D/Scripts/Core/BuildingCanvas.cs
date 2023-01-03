using System;
using System.Collections.Generic;
using System.Linq;
using TerrainEngine;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingCanvas
{

    private GameObject gameObject;
    private GameObject buildingGO;

    public static List<Vector3> boundaryCoordinates = new List<Vector3>();

    public static List<double> centerLatLon;

    private static BuildingCanvas b_instance = null;

    public static BuildingCanvas Get()
    {
        if (b_instance == null)
        {
            b_instance = new BuildingCanvas();
            b_instance.gameObject = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.BUILDING_CANVAS);
        }
        return b_instance;
    }

    public void GenerateCanvas(OsmBuildingData building, bool autoGenerateFloor = true)
    {
        //Clear if any record present of previous create floorplan
        TransformDatas.allNodeList.Clear();
        TransformDatas.wallListenersList.Clear();

        List<Vector3> pointList = new List<Vector3>();
        var _building = TerrainRuntime.finalBuildingData;

        boundaryCoordinates = new List<Vector3>();
        if (building == null)
        {
            return;
        }
        centerLatLon = building.center.coordinates;
        Vector2 centerCoordinate = ConvertCoordinate.GeoToWorldPosition((float)building.center.coordinates[1], (float)building.center.coordinates[0]);
        foreach (List<List<float>> firstList in building.geometry.coordinates)
        {
            foreach (List<float> coordinateList in firstList)
            {
                if (coordinateList.Count == 2)
                {
                    Vector2 coord = ConvertCoordinate.GeoToWorldPosition((float)coordinateList[1], (float)coordinateList[0]);
                    boundaryCoordinates.Add(coord - centerCoordinate);
                }
            }
        }
        var buildingData = _building.ToList().Where(x => x.Key.Split("[")[0] == building.id).FirstOrDefault();
        if (buildingData.Value != null && 1 == 2)  // TODO: Handle later
        {
            pointList.AddRange(buildingData.Value.localVertices);
        }
        else
        {
            foreach (var b in boundaryCoordinates)
            {
                pointList.Add(new Vector3(b.x, 0, b.y));
            }
        }

        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshFilter>());
        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshRenderer>());
        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshCollider>());

        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = BoundedMeshCreator.GetMesh(pointList);
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Material material = Resources.Load("Materials/BuildingCanvas") as Material;
        meshRenderer.material = material;

        gameObject.AddComponent<MeshCollider>();
        gameObject.transform.eulerAngles = new Vector3(-90, 0, 0);
        gameObject.transform.position += new Vector3(0, 0, 0.1f);

        if (autoGenerateFloor)
        {
            if (buildingData.Value != null && 1 == 2)  // TODO: Handle later
            {
                pointList.Add(buildingData.Value.localVertices[0]);
            }
            else
            {
                pointList.Add(new Vector3(boundaryCoordinates[0].x, 0, boundaryCoordinates[0].y));
            }
            AutoFloorPlanGenerator.Generate(pointList, gameObject.transform.position);
            CreatorUIController.SetupAddFloorDropdown();
        }
        UpdateCameraOrthoSize();
    }

    private void UpdateCameraOrthoSize()
    {
        var mainPanel = CreatorUIController.getRoot().Q<VisualElement>("main-panel");
        float orthoSizePadding = 5f;
        float screenRatio = (float)mainPanel.localBound.width / (float)mainPanel.localBound.height;
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        var targetBounds = meshRenderer.bounds;
        float targetRatio = targetBounds.size.x / targetBounds.size.y;

        var camera = SceneObject.GetCamera(SceneObject.Mode.Creator).GetComponent<Camera>(); ;
        var player = SceneObject.GetPlayer(SceneObject.Mode.Creator);
        camera.orthographicSize = orthoSizePadding;

        if (float.IsNaN((float)mainPanel.localBound.height) || (float)mainPanel.localBound.height == 0 || screenRatio >= targetRatio)
        {
            camera.orthographicSize = targetBounds.size.y / 2;
        }
        else
        {
            float differenceInSize = targetRatio / screenRatio;
            camera.orthographicSize = targetBounds.size.y / 2 * differenceInSize;
        }
        camera.orthographicSize += orthoSizePadding;
        CreatorEventManager.SetMaxScale(camera.orthographicSize + orthoSizePadding);

        player.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, -0.5f);

    }
}

public class AutoFloorPlanGenerator
{
    public static void Generate(List<Vector3> boundaryCoordinates, Vector3 positionOffset)
    {
        if (boundaryCoordinates.Count <= 1) return;

        var floorBoundary = boundaryCoordinates;
        // foreach (var coord in boundaryCoordinates)
        // {
        //     floorBoundary.Add(new Vector3(coord.x, 0, coord.y));
        // }
        NewBuildingController.CreateRoof();
        NewBuildingController.CreateFloor(floorBoundary);

        NewBuildingController.CreateFloorPlan(null);
        NewBuildingController.CreateFloor(floorBoundary);
        NewBuildingController.CreateCeiling(floorBoundary);
        int boundryCount = boundaryCoordinates.Count;
        var previousCoordinate = new Vector3(boundaryCoordinates[0].x, boundaryCoordinates[0].z, 0);
        for (int i = 1; i < boundryCount; i++)
        {
            var zUpPosition = new Vector3(0, 0, WHConstants.DefaultZ);
            NewBuildingController.CreateWall(previousCoordinate + zUpPosition,
             new Vector3(boundaryCoordinates[i].x, boundaryCoordinates[i].z, 0) + zUpPosition, true, true);
            previousCoordinate = new Vector3(boundaryCoordinates[i].x, boundaryCoordinates[i].z, 0);
        }
    }
}