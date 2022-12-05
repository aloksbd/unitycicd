using System;
using TerrainEngine;

public class LatLonInput
{
    public bool IsValid
    {
        get 
        {
            return (latitude >= GeoConst.LATITUDE_MIN && latitude <= GeoConst.LATITUDE_MAX) &&
                   (longitude >= GeoConst.LONGITUDE_MIN && longitude <= GeoConst.LONGITUDE_MAX);
        }
    }
    public string ErrorMessage
    {
        get { return errorMsg; }
    }

    public double Latitude
    {
        get { return latitude; }
        set 
        {
            if (value >= GeoConst.LATITUDE_MIN && value <= GeoConst.LATITUDE_MAX)
            {
                latitude = value;
            }
        }
    }

    public double Longitude
    {
        get { return longitude; }
        set
        {
            if (value >= GeoConst.LONGITUDE_MIN && value <= GeoConst.LONGITUDE_MAX)
            {
                longitude = value;
            }
        }
    }

    public bool Parse(string value)
    {
        if (value.Length > 0 && value != valuePrevious)
        {
            valuePrevious = value;
            try
            {
                string[] tokens = { ",", " ", ", " };
                string[] latlonS = value.Split(tokens, System.StringSplitOptions.RemoveEmptyEntries);
                double[] latlonD = Array.ConvertAll(latlonS, double.Parse);
                if (latlonD.Length == 2 &&
                    latlonD[0] >= GeoConst.LATITUDE_MIN && latlonD[0] <= GeoConst.LATITUDE_MAX &&
                    latlonD[1] >= GeoConst.LONGITUDE_MIN && latlonD[1] <= GeoConst.LONGITUDE_MAX)
                {
                    latitude = latlonD[0];
                    longitude = latlonD[1];
                    errorMsg = "";
                    return true;
                }
                else
                {
                    OnParseError(
                        String.Format("The valid range for latitude is {0} to {1}, longitude {2} to {3}.",
                            (int)GeoConst.LATITUDE_MIN, (int)GeoConst.LATITUDE_MAX,
                            (int)GeoConst.LONGITUDE_MIN, (int)GeoConst.LONGITUDE_MAX)
                    );
                }
            }
            catch (Exception e)
            {
                OnParseError("Expecting latitude, longitude");
            }
        }
        return false;
    }

    private void OnParseError(string msg)
    {
        latitude = GeoConst.INVALID_LATITUDE;
        longitude = GeoConst.INVALID_LATITUDE;
        errorMsg = msg;
    }

    private string valuePrevious;
    private double latitude = GeoConst.INVALID_LATITUDE;
    private double longitude = GeoConst.INVALID_LATITUDE;
    private string errorMsg = "";
}
