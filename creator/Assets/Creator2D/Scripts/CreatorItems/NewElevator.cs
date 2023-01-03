using UnityEngine;

public class NewElevator : NewItemWithMesh, IHas3DObject, NewIScalable
{
    private Vector3 _scale = Vector3.one;
    public Vector3 Scale => _scale;

    public NewElevator(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }

    public GameObject GetGameObject()
    {
        return PrefabFinder.Find("Elevator");
    }

    public override void SetDimension(float length, float height, float width)
    {
        base.SetDimension(length, height, width);
        SetScale(new Vector3(length, height, width));
    }

    public override CreatorItem Clone()
    {
        var worldPosition = gameObject.transform.position;
        var worldRotation = gameObject.transform.rotation;
        var worldScale = gameObject.transform.localScale;
        CreatorItem clone = new NewElevator(GameObject.Instantiate(gameObject), new UIItem(name));
        clone.gameObject.transform.position = worldPosition;
        clone.gameObject.transform.rotation = worldRotation;
        clone.gameObject.transform.localScale = worldScale;
        clone.SetName(name);
        clone.SetPosition(this.Position);
        clone.GetComponent<NewIHasRotation>().SetRotation(this.EulerAngles.x, this.EulerAngles.y, this.EulerAngles.z);
        clone.GetComponent<NewIHasDimension>().SetDimension(Dimension.Length, Dimension.Height, Dimension.Width);

        ObjectTransformHandler transformHandler = new ObjectTransformHandler(clone.gameObject, clone as NewElevator, "elevator", true);
        CloneChildren(clone);
        return clone;
    }

    public void SetScale(Vector3 scale)
    {
        _scale = scale;
    }

    public void ScaleBy(float scale)
    {
        _scale *= scale;
    }
}