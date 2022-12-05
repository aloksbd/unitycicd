using System;

namespace TerrainEngine
{
    public class GeoConst
    {
        //  WGS84 values
        public const double INVALID_LATITUDE  = 9999d;
        public const double INVALID_LONGITUDE = 9999d;
        public const double LATITUDE_MIN      = -85.0d;
        public const double LATITUDE_MAX      =  85.0d;
        public const double LONGITUDE_MIN     = -180.0d;
        public const double LONGITUDE_MAX     =  180.0d;

        //  Angular trig
        public const double DEGREES_TO_RADIANS_COEFF = (Math.PI / 180d); // radians = degrees * DEGREES_TO_RADIANS_COEFF
        public const double RADIANS_TO_DEGREES_COEFF = (180d / Math.PI); // degress = radians * RADIANS_TO_DEGREES_COEFF

        //  
        //  Earth's spherical radius
        //
        //  It's not a perfect sphere, but vertically squished. Precision calls for 
        //  discriminatingbetween its equatorial (horizontal) from polar (vertical) radii.

        public const double EARTH_RADIUS_EQM = 6378137; // earth equatorial radius in meters
        public const double EARTH_RADIUS_EQK = (EARTH_RADIUS_EQM / 1000d);   // earth equatorial radius in kilometers
        public const double EARTH_RADIUS_EQM_x_PI = (EARTH_RADIUS_EQM * Math.PI); // earth equatorial radius in meters * pi
        public const double EARTH_RADIUS_TO_RADIANS_COEFF = (EARTH_RADIUS_EQM * DEGREES_TO_RADIANS_COEFF); // earth eq radius to radians coefficient

        public const double EARTH_RADIUS_POM = 6356752.3142; // earth polar radius in meters
        public const double EARTH_RADIUS_POK = (EARTH_RADIUS_POM / 1000d); // earth polar radius in kilometers

        //  misc
        public const float EVEREST_PEAK_METERS = 8848.0f;
    }
}
