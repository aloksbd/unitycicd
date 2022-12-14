using System;
using UnityEngine;
using System.Collections.Generic;

namespace ObjectModel
{
    public class WallType
    {
        // public static Dictionary<Guid, IHasMaterial>;
        public static Guid Brick = Guid.NewGuid();
        public static Guid Glass = Guid.NewGuid();
    }

    [Serializable]
    public class Wall : Item
    {
        private Wall() : base(() => NamingStrategy.GetName(WHConstants.WALL)) { }

        private Wall(IHasPosition position, IHasRotation rotation, IHasDimension dimension, Item Parent) : base(() => NamingStrategy.GetName(WHConstants.WALL, Parent.Children))
        {

            AddComponent(position);
            AddComponent(rotation);
            AddComponent(dimension);
            AddComponent(new Selectable());

            var mesh = new HasMesh(
                () =>
                {
                    var wallCreator = new WallCreator(dimension.Height, dimension.Length, dimension.Width, Children);
                    return wallCreator.CreateWallMesh();
                }
            );
            AddComponent(mesh);

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
                    material.color = Color.magenta;
                    return material;
                }
            );
            AddComponent(gameObject3d);
        }

        public static Wall Create(Vector3 position, float yAngle, float length, float width, float height, Item Parent)
        {
            var _position = new HasPosition(position);
            var rotation = new HasRotation(new Vector3(0, yAngle, 0));
            var dimension = new Dimension(length, width, height);
            return new Wall(_position, rotation, dimension, Parent);
        }

        override protected IItem GetClonedItem()
        {
            return new Wall();
        }
    }

    // Just a box creator for now
    public class WallCreator
    {
        private float height, length, breadth;
        private List<IItem> _children = new List<IItem>();
        private List<Cube> subCubes = new List<Cube>();

        public WallCreator(float height, float length, float breadth, List<IItem> children)
        {
            this.height = height;
            this.length = length;
            this.breadth = breadth;
            _children = children;
            subCubes.Add(new Cube(Vector3.zero, height, length, breadth));
        }
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        int vertexIndex = 0;
        public void createWall()
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

        public Mesh CreateWallMesh()
        {
            if (_children.Count > 0)
            {
                foreach (var door in _children)
                {

                    var weakPosition = door.GetComponent<IHasPosition>();
                    var weakDimension = door.GetComponent<IHasDimension>();
                    if (weakPosition.IsAlive && weakDimension.IsAlive)
                    {
                        var position = (weakPosition.Target as IHasPosition).Position;
                        var dimension = (weakDimension.Target as IHasDimension);

                        foreach (var cube in subCubes)
                        {
                            if (position.x >= cube.position.x && position.x < cube.position.x + cube.length
                                && position.y >= cube.position.y && position.y < cube.position.y + cube.height)
                            {
                                subCubes.Remove(cube);

                                if (position.x > cube.position.x)
                                {
                                    subCubes.Add(new Cube(cube.position, cube.height, position.x - cube.position.x, breadth));
                                }

                                if (position.y > cube.position.y)
                                {
                                    subCubes.Add(new Cube(new Vector3(position.x, cube.position.y, 0), position.y - cube.position.y, dimension.Length, breadth));
                                }

                                if (position.y + dimension.Height < cube.position.y + cube.height)
                                {
                                    subCubes.Add(new Cube(new Vector3(position.x, dimension.Height + position.y, 0), cube.height + cube.position.y - dimension.Height - position.y, dimension.Length, breadth));
                                }

                                if (position.x + dimension.Length < cube.position.x + cube.length)
                                {
                                    subCubes.Add(new Cube(new Vector3(dimension.Length + position.x, cube.position.y, 0), cube.height, cube.length + cube.position.x - dimension.Length - position.x, breadth));
                                }

                                break;
                            }
                        }
                    }
                }
                foreach (var cube in subCubes)
                {
                    createVertices(cube.position, cube.height, cube.length, cube.breadth);
                    createWall();
                }
            }
            else
            {
                createVertices(Vector3.zero, height, length, breadth);
                createWall();
            }
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        private Vector3[] voxelVerts;
        private void createVertices(Vector3 position, float height, float length, float breadth)
        {
            var x = position.x;
            var y = position.y;
            var z = position.z;

            voxelVerts = new Vector3[8] {
                new Vector3(x, y, 0),
                new Vector3(x+length, y, 0),
                new Vector3(x+length, y+height, 0),
                new Vector3(x, y+height, 0),
                new Vector3(x, y, breadth),
                new Vector3(x+length, y, breadth),
                new Vector3(x+length, y+height, breadth),
                new Vector3(x, y+height, breadth),
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

        private struct Cube
        {
            public Vector3 position;
            public float height;
            public float length;
            public float breadth;

            public Cube(Vector3 position, float height, float length, float breadth)
            {
                this.position = position;
                this.height = height;
                this.length = length;
                this.breadth = breadth;
            }
        }
    }
}