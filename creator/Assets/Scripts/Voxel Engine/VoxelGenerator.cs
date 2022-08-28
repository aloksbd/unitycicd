using System.Collections.Generic;
using UnityEngine;

public class VoxelGenerator : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    int[,,] blocks;

    public void setBlocks(int[,,] _blocks)
    {
        blocks = _blocks;
        CreateMeshData();
        CreateMesh();
    }

    void CreateMeshData()
    {

        for (int y = 0; y < blocks.GetLength(0); y++)
        {
            for (int x = 0; x < blocks.GetLength(1); x++)
            {
                for (int z = 0; z < blocks.GetLength(2); z++)
                {

                    AddVoxelDataToChunk(new Vector3(x, y, z));

                }
            }
        }

    }

    bool CheckVoxel(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (y > blocks.GetLength(0) - 1)
            return false;
        if (x < 0 || x > blocks.GetLength(1) - 1 || z < 0 || z > blocks.GetLength(2) - 1)
            return false;
        if (y < 0) return true;
        if (blocks[y, x, z] == 0)
            return false;
        return true;
    }

    void AddVoxelDataToChunk(Vector3 pos)
    {

        for (int p = 0; p < 6; p++)
        {
            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);
            if (blocks[y, x, z] == 0)
            {
                return;
            }
            if (!CheckVoxel(pos + faceChecks[p]))
            {

                vertices.Add(pos + voxelVerts[voxelTris[p, 0]]);
                vertices.Add(pos + voxelVerts[voxelTris[p, 1]]);
                vertices.Add(pos + voxelVerts[voxelTris[p, 2]]);
                vertices.Add(pos + voxelVerts[voxelTris[p, 3]]);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
                vertexIndex += 4;

            }
        }

    }

    void CreateMesh()
    {

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        Debug.Log(vertexIndex);
    }

    private Vector3[] voxelVerts = new Vector3[8] {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    private Vector3[] faceChecks = new Vector3[6] {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)
    };

    private int[,] voxelTris = new int[6, 4] {
        {0, 3, 1, 2}, // Back Face
		{5, 6, 4, 7}, // Front Face
		{3, 7, 2, 6}, // Top Face
		{1, 5, 0, 4}, // Bottom Face
		{4, 7, 0, 3}, // Left Face
		{1, 2, 5, 6} // Right Face
	};
}
