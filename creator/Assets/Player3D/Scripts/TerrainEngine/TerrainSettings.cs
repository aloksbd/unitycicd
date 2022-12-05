using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSettings
{
    //  From UI
    public bool updated = false;

    //  Location
    public string latitudeUser = "";
    public string longitudeUser = "";

    public enum gridSizeOption
    {
        _1 = 1,
        _2x2 = 2,
        _4x4 = 4,
        _8x8 = 8,
        _16x16 = 16,
        _32x32 = 32,
        _64x64 = 64,

        _valid = (gridSizeOption._1 |
                  gridSizeOption._2x2 |
                  gridSizeOption._4x4 |
                  gridSizeOption._8x8 |
                  gridSizeOption._16x16 |
                  gridSizeOption._32x32 |
                  gridSizeOption._64x64)
    }

    //  Terrain configuration
    public gridSizeOption terrainGridSize = gridSizeOption._4x4;
    public bool SetGridSize(string gridSizeAsString)
    { 
        terrainGridSize = (gridSizeOption)Enum.Parse(typeof(gridSizeOption), gridSizeAsString);
        return (terrainGridSize & gridSizeOption._valid) != 0;
    }

    public float    areaSize = 25f;
    public int      heightmapResolution = 1024;
    public int      imageResolution = 1024;
    public float    elevationExaggeration = 1.4f;
    public int      smoothIterations = 1;
    public bool     farTerrain = true;
    public int      farTerrainHeightmapResolution = 512;
    public int      farTerrainImageResolution = 1024;
    public float    areaSizeFarMultiplier = 4f;

    // Performance Settings
    public float    heightmapPixelError = 10f;
    public float    farTerrainQuality = 10f;
    public int      cellSize = 64;
    public int      concurrentTasks = 4;
    public float    elevationDelay = 0.5f;
    public float    imageryDelay = 0.5f;

    // Advanced Settings
    public bool     elevationOnly = false;
    public bool     fastStartBuild = true;
    public bool     showTileOnFinish = true;
    public bool     progressiveTexturing = true;
    public bool     spiralGeneration = true;
    public bool     delayedLOD = false;

    [HideInInspector] public bool   IsCustomGeoServer = false;
    [HideInInspector] public bool   progressiveGeneration = false;
    [HideInInspector] public float  terrainDistance;
    [HideInInspector] public float  terrainCurvator;
    [HideInInspector] public int    farTerrainCellSize;
    public float    farTerrainBelowHeight = 100f;

    public bool     stitchTerrainTiles = true;
    [Range(5, 100)] public int levelSmooth = 5;
    [Range(1, 7)]   public int power = 1;
    public bool     trend = false;
    public int      stitchDistance = 4;
    public float    stitchDelay = 0.25f;
}
