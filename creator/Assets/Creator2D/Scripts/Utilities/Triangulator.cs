using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

struct Triangle
{
    public int p1;
    public int p2;
    public int p3;
    public Triangle(int point1, int point2, int point3)
    {
        p1 = point1;
        p2 = point2;
        p3 = point3;
    }
}

class Edge
{
    public int p1;
    public int p2;
    public Edge(int point1, int point2)
    {
        p1 = point1;
        p2 = point2;
    }
    public Edge() : this(0, 0) { }
    public bool Equals(Edge other)
    {
        return ((this.p1 == other.p2) && (this.p2 == other.p1)) || ((this.p1 == other.p1) && (this.p2 == other.p2));
    }
}


public class Triangulator
{

    public bool TriangulatePolygonSubFunc_InCircle(Vector2 p, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (Mathf.Abs(p1.y - p2.y) < float.Epsilon && Mathf.Abs(p2.y - p3.y) < float.Epsilon)
        {
            return false;
        }
        float m1, m2, mx1, mx2, my1, my2, xc, yc;
        if (Mathf.Abs(p2.y - p1.y) < float.Epsilon)
        {
            m2 = -(p3.x - p2.x) / (p3.y - p2.y);
            mx2 = (p2.x + p3.x) * 0.5f;
            my2 = (p2.y + p3.y) * 0.5f;
            xc = (p2.x + p1.x) * 0.5f;
            yc = m2 * (xc - mx2) + my2;
        }
        else if (Mathf.Abs(p3.y - p2.y) < float.Epsilon)
        {
            m1 = -(p2.x - p1.x) / (p2.y - p1.y);
            mx1 = (p1.x + p2.x) * 0.5f;
            my1 = (p1.y + p2.y) * 0.5f;
            xc = (p3.x + p2.x) * 0.5f;
            yc = m1 * (xc - mx1) + my1;
        }
        else
        {
            m1 = -(p2.x - p1.x) / (p2.y - p1.y);
            m2 = -(p3.x - p2.x) / (p3.y - p2.y);
            mx1 = (p1.x + p2.x) * 0.5f;
            mx2 = (p2.x + p3.x) * 0.5f;
            my1 = (p1.y + p2.y) * 0.5f;
            my2 = (p2.y + p3.y) * 0.5f;
            xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
            yc = m1 * (xc - mx1) + my1;
        }
        float dx = p2.x - xc;
        float dy = p2.y - yc;
        float rsqr = dx * dx + dy * dy;
        dx = p.x - xc;
        dy = p.y - yc;
        double drsqr = dx * dx + dy * dy;
        return (drsqr <= rsqr);
    }


    public Mesh CreateInfluencePolygon(Vector2[] XZofVertices)
    {
        Vector3[] Vertices = new Vector3[XZofVertices.Length];
        for (int ii1 = 0; ii1 < XZofVertices.Length; ii1++)
        {
            Vertices[ii1] = new Vector3(XZofVertices[ii1].x, 0, XZofVertices[ii1].y);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = Vertices;
        mesh.uv = XZofVertices;
        mesh.triangles = TriangulatePolygon(XZofVertices);
        mesh.RecalculateNormals();
        return mesh;
    }


    public int[] TriangulatePolygon(Vector2[] XZofVertices)
    {
        List<Edge> boundaryEdges = new List<Edge>();
        for (int ind = 1; ind < XZofVertices.Length; ind++)
        {
            boundaryEdges.Add(new Edge(ind - 1, ind));
        }
        // Add the last edge to close the boundary
        boundaryEdges.Add(new Edge(0, XZofVertices.Length - 1));

        int VertexCount = XZofVertices.Length;
        float xmin = XZofVertices[0].x;
        float ymin = XZofVertices[0].y;
        float xmax = xmin;
        float ymax = ymin;
        for (int ii1 = 1; ii1 < VertexCount; ii1++)
        {
            if (XZofVertices[ii1].x < xmin)
            {
                xmin = XZofVertices[ii1].x;
            }
            else if (XZofVertices[ii1].x > xmax)
            {
                xmax = XZofVertices[ii1].x;
            }
            if (XZofVertices[ii1].y < ymin)
            {
                ymin = XZofVertices[ii1].y;
            }
            else if (XZofVertices[ii1].y > ymax)
            {
                ymax = XZofVertices[ii1].y;
            }
        }
        float dx = xmax - xmin;
        float dy = ymax - ymin;
        float dmax = (dx > dy) ? dx : dy;
        float xmid = (xmax + xmin) * 0.5f;
        float ymid = (ymax + ymin) * 0.5f;
        Vector2[] ExpandedXZ = new Vector2[3 + VertexCount];
        for (int ii1 = 0; ii1 < VertexCount; ii1++)
        {
            ExpandedXZ[ii1] = XZofVertices[ii1];
        }
        ExpandedXZ[VertexCount] = new Vector2((xmid - 2 * dmax), (ymid - dmax));
        ExpandedXZ[VertexCount + 1] = new Vector2(xmid, (ymid + 2 * dmax));
        ExpandedXZ[VertexCount + 2] = new Vector2((xmid + 2 * dmax), (ymid - dmax));
        List<Triangle> TriangleList = new List<Triangle>();
        TriangleList.Add(new Triangle(VertexCount, VertexCount + 1, VertexCount + 2));
        for (int ii1 = 0; ii1 < VertexCount; ii1++)
        {
            List<Edge> Edges = new List<Edge>();
            for (int ii2 = 0; ii2 < TriangleList.Count; ii2++)
            {
                if (TriangulatePolygonSubFunc_InCircle(ExpandedXZ[ii1], ExpandedXZ[TriangleList[ii2].p1], ExpandedXZ[TriangleList[ii2].p2], ExpandedXZ[TriangleList[ii2].p3]))
                {
                    Edges.Add(new Edge(TriangleList[ii2].p1, TriangleList[ii2].p2));
                    Edges.Add(new Edge(TriangleList[ii2].p2, TriangleList[ii2].p3));
                    Edges.Add(new Edge(TriangleList[ii2].p3, TriangleList[ii2].p1));
                    TriangleList.RemoveAt(ii2);
                    ii2--;
                }
            }
            if (ii1 >= VertexCount)
            {
                continue;
            }
            for (int ii2 = Edges.Count - 2; ii2 >= 0; ii2--)
            {
                for (int ii3 = Edges.Count - 1; ii3 >= ii2 + 1; ii3--)
                {
                    if (Edges[ii2].Equals(Edges[ii3]))
                    {
                        Edges.RemoveAt(ii3);
                        Edges.RemoveAt(ii2);
                        ii3--;
                        continue;
                    }
                }
            }
            for (int ii2 = 0; ii2 < Edges.Count; ii2++)
            {
                TriangleList.Add(new Triangle(Edges[ii2].p1, Edges[ii2].p2, ii1));
            }
            Edges.Clear();
            Edges = null;
        }
        for (int ii1 = TriangleList.Count - 1; ii1 >= 0; ii1--)
        {
            if (TriangleList[ii1].p1 >= VertexCount || TriangleList[ii1].p2 >= VertexCount || TriangleList[ii1].p3 >= VertexCount)
            {
                TriangleList.RemoveAt(ii1);
            }
            else
            {
                // check if all edges in triangle are inside boundary else remove it.
                // logic - if the midpoint of edge is inside polygon the edge is inside polygon
                var p1 = XZofVertices[TriangleList[ii1].p1];
                var p2 = XZofVertices[TriangleList[ii1].p2];
                var p3 = XZofVertices[TriangleList[ii1].p3];

                var edges = new List<Edge>() { new Edge(TriangleList[ii1].p1, TriangleList[ii1].p2), new Edge(TriangleList[ii1].p2, TriangleList[ii1].p3), new Edge(TriangleList[ii1].p3, TriangleList[ii1].p1) };
                var pp = new List<Vector2>() { (p1 + p2) / 2, (p2 + p3) / 2, (p3 + p1) / 2 };
                for (int ppi = 0; ppi < 3; ppi++)
                {
                    // skip if the line is edge of boundary
                    var isEdge = false;
                    foreach (var edge in boundaryEdges)
                    {
                        if (edges[ppi].Equals(edge))
                        {
                            isEdge = true;
                            break;
                        }
                    }
                    if (isEdge) { continue; }
                    if (!IsPointInPolygon(pp[ppi], XZofVertices))
                    {
                        TriangleList.RemoveAt(ii1);
                        break;
                    }
                }
            }

        }

        TriangleList.TrimExcess();

        int[] Triangles = new int[3 * TriangleList.Count];
        for (int ii1 = 0; ii1 < TriangleList.Count; ii1++)
        {
            Triangles[3 * ii1] = TriangleList[ii1].p1;
            Triangles[3 * ii1 + 1] = TriangleList[ii1].p2;
            Triangles[3 * ii1 + 2] = TriangleList[ii1].p3;
        }
        return Triangles;
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int polygonLength = polygon.Length, i = 0;
        bool inside = false;
        // x, y for tested point.
        float pointX = point.x, pointY = point.y;
        // start / end point for the current polygon segment.
        float startX, startY, endX, endY;
        Vector2 endPoint = polygon[polygonLength - 1];
        endX = endPoint.x;
        endY = endPoint.y;
        while (i < polygonLength)
        {
            startX = endX; startY = endY;
            endPoint = polygon[i++];
            endX = endPoint.x; endY = endPoint.y;

            inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                      && /* if so, test if it is under the segment */
                      ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
        }
        return inside;
    }
}



public class NormalInverter
{
    public static void Invert(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int t = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = t;
        }

        mesh.triangles = triangles;
    }
}