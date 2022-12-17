using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingCanvas
{

    private GameObject gameObject;
    private GameObject buildingGO;

    List<Vector3> boundaryCoordinates = new List<Vector3>();

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

        List<Vector2> pointList = new List<Vector2>();

        foreach (var b in boundaryCoordinates)
        {
            pointList.Add(new Vector2(b.x, b.y));
        }

        // _boundary is closed meaning the first coordinate is also repeated in last one
        pointList.RemoveAt(pointList.Count - 1);

        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshFilter>());
        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshRenderer>());
        UnityEngine.GameObject.DestroyImmediate(gameObject.GetComponent<MeshCollider>());

        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = new Triangulator().CreateInfluencePolygon(pointList.ToArray());
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Material material = Resources.Load("Materials/BuildingCanvas") as Material;
        meshRenderer.material = material;

        gameObject.AddComponent<MeshCollider>();
        gameObject.transform.eulerAngles = new Vector3(-90, 0, 0);
        UpdateCameraOrthoSize();

        if (autoGenerateFloor)
        {
            AutoFloorPlanGenerator.Generate(boundaryCoordinates, gameObject.transform.position);
            CreatorUIController.SetupAddFloorDropdown();
        }
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

        if (screenRatio >= targetRatio)
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

        NewBuildingController.CreateFloorPlan(null);
        var floorBoundary = new List<Vector3>();
        foreach (var coord in boundaryCoordinates)
        {
            floorBoundary.Add(new Vector3(coord.x, 0, coord.y));
        }
        NewBuildingController.CreateFloor(floorBoundary);
        NewBuildingController.CreateCeiling(floorBoundary);

        var previousCoordinate = boundaryCoordinates[0];
        for (int i = 1; i < boundaryCoordinates.Count; i++)
        {
            var zUpPosition = new Vector3(0, 0, -0.2f);
            NewBuildingController.CreateWall(previousCoordinate + positionOffset + zUpPosition, boundaryCoordinates[i] + positionOffset + zUpPosition);
            previousCoordinate = boundaryCoordinates[i];
        }
    }
}