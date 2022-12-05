/// <summary>
/// This is the precise Geo-Location algorithm for objects in Unity based on Mercator projection.
/// It keeps all the calculations including vectors on double precision and finally coverts them
/// to floats when falls back to Unity transforms.
/// </summary>

using UnityEngine;
using System;
using TerrainEngine;

[ExecuteInEditMode]
public class LatLon2UnityMercator : MonoBehaviour
{
    //  WGS84 bounds
    public double areaTop;
    public double areaBottom;
    public double areaLeft;
    public double areaRight;

    //  WGS84 extents
    public double areaWidth;
    public double areaLength;

    public bool forceMoveToLatLon = true;

    public double destinationLat;
    public double destinationLon;

    public double scaleFactor = 1;

    private static GeoLocation.Config areaConfig;

    void Update ()
    {
        transform.position = GeoLocation.Locate(
            destinationLat,
            destinationLon, 
            ref AreaConfigFromInspector(this));
    }

    static ref GeoLocation.Config AreaConfigFromInspector(LatLon2UnityMercator _this)
    {
        if (areaConfig == null)
        {
            areaConfig = new GeoLocation.Config();
        }

        areaConfig.wgs84Bounds.top = _this.areaTop;
        areaConfig.wgs84Bounds.left = _this.areaLeft;
        areaConfig.wgs84Bounds.bottom = _this.areaBottom;
        areaConfig.wgs84Bounds.right = _this.areaRight;

        areaConfig.worldAreaWidth = _this.areaWidth;
        areaConfig.worldAreaHeight = _this.areaLength;
        areaConfig.worldScaleFactor = _this.scaleFactor;

        return ref areaConfig;
    }
}

