using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Block", menuName = "Terrain/Block")]
public class Block : ScriptableObject
{
    public string blockName;
    public byte blockID;

    [Header("BlockInfo")]

    public Color color;

    public bool isSolid = true;
    public bool isFluid = false;
}
