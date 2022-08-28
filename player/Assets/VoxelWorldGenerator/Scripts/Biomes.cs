using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "Terrain/Biome")]
public class Biomes : ScriptableObject  
{
    public enum LodeTypes
    {
        ReplaceGround, ReplaceFluid, ReplaceFluidAndGround, ReplaceAll
    }

    [Header("BiomeInfo")]

    [Tooltip("Name of the biome")]
    public string biomeName;

    [Header("TerrainInfo")]

    [Tooltip("The minimum Height of the Terrain. NOT IN ABSOLUTE BLOCKS")]
    public int minTerrainHeight;
    [Tooltip("The maximum Height of the Terrain. NOT IN ABSOLUTE BLOCKS")]
    public int maxTerrainHeight;

    [Tooltip("The Terrain Perlin Noise Scale. Lower values = less Mountains, High values = more Mountains")]
    public float terrainNoiseScale;

    [Space]

    [Tooltip("Set if you want the edge of the World rendered")]
    public bool renderWorldBorder;

    [Header("Ground")]

    [Tooltip("The Block at the Top of the Terrain. f.ex.: Grass, Sand")]
    public Block TopLayerBlock;
    [Tooltip("The Block under the Top Block of the Terrain. f.ex.: Dirt")]
    public Block UnderTopLayerBlock;
    [Tooltip("The Distance of the Block under the Top Block from the Top Block")]
    public int UnderTopLayerBlockDistance = 4;
    [Tooltip("The Ground Block of the Terrain. f.ex.: Stone")]
    public Block BottomLayerBlock;

    [Header("Water")]

    public WaterSettings waterSettings;

    [Header("Props")]

    public BlockProps[] blockProps;

    [Space]

    public TreeProps[] treeProps;

    [Space]

    public CactusProps[] cactusProps;

    [Space]

    public ModelProps[] modelProps;

    [Space]

    [Header("Lodes")]

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    [Header("Block Info")]

    [Tooltip("The name of the Lode")]
    public string lodeName;
    [Tooltip("Lode will replace certain areas whit this block based on Perlin Noise")]
    public Block block;

    [Space]

    [Tooltip("The replacetype of the Lode")]
    public Biomes.LodeTypes LodeType;

    [Header("Noise")]

    [Tooltip("Noisescale: Lower values=less variation, Higher values=more variation")]
    public float scale = 1f;
    [Tooltip("Noise threshold: Lower values=more Blocks, higher values=less Blocks")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;
    [Tooltip("Offset: the offset from the original value to keep Noise variation")]
    public float offset;

    [Space]

    [Tooltip("The minimum Noise height in Blocks")]
    public int minHeight = 0;
    [Tooltip("The maximum Noise height in Blocks")]
    public int maxHeight = 100;
}

[System.Serializable]
public class BlockProps
{
    [Tooltip("The Block to place")]
    public Block block;
    [Tooltip("Offset: the offset from the original value to keep Noise variation")]
    public int offset;

    [Header("BlockZone")]

    [Tooltip("Check if you want to control the minimum and maximum Height")]
    public bool useHeight;
    [Tooltip("The minimum spawnheight in Blocks")]
    public int minHeight;
    [Tooltip("The maximum spawnheight in Blocks")]
    public int maxHeight;

    [Space]

    [Tooltip("The Zone Scale: lower values=less variation, high values=more variation")]
    public float ZoneScale = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("The Zone Threshold: lower values=bigger Zones, high values=smaller Zones")]
    public float ZoneThreshold = 0.4f;

    [Tooltip("Check if you want to replace the ground of the Zone whit a certain Block")]
    public bool replaceZoneWhitBlock = false;
    [Tooltip("The Block that replaces the Zoneground. Only work when replaceZoneWhitBlock is checked.")]
    public Block replaceBlock;

    [Header("BlockPlacement")]

    [Tooltip("The Placement Scale: lower values=less variation, high values=more variation")]
    public float PlacementScale = 15f;
    [Range(0f, 1f)]
    [Tooltip("The Placement Threshold: lower values=more Blocks, high values=less Blocks")]
    public float PlacementThreshold = 0.7f;

    [Header("BlockInfo")]

    [Tooltip("The offset from the ground in blocks: 0=in the ground")]
    public int GroundOffset;
}

[System.Serializable]
public class TreeProps
{
    [Tooltip("Offset: the offset from the original value to keep Noise variation")]
    public int offset;

    [Tooltip("The trunk block of the tree. f.ex.: Wood")]
    public Block treeTrunkBlock;
    [Tooltip("The leave block of the tree")]
    public Block treeLeavesBlock;

    [Header("TreeZone")]

    [Tooltip("Check if you want to control the minimum and maximum Height")]
    public bool useHeight;
    [Tooltip("The minimum spawnheight in Blocks")]
    public int minHeight;
    [Tooltip("The maximum spawnheight in Blocks")]
    public int maxHeight;

    [Space]

    [Tooltip("The Zone Scale: lower values=less variation, high values=more variation")]
    public float ZoneScale = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("The Zone Threshold: lower values=bigger Zones, high values=smaller Zones")]
    public float ZoneThreshold = 0.4f;

    [Tooltip("Check if you want to replace the ground of the Zone whit a certain Block")]
    public bool replaceZoneWhitBlock = false;
    [Tooltip("The Block that replaces the Zoneground. Only work when replaceZoneWhitBlock is checked.")]
    public Block replaceBlock;

    [Header("TreePlacement")]

    [Tooltip("The Placement Scale: lower values=less variation, high values=more variation")]
    public float PlacementScale = 15f;
    [Range(0f, 1f)]
    [Tooltip("The Placement Threshold: lower values=more Trees, high values=less Trees")]
    public float PlacementThreshold = 0.7f;

    [Header("TreeTrunk")]

    [Tooltip("minimum tree trunkheight. NOT IN BLOCKS")]
    public int minTrunkHeight = 4;
    [Tooltip("maximum tree trunkheight. NOT IN BLOCKS")]
    public int maxTrunkHeight = 6;
}

[System.Serializable]
public class CactusProps
{
    [Tooltip("Offset: the offset from the original value to keep Noise variation")]
    public int offset;

    [Tooltip("The Cactus block")]
    public Block CactusBlock;

    [Header("CactusZone")]

    [Tooltip("Check if you want to control the minimum and maximum Height")]
    public bool useHeight;
    [Tooltip("The minimum spawnheight in Blocks")]
    public int minHeight;
    [Tooltip("The maximum spawnheight in Blocks")]
    public int maxHeight;

    [Space]

    [Tooltip("The Zone Scale: lower values=less variation, high values=more variation")]
    public float ZoneScale = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("The Zone Threshold: lower values=bigger Zones, high values=smaller Zones")]
    public float ZoneThreshold = 0.4f;

    [Tooltip("Check if you want to replace the ground of the Zone whit a certain Block")]
    public bool replaceZoneWhitBlock = false;
    [Tooltip("The Block that replaces the Zoneground. Only work when replaceZoneWhitBlock is checked.")]
    public Block replaceBlock;

    [Header("CactusPlacement")]

    [Tooltip("The Placement Scale: lower values=less variation, high values=more variation")]
    public float PlacementScale = 15f;
    [Range(0f, 1f)]
    [Tooltip("The Placement Threshold: lower values=more Cactus, high values=less Cactus")]
    public float PlacementThreshold = 0.7f;

    [Header("CactusHeight")]

    [Tooltip("minimum cactus height. NOT IN BLOCKS")]
    public int minTrunkHeight = 4;
    [Tooltip("maximum cactus height. NOT IN BLOCKS")]
    public int maxTrunkHeight = 6;
}

[System.Serializable]
public class ModelProps
{
    [Tooltip("The Prefab of the Model")]
    public GameObject Prefab;
    [Tooltip("Offset: the offset from the original value to keep Noise variation")]
    public int offset;

    [Header("ModelZone")]

    [Tooltip("Check if you want to control the minimum and maximum Height")]
    public bool useHeight;
    [Tooltip("The minimum spawnheight in Blocks")]
    public int minHeight;
    [Tooltip("The maximum spawnheight in Blocks")]
    public int maxHeight;

    [Space]

    [Tooltip("The Zone Scale: lower values=less variation, high values=more variation")]
    public float ZoneScale = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("The Zone Threshold: lower values=bigger Zones, high values=smaller Zones")]
    public float ZoneThreshold = 0.4f;

    [Tooltip("Check if you want to replace the ground of the Zone whit a certain Block")]
    public bool replaceZoneWhitBlock = false;
    [Tooltip("The Block that replaces the Zoneground. Only work when replaceZoneWhitBlock is checked.")]
    public Block replaceBlock;

    [Header("ModelPlacement")]

    [Tooltip("The Placement Scale: lower values=less variation, high values=more variation")]
    public float PlacementScale = 15f;
    [Range(0f, 1f)]
    [Tooltip("The Placement Threshold: lower values=more Models, high values=less Models")]
    public float PlacementThreshold = 0.7f;

    [Header("ModelInfo")]

    [Tooltip("The Size of the model prefab")]
    public Vector3 modelSize = Vector3.one;
    [Tooltip("The Rotation of the model prefab")]
    public Vector3 modelRotation = Vector3.zero;

    [Tooltip("The Offset from the Ground")]
    public int yOffset;
}

[System.Serializable]
public class WaterSettings
{
    [Tooltip("Spawn Props(Trees, Cactus,..) on water")]
    public bool spawnPropsOnWater = false;

    [Space]

    [Tooltip("Using sea level will replace air under the sealevel whit water")]
    public bool useSeaLevel;
    [Tooltip("The sealevel: The air under sealevel will be replaced whit water")]
    public int sealevel;
    [Tooltip("The Block you want to have in your sea. f.ex.: Water, Lava")]
    public Block seaBlock;
}