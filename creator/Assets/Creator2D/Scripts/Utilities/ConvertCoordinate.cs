using System;
using UnityEngine;
class ConvertCoordinate
{

    private const int EARTH_RADIUS = 6378137;
    private const float ORIGIN_SHIFT = 2 * Mathf.PI * EARTH_RADIUS / 2;

    private static Vector2 REF_POINT = new Vector2(0.0f, 0.0f);

    private const float SCALE = 1.0f;

    public static Vector2 GeoToWorldPosition(float lat, float lon)
    {
        var posx = lon * ORIGIN_SHIFT / 180;
        var posy = Mathf.Log(Mathf.Tan((90 + lat) * Mathf.PI / 360)) / (Mathf.PI / 180);
        posy = posy * ORIGIN_SHIFT / 180;
        return new Vector2((posx - REF_POINT.x) * SCALE, (posy - REF_POINT.y) * SCALE);
    }

    public class GeoPosition
    {
        public float latitude;
        public float longitude;

        public GeoPosition(float longitude, float latitude)
        {
            this.longitude = longitude;
            this.latitude = latitude;
        }
    }

    public static GeoPosition WorldPositionToGeo(Vector2 m)
    {
        var actualX = (float)(m.x + REF_POINT.x) / SCALE;
        var actualY = (float)(m.y + REF_POINT.y) / SCALE;

        var vx = (float)(actualX / ORIGIN_SHIFT) * 180;
        var vy = (actualY / ORIGIN_SHIFT) * 180;
        vy = 180 / Mathf.PI * (2 * Mathf.Atan(Mathf.Exp(vy * Mathf.PI / 180)) - Mathf.PI / 2);
        return new GeoPosition(vy, vx);
    }

    public static GeoPosition WorldPositionToLatLon(Vector2 m)
    {
        var actualX = (float)(m.x + REF_POINT.x) / SCALE;
        var actualY = (float)(m.y + REF_POINT.y) / SCALE;

        var vx = (float)(actualX / ORIGIN_SHIFT) * 180;
        var vy = (actualY / ORIGIN_SHIFT) * 180;
        vy = 180 / Mathf.PI * (2 * Mathf.Atan(Mathf.Exp(vy * Mathf.PI / 180)) - Mathf.PI / 2);
        return new GeoPosition(vy, vx);
    }

    public static Vector2 PixelToUnit(Sprite sprite)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit;
        int width = sprite.texture.width;
        int height = sprite.texture.height;
        return new Vector2(width / pixelsPerUnit, height / pixelsPerUnit);
    }

}