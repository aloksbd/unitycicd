using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    public GameObject chunkObject;
    public MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> waterTriangles = new List<int>();
    List<Color> colors = new List<Color>();
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] voxelMap;

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    public bool isVoxelMapPopulated = false;

    public Chunk(ChunkCoord _coord, bool generateOnLoad)
    {
        voxelMap = new byte[World.Instance.ChunkWidht, World.Instance.ChunkHeight, World.Instance.ChunkWidht];
        coord = _coord;
        isVoxelMapPopulated = false;

        if (generateOnLoad)
            Init();
    }

    public void Init()  {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        if (World.Instance.collisionType != World.CollisionTypes.none)
            meshCollider = chunkObject.AddComponent<MeshCollider>();

        Material[] materials = new Material[2];
        materials[0] = World.Instance.material;
        materials[1] = World.Instance.waterMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * World.Instance.ChunkWidht, 0f, coord.z * World.Instance.ChunkWidht);
        chunkObject.name = "Chunk " + coord.x + "/" + coord.z;

        if (World.Instance.shadowType == World.ShadowTypes.off)
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        else if (World.Instance.shadowType == World.ShadowTypes.on)
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        else
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        PopulateVoxelMap();
        UpdateChunk();
    }

    public Vector3Int position
    {
        get { return Vector3Int.FloorToInt(chunkObject.transform.position); }
    }

    public bool IsVoxelInChunk(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x > World.Instance.ChunkWidht - 1 || pos.y < 0 || pos.y > World.Instance.ChunkHeight - 1 || pos.z < 0 || pos.z > World.Instance.ChunkWidht - 1)
            return false;
        else
            return true;
    }

    public void EditVoxel(Vector3Int pos, byte newID)
    {
        pos.x -= position.x;
        pos.z -= position.z;

        voxelMap[pos.x, pos.y, pos.z] = newID;

        UpdateSurroundingVoxel(pos);

        UpdateChunk();
    }

    public void UnsafeEditVoxel (Vector3Int pos, byte newID)
    {
        pos.x -= position.x;
        pos.z -= position.z;

        voxelMap[pos.x, pos.y, pos.z] = newID;

        UpdateSurroundingVoxel(pos);
    }

    public void SafeEdit ()
    {
        UpdateChunk();
    }

    void UpdateSurroundingVoxel(Vector3Int pos)
    {
        for (int p = 0; p < 4; p++)
        {
            Vector3Int current = pos + VoxelData.ChunkChecks[p];

            if (!IsVoxelInChunk(current))
            {
                if (World.Instance.GetChunkFromV3I(current + position) != null)
                    World.Instance.GetChunkFromV3I(current + position).UpdateChunk();
            }
        }
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < World.Instance.ChunkHeight; y++)
        {
            for (int x = 0; x < World.Instance.ChunkWidht; x++)
            {
                for (int z = 0; z < World.Instance.ChunkWidht; z++)
                {
                    voxelMap[x, y, z] = World.Instance.GetVoxel(new Vector3Int(x, y, z) + position);
                }
            }
        }

        isVoxelMapPopulated = true;
    }

    public void UpdateChunk()
    {
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3Int pos = v.position -= position;
            voxelMap[pos.x, pos.y, pos.z] = v.id;
        }

        ClearMeshData();

        for (int y = 0; y < World.Instance.ChunkHeight; y++)
        {
            for (int x = 0; x < World.Instance.ChunkWidht; x++)
            {
                for (int z = 0; z < World.Instance.ChunkWidht; z++)
                {
                    if (World.Instance.GetBlockFromID(voxelMap[x, y, z]).isSolid)
                        AddVoxelDataToChunk(new Vector3Int(x, y, z));
                }
            }
        }

        CreateMesh();
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        waterTriangles.Clear();

        colors.Clear();
        uvs.Clear();
    }

    bool CheckIfVoxelRender(Vector3Int pos, bool selfFluid)
    {
        if (!IsVoxelInChunk(pos))
            return World.Instance.CheckForVoxel(pos + position, selfFluid);

        if (World.Instance.GetBlockFromID(voxelMap[pos.x, pos.y, pos.z]).isSolid 
            && (!World.Instance.GetBlockFromID(voxelMap[pos.x, pos.y, pos.z]).isFluid || selfFluid))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public byte GetVoxelFromGlobalVector3I(Vector3Int pos)
    {
        pos.x -= position.x;
        pos.z -= position.z;

        return voxelMap[pos.x, pos.y, pos.z];
    }

    void AddVoxelDataToChunk(Vector3Int pos)
    {
        bool fluid = World.Instance.GetBlockFromID(voxelMap[pos.x, pos.y, pos.z]).isFluid;

        for (int p = 0; p < 6; p++)
        {
            if (!CheckIfVoxelRender(pos + VoxelData.faceChecks[p], fluid))
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                Color col = World.Instance.GetBlockFromID(voxelMap[pos.x, pos.y, pos.z]).color;
                colors.Add(col);
                colors.Add(col);
                colors.Add(col);
                colors.Add(col);

                uvs.Add(VoxelData.voxelUvs[0]);
                uvs.Add(VoxelData.voxelUvs[1]);
                uvs.Add(VoxelData.voxelUvs[2]);
                uvs.Add(VoxelData.voxelUvs[3]);

                if (!fluid)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    waterTriangles.Add(vertexIndex);
                    waterTriangles.Add(vertexIndex + 1);
                    waterTriangles.Add(vertexIndex + 2);
                    waterTriangles.Add(vertexIndex + 2);
                    waterTriangles.Add(vertexIndex + 1);
                    waterTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles, 0);
        mesh.SetTriangles(waterTriangles, 1);

        mesh.colors = colors.ToArray();

        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        if (World.Instance.collisionType != World.CollisionTypes.none)
        {
            Mesh collMesh = new Mesh();

            collMesh.vertices = vertices.ToArray();

            if (World.Instance.collisionType == World.CollisionTypes.all)
            {
                collMesh.subMeshCount = 2;
                collMesh.SetTriangles(triangles, 0);
                collMesh.SetTriangles(waterTriangles, 1);
            }
            else if (World.Instance.collisionType == World.CollisionTypes.onlyGround)
            {
                collMesh.subMeshCount = 1;
                collMesh.triangles = triangles.ToArray();
            }
            else
            {
                collMesh.subMeshCount = 1;
                collMesh.triangles = waterTriangles.ToArray();
            }

            collMesh.colors = colors.ToArray();
            collMesh.uv = uvs.ToArray();

            collMesh.RecalculateNormals();
            collMesh.RecalculateBounds();
            meshCollider.sharedMesh = collMesh;
        }
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }
}
