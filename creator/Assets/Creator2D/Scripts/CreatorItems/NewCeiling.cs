using UnityEngine;
using System.Collections.Generic;

public class NewCeiling : NewItemWithMesh, NewIHasBoundary
{
    private List<Vector3> _boundary;
    public List<Vector3> Boundary => _boundary;

    public NewCeiling(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }

    public override Mesh GetMesh()
    {
        var mesh = BoundedMeshCreator.GetMesh(_boundary);
        NormalInverter.Invert(mesh);
        var dimension = Parent.GetComponent<NewIHasDimension>().Dimension;
        SetPosition(new Vector3(0.0f, 0.0f, dimension.Height));
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