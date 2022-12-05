using System.Collections.Generic;
using UnityEngine;  // Mathf

namespace TerrainEngine
{
    //
    //  CellMetrics - retains metrics and metrics-derived metadata for one cell withing a terrain grid.
    //
    public class CellMetrics
    {
        public int key { get; set; }            // array index value (why use a dictionary?)

        //  Bounding box in metric (unity coordinates), Mercator, and WGS84 units
        public Bounds2D worldBounds;            // world (unity) bounding box
        public Bounds2D mercatorBounds;         // mercator bounding box 
        public Wgs84Bounds wgs84Bounds;         // latitude/longitude bounding box

        public float imageOffsetX;              // x-offset of base image in meter
        public float imageOffsetY;              // y-offset of base image in meters
        public int imageFileHeight;             // base image height, as read from file
        public int imageFileWidth;              // base image width, as read from file

        public string slippyTileName;           // cache file name for heightmaps and base images
        public string terrainGridName;          // name of the grid to which the cell belongs
    }

    //
    //  GridMetrics - retains metrics and metrics-derived metadata for an entire terrain grid.
    //
    public class GridMetrics
    {
        //  Grid layout
        public float spanX_km;          // total projection width of grid in kilometers
        public float spanY_km;          // total projection height of grid in kilometers
        public float spanX_m;           // total projection width of terrain grid in meters
        public float spanY_m;           // total vertical height of terrain grid in meters
        public float spanZ_m;           // total projection height of terrain grid in meters

        public int cellCountX;          // number of cells in X dimension
        public int cellCountY;          // number of cells in Y dimension
        public int cellCountEdge;       // TODO: deprecate; assumes a square grid (cellCountX == cellCountY == cellCountEdge)
        public float cellSpanX;         // width of each cell in kilometers
        public float cellSpanY;         // width of each cell in kilometers
        public int cellCount;           // total number of cells: cellCountX * cellCountY
        public int cellsPerTerrain = 1; // number of grid cells per unity terrain

        //  Heightmap metrics
        public int heightMapResolution; // pixel resolution per heightmap
        public int splitSizeFinal;
        public int gridSizeTerrain;
        public int heightmapResolutionSplit;
        public float spanAspectRatio;

        //  Base texture metrics
        public int baseImagesTotal;
        public int baseImagesPerEdge;
        public double mercatorBaseImageWidth;
        public double mercatorBaseImageHeight;
        public float baseImageSpanX;
        public float baseImageSpanY;

        public List<CellMetrics> cellMetrics;

        //  Bounding box in world (unity), Mercator, and WGS84 units
        public Bounds2D worldBounds;
        public Bounds2D mercatorBounds;
        public Wgs84Bounds wgs84Bounds;

        private List<double> cellLatitidudes;
        private List<double> cellLongitudes;

        public void InitializeArea(
            double latitideCenterPt, double longitudeCenterPt,
            double spanX_km /* km */, double spanY_km /* km */,
            int tileCountX, int tileCountY)
        {
            this.spanY_km = (float)spanX_km;
            this.spanX_km = (float)spanY_km;
            this.spanAspectRatio = this.spanY_km / this.spanX_km;
            Trace.Assert(this.spanAspectRatio == 1.0f, "For now, the area span must be a square.");

            this.spanX_m = this.spanX_km * 1000f;
            this.spanY_m = 4000;
            this.spanZ_m = this.spanX_m * this.spanAspectRatio;

            this.worldBounds = new Bounds2D()
            {
                top = spanZ_m / 2,
                left = -spanX_m / 2,
                bottom = -spanZ_m / 2,
                right = spanX_m / 2
            };

            Trace.Assert(tileCountX == tileCountY, "For now, the grid must be a square.");
            this.cellCountX = tileCountX;
            this.cellCountY = tileCountY;
            this.cellCountEdge = this.cellCountX;
            this.cellCount = this.cellCountX * this.cellCountY;

            this.cellSpanX = this.spanX_km / this.cellCountX;
            this.cellSpanY = this.spanY_km / this.cellCountY;
            Trace.Assert(this.cellSpanX == this.cellSpanY, "Cell span must be a square.");

            this.wgs84Bounds = new Wgs84Bounds();

            AreaBounds.Wgs84CenterPtToBBox(
                latitideCenterPt, longitudeCenterPt,
                this.spanY_km, this.spanX_km,
                out this.wgs84Bounds.top, out this.wgs84Bounds.left,
                out this.wgs84Bounds.bottom, out this.wgs84Bounds.right);

            AreaBounds.Wgs84CenterPtToTileGrid(
                latitideCenterPt, longitudeCenterPt,
                this.cellSpanX, this.cellCountX, this.cellCountY,
                out this.cellLatitidudes, out this.cellLongitudes);

            mercatorBounds = this.wgs84Bounds.ToMercator();

            cellMetrics = new List<CellMetrics>();
        }

        public void InitializeHeightMapMetrics(
            int areaHeightmapResolution)
        {
            Trace.Assert(this.cellCount > 0 && this.cellCountEdge > 0, "InitializeArea() must be invoked before InitializeHeightMapMetrics().");
            this.heightMapResolution = (areaHeightmapResolution / this.cellCountEdge) + 1;
            this.heightmapResolutionSplit = areaHeightmapResolution / (int)Mathf.Sqrt((float)this.cellCount);
            this.splitSizeFinal = (int)Mathf.Sqrt(this.cellCount);
        }

        public void InitializeBaseImageMetrics()
        {
            Trace.Assert(this.cellCount > 0, "InitializeArea() must be invoked before InitializeBaseImageMetrics().");
            this.baseImagesTotal = (int)(Mathf.Pow(this.cellsPerTerrain, 2)) * this.cellCount;
            this.baseImagesPerEdge = (int)(Mathf.Sqrt(this.baseImagesTotal));
            this.mercatorBaseImageHeight = this.mercatorBounds.Height / (double)this.baseImagesPerEdge;
            this.mercatorBaseImageWidth = this.mercatorBounds.Width / (double)this.baseImagesPerEdge;
            this.baseImageSpanX = this.cellSpanX / (float)this.baseImagesPerEdge;   //  texture span width in meters
            this.baseImageSpanY = this.cellSpanY / (float)this.baseImagesPerEdge;   //  texture span height in meters
        }

        public void InitializeCellMetrics(string gridName)
        {
            Trace.Assert(gridName != null && gridName != "", "gridName argument cannot be empty");
            int index = 0;

            for (int i = 0; i < this.baseImagesPerEdge; i++) // rows
            {
                for (int j = 0; j < this.baseImagesPerEdge; j++) // columns
                {
                    double worldTop = this.worldBounds.top - (this.cellSpanY * 1000.0d * (double)i);
                    double worldLeft = this.worldBounds.left + (this.cellSpanX * 1000.0d * (double)j);

                    Bounds2D worldBounds = new Bounds2D()
                    {
                        top = worldTop,
                        bottom = worldTop - (cellSpanY * 1000.0f),
                        left = worldLeft,
                        right = worldLeft + (cellSpanX * 1000.0f)
                    };

                    Wgs84Bounds wgs84 = new Wgs84Bounds()
                    {
                        top = cellLatitidudes[i],
                        bottom = cellLatitidudes[i + 1],
                        left = cellLongitudes[j],
                        right = cellLongitudes[j + 1]
                    };

                    double mercatorTop = this.mercatorBounds.top - (this.mercatorBaseImageHeight * (double)i);
                    double mercatorLeft = this.mercatorBounds.left + (this.mercatorBaseImageWidth * (double)j);

                    Bounds2D mercator = new Bounds2D()
                    {
                        top = mercatorTop,
                        left = mercatorLeft,
                        bottom = mercatorTop - this.mercatorBaseImageHeight,
                        right = mercatorLeft + this.mercatorBaseImageWidth,
                    };

                    float offsetX = (this.cellSpanX - (this.baseImageSpanX * ((float)this.baseImagesPerEdge - (float)j))) * -1f;
                    float offsetY = (this.cellSpanY - this.baseImageSpanY - ((float)this.baseImageSpanY * (float)i)) * -1f;

                    CellMetrics cell = new CellMetrics()
                    {
                        key = index,
                        worldBounds = worldBounds,
                        mercatorBounds = mercator,
                        wgs84Bounds = wgs84,
                        imageOffsetX = offsetX,
                        imageOffsetY = offsetY,
                        slippyTileName = SlippyTilesHelper.FileNameByMercator(mercator),
                        terrainGridName = gridName
                    };

                    if (cell.slippyTileName != "")
                    {
                        cellMetrics.Add(cell);
                    }

                    Trace.Log(TerrainController.tileDiagnostics,
                        "Terrain wanted: Index {0}: Filename: '{1}'",
                        index,
                        cell.slippyTileName);

                    index++;
                }
            }
        }

        public int GetBoundingCellIndex(Vector3 position)
        {
            for (int i = 0; i < cellMetrics.Count; i++)
            {
                if (position.x > cellMetrics[i].worldBounds.left && 
                    position.z < cellMetrics[i].worldBounds.top &&
                    position.x < cellMetrics[i].worldBounds.right &&
                    position.z > cellMetrics[i].worldBounds.bottom)
                {
                    return i;
                }
            }

            return -1;
        }

        public Vector3 GeoLocate(
            double targetLatitude,
            double targetLongitude)
        {
            return GeoLocation.Locate(
                targetLatitude,
                targetLongitude,
                ref wgs84Bounds,
                spanX_km * 1000,
                spanY_km * 1000,
                1.0d);
        }
    }
}