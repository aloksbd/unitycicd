using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TerrainEngine
{
    public class SlippyTilesHelper
    {
        public static string GetSlippyTilesNameByImage(CellMetrics terrainMapping, List<CellMetrics> coordinateList = null)
        {
            double XMin = terrainMapping.mercatorBounds.left;
            double YMin = terrainMapping.mercatorBounds.bottom;
            double XMax = terrainMapping.mercatorBounds.right;
            double YMax = terrainMapping.mercatorBounds.top;

            string name = ImageNamewithSlippyMap(XMax, YMax) + "-" +
                          ImageNamewithSlippyMap(XMin, YMin) + ".tiff"
                          .Replace("--", "-");

            if (coordinateList == null || 
                coordinateList.FirstOrDefault(x => x.slippyTileName == name) == null)
            {
                return name;
            }
            else
            {
                return "";
            }
        }

        public static string FileNameByMercator(Bounds2D mercatorBounds)
        {
            string name = ImageNamewithSlippyMap(mercatorBounds.right, mercatorBounds.top) + "-" +
                          ImageNamewithSlippyMap(mercatorBounds.left, mercatorBounds.bottom) + ".tiff"
                          .Replace("--", "-");
            return name;
        }

        private static string ImageNamewithSlippyMap(double lat, double lon)
        {
            int zoom = 16;
            int xtile = (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << zoom))); ;
            int ytile = (int)Math.Floor(
                (1 - Math.Log(Math.Tan(AreaBounds.DegreesToRadians(lat)) + 1 / Math.Cos(AreaBounds.DegreesToRadians(lat))) / Math.PI) 
                / 2 * (1 << zoom));
            string slippyTileDigits = zoom + "-" + xtile + "-" + ytile;
            return slippyTileDigits.Replace("--", "-");
        }
    }
}