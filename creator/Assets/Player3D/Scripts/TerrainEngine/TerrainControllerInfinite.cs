using UnityEngine;
using System.Collections.Generic;
using MEC;

namespace TerrainEngine
{
    public class TerrainInfiniteController : MonoBehaviour
    {
        private GameObject PlayerObject;

        private int _gridWidth;
        private int _gridHeight;
        public static Terrain[,] _grid;

        private TerrainController runTime;

        static float centerOffset;
        int chunks;

        public static bool northDetected = false;
        public static bool southDetected = false;
        public static bool eastDetected = false;
        public static bool westDetected = false;
        public static List<string> s_northTerrains;
        public static List<string> s_southTerrains;
        public static List<string> s_eastTerrains;
        public static List<string> s_westTerrains;
        public static List<Terrain> s_northTerrainsNeighbor;
        public static List<Terrain> s_southTerrainsNeighbor;
        public static List<Terrain> s_eastTerrainsNeighbor;
        public static List<Terrain> s_westTerrainsNeighbor;


        public static List<int> s_northIndexes;
        public static int northIndex;
        public static int southIndex;
        public static int eastIndex;
        public static int westIndex;


        public static List<int> s_northIndexImagery;
        public static List<int> s_southIndexImagery;
        public static List<int> s_eastIndexImagery;
        public static List<int> s_westIndexImagery;

        public static bool inProgressNorth;
        public static bool inProgressSouth;
        public static bool inProgressEast;
        public static bool inProgressWest;
        public static bool hybridNorth;
        public static bool hybridSouth;
        public static bool hybridEast;
        public static bool hybridWest;
        public static bool isOneStepNorth;
        public static bool isOneStepSouth;
        public static bool isOneStepEast;
        public static bool isOneStepWest;

        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            PlayerObject = TerrainPlayer.Get(gameObject).gameObject;

            runTime = TerrainController.Get();
            _gridWidth = (int)runTime.terrainGridSize;
            _gridHeight = (int)runTime.terrainGridSize;
            _grid = new Terrain[_gridWidth, _gridHeight];
            chunks = (_gridWidth * _gridHeight);

            int counter = 0;

            if (transform.childCount == _gridWidth * _gridHeight)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        _grid[y, x] = transform.GetChild(counter).GetComponent<Terrain>();
                        counter++;
                    }
                }
            }

            centerOffset = _grid[0, 0].terrainData.size.x / 2f;

            s_northTerrains = new List<string>();
            s_southTerrains = new List<string>();
            s_eastTerrains = new List<string>();
            s_westTerrains = new List<string>();

            northIndex = 0;
            southIndex = chunks - _gridWidth;
            eastIndex = _gridWidth - 1;
            westIndex = 0;

            s_northIndexes = new List<int>();
        }

        public void UnloadAllAssets()
        {
            PlayerObject = null;

            _gridWidth = 0;
            _gridHeight = 0;
            _grid = null;

            runTime = null;

            centerOffset = 0;
            chunks = 0;

            northDetected = false;
            southDetected = false;
            eastDetected = false;
            westDetected = false;

            s_northTerrains = null;
            s_southTerrains = null;
            s_eastTerrains = null;
            s_westTerrains = null;
            s_northTerrainsNeighbor = null;
            s_southTerrainsNeighbor = null;
            s_eastTerrainsNeighbor = null;
            s_westTerrainsNeighbor = null;

            s_northIndexes = null;
            northIndex = 0;
            southIndex = 0;
            eastIndex = 0;
            westIndex = 0;

            s_northIndexImagery = null;
            s_southIndexImagery = null;
            s_eastIndexImagery = null;
            s_westIndexImagery = null;

            inProgressNorth = false;
            inProgressSouth = false;
            inProgressEast = false;
            inProgressWest = false;
            hybridNorth = false;
            hybridSouth = false;
            hybridEast = false;
            hybridWest = false;
            isOneStepNorth = false;
            isOneStepSouth = false;
            isOneStepEast = false;
            isOneStepWest = false;
        }

        void Update()
        {
            if (_grid == null)
            {
                return;
            }

            if (s_northTerrains.Count > 0) inProgressNorth = true; else inProgressNorth = false;
            if (s_southTerrains.Count > 0) inProgressSouth = true; else inProgressSouth = false;
            if (s_eastTerrains.Count > 0) inProgressEast = true; else inProgressEast = false;
            if (s_westTerrains.Count > 0) inProgressWest = true; else inProgressWest = false;

            if (s_northTerrains.Count <= _gridWidth) isOneStepNorth = true; else isOneStepNorth = false;
            if (s_southTerrains.Count <= _gridWidth) isOneStepSouth = true; else isOneStepSouth = false;
            if (s_eastTerrains.Count <= _gridWidth) isOneStepEast = true; else isOneStepEast = false;
            if (s_westTerrains.Count <= _gridWidth) isOneStepWest = true; else isOneStepWest = false;

            Vector3 playerPosition = new Vector3(PlayerObject.transform.position.x, PlayerObject.transform.position.y, PlayerObject.transform.position.z);
            Terrain playerTerrain = null;
            int xOffset = 0;
            int yOffset = 0;

            northDetected = false;
            southDetected = false;
            eastDetected = false;
            westDetected = false;

            //foreach(Terrain t in TerraLand.TerrainRuntime.terrainsInProgress)
            //t.drawHeightmap = false;

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if ((playerPosition.x >= _grid[x, y].transform.position.x + centerOffset) &&
                        (playerPosition.x <= (_grid[x, y].transform.position.x + _grid[x, y].terrainData.size.x) + centerOffset) &&
                        (playerPosition.z >= _grid[x, y].transform.position.z - centerOffset) &&
                        (playerPosition.z <= (_grid[x, y].transform.position.z + _grid[x, y].terrainData.size.z) - centerOffset))
                    {
                        playerTerrain = _grid[x, y];
                        xOffset = ((_gridWidth - 1) / 2) - x;
                        yOffset = ((_gridHeight - 1) / 2) - y;
                        break;
                    }
                }

                if (playerTerrain != null)
                    break;
            }

            if (runTime.IsInState(TerrainController.TerrainState.WorldIsGenerated) &&
                playerTerrain != _grid[(_gridWidth - 1) / 2, (_gridHeight - 1) / 2])
            {
                Terrain[,] newGrid = new Terrain[_gridWidth, _gridHeight];

                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        int newX = x + xOffset;

                        // Moving EAST
                        if (newX < 0)
                        {
                            newX = _gridWidth - 1;
                            _grid[x, y].drawHeightmap = false;
                            Timing.RunCoroutine(GenerateEAST(x, y));
                        }

                        //Moving WEST
                        else if (newX > (_gridWidth - 1))
                        {
                            newX = 0;
                            _grid[x, y].drawHeightmap = false;
                            Timing.RunCoroutine(GenerateWEST(x, y));
                        }

                        int newY = y + yOffset;

                        //Moving SOUTH
                        if (newY < 0)
                        {
                            newY = _gridHeight - 1;
                            _grid[x, y].drawHeightmap = false;
                            Timing.RunCoroutine(GenerateSOUTH(x, y));
                        }

                        //Moving NORTH
                        else if (newY > (_gridHeight - 1))
                        {
                            newY = 0;
                            _grid[x, y].drawHeightmap = false;
                            Timing.RunCoroutine(GenerateNORTH(x, y));
                        }

                        newGrid[newX, newY] = _grid[x, y];
                    }
                }

                _grid = newGrid;
                UpdatePositions();
            }
        }

        private IEnumerator<float> GenerateNORTH(int x, int y)
        {
            if (s_northTerrains.Count == 0)
                s_northIndexImagery = new List<int>();

            string terrainGridName = _grid[x, y].name;
            s_northTerrains.Add(terrainGridName);
            s_northIndexImagery.Add(int.Parse(_grid[x, y].name.Split(new char[] { ' ' })[0]) - 1);

            if (!northDetected)
            {
                TerrainRuntime.GetTerrainBoundsNORTH(terrainGridName);
                northDetected = true;
            }

            yield return 0;
        }

        private IEnumerator<float> GenerateSOUTH(int x, int y)
        {
            if (s_southTerrains.Count == 0)
                s_southIndexImagery = new List<int>();
             
            string terrainGridName = _grid[x, y].name;
            s_southTerrains.Add(terrainGridName);
            s_southIndexImagery.Add(int.Parse(_grid[x, y].name.Split(new char[] { ' ' })[0]) - 1);

            if (!southDetected)
            {
                TerrainRuntime.GetTerrainBoundsSOUTH(terrainGridName);
                southDetected = true;
            }

            yield return 0;
        }

        private IEnumerator<float> GenerateEAST(int x, int y)
        {
            if (s_eastTerrains.Count == 0)
                s_eastIndexImagery = new List<int>();
            string terrainGridName = _grid[x, y].name; 
            s_eastTerrains.Add(terrainGridName);
            s_eastIndexImagery.Add(int.Parse(_grid[x, y].name.Split(new char[] { ' ' })[0]) - 1);

            if (!eastDetected)
            {
                TerrainRuntime.GetTerrainBoundsEAST(terrainGridName);
                eastDetected = true;
            }

            yield return 0;
        }

        private IEnumerator<float> GenerateWEST(int x, int y)
        {
            if (s_westTerrains.Count == 0)
                s_westIndexImagery = new List<int>();

            string terrainGridName = _grid[x, y].name;
            s_westTerrains.Add(terrainGridName);
            s_westIndexImagery.Add(int.Parse(_grid[x, y].name.Split(new char[] { ' ' })[0]) - 1);

            if (!westDetected)
            {
                TerrainRuntime.GetTerrainBoundsWEST(terrainGridName);
                westDetected = true;
            }

            yield return 0;
        }

        private void UpdatePositions()
        {
            Terrain middle = _grid[(_gridWidth - 1) / 2, (_gridHeight - 1) / 2];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (!(x.Equals((_gridWidth - 1) / 2) && y.Equals((_gridHeight - 1) / 2)))
                    {
                        int xOffset = ((_gridWidth - 1) / 2) - x;
                        int yOffset = ((_gridHeight - 1) / 2) - y;

                        _grid[x, y].transform.position = new Vector3
                        (
                            middle.transform.position.x - (middle.terrainData.size.x * xOffset),
                            middle.transform.position.y,
                            middle.transform.position.z + (middle.terrainData.size.z * yOffset)
                        );
                    }
                }
            }

            northDetected = false;
            southDetected = false;
            eastDetected = false;
            westDetected = false;
        }
    }
}

