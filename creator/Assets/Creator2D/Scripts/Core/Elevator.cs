using System;
using UnityEngine;
using System.Collections.Generic;

namespace ObjectModel
{
    public class ElevatorType
    {
        // public static Dictionary<Guid, IHasMaterial>;
        public static Guid Brick = Guid.NewGuid();
        public static Guid Glass = Guid.NewGuid();
    }

    public class Elevator : Item
    {

        private Elevator(IHasPosition position, IHasRotation rotation, IHasDimension dimension, IHasMesh mesh, Item Parent) : base(() => NamingStrategy.GetName("Elevator", Parent.Children))
        {
            AddComponent(position);
            AddComponent(rotation);
            AddComponent(dimension);
            AddComponent(mesh);
            AddComponent(new Selectable());

            var gameObject3d = new GameObject3D(
                Name,
                () => mesh.CreateMesh(),
                () => GameObject3D.ChildrenToIGameObject3D(Children),
                () =>
                {
                    var weakPosition = GetComponent<IHasPosition>();
                    if (weakPosition.IsAlive)
                    {
                        return (weakPosition.Target as IHasPosition).Position;
                    }
                    return new Vector3(0, 0, 0);
                },
                () =>
                {
                    var weakPosition = GetComponent<IHasRotation>();
                    if (weakPosition.IsAlive)
                    {
                        return (weakPosition.Target as IHasRotation).EulerAngles;
                    }
                    return new Vector3(0, 0, 0);
                },
                () =>
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = Color.white;
                    return material;
                }
            );
            AddComponent(gameObject3d);
        }

        public static Elevator Create(Vector3 position, float zAngle, float length, float width, float height, Item Parent)
        {
            var _position = new HasPosition(position);
            var rotation = new HasRotation(new Vector3(0, 0, zAngle));
            var dimension = new Dimension(length, width, height);

            var mesh = new HasMesh(
                () =>
                {
                    var elevatorCreator = new ElevatorCreator(height, length, width);
                    return elevatorCreator.CreateElevatorMesh();
                }
            );
            return new Elevator(_position, rotation, dimension, mesh, Parent);
        }
    }

    // Just a box creator for now
    public class ElevatorCreator
    {
        private float height, length, breadth;
        public ElevatorCreator(float height, float length, float breadth)
        {
            this.height = height;
            this.length = length;
            this.breadth = breadth;
            createVertices();
        }
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        int vertexIndex = 0;
        public void createElevator()
        {
            for (int p = 0; p < 6; p++)
            {
                vertices.Add(voxelVerts[voxelTris[p, 0]]);
                vertices.Add(voxelVerts[voxelTris[p, 1]]);
                vertices.Add(voxelVerts[voxelTris[p, 2]]);
                vertices.Add(voxelVerts[voxelTris[p, 3]]);
                uvs.Add(voxelUvs[0]);
                uvs.Add(voxelUvs[1]);
                uvs.Add(voxelUvs[2]);
                uvs.Add(voxelUvs[3]);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
                vertexIndex += 4;
            }
        }
        public Mesh CreateElevatorMesh()
        {
            createElevator();
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        private Vector3[] voxelVerts;
        private void createVertices()
        {
            voxelVerts = new Vector3[8] {
            new Vector3(0, 0, 0),
            new Vector3(length, 0, 0),
            new Vector3(length, height, 0),
            new Vector3(0, height, 0),
            new Vector3(0, 0, breadth),
            new Vector3(length, 0, breadth),
            new Vector3(length, height, breadth),
            new Vector3(0, height, breadth),
        };
        }
        private int[,] voxelTris = new int[6, 4] {
            {0, 3, 1, 2}, // Back Face
            {5, 6, 4, 7}, // Front Face
            {3, 7, 2, 6}, // Top Face
            {1, 5, 0, 4}, // Bottom Face
            {4, 7, 0, 3}, // Left Face
            {1, 2, 5, 6} // Right Face
        };
        private Vector2[] voxelUvs = new Vector2[4] {
            new Vector2 (0.0f, 0.0f),
            new Vector2 (0.0f, 1.0f),
            new Vector2 (1.0f, 0.0f),
            new Vector2 (1.0f, 1.0f)
        };
    }
}
