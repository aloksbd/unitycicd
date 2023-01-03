using UnityEngine;

public class CreatorElevatorFactory : ICreatorItemFactory
{
    private Vector3 _position;
    private Sprite _sprite;
    private float _height;

    public CreatorElevatorFactory(Vector3 position, Sprite sprite, float height)
    {
        _height = height;
        _position = position;
        _sprite = sprite;
    }

    public CreatorItem Create(string name, bool createGO = true)
    {
        NewElevator item;
        GameObject elevator = null;
        if (createGO)
        {
            UIItem uiItem = new ElevatorUIFactory().Create(name);
            elevator = CreateElevator();
            item = new NewElevator(elevator, uiItem);
        }
        else
        {
            item = new NewElevator(null, null);
        }
        item.SetPosition(new Vector3(_position.x, _position.y, 0));
        item.SetDimension(WHConstants.DefaultElevatorLength, _height, WHConstants.DefaultElevator2DHeight);
        item.SetName(name);

        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        if (createGO)
        {
            ObjectTransformHandler transformHandler = new ObjectTransformHandler(elevator, item, "elevator");
        }
        return item;
    }

    private GameObject CreateElevator()
    {
        GameObject elevatorGO = new GameObject();
        SpriteRenderer spriteRenderer = elevatorGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = _sprite;

        Vector2 unit = ConvertCoordinate.PixelToUnit(spriteRenderer.sprite);

        BoxCollider lineCollider = elevatorGO.AddComponent<BoxCollider>();
        lineCollider.transform.parent = spriteRenderer.transform;
        lineCollider.size = new Vector3(unit.x, unit.y, 1f);
        lineCollider.center = new Vector3(0.0f, 0.0f, 0.0f);

        elevatorGO.tag = WHConstants.METABLOCK;

        // _startPosition.y -= WHConstants.DefaultDoor2DHeight / 2;
        elevatorGO.transform.position = _position;
        elevatorGO.transform.localScale = new Vector3(WHConstants.DefaultElevatorLength / unit.x, WHConstants.DefaultElevator2DHeight / unit.y, 1);
        return elevatorGO;
    }
}