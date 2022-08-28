using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static void MakeBlock (Vector3Int pos, Queue<VoxelMod> queue, byte blockID)
    {
        queue.Enqueue(new VoxelMod(new Vector3Int(pos.x, pos.y, pos.z), blockID));
    }

    public static void MakeTree(Vector3Int pos, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight, byte treeTrunk, byte TreeLeaves)
    {
        for (int i = 0; i < 8; i++)
        {
            VoxelMod v = new VoxelMod(pos + VoxelData.treeChecks[i], 6);

            if (queue.Contains(v))
                return;
        }

        int TrunkHeight = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.y), 250, 3));

        if (TrunkHeight < minTrunkHeight)
            TrunkHeight = minTrunkHeight;

        for (int i = 1; i < TrunkHeight; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3Int(pos.x, pos.y + i, pos.z), treeTrunk));
        }

        for (int x = -1; x < 2; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = -1; z < 2; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3Int(pos.x + x, pos.y + TrunkHeight - 1 + y, pos.z + z), TreeLeaves));
                }
            }
        }
    }

    public static void MakeCactus(Vector3Int pos, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight, byte Cactus)
    {
        for (int i = 0; i < 8; i++)
        {
            VoxelMod v = new VoxelMod(pos + VoxelData.treeChecks[i], 6);

            if (queue.Contains(v))
                return;
        }

        int TrunkHeight = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.y), 250, 3));

        if (TrunkHeight < minTrunkHeight)
            TrunkHeight = minTrunkHeight;

        for (int i = 1; i < TrunkHeight; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3Int(pos.x, pos.y + i, pos.z), Cactus));
        }
    }
}
