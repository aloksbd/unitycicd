using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        position.x += (offset + World.Instance.seed + 0.1f);
        position.y += (offset + World.Instance.seed + 0.1f);

        return Mathf.PerlinNoise(position.x / World.Instance.ChunkWidht * scale, position.y / World.Instance.ChunkWidht * scale);

    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + offset + World.Instance.seed + 0.1f) * scale;
        float y = (position.y + offset + World.Instance.seed + 0.1f) * scale;
        float z = (position.z + offset + World.Instance.seed + 0.1f) * scale;

        float noise = PerlinNoise3D(x, y, z);

        if (noise > threshold)
            return true;
        else
            return false;

    }

    public static float PerlinNoise3D(float x, float y, float z)
    {
        y += 1;
        z += 2;
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);
        return (xy + xz + yz + yx + zx + zy) / 6;
    }
}
