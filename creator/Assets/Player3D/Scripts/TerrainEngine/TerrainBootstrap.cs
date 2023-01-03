using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TerrainEngine;

public class TerrainBootstrap : MonoBehaviour
{
    private static double s_Latitude = GeoConst.INVALID_LATITUDE;
    private static double s_Longitude = GeoConst.INVALID_LONGITUDE;

    private const double WAIKIKI_LATITUDE = 21.276480405088737d;
    private const double WAIKIKI_LONGITUDE = -157.82762347699511d;

    //private const double WAIKIKI_LATITUDE = 21.28101225;
    //private const double WAIKIKI_LONGITUDE = -157.82576905;

    //private const double WAIKIKI_LATITUDE = 21.294927;
    //private const double WAIKIKI_LONGITUDE = -157.852626;

    private readonly TerrainSettings _settings = new TerrainSettings();

    public static double Latitude
    {
        get
        {
            return (AreaBounds.IsValidLatitude(s_Latitude)) ?
                s_Latitude :
                WAIKIKI_LATITUDE;
        }

        set
        {
            if (AreaBounds.IsValidLatitude(value))
            {
                s_Latitude = value;
            }
            else
            {
                Trace.Warning("Latitude value {0} must be between -85 and 85.", value);
            }
        }
    }

    public static double Longitude
    {
        get
        {
            return (AreaBounds.IsValidLongitude(s_Longitude)) ?
                s_Longitude :
                WAIKIKI_LONGITUDE;
        }

        set
        {
            if (AreaBounds.IsValidLongitude(value))
            {
                s_Longitude = value;
            }
            else
            {
                Trace.Warning("Longitude value must be between -180 and 180");
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (DeeplinkHandler.Instance.isDeeplinkCalled)
        {
            if (DeeplinkHandler.PlayData.latitude != null && DeeplinkHandler.PlayData.longitude != null)
            {
                Latitude = Convert.ToDouble(DeeplinkHandler.PlayData.latitude);
                Longitude = Convert.ToDouble(DeeplinkHandler.PlayData.longitude);
            }
        }
        if (TerrainController.Get() != null)
        {
            _settings.latitudeUser = Latitude.ToString();
            _settings.longitudeUser = Longitude.ToString();
            TerrainController.Settings = _settings;
        }
    }

}
