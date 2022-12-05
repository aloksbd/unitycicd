using System;
using UnityEngine;

namespace TerrainEngine
{
    public class GeoLocation
    {
        public class Config
        {
            //  WGS84 bounds
            public Wgs84Bounds wgs84Bounds;

            //  Area extents (1 world unit = 1 unity unit = 1 meter)
            public double worldAreaWidth;
            public double worldAreaHeight;
            public double worldScaleFactor = 1;
        }

        public static Vector3 Locate(
            double targetLatitude,
            double targetLongitude, 
            ref Config config)
        {
            return Locate(
                targetLatitude,
                targetLongitude,
                ref config.wgs84Bounds,
                config.worldAreaWidth,
                config.worldAreaHeight,
                config.worldScaleFactor);
        }

        public static Vector3 Locate(
            double targetLatitude,
            double targetLongitude,
            ref Wgs84Bounds wgs84Bounds,
            double worldAreaWidth,
            double worldAreaHeight,
            double worldScaleFactor)
        {
            //  Parameter conversions...
            //
            //  Mercator coordinates from latitude
            double mercatorLeft = AreaBounds.LongitudeToMercator(wgs84Bounds.left);
            double mercatorTop = AreaBounds.LatitudeToMercator(wgs84Bounds.top);
            double mercatorRight = AreaBounds.LongitudeToMercator(wgs84Bounds.right);
            double mercatorBottom = AreaBounds.LatitudeToMercator(wgs84Bounds.bottom);
            double mercatorHeight = Math.Abs(mercatorTop - mercatorBottom);
            double mercatorWidth = Math.Abs(mercatorLeft - mercatorRight);
            //
            //  Unity terrain coordinates (1 meter = 1 unity coord unit)
            double worldAreaScaledWidth = worldAreaWidth * worldScaleFactor;
            double worldAreaScaledHeight = worldAreaHeight * worldScaleFactor;

            //  Computations...
            double mercatorTargetLat = AreaBounds.LatitudeToMercator(targetLatitude);
            double mercatorTargetLon = AreaBounds.LongitudeToMercator(targetLongitude);

            double[] latlonDeltaNormalized = AreaBounds.GetNormalizedDelta(
                mercatorTargetLat,
                mercatorTargetLon,
                mercatorTop, mercatorLeft, mercatorHeight, mercatorWidth);

            Vector2d worldPositionXZ = AreaBounds.GetWorldPositionFromTile(
                latlonDeltaNormalized[0], latlonDeltaNormalized[1], worldAreaScaledHeight, worldAreaScaledWidth);

            Vector3d worldPosition = new Vector3d(worldPositionXZ.x + worldAreaScaledHeight / 2, 0, worldPositionXZ.y - worldAreaScaledWidth / 2);

            return (Vector3)worldPosition;
        }
    }
}