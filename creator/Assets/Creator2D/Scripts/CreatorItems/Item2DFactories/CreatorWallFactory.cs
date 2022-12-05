using UnityEngine;

public class CreatorWallFactory : ICreatorItemFactory
{
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    public CreatorWallFactory(Vector3 startPosition, Vector3 endPosition)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;
    }

    public CreatorItem Create(string name)
    {
        UIItem uiItem = new WallUIFactory().Create(name);
        GameObject wall = CreateWall();
        float angle = Mathf.Atan2(_endPosition.y - _startPosition.y, _endPosition.x - _startPosition.x) * 180 / Mathf.PI;
        wall.transform.eulerAngles = new Vector3(0, 0, angle);
        NewWall item = new NewWall(wall, uiItem);
        item.SetDimension(Vector3.Distance(_startPosition, _endPosition), WHConstants.DefaultWallHeight, WHConstants.DefaultWallBreadth);
        item.SetPosition(new Vector3(_startPosition.x, _startPosition.y, 0));
        item.SetName(name);
        item.SetRotation(0, -angle, 0);
        uiItem._delegate = item;

        WallTransformHandler wallTransformHandler = new WallTransformHandler(wall, item);
        return item;
    }

    private GameObject CreateWall()
    {
        GameObject wallGO = new GameObject();
        LineRenderer lineRenderer = wallGO.AddComponent<LineRenderer>();
        CreateLine(lineRenderer);
        float lineLength = Vector3.Distance(_startPosition, _endPosition);
        float lineWidth = lineRenderer.endWidth;

        BoxCollider lineCollider = wallGO.AddComponent<BoxCollider>();
        lineCollider.transform.parent = lineRenderer.transform;
        lineCollider.center = new Vector3(lineLength / 2, 0.0f, 0.0f);
        lineCollider.size = new Vector3(lineLength, lineWidth, 1f);

        wallGO.tag = WHConstants.METABLOCK;
        wallGO.transform.position = _startPosition;

        return wallGO;
    }

    private LineRenderer CreateLine(LineRenderer lineRenderer)
    {
        int lengthOfLineRenderer = 2;
        Color lineRendererColor = Color.black;

        // This creates a special material just for this one object. If you change this material it will only affect this object:
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = WHConstants.DefaultWall2DHeight;
        lineRenderer.positionCount = lengthOfLineRenderer;
        lineRenderer.material.color = lineRendererColor;

        var points = new Vector3[lengthOfLineRenderer];
        points[0] = _startPosition;
        points[1] = _endPosition;
        lineRenderer.SetPositions(points);

        return lineRenderer;
    }
}