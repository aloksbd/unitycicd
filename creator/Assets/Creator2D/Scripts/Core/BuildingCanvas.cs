using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingCanvas : MonoBehaviour
{
    [Header("Material")]
    public Material material;

    List<Vector3> boundaryCoordinates = new List<Vector3>();

    async void Start()
    {
        BuildingData building = await Buildings.GetBuildingDetail();
        if (building == null)
        {
            return;
        }
        Vector2 centerCoordinate = ConvertCoordinate.GeoToWorldPosition(building.center.coordinates[1], building.center.coordinates[0]);
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

        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = new Triangulator().CreateInfluencePolygon(pointList.ToArray());
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        gameObject.AddComponent<MeshCollider>();
        gameObject.transform.Rotate(new Vector3(-90, 0, 0));
        UpdateCameraOrthoSize();

        AutoFloorPlanGenerator.Generate(boundaryCoordinates, gameObject.transform.position);
        CreatorUIController.SetupAddFloorDropdown();
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