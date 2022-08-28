using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public enum GenTypes
    {
        OnStart, Enumerated
    }

    public enum ShadowTypes
    {
        on, off, fixedShadows
    }

    public enum CollisionTypes
    {
        all, onlyGround, onlyWater, none
    }

    [Header("World Info")]

    [Tooltip("Generation Seed of the World. DONT SET IT TO HIGH OR TO LOW!")]
    public int seed;
    [Tooltip("World Biome used for Generation")]
    public Biomes biome;
    [Tooltip("Normal block Material")]
    public Material material;
    [Tooltip("Water and Transparent Material")]
    public Material waterMaterial;

    [Space]

    [Tooltip("Generation type of the World. Onstart spawns World Instiantly, Enumerated delays every Chunk by 1 Frame")]
    public GenTypes generationtype;
    [Tooltip("Shadowtype of the Chunk Meshrenderer. If weird lines appear through the wall set it to fixedShadow")]
    public ShadowTypes shadowType;
    [Tooltip("Set how you want your Worldcollision")]
    public CollisionTypes collisionType;

    [Space]

    [Tooltip("The Widht of a Chunk. NEVER CHANGE ON PLAYTIME")]
    public int ChunkWidht = 16;
    [Tooltip("The Height of a Chunk. NEVER CHANGE ON PLAYTIME")]
    public int ChunkHeight = 70;
    [Tooltip("The Size of the World in Chunks. NEVER CHANGE ON PLAYTIME")]
    public int WorldSizeInChunks = 15;

    public int WorldSizeInVoxels { get { return WorldSizeInChunks * ChunkWidht; }}

    public static World Instance;

    Chunk[,] chunks;

    Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    [Space]

    [Tooltip("ALL BLOCKS NEED TO BE IN THIS LIST")]
    public List<Block> blocks = new List<Block>();

    Block[] orderedBlocks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        chunks = new Chunk[WorldSizeInChunks, WorldSizeInChunks];

        Random.InitState(seed);

        OrderBlocks();

        if (generationtype == GenTypes.OnStart)
            GenerateWorld();
        else
            StartCoroutine(_GenerateWorld());
    }

    void OrderBlocks ()
    {
        orderedBlocks = new Block[blocks.Count];

        for (int i = 0; i < blocks.Count; i++)
        {
            orderedBlocks[i] = blocks[i];
        }

        bool itemMoved;
        do
        {
            itemMoved = false;
            for (int i = 0; i < orderedBlocks.Length; i++)
            {
                if (i == orderedBlocks.Length - 1)
                {
                    if (orderedBlocks[i].blockID < orderedBlocks[i-1].blockID)
                    {
                        Block lowerValue = orderedBlocks[i - 1];
                        orderedBlocks[i - 1] = orderedBlocks[i];
                        orderedBlocks[i] = lowerValue;
                        itemMoved = true;
                    }
                }
                else if (orderedBlocks[i].blockID > orderedBlocks[i + 1].blockID)
                {
                    Block lowerValue = orderedBlocks[i + 1];
                    orderedBlocks[i + 1] = orderedBlocks[i];
                    orderedBlocks[i] = lowerValue;
                    itemMoved = true;
                }
            }
        } while (itemMoved);
    }

    void GenerateWorld()
    {
        for (int x = 0; x < WorldSizeInChunks; x++)
        {
            for (int z = 0; z < WorldSizeInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), true);
            }
        }

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoord c = GetChunkCoordFromV3I(v.position);

            if (IsVoxelInWorld(v.position))
                chunks[c.x, c.z].modifications.Enqueue(v);
        }

        for (int i = 0; i < WorldSizeInChunks; i++)
        {
            for (int p = 0; p < WorldSizeInChunks; p++)
            {
                chunks[i, p].UpdateChunk();
            }
        }
    }

    IEnumerator _GenerateWorld ()
    {
        for (int x = 0; x < WorldSizeInChunks; x++)
        {
            for (int z = 0; z < WorldSizeInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), true);

                yield return null;
            }
        }

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();

            ChunkCoord c = GetChunkCoordFromV3I(v.position);

            if (IsVoxelInWorld(v.position))
                chunks[c.x, c.z].modifications.Enqueue(v);
        }

        yield return null;

        for (int i = 0; i < WorldSizeInChunks; i++)
        {
            for (int p = 0; p < WorldSizeInChunks; p++)
            {
                chunks[i, p].UpdateChunk();
            }
        }
    }

    ChunkCoord GetChunkCoordFromV3I(Vector3Int pos)
    {
        int x = pos.x / ChunkWidht;
        int z = pos.z / ChunkWidht;
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromV3I(Vector3Int pos)
    {
        int x = Mathf.FloorToInt((float)pos.x / ChunkWidht);
        int z = Mathf.FloorToInt((float)pos.z / ChunkWidht);
        return chunks[x, z];
    }

    public byte GetVoxel(Vector3Int pos)
    {
        //Base
        if (!IsVoxelInWorld(pos))
            return 0;

        if (pos.y == 0)
            return 1;

        //Terrain
        int terrainheight = Mathf.FloorToInt(biome.maxTerrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainNoiseScale)) + biome.minTerrainHeight;
        byte voxelValue;

        if (pos.y == terrainheight)
            voxelValue = biome.TopLayerBlock.blockID;
        else if (pos.y < terrainheight && pos.y > terrainheight - biome.UnderTopLayerBlockDistance)
            voxelValue = biome.UnderTopLayerBlock.blockID;
        else if (pos.y > terrainheight)
            voxelValue = 0;
        else
            voxelValue = biome.BottomLayerBlock.blockID;

        if (biome.waterSettings.useSeaLevel && voxelValue == 0 && pos.y <= biome.waterSettings.sealevel)
            voxelValue = biome.waterSettings.seaBlock.blockID;

        //Lodes
        foreach (Lode lode in biome.lodes)
        {
            if (pos.y > lode.minHeight && pos.y < lode.maxHeight)
            {
                if (Noise.Get3DPerlin(pos, lode.offset, lode.scale, lode.threshold))
                {
                    bool fluid = GetBlockFromID(voxelValue).isFluid;

                    if (lode.LodeType == Biomes.LodeTypes.ReplaceAll)
                    {
                        voxelValue = lode.block.blockID;
                    }
                    else if (lode.LodeType == Biomes.LodeTypes.ReplaceGround)
                    {
                        if (voxelValue != 0 && !fluid)
                            voxelValue = lode.block.blockID;
                    }
                    else if (lode.LodeType == Biomes.LodeTypes.ReplaceFluid)
                    {
                        if (fluid)
                            voxelValue = lode.block.blockID;
                    }
                    else if (lode.LodeType == Biomes.LodeTypes.ReplaceFluidAndGround)
                    {
                        if (voxelValue != 0)
                            voxelValue = lode.block.blockID;
                    }
                }
            }
        }

        //Props
        if (pos.y == terrainheight && voxelValue != 0)
        {
            bool fluidCheck = false;

            bool onFluid = GetBlockFromID(voxelValue).isFluid;

            if (onFluid && biome.waterSettings.spawnPropsOnWater)
                fluidCheck = true;
            else if (!onFluid)
                fluidCheck = true;

            if (fluidCheck)
            {
                foreach (BlockProps bp in biome.blockProps)
                {
                    bool inHeight = false;

                    if (bp.useHeight)
                    {
                        if (pos.y > bp.minHeight && pos.y < bp.maxHeight)
                            inHeight = true;
                    }
                    else
                        inHeight = true;

                    if (inHeight)
                    {
                        if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), bp.offset, bp.ZoneScale) > bp.ZoneThreshold)
                        {
                            if (bp.replaceZoneWhitBlock)
                                voxelValue = bp.replaceBlock.blockID;

                            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), bp.offset + 52836, bp.PlacementScale) > bp.PlacementThreshold)
                            {
                                if (bp.GroundOffset == 0)
                                    voxelValue = bp.block.blockID;
                                else
                                    Structure.MakeBlock(new Vector3Int(pos.x, pos.y + bp.GroundOffset, pos.z), modifications, bp.block.blockID);
                            }
                        }
                    }
                }

                foreach (TreeProps tp in biome.treeProps)
                {
                    bool inHeight = false;

                    if (tp.useHeight)
                    {
                        if (pos.y > tp.minHeight && pos.y < tp.maxHeight)
                            inHeight = true;
                    }
                    else
                        inHeight = true;

                    if (inHeight)
                    {
                        if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), tp.offset, tp.ZoneScale) > tp.ZoneThreshold)
                        {
                            if (tp.replaceZoneWhitBlock)
                                voxelValue = tp.replaceBlock.blockID;

                            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), tp.offset + 52836, tp.PlacementScale) > tp.PlacementThreshold)
                            {
                                Structure.MakeTree(new Vector3Int(pos.x, pos.y, pos.z), modifications, tp.minTrunkHeight, tp.maxTrunkHeight, tp.treeTrunkBlock.blockID, tp.treeLeavesBlock.blockID);
                            }
                        }
                    }
                }

                foreach (CactusProps cp in biome.cactusProps)
                {
                    bool inHeight = false;

                    if (cp.useHeight)
                    {
                        if (pos.y > cp.minHeight && pos.y < cp.maxHeight)
                            inHeight = true;
                    }
                    else
                        inHeight = true;

                    if (inHeight)
                    {
                        if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), cp.offset, cp.ZoneScale) > cp.ZoneThreshold)
                        {
                            if (cp.replaceZoneWhitBlock)
                                voxelValue = cp.replaceBlock.blockID;

                            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), cp.offset + 52836, cp.PlacementScale) > cp.PlacementThreshold)
                            {
                                Structure.MakeCactus(new Vector3Int(pos.x, pos.y, pos.z), modifications, cp.minTrunkHeight, cp.maxTrunkHeight, cp.CactusBlock.blockID);
                            }
                        }
                    }
                }

                foreach (ModelProps mp in biome.modelProps)
                {
                    bool inHeight = false;

                    if (mp.useHeight)
                    {
                        if (pos.y > mp.minHeight && pos.y < mp.maxHeight)
                            inHeight = true;
                    }
                    else
                        inHeight = true;

                    if (inHeight)
                    {
                        if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), mp.offset, mp.ZoneScale) > mp.ZoneThreshold)
                        {
                            if (mp.replaceZoneWhitBlock)
                                voxelValue = mp.replaceBlock.blockID;

                            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), mp.offset + 52836, mp.PlacementScale) > mp.PlacementThreshold)
                            {
                                GameObject g = Instantiate(mp.Prefab, transform);
                                g.transform.position = new Vector3(pos.x, pos.y + mp.yOffset, pos.z);
                                g.transform.rotation = Quaternion.Euler(mp.modelRotation);
                                g.transform.localScale = mp.modelSize;
                            }
                        }
                    }
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < WorldSizeInChunks - 1 && coord.z > 0 && coord.z < WorldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    public bool IsVoxelInWorld(Vector3Int pos)
    {
        if (pos.x >= 0 && pos.x < WorldSizeInVoxels && pos.y >= 0 && pos.y < ChunkHeight && pos.z >= 0 && pos.z < WorldSizeInVoxels)
            return true;
        else
            return false;
    }

    public bool IsSolidHeightVoxel(Vector3Int pos)
    {
        if (!IsVoxelInWorld(pos))
            return false;

        if (GetChunkFromV3I(pos).isVoxelMapPopulated)
        {
            if (GetBlockFromID(GetChunkFromV3I(pos).GetVoxelFromGlobalVector3I(pos)).isSolid)
            {
                return false;
            }
            else
            {
                if (!IsVoxelInWorld(new Vector3Int(pos.x, pos.y - 1, pos.z)))
                    return false;

                if (GetBlockFromID(GetChunkFromV3I(pos).GetVoxelFromGlobalVector3I(new Vector3Int(pos.x, pos.y - 1, pos.z))).isSolid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        if (GetBlockFromID(GetVoxel(pos)).isSolid)
        {
            return false;
        }
        else
        {
            if (!IsVoxelInWorld(new Vector3Int(pos.x, pos.y - 1, pos.z)))
                return false;

            if (GetBlockFromID(GetVoxel(new Vector3Int(pos.x, pos.y - 1, pos.z))).isSolid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool CheckForVoxel(Vector3Int pos, bool selfFluid)
    {
        ChunkCoord thisChunk = GetChunkCoordFromV3I(pos);

        if (!IsVoxelInWorld(pos) || pos.y < 0 || pos.y > ChunkHeight) {
            if (biome.renderWorldBorder)
                return false;
            else
                return true;
        }

        byte b;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            b = chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3I(pos);
        }
        else
        {
            b = GetVoxel(pos);
        }

        if (GetBlockFromID(b).isSolid && (!GetBlockFromID(b).isFluid || selfFluid))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Block GetBlockFromID (byte ID)
    {
        try
        {
            return orderedBlocks[ID];
        }
        catch (System.Exception)
        {
            Debug.LogError("Error loading Block whit ID: " + ID);
        }

        return null;
    }
}

public class VoxelMod
{
    public Vector3Int position;
    public byte id;

    public VoxelMod(Vector3Int _pos, byte _id)
    {
        position = _pos;
        id = _id;
    }
}
