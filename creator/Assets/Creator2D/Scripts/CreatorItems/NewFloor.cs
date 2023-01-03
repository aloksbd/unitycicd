using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class NewFloor : NewItemWithMesh, NewIHasBoundary
{
    private List<Vector3> _boundary;
    public List<Vector3> Boundary => _boundary;

    public NewFloor(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }

    public override Mesh GetMesh()
    {
        return BoundedMeshCreator.GetMesh(_boundary);
    }

    public void SetBoundary(List<Vector3> boundary)
    {
        _boundary = boundary;
    }

    public override CreatorItem Clone()
    {
        CreatorItem clone = new NewFloor(GameObject.Instantiate(gameObject), new UIItem(name));
        clone.SetName(name);
        clone.SetPosition(this.Position);
        clone.GetComponent<NewIHasRotation>().SetRotation(this.EulerAngles.x, this.EulerAngles.y, this.EulerAngles.z);
        clone.GetComponent<NewIHasDimension>().SetDimension(Dimension.Length, Dimension.Height, Dimension.Width);
        clone.GetComponent<NewIHasBoundary>().SetBoundary(_boundary);
        CloneChildren(clone);
        return clone;
    }
}

public interface NewIHasBoundary
{
    List<Vector3> Boundary { get; }
    void SetBoundary(List<Vector3> boundary);
}

public class BoundedMeshCreator
{
    private static void PrepareVertices(List<Vector3> worldVertices)
    {
        //  Remove noisy vertices
        for (int i = 0; i < worldVertices.Count; i++)
        {

            int prev = i - 1;
            if (prev < 0)
            {
                prev = worldVertices.Count - 1;
            }

            int next = i + 1;
            if (next >= worldVertices.Count)
            {
                next = 0;
            }

            //  reject if too close to prior vertex
            if ((worldVertices[prev] - worldVertices[i]).magnitude < 0.01f)
            {
                worldVertices.RemoveAt(i);
                i--;
                continue;
            }

            //  reject if too close to next vertex
            if ((worldVertices[next] - worldVertices[i]).magnitude < 0.01f)
            {
                worldVertices.RemoveAt(next);
                continue;
            }

            //  reject if angle between previous and next vertex is too acute
            float a1 = Angle2D(worldVertices[prev], worldVertices[i]);
            float a2 = Angle2D(worldVertices[i], worldVertices[next]);

            if (Mathf.Abs(a1 - a2) < 5)
            {
                worldVertices.RemoveAt(i);
                i--;
            }
        }

        if (worldVertices.First() == worldVertices.Last())
        {
            worldVertices.Remove(worldVertices[worldVertices.Count - 1]);
            if (worldVertices.Count < 3)
            {
                return;
            }
        }

        //  Normalize vertex sort order
        if (IsReversed(worldVertices))
        {
            worldVertices.Reverse();
        }

    }

    private static bool IsReversed(List<Vector3> points)
    {
        float minZ = float.MaxValue;
        int i2 = -1;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];
            if (p.z < minZ)
            {
                minZ = p.z;
                i2 = i;
            }
        }

        int i1 = i2 - 1;
        int i3 = i2 + 1;

        if (i1 < 0)
        {
            i1 += points.Count;
        }

        if (i3 >= points.Count)
        {
            i3 -= points.Count;
        }

        Vector3 p1 = points[i1];
        Vector3 p2 = points[i2];
        Vector3 p3 = points[i3];

        Vector3 s1 = p2 - p1;
        Vector3 s2 = p3 - p1;

        Vector3 side1 = s1;
        Vector3 side2 = s2;

        return Vector3.Cross(side1, side2).y <= 0;
    }

    public static float Angle2D(Vector3 point1, Vector3 point2)
    {
        return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) * Mathf.Rad2Deg;
    }

    public static Mesh GetMesh(List<Vector3> boundary)
    {
        PrepareVertices(boundary);
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv;
        List<int> roofTriangles;

        CreateRoofMesh(vertices, boundary, out uv, out roofTriangles);
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);

        mesh.SetTriangles(roofTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    private static void CreateRoofMesh(List<Vector3> vertices, List<Vector3> boundary, out List<Vector2> uv, out List<int> triangles)
    {
        List<Vector2> roofPoints = CreateRoofVertices(vertices, boundary);
        triangles = CreateRoofTriangles(vertices, roofPoints);


        float minX = vertices.Min(p => p.x);
        float minZ = vertices.Min(p => p.z);
        float maxX = vertices.Max(p => p.x);
        float maxZ = vertices.Max(p => p.z);
        float offX = maxX - minX;
        float offZ = maxZ - minZ;

        uv = vertices.Select(v => new Vector2((v.x - minX) / offX, (v.z - minZ) / offZ)).ToList();
    }

    private static List<int> CreateRoofTriangles(List<Vector3> vertices, List<Vector2> roofPoints)
    {
        List<int> triangles = new List<int>();
        int[] trs = Triangulator2.Triangulate(roofPoints, clockwise: true);
        if (trs != null) triangles.AddRange(trs);
        return triangles;
    }

    private static List<Vector2> CreateRoofVertices(List<Vector3> vertices, List<Vector3> boundary)
    {
        Vector3[] targetVertices = new Vector3[boundary.Count];
        for (int i = 0; i < boundary.Count; i++)
        {
            targetVertices[i] = boundary[i];
        }

        List<Vector2> roofPoints = new List<Vector2>();
        foreach (Vector3 p in targetVertices)
        {
            Vector3 tv = new Vector3(p.x, 0, p.z);
            Vector2 rp = new Vector2(p.x, p.z);

            vertices.Add(tv);
            roofPoints.Add(rp);
        }

        return roofPoints;
    }
}