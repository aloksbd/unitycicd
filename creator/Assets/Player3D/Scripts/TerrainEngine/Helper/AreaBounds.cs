using System;
using System.Collections.Generic;

namespace TerrainEngine
{
    public class Bounds2D
    {
        public double top;
        public double left;
        public double bottom;
        public double right;

        public Bounds2D() { }

        public Bounds2D(Bounds2D other)
        {
            this.top = other.top;
            this.left = other.left;
            this.bottom = other.bottom;
            this.right = other.right;
        }

        public double Width
        {
            get { return Math.Abs(this.right - this.left); }
            set { this.right = this.left + value; }
        }

        public double Height
        {
            get { return Math.Abs(this.top - this.bottom); }
            set { this.bottom = this.top + value; }
        }

        public void Offset(double offsetX, double offsetY)
        {
            this.top += offsetY;
            this.bottom += offsetY;
            this.left += offsetX;
            this.right += offsetX;
        }
    }

    public class Wgs84Bounds : Bounds2D
    {
        public Wgs84Bounds() 
        {
            top = GeoConst.INVALID_LATITUDE;
            left = GeoConst.INVALID_LONGITUDE;
            bottom = GeoConst.INVALID_LATITUDE;
            right = GeoConst.INVALID_LATITUDE;
        }

        public Wgs84Bounds(Wgs84Bounds other)
        {
            this.top = other.top;
            this.left = other.left;
            this.bottom = other.bottom;
            this.right = other.right;
        }

        public void OffsetLatitude(
            double degrees)
        {
            Offset(0, degrees);
        }

        public void OffsetLongitude(
            double degrees)
        {
            Offset(degrees, 0);
        }

        public Bounds2D ToMercator()
        {
            Bounds2D mercatorBounds = new Bounds2D();
            Trace.Assert(
                this.top != GeoConst.INVALID_LATITUDE &&
                this.left != GeoConst.INVALID_LONGITUDE &&
                this.bottom != GeoConst.INVALID_LATITUDE &&
                this.right != GeoConst.INVALID_LONGITUDE,
                "Cannot convert invalid WGS84 values to mercator");
            
            mercatorBounds.top = AreaBounds.LatitudeToMercator(this.top);
            mercatorBounds.left = AreaBounds.LongitudeToMercator(this.left);
            mercatorBounds.bottom = AreaBounds.LatitudeToMercator(this.bottom); ;
            mercatorBounds.right = AreaBounds.LongitudeToMercator(this.right); ;

            return mercatorBounds;
        }
    }

    public class AreaBounds
    {
        public static bool IsValidLatitude(double lat)
        {
            return (lat >= -85.0d) && (lat <= 85.0d);
        }

        public static bool IsValidLongitude(double lon)
        {
            return (lon >= -180.0d) && (lon <= 180.0d);
        }

        //  Computes the bounding latitudes and longitudes for each tile of a grid
        //  centered on {latitudeCenterPt, longitudeCenterPt}. The output is an array of 
        //  latitudes ordered from north to south and longitudes ordered east to west.
        public static bool Wgs84CenterPtToTileGrid(
            double latitideCenterPt, double longitudeCenterPt,
            double tileSize /* km */, int tileCountX, int tileCountY,
            out List<double> latitudes, out List<double> longitudes)
        {
            latitudes = new List<double>();
            longitudes = new List<double>();

            try
            {
                Trace.Assert(IsValidLatitude(latitideCenterPt),
                    "Latitude value {0} must be between -85 and 85.", latitideCenterPt);
                Trace.Assert(IsValidLongitude(longitudeCenterPt),
                    "Longitude value {0} must be between -180 and 180.", longitudeCenterPt);
                Trace.Assert(tileCountX > 0 && tileCountX % 2 == 0,
                    "Tile count '{0}' is not an even number > 0 ", tileCountX);
                Trace.Assert(tileCountY > 0 && tileCountY % 2 == 0,
                    "Tile count '{0}' is not an even number > 0 ", tileCountY);
            }
            catch
            {
                return false;
            }

            // Initialize output
            while (latitudes.Count <= tileCountY)
            {
                latitudes.Add(GeoConst.INVALID_LATITUDE);
            }
            while (longitudes.Count <= tileCountX)
            {
                longitudes.Add(GeoConst.INVALID_LATITUDE);
            }
            int iCenterLat = (tileCountY / 2);
            int iCenterLon = (tileCountX / 2);
            latitudes[iCenterLat] = latitideCenterPt;     
            longitudes[iCenterLon] = longitudeCenterPt;

            //  Generate WGS 84 bounding boxes by expanding outward 
            //  from the centerpoint by 1 tile in all directions
            //  until the outermost tile in each dimension is reached.
            int iLat = 0, iLon = 0;
            double latitudeTop, longitudeLeft, latitudeBottom, longitudeRight;

            while (tileCountX > 0 || tileCountY > 0)
            {
                if (tileCountY > 0)
                {
                    iLat++;
                    tileCountY -= 2;
                }
                if (tileCountX > 0)
                {
                    iLon++;
                    tileCountX -= 2;
                }

                if (!Wgs84CenterPtToBBox(
                        latitideCenterPt, longitudeCenterPt,
                        tileSize * iLat * 2, tileSize * iLon * 2, 
                        out latitudeTop, out longitudeLeft, out latitudeBottom, out longitudeRight))
                {
                    latitudes.Clear();
                    longitudes.Clear();
                    return false;
                }

                latitudes[iCenterLat - iLat] = latitudeTop; 
                longitudes[iCenterLon - iLon] = longitudeLeft;
                latitudes[iCenterLat + iLat] = latitudeBottom; 
                longitudes[iCenterLon + iLon] = longitudeRight;
            }
            return true;
        }

        //  Computes the bounding latitudes and longitudes for an area of the 
        //  specified distances in north-south and east-west dimensions,
        //  centered on {latitudeCenterPt, longitudeCenterPt}. 
        public static bool Wgs84CenterPtToBBox(
            double latitideCenterPt, double longitudeCenterPt,
            double areaSizeLat /* km */, double areaSizeLon /* km */,
            out double latitudeTop, out double longitudeLeft, out double latitudeBottom, out double longitudeRight)
        {
            latitudeTop = latitudeBottom = GeoConst.INVALID_LATITUDE;
            longitudeLeft = longitudeRight = GeoConst.INVALID_LATITUDE;

            try
            {
                Trace.Assert(IsValidLatitude(latitideCenterPt),
                    "Latitude value {0} must be between -85 and 85.", latitideCenterPt);
                Trace.Assert(IsValidLongitude(longitudeCenterPt),
                    "Longitude value {0} must be between -180 and 180.", longitudeCenterPt);
            }
            catch
            {
                return false;
            }

            // Offsets in meters
            double dn = (areaSizeLat / 2d) * 1000d;
            double de = (areaSizeLon / 2d) * 1000d;

            // Coordinate offsets in radians
            double dLat = dn / GeoConst.EARTH_RADIUS_EQM;
            double dLon = de / (GeoConst.EARTH_RADIUS_EQM * Math.Cos(Math.PI * latitideCenterPt / 180));

            latitudeTop = latitideCenterPt + dLat * 180d / Math.PI;
            longitudeLeft = longitudeCenterPt - dLon * 180d / Math.PI;
            latitudeBottom = latitideCenterPt - dLat * 180d / Math.PI;
            longitudeRight = longitudeCenterPt + dLon * 180d / Math.PI;

            return true;
        }

        public static void Wgs84PointAtDistanceFrom(
            double latitudeFrom,
            double longitudeFrom,
            double initialBearingRadians,
            double distanceKilometres,
            out double latitudeTo, out double longitudeTo)
        {
            double Wgs84Km =
                GeoConst.EARTH_RADIUS_POK +
                (90 - Math.Abs(latitudeFrom)) / 90 * (GeoConst.EARTH_RADIUS_EQK - GeoConst.EARTH_RADIUS_POK);

            double distRatio = distanceKilometres / Wgs84Km;
            double distRatioSine = Math.Sin(distRatio);
            double distRatioCosine = Math.Cos(distRatio);

            double startLatRad = DegreesToRadians(latitudeFrom);
            double startLonRad = DegreesToRadians(longitudeFrom);

            double startLatCos = Math.Cos(startLatRad);
            double startLatSin = Math.Sin(startLatRad);

            double endLatRads = Math.Asin((startLatSin * distRatioCosine) + (startLatCos * distRatioSine * Math.Cos(initialBearingRadians)));

            double endLonRads = startLonRad
                + Math.Atan2(
                    Math.Sin(initialBearingRadians) * distRatioSine * startLatCos,
                    distRatioCosine - startLatSin * Math.Sin(endLatRads));

            latitudeTo = RadiansToDegrees(endLatRads);
            longitudeTo = RadiansToDegrees(endLonRads);
        }

        public static double DegreesToRadians(double degrees)
        {
            return degrees * GeoConst.DEGREES_TO_RADIANS_COEFF;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * GeoConst.RADIANS_TO_DEGREES_COEFF;
        }

        public static double LatitudeToMercator(double lat)
        {
            return Math.Log(Math.Tan((90.0 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0) * GeoConst.EARTH_RADIUS_TO_RADIANS_COEFF;
        }

        public static double LongitudeToMercator(double lon)
        {
            return lon * 20037508.34 / 180.0;
        }

        public static double[] GetNormalizedDelta(
            double lat, double lon,
            double worldTop, double worldLeft,
            double latSize, double lonSize)
        {
            double worldBottom = worldTop - latSize;
            double worldRight = worldLeft + lonSize;
            double latMultiplier = (worldTop - lat) / (worldTop - worldBottom);
            double lonMultiplier = (worldRight - lon) / (worldRight - worldLeft);

            return new double[] { latMultiplier, lonMultiplier };
        }

        public static Vector2d GetWorldPositionFromTile(
            double latDelta, double lonDelta,
            double worldSizeLat, double worldSizeLon)
        {
            return new Vector2d(-worldSizeLon * lonDelta, worldSizeLat * (1 - latDelta));
        }

        public static double MercatorLatitudeDriftFactor(double latitude)
        {
            const double c2 = 0.00001120378;
            double latRadians = DegreesToRadians(latitude);
            return (1 + c2 * (Math.Cos(2 * latRadians) - 1)) / Math.Cos(latRadians);
        }
    }
}
