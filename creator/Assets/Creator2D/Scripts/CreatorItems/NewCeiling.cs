using UnityEngine;
using System.Collections.Generic;

public class NewCeiling : NewItemWithMesh, NewIHasBoundary
{
    private List<Vector3> _boundary;
    public List<Vector3> Boundary => _boundary;

    public NewCeiling(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }

    public override Mesh GetMesh()
    {
        List<Vector2> pointList = new List<Vector2>();

        foreach (var b in _boundary)
        {
            pointList.Add(new Vector2(b.x, b.z));
        }

        // _boundary is closed meaning the first coordinate is also repeated in last one
        pointList.RemoveAt(pointList.Count - 1);

        var mesh = new Triangulator().CreateInfluencePolygon(pointList.ToArray());
        NormalInverter.Invert(mesh);
        return mesh;
    }

    public void SetBoundary(List<Vector3> boundary)
    {
        _boundary = boundary;
    }

    public override CreatorItem Clone()
    {
        CreatorItem clone = new NewCeiling(GameObject.Instantiate(gameObject), new UIItem(name));
        clone.SetName(name);
        clone.SetPosition(this.Position);
        clone.GetComponent<NewIHasRotation>().SetRotation(this.EulerAngles.x, this.EulerAngles.y, this.EulerAngles.z);
        clone.GetComponent<NewIHasDimension>().SetDimension(Dimension.Length, Dimension.Height, Dimension.Width);
        clone.GetComponent<NewIHasBoundary>().SetBoundary(_boundary);
        CloneChildren(clone);
        return clone;
    }
}